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
namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
#if SILVERLIGHT
    public static partial class AdalError
#else
    internal static partial class AdalError
#endif
    {
        /// <summary>
        /// Unauthorized user information access
        /// </summary>
        public const string UnauthorizedUserInformationAccess = "unauthorized_user_information_access";

        /// <summary>
        /// Cannot access user information
        /// </summary>
        public const string CannotAccessUserInformation = "user_information_access_failed";

        /// <summary>
        /// Need to set callback uri as local setting
        /// </summary>
        public const string NeedToSetCallbackUriAsLocalSetting = "need_to_set_callback_uri_as_local_setting";
    }

    internal static partial class AdalErrorMessage
    {
        public const string CannotAccessUserInformation = "Cannot access user information. Check machine's Privacy settings or initialize UserCredential with userId";
        public const string RedirectUriUnsupportedWithPromptBehaviorNever = "PromptBehavior.Never is supported in SSO mode only (null or application's callback URI as redirectUri)";
        public const string UnauthorizedUserInformationAccess = "Unauthorized accessing user information. Check application's 'Enterprise Authentication' capability";
        public const string NeedToSetCallbackUriAsLocalSetting = "You need to add the value of WebAuthenticationBroker.GetCurrentApplicationCallbackUri() to an application's local setting named CurrentApplicationCallbackUri.";
    }

    internal static class Constant
    {
        public const string MsAppScheme = "ms-app";
        public static readonly Uri SsoPlaceHolderUri = new Uri("https://sso");
    }
}
