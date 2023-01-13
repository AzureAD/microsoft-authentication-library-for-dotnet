// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Platforms.net6
{
    internal class MsalJsonSerializerOptions
    {
        private static JsonSerializerOptions s_serializerOptions;

        public static JsonSerializerOptions Options
        {
            get
            {
                return s_serializerOptions ??=
                    new JsonSerializerOptions()
                    {
                        NumberHandling = JsonNumberHandling.AllowReadingFromString |
                        JsonNumberHandling.WriteAsString,
                        AllowTrailingCommas = true,
                        Converters =
                        {
                            new JsonStringConverter(),
                        }
                    };
            }
        }
    }
}
