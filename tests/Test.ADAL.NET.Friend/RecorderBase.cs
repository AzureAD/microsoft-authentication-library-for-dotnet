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

using System.Collections.Generic;
using System.IO;
using Test.ADAL.Common;

namespace Test.ADAL.NET.Friend
{
    public class RecorderBase
    {
        protected static Dictionary<string, string> IOMap;

        private static string dictionaryFilePath;

        public static void Initialize()
        {
            if (IOMap == null)
            {
                dictionaryFilePath = RecorderSettings.Path + @"recorded_data.dat";
                IOMap = (RecorderSettings.Mode == RecorderMode.Replay && File.Exists(dictionaryFilePath))
                    ? SerializationHelper.DeserializeDictionary(dictionaryFilePath)
                    : new Dictionary<string, string>();
            }
        }

        public static void WriteToFile()
        {
            SerializationHelper.SerializeDictionary(IOMap, dictionaryFilePath);
        }
    }
}
