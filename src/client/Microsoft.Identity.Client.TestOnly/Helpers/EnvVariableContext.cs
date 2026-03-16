// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Test.Common.Core.Helpers
{
    /// <summary>
    /// Captures the process env variables and resets the state to these variables on Dispose.
    /// </summary>
    public class EnvVariableContext : IDisposable
    {
        private readonly System.Collections.IDictionary _originalVariables;

        public EnvVariableContext()
        {
            _originalVariables = Environment.GetEnvironmentVariables();
        }

        public void Dispose()
        {
            var newVariables = Environment.GetEnvironmentVariables();

            foreach (var key in newVariables.Keys)
            {
                // delete new variables 
                if (!_originalVariables.Contains(key))
                {
                    Environment.SetEnvironmentVariable(key.ToString(), null);
                }
            }

            // restore variables to old values            
            foreach (var key in _originalVariables.Keys)
            {
                Environment.SetEnvironmentVariable(key.ToString(), _originalVariables[key].ToString());
            }
        }
    }
}
