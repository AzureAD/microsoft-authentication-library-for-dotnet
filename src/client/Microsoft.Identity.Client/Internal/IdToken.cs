// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using Microsoft.Identity.Client.Utils;
#if SUPPORTS_SYSTEM_TEXT_JSON
using System.Text.Json;
#else
using Microsoft.Identity.Json;
using Microsoft.Identity.Json.Linq;
#endif

namespace Microsoft.Identity.Client.Internal
{
    internal static class IdTokenClaim
    {
        public const string Issuer = "iss";
        public const string ObjectId = "oid";
        public const string Subject = "sub";
        public const string TenantId = "tid";
        public const string Version = "ver";
        public const string PreferredUsername = "preferred_username";
        public const string Name = "name";
        public const string Email = "email";
        public const string HomeObjectId = "home_oid";
        public const string GivenName = "given_name";
        public const string FamilyName = "family_name";
        public const string Upn = "upn";
    }

    internal static class JsonClaimValueTypes
    {
        public const string Json = "JSON";
        public const string JsonArray = "JSON_ARRAY";
        public const string JsonNull = "JSON_NULL";
    }

    internal class IdToken
    {
        private const string DefaultIssuser = "LOCAL AUTHORITY";

        /// <summary>
        /// The OID claim is a unique identifier (GUID) for the user object in Azure AD.
        /// Guest Users have different OID.
        /// This is a stable ID across all apps.
        /// 
        /// IMPORTANT: There are rare cases where this is missing! 
        /// </summary>
        /// <remarks>
        /// Avoid using as it is not guaranteed non-null. Use <see cref="GetUniqueId"/> instead.
        /// </remarks>
        public string ObjectId { get; private set; }

        /// <summary>
        /// The sub claim is a unique identifier for user + app. 
        /// </summary>
        public string Subject { get; private set; }

        public string TenantId { get; private set; }

        public string PreferredUsername { get; private set; }

        public string Name { get; private set; }
        public string Email { get; private set; }

        public string Upn { get; private set; }

        public string GivenName { get; private set; }

        public string FamilyName { get; private set; }

        public string GetUniqueId()
        {
            return ObjectId ?? Subject;
        }

        public ClaimsPrincipal ClaimsPrincipal { get; private set; }


        private static IdToken ClaimsToToken(List<Claim> claims)
        {
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));
            return new IdToken
            {
                ClaimsPrincipal = principal,
                ObjectId = FindClaim(claims, IdTokenClaim.ObjectId),
                Subject = FindClaim(claims, IdTokenClaim.Subject),
                TenantId = FindClaim(claims, IdTokenClaim.TenantId),
                PreferredUsername = FindClaim(claims, IdTokenClaim.PreferredUsername),
                Name = FindClaim(claims, IdTokenClaim.Name),
                Email = FindClaim(claims, IdTokenClaim.Email),
                Upn = FindClaim(claims, IdTokenClaim.Upn),
                GivenName = FindClaim(claims, IdTokenClaim.GivenName),
                FamilyName = FindClaim(claims, IdTokenClaim.FamilyName)
            };

            static string FindClaim(List<Claim> claims, string type) =>
                claims.SingleOrDefault(_ => string.Equals(_.Type, type, StringComparison.OrdinalIgnoreCase))?.Value;
        }

        #region Using Newtonsoft
