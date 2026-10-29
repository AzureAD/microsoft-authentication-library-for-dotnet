# Microsoft Authentication Library (MSAL) for .NET

The MSAL library for .NET is part of the [Microsoft identity platform for developers](https://aka.ms/aaddevv2) (formerly named Azure AD) v2.0. It enables you to acquire security tokens to call protected APIs. It uses industry standard OAuth2 and OpenID Connect. The library also supports [Azure AD B2C](https://azure.microsoft.com/services/active-directory-b2c/).

**Quick links:**

| [Conceptual documentation](https://aka.ms/msalnet) | [Getting Started](https://learn.microsoft.com/entra/msal/dotnet/getting-started/choosing-msal-dotnet) | [Sample Code](https://aka.ms/aaddevsamplesv2) | [API Reference](https://learn.microsoft.com/dotnet/api/overview/) | [Support](README.md#community-help-and-support) | [Feedback](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues)
| ------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------ | ----------------------------------------------- | ------------------------------------------------------------------------------------------------------- |

## NuGet Package

[![NuGet](https://img.shields.io/nuget/v/microsoft.identity.client.svg?style=flat-square&label=nuget&colorB=00b200)](https://www.nuget.org/packages/Microsoft.Identity.Client/)

## Version Lifecycle and Support Matrix

See [Long Term Support policy](./supportPolicy.md) for details.

The following table lists MSAL.NET versions currently supported and receiving security fixes.

| Major Version | Last Release | Patch Release Date  | Support Phase|End of Support |
| --------------|--------------|--------|------------|--------|
| 4.x           | [![NuGet](https://img.shields.io/nuget/v/microsoft.identity.client.svg?style=flat-square&label=nuget&colorB=00b200)](https://www.nuget.org/packages/Microsoft.Identity.Client/)   |Monthly| Active | Not planned.<br/>✅Supported versions: from 4.77.1 to [![NuGet](https://img.shields.io/nuget/v/microsoft.identity.client.svg?style=flat-square&label=nuget&colorB=00b200)](https://www.nuget.org/packages/Microsoft.Identity.Client/)<br/>⚠️Unsupported versions &lt; `4.77.1`.|

### Performance perspectives

[Our documentation](https://learn.microsoft.com/entra/msal/dotnet/advanced/performance-testing) describes the approach to performance testing.

View some of the historical performance benchmark results in [our dashboard](https://azuread.github.io/microsoft-authentication-library-for-dotnet/benchmarks/).

### Reduced logging (opt-in)

MSAL moves selected routine Information messages to Verbose (Debug in
Microsoft.Extensions.Logging) when reduced logging is enabled. This includes per-request
version banners, parameter dumps, regional-authority selection, valid cache-hit
details, and the redundant acquisition-success heading. Acquisition-start and
result summaries remain at Information; warnings, errors, and Always logs are
unchanged. PII logging settings still apply.

To enable reduced logging, set the process environment variable
`MSAL_REDUCED_LOGGING=true` (or `1`) before building MSAL applications.
The default is **false**: an unset, empty, `false`, or `0` value preserves the
existing Information-level diagnostics. Boolean values are case-insensitive.
Unrecognized values leave reduced logging disabled and generate a warning through
the configured logger.
The setting is captured once per MSAL application at construction, including for
its subsequent requests and background refreshes. Existing applications do not
change when the variable changes; restart the host after changing deployment
configuration.

This requires an MSAL upgrade and the opt-in environment variable, including when MSAL is
used through Microsoft.Identity.Web or MISE. It applies to MSAL applications in
the process, not just one consuming SDK, and does not change logs emitted by
those other libraries. Enable Verbose/Debug to see the demoted diagnostics.

## Support SLA

MSAL.NET became Generally Available with MSAL.NET 3.0.8. Since MSAL.NET moved to version 4:

- Major versions are supported for twelve months after the release of the next major version.
- Minor versions older than N-1 are not supported.

> **Note**
> Minor versions include bug fixes or features with non-breaking (additive) API changes. It is expected that applications using the library can upgrade through the IDE or CLI with no friction. We will not patch old minor versions of the library. When opening new issues, please confirm that you are using the latest minor version.

## Using MSAL.NET

- Guides, tutorials, and detailed walkthroughs are available [on Microsoft Learn](https://learn.microsoft.com/entra/msal/dotnet/getting-started/choosing-msal-dotnet).
- API documentation is available [on Microsoft Learn](https://learn.microsoft.com/dotnet/api/microsoft.identity.client)
- Code samples are available from our [Samples](https://aka.ms/aaddevsamplesv2) page.

## Concurrent token acquisition

Cache-eligible confidential-client requests (client credentials, including app token providers,
on-behalf-of, long-running on-behalf-of, and local silent acquisition)
coordinate equivalent concurrent acquisitions within the process. The acquiring
request publishes through the existing token cache; waiting callers recheck their own cache
and receive their own `AuthenticationResult`. Warm cache hits do not wait. When a usable token
is due for proactive refresh, callers receive it immediately while one eligible refresh runs.
Canceling a waiter does not cancel another caller's acquisition. Direct and AppTokenProvider
client-credential acquisitions have separate gates, including when a provider forwards a
same-scope request to a direct client using the shared cache. Callback-driven acquisitions
coordinate only within the same application instance and configured acquisition strategy:
app token providers, dynamic certificate/assertion credentials, service-failure callbacks,
and ordered request-modification callbacks distinguish strategies by object identity.
Different strategies acquire their own gates, including for nested acquisitions; nesting
does not bypass a live admission. A callback must not await a same-strategy acquisition
that depends on its own publication. Completion-only observers do not distinguish strategies.
Admission is released before completion callbacks, and proactive refresh owns separate
telemetry from the foreground cache result.

For eligible coordinated confidential-client acquisitions, a final direct token-endpoint
HTTP 429 or 5xx response without `Retry-After` is cached for 60 seconds using the existing
throttling infrastructure. This failure cache is scoped to the application instance and
equivalent operation, not the shared token cache. Followers receive independent
`MsalThrottledServiceException` wrappers carrying the original service error. A usable cached
token takes precedence, including while proactive refresh is throttled. Existing HTTP retries
and `Retry-After` enforcement are unchanged. Cache-bypass operations do not use this new policy.
Disabling internal HTTP retries with `retryOnceOn5xx: false` does not disable this throttle.
Caller-managed acquisition retries can receive `MsalThrottledServiceException` until the
60-second window expires; these wrappers preserve the original headers and do not synthesize
`Retry-After`. External retry policies must allow for this local backoff rather than retrying
immediately.
Credential errors (`invalid_client`, `invalid_grant`), claims challenges, cancellation,
network errors, HTTP timeouts, and arbitrary provider or request-callback failures are not
cached by this policy. Such failures can still cause successive acquisitions; coordination
does not bound the latency of repeated timeouts or hung callbacks.

No new configuration is required. Coordination is scoped to the existing cache instance, or
the existing shared in-memory cache when enabled. It does not change cache callbacks, loading,
serialization, or storage, and it is not a distributed lock. Independently loaded in-memory
snapshots backed by external persistence may not observe another request's publication and
can still acquire separately. A successful equivalent single-leg burst needs one acquisition
only when its publication is visible through the existing cache checks; retries and multi-leg
exchanges can require additional requests. Different authority aliases or scope supersets
are not guaranteed to coalesce.

Public-client and managed-identity acquisition are unchanged; managed identity retains its
existing semaphore and cache recheck, without the new proactive-refresh coordination.
Force-refresh, claims-driven cache bypass, disabled
caching, and default long-running OBO initiation retain their existing behavior; opt-in cache
search for long-running initiation remains eligible. Binding and revoked-token validation
still use the existing cache checks. Acquisition callbacks run for actual acquisitions, not
for callers served from cache. Cache-validation extensions also run on post-admission
cache rechecks.

Coalescing effectiveness is reported through two aggregate counts appended to the third
section of `x-client-current-telemetry`: `coalesced_requests` and `successful_acquisitions`.
The first counts successful post-admission cache returns and suppressed proactive refresh
attempts, even when the active refresh later fails. The second counts successful coordinated
acquisitions, not individual HTTP attempts. Ordinary warm-cache hits and bypasses do not count.

Counts are accumulated per application instance across initial expired, proactive, and
missing-token reasons, then consumed by the next token request with one of those reasons.
They describe prior operations, not the carrying request's reason, flow, or tenant, and are
not an exact count of ESTS HTTP calls saved. SDK-managed HTTP retries and protocol replays
send zero for these two fields rather than repeating a snapshot. Delivery is best-effort:
failed sends, application disposal, or no subsequent eligible request can lose counts.
No extra requests or persistent storage are introduced. AppTokenProvider operations are
counted locally but may never traverse MSAL's ESTS HTTP path to report them.
ESTS must support the two optional trailing fields before they can be used in server-side
reporting; ingestion and server-side validation are not part of this SDK implementation.

### Rollout and rollback

Start with a canary deployment of the package version containing this change before widening
deployment. Compare token-acquisition failures, throttling, latency, cache-hit rate, and
token-endpoint traffic with the previously deployed version under equivalent workloads.
Include concurrent cache misses, proactive refresh, and application callbacks in canary
coverage. Do not rely on the new aggregate counters until ESTS ingestion supports them.
Pause rollout if authentication failures, latency, or cache behavior regress.

There is no runtime feature flag for coordination or its scoped failure cache. To roll back,
pin the previously deployed MSAL.NET package version, restore dependencies, and redeploy
affected applications. Cache serialization is unchanged; rollback does not require clearing
the persisted token cache.

## Where do I file issues

You can file new issues in [this repository](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues).

## Community help and support

We use [Stack Overflow](https://stackoverflow.com/questions/tagged/azure-ad-msal) with the community to provide support. We highly recommend you ask your questions on Stack Overflow first and browse [existing issues](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues) to see if someone has asked your question before.

If you find a bug or have a feature request, please raise the issue on [GitHub Issues](../../issues).

## Contribute

We welcome contributions and feedback. You can fork and clone the repo and start contributing now. Read our [Contribution Guide](CONTRIBUTING.md) for more information.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Security library

This library controls how users sign-in and access services. We recommend you always take the latest version of our library in your app when possible. We use [semantic versioning](http://semver.org) so you can control the risk associated with updating your app. As an example, always downloading the latest minor version number (e.g. x.*y*.z) ensures you get the latest security and feature enhancements but our API surface remains the same. You can always see the latest version and release notes under the Releases tab of GitHub.

## Security reporting

If you find a security issue with our libraries or services please report it to https://msrc.microsoft.com/report/vulnerability in as much detail as possible. Your submission may be eligible for a bounty through the [Microsoft Bug Bounty](https://aka.ms/bugbounty) program. Please do not post security issues to GitHub Issues or any other public site. We will contact you shortly after receiving the information. We encourage you to get notifications of when security incidents occur by visiting the [Microsoft Technical Security Notifications page](https://www.microsoft.com/msrc/technical-security-notifications?rtc=1) and subscribing to Security Advisory Alerts.

## Data collection

The software may collect information about you and your use of the software and send it to Microsoft. Microsoft may use this information to provide services and improve our products and services. You may turn off the telemetry as described in the repository. There are also some features in the software that may enable you and Microsoft to collect data from users of your applications. If you use these features, you must comply with applicable law, including providing appropriate notices to users of your applications together with a copy of Microsoft's privacy statement. Our privacy statement is located at https://go.microsoft.com/fwlink/?LinkID=824704. You can learn more about data collection and use in the help documentation and our privacy statement. Your use of the software operates as your consent to these practices.

See the [our telemetry documentation](https://learn.microsoft.com/entra/msal/dotnet/resources/telemetry-overview) for an example of the telemetry collected by MSAL.NET.

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft trademarks or logos is subject to and must follow [Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/legal/intellectualproperty/trademarks/usage/general). Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship. Any use of third-party trademarks or logos are subject to those third-party's policies.

Copyright © Microsoft Corporation. All rights reserved. Licensed under the MIT License (the "License").
