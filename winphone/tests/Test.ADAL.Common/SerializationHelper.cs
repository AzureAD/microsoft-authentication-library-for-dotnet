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

using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

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
            try
            {
                using (StreamWriter sw = new StreamWriter(stream, Encoding.GetEncoding("iso-8859-1"), 10000, true))
                {
                    sw.Write(str);
                    sw.Flush();
                }

                stream.Seek(0, SeekOrigin.Begin);
            }
            catch (Exception e)
            {
                
            }
        }

        public static WebException DeserializeWebException(string str)
        {
            using (Stream stream = new MemoryStream())
            {
                StringToStream(EncodingHelper.Base64Decode(str), stream);
                stream.Seek(0, SeekOrigin.Begin);
                Dictionary<string, string> dictionary = DeserializeDictionary(stream);
                WebResponse replayerWebResponse = new ReplayerWebResponse(dictionary); 
                return new WebException("", null, WebExceptionStatus.UnknownError, replayerWebResponse);
            }
        }
    }

    class ReplayerWebResponse : WebResponse
    {
        private readonly string responseBody;

        private readonly WebHeaderCollection headers;

        public ReplayerWebResponse(Dictionary<string, string> dictionary)
        {
            this.responseBody = dictionary["Body"];
            this.StatusCode = (HttpStatusCode)Enum.Parse(typeof(HttpStatusCode), dictionary["StatusCode"]);
            this.headers = new WebHeaderCollection();
        }

        public HttpStatusCode StatusCode { get; private set; }

        public override long ContentLength
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override string ContentType
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override Uri ResponseUri
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override WebHeaderCollection Headers
        {
            get
            {
                return this.headers;
            }            
        }

        public override Stream GetResponseStream()
        {
            MemoryStream responseStream = new MemoryStream();
            SerializationHelper.StringToStream(responseBody, responseStream);
            responseStream.Position = 0;
            return responseStream;
        }
    }
}
