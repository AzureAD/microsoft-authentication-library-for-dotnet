using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Component to be used with confidential client applications like Web Apps/API.
    /// This component supports Subject Name + Issuer authentication in order to help, in the future,
    /// Azure AD certificates rollover
    /// </summary>
    public interface IConfidentialClientApplicationWithCertificate
    {
        /// <summary>
        /// Acquires token from the service for the confidential client using the client credentials flow. (See https://aka.ms/msal-net-client-credentials)
        /// This method enables application developers to achieve easy certificates roll-over
        /// in Azure AD: this method will send the public certificate to Azure AD
        /// along with the token request, so that Azure AD can use it to validate the subject name based on a trusted issuer policy.
        /// This saves the application admin from the need to explicitly manage the certificate rollover
        /// (either via portal or powershell/CLI operation)
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <returns>Authentication result containing application token for the requested scopes</returns>
        Task<AuthenticationResult> AcquireTokenForClientWithCertificateAsync(IEnumerable<string> scopes);

        /// <summary>
        /// Acquires token from the service for the confidential client using the client credentials flow. (See https://aka.ms/msal-net-client-credentials)
        /// This method attempts to look up valid access token in the cache unless<paramref name="forceRefresh"/> is true
        /// This method enables application developers to achieve easy certificates roll-over
        /// in Azure AD: this method will send the public certificate to Azure AD
        /// along with the token request, so that Azure AD can use it to validate the subject name based on a trusted issuer policy.
        /// This saves the application admin from the need to explicitly manage the certificate rollover
        /// (either via portal or powershell/CLI operation)
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <param name="forceRefresh">If TRUE, API will ignore the access token in the cache and attempt to acquire new access token using client credentials</param>
        /// <returns>Authentication result containing application token for the requested scopes</returns>
        Task<AuthenticationResult> AcquireTokenForClientWithCertificateAsync(IEnumerable<string> scopes, bool forceRefresh);

        /// <summary>
        /// Acquires token using On-Behalf-Of flow. (See https://aka.ms/msal-net-on-behalf-of)
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <param name="userAssertion">Instance of UserAssertion containing user's token.</param>
        /// <returns>Authentication result containing token of the user for the requested scopes</returns>
        Task<AuthenticationResult> AcquireTokenOnBehalfOfWithCertificateAsync(IEnumerable<string> scopes, UserAssertion userAssertion);

        /// <summary>
        /// Acquires token using On-Behalf-Of flow. (See https://aka.ms/msal-net-on-behalf-of)
        /// </summary>
        /// <param name="scopes">Array of scopes requested for resource</param>
        /// <param name="userAssertion">Instance of UserAssertion containing user's token.</param>
        /// <param name="authority">Specific authority for which the token is requested. Passing a different value than configured does not change the configured value</param>
        /// <returns>Authentication result containing token of the user for the requested scopes</returns>
        Task<AuthenticationResult> AcquireTokenOnBehalfOfWithCertificateAsync(IEnumerable<string> scopes, UserAssertion userAssertion, string authority);
    }
}
