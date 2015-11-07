using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MSAL;
using MSAL.Desktop;

namespace DesktopApp
{
    class Program
    {
        static void Main(string[] args)
        {
            PublicClientApplication app = new PublicClientApplication();
            AuthenticationResult result = app.AcquireTokenAsync(new[] {"scope1", "scope2"}, new PlatformParameters()).Result;
        }
    }
}
