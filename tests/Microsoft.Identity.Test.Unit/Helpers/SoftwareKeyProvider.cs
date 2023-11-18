using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Microsoft.Identity.Test.Common.Core.Helpers
{
    /// <summary>
    /// Creates a software key in the Microsoft Software Key Storage Provider
    /// </summary>
    public class SoftwareKeyProvider : IDisposable
    {
        private readonly object _lockObject = new object();
        private readonly CngKey _createdKey;

        public SoftwareKeyProvider()
        {
            try
            {
                // Use a lock to ensure thread safety during key creation
                lock (_lockObject)
                {
                    // Specify the key parameters
                    string keyName = "ResourceBindingKey";
                    string providerName = "Microsoft Software Key Storage Provider";

                    // Check if the key already exists
                    if (CngKey.Exists(keyName, new CngProvider(providerName)))
                    {
                        Debug.WriteLine($"Key with name '{keyName}' already exists. Reusing existing key.");
                        return; // Return early if the key already exists
                    }

                    CngKeyCreationParameters keyParams = new CngKeyCreationParameters
                    {
                        KeyUsage = CngKeyUsages.AllUsages,
                        Provider = new CngProvider(providerName),
                        ExportPolicy = CngExportPolicies.AllowPlaintextExport,
                        KeyCreationOptions = CngKeyCreationOptions.None | CngKeyCreationOptions.OverwriteExistingKey,
                    };

                    // Create the key
                    _createdKey = CngKey.Create(CngAlgorithm.ECDsaP256, keyName, keyParams);

                    // Add a 10-second delay
                    Task.Delay(10000).Wait();

                    Debug.WriteLine("Key created successfully!");
                }
            }
            catch (CryptographicException ex)
            {
                Debug.WriteLine($"Error creating key: {ex.Message}");
                // Handle the exception as needed
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating key: {ex.Message}");
                // Handle the exception as needed
            }
        }

        public void Dispose()
        {
            // Use a lock to ensure thread safety during key deletion
            lock (_lockObject)
            {
                // Delete the key in the dispose method
                _createdKey?.Delete();
                Debug.WriteLine($"Deleted the key.");
            }
        }
    }
}
