Carefully review all markdown documents in the ../.clinerules folder. Those are your custom instructions.

---

# GitHub Copilot Agent Skills (Repository Skills)

This repository defines **Copilot Agent Skills** under `.github/skills/`.

## How skills work
- A **skill** is defined by a folder: `.github/skills/<skill-folder>/`
- Each skill must contain a file named **`SKILL.md`**
- `SKILL.md` must start with YAML frontmatter that includes at least:
  - `name`
  - `description`

> Note: Copilot does **not** read `copilot-instructions.md` files inside subfolders.
> Only `.github/copilot-instructions.md` is treated as the repo-wide instructions file.
> Skill content must be inside `.github/skills/**/SKILL.md`.

## How to use skills in Copilot Chat
- Prefer invoking a relevant skill explicitly when available:
  - `@<skill-name> ...your question...`
- If unsure which skill applies, ask:
  - “What skills are available in this repo?”
  - “Which skill should I use for this task?”

## Expectations when using skills
- Follow the skill’s guidance and patterns exactly (APIs, naming, examples).
- If code is requested, provide complete, runnable code with required imports.
- If multiple approaches exist, explain the tradeoffs and recommend one.

---

# Copilot Instructions for MSAL.NET mTLS Proof-of-Possession

## 🚀 Quick Start: Discover Available Skills

**Ask these questions in VS Code Copilot Chat to discover and explore all available skills:**

What can you tell me about mTLS PoP in MSAL.NET?

Code

Copilot will automatically reference and describe:
- `@msal-mtls-pop-guidance` - Foundational concepts
- `@msal-mtls-pop-vanilla` - Direct token acquisition
- `@msal-mtls-pop-fic-two-leg` - Token exchange patterns

---

## 📚 Available Skills Overview

This repository contains **three GitHub Agent Skills** for mTLS Proof-of-Possession (PoP) authentication:

| Skill | Purpose | Best For |
|-------|---------|----------|
| **@msal-mtls-pop-guidance** | Foundational concepts, terminology, decision frameworks | Learning the fundamentals, comparing approaches |
| **@msal-mtls-pop-vanilla** | Direct single-step token acquisition with complete code | Quick implementation with MSI or Confidential Client |
| **@msal-mtls-pop-fic-two-leg** | Two-step token exchange patterns | Complex scenarios requiring token exchange |

---

## 🔍 Discovery Prompts: Explore Each Skill

### **Discover Skill 1: Guidance & Concepts**
@msal-mtls-pop-guidance What is mTLS PoP and why do I need it? @msal-mtls-pop-guidance What are the main concepts I need to understand? @msal-mtls-pop-guidance Explain vanilla vs FIC two-leg flows @msal-mtls-pop-guidance What are the MSI limitations? @msal-mtls-pop-guidance Which approach should I use for my scenario? @msal-mtls-pop-guidance What version requirements exist?

Code

### **Discover Skill 2: Vanilla (Direct) Token Acquisition**
@msal-mtls-pop-vanilla What code examples do you have for mTLS PoP? @msal-mtls-pop-vanilla Show me System-Assigned Managed Identity (SAMI) example @msal-mtls-pop-vanilla Show me User-Assigned Managed Identity (UAMI) example @msal-mtls-pop-vanilla Show me Confidential Client with certificate example @msal-mtls-pop-vanilla How do I configure the HttpClient for mTLS? @msal-mtls-pop-vanilla What helper classes are available? @msal-mtls-pop-vanilla How do I handle certificate binding safely?

Code

### **Discover Skill 3: FIC Two-Leg Token Exchange**
@msal-mtls-pop-fic-two-leg What is the two-leg token exchange pattern? @msal-mtls-pop-fic-two-leg Show me a complete end-to-end example @msal-mtls-pop-fic-two-leg How does certificate binding work between legs? @msal-mtls-pop-fic-two-leg What's the difference between Leg 1 and Leg 2? @msal-mtls-pop-fic-two-leg What helper classes are available? @msal-mtls-pop-fic-two-leg Why is MSI limited to Leg 1 only?

Code

---

## 🎯 Comprehensive Question Bank

Use these questions to explore the full depth of available skills:

### **Foundation & Architecture Questions**

