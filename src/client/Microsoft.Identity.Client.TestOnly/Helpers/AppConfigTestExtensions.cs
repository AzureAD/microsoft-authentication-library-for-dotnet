// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal.Logger;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.PlatformsCommon.Shared;

namespace Microsoft.Identity.Test.Common.Core.Helpers
{
    internal static class AppConfigTestExtensions
    {
        public static T WithCachePartitioningAsserts<T>(
            this AbstractApplicationBuilder<T> builder,
            IPlatformProxy platformProxy)
            where T : AbstractApplicationBuilder<T>
        {
            (platformProxy as AbstractPlatformProxy).UserTokenCacheAccessorForTest =
                   new UserAccessorWithPartitionAsserts(new NullLogger(), null);

            (platformProxy as AbstractPlatformProxy).AppTokenCacheAccessorForTest =
                  new AppAccessorWithPartitionAsserts(new NullLogger(), null);

            builder.Config.PlatformProxy = platformProxy;
            return (T)builder;
        }
    }
}
