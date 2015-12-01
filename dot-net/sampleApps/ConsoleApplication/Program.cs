using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MSAL;

namespace ConsoleApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            PublicClientApplication app = new PublicClientApplication();

            AuthenticationResult result = null;
            try
            {
                result = app.AcquireTokenSilentAsync(new[] {""}).Result;
            }
            catch (Exception exc)
            {
                result = app.AcquireTokenAsync(new[] {""}).Result;
            }
        }
    }
}
