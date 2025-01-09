// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.Core;
using System.Text.Json.Serialization;
using Microsoft.Identity.Client.Utils;
using Foundation;
using System.Text.Json;

namespace Microsoft.Identity.Client.Platforms.iOS
{
    internal class IntuneEnrollmentIdHelper
    {
        const string EnrollmentIdKey = "intune_app_protection_enrollment_id_V1";
        const string Intune_MamResourceKey = "intune_mam_resource_V 1";

        internal static string GetEnrollmentId(ILoggerAdapter logger)
        {
            var keychainData = GetRawEnrollmentId();
            if(!string.IsNullOrEmpty(keychainData))
            {
                try
                {
                    var enrollmentIDs = JsonHelper.DeserializeFromJson<EnrollmentIDs>(keychainData);

                    if ((enrollmentIDs?.EnrollmentIds?.Count ?? 0) > 0)
                    {
                        return enrollmentIDs.EnrollmentIds[0].EnrollmentId;
                    }
                }
                catch (JsonException jEx)
                {
                    logger.Error($"Failed to parse enrollmentID for KeychainData");
                    logger.ErrorPii(jEx);

                    return string.Empty;
                }
            }
            return string.Empty;
        }

        internal static string GetRawEnrollmentId()
        {
            var keychainData = NSUserDefaults.StandardUserDefaults.StringForKey(EnrollmentIdKey);
            return keychainData;
        }

        internal static string GetRawMamResources()
        {
            var keychainData = NSUserDefaults.StandardUserDefaults.StringForKey(Intune_MamResourceKey);
            return keychainData;
        }
    }

    /// <summary>
    /// This class corresponds to the EnrollmentIDs entry in the Keychain
    /// </summary>
    internal class EnrollmentIDs
    {
        [JsonPropertyName("enrollment_ids")]
        public List<EnrollmentIdProps> EnrollmentIds { get; set; }
    }

    internal class EnrollmentIdProps
    {
        private const string HomeAccountIdKey = "home_account_id";

        private const string TidsKey = "tid";

        private const string UserIdKey = "user_id";

        private const string OidKey = "oid";

        private const string EnrollmentIdKey = "enrollment_id";

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
}