@msal-mtls-pop-guidance What is mTLS Proof-of-Possession (PoP)? @msal-mtls-pop-guidance Why would I use mTLS PoP instead of bearer tokens? @msal-mtls-pop-guidance What are the security benefits of mTLS PoP? @msal-mtls-pop-guidance What MSAL.NET version do I need? @msal-mtls-pop-guidance What namespaces do I need to import? @msal-mtls-pop-guidance What are the three UAMI identifier types? @msal-mtls-pop-guidance Explain the difference between SAMI and UAMI @msal-mtls-pop-guidance What's the api://AzureADTokenExchange resource?

Code

### **Vanilla Flow - Quick Implementation**

@msal-mtls-pop-vanilla How do I get started with mTLS PoP in 5 minutes? @msal-mtls-pop-vanilla Show me the simplest working example @msal-mtls-pop-vanilla What's the bare minimum code I need? @msal-mtls-pop-vanilla How do I test my implementation?

Code

### **Vanilla Flow - System-Assigned Managed Identity (SAMI)**

@msal-mtls-pop-vanilla What is SAMI and when should I use it? @msal-mtls-pop-vanilla Show me how to create a ManagedIdentityApplicationBuilder for SAMI @msal-mtls-pop-vanilla What's the complete code for SAMI mTLS PoP? @msal-mtls-pop-vanilla How do I acquire a token for System-Assigned Managed Identity? @msal-mtls-pop-vanilla Show me the SAMI example with Credential Guard attestation @msal-mtls-pop-vanilla How do I use the binding certificate from SAMI token? @msal-mtls-pop-vanilla What errors might I encounter with SAMI and how do I fix them?

Code

### **Vanilla Flow - User-Assigned Managed Identity (UAMI)**

@msal-mtls-pop-vanilla What is UAMI and when should I use it? @msal-mtls-pop-vanilla Show me the three ways to identify a UAMI @msal-mtls-pop-vanilla How do I use UAMI by ClientId? @msal-mtls-pop-vanilla How do I use UAMI by ResourceId? @msal-mtls-pop-vanilla How do I use UAMI by ObjectId? @msal-mtls-pop-vanilla What's the complete code for UAMI mTLS PoP? @msal-mtls-pop-vanilla Show me how to handle different UAMI identifier types @msal-mtls-pop-vanilla How do I know which UAMI identifier to use?

Code

### **Vanilla Flow - Confidential Client**

@msal-mtls-pop-vanilla What is Confidential Client and when should I use it? @msal-mtls-pop-vanilla How do I configure a Confidential Client with certificate (SNI)? @msal-mtls-pop-vanilla Show me the complete Confidential Client mTLS PoP example @msal-mtls-pop-vanilla How do I load a certificate for mTLS PoP? @msal-mtls-pop-vanilla What's the difference between SAMI, UAMI, and Confidential Client? @msal-mtls-pop-vanilla When should I use Confidential Client instead of MSI?

Code

### **Certificate & HTTP Configuration**

@msal-mtls-pop-vanilla How do I get the binding certificate from the token result? @msal-mtls-pop-vanilla What is a binding certificate and why do I need it? @msal-mtls-pop-vanilla How do I add the certificate to HttpClientHandler? @msal-mtls-pop-vanilla What are null-safe certificate handling best practices? @msal-mtls-pop-vanilla How do I avoid compiler warnings with certificate binding? @msal-mtls-pop-vanilla Show me the complete HttpClient setup for mTLS PoP @msal-mtls-pop-vanilla What's the correct pattern for checking if certificate is null? @msal-mtls-pop-vanilla How do I dispose of HttpClient properly?

Code

### **Authorization & Endpoints**

@msal-mtls-pop-vanilla What's the correct Authorization header for mTLS PoP? @msal-mtls-pop-vanilla Why is the "mtls_pop" scheme important? @msal-mtls-pop-vanilla What's the mTLS-specific endpoint for Microsoft Graph? @msal-mtls-pop-vanilla Why use https://mtlstb.graph.microsoft.com instead of regular endpoint? @msal-mtls-pop-vanilla Should I use /applications or /me for service-to-service calls? @msal-mtls-pop-vanilla How do I call Microsoft Graph with mTLS PoP tokens? @msal-mtls-pop-vanilla What other endpoints support mTLS PoP? @msal-mtls-pop-vanilla How do I verify my endpoint is correct?

