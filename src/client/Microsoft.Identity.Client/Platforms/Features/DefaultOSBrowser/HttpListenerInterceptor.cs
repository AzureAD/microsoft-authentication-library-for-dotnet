// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Platforms.Shared.Desktop.OsBrowser;

namespace Microsoft.Identity.Client.Platforms.Shared.DefaultOSBrowser
{
    internal class HttpListenerInterceptor : IUriInterceptor
    {
        private ILoggerAdapter _logger;

        #region Test Hooks 
        public Action TestBeforeTopLevelCall { get; set; }
        public Action<string> TestBeforeStart { get; set; }
        public Action TestBeforeGetContext { get; set; }
        #endregion

        public HttpListenerInterceptor(ILoggerAdapter logger)
        {
            _logger = logger;
        }

        public async Task<Uri> ListenToSingleRequestAndRespondAsync(
            int port,
            string path,
            Func<Uri, MessageAndHttpCode> responseProducer,
            CancellationToken cancellationToken)
        {
            TestBeforeTopLevelCall?.Invoke();
            cancellationToken.ThrowIfCancellationRequested();

            HttpListener httpListener = null;
            string urlToListenTo = string.Empty;
            try
            {
                if(string.IsNullOrEmpty(path))
                {
                    path = "/";
                }
                else
                {
                    path = (path.StartsWith("/") ? path : "/" + path);
                }

                urlToListenTo = "http://localhost:" + port + path;

                if (!urlToListenTo.EndsWith("/"))
                {
                    urlToListenTo += "/";
                }

                httpListener = new HttpListener();
                httpListener.Prefixes.Add(urlToListenTo);

                TestBeforeStart?.Invoke(urlToListenTo);

                httpListener.Start();
                _logger.Info(() => "Listening for authorization code on " + urlToListenTo);

                using (cancellationToken.Register(() =>
                {
                    _logger.Warning("HttpListener stopped because cancellation was requested.");
                    TryStopListening(httpListener);
                }))
                {
                    TestBeforeGetContext?.Invoke();
                    HttpListenerContext context = await httpListener.GetContextAsync()
                        .ConfigureAwait(false);

                    cancellationToken.ThrowIfCancellationRequested();

                    Respond(responseProducer, context);
                    _logger.Verbose(()=>"HttpListner received a message on " + urlToListenTo);

                    // the request URL should now contain the auth code and pkce
                    return context.Request.Url;
                }
            }
            // If cancellation is requested before GetContextAsync is called, then either 
            // an ObjectDisposedException or an HttpListenerException is thrown.
            // But this is just cancellation...
            catch (Exception ex) when (ex is HttpListenerException || ex is ObjectDisposedException)
            {
                _logger.Info(() => "HttpListenerException - cancellation requested? " + cancellationToken.IsCancellationRequested);
                cancellationToken.ThrowIfCancellationRequested();

                if (ex is HttpListenerException)
                {
                    throw new MsalClientException(MsalError.HttpListenerError, 
                        $"An HttpListenerException occurred while listening on {urlToListenTo} for the system browser to complete the login. " +
                        "Possible cause and mitigation: the app is unable to listen on the specified URL; " +
                        "run 'netsh http add iplisten 127.0.0.1' from the Admin command prompt.",
                        ex);
                }

                // if cancellation was not requested, propagate original ex
                throw;
            }
            finally
            {
                TryStopListening(httpListener);
            }
        }

        private static void TryStopListening(HttpListener httpListener)
        {
            try
            {
                httpListener?.Abort();
            }
            catch
            {
            }
        }

        private void Respond(Func<Uri, MessageAndHttpCode> responseProducer, HttpListenerContext context)
        {
            MessageAndHttpCode messageAndCode = responseProducer(context.Request.Url);
            _logger.Info(() => "Processing a response message to the browser. HttpStatus:" + messageAndCode.HttpCode);

            switch (messageAndCode.HttpCode)
            {
                case HttpStatusCode.Found:
                    context.Response.StatusCode = (int)HttpStatusCode.Found;
                    context.Response.RedirectLocation = messageAndCode.Message;
                    break;
                case HttpStatusCode.OK:
                    byte[] buffer = System.Text.Encoding.UTF8.GetBytes(messageAndCode.Message);
                    context.Response.ContentLength64 = buffer.Length;
                    context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                    break;
                default:
                    throw new NotImplementedException("HttpCode not supported" + messageAndCode.HttpCode);
            }

            context.Response.OutputStream.Close();
        }
    }
}
