// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Labs
{
    /// <summary>
    /// Represents the authentication style used by a test user.
    /// </summary>
    public enum AuthType
    {
        /// <summary>
        /// A basic username/password account with no federation or multi-factor authentication (MFA).
        /// </summary>
        Basic,

        /// <summary>
        /// A federated account (for example, via ADFS, Ping, or another identity provider).
        /// </summary>
        Federated,

        /// <summary>
        /// An account that requires multi-factor authentication (MFA).
        /// </summary>
        Mfa,

        /// <summary>
        /// A business-to-business (B2B) guest account invited into the tenant.
        /// </summary>
        Guest
    }

    /// <summary>
    /// Identifies the cloud or sovereign environment where identities and applications are hosted.
    /// </summary>
    public enum CloudType
    {
        /// <summary>
        /// Azure Public (global) cloud.
        /// </summary>
        Public,

        /// <summary>
        /// Azure Government (GCC/GCC High) cloud.
        /// </summary>
        Gcc,

        /// <summary>
        /// Azure Government Department of Defense (DoD) environment.
        /// </summary>
        Dod,

        /// <summary>
        /// Azure China cloud (operated by 21Vianet).
        /// </summary>
        China,

        /// <summary>
        /// Microsoft Cloud for Germany / Azure Germany.
        /// </summary>
        Germany,

        /// <summary>
        /// Integration or pre‑production environment used for testing.
        /// </summary>
        Canary
    }

    /// <summary>
    /// Names the functional test scenario or user pool.
    /// </summary>
    public enum Scenario
    {
        /// <summary>
        /// Basic or smoke-test scenarios.
        /// </summary>
        Basic,

        /// <summary>
        /// On‑Behalf‑Of (OBO) flow scenarios.
        /// </summary>
        Obo,

        /// <summary>
        /// Confidential Client Application (CCA) scenarios.
        /// </summary>
        Cca,

        /// <summary>
        /// Device Code flow scenarios.
        /// </summary>
        DeviceCode,

        /// <summary>
        /// Resource Owner Password Credentials (ROPC) flow scenarios.
        /// </summary>
        Ropc,

        /// <summary>
        /// Daemon (application‑only) scenarios without user interaction.
        /// </summary>
        Daemon
    }

    /// <summary>
    /// Specifies the type of application whose credentials should be resolved.
    /// </summary>
    public enum AppKind
    {
        /// <summary>
        /// A public client application (desktop, mobile, or SPA) that does not use a client secret.
        /// </summary>
        PublicClient,

        /// <summary>
        /// A confidential client application (web app/service) that authenticates with a client secret or certificate.
        /// </summary>
        ConfidentialClient,

        /// <summary>
        /// A headless/background application that runs without user interaction.
        /// </summary>
        Daemon,

        /// <summary>
        /// A protected Web API (resource) that validates tokens issued to clients.
        /// </summary>
        WebApi,

        /// <summary>
        /// A web application (confidential client) that signs in users and can call downstream APIs.
        /// </summary>
        WebApp
    }
}
