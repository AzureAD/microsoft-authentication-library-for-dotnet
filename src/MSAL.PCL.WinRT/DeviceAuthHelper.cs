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
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Certificates;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;
using Microsoft.Identity.Client.Interfaces;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client
{
    internal class DeviceAuthHelper : IDeviceAuthHelper
    {
        public bool CanHandleDeviceAuthChallenge
        {
            get { return false; }
        }

        public async Task<string> CreateDeviceAuthChallengeResponse(IDictionary<string, string> challengeData)
        {
            throw new NotImplementedException();
        }
    }
}
