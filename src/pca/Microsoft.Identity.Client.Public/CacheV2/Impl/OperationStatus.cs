// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.CacheV2.Impl
{
    internal class OperationStatus
    {
        public OperationStatusType StatusType { get; set; }
        public int Code { get; set; }
        public string StatusDescription { get; set; }
        public long PlatformCode { get; set; }
        public string PlatformDomain { get; set; }

        public static OperationStatus CreateSuccess()
        {
            return new OperationStatus
            {
                StatusType = OperationStatusType.Success
            };
        }
    }
}
