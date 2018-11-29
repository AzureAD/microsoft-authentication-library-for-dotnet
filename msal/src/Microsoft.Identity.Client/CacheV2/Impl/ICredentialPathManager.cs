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

using Microsoft.Identity.Client.CacheV2.Schema;

namespace Microsoft.Identity.Client.CacheV2.Impl
{
    /// <summary>
    /// Interface providing mechanism to transform the unified schema types into their appropriate "path"
    /// or "key" for storage/retrieval.  For example, on Windows, this will be a relative file system path.
    /// But on iOS/macOS is will be a path to keychain storage.
    /// </summary>
    internal interface ICredentialPathManager
    {
        string GetCredentialPath(Credential credential);
        string ToSafeFilename(string data);

        string GetCredentialPath(
            string homeAccountId,
            string environment,
            string realm,
            string clientId,
            string familyId,
            CredentialType credentialType);

        string GetAppMetadataPath(string environment, string clientId);
        string GetAccountPath(Microsoft.Identity.Client.CacheV2.Schema.Account account);
        string GetAccountPath(string homeAccountId, string environment, string realm);
        string GetAppMetadataPath(AppMetadata appMetadata);
        string GetAccountsPath(string homeAccountId, string environment);
    }
}