#if !SUPPORTS_SYSTEM_TEXT_JSON
        // There are quite a bit of API differences, so duplicated code, ideally will need to be refactored.

        public static IdToken Parse(string idToken)
        {
            if (string.IsNullOrEmpty(idToken))
            {
                return null;
            }

            string[] idTokenSegments = idToken.Split(new[] { '.' });

            if (idTokenSegments.Length < 2)
            {
                throw new MsalClientException(
                    MsalError.InvalidJwtError,
                    MsalErrorMessage.IDTokenMustHaveTwoParts);
            }

            try
            {
                string payload = Base64UrlHelpers.Decode(idTokenSegments[1]);
                var idTokenClaims = JsonConvert.DeserializeObject<Dictionary<string, object>>(payload);

                List<Claim> claims = GetClaimsFromRawToken(idTokenClaims);
                return ClaimsToToken(claims);
            }
            catch (JsonException exc)
            {
                throw new MsalClientException(
                    MsalError.JsonParseError,
                    MsalErrorMessage.FailedToParseIDToken,
                    exc);
            }
        }

        #region IdToken to Claims parsing - logic copied from Wilson!
        private static List<Claim> GetClaimsFromRawToken(Dictionary<string, object> idTokenClaims)
        {
            List<Claim> claims = new List<Claim>();

            string issuer = null;
            if (idTokenClaims.TryGetValue(IdTokenClaim.Issuer, out object issuerObj))
            {
                issuer = issuerObj as string;
            }
            issuer ??= DefaultIssuser;

            foreach (KeyValuePair<string, object> keyValuePair in idTokenClaims)
            {
                if (keyValuePair.Value == null)
                {
                    claims.Add(new Claim(keyValuePair.Key, string.Empty, JsonClaimValueTypes.JsonNull, issuer, issuer));
                    continue;
                }

                var claimValue = keyValuePair.Value as string;
                if (claimValue != null)
                {
                    claims.Add(new Claim(keyValuePair.Key, claimValue, ClaimValueTypes.String, issuer, issuer));
                    continue;
                }

                var jtoken = keyValuePair.Value as JToken;
                if (jtoken != null)
                {
                    AddClaimsFromJToken(claims, keyValuePair.Key, jtoken, issuer);
                    continue;
                }

                var objects = keyValuePair.Value as IEnumerable<object>;
                if (objects != null)
                {
                    foreach (var obj in objects)
                    {
                        claimValue = obj as string;
                        if (claimValue != null)
                        {
                            claims.Add(new Claim(keyValuePair.Key, claimValue, ClaimValueTypes.String, issuer, issuer));
                            continue;
                        }

                        jtoken = obj as JToken;
                        if (jtoken != null)
                        {
                            AddDefaultClaimFromJToken(claims, keyValuePair.Key, jtoken, issuer);
                            continue;
                        }

                        // DateTime claims require special processing. JsonConvert.SerializeObject(obj) will result in "\"dateTimeValue\"". The quotes will be added.
                        if (obj is DateTime dateTimeValue)
                            claims.Add(
                                new Claim(
                                    keyValuePair.Key,
                                    dateTimeValue.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture),
                                    ClaimValueTypes.DateTime,
                                    issuer,
                                    issuer));
                        else
                            claims.Add(
                                new Claim(
                                    keyValuePair.Key,
                                    JsonConvert.SerializeObject(obj),
                                    GetClaimValueType(obj),
                                    issuer,
                                    issuer));
                    }

                    continue;
                }

                IDictionary<string, object> dictionary = keyValuePair.Value as IDictionary<string, object>;
                if (dictionary != null)
                {
                    foreach (var item in dictionary)
                        claims.Add(new Claim(keyValuePair.Key, "{" + item.Key + ":" + JsonConvert.SerializeObject(item.Value) + "}", GetClaimValueType(item.Value), issuer, issuer));

                    continue;
                }

                // DateTime claims require special processing. JsonConvert.SerializeObject(keyValuePair.Value) will result in "\"dateTimeValue\"". The quotes will be added.
                if (keyValuePair.Value is DateTime dateTime)
                    claims.Add(new Claim(keyValuePair.Key, dateTime.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture), ClaimValueTypes.DateTime, issuer, issuer));
                else
                    claims.Add(new Claim(keyValuePair.Key, JsonConvert.SerializeObject(keyValuePair.Value), GetClaimValueType(keyValuePair.Value), issuer, issuer));
            }

            return claims;
        }

        private static void AddClaimsFromJToken(List<Claim> claims, string claimType, JToken jtoken, string issuer)
        {
            if (jtoken.Type == JTokenType.Object)
            {
                claims.Add(new Claim(claimType, jtoken.ToString(Formatting.None), JsonClaimValueTypes.Json));
            }
            else if (jtoken.Type == JTokenType.Array)
            {
                var jarray = jtoken as JArray;
                foreach (var item in jarray)
                {
                    switch (item.Type)
                    {
                        case JTokenType.Object:
                            claims.Add(new Claim(claimType, item.ToString(Formatting.None), JsonClaimValueTypes.Json, issuer, issuer));
                            break;

                        // only go one level deep on arrays.
                        case JTokenType.Array:
                            claims.Add(new Claim(claimType, item.ToString(Formatting.None), JsonClaimValueTypes.JsonArray, issuer, issuer));
                            break;

                        default:
                            AddDefaultClaimFromJToken(claims, claimType, item, issuer);
                            break;
                    }
                }
            }
            else
            {
                AddDefaultClaimFromJToken(claims, claimType, jtoken, issuer);
            }
        }

        private static void AddDefaultClaimFromJToken(List<Claim> claims, string claimType, JToken jtoken, string issuer)
        {
            JValue jvalue = jtoken as JValue;
            if (jvalue != null)
            {
                // String is special because item.ToString(Formatting.None) will result in "/"string/"". The quotes will be added.
                // Boolean needs item.ToString otherwise 'true' => 'True'
                if (jvalue.Type == JTokenType.String)
                    claims.Add(new Claim(claimType, jvalue.Value.ToString(), ClaimValueTypes.String, issuer, issuer));
                // DateTime claims require special processing. jtoken.ToString(Formatting.None) will result in "\"dateTimeValue\"". The quotes will be added.
                else if (jvalue.Value is DateTime dateTimeValue)
                    claims.Add(new Claim(claimType, dateTimeValue.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture), ClaimValueTypes.DateTime, issuer, issuer));
                else
                    claims.Add(new Claim(claimType, jtoken.ToString(Formatting.None), GetClaimValueType(jvalue.Value), issuer, issuer));
            }
            else
                claims.Add(new Claim(claimType, jtoken.ToString(Formatting.None), GetClaimValueType(jtoken), issuer, issuer));
        }

        private static string GetClaimValueType(object obj)
        {
            if (obj == null)
                return JsonClaimValueTypes.JsonNull;

            var objType = obj.GetType();

            if (objType == typeof(string))
                return ClaimValueTypes.String;

            if (objType == typeof(int))
                return ClaimValueTypes.Integer;

            if (objType == typeof(bool))
                return ClaimValueTypes.Boolean;

            if (objType == typeof(double))
                return ClaimValueTypes.Double;

            if (objType == typeof(long))
            {
                long l = (long)obj;
                if (l >= int.MinValue && l <= int.MaxValue)
                    return ClaimValueTypes.Integer;

                return ClaimValueTypes.Integer64;
            }

            if (objType == typeof(DateTime))
                return ClaimValueTypes.DateTime;

            if (objType == typeof(JObject))
                return JsonClaimValueTypes.Json;

            if (objType == typeof(JArray))
                return JsonClaimValueTypes.JsonArray;

            return objType.ToString();
        }

        #endregion
