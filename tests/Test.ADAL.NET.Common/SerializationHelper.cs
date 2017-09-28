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
                    if (key.StartsWith("Header-", StringComparison.OrdinalIgnoreCase))
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
