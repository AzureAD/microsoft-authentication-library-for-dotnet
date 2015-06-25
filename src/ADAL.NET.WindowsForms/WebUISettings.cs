
namespace Microsoft.IdentityModel.Clients.ActiveDirectory.WindowsForms
{
    public static class WebUISettings
    {
        private static long _silentWebUITimeout = 2000;

        /// <summary>
        /// This is how long all redirect navigations are allowed to run for before a graceful 
        /// termination of the entire browser based authentication process is attempted.
        /// </summary>
        public static long SilentWebUITimeout
        {
            get { return _silentWebUITimeout; } 
            set { _silentWebUITimeout = value; }
        }
    }
}
