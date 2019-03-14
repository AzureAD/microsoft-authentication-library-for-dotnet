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
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Identity.Test.LabInfrastructure.CloudInfrastructure
{
    public static class CloudConfigurationProvider
    {
        const string CloudConfig = "cloudConfig.json";
        const string DefaultCloudType = "DefaultCloud";
        static CloudConfiguration _cloudConfiguration;

        static CloudConfigurationProvider()
        {
            LoadConfiguration(DefaultCloudType);
        }

        public static void LoadConfiguration(string configuration)
        {
            var configBuilder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile(CloudConfig);
            var config = configBuilder.Build();
            _cloudConfiguration = new CloudConfiguration(config.GetSection(configuration));
        }

        public static CloudType CloudType
        {
            get
            {
                return _cloudConfiguration.CloudType;
            }
        }

        public static string Authority
        {
            get
            {
                return _cloudConfiguration.Authority;
            }
        }

        public static string Scopes
        {
            get
            {
                return _cloudConfiguration.Scopes;
            }
        }
    }
}
