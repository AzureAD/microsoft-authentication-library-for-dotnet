# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

param(
    [string] $BuildConfiguration = 'Release'
)

$ErrorActionPreference = 'Stop'

if (-not [Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([Runtime.InteropServices.OSPlatform]::Windows)) {
    throw 'This check must run on Windows so the OS guard cannot mask a missing target-framework guard.'
}

$repoRoot = Split-Path $PSScriptRoot -Parent
$msalPath = (Resolve-Path (Join-Path $repoRoot "src\client\Microsoft.Identity.Client\bin\$BuildConfiguration\netstandard2.0\Microsoft.Identity.Client.dll")).Path
$abstractionsPath = (Resolve-Path (Join-Path $repoRoot "tests\Microsoft.Identity.Test.Unit\bin\$BuildConfiguration\net10.0\Microsoft.IdentityModel.Abstractions.dll")).Path
Add-Type -Path $abstractionsPath
Add-Type -Path $msalPath

$assembly = [Microsoft.Identity.Client.ManagedIdentityPopExtensions].Assembly
$targetFramework = $assembly.GetCustomAttributes([Runtime.Versioning.TargetFrameworkAttribute], $false)[0].FrameworkName
if ($targetFramework -ne '.NETStandard,Version=v2.0') {
    throw "Expected the compiled netstandard2.0 asset, but loaded $targetFramework."
}

Add-Type -ReferencedAssemblies @(
    $msalPath,
    (Join-Path $PSHOME 'ref\System.Runtime.dll'),
    (Join-Path $PSHOME 'ref\System.Net.Http.dll'),
    (Join-Path $PSHOME 'ref\netstandard.dll')
) -TypeDefinition @'
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

public sealed class NetStandardGuardHttpClientFactory : HttpMessageHandler, IMsalHttpClientFactory
{
    public HttpClient Client { get; }
    public int RequestCount { get; private set; }

    public NetStandardGuardHttpClientFactory()
    {
        Client = new HttpClient(this, disposeHandler: false);
    }

    public HttpClient GetHttpClient() => Client;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestCount++;
        throw new InvalidOperationException("The target-framework guard must reject before any HTTP request.");
    }
}
'@

$factory = [NetStandardGuardHttpClientFactory]::new()
try {
    $app = [Microsoft.Identity.Client.ManagedIdentityApplicationBuilder]::Create(
        [Microsoft.Identity.Client.AppConfig.ManagedIdentityId]::SystemAssigned
    ).WithHttpClientFactory($factory).Build()

    $cases = [ordered]@{
        'PoP' = { param($request) [Microsoft.Identity.Client.ManagedIdentityPopExtensions]::WithMtlsProofOfPossession($request) }
        'PoP with options' = { param($request) [Microsoft.Identity.Client.ManagedIdentityPopExtensions]::WithMtlsProofOfPossession($request, [Microsoft.Identity.Client.AppConfig.PoPOptions]::new()) }
        'Bearer over mTLS' = { param($request) [Microsoft.Identity.Client.ManagedIdentityPopExtensions]::WithRequestOverMtls($request) }
    }

    foreach ($case in $cases.GetEnumerator()) {
        $request = $app.AcquireTokenForManagedIdentity('https://management.azure.com/')
        $rejected = $false
        try {
            & $case.Value $request | Out-Null
        }
        catch {
            $exception = $_.Exception.GetBaseException()
            if ($exception -isnot [Microsoft.Identity.Client.MsalClientException] -or
                $exception.ErrorCode -ne [Microsoft.Identity.Client.MsalError]::MtlsNotSupportedForManagedIdentity) {
                throw
            }
            $rejected = $true
        }

        if (-not $rejected -or $factory.RequestCount -ne 0) {
            throw "$($case.Key) must reject the netstandard2.0 asset before any HTTP request."
        }
        Write-Host "Passed: $($case.Key) rejects netstandard2.0 without HTTP."
    }
}
finally {
    $factory.Client.Dispose()
    $factory.Dispose()
}
