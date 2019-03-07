// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.AppConfig
{
    /// <summary>
    /// 
    /// </summary>
    public interface IMatsConfig
    {
        /// <summary>
        /// 
        /// </summary>
        string AppName { get; }

        /// <summary>
        /// 
        /// </summary>
        string AppVer { get; }

        /// <summary>
        /// 
        /// </summary>
        string SessionId { get; }
    }
}
