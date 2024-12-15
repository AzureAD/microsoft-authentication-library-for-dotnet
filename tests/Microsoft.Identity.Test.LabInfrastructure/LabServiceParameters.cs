// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Test.LabInfrastructure
{
    public enum FederationProvider
    {
        Unknown,
        None,
        AdfsV4,
        [Obsolete("ADFSv3 is out of support, do not use. The Arlingthon lab is federated to ADFSv3, so this value is needed")]
        AdfsV3,
        PingFederateV83,
        Shibboleth,
        ADFSv2019,
        B2C,
        Ping,
        CIAM,
        CIAMCUD
    }

    public enum B2CIdentityProvider
    {
        None, // Non-B2C user
        Local, // Local B2C account
        Facebook,
        Google,
        MSA,
        Amazon,
        Microsoft,
        Twitter
    }

    public enum UserType
    {
        Member, //V1 lab api only
        Guest,
        B2C,
        Cloud,
        Federated,
        OnPrem,
        MSA,
    }

    internal enum MFA
    {
        None,
        MfaOnAll,
        AutoMfaOnAll
    }

    internal enum ProtectionPolicy
    {
        None,
        CA,
        CADJ,
        MAM,
        MDM,
        MDMCA,
        MAMCA,
        MAMSPO
    }

    internal enum HomeDomain //Must add ".com" to end for lab query
    {
        None,
        MsidLab2,
        MsidLab3,
        MsidLab4
    }

    internal enum HomeUPN //Must replace "_" with "@" add ".com" to end for lab query
    {
        None,
        GidLab_Msidlab2,
        GidLab_Msidlab3,
        GidLab_Msidlab4,
    }

    public enum AzureEnvironment
    {
        azurecloud,
        azureb2ccloud,
        azurechinacloud,
        azuregermanycloud,
        azureppe,
        azureusgovernment
    }

    internal enum SignInAudience
    {
        AzureAdMyOrg,
        AzureAdMultipleOrgs,
        AzureAdAndPersonalMicrosoftAccount
    }

    internal enum AppPlatform
    {
        web,
        spa
    }

    internal enum PublicClient
    {
        yes,
        no
    }
}
