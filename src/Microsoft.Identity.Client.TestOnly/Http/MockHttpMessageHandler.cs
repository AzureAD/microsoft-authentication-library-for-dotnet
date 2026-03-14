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

namespace Microsoft.Identity.Client.TestOnly.Http
{
    /// <summary>
    /// A test <see cref="HttpClientHandler"/> that validates outgoing HTTP requests against
    /// configured expectations and returns a pre-defined <see cref="HttpResponseMessage"/>.
    /// </summary>
    /// <remarks>
    /// Queue instances of this class onto a <see cref="MockHttpClientFactory"/> so that each
    /// outgoing MSAL request is matched against the next handler in the queue.
    ///
    /// Validation failures throw <see cref="MockHttpValidationException"/> rather than using
    /// a specific test framework, making this class usable from xUnit, NUnit, MSTest, and others.
    /// </remarks>
    public sealed class MockHttpMessageHandler : HttpClientHandler
    {
        /// <summary>Gets or sets the response to return for the matched request.</summary>
        public HttpResponseMessage ResponseMessage { get; set; }

        /// <summary>
        /// Gets or sets the expected base URL (path only; query string is ignored here
        /// and validated separately via <see cref="ExpectedQueryParams"/>).
        /// </summary>
        public string ExpectedUrl { get; set; }

        /// <summary>Gets or sets the expected query string parameters.</summary>
        public IDictionary<string, string> ExpectedQueryParams { get; set; }

        /// <summary>Gets or sets the expected POST body parameters.</summary>
        public IDictionary<string, string> ExpectedPostData { get; set; }

        /// <summary>Gets or sets headers that must be present on the request.</summary>
        public IDictionary<string, string> ExpectedRequestHeaders { get; set; }

        /// <summary>Gets or sets header names that must NOT be present on the request.</summary>
        public IList<string> UnexpectedRequestHeaders { get; set; }

        /// <summary>Gets or sets POST body keys that must NOT be present.</summary>
        public IDictionary<string, string> UnExpectedPostData { get; set; }

        /// <summary>Gets or sets query parameters that must NOT be present (or must not match).</summary>
        public IDictionary<string, string> NotExpectedQueryParams { get; set; }

        /// <summary>Gets or sets the expected HTTP method.</summary>
        public HttpMethod ExpectedMethod { get; set; }

        /// <summary>Gets or sets an exception to throw instead of returning a response.</summary>
        public Exception ExceptionToThrow { get; set; }

        /// <summary>
        /// Gets or sets a callback for additional custom request validation.
        /// Invoked after all built-in validation passes.
        /// </summary>
        public Action<HttpRequestMessage> AdditionalRequestValidation { get; set; }

        /// <summary>
        /// Gets the actual request message captured during <see cref="SendAsync"/>.
        /// Available after the handler has been invoked.
        /// </summary>
        public HttpRequestMessage ActualRequestMessage { get; private set; }

        /// <summary>
        /// Gets the parsed POST body key/value pairs captured during <see cref="SendAsync"/>.
        /// Available after the handler has been invoked.
        /// </summary>
        public Dictionary<string, string> ActualRequestPostData { get; private set; }

        /// <summary>
        /// Gets the request headers captured during <see cref="SendAsync"/>.
        /// Available after the handler has been invoked.
        /// </summary>
        public HttpRequestHeaders ActualRequestHeaders { get; private set; }

        /// <summary>Gets or sets header names that are expected to be present on the request.</summary>
        public IList<string> PresentRequestHeaders { get; set; }

        /// <summary>
        /// Gets or sets the mTLS client certificate expected to be bound to the outgoing request.
        /// When set, the handler asserts that exactly one client certificate is configured on the
        /// underlying <see cref="HttpClientHandler"/> and that it matches this value.
        /// </summary>
        public X509Certificate2 ExpectedMtlsBindingCertificate { get; set; }

