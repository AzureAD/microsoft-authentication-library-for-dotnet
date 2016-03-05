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

            //externally available
            string identifier = args[0];
            IEnumerable<User> adalUsers = app.GetUsers(identifier);

            try
            {
                if (adalUsers.Any())
                {
                    result = app.AcquireTokenSilentAsync(new[] {""}, adalUsers.First()).Result;
                }
            }
            catch (Exception exc)
            {
                //log error
            }
            finally
            {
                if (result == null)
                {
                    result = app.AcquireTokenAsync(new[] {""}).Result;
                }
            }
        }
    }
}
