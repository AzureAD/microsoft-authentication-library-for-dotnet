// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.Instance.Validation
{
    internal class B2CAuthorityValidator : IAuthorityValidator
    {
        /// <inheritdoc />
        public Task ValidateAuthorityAsync(
            AuthorityInfo authorityInfo,
            RequestContext requestContext)
        {
            return Task.FromResult(0);
        }
    }
}