        /// <inheritdoc />
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
                string actualBase = uri.AbsoluteUri.Split('?')[0];
                string expectedBase = ExpectedUrl.Split('?')[0];
                if (!string.Equals(expectedBase, actualBase, StringComparison.Ordinal))
                {
                    throw new MockHttpValidationException(
                        $"Expected URL '{expectedBase}' but got '{actualBase}'.");
                }
            }

            if (ExpectedMtlsBindingCertificate != null)
            {
                if (base.ClientCertificates.Count != 1)
                {
                    throw new MockHttpValidationException(
                        $"Expected exactly 1 client certificate for mTLS binding, but found {base.ClientCertificates.Count}.");
                }
                if (!ExpectedMtlsBindingCertificate.Equals(base.ClientCertificates[0]))
                {
                    throw new MockHttpValidationException(
                        "The mTLS binding certificate on the request does not match the expected certificate.");
                }
            }

            if (ExpectedMethod != null && !Equals(ExpectedMethod, request.Method))
            {
                throw new MockHttpValidationException(
                    $"Expected HTTP method '{ExpectedMethod}' but got '{request.Method}'.");
            }

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

            if (string.IsNullOrEmpty(uri.Query))
            {
                throw new MockHttpValidationException(
                    $"Provided url ({uri.AbsoluteUri}) does not contain query parameters as expected.");
            }

            Dictionary<string, string> inputQp = ParseQueryString(uri.Query.TrimStart('?'));

            if (inputQp.Count != ExpectedQueryParams.Count)
            {
                throw new MockHttpValidationException(
                    $"Different number of query params. Expected {ExpectedQueryParams.Count}, got {inputQp.Count}.");
            }

