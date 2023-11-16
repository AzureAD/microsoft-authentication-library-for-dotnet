// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Security.Cryptography;

namespace Microsoft.Identity.Test.Common.Core.Helpers
{
    /// <summary>
    /// Creates a software key in the Microsoft Software Key Storage Provider
    /// </summary>
    public class SoftwareKeyProvider : IDisposable
    {
        private readonly CngKey _createdKey;

        public SoftwareKeyProvider()
        {
            try
            {
                // Specify the key parameters
                CngKeyCreationParameters keyParams = new CngKeyCreationParameters
                {
                    KeyUsage = CngKeyUsages.AllUsages,
                    Provider = new CngProvider("Microsoft Software Key Storage Provider"), // Specify the Microsoft KSP
                    ExportPolicy = CngExportPolicies.AllowPlaintextExport,
                    KeyCreationOptions = CngKeyCreationOptions.None | CngKeyCreationOptions.OverwriteExistingKey, // Optional: Specify MachineKey or None
                };

                // Create the key
                _createdKey = CngKey.Create(CngAlgorithm.ECDsaP256, "ResourceBindingKey", keyParams);

                Debug.WriteLine("Key created successfully!");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating key: {ex.Message}");
            }
        }

        public void Dispose()
        {
            // Delete the key in the dispose method
            _createdKey?.Delete();
        }
    }
}
