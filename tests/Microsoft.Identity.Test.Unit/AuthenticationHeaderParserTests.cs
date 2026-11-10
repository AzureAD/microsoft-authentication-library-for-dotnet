// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if NET_CORE
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit
{
    [TestClass]
    public class AuthenticationHeaderParserTests
    {
        [TestMethod]
        [DataRow("parser")]
        [DataRow("multiScheme")]
        [DataRow("singleScheme")]
        [DataRow("legacy")]
        public async Task DefaultResourceProbesRemainAnonymousAsync(string api)
        {
            foreach (string challenge in new[] { "NTLM", "Negotiate" })
            {
                for (int redirectMode = 0; redirectMode <= 2; redirectMode++)
                {
                    await AssertProbeAsync(api, challenge, redirectMode, httpClient: null, expectAuthorization: false)
                        .ConfigureAwait(false);
                }
            }
        }

        [TestMethod]
        [DataRow("NTLM")]
        [DataRow("Negotiate")]
        public async Task ExplicitAnonymousClientRemainsAnonymousAsync(string challenge)
        {
            // Arrange
            using var client = new HttpClient(new HttpClientHandler { UseDefaultCredentials = false });

            // Act / Assert
            foreach (string api in new[] { "parser", "multiScheme", "singleScheme", "legacy" })
            {
                await AssertProbeAsync(api, challenge, redirectMode: 2, client, expectAuthorization: false)
                    .ConfigureAwait(false);
            }
        }

        [TestMethod]
        [DataRow("parser")]
        [DataRow("multiScheme")]
        [DataRow("singleScheme")]
        [DataRow("legacy")]
        public async Task CustomClientRetainsExplicitAuthenticationAsync(string api)
        {
            // Arrange
            using var handler = new HttpClientHandler { UseDefaultCredentials = true };
            using var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Custom");

            // Act / Assert
            await AssertProbeAsync(api, "Custom", redirectMode: 0, client, expectAuthorization: true)
                .ConfigureAwait(false);
            Assert.IsTrue(handler.UseDefaultCredentials);
        }

        private static async Task AssertProbeAsync(
            string api,
            string challenge,
            int redirectMode,
            HttpClient httpClient,
            bool expectAuthorization)
        {
            // Arrange
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            using var redirectTarget = new TcpListener(IPAddress.Loopback, 0);
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            listener.Start();
            redirectTarget.Start();
            string origin = $"http://127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}";
            string target = $"http://127.0.0.1:{((IPEndPoint)redirectTarget.LocalEndpoint).Port}";
            string redirectUri = (redirectMode == 2 ? target : origin) + "/challenge";
            Task<List<bool>> requests = ServeRequestsAsync(listener, challenge, redirectUri, cancellation.Token);
            Task<List<bool>> targetRequests = ServeRequestsAsync(redirectTarget, challenge, null, cancellation.Token);

            try
            {
                // Act
                string[] schemes = await ProbeAsync(
                    api,
                    origin + (redirectMode == 0 ? "/challenge" : "/redirect"),
                    httpClient,
                    challenge,
                    cancellation.Token).ConfigureAwait(false);

                // Assert
                CollectionAssert.AreEqual(new[] { api == "legacy" ? string.Empty : challenge }, schemes);
            }
            finally
            {
                cancellation.Cancel();
                await Task.WhenAll(requests, targetRequests).ConfigureAwait(false);
            }

            // Assert
            List<bool> observedRequests = await requests.ConfigureAwait(false);
            List<bool> observedTargetRequests = await targetRequests.ConfigureAwait(false);
            Assert.HasCount(redirectMode == 1 ? 2 : 1, observedRequests);
            Assert.HasCount(redirectMode == 2 ? 1 : 0, observedTargetRequests);
            Assert.IsTrue(observedRequests.Concat(observedTargetRequests)
                .All(hasAuthorization => hasAuthorization == expectAuthorization),
                "Resource probes must not add authentication unless the caller supplies it explicitly.");
        }

        private static async Task<List<bool>> ServeRequestsAsync(
            TcpListener listener,
            string challenge,
            string redirectUri,
            CancellationToken cancellationToken)
        {
            var requests = new List<bool>();
            try
            {
                while (true)
                {
                    using TcpClient connection = await listener.AcceptTcpClientAsync(cancellationToken)
                        .ConfigureAwait(false);
                    using NetworkStream stream = connection.GetStream();
                    using var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, leaveOpen: true);
                    string requestLine = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                    bool hasAuthorization = false;
                    string line;
                    while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)))
                    {
                        hasAuthorization |= line.StartsWith("Authorization:", StringComparison.OrdinalIgnoreCase);
                    }

                    requests.Add(hasAuthorization);
                    bool redirect = requestLine.Split(' ')[1] == "/redirect";
                    string response = redirect
                        ? $"HTTP/1.1 302 Found\r\nLocation: {redirectUri}\r\n"
                        : $"HTTP/1.1 401 Unauthorized\r\nWWW-Authenticate: {challenge}\r\n";
                    byte[] bytes = Encoding.ASCII.GetBytes(response + "Content-Length: 0\r\nConnection: close\r\n\r\n");
                    await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return requests;
            }
        }

        private static async Task<string[]> ProbeAsync(
            string api,
            string resourceUri,
            HttpClient httpClient,
            string scheme,
            CancellationToken cancellationToken)
        {
            switch (api)
            {
                case "parser":
                    AuthenticationHeaderParser parser = httpClient is null
                        ? await AuthenticationHeaderParser.ParseAuthenticationHeadersAsync(resourceUri, cancellationToken)
                            .ConfigureAwait(false)
                        : await AuthenticationHeaderParser.ParseAuthenticationHeadersAsync(
                            resourceUri, httpClient, cancellationToken).ConfigureAwait(false);
                    return parser.WwwAuthenticateParameters.Select(parameters => parameters.AuthenticationScheme).ToArray();
                case "multiScheme":
                    IReadOnlyList<WwwAuthenticateParameters> parameters = httpClient is null
                        ? await WwwAuthenticateParameters.CreateFromAuthenticationResponseAsync(
                            resourceUri, cancellationToken).ConfigureAwait(false)
                        : await WwwAuthenticateParameters.CreateFromAuthenticationResponseAsync(
                            resourceUri, httpClient, cancellationToken).ConfigureAwait(false);
                    return parameters.Select(parameter => parameter.AuthenticationScheme).ToArray();
                case "singleScheme":
                    WwwAuthenticateParameters single = httpClient is null
                        ? await WwwAuthenticateParameters.CreateFromAuthenticationResponseAsync(
                            resourceUri, scheme, cancellationToken).ConfigureAwait(false)
                        : await WwwAuthenticateParameters.CreateFromAuthenticationResponseAsync(
                            resourceUri, scheme, httpClient, cancellationToken).ConfigureAwait(false);
                    return new[] { single.AuthenticationScheme };
                case "legacy":
#pragma warning disable CS0618 // Verify the obsolete probe uses the same anonymous defaults.
                    WwwAuthenticateParameters legacy = httpClient is null
                        ? await WwwAuthenticateParameters.CreateFromResourceResponseAsync(resourceUri, cancellationToken)
                            .ConfigureAwait(false)
                        : await WwwAuthenticateParameters.CreateFromResourceResponseAsync(
                            httpClient, resourceUri, cancellationToken).ConfigureAwait(false);
#pragma warning restore CS0618
                    return new[] { legacy.AuthenticationScheme };
                default:
                    throw new ArgumentOutOfRangeException(nameof(api));
            }
        }
    }
}
#endif
