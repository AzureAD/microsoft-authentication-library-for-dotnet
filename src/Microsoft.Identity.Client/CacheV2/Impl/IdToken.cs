// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.CacheV2.Impl
{
    internal class IdToken : Jwt
    {
        public IdToken(string raw)
            : base(raw)
        {
        }

        public string PreferredUsername => JsonUtils.GetExistingOrEmptyString(Json, "preferred_username");
        public string GivenName => JsonUtils.GetExistingOrEmptyString(Json, StorageJsonKeys.GivenName);
        public string FamilyName => JsonUtils.GetExistingOrEmptyString(Json, StorageJsonKeys.FamilyName);
        public string MiddleName => JsonUtils.GetExistingOrEmptyString(Json, StorageJsonKeys.MiddleName);
        public string Name => JsonUtils.GetExistingOrEmptyString(Json, StorageJsonKeys.Name);
        public string AlternativeId => JsonUtils.GetExistingOrEmptyString(Json, "altsecid");
        public string Upn => JsonUtils.GetExistingOrEmptyString(Json, "upn");
        public string Email => JsonUtils.GetExistingOrEmptyString(Json, "email");
        public string Subject => JsonUtils.GetExistingOrEmptyString(Json, "sub");
        public string Oid => JsonUtils.GetExistingOrEmptyString(Json, "oid");
        public string TenantId => JsonUtils.GetExistingOrEmptyString(Json, "tid");
    }
}
