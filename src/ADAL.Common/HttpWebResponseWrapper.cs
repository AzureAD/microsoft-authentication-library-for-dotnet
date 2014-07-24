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

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal class HttpWebResponseWrapper : IHttpWebResponse
    {
        private WebResponse response;

        public HttpWebResponseWrapper(WebResponse response)
        {
            this.response = response;
        }

        public HttpStatusCode StatusCode
        {
            get
            {
                var httpWebResponse = this.response as HttpWebResponse;
                return (httpWebResponse != null) ? httpWebResponse.StatusCode : HttpStatusCode.NotImplemented;
            }
        }

        public WebHeaderCollection Headers
        {
            get
            {
                return this.response.Headers;
            }
        }

        public Stream GetResponseStream()
        {
            return this.response.GetResponseStream();
        }

        public void Close()
        {
            PlatformSpecificHelper.CloseHttpWebResponse(this.response);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (response != null)
                {
                    ((IDisposable)response).Dispose();
                    response = null;
                }
            }
        }
    }
}