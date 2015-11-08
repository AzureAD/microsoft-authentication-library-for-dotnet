using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSAL.Desktop
{
    public static class PublicClientApplicationExtensions
    {

        //TODO try this with extension method. investigate if they work on mono
        public static async Task<AuthenticationResult> AcquireTokenAsync(this PublicClientApplication application, string[] scope, IPlatformParameters parameters)
        {
            return null;
        }

        // AcquireTokenAsync(string[] scope, IPlatformParameters parameters) will collide with 
        // AcquireTokenAsync(string[] scope, UserIdentifier userId)
        // if null is passed for 2nd parameter
        public static async Task<AuthenticationResult> AcquireTokenAsync(this PublicClientApplication application, string[] scope, IPlatformParameters parameters, UserIdentifier userId)
        {
            return null;
        }

        public static async Task<AuthenticationResult> AcquireTokenAsync(this PublicClientApplication application, string[] scope, IPlatformParameters parameters, UserIdentifier userId,
            string extraQueryParameters)
        {
            return null;
        }

        public static async Task<AuthenticationResult> AcquireTokenAsync(this PublicClientApplication application, string[] scope, IPlatformParameters parameters, UserIdentifier userId,
            string extraQueryParameters, string[] additionalScope, string authority)
        {
            return null;
        }
    }
}
