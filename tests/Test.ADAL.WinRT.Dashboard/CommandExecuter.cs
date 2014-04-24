//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Test.ADAL.Common;

namespace Test.ADAL.WinRT.Dashboard
{
    static class CommandExecuter
    {
        static AuthenticationContext context;

        public static async Task<AuthenticationResultProxy> ExecuteAsync(CommandProxy proxy)
        {
            AuthenticationResultProxy resultProxy = null;
            AuthenticationResult result = null;

            foreach (var command in proxy.Commands)
            {
                var arg = command.Arguments;
                switch (command.CommandType)
                {
                    case CommandType.ClearDefaultTokenCache:
                    {
                        var dummyContext = new AuthenticationContext("https://dummy/dummy", false);
                        dummyContext.TokenCacheStore.Clear();
                        break;
                    }

                    case CommandType.SetEnvironmentVariable:
                    {
                        var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                        localSettings.Values[arg.EnvironmentVariable] = arg.EnvironmentVariableValue;

                        break;
                    }

                    case CommandType.SetCorrelationId:
                    {
                        context.CorrelationId = arg.CorrelationId;
                        break;
                    }

                    case CommandType.CreateContextA:
                    {
                        context = new AuthenticationContext(arg.Authority);
                        break;
                    }

                    case CommandType.CreateContextAV:
                    {
                        context = new AuthenticationContext(arg.Authority, arg.ValidateAuthority);
                        break;
                    }

                    case CommandType.CreateContextAVC:
                    {
                        IDictionary<TokenCacheKey, string> tokenCacheStore = null;
                        if (arg.TokenCacheStoreType == TokenCacheStoreType.InMemory)
                        {
                            tokenCacheStore = new Dictionary<TokenCacheKey, string>();
                        }
                        else if (arg.TokenCacheStoreType == TokenCacheStoreType.ShortLived)
                        {
                            tokenCacheStore = new ShortLivedTokenCache();
                        }

                        context = new AuthenticationContext(arg.Authority, arg.ValidateAuthority, tokenCacheStore);
                        break;
                    }

                    case CommandType.ClearDefaultToSSOMode:
                    {
                        //context.DefaultToSSOMode = false;
                        break;
                    }

                    case CommandType.SetDefaultToSSOMode:
                    {
                        //context.DefaultToSSOMode = true;
                        break;
                    }

                    case CommandType.ClearUseCorporateNetwork:
                    {
                        //context.UseCorporateNetwork = false;
                        break;
                    }

                    case CommandType.SetUseCorporateNetwork:
                    {
                        //context.UseCorporateNetwork = true;
                        break;
                    }

                    case CommandType.AquireTokenAsyncRC:
                    {
                        result = await context.AcquireTokenAsync(arg.Resource, arg.ClientId);
                        break;
                    }

// Disabled Non-Interactive Feature
#if false
                    case CommandType.AquireTokenAsyncRCUP:
                    {
                        UserCredential credential;

                        if (arg.Password != null)
                        {
                            credential = new UserCredential(arg.UserId, arg.Password);
                        }
                        else
                        {
                            credential = new UserCredential(arg.UserId);
                        }

                        result = await context.AcquireTokenAsync(arg.Resource, arg.ClientId, credential);
                        break;
                    }
#endif

                    case CommandType.AquireTokenAsyncRCR:
                    {
                        result = await context.AcquireTokenAsync(arg.Resource, arg.ClientId, arg.RedirectUri);
                        break;
                    }

                    case CommandType.AquireTokenAsyncRCRU:
                    {
                        result = await context.AcquireTokenAsync(arg.Resource, arg.ClientId, arg.RedirectUri, arg.UserId);
                        break;
                    }

                    case CommandType.AquireTokenAsyncRCRUX:
                    {
                        result = await context.AcquireTokenAsync(arg.Resource, arg.ClientId, arg.RedirectUri, arg.UserId, arg.Extra);
                        break;
                    }

                    case CommandType.AquireTokenAsyncRCRP:
                    {
                        result = await context.AcquireTokenAsync(arg.Resource, arg.ClientId, arg.RedirectUri, (arg.PromptBehavior == PromptBehaviorProxy.Always) ? PromptBehavior.Always : PromptBehavior.Auto);
                        break;
                    }

                    case CommandType.AquireTokenAsyncRCRPU:
                    {
                        result = await context.AcquireTokenAsync(arg.Resource, arg.ClientId, arg.RedirectUri, (arg.PromptBehavior == PromptBehaviorProxy.Always) ? PromptBehavior.Always : PromptBehavior.Auto, arg.UserId);
                        break;
                    }

                    case CommandType.AcquireTokenByRefreshTokenAsyncRC:
                    {
                        result = await context.AcquireTokenByRefreshTokenAsync(arg.RefreshToken, arg.ClientId);
                        break;
                    }

                    case CommandType.AcquireTokenByRefreshTokenAsyncRCR:
                    {
                        result = await context.AcquireTokenByRefreshTokenAsync(arg.RefreshToken, arg.ClientId, arg.Resource);
                        break;
                    }

                    case CommandType.CreateFromResourceUrlAsync:
                    {
                        var parameters = await AuthenticationParameters.CreateFromResourceUrlAsync(new Uri(arg.Extra));
                        resultProxy = new AuthenticationResultProxy
                                 {
                                     AuthenticationParametersAuthority = parameters.Authority,
                                     AuthenticationParametersResource = parameters.Resource
                                 };
                        break;
                    }

                    case CommandType.CreateFromResponseAuthenticateHeader:
                    {
                        var parameters = AuthenticationParameters.CreateFromResponseAuthenticateHeader(arg.Extra);
                        resultProxy = new AuthenticationResultProxy
                        {
                            AuthenticationParametersAuthority = parameters.Authority,
                            AuthenticationParametersResource = parameters.Resource
                        };
                        break;
                    }

                    /*case CommandType.AcquireTokenByRefreshTokenAsyncRCC:
                    {
                        result = await context.AcquireTokenByRefreshTokenAsync(arg.RefreshToken, arg.ClientId,
                            (arg.ClientId != null && arg.ClientSecret != null) ? new ClientCredential(arg.ClientId, arg.ClientSecret) : null);
                        break;
                    }*/

                    default:
                        throw new Exception("Unknown command");
                }
            }

            return resultProxy ?? 
                       new AuthenticationResultProxy
                       {
                           AccessToken = result.AccessToken,
                           AccessTokenType = result.AccessTokenType,
                           ExpiresOn = result.ExpiresOn,
                           IsMultipleResourceRefreshToken =
                               result.IsMultipleResourceRefreshToken,
                           RefreshToken = result.RefreshToken,
                           TenantId = result.TenantId,
                           UserInfo =
                               (result.UserInfo != null)
                                   ? new UserInfoProxy
                                     {
                                         FamilyName =
                                             result.UserInfo.FamilyName,
                                         GivenName =
                                             result.UserInfo.GivenName,
                                         IdentityProvider =
                                             result.UserInfo
                                             .IdentityProvider,
                                         IsUserIdDisplayable =
                                             result.UserInfo
                                             .IsUserIdDisplayable,
                                         UserId = result.UserInfo.UserId
                                     }
                                   : null,
                           Error = result.Error,
                           ErrorDescription = result.ErrorDescription,
                           Status =
                               (result.Status == AuthenticationStatus.Succeeded)
                                   ? AuthenticationStatusProxy.Succeeded
                                   : AuthenticationStatusProxy.Failed
                       };
        }
    }
}
