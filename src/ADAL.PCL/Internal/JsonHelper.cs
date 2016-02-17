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

using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Microsoft.Identity.Client.Internal
{
    internal static class JsonHelper
    {
        internal static string EncodeToJson<T>(T toEncode)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(T));
                ser.WriteObject(stream, toEncode);
                return Encoding.UTF8.GetString(stream.ToArray(), 0, (int)stream.Position);
            }
        }

        internal static T DecodeFromJson<T>(string json)
        {
            T response;
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof (T));
            using (MemoryStream stream = new MemoryStream(new StringBuilder(json).ToByteArray()))
            {
                response = ((T) serializer.ReadObject(stream));
            }

            return response;
        }
    }
}
