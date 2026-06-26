namespace EShop.ApiGateway.Options;

public class RateLimitingOptions
{
    public PolicyOptions Auth { get; set; } = new();
    public PolicyOptions General { get; set; } = new();

    public static RateLimitingOptions Default() => new()
    {
        Auth = new() { PermitLimit = 10, WindowMinutes = 1 },
        General = new()
        {
            PermitLimitAuthenticated = 100,
            PermitLimitAnonymous = 30,
            WindowMinutes = 1
        }
    };

    public class PolicyOptions
    {
        public int PermitLimit { get; set; }
        public int WindowMinutes { get; set; }
        public int? PermitLimitAuthenticated { get; set; }
        public int? PermitLimitAnonymous { get; set; }
    }
}
