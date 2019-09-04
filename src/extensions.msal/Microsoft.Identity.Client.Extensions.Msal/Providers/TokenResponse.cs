// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Microsoft.Identity.Client.Extensions.Msal.Providers
{
        /// <summary>
    /// Used to hold the deserialized token response.
    /// </summary>
    [DataContract]
    internal class TokenResponse
    {
        private const string TokenResponseFormatExceptionMessage = "Token response is not in the expected format.";

        internal enum DateFormat
        {
            Unix,
            DateTimeString
        }

        // MSI endpoint return access_token
        [DataMember(Name = "access_token", IsRequired = false)]
        public string AccessToken { get; private set; }

        // MSI endpoint return expires_on
        [DataMember(Name = "expires_on", IsRequired = false)]
        public string ExpiresOn { get; private set; }

        [DataMember(Name = "error_description", IsRequired = false)]
        public string ErrorDescription { get; private set; }

        // MSI endpoint return token_type
        [DataMember(Name = "token_type", IsRequired = false)]
        public string TokenType { get; private set; }

        /// <summary>
        /// Parse token response returned from OAuth provider.
        /// While more fields are returned, we only need the access token.
        /// </summary>
        /// <param name="tokenResponse">This is the response received from OAuth endpoint that has the access token in it.</param>
        /// <returns></returns>
        public static TokenResponse Parse(string tokenResponse)
        {
            try
            {
                return DeserializeFromJson<TokenResponse>(Encoding.UTF8.GetBytes(tokenResponse));
            }
            catch (Exception exp)
            {
                throw new FormatException($"{TokenResponseFormatExceptionMessage} Exception Message: {exp.Message}");
            }
        }

        private static T DeserializeFromJson<T>(byte[] jsonByteArray)
        {
            if (jsonByteArray == null || jsonByteArray.Length == 0)
            {
                return default;
            }

            T response;
            var serializer = new DataContractJsonSerializer(typeof (T));
            using (var stream = new MemoryStream(jsonByteArray))
            {
                response = (T) serializer.ReadObject(stream);
            }

            return response;
        }
    }
}
