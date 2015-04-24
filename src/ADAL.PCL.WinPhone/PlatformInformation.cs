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

using System;
using System.Threading.Tasks;

using Windows.Security.Authentication.Web;
using Windows.Storage;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal class PlatformInformation : PlatformInformationBase
    {
        public override string GetProductName()
        {
            return "PCL.WinPhone";
        }

        public override string GetEnvironmentVariable(string variable)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            return localSettings.Values.ContainsKey(variable) ? localSettings.Values[variable].ToString() : null;
        }

        public override Task<string> GetUserPrincipalNameAsync()
        {
            return null;   // TODO: Fix this. Not return null.
        }

        public override string GetProcessorArchitecture()
        {
            return null;
        }

        public override string GetOperatingSystem()
        {
            return null;
        }

        public override string GetDeviceModel()
        {
            return null;
        }

        public override Uri ValidateRedirectUri(Uri redirectUri, CallState callState)
        {
            if (redirectUri == null)
            {
                redirectUri = Constant.SsoPlaceHolderUri;
                PlatformPlugin.Logger.Verbose(callState, "ms-app redirect Uri is used");
            }

            return redirectUri;
        }

        public override string GetRedirectUriAsString(Uri redirectUri, CallState callState)
        {
            string redirectUriString;

            if (ReferenceEquals(redirectUri, Constant.SsoPlaceHolderUri))
            {
                try
                {
                    redirectUriString = WebAuthenticationBroker.GetCurrentApplicationCallbackUri().OriginalString;
                }
                catch (FormatException ex)
                {
                    // This is the workaround for a bug in managed Uri class of WinPhone SDK which makes it throw UriFormatException when it gets called from unmanaged code. 
                    const string CurrentApplicationCallbackUriSetting = "CurrentApplicationCallbackUri";
                    if (ApplicationData.Current.LocalSettings.Values.ContainsKey(CurrentApplicationCallbackUriSetting))
                    {
                        redirectUriString = (string)ApplicationData.Current.LocalSettings.Values[CurrentApplicationCallbackUriSetting];
                    }
                    else
                    {
                        throw new AdalException(AdalErrorEx.NeedToSetCallbackUriAsLocalSetting, AdalErrorMessageEx.NeedToSetCallbackUriAsLocalSetting, ex);
                    }
                }
            }
            else
            {
                redirectUriString = redirectUri.OriginalString;
            }

            return redirectUriString;
        }
    }
}
