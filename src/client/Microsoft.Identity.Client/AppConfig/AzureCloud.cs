// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// </summary>
    public enum AzureCloudInstance
    {
        /// <summary>
        /// Value communicating that the AzureCloudInstance is not specified.
        /// </summary>
        None,

        /// <summary>
        /// Microsoft Azure public cloud. Maps to https://login.microsoftonline.com
        /// </summary>
        AzurePublic,

        /// <summary>
        /// Microsoft Azure China cloud. Maps to https://login.chinacloudapi.cn
        /// </summary>
        AzureChina,

        /// <summary>
        /// Microsoft Azure German cloud ("Black Forest"). Maps to https://login.microsoftonline.de
        /// </summary>
        AzureGermany,

        /// <summary>
        /// US Government cloud. Maps to https://login.microsoftonline.us
        /// </summary>
        AzureUsGovernment,
    };
}
