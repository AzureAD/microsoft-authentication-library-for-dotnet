// ------------------------------------------------------------------------------
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
// ------------------------------------------------------------------------------

namespace Microsoft.Identity.Client.AppConfig
{
    /*
     previous PR comments:
     Given that our current default authority is common, I think it should be AAD + MSA
     Don't we want to use a [Flags] enum (AAD = 1, MSA =2, AAD+MSA = 3) if we have the notion of Default?

     For the naming of the enumeration constants, I propose that we align with the signInAudience of the https://docs.microsoft.com/en-us/azure/active-directory/develop/reference-app-manifest#manifest-reference that is:

     Constant | description
     ---------- | -------------
     AzureADMyOrg | Users with a Microsoft work or school account in my organization’s Azure AD tenant (i.e. single tenant)
     AzureADMultipleOrgs |  Users with a Microsoft work or school account in any organization’s Azure AD tenant (i.e. multi-tenant)
     AzureADandPersonalMicrosoftAccount |  Users with a personal Microsoft account, or a work or school account in any organization’s Azure AD tenant.

     Maps to instance/common (for instance https://login.microsoftonline.com/common)
     the reason is instance can be something else (think of national / sovereign clouds, or even B2C)
    */

    /// <summary>
    /// </summary>
    public enum AadAuthorityAudience
    {
        /// <summary>
        /// </summary>
        None,

        /// <summary>
        ///     Default is AzureAdOnly ?? TODO: WHAT SHOULD THE DEFAULT BE
        /// </summary>
        Default,

        /// <summary>
        /// Maps to https://[instance]/[tenantId]
        /// </summary>
        AzureAdSpecificDirectoryOnly,

        /// <summary>
        ///     Maps to https://[instance]/common/
        /// </summary>
        AzureAdAndPersonalMicrosoftAccount,

        /// <summary>
        ///     Maps to https://[instance]/organizations/
        /// </summary>
        AzureAdOnly,

        /// <summary>
        ///     Maps to https://[instance]/consumers/
        /// </summary>
        MicrosoftAccountOnly
    }
}