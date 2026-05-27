namespace DigitalCards.Domain;

public sealed class BusinessSubscription
{
    public BusinessSubscription(
        Guid businessId,
        string subscriptionStatus,
        int maxClients,
        bool createdViaSelfService,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        string? stripePlanKey = null,
        string? stripeCustomerId = null,
        string? stripeSubscriptionId = null,
        string? stripeCheckoutSessionId = null,
        DateTimeOffset? subscriptionEndsAt = null,
        DateTimeOffset? graceEndsAt = null)
    {
        BusinessId = businessId;
        SubscriptionStatus = subscriptionStatus;
        MaxClients = maxClients;
        CreatedViaSelfService = createdViaSelfService;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        StripePlanKey = stripePlanKey;
        StripeCustomerId = stripeCustomerId;
        StripeSubscriptionId = stripeSubscriptionId;
        StripeCheckoutSessionId = stripeCheckoutSessionId;
        SubscriptionEndsAt = subscriptionEndsAt;
        GraceEndsAt = graceEndsAt;
    }

    public Guid BusinessId { get; }
    public string SubscriptionStatus { get; }
    public int MaxClients { get; }
    public bool CreatedViaSelfService { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; }
    public string? StripePlanKey { get; }
    public string? StripeCustomerId { get; }
    public string? StripeSubscriptionId { get; }
    public string? StripeCheckoutSessionId { get; }
    public DateTimeOffset? SubscriptionEndsAt { get; }
    public DateTimeOffset? GraceEndsAt { get; }

    public BusinessSubscription WithStripeActivation(
        string customerId,
        string subscriptionId,
        DateTimeOffset subscriptionEndsAt,
        DateTimeOffset updatedAt)
    {
        return new BusinessSubscription(
            BusinessId,
            "active",
            MaxClients,
            CreatedViaSelfService,
            CreatedAt,
            updatedAt,
            StripePlanKey,
            customerId,
            subscriptionId,
            StripeCheckoutSessionId,
            subscriptionEndsAt,
            graceEndsAt: null);
    }

    public BusinessSubscription WithRenewal(DateTimeOffset subscriptionEndsAt, DateTimeOffset updatedAt)
    {
        return new BusinessSubscription(
            BusinessId,
            "active",
            MaxClients,
            CreatedViaSelfService,
            CreatedAt,
            updatedAt,
            StripePlanKey,
            StripeCustomerId,
            StripeSubscriptionId,
            StripeCheckoutSessionId,
            subscriptionEndsAt,
            graceEndsAt: null);
    }

    public BusinessSubscription WithPastDue(DateTimeOffset graceEndsAt, DateTimeOffset updatedAt)
    {
        return new BusinessSubscription(
            BusinessId,
            "past_due",
            MaxClients,
            CreatedViaSelfService,
            CreatedAt,
            updatedAt,
            StripePlanKey,
            StripeCustomerId,
            StripeSubscriptionId,
            StripeCheckoutSessionId,
            SubscriptionEndsAt,
            graceEndsAt);
    }

    public BusinessSubscription WithCanceled(DateTimeOffset updatedAt)
    {
        return new BusinessSubscription(
            BusinessId,
            "canceled",
            MaxClients,
            CreatedViaSelfService,
            CreatedAt,
            updatedAt,
            StripePlanKey,
            StripeCustomerId,
            StripeSubscriptionId,
            StripeCheckoutSessionId,
            SubscriptionEndsAt,
            GraceEndsAt);
    }

    public BusinessSubscription WithCheckoutSession(string sessionId, DateTimeOffset updatedAt)
    {
        return new BusinessSubscription(
            BusinessId,
            SubscriptionStatus,
            MaxClients,
            CreatedViaSelfService,
            CreatedAt,
            updatedAt,
            StripePlanKey,
            StripeCustomerId,
            StripeSubscriptionId,
            sessionId,
            SubscriptionEndsAt,
            GraceEndsAt);
    }

    public BusinessSubscription WithPlanUpdate(string planKey, int maxClients, DateTimeOffset updatedAt)
    {
        return new BusinessSubscription(
            BusinessId,
            SubscriptionStatus,
            maxClients,
            CreatedViaSelfService,
            CreatedAt,
            updatedAt,
            planKey,
            StripeCustomerId,
            StripeSubscriptionId,
            StripeCheckoutSessionId,
            SubscriptionEndsAt,
            GraceEndsAt);
    }
}
