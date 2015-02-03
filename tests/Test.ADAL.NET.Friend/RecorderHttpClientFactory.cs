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
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.IdentityModel.Clients.ActiveDirectory;

using Test.ADAL.Common;

namespace Test.ADAL.NET.Friend
{
    public static class RecorderJwtId
    {
        public static int JwtIdIndex { get; set; }
    }

    class RecorderHttpClientFactory :  RecorderBase, IHttpClientFactory
    {
        public RecorderHttpClientFactory()
        {
            Initialize();
        }

        public IHttpClient Create(string uri, CallState callState)
        {
            return new RecorderHttpClient(uri, callState);
        }

        public bool AddAdditionalHeaders
        {
            get { return false; }
        }

        public DateTime GetJsonWebTokenValidFrom()
        {
            const string JsonWebTokenValidFrom = "JsonWebTokenValidFrom";
            if (IOMap.ContainsKey(JsonWebTokenValidFrom))
            {
                return new DateTime(long.Parse(IOMap[JsonWebTokenValidFrom]));
            }

            DateTime result = DateTime.UtcNow;

            IOMap[JsonWebTokenValidFrom] = result.Ticks.ToString();

            return result;
        }

        public string GetJsonWebTokenId()
        {
            const string JsonWebTokenIdPrefix = "JsonWebTokenId";
            string jsonWebTokenId = JsonWebTokenIdPrefix + RecorderJwtId.JwtIdIndex;
            if (IOMap.ContainsKey(jsonWebTokenId))
            {
                return IOMap[jsonWebTokenId];
            }

            string id = Guid.NewGuid().ToString();

            IOMap[jsonWebTokenId] = id;

            return id;
        }
    }
}
