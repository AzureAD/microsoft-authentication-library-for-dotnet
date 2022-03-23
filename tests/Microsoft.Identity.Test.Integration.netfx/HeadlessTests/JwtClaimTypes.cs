// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

public struct JwtClaimTypes
{
    /// <summary>
    /// http://tools.ietf.org/html/rfc7519#section-4
    /// </summary>
    public const string Actort = "actort";

    /// <summary>
    /// AzureSpecific
    /// </summary>
    public const string ActorToken = "actortoken";

    /// <summary>
    /// http://openid.net/specs/openid-connect-core-1_0.html#IDToken
    /// </summary>
    public const string Acr = "acr";

    /// <summary>
    /// https://tools.ietf.org/html/rfc7515#section-4.1.1
    /// </summary>
    public const string Alg = "alg";

    /// <summary>
    /// https://tools.ietf.org/html/rfc7515#section-4.1.1
    /// </summary>
    public const string Altsecid = "altsecid";

    /// <summary>
    /// http://openid.net/specs/openid-connect-core-1_0.html#IDToken
    /// </summary>
    public const string Amr = "amr";

    /// <summary>
    /// Azure specific
    /// </summary>
    public const string AppId = "appid";

    /// <summary>
    /// Azure specific
    /// </summary>
    public const string AppIdAcr = "appidacr";

    /// <summary>
    /// http://openid.net/specs/openid-connect-core-1_0.html#CodeIDToken
    /// </summary>
    public const string AtHash = "at_hash";

    /// <summary>
    /// http://tools.ietf.org/html/rfc7519#section-4
    /// </summary>
    public const string Aud = "aud";

    /// <summary>
    /// http://openid.net/specs/openid-connect-core-1_0.html#IDToken
    /// </summary>
    public const string AuthTime = "auth_time";

    /// <summary>
    /// http://openid.net/specs/openid-connect-core-1_0.html#IDToken
    /// </summary>
    public const string Azp = "azp";

    /// <summary>
    /// Azure specific
    /// </summary>
    public const string AzpAcr = "azpacr";

    /// <summary>
    /// http://tools.ietf.org/html/rfc7519#section-4
    /// </summary>
    public const string Birthdate = "birthdate";

    /// <summary>
    /// http://tools.ietf.org/html/rfc7519#section-4
    /// </summary>
    public const string CHash = "c_hash";

    /// <summary>
    /// Azure Specific
    /// </summary>
    public const string Cid = "cid";

    /// <summary>
    /// Azure Specific
    /// </summary>
    public const string ClientAppid = "clientappid";

    /// <summary>
    /// http://tools.ietf.org/html/rfc7519#section-4
    /// </summary>
    public const string Email = "email";

    /// <summary>
    /// when hueristically checking for an appToken
    /// these claims should never be found
    /// </summary>
    public static IList<string> ExcludedAppClaims = new List<string> { JwtClaimTypes.Scp, JwtClaimTypes.UniqueName };

    /// <summary>
    /// http://tools.ietf.org/html/rfc7519#section-4
    /// </summary>
    public const string Exp = "exp";

    /// <summary>
    /// http://tools.ietf.org/html/rfc7519#section-4
    /// </summary>
    public const string FamilyName = "family_name";

    /// <summary>
    /// http://tools.ietf.org/html/rfc7519#section-4
    /// </summary>
    public const string Gender = "gender";

    /// <summary>
    /// http://tools.ietf.org/html/rfc7519#section-4
    /// </summary>
    public const string GivenName = "given_name";

    /// <summary>
    /// http://tools.ietf.org/html/rfc7519#section-4
    /// </summary>
    public const string Iat = "iat";

    /// <summary>
    /// AAD claim that determines what type of token
    /// </summary>
    public const string Idtyp = "idtyp";

    /// <summary>
    /// Azure Specific
    /// </summary>
    public const string IsConsumer = "isconsumer";

    /// <summary>
    /// http://tools.ietf.org/html/rfc7519#section-4
    /// </summary>
    public const string Iss = "iss";

    /// <summary>
    /// http://tools.ietf.org/html/rfc7519#section-4
    /// </summary>
    public const string Jti = "jti";

    /// <summary>
    /// https://tools.ietf.org/html/rfc7515#section-4.1.4
    /// </summary>
    public const string Kid = "kid";

    /// <summary>
    /// Azure Specific
    /// </summary>
    public const string IpAddr = "ipaddr";

    /// <summary>
    /// Azure Specific
    /// </summary>
    public const string Name = "name";

    /// <summary>
    /// http://tools.ietf.org/html/rfc7519#section-4
    /// </summary>
    public const string NameId = "nameid";

    /// <summary>
    /// http://tools.ietf.org/html/rfc7519#section-4
    /// </summary>
    public const string Nonce = "nonce";

    /// <summary>
    /// http://tools.ietf.org/html/rfc7519#section-4
    /// </summary>
    public const string Nbf = "nbf";

    /// <summary>
    /// Azure Specific
    /// </summary>
    public const string Oid = "oid";

    /// <summary>
    /// http://tools.ietf.org/html/rfc7519#section-4
    /// </summary>
    public const string PopJwk = "pop_jwk";

    /// <summary>
    /// http://tools.ietf.org/html/rfc7519#section-4
    /// </summary>
    public const string Prn = "prn";

    /// <summary>
    /// Azure specific
    /// </summary>
    public const string Puid = "puid";

    /// <summary>
    /// Azure specific
    /// </summary>
    public const string Roles = "roles";

    /// <summary>
    /// Azure specific
    /// </summary>
    public const string Scp = "scp";

    /// <summary>
    /// http://openid.net/specs/openid-connect-frontchannel-1_0.html#OPLogout
    /// </summary>
    public const string Sid = "sid";

    /// <summary>
    /// Azure specific
    /// </summary>
    public const string Smtp = "smtp";

    /// <summary>
    /// http://tools.ietf.org/html/rfc7519#section-4
    /// </summary>
    public const string Sub = "sub";

    /// <summary>
    /// Azure specific
    /// </summary>
    public const string Tid = "tid";

    /// <summary>
    /// https://tools.ietf.org/html/rfc7519#section-5.1
    /// </summary>
    public const string Typ = "typ";

    /// <summary>
    /// http://tools.ietf.org/html/rfc7519#section-4
    /// </summary>
    public const string UniqueName = "unique_name";

    /// <summary>
    /// Azure Specific
    /// </summary>
    public const string Upn = "upn";

    /// <summary>
    /// Azure Specific
    /// </summary>
    public const string Ver = "ver";

    /// <summary>
    /// http://tools.ietf.org/html/rfc7519#section-4
    /// </summary>
    public const string Website = "website";

    /// <summary>
    /// https://tools.ietf.org/html/rfc7515#section-4.1.4
    /// </summary>
    public const string X5t = "x5t";

    /// <summary>
    /// Contains the X.509 public key certificate or certificate chain corresponding to the key used to digitally sign the JWS.
    /// https://tools.ietf.org/html/rfc7515#section-4.1.6
    /// </summary>
    public const string X5c = "x5c";

    /// <summary>
    /// An STI(Substrate Token Issuer) specific claim that contains the key used to validate the signature on the token. The key itself is signed and must be validated before use.
    /// https://aka.ms/s2s/epk
    /// </summary>
    public const string Epk = "epk";
}

