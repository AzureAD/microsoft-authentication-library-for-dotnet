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
    internal class HttpListnerInterceptor : IUriInterceptor
    {
        private ICoreLogger _logger;

        public HttpListnerInterceptor(ICoreLogger logger)
        {
            _logger = logger;
        }

        public async Task<Uri> ListenToSingleRequestAndRespondAsync(
            int port,
            Func<Uri, string> responseProducer,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            HttpListener httpListener = null;
            try
            {
                string urlToListenTo = "http://localhost:" + port + "/";

                httpListener = new HttpListener();
                httpListener.Prefixes.Add(urlToListenTo);
                httpListener.Start();
                Console.WriteLine("Listening to " + urlToListenTo);

                using (cancellationToken.Register(() =>
                {
                    Console.WriteLine("HttpListener stopped because cancellation was requested.");
                    TryStopListening(httpListener);
                }))
                {
                    HttpListenerContext context = await httpListener.GetContextAsync()
                        .ConfigureAwait(false);

                    cancellationToken.ThrowIfCancellationRequested();

                    Respond(responseProducer, context);
                    Console.WriteLine("HttpListner received a message on " + urlToListenTo);

                    // the request URL should now contain the auth code and pkce
                    return context.Request.Url;
                }
            }
            catch (ObjectDisposedException)
            {
                // If cancellation is requested before GetContextAsync is called
                // then an ObjectDisposedException is fired by GetContextAsync.
                // This is still just cancellation

                cancellationToken.ThrowIfCancellationRequested();

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

        private void Respond(Func<Uri, string> responseProducer, HttpListenerContext context)
        {
            string responseMessage = responseProducer(context.Request.Url);

            // TODO: handle redirects
            //response.StatusCode = 302;
            //response.RedirectLocation = @"https://www.google.com";
            ////// Construct a response.
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseMessage);
            context.Response.ContentLength64 = buffer.Length;
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.OutputStream.Close();
        }
    }
}