Code

### **Production Patterns & Best Practices**

@msal-mtls-pop-vanilla What production-grade patterns should I follow? @msal-mtls-pop-vanilla Why should I use ConfigureAwait(false)? @msal-mtls-pop-vanilla How do I add CancellationToken support? @msal-mtls-pop-vanilla How do I implement IDisposable correctly? @msal-mtls-pop-vanilla What validation should I do with ArgumentNullException? @msal-mtls-pop-vanilla Show me the complete production helper class pattern @msal-mtls-pop-vanilla How do I add proper error handling? @msal-mtls-pop-vanilla What logging should I add for debugging?

Code

### **Credential Guard & Attestation**

@msal-mtls-pop-vanilla What is Credential Guard attestation? @msal-mtls-pop-vanilla How do I enable .WithAttestationSupport()? @msal-mtls-pop-vanilla Why should I use attestation support? @msal-mtls-pop-vanilla What's the security benefit of attestation?

Code

### **FIC Two-Leg Flow - Concepts**

@msal-mtls-pop-fic-two-leg What is FIC (Federated Identity Credentials)? @msal-mtls-pop-fic-two-leg What is a two-leg token exchange pattern? @msal-mtls-pop-fic-two-leg When should I use FIC two-leg instead of vanilla? @msal-mtls-pop-fic-two-leg What are the four valid FIC scenario combinations? @msal-mtls-pop-fic-two-leg Show me the FIC matrix (MSI/ConfApp × Bearer/PoP) @msal-mtls-pop-fic-two-leg What's the difference between vanilla and FIC flows? @msal-mtls-pop-fic-two-leg Why is FIC two-leg more complex?

Code

### **FIC Two-Leg Flow - Leg 1 (Acquisition)**

@msal-mtls-pop-fic-two-leg What happens in Leg 1? @msal-mtls-pop-fic-two-leg How do I acquire a Leg 1 token? @msal-mtls-pop-fic-two-leg Can I use MSI for Leg 1? @msal-mtls-pop-fic-two-leg Can I use Confidential Client for Leg 1? @msal-mtls-pop-fic-two-leg What resource should I request in Leg 1? @msal-mtls-pop-fic-two-leg Show me complete MSI Leg 1 code @msal-mtls-pop-fic-two-leg Show me complete Confidential Client Leg 1 code @msal-mtls-pop-fic-two-leg What's the api://AzureADTokenExchange resource? @msal-mtls-pop-fic-two-leg How do I extract the binding certificate from Leg 1 result?

Code

### **FIC Two-Leg Flow - Certificate Binding**

@msal-mtls-pop-fic-two-leg What is certificate binding between legs? @msal-mtls-pop-fic-two-leg Why must I pass TokenBindingCertificate to Leg 2? @msal-mtls-pop-fic-two-leg How do I include the certificate in Leg 2? @msal-mtls-pop-fic-two-leg What happens if I forget the certificate binding? @msal-mtls-pop-fic-two-leg How do I extract and pass the certificate safely? @msal-mtls-pop-fic-two-leg Is certificate binding required in all scenarios? @msal-mtls-pop-fic-two-leg Show me the complete certificate binding pattern

Code

### **FIC Two-Leg Flow - Leg 2 (Exchange)**

@msal-mtls-pop-fic-two-leg What happens in Leg 2? @msal-mtls-pop-fic-two-leg Why can only Confidential Client do Leg 2? @msal-mtls-pop-fic-two-leg Why can't MSI perform Leg 2? @msal-mtls-pop-fic-two-leg How do I acquire a Leg 2 token? @msal-mtls-pop-fic-two-leg What does .WithAzureRegion() do? @msal-mtls-pop-fic-two-leg Why is region specification important in Leg 2? @msal-mtls-pop-fic-two-leg Show me complete Leg 2 Confidential Client code @msal-mtls-pop-fic-two-leg How do I use ClientSignedAssertion in Leg 2? @msal-mtls-pop-fic-two-leg Can I use bearer tokens in Leg 2? @msal-mtls-pop-fic-two-leg Can I use mTLS PoP tokens in Leg 2?

Code

### **FIC Two-Leg Flow - Complete Scenarios**

