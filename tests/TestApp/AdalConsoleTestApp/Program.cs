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
using System.Linq;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace AdalConsoleTestApp
{
    public class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Program instance = new Program();
            instance.RunApp();
        }

        private async void RunApp()
        {
            AuthenticationContext context = new AuthenticationContext("https://login.windows.net/common");
            DeviceCodeResult codeResult = await context.AcquireDeviceCodeAsync("https://graph.windows.net", "04b07795-8ddb-461a-bbee-02f9e1bf7b46");
            Console.WriteLine(codeResult.Message);
        }
    }
}
