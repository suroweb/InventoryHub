namespace Shared.Models;

public enum SubscriptionTier
{
    Free = 0,
    Starter = 1,
    Professional = 2,
    Enterprise = 3
}

public static class SubscriptionTierLimits
{
    public static int GetMaxProducts(SubscriptionTier tier) => tier switch
    {
        SubscriptionTier.Free => 10,
        SubscriptionTier.Starter => 100,
        SubscriptionTier.Professional => 1000,
        SubscriptionTier.Enterprise => int.MaxValue,
        _ => 10
    };

    public static int GetMaxUsers(SubscriptionTier tier) => tier switch
    {
        SubscriptionTier.Free => 1,
        SubscriptionTier.Starter => 5,
        SubscriptionTier.Professional => 25,
        SubscriptionTier.Enterprise => int.MaxValue,
        _ => 1
    };

    public static int GetApiRateLimitPerMinute(SubscriptionTier tier) => tier switch
    {
        SubscriptionTier.Free => 60,
        SubscriptionTier.Starter => 300,
        SubscriptionTier.Professional => 1000,
        SubscriptionTier.Enterprise => 5000,
        _ => 60
    };
}
