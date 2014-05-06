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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

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
            NetworkPlugin.WebUIFactory = new RecorderWebUIFactory();
            NetworkPlugin.HttpWebRequestFactory = new RecorderHttpWebRequestFactory();
            NetworkPlugin.RequestCreationHelper = new RecorderRequestCreationHelper();
        }

        private static void ResetRecorderNetworkPlugin()
        {
            NetworkPlugin.SetToDefault();
        }
    }
}
