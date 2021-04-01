// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.OAuth2;

namespace Microsoft.Identity.Client.Platforms.netcore
{
    internal class NetCoreDeviceAuthManager : IDeviceAuthManager
    {
        public bool TryCreateDeviceAuthChallengeResponseAsync(HttpResponseHeaders responseHeaders, Uri endpointUri, out string responseHeader)
        {
            responseHeader = string.Empty;
            X509Certificate2 certificate = null;

            if (!DeviceAuthHelper.IsDeviceAuthChallenge(responseHeaders))
            {
                return false;
            }
            if (!DeviceAuthHelper.CanOSPerformPKeyAuth())
            {
                responseHeader = DeviceAuthHelper.GetBypassChallengeResponse(responseHeaders);
                return true;
            }

            IDictionary<string, string> challengeData = DeviceAuthHelper.ParseChallengeData(responseHeaders);

            if (!challengeData.ContainsKey("SubmitUrl"))
            {
                challengeData["SubmitUrl"] = endpointUri.AbsoluteUri;
            }

            try
            {
                certificate = DeviceAuthHelper.FindCertificate(challengeData);
            }
            catch (MsalException ex)
            {
                if (ex.ErrorCode == MsalError.DeviceCertificateNotFound)
                {
                    responseHeader = DeviceAuthHelper.GetBypassChallengeResponse(responseHeaders);
                    return true;
                }
            }

            DeviceAuthJWTResponse responseJWT = new DeviceAuthJWTResponse(challengeData["SubmitUrl"],
                challengeData["nonce"], Convert.ToBase64String(certificate.GetRawCertData()));

            byte[] signedResponse = new NetCoreCryptographyManager().SignWithCertificate(responseJWT.GetResponseToSign(), certificate);

            DeviceAuthHelper.FormatResponseHeader(responseJWT, signedResponse, challengeData, out responseHeader);

            return true;
        }
    }
}
