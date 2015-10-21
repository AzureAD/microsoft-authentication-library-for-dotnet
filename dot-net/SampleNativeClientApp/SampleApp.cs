using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MSAL;

namespace SampleNativeClientApp
{
    public class SampleApp
    {
        public static void Main(string[] args)
        {
            PublicClientApplication app = new PublicClientApplication();
            AuthenticationResult result;
            try
            {
                result = app.AcquireTokenSilentAsync(new[] {"scope1", "scope2"}).Result;
            }
            catch (Exception exc)
            {
                //simply passing null for IPlatformParameters will not work, unless the null is typecasted to class type.
                //this is because we have a collision with UserIdentifier.
                result = app.AcquireTokenAsync(new[] {"scope1", "scope2"}, (IPlatformParameters)null).Result; //this is insane.

            }
        }



    }
}
