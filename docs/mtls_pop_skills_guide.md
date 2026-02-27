# GitHub Copilot Chat - Better Prompts for MSAL.NET Skills

This guide helps internal developers ask better questions in GitHub Copilot Chat to get more relevant answers about MSAL.NET authentication patterns, mTLS Proof of Possession, and Federated Identity Credentials.

## 🚀 Quick Start

1. **Go to repo**: https://github.com/AzureAD/microsoft-authentication-library-for-dotnet
2. **Open Copilot Chat**: Click 💬 icon (top right) or press `Ctrl+Alt+I`
3. **Verify context**: Ensure `AzureAD/microsoft-authentication-library-for-dotnet` is selected
4. **Pick a prompt** from sections below
5. **Ask away!**

---

## 📚 Prompt Categories

### General mTLS Proof of Possession

Use these to understand the fundamentals:

```
How does mTLS Proof of Possession work in MSAL .NET?
```

```
What are the main differences between Bearer tokens and mTLS PoP tokens?
```

```
What are the requirements for using mTLS PoP in MSAL.NET?
```

```
Which frameworks and platforms support mTLS PoP in MSAL.NET?
```

---

### FIC (Federated Identity Credentials) with mTLS PoP

Use these for token exchange and two-leg flows:

#### Understanding FIC Two-Leg Flow

```
What is FIC two-leg flow with mTLS PoP?
```

```
Explain the two-leg token exchange pattern in MSAL.NET
```

```
What are the valid combinations for FIC mTLS PoP in MSAL.NET?
```

#### Leg 1: Token Acquisition

```
How do I acquire a token for api://AzureADTokenExchange with mTLS PoP?
```

```
Can I use Managed Identity for Leg 1 of FIC two-leg flow?
```

```
Can I use Confidential Client for Leg 1 of FIC two-leg flow?
```

```
How do I enable WithMtlsProofOfPossession() for Managed Identity?
```

#### Leg 2: Token Exchange

```
How do I implement Leg 2 of FIC two-leg flow with mTLS PoP?
```

```
How do I use the Leg 1 token as an assertion in Leg 2?
```

```
Can I use MSI (Managed Identity) for both Leg 1 and Leg 2?
```

```
Why does Leg 2 require Confidential Client instead of Managed Identity?
```

#### Certificate Binding

```
How does TokenBindingCertificate work in FIC two-leg flow?
```

```
How do I pass the binding certificate from Leg 1 to Leg 2?
```

```
Where do I get the BindingCertificate from the mTLS PoP token?
```

#### Practical Scenarios

```
How do I do token exchange in Kubernetes with mTLS PoP?
```

```
Show me how to implement FIC two-leg flow for workload identity federation
```

```
Can I exchange an MSI mTLS PoP token for a Bearer token in Leg 2?
```

```
Can I request mTLS PoP tokens in both Leg 1 and Leg 2?
```

---

### Implementation & Code Examples

Use these to get working code:

#### General Implementation

```
Show me the WithMtlsProofOfPossession() implementation
```

```
How do I configure the Azure region for mTLS PoP?
```

```
Show me how to set up a certificate for mTLS PoP
```

```
How do I enable SendX5C for mTLS PoP?
```

#### FIC Implementation

```
Show me a complete code example for FIC two-leg flow with mTLS PoP
```

```
How do I use ClientSignedAssertion with mTLS PoP in Leg 2?
```

```
Show me how to extract and use the BindingCertificate in Leg 2
```

#### Error Handling

```
What do I do if I get "MtlsCertificateNotProvided" error?
```

```
What does "MtlsPopWithoutRegion" error mean?
```

```
How do I troubleshoot mTLS PoP token acquisition failures?
```

```
What are common errors when using FIC two-leg flow with mTLS PoP?
```

---

### Credential Setup

Use these to configure credentials:

```
How do I set up a certificate for mTLS PoP in MSAL.NET?
```

```
What's the difference between standard certificates and SNI certificates for mTLS PoP?
```

```
How do I configure Federated Identity Credentials for use with mTLS PoP?
```

