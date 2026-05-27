namespace DigitalCards.Application.Abstractions;

public interface IStripeService
{
    Task<string> CreateCheckoutSessionAsync(
        string planKey,
        Guid businessId,
        string successUrl,
        string cancelUrl,
        CancellationToken cancellationToken = default);

    Task<string> CreatePortalSessionAsync(
        string stripeCustomerId,
        string returnUrl,
        CancellationToken cancellationToken = default);

    StripeWebhookEvent ConstructWebhookEvent(string payload, string stripeSignatureHeader);
}

public sealed record StripeWebhookEvent(
    string Type,
    string? BusinessId,
    string? PlanKey,
    string? CustomerId,
    string? SubscriptionId,
    string? CheckoutSessionId,
    DateTimeOffset? PeriodEnd);
