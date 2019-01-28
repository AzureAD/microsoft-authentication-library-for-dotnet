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

using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.Utils
{
    internal static class JsonHelper
    {
        internal static string SerializeToJson<T>(T toEncode)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof (T));
                ser.WriteObject(stream, toEncode);
                return Encoding.UTF8.GetString(stream.ToArray(), 0, (int) stream.Position);
            }
        }

        internal static T DeserializeFromJson<T>(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return default;
            }

            return DeserializeFromJson<T>(json.ToByteArray());
        }

        internal static T TryToDeserializeFromJson<T>(string json, RequestContext requestContext = null)
        {
            if (string.IsNullOrEmpty(json))
            {
                return default;
            }

            T result = default;
            try
            {
                result = DeserializeFromJson<T>(json.ToByteArray());
            }
            catch (System.Runtime.Serialization.SerializationException ex)
            {
                requestContext?.Logger?.WarningPii(ex);
            }

            return result;
        }

        internal static T DeserializeFromJson<T>(byte[] jsonByteArray)
        {
            if (jsonByteArray == null || jsonByteArray.Length == 0)
            {
                return default;
            }

            T response;
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof (T));
            using (MemoryStream stream = new MemoryStream(jsonByteArray))
            {
                response = (T) serializer.ReadObject(stream);
            }

            return response;
        }
    }
}