```
Can I use Managed Identity with mTLS PoP?
```

```
Which credential types support mTLS PoP?
```

---

### Decision Making & Best Practices

Use these to make informed decisions:

#### Choosing the Right Flow

```
Should I use Managed Identity or Confidential Client for mTLS PoP?
```

```
Which flow should I use for service-to-service communication with mTLS PoP?
```

```
When should I use FIC two-leg flow instead of direct Client Credentials?
```

#### Security & Best Practices

```
What are the security benefits of mTLS PoP over Bearer tokens?
```

```
What are the best practices for using mTLS PoP in production?
```

```
How do I implement token caching with mTLS PoP?
```

```
What's the recommended way to handle mTLS PoP certificate rotation?
```

---

### Validation & Review

Use these to validate your implementation:

```
Review this mTLS PoP implementation for correctness and best practices
```

```
Is this FIC two-leg flow implementation correct?
```

```
How do I validate that my mTLS PoP token is properly bound?
```

```
Are there any common pitfalls in FIC two-leg flow I should avoid?
```

---

## 🎯 Prompt Tips

### ✅ DO This

- **Be specific**: "What is FIC two-leg flow with mTLS PoP?" vs "How does FIC work?"
- **Use skill terms**: "Leg 1", "Leg 2", "TokenBindingCertificate", "assertion"
- **Ask follow-ups**: Copilot remembers context in the same chat
- **Reference errors**: "I'm getting MtlsPopWithoutRegion, what's the solution?"

### ❌ DON'T Do This

- ❌ Generic: "How does FIC work with mTLS pop?" → Use specific prompts instead
- ❌ Vague: "Tell me about mTLS" → Reference specific components
- ❌ Multi-part: Ask one question at a time for best results
- ❌ Off-topic: Keep questions related to MSAL.NET authentication

---

## 📋 Common Workflows

### Workflow 1: "I need to implement FIC two-leg flow with mTLS PoP"

1. Ask: `What is FIC two-leg flow with mTLS PoP?`
2. Ask: `Show me a complete code example for FIC two-leg flow with mTLS PoP`
3. Ask: `How do I pass the binding certificate from Leg 1 to Leg 2?`
4. Ask: `What are common errors when using FIC two-leg flow with mTLS PoP?`

### Workflow 2: "I'm getting an error with mTLS PoP"

1. Ask: `What does [ERROR_NAME] mean?`
2. Ask: `How do I troubleshoot this error?`
3. Ask: `Show me the correct implementation`
4. Ask: `Review my code for correctness`

### Workflow 3: "Should I use mTLS PoP for my scenario?"

1. Ask: `What are the security benefits of mTLS PoP over Bearer tokens?`
2. Ask: `Can I use mTLS PoP with [my credential type]?`
3. Ask: `What are the requirements for using mTLS PoP?`
4. Ask: `What are the best practices for using mTLS PoP?`

---

## 📖 Related Resources

- **Skill**: [FIC Two-Leg Flow Skill](.github/skills/msal-mtls-pop-fic-two-leg/SKILL.md)
- **Skill**: [mTLS PoP Vanilla Skill](.github/skills/msal-mtls-pop-vanilla/SKILL.md)
- **Skill**: [mTLS PoP Guidance Skill](.github/skills/msal-mtls-pop-guidance/SKILL.md)
- **Docs**: [mTLS PoP Design Doc](docs/sni_mtls_pop_token_design.md)
- **Tests**: [mTLS PoP Tests](tests/Microsoft.Identity.Test.Unit/PublicApiTests/MtlsPopTests.cs)

---

## 💡 Need Help?

If a prompt isn't returning good results:

1. **Try a more specific version** - Add context like credential type or scenario
2. **Ask a follow-up** - Reference previous answers with "Based on that, how do I..."
3. **Reference code** - Share error messages or code snippets in your question
4. **Break it down** - Ask one aspect at a time instead of multi-part questions

**Pro Tip**: Copilot searches the entire repo including design docs, code, tests, and skills. The more specific you are, the better results you get!

---

**Last updated**: 2026-02-27
