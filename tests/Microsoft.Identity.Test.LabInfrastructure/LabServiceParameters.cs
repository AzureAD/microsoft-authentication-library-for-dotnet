// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Test.LabInfrastructure
{
    public enum FederationProvider
    {
        Unknown,
        None,
        AdfsV2,
        AdfsV3,
        AdfsV4,
        PingFederateV83,
        Shibboleth,
        ADFSv2019,
        B2C,
        Ping
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

    public enum MFA
    {
        None,
        MfaOnAll,
        AutoMfaOnAll
    }

    public enum ProtectionPolicy
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

    public enum HomeDomain //Must add ".com" to end for lab queury
    {
        None,
        MsidLab2,
        MsidLab3,
        MsidLab4
    }

    public enum HomeUPN //Must replace "_" with "@" add ".com" to end for lab queury
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

    public enum SignInAudience
    {
        AzureAdMyOrg,
        AzureAdMultipleOrgs,
        AzureAdAndPersonalMicrosoftAccount
    }
}
