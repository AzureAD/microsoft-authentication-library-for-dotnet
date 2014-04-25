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
using System.Text;
using System.Threading.Tasks;

using Test.ADAL.Common;

namespace Test.ADAL.NET.Friend
{
    public class RecorderBase
    {
        protected static Dictionary<string, string> IOMap;

        private static string DictionaryFilePath;

        public static void Initialize()
        {
            if (IOMap == null)
            {
                DictionaryFilePath = RecorderSettings.Path + @"recorded_data.dat";
                IOMap = (RecorderSettings.Mode == RecorderMode.Replay && File.Exists(DictionaryFilePath))
                    ? SerializationHelper.DeserializeDictionary(DictionaryFilePath)
                    : new Dictionary<string, string>();
            }
        }

        public static void WriteToFile()
        {
            SerializationHelper.SerializeDictionary(IOMap, DictionaryFilePath);
        }
    }
}
