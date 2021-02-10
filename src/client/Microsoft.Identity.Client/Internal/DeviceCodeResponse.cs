// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Json;
using Microsoft.Identity.Json.Linq;

namespace Microsoft.Identity.Client.Internal
{
    [JsonObject]
    [Preserve(AllMembers = true)]
    internal class DeviceCodeResponse : OAuth2ResponseBase, IJsonSerializable<DeviceCodeResponse>
    {
        private const string UserCodePropertyName = "user_code";
        private const string DeviceCodePropertyName = "device_code";
        private const string VerificationUrlPropertyName = "verification_url";
        private const string VerificationUriPropertyName = "verification_uri";
        private const string ExpiresInPropertyName = "expires_in";
        private const string IntervalPropertyName = "interval";
        private const string MessagePropertyName = "message";

        [JsonProperty(PropertyName = UserCodePropertyName)]
        public string UserCode { get; internal set; }

        [JsonProperty(PropertyName = DeviceCodePropertyName)]
        public string DeviceCode { get; internal set; }

        [JsonProperty(PropertyName = VerificationUrlPropertyName)]
        public string VerificationUrl { get; internal set; }

        // This is the OAuth2 standards compliant value.
        // It should be used if it's present, if it's not then fallback to VerificiationUrl
        [JsonProperty(PropertyName = VerificationUriPropertyName)]
        public string VerificationUri { get; internal set; }

        [JsonProperty(PropertyName = ExpiresInPropertyName)]
        public long ExpiresIn { get; internal set; }

        [JsonProperty(PropertyName = IntervalPropertyName)]
        public long Interval { get; internal set; }

        [JsonProperty(PropertyName = MessagePropertyName)]
        public string Message { get; internal set; }

        public new DeviceCodeResponse DeserializeFromJson(string json)
        {
            JObject jObject = JObject.Parse(json);

            UserCode = jObject[UserCodePropertyName]?.ToString();
            DeviceCode = jObject[DeviceCodePropertyName]?.ToString();
            VerificationUrl = jObject[VerificationUrlPropertyName]?.ToString();
            VerificationUri = jObject[VerificationUriPropertyName]?.ToString();
            ExpiresIn = Int64.Parse(jObject[ExpiresInPropertyName]?.ToString(), System.Globalization.CultureInfo.InvariantCulture);
            Interval = Int64.Parse(jObject[IntervalPropertyName]?.ToString(), System.Globalization.CultureInfo.InvariantCulture);
            Message = jObject[MessagePropertyName]?.ToString();
            base.DeserializeFromJson(json);

            return this;
        }

        public new string SerializeToJson()
        {
            JObject jObject = new JObject(
                new JProperty(UserCodePropertyName, UserCode),
                new JProperty(DeviceCodePropertyName, DeviceCode),
                new JProperty(VerificationUrlPropertyName, VerificationUrl),
                new JProperty(VerificationUriPropertyName, VerificationUri),
                new JProperty(ExpiresInPropertyName, ExpiresIn),
                new JProperty(IntervalPropertyName, Interval),
                new JProperty(MessagePropertyName, Message),
                JObject.Parse(base.SerializeToJson()).Properties());

            return jObject.ToString(Formatting.None);
        }

        public DeviceCodeResult GetResult(string clientId, ISet<string> scopes)
        {
            // VerificationUri should be used if it's present, and if not fall back to VerificationUrl
            string verification = string.IsNullOrWhiteSpace(VerificationUri) ? VerificationUrl : VerificationUri;

            return new DeviceCodeResult(
                UserCode,
                DeviceCode,
                verification,
                DateTime.UtcNow.AddSeconds(ExpiresIn),
                Interval,
                Message,
                clientId,
                scopes);
        }
    }
}
