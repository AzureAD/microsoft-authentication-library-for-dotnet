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
        private string Secret { get; set; }

        public ClientCredential(string secret, ClientCredentialType clientCredentialType)
        {
            
        }

        internal IDictionary<string, string> ToParameters()
        {
            return null;
        }

        //public static ClientCredential()

    }
}
