using DigitalCards.Application.Abstractions;
using DigitalCards.Application.Models;
using DigitalCards.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DigitalCards.Application.Services;

public sealed class BusinessSignupService
{
    private const string DefaultLogoPath = "/img/demo-coffee.svg";
    private const int GracePeriodDays = 3;

    private readonly IBusinessRepository _businesses;
    private readonly IBusinessCredentialRepository _businessCredentials;
    private readonly IPilotBusinessRepository _pilotBusinesses;
    private readonly IBusinessSubscriptionRepository _subscriptions;
    private readonly IStripeService _stripe;
    private readonly IEmailSender _emailSender;
    private readonly IClock _clock;
    private readonly IPasswordHasher<BusinessPasswordHashSubject> _passwordHasher;
    private readonly StripeSignupOptions _stripeOptions;
    private readonly ILogger<BusinessSignupService> _logger;

    public BusinessSignupService(
        IBusinessRepository businesses,
        IBusinessCredentialRepository businessCredentials,
        IPilotBusinessRepository pilotBusinesses,
        IBusinessSubscriptionRepository subscriptions,
        IStripeService stripe,
        IEmailSender emailSender,
        IClock clock,
        IPasswordHasher<BusinessPasswordHashSubject> passwordHasher,
        IOptions<StripeSignupOptions> stripeOptions,
        ILogger<BusinessSignupService> logger)
    {
        _businesses = businesses;
        _businessCredentials = businessCredentials;
        _pilotBusinesses = pilotBusinesses;
        _subscriptions = subscriptions;
        _stripe = stripe;
        _emailSender = emailSender;
        _clock = clock;
        _passwordHasher = passwordHasher;
        _stripeOptions = stripeOptions.Value;
        _logger = logger;
    }

