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
using System.Net;
using System.Text;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Test.ADAL.Common
{
    internal partial class SerializationHelper
    {
        public static void SerializeDictionary(Dictionary<string, string> dictionary, string dictionaryFilename)
        {
            using (var stream = File.Create(dictionaryFilename))
            {
                SerializeDictionary(dictionary, stream);
            }
        }

        public static Dictionary<string, string> DeserializeDictionary(string dictionaryFilename)
        {
            using (var stream = File.OpenRead(dictionaryFilename))
            {
                return DeserializeDictionary(stream);
            }
        }

        public static string StreamToString(Stream stream)
        {
            using (StreamReader sr = new StreamReader(stream, Encoding.GetEncoding("iso-8859-1"), false, 10000, true))
            {
                return sr.ReadToEnd();
            }
        }

        public static string SerializeException(HttpRequestWrapperException ex)
        {
            var dictionary = new Dictionary<string, string>();
            dictionary["StatusCode"] = ((int)(ex.WebResponse.StatusCode)).ToString();
            Stream responseStream = ex.WebResponse.ResponseStream;
            if (responseStream != null)
            {
                dictionary["Body"] = StreamToString(responseStream);
                responseStream.Position = 0;

                if (ex.WebResponse.Headers.Keys.Contains("WWW-Authenticate", StringComparer.OrdinalIgnoreCase))
                {
                    dictionary["WWW-AuthenticateHeader"] = ex.WebResponse.Headers["WWW-Authenticate"];
                }
            }
            else
            {
                dictionary["Body"] = string.Empty;
            }

            foreach (var headerKey in ex.WebResponse.Headers.Keys)
            {
                dictionary["Header-" + headerKey] = ex.WebResponse.Headers[headerKey];
            }

            using (Stream stream = new MemoryStream())
            {
                SerializeDictionary(dictionary, stream);
                stream.Seek(0, SeekOrigin.Begin);
                return EncodingHelper.Base64Encode(StreamToString(stream));
            }
        }

        private static void SerializeDictionary(Dictionary<string, string> dictionary, Stream stream)
        {
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write(dictionary.Count);
            foreach (var kvp in dictionary)
            {
                writer.Write(kvp.Key);
                writer.Write(kvp.Value);
            }

            writer.Flush();
        }
    }
}
