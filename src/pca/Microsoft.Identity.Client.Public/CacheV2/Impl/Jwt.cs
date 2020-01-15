// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.CacheV2.Impl.Utils;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Json.Linq;

namespace Microsoft.Identity.Client.CacheV2.Impl
{
    internal class Jwt
    {
        public Jwt(string raw)
        {
            Raw = raw;
            if (string.IsNullOrWhiteSpace(Raw))
            {
                // warning: constructed jwt from empty string
                return;
            }

            string[] sections = Raw.Split('.');
            if (sections.Length != 3)
            {
                throw new ArgumentException("failed jwt decode: wrong number of sections", nameof(Raw));
            }

            Payload = Base64UrlHelpers.DecodeToString(sections[1]);
            Json = JObject.Parse(Payload);
            IsSigned = !string.IsNullOrEmpty(sections[2]);
        }

        protected JObject Json { get; }
        public string Raw { get; }
        public string Payload { get; }
        public bool IsSigned { get; }
        public bool IsEmpty => Json.IsEmpty();
    }
}
