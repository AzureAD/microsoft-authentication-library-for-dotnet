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

namespace Microsoft.Identity.Lab.Api.Core.Mocks
{
    /// <summary>
    /// A test <see cref="System.Net.Http.HttpMessageHandler"/> that returns a pre-configured
    /// <see cref="System.Net.Http.HttpResponseMessage"/> and validates request properties
    /// (URL, method, query parameters, post data, headers) against expectations.
    /// Enqueue instances into <see cref="MockHttpManager"/> to drive MSAL unit tests.
    /// </summary>
    public class MockHttpMessageHandler : HttpClientHandler
    {
        /// <summary>
        /// response message to return when this handler is invoked. Tests should set this to the desired response before executing the HTTP request. This allows tests to simulate various server responses and conditions, enabling comprehensive testing of MSAL's HTTP handling logic.
        /// </summary>
        public HttpResponseMessage ResponseMessage { get; set; }
        /// <summary>
        /// expected URL to be called in the HTTP request. Tests can set this property to validate that the HTTP request is being made to the correct endpoint. The validation will ignore query parameters, allowing tests to focus on the base URL while still validating query parameters separately if needed.
        /// </summary>
        public string ExpectedUrl { get; set; }
        /// <summary>
        /// Gets or sets the collection of expected query parameters and their corresponding values.
        /// </summary>
        public IDictionary<string, string> ExpectedQueryParams { get; set; }
        /// <summary>
        /// Expected post data key-value pairs to be included in the HTTP request body. Tests can set this property to validate that the HTTP request contains the expected form data, which is particularly important for token acquisition requests where parameters like "client_id", "scope", and "grant_type" are critical. The validation will check that all expected keys are present and that their values match the expected values, with special handling for scope values to ensure they are compared as sets rather than raw strings.
        /// </summary>
        public IDictionary<string, string> ExpectedPostData { get; set; }
        /// <summary>
        /// Expected request headers to be included in the HTTP request. Tests can set this property to validate that the HTTP request contains the expected headers, which may include important information such as "Authorization", "Content-Type", or custom headers used for telemetry or correlation. The validation will check that all expected headers are present and that their values match the expected values.
        /// </summary>
        public IDictionary<string, string> ExpectedRequestHeaders { get; set; }
        /// <summary>
        /// Unexpected request headers that should not be present in the HTTP request. Tests can set this property to validate that certain headers are not included in the HTTP request, which can be important for ensuring that sensitive information is not sent or that certain conditions are met. The validation will check that none of the specified headers are present in the request.
        /// </summary>
        public IList<string> UnexpectedRequestHeaders { get; set; }
        /// <summary>
        /// Unexpected post data key-value pairs that should not be included in the HTTP request body. Tests can set this property to validate that certain form data parameters are not included in the HTTP request, which can be important for ensuring that incorrect or sensitive information is not sent. The validation will check that none of the specified keys are present in the post data of the request.
        /// </summary>
        public IDictionary<string, string> UnExpectedPostData { get; set; }
        /// <summary>
        /// Unexpected query parameters that should not be included in the HTTP request. Tests can set this property to validate that certain query parameters are not included in the HTTP request, which can be important for ensuring that incorrect or sensitive information is not sent. The validation will check that none of the specified query parameters are present in the request.
        /// </summary>  
        public IDictionary<string, string> NotExpectedQueryParams { get; set; }
        /// <summary>
        /// Expected HTTP method (GET, POST, etc.) of the request. Tests can set this property to validate that the HTTP request is using the correct method, which is important for ensuring that the request is being made in accordance with the expected API contract. The validation will check that the method of the incoming request matches the expected method.
        /// </summary>
        public HttpMethod ExpectedMethod { get; set; }

        /// <summary>
        /// Exception to throw when this handler is invoked. Tests can set this property to simulate error conditions such as network failures or timeouts. When set, the handler will throw the specified exception instead of returning a response, allowing tests to validate MSAL's error handling logic in scenarios where HTTP requests fail due to exceptions.
        /// </summary>
        public Exception ExceptionToThrow { get; set; }
        /// <summary>
        /// additional validation logic to be executed on the HTTP request message. Tests can set this property to provide a custom validation function that will be invoked with the HTTP request message when the handler is executed. This allows tests to perform complex or specific validations that are not covered by the other properties, such as validating the presence of certain headers, checking the structure of the request body, or asserting on other aspects of the request that are relevant to the test scenario.
        /// </summary>
        public Action<HttpRequestMessage> AdditionalRequestValidation { get; set; }

        /// <summary>
        /// Once the http message is executed, this property holds the request message
        /// </summary>
        public HttpRequestMessage ActualRequestMessage { get; private set; }
        /// <summary>
        /// a dictionary of key-value pairs representing the post data included in the HTTP request body. This property is populated when the handler processes a request with content, allowing tests to assert on the actual post data sent in the request. The keys and values in this dictionary correspond to the form data parameters included in the request body, which is particularly important for validating token acquisition requests where specific parameters are expected.
        /// </summary>
        public Dictionary<string, string> ActualRequestPostData { get; private set; }
        /// <summary>
        /// a collection of HTTP request headers. This property is populated when the handler processes a request, allowing tests to assert on the actual headers sent in the request. The headers in this collection correspond to the headers included in the request, which is particularly important for validating token acquisition requests where specific headers are expected.
        /// </summary>  
        public HttpRequestHeaders ActualRequestHeaders { get; private set; }
        /// <summary>
        /// provides a list of request header names that are expected to be present in the HTTP request. Tests can set this property to validate that certain headers are included in the HTTP request, without necessarily asserting on their values. This is useful for scenarios where the presence of a header is important, but the specific value may vary or is not relevant to the test. The validation will check that all specified header names are present in the request headers.
        /// </summary>
        public IList<string> PresentRequestHeaders { get; set; }
        /// <summary>
        /// expected client certificate to be included in the HTTP request for mutual TLS (mTLS) scenarios. Tests can set this property to validate that the HTTP request includes the expected client certificate, which is important for ensuring that mTLS authentication is being performed correctly. The validation will check that the request includes a client certificate and that it matches the expected certificate specified in this property.
        /// </summary>
        public X509Certificate2 ExpectedMtlsBindingCertificate { get; set; }

