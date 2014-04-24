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
using Microsoft.Owin.Hosting;
using Owin;

namespace Test.ADAL.NET
{
    internal static class SerializationHelper
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
            using (StreamReader sr = new StreamReader(stream, Encoding.Default, false, 10000, true))
            {
                return sr.ReadToEnd();
            }
        }

        public static void StringToStream(string str, Stream stream)
        {
            using (StreamWriter sw = new StreamWriter(stream, Encoding.Default, 10000, true))
            {
                sw.Write(str);
                sw.Flush(); 
            }

            stream.Seek(0, SeekOrigin.Begin);
        }

        public static string SerializeWebException(WebException ex)
        {
            var dictionary = new Dictionary<string, string>();
            dictionary["StatusCode"] = ((int)((HttpWebResponse)(ex.Response)).StatusCode).ToString();
            Stream responseStream = ex.Response.GetResponseStream();
            if (responseStream != null)
            {
                dictionary["Body"] = StreamToString(responseStream);
                responseStream.Position = 0;

                if (ex.Response.Headers.AllKeys.Contains("WWW-Authenticate", StringComparer.OrdinalIgnoreCase))
                {
                    dictionary["WWW-AuthenticateHeader"] = ex.Response.Headers["WWW-Authenticate"];
                }
            }
            else
            {
                dictionary["Body"] = string.Empty;
            }

            using (Stream stream = new MemoryStream())
            {
                SerializeDictionary(dictionary, stream);
                stream.Seek(0, SeekOrigin.Begin);
                return EncodingHelper.Base64Encode(StreamToString(stream));
            }
        }

        public static WebException DeserializeWebException(string str)
        {
            Dictionary<string, string> dictionary = null;
            using (Stream stream = new MemoryStream())
            {
                StringToStream(EncodingHelper.Base64Decode(str), stream);
                stream.Seek(0, SeekOrigin.Begin);
                dictionary = DeserializeDictionary(stream);
            }

            const string WebExceptionGeneratorUrl = "http://localhost:8081";
            WebExceptionGenerator.Settings = dictionary;
            using (WebApp.Start<WebExceptionGenerator>(WebExceptionGeneratorUrl))
            {
                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(WebExceptionGeneratorUrl);
                    request.ContentType = "application/x-www-form-urlencoded";
                    request.GetResponse();
                    return null;
                }
                catch (WebException ex)
                {
                    return ex;
                }
            }
        }

        public static WebResponse DeserializeWebResponse(string str)
        {
            Dictionary<string, string> dictionary = new Dictionary<string,string>();
            dictionary["Body"] = str;
            dictionary["StatusCode"] = ((int)HttpStatusCode.OK).ToString();

            const string WebExceptionGeneratorUrl = "http://localhost:8081";
            WebExceptionGenerator.Settings = dictionary;
            using (WebApp.Start<WebExceptionGenerator>(WebExceptionGeneratorUrl))
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(WebExceptionGeneratorUrl);
                request.ContentType = "application/x-www-form-urlencoded";
                return request.GetResponse();
            }
        }

        public static string SerializeDateTime(DateTime dateTime)
        {
            return dateTime.Ticks.ToString();
        }

        public static DateTime DeserializeDateTime(string str)
        {
            return new DateTime(long.Parse(str));
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

        internal class WebExceptionGenerator
        {
            public static Dictionary<string, string> Settings { get; set; }

            public void Configuration(IAppBuilder app)
            {
                app.Run(ctx =>
                {
                    var response = ctx.Response;
                    if (Settings.ContainsKey("WWW-AuthenticateHeader"))
                    {
                        response.Headers.Add("WWW-Authenticate",
                            new string[] { Settings["WWW-AuthenticateHeader"] });
                    }

                    response.StatusCode = int.Parse(Settings["StatusCode"]);
                    return response.WriteAsync(Settings["Body"]);
                });
            }
        }
    }
}
