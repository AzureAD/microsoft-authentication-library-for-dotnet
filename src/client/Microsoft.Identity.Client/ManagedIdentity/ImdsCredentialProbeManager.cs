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
    internal class ImdsCredentialProbeManager : IProbe
    {
        private const string CredentialEndpoint = "http://169.254.169.254/metadata/identity/credential";
        private const string ProbeBody = ".";
        private const string ImdsHeader = "IMDS/";
        private static readonly SemaphoreSlim s_lock = new(1);
        private static ProbeResult s_cachedResult;

        private readonly IHttpManager _httpManager;
        private readonly ILoggerAdapter _logger;

        public ImdsCredentialProbeManager(IHttpManager httpManager, ILoggerAdapter logger)
        {
            _httpManager = httpManager ?? throw new ArgumentNullException(nameof(httpManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ProbeResult> ExecuteProbeAsync(CancellationToken cancellationToken = default)
        {
            if (s_cachedResult != null)
            {
                _logger.Info("[Probe] Using cached probe result.");
                return s_cachedResult;
            }

            await s_lock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (s_cachedResult != null)
                {
                    return s_cachedResult;
                }

                _logger.Info("[Probe] Initiating probe to IMDS credential endpoint.");

                var request = new ManagedIdentityRequest(HttpMethod.Post, new Uri($"{CredentialEndpoint}?cred-api-version=1.0"))
                {
                    Content = ProbeBody
                };
                HttpContent httpContent = request.CreateHttpContent();

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

                s_cachedResult = EvaluateProbeResponse(response);
                return s_cachedResult;
            }
            catch (Exception ex)
            {
                _logger.Error($"[Probe] Exception during probe: {ex.Message}");
                s_cachedResult = ProbeResult.Failure(ex.Message);
                return s_cachedResult;
            }
            finally
            {
                s_lock.Release();
            }
        }

        private ProbeResult EvaluateProbeResponse(HttpResponse response)
        {
            if (response.StatusCode == HttpStatusCode.BadRequest &&
                response.HeadersAsDictionary.TryGetValue("Server", out string serverHeader) &&
                serverHeader.StartsWith(ImdsHeader, StringComparison.OrdinalIgnoreCase))
            {
                _logger.Info($"[Probe] Credential endpoint supported. Server: {serverHeader}");
                return ProbeResult.Success();
            }

            _logger.Verbose(() => "[Probe] Credential endpoint not supported.");
            return ProbeResult.Failure("Credential endpoint not supported.");
        }
    }

    internal class ProbeResult
    {
        public bool IsSuccess { get; }
        public string Message { get; }

        private ProbeResult(bool isSuccess, string message)
        {
            IsSuccess = isSuccess;
            Message = message;
        }

        public static ProbeResult Success() => new ProbeResult(true, "Credential endpoint is supported.");

        public static ProbeResult Failure(string message) => new ProbeResult(false, message);
    }
}
