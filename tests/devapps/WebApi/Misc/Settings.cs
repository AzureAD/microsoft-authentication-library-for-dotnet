namespace WebApi.Controllers
{
    internal static class Settings
    {
        public const int NetworkAccessPenaltyMs = 200;
        public const double CacheHitRatio = 0.95;
        public const int CacheAccessPenaltyMs = 100;

        // for S2S
        public const int NumberOfTenants = 500;

        // for OBO
        public const int NumberOfUsers = 300;
        public const int NumberOfUsersRefreshFlow = 50;
    }
}