        /// <summary>
        /// SendAsync override that performs validation on the incoming HTTP request against the configured expectations and returns the pre-configured response or throws the specified exception. This method is the core of the mock handler's functionality, allowing it to validate that the HTTP request being made by MSAL matches the expectations set in the test and to simulate various responses and error conditions as needed for comprehensive testing of MSAL's HTTP handling logic.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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
                MockCoreAssert.AreEqual(
                    ExpectedUrl.Split('?')[0],
                    uri.AbsoluteUri.Split('?')[0]);
            }

            if (ExpectedMtlsBindingCertificate != null)
            {
                if (base.ClientCertificates.Count != 1)
                {
                    throw new InvalidOperationException($"Expected 1 client certificate but found {base.ClientCertificates.Count}.");
                }

                MockCoreAssert.AreEqual(ExpectedMtlsBindingCertificate, base.ClientCertificates[0]);
            }

            MockCoreAssert.AreEqual(ExpectedMethod, request.Method);

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
            if (ExpectedQueryParams != null && ExpectedQueryParams.Any())
            {
                MockCoreAssert.IsFalse(string.IsNullOrEmpty(uri.Query), $"Provided url ({uri.AbsoluteUri}) does not contain query parameters as expected.");
                Dictionary<string, string> inputQp = CoreHelpers.ParseKeyValueList(uri.Query.Substring(1), '&', false, null);
                MockCoreAssert.HasCount(ExpectedQueryParams.Count, inputQp, "Different number of query params.");
                foreach (var key in ExpectedQueryParams.Keys)
                {
                    MockCoreAssert.IsTrue(inputQp.ContainsKey(key), $"Expected query parameter ({key}) not found in the url ({uri.AbsoluteUri}).");
                    MockCoreAssert.AreEqual(ExpectedQueryParams[key], inputQp[key], $"Value mismatch for query parameter: {key}.");
                }
            }
        }

        private void ValidateNotExpectedQueryParams(Uri uri)
        {
            if (NotExpectedQueryParams != null && NotExpectedQueryParams.Any())
            {
                // Parse actual query params again (or reuse inputQp if you like)
                Dictionary<string, string> actualQueryParams = CoreHelpers.ParseKeyValueList(uri.Query.Substring(1), '&', false, null);
                List<string> unexpectedKeysFound = new List<string>();

                foreach (KeyValuePair<string, string> kvp in NotExpectedQueryParams)
                {
                    // Check if the request's query has this key
                    if (actualQueryParams.TryGetValue(kvp.Key, out string value))
                    {
                        // Optionally, also check if we care about matching the *value*:
                        if (string.Equals(value, kvp.Value, StringComparison.OrdinalIgnoreCase))
                        {
                            unexpectedKeysFound.Add(kvp.Key);
                        }
                    }
                }

                // Fail if any "not expected" key/value pairs were found
                MockCoreAssert.IsEmpty(
                    unexpectedKeysFound,
                    $"Did not expect to find these query parameter keys/values: {string.Join(", ", unexpectedKeysFound)}"
                );
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
                    MockCoreAssert.IsTrue(ActualRequestPostData.ContainsKey(key));
                    if (key.Equals(OAuth2Parameter.Scope, StringComparison.OrdinalIgnoreCase))
                    {
                        MockCoreAssert.AreScopesEqual(ExpectedPostData[key], ActualRequestPostData[key]);
                    }
                    else
                    {
                        MockCoreAssert.AreEqual(ExpectedPostData[key], ActualRequestPostData[key]);
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
                MockCoreAssert.IsEmpty(unexpectedKeysFound, $"Did not expect to find post data keys: {string.Join(", ", unexpectedKeysFound)}");
            }
        }

        private void ValidateHeaders(HttpRequestMessage request)
        {
            if (PresentRequestHeaders != null)
            {
                foreach (var headerName in PresentRequestHeaders)
                {
                    MockCoreAssert.IsTrue(request.Headers.Contains(headerName),
                        $"Expected request header to be present: {headerName}.");
                }
            }

            ActualRequestHeaders = request.Headers;
            if (ExpectedRequestHeaders != null)
            {
                foreach (var kvp in ExpectedRequestHeaders)
                {
                    MockCoreAssert.IsTrue(request.Headers.Contains(kvp.Key), $"Expected request header not found: {kvp.Key}.");
                    var headerValue = request.Headers.GetValues(kvp.Key).FirstOrDefault();
                    MockCoreAssert.AreEqual(kvp.Value, headerValue, $"Value mismatch for request header {kvp.Key}.");
                }
            }

            if (UnexpectedRequestHeaders != null)
            {
                foreach (var item in UnexpectedRequestHeaders)
                {
                    MockCoreAssert.IsFalse(request.Headers.Contains(item), $"Not expecting a request header with key={item} but it was found.");
                }
            }
        }
    }
}