@msal-mtls-pop-fic-two-leg Show me MSI Leg 1 → ConfApp Leg 2 with Bearer token @msal-mtls-pop-fic-two-leg Show me MSI Leg 1 → ConfApp Leg 2 with mTLS PoP token @msal-mtls-pop-fic-two-leg Show me ConfApp Leg 1 → ConfApp Leg 2 with Bearer token @msal-mtls-pop-fic-two-leg Show me ConfApp Leg 1 → ConfApp Leg 2 with mTLS PoP token @msal-mtls-pop-fic-two-leg Show me the complete end-to-end FIC flow @msal-mtls-pop-fic-two-leg How do I integrate Leg 1 and Leg 2 together? @msal-mtls-pop-fic-two-leg What's the complete flow from start to API call?

Code

### **FIC Two-Leg Flow - Helper Classes**

@msal-mtls-pop-fic-two-leg What helper classes are available? @msal-mtls-pop-fic-two-leg Show me FicLeg1Acquirer usage @msal-mtls-pop-fic-two-leg Show me FicAssertionProvider usage @msal-mtls-pop-fic-two-leg Show me FicLeg2Exchanger usage @msal-mtls-pop-fic-two-leg Show me ResourceCaller usage @msal-mtls-pop-fic-two-leg How do these helper classes work together? @msal-mtls-pop-fic-two-leg Can I use these classes as-is or do I need to modify them?

Code

### **Error Handling & Troubleshooting**

@msal-mtls-pop-vanilla What errors might I encounter? @msal-mtls-pop-vanilla How do I debug certificate binding issues? @msal-mtls-pop-vanilla What does "certificate not found" error mean? @msal-mtls-pop-vanilla How do I verify my token is actually a PoP token? @msal-mtls-pop-vanilla What should I check if my API call fails with mTLS PoP? @msal-mtls-pop-fic-two-leg What are common FIC two-leg errors? @msal-mtls-pop-fic-two-leg What does "certificate binding mismatch" mean? @msal-mtls-pop-fic-two-leg How do I troubleshoot token exchange failures?

Code

### **Testing & Validation**

@msal-mtls-pop-vanilla How do I test my mTLS PoP implementation? @msal-mtls-pop-vanilla Where are the integration tests? @msal-mtls-pop-vanilla Can I run the tests locally? @msal-mtls-pop-vanilla How do I verify my certificate binding is working? @msal-mtls-pop-vanilla What test scenarios should I cover? @msal-mtls-pop-fic-two-leg How do I test FIC two-leg flows? @msal-mtls-pop-fic-two-leg Are there E2E test examples?

Code

---

## 📖 Complete Reference Guide

### Key Concepts

**mTLS Proof-of-Possession (PoP)**
- Token bound to a specific client certificate
- More secure than bearer tokens
- Requires certificate in HTTP request
- Cannot be replayed without the certificate

**Vanilla Flow**
- Single-step direct token acquisition
- MSI (SAMI/UAMI) or Confidential Client
- Fastest path to mTLS PoP tokens
- Recommended for most use cases

**FIC Two-Leg Flow**
- First leg: Get token for `api://AzureADTokenExchange`
- Second leg: Exchange for actual resource access
- MSI can do Leg 1 only
- Confidential Client required for Leg 2
- Certificate binding between legs is critical

**Version Requirements**
- MSAL.NET 4.82.1+
- Namespaces: `Microsoft.Identity.Client.AppConfig`, `Microsoft.Identity.Client.KeyAttestation`

### Capability Comparison

| Feature | SAMI | UAMI | ConfApp |
|---------|------|------|---------|
| Vanilla mTLS PoP | ✅ | ✅ | ✅ |
| FIC Leg 1 | ✅ | ✅ | ✅ |
| FIC Leg 2 | ❌ | ❌ | ✅ |
| Custom Certificate | ❌ | ❌ | ✅ |
| Region Specification | ❌ | ❌ | ✅ |

### Endpoints

- **mTLS Graph**: `https://mtlstb.graph.microsoft.com`
- **Token Exchange**: `api://AzureADTokenExchange`
- **Token Scheme**: `mtls_pop` (authorization header)

---

## 🎓 Learning Paths

