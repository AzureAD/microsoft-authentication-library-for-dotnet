// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;

namespace Microsoft.Identity.Client.ManagedIdentity
{
    internal class ImdsCredentialProbeManager
    {
        private const string CredentialEndpoint = "http://169.254.169.254/metadata/identity/credential";
        private const string ProbeBody = ".";
        private const string ImdsHeader = "IMDS/";
        private readonly IHttpManager _httpManager;
        private readonly ILoggerAdapter _logger;

        public ImdsCredentialProbeManager(IHttpManager httpManager, ILoggerAdapter logger)
        {
            _httpManager = httpManager ?? throw new ArgumentNullException(nameof(httpManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> ExecuteProbeAsync(CancellationToken cancellationToken = default)
        {
            _logger.Info("[Probe] Initiating probe to IMDS credential endpoint.");

            var request = new ManagedIdentityRequest(HttpMethod.Post, new Uri($"{CredentialEndpoint}?cred-api-version=1.0"))
            {
                Content = ProbeBody
            };

            HttpContent httpContent = request.CreateHttpContent();

            _logger.Info($"[Probe] Sending request to {CredentialEndpoint}");
            _logger.Verbose(() => $"[Probe] Request Headers: {string.Join(", ", request.Headers)}");
            _logger.Verbose(() => $"[Probe] Request Body: {ProbeBody}");

            try
            {
                HttpResponse response = await _httpManager.SendRequestAsync(
                    request.ComputeUri(),
                    request.Headers,
                    httpContent,
                    request.Method,
                    _logger,
                    doNotThrow: true,
                    mtlsCertificate: null,
                    customHttpClient: null,
                    cancellationToken).ConfigureAwait(false);

                LogResponseDetails(response);

                return EvaluateProbeResponse(response);
            }
            catch (Exception ex)
            {
                _logger.Error($"[Probe] Exception during probe: {ex.Message}");
                _logger.Error($"[Probe] Stack Trace: {ex.StackTrace}");
                return false;
            }
        }

        private void LogResponseDetails(HttpResponse response)
        {
            if (response == null)
            {
                _logger.Error("[Probe] No response received from the server.");
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
                _logger.Error("[Probe] No response received from the server.");
                return false;
            }

            _logger.Info($"[Probe] Evaluating response from credential endpoint. Status Code: {response.StatusCode}");

            if (response.HeadersAsDictionary.TryGetValue("Server", out string serverHeader) &&
                serverHeader.StartsWith(ImdsHeader, StringComparison.OrdinalIgnoreCase))
            {
                _logger.Info($"[Probe] Credential endpoint supported. Server: {serverHeader}");
                return true;
            }

            _logger.Warning($"[Probe] Credential endpoint not supported. Status Code: {response.StatusCode}");
            return false;
        }
    }
}
