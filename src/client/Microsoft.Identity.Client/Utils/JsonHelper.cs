// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
