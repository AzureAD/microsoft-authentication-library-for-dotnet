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
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Common.Core.Mocks
{
    internal class MockHttpMessageHandler : HttpClientHandler
    {
        public HttpResponseMessage ResponseMessage { get; set; }
        public string ExpectedUrl { get; set; }
        public IDictionary<string, string> ExpectedQueryParams { get; set; }
        public IDictionary<string, string> ExpectedPostData { get; set; }
        public IDictionary<string, string> ExpectedRequestHeaders { get; set; }
        public IList<string> UnexpectedRequestHeaders { get; set; }
        public IDictionary<string, string> UnExpectedPostData { get; set; }
        public HttpMethod ExpectedMethod { get; set; }

        public Exception ExceptionToThrow { get; set; }
        public Action<HttpRequestMessage> AdditionalRequestValidation { get; set; }

        /// <summary>
        /// Once the http message is executed, this property holds the request message
        /// </summary>
        public HttpRequestMessage ActualRequestMessage { get; private set; }
        public Dictionary<string, string> ActualRequestPostData { get; private set; }
        public HttpRequestHeaders ActualRequestHeaders { get; private set; }
        public X509Certificate2 ExpectedMtlsBindingCertificate { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {

            ActualRequestMessage = request;

            if (ExceptionToThrow != null)
            {
                throw ExceptionToThrow;
            }

            var uri = request.RequestUri;

            if (!string.IsNullOrEmpty(ExpectedUrl))
            {
                Assert.AreEqual(
                    ExpectedUrl.Split('?')[0],
                    uri.AbsoluteUri.Split('?')[0]);
            }

            if (ExpectedMtlsBindingCertificate != null)
            {
                Assert.AreEqual(1, base.ClientCertificates.Count);
                Assert.AreEqual(ExpectedMtlsBindingCertificate, base.ClientCertificates[0]);
            }

            Assert.AreEqual(ExpectedMethod, request.Method);

            ValidateQueryParams(uri);

            await ValidatePostDataAsync(request).ConfigureAwait(false);

            ValidateNotExpectedPostData();

            ValidateHeaders(request);

            AdditionalRequestValidation?.Invoke(request);

            cancellationToken.ThrowIfCancellationRequested();

            return ResponseMessage;
        }

        private void ValidateQueryParams(Uri uri)
        {
            if (ExpectedQueryParams != null && ExpectedQueryParams.Any())
            {
                Assert.IsFalse(string.IsNullOrEmpty(uri.Query), $"Provided url ({uri.AbsoluteUri}) does not contain query parameters as expected.");
                var inputQp = CoreHelpers.ParseKeyValueList(uri.Query.Substring(1), '&', false, null);
                Assert.AreEqual(ExpectedQueryParams.Count, inputQp.Count, "Different number of query params.");
                foreach (var key in ExpectedQueryParams.Keys)
                {
                    Assert.IsTrue(inputQp.ContainsKey(key), $"Expected query parameter ({key}) not found in the url ({uri.AbsoluteUri}).");
                    Assert.AreEqual(ExpectedQueryParams[key], inputQp[key], $"Value mismatch for query parameter: {key}.");
                }
            }
        }

        private async Task ValidatePostDataAsync(HttpRequestMessage request)
        {
            if (request.Method != HttpMethod.Get && request.Content != null)
            {
                string postData = await request.Content.ReadAsStringAsync().ConfigureAwait(false);
                ActualRequestPostData = CoreHelpers.ParseKeyValueList(postData, '&', true, null);
            }

            if (ExpectedPostData != null)
            {
                foreach (string key in ExpectedPostData.Keys)
                {
                    Assert.IsTrue(ActualRequestPostData.ContainsKey(key));
                    if (key.Equals(OAuth2Parameter.Scope, StringComparison.OrdinalIgnoreCase))
                    {
                        CoreAssert.AreScopesEqual(ExpectedPostData[key], ActualRequestPostData[key]);
                    }
                    else
                    {
                        Assert.AreEqual(ExpectedPostData[key], ActualRequestPostData[key]);
                    }
                }
            }
        }

        private void ValidateNotExpectedPostData()
        {
            if (UnExpectedPostData != null)
            {
                List<string> unexpectedKeysFound = new List<string>();

                // Check each key in the unexpected post data dictionary
                foreach (var key in UnExpectedPostData.Keys)
                {
                    if (ActualRequestPostData.ContainsKey(key))
                    {
                        unexpectedKeysFound.Add(key);
                    }
                }

                // Assert that no unexpected keys were found, reporting all violations at once
                Assert.IsTrue(unexpectedKeysFound.Count == 0, $"Did not expect to find post data keys: {string.Join(", ", unexpectedKeysFound)}");
            }
        }

        private void ValidateHeaders(HttpRequestMessage request)
        {
            ActualRequestHeaders = request.Headers;
            if (ExpectedRequestHeaders != null)
            {
                foreach (var kvp in ExpectedRequestHeaders)
                {
                    // Skip validation for x-ms-client-request-id since it's random
                    if (kvp.Key.Equals("x-ms-client-request-id", StringComparison.OrdinalIgnoreCase))
                        continue;

                    Assert.IsTrue(request.Headers.Contains(kvp.Key), $"Expected request header not found: {kvp.Key}.");
                    var headerValue = request.Headers.GetValues(kvp.Key).FirstOrDefault();
                    Assert.AreEqual(kvp.Value, headerValue, $"Value mismatch for request header {kvp.Key}.");
                }
            }

            if (UnexpectedRequestHeaders != null)
            {
                foreach (var item in UnexpectedRequestHeaders)
                {
                    Assert.IsFalse(request.Headers.Contains(item), $"Not expecting a request header with key={item} but it was found.");
                }
            }
        }
    }
}
