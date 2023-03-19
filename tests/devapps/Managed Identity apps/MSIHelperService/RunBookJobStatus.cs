// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace MSIHelperService
{
    /// <summary>
    /// RunBookJobStatus
    /// </summary>
    public class RunBookJobStatus
    {
        /// <summary>
        /// Id
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// Name
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Type
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// Properties
        /// </summary>
        public Properties? Properties { get; set; }
    }

    /// <summary>
    /// Properties
    /// </summary>
    public class Properties
    {
        /// <summary>
        /// JobId
        /// </summary>
        public string? JobId { get; set; }

        /// <summary>
        /// Runbook
        /// </summary>
        public Runbook? Runbook { get; set; }

        /// <summary>
        /// ProvisioningState
        /// </summary>
        public string? ProvisioningState { get; set; }

        /// <summary>
        /// CreationTime
        /// </summary>
        public DateTime? CreationTime { get; set; }

        /// <summary>
        /// EndTime
        /// </summary>
        public object? EndTime { get; set; }

        /// <summary>
        /// Exception
        /// </summary>
        public object? Exception { get; set; }

        /// <summary>
        /// LastModifiedTime
        /// </summary>
        public DateTime? LastModifiedTime { get; set; }

        /// <summary>
        /// LastStatusModifiedTime
        /// </summary>
        public DateTime? LastStatusModifiedTime { get; set; }

        /// <summary>
        /// StartTime
        /// </summary>
        public object? StartTime { get; set; }

        /// <summary>
        /// Status
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// StatusDetails
        /// </summary>
        public string? StatusDetails { get; set; }

        /// <summary>
        /// Parameters
        /// </summary>
        public Parameters? Parameters { get; set; }

        /// <summary>
        /// RunOn
        /// </summary>
        public string? RunOn { get; set; }
    }

    /// <summary>
    /// Parameters
    /// </summary>
    public class Parameters
    {
        /// <summary>
        /// Tag01
        /// </summary>
        public string? Tag01 { get; set; }

        /// <summary>
        /// Tag02
        /// </summary>
        public string? Tag02 { get; set; }
    }
        
    /// <summary>
    /// Runbook
    /// </summary>
    public class Runbook
    {
        /// <summary>
        /// Name
        /// </summary>
        public string? Name { get; set; }
    }
}
