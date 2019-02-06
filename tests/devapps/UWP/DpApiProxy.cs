using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.DataProtection;
using Windows.Storage.Streams;

namespace UWP
{
    public class DpApiProxy
    {
        public static async Task<IBuffer> SampleProtectAsync(
           byte[] blobToProtect,
           String strDescriptor)
        {
            // Create a DataProtectionProvider object for the specified descriptor.
            DataProtectionProvider provider = new DataProtectionProvider(strDescriptor);

            // Encode the plaintext input message to a buffer.
            IBuffer buffMsg = CryptographicBuffer.CreateFromByteArray(blobToProtect);

            // Encrypt the message.
            IBuffer buffProtected = await provider.ProtectAsync(buffMsg).AsTask().ConfigureAwait(false);

            // Execution of the SampleProtectAsync function resumes here
            // after the awaited task (Provider.ProtectAsync) completes.
            return buffProtected;
        }

        public static async Task<byte[]> SampleUnprotectDataAsync(
            IBuffer buffProtected)
        {
            // Create a DataProtectionProvider object.
            DataProtectionProvider provider = new DataProtectionProvider();

            // Decrypt the protected message specified on input.
            IBuffer buffUnprotected = await provider.UnprotectAsync(buffProtected).AsTask().ConfigureAwait(false);

            // Execution of the SampleUnprotectData method resumes here
            // after the awaited task (Provider.UnprotectAsync) completes
            // Convert the unprotected message from an IBuffer object to a string.
            CryptographicBuffer.CopyToByteArray(buffUnprotected,  out byte[] blob);

            // Return the plaintext string.
            return blob;
        }
    }
}
