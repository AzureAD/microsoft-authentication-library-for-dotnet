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
using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Test.ADAL.Common
{
    internal partial class SerializationHelper
    {
        private static Dictionary<string, string> DeserializeDictionary(Stream stream)
        {
            BinaryReader reader = new BinaryReader(stream);
            int count = reader.ReadInt32();
            var dictionary = new Dictionary<string, string>(count);
            for (int n = 0; n < count; n++)
            {
                var key = reader.ReadString();
                var value = reader.ReadString();
                dictionary.Add(key, value);
            }

            return dictionary;
        }

        public static void StringToStream(string str, Stream stream)
        {
            using (StreamWriter sw = new StreamWriter(stream, Encoding.GetEncoding("iso-8859-1"), 10000, true))
            {
                sw.Write(str);
                sw.Flush();
            }

            stream.Seek(0, SeekOrigin.Begin);
        }

        public static HttpRequestWrapperException DeserializeException(string str)
        {
            using (Stream stream = new MemoryStream())
            {
                StringToStream(EncodingHelper.Base64Decode(str), stream);
                stream.Seek(0, SeekOrigin.Begin);
                Dictionary<string, string> dictionary = DeserializeDictionary(stream);
                Stream bodyStream = new MemoryStream();

                var headers = new Dictionary<string, string>();
                foreach (var key in dictionary.Keys)
                {
                    if (key.StartsWith("Header-"))
                    {
                        headers.Add(key.Substring(7), dictionary[key]);
                    }
                }

                StringToStream(dictionary["Body"], bodyStream);
                bodyStream.Position = 0;
                return new HttpRequestWrapperException(
                    new HttpWebResponseWrapper(bodyStream, headers, (HttpStatusCode)Enum.Parse(typeof(HttpStatusCode), dictionary["StatusCode"])), 
                    new HttpRequestException());
            }
        }
    }
}
