// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MsalCdtExtension
{
    public class Constraint
    {
        public string Version { get; set; }
        public string Type { get; set; }
        public string Action { get; set; }
        public List<ConstraintTarget> Targets { get; set; }
        public Dictionary<string, object> AdditionalProperties { get; set; }
    }

    public class ConstraintTarget
    {
        public string Value { get; set; }
        public string Policy { get; set; }
        public Dictionary<string, object> AdditionalProperties { get; set; }
        public ConstraintTarget(string value, string policy)
        {
            Value = value;
            Policy = policy;
        }
    }
}