#endif
        #endregion

        #region Using System.Text.Json
#if SUPPORTS_SYSTEM_TEXT_JSON
        // There are quite a bit of API differences, so duplicated code, ideally will need to be refactored.
        public static IdToken Parse(string idToken)
        {
            if (string.IsNullOrEmpty(idToken))
            {
                return null;
            }

            string[] idTokenSegments = idToken.Split(new[] { '.' });

            if (idTokenSegments.Length < 2)
            {
                throw new MsalClientException(
                    MsalError.InvalidJwtError,
                    MsalErrorMessage.IDTokenMustHaveTwoParts);
            }

            try
            {
                string payload = Base64UrlHelpers.Decode(idTokenSegments[1]);
                var idTokenClaims = JsonDocument.Parse(payload);
                List<Claim> claims = GetClaimsFromRawToken(idTokenClaims);
                return ClaimsToToken(claims);
            }
            catch (JsonException exc)
            {
                throw new MsalClientException(
                    MsalError.JsonParseError,
                    MsalErrorMessage.FailedToParseIDToken,
                    exc);
            }
        }

        private static List<Claim> GetClaimsFromRawToken(JsonDocument jsonDocument)
        {
            var idTokenClaims = jsonDocument.RootElement;

            List<Claim> claims = new List<Claim>();

            string issuer = null;
            if (idTokenClaims.TryGetProperty(IdTokenClaim.Issuer, out JsonElement issuerObj))
            {
                issuer = issuerObj.ValueKind == JsonValueKind.String ? issuerObj.GetString() : null;
            }
            issuer ??= DefaultIssuser;

            foreach (var jsonProperty in idTokenClaims.EnumerateObject())
            {
                if (jsonProperty.Value.ValueKind == JsonValueKind.Null)
                {
                    claims.Add(new Claim(jsonProperty.Name, string.Empty, JsonClaimValueTypes.JsonNull, issuer, issuer));
                    continue;
                }

                var claimValue = jsonProperty.Value.ValueKind == JsonValueKind.String ? jsonProperty.Value.GetString() : null;
                if (claimValue != null)
                {
                    claims.Add(new Claim(jsonProperty.Name, claimValue, ClaimValueTypes.String, issuer, issuer));
                    continue;
                }

                if (jsonProperty.Value.ValueKind == JsonValueKind.Object)
                {
                    AddClaimsFromJToken(claims, jsonProperty.Name, jsonProperty.Value, issuer);
                    continue;
                }

                if (jsonProperty.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var jtoken in jsonProperty.Value.EnumerateArray())
                    {
                        claimValue = jtoken.ValueKind == JsonValueKind.String ? jtoken.GetString() : null;
                        if (claimValue != null)
                        {
                            claims.Add(new Claim(jsonProperty.Name, claimValue, ClaimValueTypes.String, issuer, issuer));
                            continue;
                        }

                        if (jtoken.ValueKind == JsonValueKind.Object)
                        {
                            AddDefaultClaimFromJToken(claims, jsonProperty.Name, jtoken, issuer);
                            continue;
                        }

                        // DateTime claims require special processing. JsonConvert.SerializeObject(obj) will result in "\"dateTimeValue\"". The quotes will be added.                        
                        if (jtoken.ValueKind == JsonValueKind.String && jtoken.TryGetDateTime(out DateTime dateTimeValue))
                            claims.Add(
                                new Claim(
                                    jsonProperty.Name,
                                    dateTimeValue.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture),
                                    ClaimValueTypes.DateTime,
                                    issuer,
                                    issuer));
                        else
                            claims.Add(
                                    new Claim(
                                        jsonProperty.Name,
                                        jtoken.ToString(),
                                        GetClaimValueType(jtoken),
                                        issuer,
                                        issuer));
                    }

                    continue;
                }

                if (jsonProperty.Value.ValueKind == JsonValueKind.Object)
                {
                    foreach (var item in jsonProperty.Value.EnumerateObject())
                        claims.Add(new Claim(jsonProperty.Name, "{" + item.Name + ":" + item.Value.ToString() + "}", GetClaimValueType(item.Value), issuer, issuer));

                    continue;
                }

                // DateTime claims require special processing. JsonConvert.SerializeObject(keyValuePair.Value) will result in "\"dateTimeValue\"". The quotes will be added.
                if (jsonProperty.Value.ValueKind == JsonValueKind.String && jsonProperty.Value.TryGetDateTime(out DateTime dateTime))
                    claims.Add(new Claim(jsonProperty.Name, dateTime.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture), ClaimValueTypes.DateTime, issuer, issuer));
                else
                    claims.Add(new Claim(jsonProperty.Name, jsonProperty.Value.ToString(), GetClaimValueType(jsonProperty.Value), issuer, issuer));
            }

            return claims;
        }

        private static void AddClaimsFromJToken(List<Claim> claims, string claimType, JsonElement jtoken, string issuer)
        {
            if (jtoken.ValueKind == JsonValueKind.Object)
            {
                claims.Add(new Claim(claimType, jtoken.ToString(), JsonClaimValueTypes.Json));
            }
            else if (jtoken.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in jtoken.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Object)
                    {
                        claims.Add(new Claim(claimType, item.ToString(), JsonClaimValueTypes.Json, issuer, issuer));
                    }
                    else if (item.ValueKind == JsonValueKind.Array)
                    {
                        // only go one level deep on arrays.
                        claims.Add(new Claim(claimType, item.ToString(), JsonClaimValueTypes.JsonArray, issuer, issuer));
                    }
                    else
                    {
                        AddDefaultClaimFromJToken(claims, claimType, item, issuer);
                    }
                }
            }
            else
            {
                AddDefaultClaimFromJToken(claims, claimType, jtoken, issuer);
            }
        }

        private static void AddDefaultClaimFromJToken(List<Claim> claims, string claimType, JsonElement jtoken, string issuer)
        {
            if (jtoken.ValueKind == JsonValueKind.String)
            {
                // DateTime claims require special processing. jtoken.ToString(Formatting.None) will result in "\"dateTimeValue\"". The quotes will be added.
                if (jtoken.TryGetDateTime(out DateTime dateTimeValue))
                    claims.Add(new Claim(claimType, dateTimeValue.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture), ClaimValueTypes.DateTime, issuer, issuer));
                else
                {
                    var stringValue = jtoken.GetString();
                    if (stringValue != null)
                        // String is special because item.ToString(Formatting.None) will result in "/"string/"". The quotes will be added.
                        // Boolean needs item.ToString otherwise 'true' => 'True'
                        claims.Add(new Claim(claimType, stringValue, ClaimValueTypes.String, issuer, issuer));
                    else
                        claims.Add(new Claim(claimType, jtoken.ToString(), GetClaimValueType(jtoken), issuer, issuer));
                }
            }
            else
                claims.Add(new Claim(claimType, jtoken.ToString(), GetClaimValueType(jtoken), issuer, issuer));
        }

        private static string GetClaimValueType(JsonElement obj)
        {
            if (obj.ValueKind == JsonValueKind.Null)
                return JsonClaimValueTypes.JsonNull;

            var valueKind = obj.ValueKind;

            if (valueKind == JsonValueKind.True || valueKind == JsonValueKind.False)
                return ClaimValueTypes.Boolean;

            if (valueKind == JsonValueKind.Number)
            {
                if (obj.TryGetInt32(out _))
                    return ClaimValueTypes.Integer;

                if (obj.TryGetDouble(out _))
                    return ClaimValueTypes.Double;

                long l = obj.GetInt64();
                if (l >= int.MinValue && l <= int.MaxValue)
                    return ClaimValueTypes.Integer;

                return ClaimValueTypes.Integer64;
            }

            if (valueKind == JsonValueKind.String)
            {
                if (obj.TryGetDateTime(out _))
                    return ClaimValueTypes.DateTime;

                return ClaimValueTypes.String;
            }

            if (valueKind == JsonValueKind.Object)
                return JsonClaimValueTypes.Json;

            if (valueKind == JsonValueKind.Array)
                return JsonClaimValueTypes.JsonArray;

            return valueKind.ToString();
        }
#endif
        #endregion
    }
}
