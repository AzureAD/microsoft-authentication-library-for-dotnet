// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Identity.Client.Utils
{
    internal static class JsonHelper
    {
        internal static JsonSerializerOptions s_jsonSerializerOptions;
            
        static JsonHelper()
        {
            s_jsonSerializerOptions = new JsonSerializerOptions
            {
                NumberHandling = JsonNumberHandling.AllowReadingFromString,
                AllowTrailingCommas = true
            };
            s_jsonSerializerOptions.Converters.Add(new JsonStringConverter());
        }

        internal static string SerializeToJson<T>(T toEncode)
        {
            return JsonSerializer.Serialize(toEncode);
        }

        internal static T DeserializeFromJson<T>(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(json, s_jsonSerializerOptions);
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
                return (T)JsonSerializer.Deserialize(stream, typeof(T), s_jsonSerializerOptions);
        }
    }
}
