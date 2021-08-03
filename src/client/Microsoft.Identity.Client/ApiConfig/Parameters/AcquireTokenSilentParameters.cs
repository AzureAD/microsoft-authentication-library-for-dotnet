using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.ApiConfig.Parameters
{
    internal class AcquireTokenSilentParameters : IAcquireTokenParameters
    {
        public bool ForceRefresh { get; set; }
        public string LoginHint { get; set; }
        public IAccount Account { get; set; }
        public bool SendX5C { get; set; } 
        
        internal bool SetPerRequestX5C = false;

        /// <inheritdoc />
        public void LogParameters(ICoreLogger logger)
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
