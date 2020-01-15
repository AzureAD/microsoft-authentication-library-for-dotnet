// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.WsTrust
{
    /// <summary>
    ///
    /// </summary>
    internal interface IWsTrustWebRequestManager
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="federationMetadataUrl"></param>
        /// <param name="requestContext"></param>
        /// <returns></returns>
        Task<MexDocument> GetMexDocumentAsync(string federationMetadataUrl, RequestContext requestContext);

        /// <summary>
        ///
        /// </summary>
        /// <param name="wsTrustEndpoint"></param>
        /// <param name="wsTrustRequest"></param>
        /// <param name="requestContext"></param>
        /// <returns></returns>
        Task<WsTrustResponse> GetWsTrustResponseAsync(
            WsTrustEndpoint wsTrustEndpoint,
            string wsTrustRequest,
            RequestContext requestContext);

        /// <summary>
        ///
        /// </summary>
        /// <param name="userRealmUriPrefix"></param>
        /// <param name="userName"></param>
        /// <param name="requestContext"></param>
        /// <returns></returns>
        Task<UserRealmDiscoveryResponse> GetUserRealmAsync(
            string userRealmUriPrefix,
            string userName,
            RequestContext requestContext);
    }
}
