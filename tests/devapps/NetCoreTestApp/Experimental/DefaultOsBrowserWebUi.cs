using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.UI;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace NetCoreTestApp.Experimental
{
    internal class DefaultOsBrowserWebUi : ICustomWebUi
    {
        // TODO: expose these as settings
        private const string CloseWindowSuccessHtml = @"<html>
  <head><title>Authentication Complete</title></head>
  <body>
    Authentication complete. You can return to the application. Feel free to close this browser tab.
  </body>
</html>";

        private const string CloseWindowFailureHtml = @"<html>
  <head><title>Authentication Failed</title></head>
  <body>
    Authentication failed. You can return to the application. Feel free to close this browser tab.
</br></br></br></br>
    Error details: error {0} error_description: {1}
  </body>
</html>";

        public async Task<Uri> AcquireAuthorizationCodeAsync(
            Uri authorizationUri,
            Uri redirectUri, 
            CancellationToken cancellationToken)
        {
            if (!redirectUri.IsLoopback)
            {
                throw new ArgumentException("Only loopback redirect uri is supported with this WebUI. Configure http://localhost or http://localhost:port during app registration. ");
            }

            // TODO: it isn't currently possible to change the redirectUri because later on
            // MSAL checks if it configured uri is identical to the one received.
            // This condition should be relaxed to take into account a different port
            // https://github.com:443/AzureAD/microsoft-authentication-library-for-dotnet/blob/dev3x/src/Microsoft.Identity.Client/UI/CustomWebUiHandler.cs#L60
            // InjectPortIfNeeded(ref authorizationUri, ref redirectUri);

            Uri result = await InterceptAuthorizationUriAsync(
                authorizationUri,
                redirectUri,
                cancellationToken)
                .ConfigureAwait(true);

            return result;
        }

        private static void InjectPortIfNeeded(ref Uri authorizationUri, ref Uri redirectUri)
        {
            // If no port is provided, find a free one. eSTS and MSAL allow any port. B2C does not currently allow http://localhost.
            if (redirectUri.IsDefaultPort)
            {
                redirectUri = new Uri(FindFreeLocalhostRedirectUri());

                // also need to update the authorizationUri because it contains the redirect uri
                var queryParams = HttpUtility.ParseQueryString(authorizationUri.Query);
                queryParams.Set("redirect_uri", redirectUri.ToString());

                var uriBuilder = new UriBuilder(authorizationUri);
                uriBuilder.Query = queryParams.ToString();
                authorizationUri = uriBuilder.Uri;
            }
        }

        public static string FindFreeLocalhostRedirectUri()
        {
            TcpListener listner = new TcpListener(IPAddress.Loopback, 0);
            try
            {
                listner.Start();
                int port = ((IPEndPoint)listner.LocalEndpoint).Port;
                return "http://localhost:" + port;
            }
            finally
            {
                listner?.Stop();
            }
        }

        private static void OpenBrowser(string url)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch
            {
                // hack because of this: https://github.com/dotnet/corefx/issues/10361
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
                else
                {
                    throw new PlatformNotSupportedException(RuntimeInformation.OSDescription);
                }
            }
        }

        private async Task<Uri> InterceptAuthorizationUriAsync(
            Uri authorizationUri,
            Uri redirectUri, 
            CancellationToken cancellationToken)
        {
            OpenBrowser(authorizationUri.ToString());
            using (var listener = new SingleMessageTcpListener(redirectUri.Port)) 
            {
                Uri authCodeUri = null;
                await listener.ListenToSingleRequestAndRespondAsync(
                    (uri) =>
                    {
                        Trace.WriteLine("Intercepted an auth code url: " + uri.ToString());
                        authCodeUri = uri;

                        return GetMessageToShowInBroswerAfterAuth(uri);
                    },
                    cancellationToken)
                .ConfigureAwait(false);

                return authCodeUri;
            }
        }

        private static string GetMessageToShowInBroswerAfterAuth(Uri uri)
        {
            // Parse the uri to understand if an error was returned. This is done just to show the user a nice error message in the browser.
            var authCodeQueryKeyValue = HttpUtility.ParseQueryString(uri.Query);

            string errorString = authCodeQueryKeyValue.Get("error");
            if (!string.IsNullOrEmpty(errorString))
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    CloseWindowFailureHtml,
                    errorString,
                    authCodeQueryKeyValue.Get("error_description"));
            }

            return CloseWindowSuccessHtml;
        }
    }
}
