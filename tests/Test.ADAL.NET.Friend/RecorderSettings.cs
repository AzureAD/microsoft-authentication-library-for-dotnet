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
using System.IO;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.ADAL.NET.Friend
{
    public enum RecorderMode
    {
        Record,
        Replay
    }

    public static class RecorderSettings
    {
        public static RecorderMode Mode { get; set; }

        public static bool Mock { get; set; }

        public static string Path
        {
            get
            {
                return (Mode == RecorderMode.Record) ? Directory.GetCurrentDirectory() + @"\..\..\..\tests\Test.ADAL.Common\" : @".\";
            }            
        }

        public static void SetMockModeByTestContext(TestContext testContext)
        {
            try
            {
                Mock = bool.Parse((string)testContext.DataRow["Mock"]);
            }
            catch (ArgumentException)
            {
                Mock = false;
            }

            if (Mock)
            {
                SetRecorderNetworkPlugin();
            }
            else
            {
                ResetRecorderNetworkPlugin();
            }            
        }

        public static void WriteRecordersToFile()
        {
            RecorderBase.WriteToFile();
        }


        private static void SetRecorderNetworkPlugin()
        {
            PlatformPlugin.WebUIFactory = new RecorderWebUIFactory();
            PlatformPlugin.HttpClientFactory = new RecorderHttpClientFactory();
        }

        private static void ResetRecorderNetworkPlugin()
        {
            PlatformPlugin.InitializeByAssemblyDynamicLinking();
        }
    }
}
