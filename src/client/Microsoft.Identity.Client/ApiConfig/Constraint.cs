// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.ApiConfig
{
    /// <summary>
    /// Delagated Constraint
    /// </summary>
    public class Constraint
    {
        /// <summary>
        /// Specifies the type of constraint
        /// </summary>
        public string ConstraintType { get; set; }

        /// <summary>
        /// specifies the constraint value
        /// </summary>
        public IEnumerable<string> ConstraintValues { get; set; }
    }
}
