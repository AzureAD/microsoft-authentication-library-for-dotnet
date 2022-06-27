// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using System.Text;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Json;

namespace Microsoft.Identity.Client.Utils
{
    internal static class JsonHelper
    {
        internal static string SerializeToJson<T>(T toEncode)
        {
            return JsonConvert.SerializeObject(toEncode);
        }

        internal static T DeserializeFromJson<T>(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return default;
            }

            return JsonConvert.DeserializeObject<T>(json);
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
            catch (JsonException ex)
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

            using (var stream = new MemoryStream(jsonByteArray))
            using (var reader = new StreamReader(stream, Encoding.UTF8))
                return (T)JsonSerializer.Create().Deserialize(reader, typeof(T));
        }
    }
}
