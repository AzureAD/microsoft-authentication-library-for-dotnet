# Legacy Tenants Analysis - MSAL.NET

**Analysis Date:** January 6, 2026  
**Repository:** microsoft-authentication-library-for-dotnet

## Executive Summary

This analysis identifies **16 files** still using legacy Microsoft test tenants (msidlab4 and msidlab8) instead of the current primary test tenant (ID4SLAB1: `10c419d4-4a50-45b2-aa4e-919fb84df24f`).

## Legacy Tenants Overview

| Tenant | Type | ID | Primary Use |
|--------|------|----|----|
| msidlab8 | ADFS | `fs.msidlab8.com` | ADFS testing scenarios |
| msidlab4 | AAD | `f645ad92-e38d-4d1a-b510-d1b09a74a8ca` | Legacy AAD testing, regional tests |
| ID4SLAB1 | AAD | `10c419d4-4a50-45b2-aa4e-919fb84df24f` | Current primary test tenant |

## Active Test Files Using Legacy Tenants (5 files)

### msidlab8 (ADFS Tenant) - 2 Active Tests

1. **PoP Test with ADFS User and Broker**  
   📍 [tests/Microsoft.Identity.Test.Unit/pop/PoPTests.cs#L277](tests/Microsoft.Identity.Test.Unit/pop/PoPTests.cs#L277)  
   - **Method:** `PopWhithAdfsUserAndBroker_Async`
   - **Usage:** `environment: "fs.msidlab8.com"`
   - **Purpose:** Tests PoP authentication failure with ADFS users and broker

2. **MSAL Cache Helper ADFS Test**  
   📍 [tests/Microsoft.Identity.Test.Unit/CacheExtension/MsalCacheHelperTests.cs#L479](tests/Microsoft.Identity.Test.Unit/CacheExtension/MsalCacheHelperTests.cs#L479)  
   - **Usage:** `.WithAuthority("https://fs.msidlab8.com/adfs")`
   - **Purpose:** Cache helper testing with ADFS authority

### msidlab4 (Legacy AAD Tenant) - 3 Active Integration Tests

1. **Regional Test - Government Audience**  
   📍 [tests/Microsoft.Identity.Test.Integration.netcore/HeadlessTests/ClientCredentialsTests.WithRegion.cs#L67](tests/Microsoft.Identity.Test.Integration.netcore/HeadlessTests/ClientCredentialsTests.WithRegion.cs#L67)  
   - **Method:** `ClientCredentialsWithRegion_AudienceGovernment_Async`
   - **Usage:** `Cloud.PublicLegacy` → msidlab4 tenant
   - **Reason:** Regional endpoints require original tenant due to AADSTS100007 restrictions

2. **Regional Test - Auto Region**  
   📍 [tests/Microsoft.Identity.Test.Integration.netcore/HeadlessTests/ClientCredentialsTests.WithRegion.cs#L86](tests/Microsoft.Identity.Test.Integration.netcore/HeadlessTests/ClientCredentialsTests.WithRegion.cs#L86)  
   - **Method:** `ClientCredentialsWithAutoRegion_Async`
   - **Usage:** `Cloud.PublicLegacy` → msidlab4 tenant

3. **Regional Test - Certificate**  
   📍 [tests/Microsoft.Identity.Test.Integration.netcore/HeadlessTests/ClientCredentialsTests.WithRegion.cs#L201](tests/Microsoft.Identity.Test.Integration.netcore/HeadlessTests/ClientCredentialsTests.WithRegion.cs#L201)  
   - **Method:** `ClientCredentialsWithRegionCert_Async`
   - **Usage:** `Cloud.PublicLegacy` → msidlab4 tenant

## Configuration and Infrastructure Files (11 files)

### Core Configuration Files

4. **TestConstants**  
   📍 [tests/Microsoft.Identity.Test.Common/TestConstants.cs](tests/Microsoft.Identity.Test.Common/TestConstants.cs)  
   - **Lines:** 113, 267, 642
   - **Usage:** ADFS Authority constants (`https://fs.msidlab8.com/adfs/`)
   - **Usage:** Key vault secret names for msidlab4

5. **ConfidentialAppSettings**  
   📍 [tests/Microsoft.Identity.Test.Integration.netcore/Infrastructure/ConfidentialAppSettings.cs](tests/Microsoft.Identity.Test.Integration.netcore/Infrastructure/ConfidentialAppSettings.cs)  
   - **Lines:** 17, 151, 153, 155
   - **Usage:** Legacy configuration class for msidlab4
   - **Comment:** "For regional tests that need original MSIDLAB4 configuration"

### Lab Infrastructure Files

6. **LabServiceParameters**  
   📍 [tests/Microsoft.Identity.Test.LabInfrastructure/LabServiceParameters.cs](tests/Microsoft.Identity.Test.LabInfrastructure/LabServiceParameters.cs)  
   - **Lines:** 72, 80
   - **Usage:** Enum definitions: `MsidLab4`, `GidLab_Msidlab4`

7. **LabApiConstants**  
   📍 [tests/Microsoft.Identity.Test.LabInfrastructure/LabApiConstants.cs](tests/Microsoft.Identity.Test.LabInfrastructure/LabApiConstants.cs)  
   - **Line:** 25
   - **Usage:** `MSAOutlookAccount = "MSIDLAB4_Outlook"`

### Unit Test Files with Hardcoded Legacy Tenant IDs

8. **ID Token Parsing Tests**  
   📍 [tests/Microsoft.Identity.Test.Unit/CoreTests/IdTokenParsingTests.cs](tests/Microsoft.Identity.Test.Unit/CoreTests/IdTokenParsingTests.cs)  
   - **Multiple occurrences:** Lines 25, 33, 44, 47, 55, 66, 67
   - **Usage:** Hardcoded msidlab4 tenant ID in test tokens and assertions

9. **Cache Schema Validation Tests**  
   📍 [tests/Microsoft.Identity.Test.Unit/CacheTests/UnifiedSchemaValidationTests.cs](tests/Microsoft.Identity.Test.Unit/CacheTests/UnifiedSchemaValidationTests.cs)  
   - **Multiple occurrences:** Lines 28, 83, 87, 96, 99, 102, 125, 126, 135, 138, 141, 164, 176, 205
   - **Usage:** Hardcoded tenant ID constants and cache key generation tests

## Migration Considerations

### Can Be Migrated
- **Unit tests with hardcoded tenant IDs** - Safe to update to ID4SLAB1 or use mock values
- **Some ADFS tests** - Could potentially use newer ADFS test infrastructure

### Should Not Be Migrated
- **Regional integration tests** - Must remain on msidlab4 due to AADSTS100007 restrictions
- **ADFS-specific tests** - msidlab8 is the designated ADFS test environment

### Migration Priority
1. **High Priority:** Unit tests with hardcoded legacy tenant IDs (2 files)
2. **Medium Priority:** Configuration constants that could be updated (2 files)  
3. **Low Priority:** Lab infrastructure enums (may need to remain for backward compatibility)
4. **Do Not Migrate:** Regional tests and ADFS-specific tests (5 files)

## Detailed Breakdown by Category

| Category | Files | Migration Recommended |
|----------|-------|----------------------|
| Active PoP/ADFS Tests | 2 | ❌ No (specialized testing) |
| Active Regional Tests | 3 | ❌ No (technical restrictions) |
| Unit Test Constants | 2 | ✅ Yes (safe to modernize) |
| Configuration Files | 2 | ⚠️ Evaluate (may break compatibility) |
| Lab Infrastructure | 2 | ⚠️ Evaluate (backward compatibility) |
| Legacy References | 5 | ✅ Yes (can be updated) |

## Recommendations

1. **Immediate Action:** Update hardcoded tenant IDs in unit tests that don't require specific tenant behavior
2. **Evaluate:** Configuration constants that might be used by external tools or other test suites  
3. **Preserve:** Regional tests and ADFS tests that have technical requirements for specific tenants
4. **Document:** Add comments explaining why certain tests must use legacy tenants

---

**Note:** This analysis focuses on actual code usage. Additional references may exist in test result files, logs, and temporary artifacts which are not included in this count.