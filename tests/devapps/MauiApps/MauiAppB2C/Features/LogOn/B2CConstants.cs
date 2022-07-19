namespace UserDetailsClient.Core.Features.LogOn
{
    public static class B2CConstants
    {
        // Azure AD B2C Coordinates
        public static string Tenant = "fabrikamb2c.onmicrosoft.com";
        public static string AzureADB2CHostname = "fabrikamb2c.b2clogin.com";
        public static string ClientID = "e5737214-6372-472c-a85a-68e8fbe6cf3c";
        public static string PolicySignUpSignIn = "b2c_1_susi";
        public static string PolicyEditProfile = "b2c_1_edit_profile";
        public static string PolicyResetPassword = "b2c_1_reset";

        public static string[] Scopes = { "https://fabrikamb2c.onmicrosoft.com/helloapi/demo.read" };

        public static string AuthorityBase = $"https://{AzureADB2CHostname}/tfp/{Tenant}/";
        public static string AuthoritySignInSignUp = $"{AuthorityBase}{PolicySignUpSignIn}";
        public static string AuthorityEditProfile = $"{AuthorityBase}{PolicyEditProfile}";
        public static string AuthorityPasswordReset = $"{AuthorityBase}{PolicyResetPassword}";
        public static string IOSKeyChainGroup = "com.microsoft.adalcache";
    }
}