            foreach (var key in ExpectedQueryParams.Keys)
            {
                if (!inputQp.ContainsKey(key))
                {
                    throw new MockHttpValidationException(
                        $"Expected query parameter ({key}) not found in the url ({uri.AbsoluteUri}).");
                }

                string expected = ExpectedQueryParams[key];
                string actual = inputQp[key];

                // Scope values are space-separated and order-independent.
                if (string.Equals(key, "scope", StringComparison.OrdinalIgnoreCase))
                {
                    ValidateScopesEqual(expected, actual);
                }
                else if (!string.Equals(expected, actual, StringComparison.Ordinal))
                {
                    throw new MockHttpValidationException(
                        $"Value mismatch for query parameter: {key}. Expected '{expected}', got '{actual}'.");
                }
            }
        }

        private void ValidateNotExpectedQueryParams(Uri uri)
        {
            if (NotExpectedQueryParams == null || !NotExpectedQueryParams.Any())
            {
                return;
            }

            Dictionary<string, string> actualQueryParams = ParseQueryString(uri.Query.TrimStart('?'));
            var unexpectedKeysFound = new List<string>();

            foreach (var kvp in NotExpectedQueryParams)
            {
                if (actualQueryParams.TryGetValue(kvp.Key, out string value) &&
                    string.Equals(value, kvp.Value, StringComparison.OrdinalIgnoreCase))
                {
                    unexpectedKeysFound.Add(kvp.Key);
                }
            }

            if (unexpectedKeysFound.Count > 0)
            {
                throw new MockHttpValidationException(
                    $"Did not expect to find these query parameter keys/values: {string.Join(", ", unexpectedKeysFound)}");
            }
        }

        private async Task ValidatePostDataAsync(HttpRequestMessage request)
        {
            if (request.Method != HttpMethod.Get && request.Content != null)
            {
                string postData = await request.Content.ReadAsStringAsync().ConfigureAwait(false);
                ActualRequestPostData = ParseQueryString(postData, urlDecode: true);
            }

            if (ExpectedPostData != null)
            {
                foreach (string key in ExpectedPostData.Keys)
                {
                    if (ActualRequestPostData == null || !ActualRequestPostData.ContainsKey(key))
                    {
                        throw new MockHttpValidationException(
                            $"Expected POST parameter '{key}' was not found in the request body.");
                    }

                    string expected = ExpectedPostData[key];
                    string actual = ActualRequestPostData[key];

                    if (string.Equals(key, "scope", StringComparison.OrdinalIgnoreCase))
                    {
                        ValidateScopesEqual(expected, actual);
                    }
                    else if (!string.Equals(expected, actual, StringComparison.Ordinal))
                    {
                        throw new MockHttpValidationException(
                            $"Value mismatch for POST parameter '{key}'. Expected '{expected}', got '{actual}'.");
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

            var unexpectedKeysFound = new List<string>();
            foreach (var key in UnExpectedPostData.Keys)
            {
                if (ActualRequestPostData != null && ActualRequestPostData.ContainsKey(key))
                {
                    unexpectedKeysFound.Add(key);
                }
            }

            if (unexpectedKeysFound.Count > 0)
            {
                throw new MockHttpValidationException(
                    $"Did not expect to find post data keys: {string.Join(", ", unexpectedKeysFound)}");
            }
        }

        private void ValidateHeaders(HttpRequestMessage request)
        {
            if (PresentRequestHeaders != null)
            {
                foreach (var headerName in PresentRequestHeaders)
                {
                    if (!request.Headers.Contains(headerName))
                    {
                        throw new MockHttpValidationException(
                            $"Expected request header to be present: {headerName}.");
                    }
                }
            }

            ActualRequestHeaders = request.Headers;

            if (ExpectedRequestHeaders != null)
            {
                foreach (var kvp in ExpectedRequestHeaders)
                {
                    if (!request.Headers.Contains(kvp.Key))
                    {
                        throw new MockHttpValidationException(
                            $"Expected request header not found: {kvp.Key}.");
                    }

                    string headerValue = request.Headers.GetValues(kvp.Key).FirstOrDefault();
                    if (!string.Equals(kvp.Value, headerValue, StringComparison.Ordinal))
                    {
                        throw new MockHttpValidationException(
                            $"Value mismatch for request header {kvp.Key}. Expected '{kvp.Value}', got '{headerValue}'.");
                    }
                }
            }

            if (UnexpectedRequestHeaders != null)
            {
                foreach (var item in UnexpectedRequestHeaders)
                {
                    if (request.Headers.Contains(item))
                    {
                        throw new MockHttpValidationException(
                            $"Not expecting a request header with key={item} but it was found.");
                    }
                }
            }
        }

        /// <summary>
        /// Parses a URL-encoded key=value string (query string or POST body) into a dictionary.
        /// </summary>
        private static Dictionary<string, string> ParseQueryString(string input, bool urlDecode = false)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(input))
            {
                return result;
            }

            foreach (string pair in input.Split('&'))
            {
                int idx = pair.IndexOf('=');
                // idx < 0: no '=' found (malformed); idx == 0: empty key (intentionally skipped)
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

                // The dictionary already uses OrdinalIgnoreCase; no need to lowercase explicitly.
                key = key.Trim();
                value = value.Trim().Trim('"').Trim();

                result[key] = value;
            }

            return result;
        }

        /// <summary>
        /// Validates that two space-separated scope strings contain the same scopes
        /// (order-independent comparison).
        /// </summary>
        private static void ValidateScopesEqual(string expectedScopes, string actualScopes)
        {
            var expected = new HashSet<string>(
                expectedScopes.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries),
                StringComparer.OrdinalIgnoreCase);

            var actual = new HashSet<string>(
                actualScopes.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries),
                StringComparer.OrdinalIgnoreCase);

            if (expected.Count != actual.Count || !expected.IsSubsetOf(actual))
            {
                throw new MockHttpValidationException(
                    $"Scope mismatch. Expected '{expectedScopes}', got '{actualScopes}'.");
            }
        }
    }
}
