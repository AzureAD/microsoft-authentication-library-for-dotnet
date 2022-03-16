// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Instance.Validation
{
    internal class B2CAuthorityValidator : IAuthorityValidator
    {
        /// <inheritdoc />
        public Task ValidateAuthorityAsync(
            AuthorityInfo authorityInfo)
        {
            return Task.FromResult(0);
        }
    }
}
