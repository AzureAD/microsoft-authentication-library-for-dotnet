// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.Core;
using System.Text.Json;
using System.Text.Json.Serialization;
#if iOS
using Foundation;
#endif

namespace Microsoft.Identity.Client.Platforms.iOS
{
    internal class IntuneEnrollmentIdHelper
    {
        const string EnrollmentIdKey = "intune_app_protection_enrollment_id_V1";

        internal static string GetEnrollmentId(ILoggerAdapter logger)
        {
#if iOS
            var keychainData = GetRawEnrollmentId();
            if(!string.IsNullOrEmpty(keychainData))
            {
                try
                {
                    var enrollmentIDs = JsonSerializer.Deserialize<EnrollmentIDs>(keychainData);

                    if ((enrollmentIDs?.EnrollmentIds?.Count ?? 0) > 0)
                    {
                        return enrollmentIDs.EnrollmentIds[0].EnrollmentId;
                    }
                }
                catch (JsonException jEx)
                {
                    logger.ErrorPii($"Failed to parse enrollmentID for KeychainData: {keychainData}", string.Empty);
                    logger.ErrorPii(jEx);

                    return string.Empty;
                }
            }
#endif
            return string.Empty;
        }

        internal static string GetRawEnrollmentId()
        {
#if iOS
            var keychainData = NSUserDefaults.StandardUserDefaults.StringForKey(EnrollmentIdKey);
            return keychainData;
#else
            return string.Empty;
#endif
        }

        /// <summary>
        /// This class corresponds to the EnrollmentIDs entry in the Keychain
        /// </summary>
        internal class EnrollmentIDs
        {
            private const string EnrollmentIdsKey = "enrollment_ids";

            private const string HomeAccountIdKey = "home_account_id";

            private const string TidsKey = "tid";

            private const string UserIdKey = "user_id";

            private const string OidKey = "oid";

            private const string EnrollmentIdKey = "enrollment_id";

            internal class EnrollmentIdProps
            {
                [JsonPropertyName(HomeAccountIdKey)]
                public string HomeAccountId { get; set; }

                [JsonPropertyName(TidsKey)]
                public string Tid { get; set; }

                [JsonPropertyName(UserIdKey)]
                public string UserId { get; set; }

                [JsonPropertyName(OidKey)]
                public string Oid { get; set; }

                [JsonPropertyName(EnrollmentIdKey)]
                public string EnrollmentId { get; set; }
            }

            [JsonPropertyName(EnrollmentIdsKey)]
            public List<EnrollmentIdProps> EnrollmentIds { get; set; }
        }
    }
}
