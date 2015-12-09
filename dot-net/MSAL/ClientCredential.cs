using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSAL
{
    public enum ClientCredentialType
    {
        ClientSecret, //client_secret
        ClientAssertion, //urn:ietf:params:oauth:client-assertion-type:jwt-bearer
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
    }
}
