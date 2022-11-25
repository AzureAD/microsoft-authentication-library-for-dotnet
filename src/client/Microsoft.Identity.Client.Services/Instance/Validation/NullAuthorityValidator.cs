// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Instance.Validation
{
    internal class NullAuthorityValidator : IAuthorityValidator
    {
        /// <inheritdoc />
        public Task ValidateAuthorityAsync(
            AuthorityInfo authorityInfo)
        {
            return Task.FromResult(0);
        }
    }
}
