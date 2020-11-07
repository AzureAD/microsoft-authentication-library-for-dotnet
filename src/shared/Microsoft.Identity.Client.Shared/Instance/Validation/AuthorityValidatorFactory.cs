// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Instance.Validation
{
    internal static class AuthorityValidatorFactory
    {
        public static IAuthorityValidator Create(AuthorityInfo authorityInfo, IServiceBundle serviceBundle)
        {
            switch (authorityInfo.AuthorityType)
            {
                case AuthorityType.Adfs:
                    return new AdfsAuthorityValidator(serviceBundle);
                case AuthorityType.Aad:
                    return new AadAuthorityValidator(serviceBundle);
                case AuthorityType.B2C:
                    return new B2CAuthorityValidator();
                default:
                    throw new InvalidOperationException("Invalid AuthorityType");
            }
        }
    }
}
