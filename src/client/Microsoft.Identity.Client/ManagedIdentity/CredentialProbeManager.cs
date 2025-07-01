// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Http.Retry;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    internal class CredentialProbeManager
    {
        private const string CredentialEndpoint = "http://169.254.169.254/metadata/identity/issuecredential";
        private const string ProbeBody = ".";
        private const string ImdsHeader = "IMDS/";
        private readonly ILoggerAdapter _logger;
        private readonly RequestContext _requestContext;
        private static readonly Regex s_serverRegex = new(@"^IMDS/\d+\.\d+\.\d+\.(\d+)$", RegexOptions.Compiled);

        public CredentialProbeManager(RequestContext requestContext)
        {
            _requestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));
            _logger = _requestContext.Logger;
        }

        public async Task<bool> ExecuteProbeAsync(CancellationToken cancellationToken = default)
        {
            _logger.Info("[Probe] Initiating probe to IMDS credential endpoint.");

            var request = new ManagedIdentityRequest(HttpMethod.Post, new Uri($"{CredentialEndpoint}?cred-api-version=1.0"))
            {
                Content = ProbeBody,
            };

            request.Headers.Add("Metadata", "true");

            HttpContent httpContent = request.CreateHttpContent();

            _logger.Info($"[Probe] Sending request to {CredentialEndpoint}");
            _logger.Verbose(() => $"[Probe] Request Headers: {string.Join(", ", request.Headers)}");
            _logger.Verbose(() => $"[Probe] Request Body: {ProbeBody}");

            try
            {
                IRetryPolicyFactory retryPolicyFactory = _requestContext.ServiceBundle.Config.RetryPolicyFactory;
                IRetryPolicy retryPolicy = retryPolicyFactory.GetRetryPolicy(RequestType.ImdsProbe);

                HttpResponse response = await _requestContext.ServiceBundle.HttpManager.SendRequestAsync(
                    request.ComputeUri(),
                    request.Headers,
                    httpContent,
                    request.Method,
                    _logger,
                    doNotThrow: true,
                    mtlsCertificate: null,
                    validateServerCertificate: null,
                    cancellationToken, 
                    retryPolicy).ConfigureAwait(false);

                LogResponseDetails(response);

                return EvaluateProbeResponse(response);
            }
            catch (Exception ex)
            {
                _logger.Warning($"[Probe] Exception during probe: {ex.Message}");
                _logger.Warning($"[Probe] Stack Trace: {ex.StackTrace}");
                return false;
            }
        }

        private void LogResponseDetails(HttpResponse response)
        {
            if (response == null)
            {
                _logger.Warning("[Probe] No response received from the server.");
                return;
            }

            _logger.Info($"[Probe] Response Status Code: {response.StatusCode}");
            _logger.Verbose(() => $"[Probe] Response Headers: {string.Join(", ", response.HeadersAsDictionary)}");

            if (response.Body != null)
            {
                _logger.Verbose(() => $"[Probe] Response Body: {response.Body}");
            }
        }

        private bool EvaluateProbeResponse(HttpResponse response)
        {
            if (response == null)
            {
                _logger.Warning("[Probe] No response received from the server.");
                return false;
            }

            _logger.Info($"[Probe] Evaluating response. Status: {(int)response.StatusCode} {response.StatusCode}");

            // --- 1. Short-circuit on non-400 -------------------------------------------------
            if (response.StatusCode != HttpStatusCode.BadRequest)
            {
                _logger.Info($"[Probe] Rejected – status code is {response.StatusCode}, expected 400.");
                return false;
            }

            // --- 2. Must have Server header --------------------------------------------------
            if (!response.HeadersAsDictionary.TryGetValue("Server", out var serverHeader) ||
                !serverHeader.StartsWith(ImdsHeader, StringComparison.OrdinalIgnoreCase))
            {
                _logger.Info("[Probe] Rejected – missing or malformed Server header.");
                return false;
            }

            // --- 3. Extract build number and ensure it’s > 1324 ------------------------------
            var m = s_serverRegex.Match(serverHeader);
            if (!m.Success || !int.TryParse(m.Groups[1].Value, out int build) || build <= 1324)
            {
                _logger.Info($"[Probe] IMDS build {m.Groups[1].Value} does not support Credential source.");
                return false;
            }

            _logger.Info($"[Probe] Credential endpoint supported – IMDS build {build}.");
            return true;
        }
    }
}
