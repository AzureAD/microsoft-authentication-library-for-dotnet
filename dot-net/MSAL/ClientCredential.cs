using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSAL
{
    public enum ClientCredentialType
    {
        ClientSecret,
        ClientAssertion,
    }

    public class ClientCredential
    {
        private string Secret { get; private set; }

        public ClientCredential(string secret, ClientCredentialType clientCredentialType)
        {
            
        }

        internal abstract IDictionary<string, string> ToParameters();

        public static ClientCredential()

    }
}
