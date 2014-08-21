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

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    /// <summary>
    /// Indicates whether AcquireToken should automatically prompt only if necessary or whether
    /// it should prompt regardless of whether there is a cached token.
    /// </summary>
#if ADAL_WINPHONE
    internal enum PromptBehavior
#else
    public enum PromptBehavior
#endif
    {
        /// <summary>
        /// Acquire token will prompt the user for credentials only when necessary.  If a token
        /// that meets the requirements is already cached then the user will not be prompted.
        /// </summary>
        Auto,

        /// <summary>
        /// The user will be prompted for credentials even if there is a token that meets the requirements
        /// already in the cache.
        /// </summary>
        Always,

        /// <summary>
        /// The user will not be prompted for credentials.  If prompting is necessary then the AcquireToken request
        /// will fail.
        /// </summary>
        Never,

        /// <summary>
        /// Re-authorizes (through displaying webview) the resource usage, making sure that the resulting access
        /// token contains updated claims. If user logon cookies are available, the user will not be asked for 
        /// credentials again and the logon dialog will dismiss automatically.
        /// </summary>
        RefreshSession
    }
}
