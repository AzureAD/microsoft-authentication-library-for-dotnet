using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.ADAL.NET.Unit
{
    internal class MockHttpMessageHandler : HttpMessageHandler
    {
        public HttpResponseMessage ResponseMessage { get; set; }

        public string Url { get; set; }

        public IDictionary<string,string> QueryParams { get; set; }

        public HttpMethod Method { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.AreEqual(Method, request.Method);

            Uri uri = request.RequestUri;
            if (!string.IsNullOrEmpty(Url))
            {
                Assert.AreEqual(Url, uri.Authority);
            }

            if (QueryParams != null)
            {
                Assert.IsNotNull(uri.Query);
                IDictionary<string, string> inputQp = EncodingHelper.ParseKeyValueList(uri.Query, '&', true, null);
                Assert.AreEqual(QueryParams.Count, inputQp.Count);
                foreach (var key in QueryParams.Keys)
                {
                    Assert.IsTrue(inputQp.ContainsKey(key));
                    Assert.AreEqual(QueryParams[key], inputQp[key]);
                }
            }
            
        
            return new TaskFactory().StartNew(()=> ResponseMessage, cancellationToken);
        }
    }
}
