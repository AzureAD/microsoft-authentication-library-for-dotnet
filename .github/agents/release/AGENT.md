# MSAL.NET Release Agent

## Purpose

Automates the MSAL.NET release process end-to-end — from queuing the OneBranch pipeline through NuGet publish to post-release cleanup. Replaces the manual spreadsheet checklist with automated checks and a permanent release report.

## Quick Start

Tell Copilot:
```
"Release MSAL"
```

The agent will:
1. Ask you for the **version number** (e.g., `4.85.0`)
2. Queue the OneBranch release pipeline with the correct variables
3. Run pre-release validation checks
4. Wait for your manual approval before NuGet publish
5. Generate a release report and create a post-release PR

## Release Flow

```
┌─────────────────────────────────────────────────────────────┐
│                    MSAL Release Agent                       │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  1. TRIGGER                                                 │
│     Agent asks: "What version are you releasing?"           │
│     Sets variables and queues ADO pipeline                  │
│                                                             │
│  2. PRE-RELEASE VALIDATION (automated)                      │
│     ✓ Package signing verification (7 packages)             │
│     ✓ Build warnings audit                                  │
│     ✓ TSA security bugs check                               │
│     ✓ Component Governance alerts check                     │
│     ✓ SDL/Compliance tasks check                            │
│                                                             │
│  3. APPROVAL GATE (manual)                                  │
│     Engineer reviews pre-release report                     │
│     Approves NuGet + IDDP publish                           │
│                                                             │
│  4. PUBLISH (automated)                                     │
│     NuGet push (7 packages)                                 │
│     IDDP feed push                                          │
│     Build retention lease (indefinite)                      │
│                                                             │
│  5. POST-RELEASE (automated)                                │
│     Create release report: build/release-logs/              │
│     Move PublicAPI.Unshipped → Shipped                      │
│     Close GitHub milestone                                  │
│     Create GitHub Release                                   │
│     Notify Teams release channel                            │
│     Create post-release summary PR                          │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## Pipeline Variables

When the agent queues the OneBranch pipeline, it sets these variables:

| Variable | Value | Behavior |
|----------|-------|----------|
| `MicrosoftIdentityClientVersion` | *(user-provided)* | The version to release |
| `Release.IDDP` | `true` | Always set — publish to IDDP feed |
| `Release.NuGet` | `true` | Always set — publish to NuGet.org |

All other variables use their pipeline defaults:

| Variable | Default |
|----------|---------|
| `MsalSourceDir` | `microsoft-authentication-library-for-dotnet\` |
| `PipelineType` | `OneBranch` |
| `MSAL_SKIP_FEDERATED_TESTS` | `True` |
| `LabApiVersion` | `2.0.0` |
| `approvers` | `IdentityDevExDotnet@microsoft.com` |
| `BuildConfig` | `configurations-prod` |
| `INCLUDE_MOBILE_AND_LEGACY_TFM` | `True` |

## Pre-Release Checks

Five automated checks run before the release is approved:

### 1. Package Signing Verification

Runs `nuget verify --all` on each .nupkg to confirm authenticode + NuGet signing.

**Packages verified:**
- `Microsoft.Identity.Client`
- `Microsoft.Identity.Client.Desktop`
- `Microsoft.Identity.Client.Broker`
- `Microsoft.Identity.Client.Desktop.WinUI3`
- `Microsoft.Identity.Client.KeyAttestation`
- `Microsoft.Identity.Client.Extensions.Msal`
- `Microsoft.Identity.Lab.Api`

### 2. Build Warnings Audit

Parses the ADO build timeline via REST API. Filters out known-safe warnings:
- `"Using Node 20 for container startup"` — infra, safe
- `"No files found for tool: binskim"` — no native binaries, expected
- `"updaterepositorypath not allowed"` — policy, safe
- Warnings from `DevApp`, `Test` stages — ignored

Flags any **unexpected** product-code warnings.

### 3. TSA Security Bugs

Queries the [TSA API](https://tsaapivnext.azurewebsites.net/Result/CodeBase/MSAL%20.NET%20AUTH%20SDK/Summary) for active bugs in the `MSAL .NET AUTH SDK` codebase. **Blocks release** if any Critical or Important bugs are active.

### 4. Component Governance Alerts

Queries the [ADO Component Governance API](https://identitydivision.visualstudio.com/IDDP/_componentGovernance/98561) for active alerts (alert type 12724953). **Blocks release** if active alerts exist.

### 5. SDL / Compliance

Verifies all OneBranch SDL pipeline tasks completed successfully:
- CredScan
- PoliCheck
- BinSkim (may be skipped — no native binaries)
- CodeQL
- Roslyn Analyzers
- API Scan

## Post-Release Actions

After successful NuGet publish:

### Build Retention
Adds an ADO retention lease (`daysValid: 36500` ≈ indefinite) so the release build and its artifacts are never deleted by ADO cleanup policies.

### Release Report
Generates a markdown report at `build/release-logs/release-{version}.md` containing:
- Pre-release check results
- Packages published (with versions)
- Build links
- Post-release task status

### Post-Release Summary PR
Creates a single PR that includes:
- The release report file
- `PublicAPI.Unshipped.txt` → `PublicAPI.Shipped.txt` updates

### Other Automated Tasks
- Close the GitHub milestone matching the version
- Create a GitHub Release with notes extracted from `CHANGELOG.md`
- Post a notification to the Teams release channel

## Known Issues & Flaky Tests

Some tests may fail intermittently during the build:

| Test Area | Issue | Resolution |
|-----------|-------|------------|
| Linux integration tests | Network-dependent, flaky | Re-run the test stage |
| Federated tests | Environment-dependent | Skipped via `MSAL_SKIP_FEDERATED_TESTS=True` |
| Network isolation warnings | OneBranch infra | Safe to ignore |

## Related Links

- **OneBranch Pipeline:** [MSAL.NET-OneBranch-Release-Official](https://identitydivision.visualstudio.com/IDDP/_build?definitionId=1545)
- **GitHub Repo:** [AzureAD/microsoft-authentication-library-for-dotnet](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet)
- **ADO Repo:** [IDDP/MSAL.NET-OneBranch](https://identitydivision.visualstudio.com/IDDP/_git/MSAL.NET-OneBranch)
- **TSA Dashboard:** [MSAL .NET AUTH SDK](https://tsaapivnext.azurewebsites.net/Result/CodeBase/MSAL%20.NET%20AUTH%20SDK/Summary)
- **Component Governance:** [IDDP CG Alerts](https://identitydivision.visualstudio.com/IDDP/_componentGovernance/98561?_a=alerts&typeId=12724953&alerts-view-option=active)
- **NuGet:** [Microsoft.Identity.Client](https://www.nuget.org/packages/Microsoft.Identity.Client)
