//----------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Cache;
using Security;
using Foundation;

namespace Microsoft.Identity.Client
{
    internal class TokenCacheAccessor : ITokenCacheAccessor
    {
        private const string AccessTokenServiceId = "com.microsoft.identity.client.accessToken";
        private const string RefreshTokenServiceId = "com.microsoft.identity.client.refreshToken";

        private const bool _defaultSyncSetting = false;
        private const SecAccessible _defaultAccessiblityPolicy = SecAccessible.AfterFirstUnlockThisDeviceOnly;

        private RequestContext _requestContext;

        public TokenCacheAccessor()
        {
        }

        public TokenCacheAccessor(RequestContext requestContext) : this()
        {
            _requestContext = requestContext;
        }

        public void SaveAccessToken(string cacheKey, string item)
        {
            SetValueForKey(cacheKey, item, AccessTokenServiceId);
        }

        public void SaveRefreshToken(string cacheKey, string item)
        {
            SetValueForKey(cacheKey, item, RefreshTokenServiceId);
        }

        public string GetRefreshToken(string refreshTokenKey)
        {
            return GetValue(refreshTokenKey, RefreshTokenServiceId);
        }

        public void DeleteAccessToken(string cacheKey)
        {
            Remove(cacheKey, AccessTokenServiceId);
        }

        public void DeleteRefreshToken(string cacheKey)
        {
            Remove(cacheKey, RefreshTokenServiceId);
        }

        public ICollection<string> GetAllAccessTokensAsString()
        {
            return GetValues(AccessTokenServiceId);
        }

        public ICollection<string> GetAllRefreshTokensAsString()
        {
            return GetValues(RefreshTokenServiceId);
        }

        public ICollection<string> GetAllAccessTokenKeys()
        {
            return GetKeys(AccessTokenServiceId);
        }

        public ICollection<string> GetAllRefreshTokenKeys()
        {
            return GetKeys(RefreshTokenServiceId);
        }

        private string GetValue(string key, string service)
        {
            var queryRecord = new SecRecord(SecKind.GenericPassword)
            {
                Account = key,
                Service = service,
                Label = key
            };

            var match = SecKeyChain.QueryAsRecord(queryRecord, out SecStatusCode resultCode);

            return (resultCode == SecStatusCode.Success)
                ? match.ValueData.ToString(NSStringEncoding.UTF8)
                : string.Empty;
        }

        private ICollection<string> GetValues(string service)
        {
            var queryRecord = new SecRecord(SecKind.GenericPassword)
            {
                Service = service
            };

            SecRecord[] records = SecKeyChain.QueryAsRecord(queryRecord, Int32.MaxValue, out SecStatusCode resultCode);

            ICollection<string> res = new List<string>();

            if (resultCode == SecStatusCode.Success)
            {
                foreach (var record in records)
                {
                    string str = record.ValueData.ToString(NSStringEncoding.UTF8);
                    res.Add(str);
                }
            }

            return res;
        }

        private ICollection<string> GetKeys(string service)
        {
            var queryRecord = new SecRecord(SecKind.GenericPassword)
            {
                Service = service
            };

            SecRecord[] records = SecKeyChain.QueryAsRecord(queryRecord, Int32.MaxValue, out SecStatusCode resultCode);

            ICollection<string> res = new List<string>();

            if (resultCode == SecStatusCode.Success)
            {
                foreach (var record in records)
                {
                    res.Add(record.Account);
                }
            }

            return res;
        }

        private SecStatusCode SetValueForKey(string key, string value, string service)
        {
            Remove(key, service);

            var result = SecKeyChain.Add(CreateRecord(key, value, service));

            if (result == SecStatusCode.Param)
            {
                _requestContext.Logger.Warning("Failed to remove cache record from iOS Keychain: SecStatusCode.Param. Ensure that your project contains an Entitlements.plist file as it may be required for keychain access due to a known bug with Xamarin/iOS.");
            }
            else if (result != SecStatusCode.Success)
            {
                _requestContext.Logger.Warning("Failed to add cache record to iOS Keychain: SecStatusCode." + result.ToString());
            }

            return result;
        }

        private SecRecord CreateRecord(string key, string value, string service)
        {
            return new SecRecord(SecKind.GenericPassword)
            {
                Account = key,
                Service = service,
                Label = key,
                ValueData = NSData.FromString(value, NSStringEncoding.UTF8),
                Accessible = _defaultAccessiblityPolicy,
                Synchronizable = _defaultSyncSetting
            };
        }

        private SecStatusCode Remove(string key, string service)
        {
            var record = new SecRecord(SecKind.GenericPassword)
            {
                Account = key,
                Service = service,
                Label = key
            };

            var result = SecKeyChain.Remove(record);

            if (result == SecStatusCode.Param)
            {
                _requestContext.Logger.Warning("Failed to remove cache record from iOS Keychain: SecStatusCode.Param. Ensure that your project contains an Entitlements.plist file as it may be required for keychain access due to a known bug with Xamarin/iOS.");
            }
            else if (result != SecStatusCode.Success )
            {
                _requestContext.Logger.Warning("Failed to remove cache record from iOS Keychain: SecStatusCode." + result.ToString());
            }
            return result;
        }

        private void RemoveAll(string service)
        {
            var queryRecord = new SecRecord(SecKind.GenericPassword)
            {
                Service = service
            };

            SecRecord[] records = SecKeyChain.QueryAsRecord(queryRecord, Int32.MaxValue, out SecStatusCode resultCode);

            if (resultCode == SecStatusCode.Success)
            {
                foreach (var record in records)
                {
                    var result = SecKeyChain.Remove(record);

                    if (result == SecStatusCode.Param)
                    {
                        _requestContext.Logger.Warning("Failed to remove cache record from iOS Keychain: SecStatusCode.Param. Ensure that your project contains an Entitlements.plist file as it may be required for keychain access due to a known bug with Xamarin/iOS.");
                    }
                    else if (result != SecStatusCode.Success)
                    {
                        _requestContext.Logger.Warning("Failed to add cache record to iOS Keychain: SecStatusCode." + result.ToString());
                    }
                }
            }
        }

        public void Clear()
        {
            foreach (var key in GetAllAccessTokenKeys())
            {
                DeleteAccessToken(key);
            }

            foreach (var key in GetAllRefreshTokenKeys())
            {
                DeleteRefreshToken(key);
            }
        }
    }
}
