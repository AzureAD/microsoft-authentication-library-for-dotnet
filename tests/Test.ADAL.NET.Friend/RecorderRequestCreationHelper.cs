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
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Test.ADAL.NET.Friend
{
    class RecorderRequestCreationHelper : RecorderBase, IRequestCreationHelper
    {
        private readonly IRequestCreationHelper internalRequestCreationHelper;

        static RecorderRequestCreationHelper()
        {
            Initialize();
        }

        public RecorderRequestCreationHelper()
        {
            this.internalRequestCreationHelper = new RequestCreationHelper();
        }

        public void AddAdalIdParameters(IDictionary<string, string> parameters)
        {
            
        }

        public void AddClientMetricsParameters(IDictionary<string, string> parameters)
        {
            
        }

        public DateTime GetJsonWebTokenValidFrom()
        {
            const string JsonWebTokenValidFrom = "JsonWebTokenValidFrom";
            if (IOMap.ContainsKey(JsonWebTokenValidFrom))
            {
                return new DateTime(long.Parse(IOMap[JsonWebTokenValidFrom]));
            }

            DateTime result = this.internalRequestCreationHelper.GetJsonWebTokenValidFrom();

            IOMap[JsonWebTokenValidFrom] = result.Ticks.ToString();

            return result;
        }
    }
}
