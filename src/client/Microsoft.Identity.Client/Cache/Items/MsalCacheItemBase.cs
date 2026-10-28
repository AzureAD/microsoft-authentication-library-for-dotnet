// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Text;
using Microsoft.Identity.Client.Utils;
using JObject = System.Text.Json.Nodes.JsonObject;

namespace Microsoft.Identity.Client.Cache.Items
{
    [System.Diagnostics.DebuggerDisplay("env: {Environment} accountId: {HomeAccountId}")]
    internal abstract class MsalCacheItemBase : MsalItemWithAdditionalFields
    {
        internal string HomeAccountId { get; set; }
        internal string Environment { get; set; }
        internal string RawClientInfo { get; set; }

        internal long SubjectKeyVersion { get; set; } = 1;

        internal bool HasSubjectIdentifier =>
            !string.IsNullOrEmpty(HomeAccountId) && string.IsNullOrEmpty(RawClientInfo);

        internal bool IsLegacySubjectEntry => HasSubjectIdentifier && SubjectKeyVersion != 1;

        internal bool MatchesHomeAccountId(string homeAccountId)
        {
            return string.Equals(HomeAccountId, homeAccountId,
                HasSubjectIdentifier ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
        }

        internal string GetHomeAccountIdKey()
        {
            if (!HasSubjectIdentifier || IsLegacySubjectEntry)
            {
                return HomeAccountId;
            }

            // Encode the opaque subject before the shared key builder folds case.
            // Fixed-width hex also prevents subjects containing delimiters from aliasing other key parts.
            var key = new StringBuilder("~subject-v1~");
            foreach (char character in HomeAccountId)
            {
                key.Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
            }

            return key.ToString();
        }

        internal override void PopulateFieldsFromJObject(JObject j)
        {
            HomeAccountId = JsonHelper.ExtractExistingOrEmptyString(j, StorageJsonKeys.HomeAccountId);
            Environment = JsonHelper.ExtractExistingOrEmptyString(j, StorageJsonKeys.Environment);
            RawClientInfo = JsonHelper.ExtractExistingOrEmptyString(j, StorageJsonKeys.ClientInfo);
            SubjectKeyVersion = JsonHelper.ExtractParsedIntOrZero(j, StorageJsonKeys.SubjectKeyVersion);
            if (!HasSubjectIdentifier)
            {
                SubjectKeyVersion = 1;
            }

            // Important: order matters.  This MUST be the last one called since it will extract the
            // remaining fields out.
            base.PopulateFieldsFromJObject(j);
        }

        internal override JObject ToJObject()
        {
            var json = base.ToJObject();
            SetItemIfValueNotNull(json, StorageJsonKeys.HomeAccountId, HomeAccountId);
            SetItemIfValueNotNull(json, StorageJsonKeys.Environment, Environment);
            SetItemIfValueNotNull(json, StorageJsonKeys.ClientInfo, RawClientInfo);
            if (HasSubjectIdentifier && SubjectKeyVersion != 0)
            {
                SetItemIfValueNotNull(json, StorageJsonKeys.SubjectKeyVersion, SubjectKeyVersion.ToString(CultureInfo.InvariantCulture));
            }

            return json;
        }
    }
}
