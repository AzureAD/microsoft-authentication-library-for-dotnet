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
using System.Linq;
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
