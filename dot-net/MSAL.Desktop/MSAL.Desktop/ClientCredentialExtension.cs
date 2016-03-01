
using System.Security.Cryptography.X509Certificates;

namespace MSAL
{
    public static class ClientCredentialExtension
    {

        public static ClientCredential CreateClientCredential(this ClientCredential cc, X509Certificate2 certificate)
        {
            //cc.
            return null;
        }

        internal static byte[] Sign(this ClientCredential cc, string message)
        {
           // new ClientCredential().
            return null;
        }
    }
}
