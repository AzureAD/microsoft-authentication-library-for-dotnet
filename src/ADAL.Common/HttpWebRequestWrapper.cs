//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal class HttpWebRequestWrapper : IHttpWebRequest
    {
        private readonly HttpWebRequest request;

        private int timeoutInMilliSeconds = 30000;

        public HttpWebRequestWrapper(string uri)
        {
            this.request = (HttpWebRequest)WebRequest.Create(uri);
        }

        public RequestParameters BodyParameters { get; set; }

        public string Accept
        {
            set
            {
                this.request.Accept = value;
            }            
        }

        public string ContentType
        {
            set
            {
                this.request.ContentType = value;
            }
        }

        public string Method
        {
            set
            {
                this.request.Method = value;
            }
        }

        public bool UseDefaultCredentials
        {
            set
            {
                this.request.UseDefaultCredentials = value;
            }
        }

        public WebHeaderCollection Headers
        {
            get
            {
                return this.request.Headers;
            }
        }

        public int TimeoutInMilliSeconds
        {
            set
            {
                this.timeoutInMilliSeconds = value;
            }        
        }

        public async Task<IHttpWebResponse> GetResponseSyncOrAsync(CallState callState)
        {
            if (this.BodyParameters != null)
            {
                using (Stream stream = await GetRequestStreamSyncOrAsync(callState))
                {
                    this.BodyParameters.WriteToStream(stream);
                }
            }

#if ADAL_NET
            if (callState != null && callState.CallSync)
            {
                this.request.Timeout = this.timeoutInMilliSeconds;
                return NetworkPlugin.HttpWebRequestFactory.CreateResponse(this.request.GetResponse());
            }

            Task<WebResponse> getResponseTask = this.request.GetResponseAsync();
            System.Threading.ThreadPool.RegisterWaitForSingleObject(
                ((IAsyncResult)getResponseTask).AsyncWaitHandle, 
                delegate (object state, bool timedOut)
                    {
                        if (timedOut)
                        {
                            ((HttpWebRequest)state).Abort();
                        }
                    },
                this.request, 
                this.timeoutInMilliSeconds, 
                true);

            return NetworkPlugin.HttpWebRequestFactory.CreateResponse(await getResponseTask);
#else
            var timer = Windows.System.Threading.ThreadPoolTimer.CreateTimer(
                delegate
                    {
                        this.request.Abort();
                    }, 
                TimeSpan.FromMilliseconds(this.timeoutInMilliSeconds));

            try
            {
                return NetworkPlugin.HttpWebRequestFactory.CreateResponse(await this.request.GetResponseAsync());
            }
            finally
            {
                timer.Cancel();
            }
#endif
        }

        public async Task<Stream> GetRequestStreamSyncOrAsync(CallState callState)
        {
#if ADAL_NET
            if (callState != null && callState.CallSync)
            {
                return this.request.GetRequestStream();
            }
#endif
            return await this.request.GetRequestStreamAsync();
        }
    }
}