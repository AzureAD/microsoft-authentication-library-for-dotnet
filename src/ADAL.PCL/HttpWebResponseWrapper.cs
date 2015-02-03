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
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal class HttpWebResponseWrapper : IHttpWebResponse
    {
        public HttpWebResponseWrapper(Stream responseStream, Dictionary<string, string> headers, HttpStatusCode statusCode)
        {
            this.ResponseStream = responseStream;
            this.Headers = headers;
            this.StatusCode = statusCode;
        }

        public HttpStatusCode StatusCode { get; private set; }

        public Dictionary<string, string> Headers { get; private set; }

        public Stream ResponseStream { get; private set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                // TODO: Address the missing Dispose method issue.
                if (this.ResponseStream != null)
                {
                    this.ResponseStream.Dispose();
                    this.ResponseStream = null;
                }
            }
        }
    }
}