namespace Microsoft.Identity.Client.ApiConfig.Parameters
{
    internal class AcquireTokenSilentParameters : IAcquireTokenParameters
    {
        public bool ForceRefresh { get; set; }
        public string LoginHint { get; set; }
        public IAccount Account { get; set; }
        public bool? SendX5C { get; set; } 

        /// <inheritdoc />
        public void LogParameters(IMsalLogger logger)
        {
            logger.Info("=== AcquireTokenSilent Parameters ===");
            logger.Info("LoginHint provided: " + !string.IsNullOrEmpty(LoginHint));
            logger.InfoPii(
                "Account provided: " + ((Account != null) ? Account.ToString() : "false"),
                "Account provided: " + (Account != null));
            logger.Info("ForceRefresh: " + ForceRefresh);
        }
    }
}
