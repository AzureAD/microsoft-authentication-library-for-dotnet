//----------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System;
using System.Reflection;
using System.Threading.Tasks;
using Foundation;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal class PlatformInformation : PlatformInformationBase
    {
        public override string GetProductName()
        {
            return "PCL.Mac";
        }

        public override string GetEnvironmentVariable(string variable)
        {
            return Environment.GetEnvironmentVariable(variable);
        }

        public override Task<string> GetUserPrincipalNameAsync()
        {
            return Task.FromResult (Environment.UserName);
        }

        public override string GetProcessorArchitecture()
        {
            return Environment.Is64BitProcess ? "x64" : "x86";
        }

        public override string GetOperatingSystem()
        {
            return NSProcessInfo.ProcessInfo.OperatingSystemVersionString;
        }

        public override string GetDeviceModel()
        {
            return null;
        }

        public override string GetAssemblyFileVersionAttribute()
        {
            // TODO: Check if assembly file version can be read in Mac assembly as well or not. For now, we use assembly version instead.
            return typeof(AdalIdHelper).GetTypeInfo().Assembly.GetName().Version.ToString();
        }
    }
}
