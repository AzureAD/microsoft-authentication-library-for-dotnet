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
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Test.ADAL.Common;

namespace Test.ADAL.WinRT.Unit
{
    class ReplayerHttpWebResponse : IHttpWebResponse
    {
        public ReplayerHttpWebResponse(string responseString, HttpStatusCode statusCode)
        {
            this.ResponseStream = new MemoryStream();
            SerializationHelper.StringToStream(responseString, this.ResponseStream);
            this.ResponseStream.Position = 0;
            this.StatusCode = statusCode;
            this.Headers = new Dictionary<string, string>();
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
                if (this.ResponseStream != null)
                {
                    ((IDisposable)this.ResponseStream).Dispose();
                    this.ResponseStream = null;
                }
            }
        }
    }
}
