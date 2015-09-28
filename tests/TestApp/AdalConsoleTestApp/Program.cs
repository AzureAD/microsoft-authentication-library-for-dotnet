using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace AdalConsoleTestApp
{
    public class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Program instance = new Program();
            instance.RunApp();
        }

        private async void RunApp()
        {
            AuthenticationContext context = new AuthenticationContext("https://login.windows.net/common");
            DeviceCodeResult codeResult = await context.AcquireDeviceCodeAsync("https://graph.windows.net", "04b07795-8ddb-461a-bbee-02f9e1bf7b46");
            Console.WriteLine(codeResult.Message);
        }
    }
}
