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
using Foundation;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Cache;
using Microsoft.Identity.Client.Internal.Interfaces;
using Security;

namespace Microsoft.Identity.Client
{
    internal class TokenCachePlugin : ITokenCachePlugin
    {
        private const string LocalSettingsContainerName = "MicrosoftAuthenticationLibrary";

        public void BeforeAccess(TokenCacheNotificationArgs args)
        {
            try
            {
                SecStatusCode res;
                var rec = new SecRecord(SecKind.GenericPassword)
                {
                    Generic = NSData.FromString(LocalSettingsContainerName),
                    Accessible = SecAccessible.Always,
                    Service = "MSAL.PCL.iOS Service",
                    Account = "MSAL.PCL.iOS cache",
                    Label = "MSAL.PCL.iOS Label",
                    Comment = "MSAL.PCL.iOS Cache",
                    Description = "Storage for cache"
                };

                var match = SecKeyChain.QueryAsRecord(rec, out res);
                if (res == SecStatusCode.Success && match != null && match.ValueData != null)
                {
                    byte[] dataBytes = match.ValueData.ToArray();
                    if (dataBytes != null)
                    {
                       // args.TokenCache.Deserialize(dataBytes);
                    }
                }
            }
            catch (Exception ex)
            {
                PlatformPlugin.Logger.Warning(null, "Failed to load cache: " + ex);
                // Ignore as the cache seems to be corrupt
            }
        }

        public void AfterAccess(TokenCacheNotificationArgs args)
        {
            if (args.TokenCache.HasStateChanged)
            {
                try
                {
                    var s = new SecRecord(SecKind.GenericPassword)
                    {
                        Generic = NSData.FromString(LocalSettingsContainerName),
                        Accessible = SecAccessible.Always,
                        Service = "MSAL.PCL.iOS Service",
                        Account = "MSAL.PCL.iOS cache",
                        Label = "MSAL.PCL.iOS Label",
                        Comment = "MSAL.PCL.iOS Cache",
                        Description = "Storage for cache"
                    };

                    var err = SecKeyChain.Remove(s);

                       // s.ValueData = NSData.FromArray(args.TokenCache.Serialize());
                        err = SecKeyChain.Add(s);
                    

                    args.TokenCache.HasStateChanged = false;
                }
                catch (Exception ex)
                {
                    PlatformPlugin.Logger.Warning(null, "Failed to save cache: " + ex);
                }
            }
        }

        public ICollection<string> GetAllAccessTokens()
        {
            throw new NotImplementedException();
        }

        public ICollection<string> AllRefreshTokens()
        {
            throw new NotImplementedException();
        }

        public void SaveAccessToken(string cacheKey, string accessTokenItem)
        {
            throw new NotImplementedException();
        }

        public void SaveRefreshToken(string cacheKey, string refreshTokenItem)
        {
            throw new NotImplementedException();
        }

        public void DeleteAccessToken(string cacheKey)
        {
            throw new NotImplementedException();
        }

        public void DeleteRefreshToken(string cacheKey)
        {
            throw new NotImplementedException();
        }
    }
}