    public async Task<SignupResult> SignupAsync(SignupCommand command, CancellationToken cancellationToken = default)
    {
        var name = command.BusinessName.Trim();
        var email = command.BusinessEmail.Trim().ToLowerInvariant();
        var planKey = command.PlanKey.Trim();

        if (string.IsNullOrWhiteSpace(name) || name.Length > 30)
            return Fail("El nombre del negocio debe tener entre 1 y 30 caracteres.");

        if (string.IsNullOrWhiteSpace(email) || email.Length > 30)
            return Fail("El correo del negocio debe tener entre 1 y 30 caracteres.");

        if (string.IsNullOrWhiteSpace(command.Password) || command.Password.Length < 8)
            return Fail("La contrasena debe tener al menos 8 caracteres.");

        if (!_stripeOptions.Plans.TryGetValue(planKey, out var plan))
            return Fail("Plan no valido. Elige Basic, Pro o Business.");

        if (await _businesses.FindByEmailAsync(email, cancellationToken) is not null)
            return Fail("Ya existe un negocio con ese correo.");

        if (await _businesses.FindByNameAsync(name, cancellationToken) is not null)
            return Fail("Ya existe un negocio con ese nombre.");

        var now = _clock.UtcNow;
        Business business;
        try
        {
            business = await _businesses.AddAsync(
                new Business(Guid.NewGuid(), name, email, string.Empty, DefaultLogoPath),
                cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return Fail("Ya existe un negocio con ese nombre o correo.");
        }

        var subject = new BusinessPasswordHashSubject(business.Id);
        await _businessCredentials.UpsertAsync(
            new BusinessCredential(
                business.Id,
                _passwordHasher.HashPassword(subject, command.Password),
                now,
                now),
            cancellationToken);

        await _pilotBusinesses.UpsertAsync(
            new PilotBusinessAccess(
                business.Id,
                isEnabled: false,
                notes: $"Self-service signup, plan: {planKey}",
                now,
                now,
                updatedByAdminUserId: null,
                BusinessActivationStatus.Inactive),
            cancellationToken);

        await _subscriptions.UpsertAsync(
            new BusinessSubscription(
                business.Id,
                subscriptionStatus: "pending_payment",
                maxClients: plan.MaxClients,
                createdViaSelfService: true,
                now,
                now,
                stripePlanKey: planKey),
            cancellationToken);

        _logger.LogInformation("Self-service signup created business {BusinessId} ({Name}) on plan {Plan}.", business.Id, name, planKey);
        return new SignupResult(business.Id, null);
    }

    public async Task<string> CreateOrResumeCheckoutAsync(Guid businessId, string baseUrl, CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptions.FindByBusinessIdAsync(businessId, cancellationToken)
            ?? throw new InvalidOperationException($"No subscription found for business {businessId}.");

        var planKey = subscription.StripePlanKey ?? "Basic";
        var successUrl = $"{baseUrl.TrimEnd('/')}/stripe/success?session_id={{CHECKOUT_SESSION_ID}}";
        var cancelUrl = $"{baseUrl.TrimEnd('/')}/stripe/cancel?businessId={businessId:D}";

        var sessionUrl = await _stripe.CreateCheckoutSessionAsync(planKey, businessId, successUrl, cancelUrl, cancellationToken);

        var now = _clock.UtcNow;
        await _subscriptions.UpsertAsync(subscription.WithCheckoutSession(ExtractSessionId(sessionUrl), now), cancellationToken);

        return sessionUrl;
    }

    public async Task<(bool Success, string? Error)> ProcessWebhookAsync(string payload, string stripeSignatureHeader, CancellationToken cancellationToken = default)
    {
        StripeWebhookEvent webhookEvent;
        try
        {
            webhookEvent = _stripe.ConstructWebhookEvent(payload, stripeSignatureHeader);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Stripe webhook signature verification failed: {Message}", ex.Message);
            return (false, "Invalid signature.");
        }

        try
        {
            await HandleWebhookEventAsync(webhookEvent, cancellationToken);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Stripe webhook event {Type}.", webhookEvent.Type);
            return (false, "Internal error processing webhook.");
        }
    }

    public async Task<IReadOnlyList<AbandonedSignupDto>> ListAbandonedAsync(CancellationToken cancellationToken = default)
    {
        var threshold = _clock.UtcNow.AddHours(-24);
        var subs = await _subscriptions.ListAbandonedAsync(threshold, cancellationToken);

        var results = new List<AbandonedSignupDto>(subs.Count);
        foreach (var sub in subs)
        {
            var business = await _businesses.FindByIdAsync(sub.BusinessId, cancellationToken);
            if (business is null) continue;
            results.Add(new AbandonedSignupDto(business.Id, business.Name, business.Email, sub.StripePlanKey, sub.CreatedAt));
        }

        return results;
    }

    public async Task DeactivateExpiredGracePeriodAsync(CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var expired = await _subscriptions.ListPastDueGraceExpiredAsync(now, cancellationToken);

        foreach (var sub in expired)
        {
            await CancelSubscriptionInternalAsync(sub, now, cancellationToken);
            _logger.LogInformation("Auto-deactivated business {BusinessId} after grace period expired.", sub.BusinessId);
        }
    }

    private async Task HandleWebhookEventAsync(StripeWebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        switch (webhookEvent.Type)
        {
            case "checkout.session.completed":
                await HandleCheckoutCompletedAsync(webhookEvent, cancellationToken);
                break;
            case "invoice.payment_succeeded":
                await HandlePaymentSucceededAsync(webhookEvent, cancellationToken);
                break;
            case "invoice.payment_failed":
                await HandlePaymentFailedAsync(webhookEvent, cancellationToken);
                break;
            case "customer.subscription.deleted":
                await HandleSubscriptionDeletedAsync(webhookEvent, cancellationToken);
                break;
            case "customer.subscription.updated":
                await HandleSubscriptionUpdatedAsync(webhookEvent, cancellationToken);
                break;
            default:
                _logger.LogDebug("Unhandled Stripe event type: {Type}", webhookEvent.Type);
                break;
        }
    }

    private async Task HandleCheckoutCompletedAsync(StripeWebhookEvent evt, CancellationToken cancellationToken)
    {
        if (!TryParseBusinessId(evt.BusinessId, out var businessId)) return;

        var sub = await _subscriptions.FindByBusinessIdAsync(businessId, cancellationToken);
        if (sub is null) return;

        var now = _clock.UtcNow;
        var endsAt = evt.PeriodEnd ?? now.AddMonths(1);

        var updated = sub.WithStripeActivation(
            evt.CustomerId ?? string.Empty,
            evt.SubscriptionId ?? string.Empty,
            endsAt,
            now);
        await _subscriptions.UpsertAsync(updated, cancellationToken);

        await ActivatePilotAsync(businessId, now, cancellationToken);

        var business = await _businesses.FindByIdAsync(businessId, cancellationToken);
        if (business is not null && _stripeOptions.Plans.TryGetValue(sub.StripePlanKey ?? string.Empty, out var plan))
        {
            try
            {
                await _emailSender.SendBusinessWelcomeAsync(
                    new BusinessWelcomeEmail(business.Email, business.Name, plan.Name, "/Business/Dashboard"),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send business welcome email to {BusinessId}.", businessId);
            }
        }

        _logger.LogInformation("Business {BusinessId} activated via checkout.session.completed.", businessId);
    }

    private async Task HandlePaymentSucceededAsync(StripeWebhookEvent evt, CancellationToken cancellationToken)
    {
        if (!TryParseBusinessId(evt.BusinessId, out var businessId)) return;

        var sub = await _subscriptions.FindByBusinessIdAsync(businessId, cancellationToken);
        if (sub is null || sub.SubscriptionStatus == "pending_payment") return;

        var now = _clock.UtcNow;
        await _subscriptions.UpsertAsync(sub.WithRenewal(evt.PeriodEnd ?? now.AddMonths(1), now), cancellationToken);
        await ActivatePilotAsync(businessId, now, cancellationToken);

        _logger.LogInformation("Business {BusinessId} subscription renewed.", businessId);
    }

    private async Task HandlePaymentFailedAsync(StripeWebhookEvent evt, CancellationToken cancellationToken)
    {
        if (!TryParseBusinessId(evt.BusinessId, out var businessId)) return;

        var sub = await _subscriptions.FindByBusinessIdAsync(businessId, cancellationToken);
        if (sub is null) return;

        var now = _clock.UtcNow;
        var graceEndsAt = now.AddDays(GracePeriodDays);
        await _subscriptions.UpsertAsync(sub.WithPastDue(graceEndsAt, now), cancellationToken);

        var business = await _businesses.FindByIdAsync(businessId, cancellationToken);
        if (business is not null)
        {
            try
            {
                await _emailSender.SendPaymentFailedAsync(
                    new BusinessPaymentFailedEmail(business.Email, business.Name, graceEndsAt),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send payment failed email to {BusinessId}.", businessId);
            }
        }

        _logger.LogInformation("Business {BusinessId} marked past_due, grace ends {GraceEndsAt}.", businessId, graceEndsAt);
    }

    private async Task HandleSubscriptionDeletedAsync(StripeWebhookEvent evt, CancellationToken cancellationToken)
    {
        if (!TryParseBusinessId(evt.BusinessId, out var businessId)) return;

        var sub = await _subscriptions.FindByBusinessIdAsync(businessId, cancellationToken);
        if (sub is null) return;

        await CancelSubscriptionInternalAsync(sub, _clock.UtcNow, cancellationToken);
        _logger.LogInformation("Business {BusinessId} subscription deleted by Stripe.", businessId);
    }

    private async Task HandleSubscriptionUpdatedAsync(StripeWebhookEvent evt, CancellationToken cancellationToken)
    {
        if (!TryParseBusinessId(evt.BusinessId, out var businessId)) return;
        if (evt.PlanKey is null) return;

        var sub = await _subscriptions.FindByBusinessIdAsync(businessId, cancellationToken);
        if (sub is null) return;

        var maxClients = _stripeOptions.Plans.TryGetValue(evt.PlanKey, out var plan) ? plan.MaxClients : sub.MaxClients;
        await _subscriptions.UpsertAsync(sub.WithPlanUpdate(evt.PlanKey, maxClients, _clock.UtcNow), cancellationToken);
        _logger.LogInformation("Business {BusinessId} plan updated to {Plan}.", businessId, evt.PlanKey);
    }

    private async Task CancelSubscriptionInternalAsync(BusinessSubscription sub, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await _subscriptions.UpsertAsync(sub.WithCanceled(now), cancellationToken);

        var existing = await _pilotBusinesses.FindByBusinessIdAsync(sub.BusinessId, cancellationToken);
        if (existing is not null)
        {
            await _pilotBusinesses.UpsertAsync(
                existing.WithState(false, existing.Notes, now, updatedByAdminUserId: null, BusinessActivationStatus.Inactive),
                cancellationToken);
        }

        var business = await _businesses.FindByIdAsync(sub.BusinessId, cancellationToken);
        if (business is not null)
        {
            try
            {
                await _emailSender.SendSubscriptionCanceledAsync(
                    new BusinessSubscriptionCanceledEmail(business.Email, business.Name),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send cancellation email to business {BusinessId}.", sub.BusinessId);
            }
        }
    }

    private async Task ActivatePilotAsync(Guid businessId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var existing = await _pilotBusinesses.FindByBusinessIdAsync(businessId, cancellationToken);
        var access = existing is null
            ? new PilotBusinessAccess(businessId, true, null, now, now, null, BusinessActivationStatus.ModernPrimary)
            : existing.WithState(true, existing.Notes, now, null, BusinessActivationStatus.ModernPrimary);
        await _pilotBusinesses.UpsertAsync(access, cancellationToken);
    }

    private static bool TryParseBusinessId(string? raw, out Guid businessId)
    {
        businessId = Guid.Empty;
        return !string.IsNullOrWhiteSpace(raw) && Guid.TryParse(raw, out businessId);
    }

    private static string ExtractSessionId(string sessionUrl)
    {
        if (!Uri.TryCreate(sessionUrl, UriKind.Absolute, out var uri)) return sessionUrl;
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        return query["session_id"] ?? sessionUrl;
    }

    private static SignupResult Fail(string message) => new(null, message);
}

public sealed class StripeSignupOptions
{
    public const string SectionName = "DigitalCards:Stripe";
    public Dictionary<string, StripePlanSignupOptions> Plans { get; init; } = new();
}

public sealed class StripePlanSignupOptions
{
    public string PriceId { get; init; } = "";
    public string Name { get; init; } = "";
    public int MaxClients { get; init; } = 300;
}
