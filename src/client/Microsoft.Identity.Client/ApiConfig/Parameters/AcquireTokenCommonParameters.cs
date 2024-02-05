// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.AuthScheme;
using Microsoft.Identity.Client.AuthScheme.Bearer;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using static Microsoft.Identity.Client.Extensibility.AbstractConfidentialClientAcquireTokenParameterBuilderExtension;

namespace Microsoft.Identity.Client.ApiConfig.Parameters
{
    internal class AcquireTokenCommonParameters
    {
        public ApiEvent.ApiIds ApiId { get; set; } = ApiEvent.ApiIds.None;
        public Guid CorrelationId { get; set; }
        public Guid UserProvidedCorrelationId { get; set; }
        public bool UseCorrelationIdFromUser { get; set; }
        public IEnumerable<string> Scopes { get; set; }
        public IDictionary<string, string> ExtraQueryParameters { get; set; }
        public string Claims { get; set; }
        public AuthorityInfo AuthorityOverride { get; set; }
        public IAuthenticationScheme AuthenticationScheme { get; set; } = new BearerAuthenticationScheme();
        public IDictionary<string, string> ExtraHttpHeaders { get; set; }
        public PoPAuthenticationConfiguration PopAuthenticationConfiguration { get; set; }
        public Func<OnBeforeTokenRequestData, Task> OnBeforeTokenRequestHandler { get; internal set; }
        public X509Certificate2 MtlsCertificate { get; internal set; }
    }
}
