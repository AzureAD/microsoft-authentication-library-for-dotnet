using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Desktop
{
    /// <summary>
    /// MSAL extensions for desktop apps
    /// </summary>
    public static class DesktopExtensions
    {
        /// <summary>
        /// Adds enhanced support for desktop applications, e.g. CLI, WinForms, WPF apps.
        /// 
        /// Support added is around: 
        /// 
        /// - Windows Authentication Manager (WAM) broker, the recommended authentication mechanism on Windows 10 - https://aka.ms/msal-net-wam
        /// - WebView2 embedded web view, based on Microsoft Edge - https://aka.ms/msal-net-webview2
        /// </summary>
        /// <remarks>These extensions live in a separate package to avoid adding dependencies to MSAL</remarks>
        public static PublicClientApplicationBuilder WithDesktopFeatures(this PublicClientApplicationBuilder builder)
        {
            WamExtension.AddSupportForWam(builder);
            AddSupportForWebView2(builder);

            return builder;
        }

        /// <summary>
        /// Enables Windows broker flows on older platforms, such as .NET framework, where these are not available in the box with Microsoft.Identity.Client
        /// For details about Windows broker, see https://aka.ms/msal-net-wam
        /// </summary>
        private static void AddSupportForWebView2(PublicClientApplicationBuilder builder)
        {
            builder.Config.WebUiFactoryCreator = () => new MsalDesktopWebUiFactory();
        }
    }
}
