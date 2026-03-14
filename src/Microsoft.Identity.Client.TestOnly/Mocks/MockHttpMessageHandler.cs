// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.TestOnly
{
    /// <summary>
    /// An <see cref="HttpClientHandler"/> that intercepts HTTP requests during tests.
    /// Configure the expected request parameters before using it in a <see cref="MockHttpManager"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Set <see cref="ExpectedUrl"/>, <see cref="ExpectedMethod"/>, and <see cref="ResponseMessage"/>
    /// at a minimum.  All configured expectations are validated when the handler processes a request;
    /// a violation throws <see cref="InvalidOperationException"/>.
    /// </para>
    /// <example>
    /// <code>
    /// httpManager.AddMockHandler(new MockHttpMessageHandler
    /// {
    ///     ExpectedUrl    = "https://management.azure.com/my-endpoint",
    ///     ExpectedMethod = HttpMethod.Get,
    ///     ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
    ///     {
    ///         Content = new StringContent("{\"access_token\":\"mock-token\"}")
    ///     }
    /// });
    /// </code>
    /// </example>
    /// </remarks>
    public class MockHttpMessageHandler : HttpClientHandler
    {
        /// <summary>Gets or sets the HTTP response that will be returned for a matching request.</summary>
        public HttpResponseMessage ResponseMessage { get; set; }

        /// <summary>Gets or sets the URL (without query string) that the request must match. Optional.</summary>
        public string ExpectedUrl { get; set; }

        /// <summary>Gets or sets the HTTP method that the request must use.</summary>
        public HttpMethod ExpectedMethod { get; set; }

        /// <summary>Gets or sets the expected query-string parameters. All listed keys and values must be present.</summary>
        public IDictionary<string, string> ExpectedQueryParams { get; set; }

        /// <summary>Gets or sets POST body parameters that must be present in the request body.</summary>
        public IDictionary<string, string> ExpectedPostData { get; set; }

        /// <summary>Gets or sets request headers that must be present with the specified values.</summary>
        public IDictionary<string, string> ExpectedRequestHeaders { get; set; }

        /// <summary>Gets or sets request header names that must <b>not</b> be present.</summary>
        public IList<string> UnexpectedRequestHeaders { get; set; }

        /// <summary>Gets or sets POST body keys that must <b>not</b> be present.</summary>
        public IDictionary<string, string> UnExpectedPostData { get; set; }

        /// <summary>Gets or sets query-string parameters that must <b>not</b> be present.</summary>
        public IDictionary<string, string> NotExpectedQueryParams { get; set; }

        /// <summary>Gets or sets an exception to throw instead of returning <see cref="ResponseMessage"/>.</summary>
        public Exception ExceptionToThrow { get; set; }

        /// <summary>Gets or sets a callback for additional custom validation of the request.</summary>
        public Action<HttpRequestMessage> AdditionalRequestValidation { get; set; }

        /// <summary>Gets or sets header names that must be present (value not checked).</summary>
        public IList<string> PresentRequestHeaders { get; set; }

        /// <summary>
        /// Gets or sets the mTLS binding certificate that must be attached to the request.
        /// When set, the handler verifies that exactly one client certificate equal to this value is present.
        /// </summary>
        public X509Certificate2 ExpectedMtlsBindingCertificate { get; set; }

        /// <summary>Gets the actual <see cref="HttpRequestMessage"/> after the handler fires.</summary>
        public HttpRequestMessage ActualRequestMessage { get; private set; }

        /// <summary>Gets the parsed POST body key-value pairs after the handler fires.</summary>
        public Dictionary<string, string> ActualRequestPostData { get; private set; }

        /// <summary>Gets the actual request headers after the handler fires.</summary>
        public HttpRequestHeaders ActualRequestHeaders { get; private set; }

        /// <inheritdoc/>
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            ActualRequestMessage = request;

            if (ExceptionToThrow != null)
            {
                throw ExceptionToThrow;
            }

            var uri = request.RequestUri;

            if (!string.IsNullOrEmpty(ExpectedUrl))
            {
                ThrowIfNotEqual(
                    ExpectedUrl.Split('?')[0],
                    uri.AbsoluteUri.Split('?')[0],
                    $"Expected URL '{ExpectedUrl}' but got '{uri.AbsoluteUri}'.");
            }

            if (ExpectedMtlsBindingCertificate != null)
            {
                ThrowIfFalse(
                    base.ClientCertificates.Count == 1,
                    $"Expected exactly 1 mTLS certificate but found {base.ClientCertificates.Count}.");
                ThrowIfFalse(
                    ExpectedMtlsBindingCertificate.Equals(base.ClientCertificates[0]),
                    "mTLS binding certificate does not match the expected certificate.");
            }

            ThrowIfNotEqual(
                ExpectedMethod,
                request.Method,
                $"Expected HTTP method {ExpectedMethod} but got {request.Method}.");

            ValidateExpectedQueryParams(uri);
            ValidateNotExpectedQueryParams(uri);
            await ValidatePostDataAsync(request).ConfigureAwait(false);
            ValidateNotExpectedPostData();
            ValidateHeaders(request);

            AdditionalRequestValidation?.Invoke(request);

            cancellationToken.ThrowIfCancellationRequested();

            return ResponseMessage;
        }

        private void ValidateExpectedQueryParams(Uri uri)
        {
            if (ExpectedQueryParams == null || !ExpectedQueryParams.Any())
            {
                return;
            }

            ThrowIfFalse(
                !string.IsNullOrEmpty(uri.Query),
                $"Request URL '{uri.AbsoluteUri}' has no query parameters but {ExpectedQueryParams.Count} were expected.");

            var inputQp = ParseKeyValueList(uri.Query.Substring(1), '&', false);

            ThrowIfFalse(
                inputQp.Count == ExpectedQueryParams.Count,
                $"Expected {ExpectedQueryParams.Count} query params but found {inputQp.Count} in '{uri.AbsoluteUri}'.");

            foreach (var key in ExpectedQueryParams.Keys)
            {
                ThrowIfFalse(inputQp.ContainsKey(key),
                    $"Expected query parameter '{key}' not found in '{uri.AbsoluteUri}'.");
                ThrowIfNotEqual(ExpectedQueryParams[key], inputQp[key],
                    $"Value mismatch for query parameter '{key}'.");
            }
        }

        private void ValidateNotExpectedQueryParams(Uri uri)
        {
            if (NotExpectedQueryParams == null || !NotExpectedQueryParams.Any())
            {
                return;
            }

            var actualQp = string.IsNullOrEmpty(uri.Query)
                ? new Dictionary<string, string>()
                : ParseKeyValueList(uri.Query.Substring(1), '&', false);

            var unexpectedFound = NotExpectedQueryParams
                .Where(kvp => actualQp.TryGetValue(kvp.Key, out var v) &&
                               string.Equals(v, kvp.Value, StringComparison.OrdinalIgnoreCase))
                .Select(kvp => kvp.Key)
                .ToList();

            ThrowIfFalse(unexpectedFound.Count == 0,
                $"Did not expect query parameter(s): {string.Join(", ", unexpectedFound)}");
        }

        private async Task ValidatePostDataAsync(HttpRequestMessage request)
        {
            if (request.Method != HttpMethod.Get && request.Content != null)
            {
                string postData = await request.Content.ReadAsStringAsync().ConfigureAwait(false);
                ActualRequestPostData = ParseKeyValueList(postData, '&', true);
            }

            if (ExpectedPostData != null)
            {
                foreach (string key in ExpectedPostData.Keys)
                {
                    ThrowIfFalse(
                        ActualRequestPostData != null && ActualRequestPostData.ContainsKey(key),
                        $"Expected POST data key '{key}' was not found.");

                    if (key.Equals("scope", StringComparison.OrdinalIgnoreCase))
                    {
                        var expectedScopes = new HashSet<string>(ExpectedPostData[key].Split(' '),
                            StringComparer.OrdinalIgnoreCase);
                        var actualScopes = new HashSet<string>(ActualRequestPostData[key].Split(' '),
                            StringComparer.OrdinalIgnoreCase);
                        ThrowIfFalse(
                            expectedScopes.SetEquals(actualScopes),
                            $"Scope mismatch. Expected: '{ExpectedPostData[key]}', got: '{ActualRequestPostData[key]}'.");
                    }
                    else
                    {
                        ThrowIfNotEqual(ExpectedPostData[key], ActualRequestPostData[key],
                            $"POST data value mismatch for key '{key}'.");
                    }
                }
            }
        }

        private void ValidateNotExpectedPostData()
        {
            if (UnExpectedPostData == null)
            {
                return;
            }

            var unexpectedFound = UnExpectedPostData.Keys
                .Where(k => ActualRequestPostData != null && ActualRequestPostData.ContainsKey(k))
                .ToList();

            ThrowIfFalse(unexpectedFound.Count == 0,
                $"Did not expect POST data key(s): {string.Join(", ", unexpectedFound)}");
        }

        private void ValidateHeaders(HttpRequestMessage request)
        {
            if (PresentRequestHeaders != null)
            {
                foreach (var headerName in PresentRequestHeaders)
                {
                    ThrowIfFalse(request.Headers.Contains(headerName),
                        $"Expected request header '{headerName}' to be present.");
                }
            }

            ActualRequestHeaders = request.Headers;

            if (ExpectedRequestHeaders != null)
            {
                foreach (var kvp in ExpectedRequestHeaders)
                {
                    ThrowIfFalse(request.Headers.Contains(kvp.Key),
                        $"Expected request header '{kvp.Key}' not found.");
                    var actual = request.Headers.GetValues(kvp.Key).FirstOrDefault();
                    ThrowIfNotEqual(kvp.Value, actual,
                        $"Value mismatch for request header '{kvp.Key}'.");
                }
            }

            if (UnexpectedRequestHeaders != null)
            {
                foreach (var item in UnexpectedRequestHeaders)
                {
                    ThrowIfFalse(!request.Headers.Contains(item),
                        $"Did not expect request header '{item}'.");
                }
            }
        }

        // ── Assertion helpers ─────────────────────────────────────────────

        private static void ThrowIfFalse(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void ThrowIfNotEqual<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException($"{message} Expected: '{expected}', Actual: '{actual}'.");
            }
        }

        // ── URL / body parsing ────────────────────────────────────────────

        internal static Dictionary<string, string> ParseKeyValueList(
            string input,
            char delimiter,
            bool urlDecode)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(input))
            {
                return result;
            }

            foreach (var pair in input.Split(delimiter))
            {
                int idx = pair.IndexOf('=');
                if (idx <= 0)
                {
                    continue;
                }

                string key = pair.Substring(0, idx);
                string value = pair.Substring(idx + 1);

                if (urlDecode)
                {
                    key = Uri.UnescapeDataString(key.Replace("+", "%20"));
                    value = Uri.UnescapeDataString(value.Replace("+", "%20"));
                }

                key = key.Trim().ToLowerInvariant();
                result[key] = value.Trim('"');
            }

            return result;
        }
    }
}
