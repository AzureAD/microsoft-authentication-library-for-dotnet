using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.AuthScheme.PoP;

namespace Microsoft.Identity.Client.AppConfig
{
    /// <summary>
    /// 
    /// </summary>
    public class PopAuthenticationConfiguration
    {
        /// <summary>
        /// 
        /// </summary>
        public Uri RequestUri { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public HttpMethod PopHttpMethod { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public IPoPCryptoProvider PopCryptoProvider { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="requestUri"></param>
        public PopAuthenticationConfiguration(Uri requestUri)
        {
            if (requestUri == null || string.IsNullOrEmpty(requestUri.AbsoluteUri))
            {
                throw new ArgumentNullException(nameof(requestUri));
            }

            RequestUri = requestUri;
        }
    }
}