### **Path 1: New to mTLS PoP (30 minutes)**
1. `@msal-mtls-pop-guidance What is mTLS PoP?`
2. `@msal-mtls-pop-guidance Explain vanilla vs FIC flows`
3. `@msal-mtls-pop-vanilla How do I get started in 5 minutes?`
4. `@msal-mtls-pop-vanilla Show me SAMI example`
5. Implement SAMI example locally

### **Path 2: UAMI Implementation (20 minutes)**
1. `@msal-mtls-pop-guidance What are the three UAMI identifier types?`
2. `@msal-mtls-pop-vanilla Show me UAMI by ClientId example`
3. `@msal-mtls-pop-vanilla Show me UAMI by ResourceId example`
4. `@msal-mtls-pop-vanilla Show me UAMI by ObjectId example`
5. Choose and implement one identifier type

### **Path 3: Confidential Client Setup (25 minutes)**
1. `@msal-mtls-pop-vanilla What is Confidential Client?`
2. `@msal-mtls-pop-vanilla How do I load a certificate?`
3. `@msal-mtls-pop-vanilla Show me complete ConfApp example`
4. `@msal-mtls-pop-vanilla How do I handle certificate safely?`
5. Implement Confidential Client locally

### **Path 4: FIC Two-Leg Deep Dive (45 minutes)**
1. `@msal-mtls-pop-fic-two-leg What is FIC two-leg?`
2. `@msal-mtls-pop-fic-two-leg Show me the four scenario combinations`
3. `@msal-mtls-pop-fic-two-leg Show me MSI Leg 1 → ConfApp Leg 2`
4. `@msal-mtls-pop-fic-two-leg How does certificate binding work?`
5. `@msal-mtls-pop-fic-two-leg Show complete end-to-end flow`
6. Implement two-leg flow locally

### **Path 5: Production Ready (60 minutes)**
1. Complete one of the above paths
2. `@msal-mtls-pop-vanilla What production patterns should I follow?`
3. `@msal-mtls-pop-vanilla How do I add error handling?`
4. `@msal-mtls-pop-vanilla How do I add proper logging?`
5. Refactor your implementation with production patterns
6. Add comprehensive error handling

---

## 🚀 Pro Tips

✅ **Start with `@msal-mtls-pop-guidance`** if you're new
✅ **Use discovery prompts** from the "Discovery Prompts" section to explore
✅ **Follow a learning path** based on your use case
✅ **Enable `.WithAttestationSupport()`** for Credential Guard
✅ **Always check null** before adding certificates to HttpClientHandler
✅ **Use `ConfigureAwait(false)`** in production code
✅ **Add `CancellationToken`** support for better control
✅ **Implement `IDisposable`** correctly for HttpClient
✅ **Test locally first** before deploying to Azure

---

## 💬 Quick Chat Commands

Copy and paste these directly into VS Code Copilot Chat:

@msal-mtls-pop-guidance What can you tell me about mTLS PoP?

@msal-mtls-pop-vanilla Show me how to get started in 5 minutes

@msal-mtls-pop-fic-two-leg Show me the complete end-to-end flow

@workspace How do I choose between vanilla and FIC flows?

Code

---

## 📚 Available Helper Classes

### Vanilla Flow
- `VanillaMsiMtlsPop.cs` - MSI token acquisition wrapper
- `MtlsPopTokenAcquirer.cs` - Generic token acquisition
- `ResourceCaller.cs` - HTTP client configuration and API calls

### FIC Two-Leg Flow
- `FicLeg1Acquirer.cs` - Leg 1 token acquisition
- `FicAssertionProvider.cs` - Client assertion generation
- `FicLeg2Exchanger.cs` - Leg 2 token exchange
- `ResourceCaller.cs` - HTTP client configuration and API calls

---

## 🔗 Related Resources

- **PR #5733**: This implementation
- **Integration Tests**: `ClientCredentialsMtlsPopTests.cs`
- **MSAL.NET Docs**: Official documentation
- **Credential Guard**: Windows security feature
- **mTLS Spec**: RFC 8705 OAUTH 2.0 Mutual-TLS Client Authentication

---

## ❓ Still Have Questions?

Use the **Question Bank** above to discover answers. Most questions are already covered in one of the three skills!

**Happy exploring!** 🚀
