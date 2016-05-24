using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.ADAL.NET.Unit.Mocks
{
    internal class MockHttpMessageHandler : HttpMessageHandler
    {
        public HttpResponseMessage ResponseMessage { get; set; }

        public string Url { get; set; }

        public IDictionary<string, string> QueryParams { get; set; }

        public IDictionary<string, string> PostData { get; set; }

        public HttpMethod Method { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.AreEqual(Method, request.Method);

            Uri uri = request.RequestUri;
            if (!string.IsNullOrEmpty(Url))
            {
                Assert.AreEqual(Url, uri.AbsoluteUri.Split(new[] { '?' })[0]);
            }
            
            //match QP passed in for validation. 
            if (QueryParams != null)
            {
                Assert.IsFalse(string.IsNullOrEmpty(uri.Query));
                IDictionary<string, string> inputQp = EncodingHelper.ParseKeyValueList(uri.Query.Substring(1), '&', true, null);
                foreach (var key in QueryParams.Keys)
                {
                    Assert.IsTrue(inputQp.ContainsKey(key));
                    Assert.AreEqual(QueryParams[key], inputQp[key]);
                }
            }

            if (PostData != null)
            {
                string text = request.Content.ReadAsStringAsync().Result;
                Dictionary<string, string> postDataInput = EncodingHelper.ParseKeyValueList(text, '&', true, null);

                foreach (string key in PostData.Keys)
                {
                    Assert.IsTrue(postDataInput.ContainsKey(key));
                    Assert.AreEqual(PostData[key], postDataInput[key]);
                }
            }
            
            return new TaskFactory().StartNew(() => ResponseMessage, cancellationToken);
        }
    }
}
