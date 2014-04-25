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
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Test.ADAL.Common;

namespace Test.ADAL.WinRT.Unit
{
    class ReplayerHttpWebResponse : IHttpWebResponse
    {
        private Stream responseStream;

        private readonly HttpStatusCode statusCode;

        public ReplayerHttpWebResponse(WebResponse response)
        {
            this.responseStream = response.GetResponseStream();
            var httpWebResponse = response as HttpWebResponse;
            this.statusCode = (httpWebResponse != null) ? httpWebResponse.StatusCode : HttpStatusCode.NotImplemented;
        }

        public ReplayerHttpWebResponse(string responseString, HttpStatusCode statusCode)
        {
            this.responseStream = new MemoryStream();
            SerializationHelper.StringToStream(responseString, responseStream);
            responseStream.Position = 0;
            this.statusCode = statusCode;
        }

        public HttpStatusCode StatusCode
        {
            get
            {
                return this.statusCode;
            }
        }

        public WebHeaderCollection Headers
        {
            get
            {
                return new WebHeaderCollection();
            }
        }

        public Stream GetResponseStream()
        {
            return this.responseStream;
        }

        public void Close()
        {
            this.responseStream = null;
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
                if (this.responseStream != null)
                {
                    ((IDisposable)this.responseStream).Dispose();
                    this.responseStream = null;
                }
            }
        }
    }
}
