using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Interfaces;
using Microsoft.Identity.Client.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.MSAL.NET.Unit
{
    internal class MockWebUI : IWebUI 
    {
        internal AuthorizationResult MockResult { get; set; }

        internal IDictionary<string, string> HeadersToValidate { get; set; }

        public async Task<AuthorizationResult> AcquireAuthorizationAsync(Uri authorizationUri, Uri redirectUri, IDictionary<string, string> additionalHeaders, CallState callState)
        {
            if (HeadersToValidate!=null)
            {
                Assert.IsNotNull(additionalHeaders);

                foreach (var key in HeadersToValidate.Keys)
                {
                    Assert.IsTrue(additionalHeaders.ContainsKey(key));
                    Assert.AreEqual(HeadersToValidate[key], additionalHeaders[key]);
                }
            }

            return await Task.Factory.StartNew(() => this.MockResult).ConfigureAwait(false);
        }
    }
}
