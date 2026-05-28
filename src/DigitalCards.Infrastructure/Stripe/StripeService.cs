using DigitalCards.Application.Abstractions;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace DigitalCards.Infrastructure.Stripe;

public sealed class StripeService : IStripeService
{
    private readonly StripeOptions _options;

    public StripeService(IOptions<StripeOptions> options)
    {
        _options = options.Value;
        StripeConfiguration.ApiKey = _options.SecretKey;
    }

    public async Task<(string Url, string SessionId)> CreateCheckoutSessionAsync(
        string planKey,
        Guid businessId,
        string successUrl,
        string cancelUrl,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Plans.TryGetValue(planKey, out var plan))
            throw new InvalidOperationException($"Stripe plan '{planKey}' not configured.");

        var meta = new Dictionary<string, string>
        {
            ["business_id"] = businessId.ToString("D"),
            ["plan_key"] = planKey
        };

        var service = new SessionService();
        var session = await service.CreateAsync(new SessionCreateOptions
        {
            Mode = "subscription",
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Price = plan.PriceId,
                    Quantity = 1
                }
            ],
            Metadata = meta,
            SubscriptionData = new SessionSubscriptionDataOptions
            {
                Metadata = meta
            },
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl
        }, cancellationToken: cancellationToken);

        return (session.Url, session.Id);
    }

    public async Task<(string? BusinessId, string? PlanKey)> GetCheckoutSessionMetadataAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        var service = new SessionService();
        var session = await service.GetAsync(sessionId, cancellationToken: cancellationToken);
        string? businessId = null;
        string? planKey = null;
        session.Metadata?.TryGetValue("business_id", out businessId);
        session.Metadata?.TryGetValue("plan_key", out planKey);
        return (businessId, planKey);
    }

    public async Task<string> CreatePortalSessionAsync(
        string stripeCustomerId,
        string returnUrl,
        CancellationToken cancellationToken = default)
    {
        var service = new global::Stripe.BillingPortal.SessionService();
        var session = await service.CreateAsync(new global::Stripe.BillingPortal.SessionCreateOptions
        {
            Customer = stripeCustomerId,
            ReturnUrl = returnUrl
        }, cancellationToken: cancellationToken);

        return session.Url;
    }

    public StripeWebhookEvent ConstructWebhookEvent(string payload, string stripeSignatureHeader)
    {
        var stripeEvent = EventUtility.ConstructEvent(
            payload,
            stripeSignatureHeader,
            _options.WebhookSecret,
            throwOnApiVersionMismatch: false);

        return MapEvent(stripeEvent);
    }

    private static StripeWebhookEvent MapEvent(Event stripeEvent)
    {
        string? businessId = null;
        string? planKey = null;
        string? customerId = null;
        string? subscriptionId = null;
        string? checkoutSessionId = null;
        DateTimeOffset? periodEnd = null;

        switch (stripeEvent.Type)
        {
            case "checkout.session.completed":
            {
                if (stripeEvent.Data.Object is Session session)
                {
                    session.Metadata.TryGetValue("business_id", out businessId);
                    session.Metadata.TryGetValue("plan_key", out planKey);
                    customerId = session.CustomerId;
                    subscriptionId = session.SubscriptionId;
                    checkoutSessionId = session.Id;
                }
                break;
            }
            case "invoice.payment_succeeded":
            case "invoice.payment_failed":
            {
                if (stripeEvent.Data.Object is Invoice invoice)
                {
                    customerId = invoice.CustomerId;
                    subscriptionId = invoice.SubscriptionId;
                    if (invoice.Lines?.Data?.Count > 0)
                    {
                        var line = invoice.Lines.Data[0];
                        planKey = line.Metadata?.TryGetValue("plan_key", out var pk) == true ? pk : null;
                        if (line.Period?.End is DateTime lineEnd)
                            periodEnd = new DateTimeOffset(lineEnd, TimeSpan.Zero);
                    }
                    businessId = invoice.Metadata?.TryGetValue("business_id", out var bid) == true ? bid : null;
                }
                break;
            }
            case "customer.subscription.deleted":
            case "customer.subscription.updated":
            {
                if (stripeEvent.Data.Object is global::Stripe.Subscription sub)
                {
                    sub.Metadata?.TryGetValue("business_id", out businessId);
                    sub.Metadata?.TryGetValue("plan_key", out planKey);
                    customerId = sub.CustomerId;
                    subscriptionId = sub.Id;
                    if (sub.CurrentPeriodEnd != default)
                        periodEnd = new DateTimeOffset(sub.CurrentPeriodEnd, TimeSpan.Zero);
                }
                break;
            }
        }

        return new StripeWebhookEvent(
            stripeEvent.Type,
            businessId,
            planKey,
            customerId,
            subscriptionId,
            checkoutSessionId,
            periodEnd);
    }
}
