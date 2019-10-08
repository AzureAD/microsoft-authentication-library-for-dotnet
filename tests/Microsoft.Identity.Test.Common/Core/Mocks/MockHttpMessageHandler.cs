// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Common.Core.Mocks
{
    internal class MockHttpMessageHandler : HttpMessageHandler
    {
        public HttpResponseMessage ResponseMessage { get; set; }
        public string ExpectedUrl { get; set; }
        public IDictionary<string, string> ExpectedQueryParams { get; set; }
        public IDictionary<string, string> ExpectedPostData { get; set; }
        public IDictionary<string, string> HttpTelemetryHeaders { get; set; }
        public HttpMethod ExpectedMethod { get; set; }
        public Exception ExceptionToThrow { get; set; }
        public Action<HttpRequestMessage> AdditionalRequestValidation { get; set; }

        /// <summary>
        /// Once the http message is executed, this property holds the request message
        /// </summary>
        public HttpRequestMessage ActualRequestMessage { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
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
                    ExpectedUrl,
                    uri.AbsoluteUri.Split(
                        new[]
                        {
                            '?'
                        })[0]);
            }

            Assert.AreEqual(ExpectedMethod, request.Method);

            // Match QP passed in for validation.
            if (ExpectedQueryParams != null)
            {
                Assert.IsFalse(
                    string.IsNullOrEmpty(uri.Query),
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "provided url ({0}) does not contain query parameters, as expected",
                        uri.AbsolutePath));
                IDictionary<string, string> inputQp = CoreHelpers.ParseKeyValueList(uri.Query.Substring(1), '&', false, null);
                Assert.AreEqual(ExpectedQueryParams.Count, inputQp.Count, "Different number of query params`");
                foreach (string key in ExpectedQueryParams.Keys)
                {
                    Assert.IsTrue(
                        inputQp.ContainsKey(key),
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "expected QP ({0}) not found in the url ({1})",
                            key,
                            uri.AbsolutePath));
                    Assert.AreEqual(ExpectedQueryParams[key], inputQp[key]);
                }
            }

            if (ExpectedPostData != null)
            {
                string postData = request.Content.ReadAsStringAsync().Result;
                Dictionary<string, string> requestPostDataPairs = CoreHelpers.ParseKeyValueList(postData, '&', true, null);

                foreach (string key in ExpectedPostData.Keys)
                {
                    Assert.IsTrue(requestPostDataPairs.ContainsKey(key));
                    if (key.Equals(OAuth2Parameter.Scope, StringComparison.OrdinalIgnoreCase))
                    {
                        CoreAssert.AreScopesEqual(ExpectedPostData[key], requestPostDataPairs[key]);
                    }
                    else
                    {
                        Assert.AreEqual(ExpectedPostData[key], requestPostDataPairs[key]);
                    }
                }
            }

            if (HttpTelemetryHeaders != null)
            {
                if (ActualRequestMessage.Headers.Contains(TelemetryConstants.XClientLastTelemetry))
                {
                    Assert.AreEqual(HttpTelemetryHeaders[TelemetryConstants.XClientLastTelemetry], ReturnValueFromRequestHeader(TelemetryConstants.XClientLastTelemetry));
                    Assert.AreEqual(HttpTelemetryHeaders[TelemetryConstants.XClientCurrentTelemetry], ReturnValueFromRequestHeader(TelemetryConstants.XClientCurrentTelemetry));
                }
            }

            AdditionalRequestValidation?.Invoke(request);

            return new TaskFactory().StartNew(() => ResponseMessage, cancellationToken);
        }

        private string ReturnValueFromRequestHeader(string telemRequest)
        {
            IEnumerable<string> telemRequestValue = ActualRequestMessage.Headers.GetValues(telemRequest);
            List<string> telemRequestValueAsList = telemRequestValue.ToList();
            string value = telemRequestValueAsList[0];
            return value;
        }
    }
}
