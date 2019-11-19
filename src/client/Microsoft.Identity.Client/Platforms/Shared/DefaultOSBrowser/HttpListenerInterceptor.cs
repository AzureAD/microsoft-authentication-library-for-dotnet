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
        private ICoreLogger _logger;

        #region Test Hooks 
        public Action TestBeforeTopLevelCall { get; set; }
        public Action TestBeforeStart { get; set; }
        public Action TestBeforeGetContext { get; set; }
        #endregion

        public HttpListenerInterceptor(ICoreLogger logger)
        {
            _logger = logger;
        }

        public async Task<Uri> ListenToSingleRequestAndRespondAsync(
            int port,
            Func<Uri, MessageAndHttpCode> responseProducer,
            CancellationToken cancellationToken)
        {
            TestBeforeTopLevelCall?.Invoke();
            cancellationToken.ThrowIfCancellationRequested();

            HttpListener httpListener = null;
            try
            {
                string urlToListenTo = "http://localhost:" + port + "/";

                httpListener = new HttpListener();
                httpListener.Prefixes.Add(urlToListenTo);

                TestBeforeStart?.Invoke();

                httpListener.Start();
                _logger.Info("Listening for authorization code on " + urlToListenTo);

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
                    _logger.Verbose("HttpListner received a message on " + urlToListenTo);

                    // the request URL should now contain the auth code and pkce
                    return context.Request.Url;
                }
            }
            catch (ObjectDisposedException)
            {
                // If cancellation is requested before GetContextAsync is called
                // then an ObjectDisposedException is fired by GetContextAsync.
                // This is still just cancellation
                _logger.Warning("ObjectDisposedException - cancellation requested? " + cancellationToken.IsCancellationRequested);
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

        private void Respond(Func<Uri, MessageAndHttpCode> responseProducer, HttpListenerContext context)
        {
            MessageAndHttpCode messageAndCode = responseProducer(context.Request.Url);
            _logger.Info("Processing a response message to the browser. HttpStatus:" + messageAndCode.HttpCode);

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
