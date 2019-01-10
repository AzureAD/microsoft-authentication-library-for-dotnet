using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.UI;
using OpenQA.Selenium;
using System;
using System.Net;
using System.Net.Sockets;

namespace Microsoft.Identity.Test.Integration.Infrastructure
{
    internal class SeleniumWebUIFactory : IWebUIFactory
    {
        private readonly Action<IWebDriver> _automationLogic;
        private readonly TimeSpan _timeout;


        public SeleniumWebUIFactory(Action<IWebDriver> automationLogic, TimeSpan timeout)
        {
            _automationLogic = automationLogic;
            _timeout = timeout;
        }

        public IWebUI CreateAuthenticationDialog(CoreUIParent coreUIParent, RequestContext requestContext)
        {
            return new SeleniumWebUI(_automationLogic, _timeout);
        }

        /// <summary>
        /// Helper method that returns an uri of the form http://localhost:12345
        /// where the port is not in use. 
        /// </summary>
        /// <remarks>If you configure an AAD Application return uri as "http://localhost" and it will accept 
        /// redirect uris from any localhost + port</remarks>
        public static string FindFreeLocalhostRedirectUri()
        {
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return "http://localhost:" + port;
        }
    }

}
