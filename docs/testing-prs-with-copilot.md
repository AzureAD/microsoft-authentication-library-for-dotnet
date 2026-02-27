# ✅ Testing Skills in VS Code (Copilot Chat)

Use this guide to validate a PR that adds or updates GitHub Copilot "skills" in the repo.

---

## 1) Checkout the PR branch

### Generic
```bash
git fetch origin pull/[PR_NUMBER]/head:[BRANCH_NAME]
git checkout [BRANCH_NAME]
```

### Example (PR 5733)
```bash
git fetch origin pull/5733/head:pr/5733
git checkout pr/5733
```

> **Tip (getting latest PR commits):**  
> If you are already checked out on `[BRANCH_NAME]`, Git may refuse to fetch *into the same branch name*.  
> Use one of these instead:

**Safe update (merge latest PR head):**
```bash
git fetch origin pull/[PR_NUMBER]/head
git merge FETCH_HEAD
```

**Force exact PR state (overwrites local changes/commits on your branch):**
```bash
git fetch origin pull/[PR_NUMBER]/head
git reset --hard FETCH_HEAD
```

---

## 2) Open VS Code
From the repo folder:

```bash
code .
```

---

## 3) Open GitHub Copilot Chat
- **Windows/Linux:** `Ctrl + Shift + I`
- **Mac:** `Cmd + Shift + I`

### Strongly recommended: force Copilot to use your local repo
In Copilot Chat:
1. Click **Add Context…**
2. Select **Workspace**

If your PR adds "skills" under a specific folder (e.g., `.github/...`), you can also add that folder explicitly.

---

## 4) Skill test prompts (copy/paste)

### Test 1 — Vanilla SAMI PoP
```text
Using the msal-mtls-pop-vanilla skill, show me how to acquire an mTLS PoP token with system-assigned managed identity (SAMI) for Microsoft Graph.
```

### Test 2 — Vanilla UAMI by ClientId
```text
Using the msal-mtls-pop-vanilla skill, show me how to acquire an mTLS PoP token with user-assigned managed identity using client ID 6325cd32-9911-41f3-819c-416cdf9104e7.
```

### Test 3 — Vanilla UAMI by ResourceId
```text
Using the msal-mtls-pop-vanilla skill, show me how to acquire an mTLS PoP token with user-assigned managed identity using resource ID /subscriptions/c1686c51-b717-4fe0-9af3-24a20a41fb0c/resourcegroups/MSIV2-Testing-MSALNET/providers/Microsoft.ManagedIdentity/userAssignedIdentities/msiv2uami.
```

### Test 4 — Vanilla UAMI by ObjectId
```text
Using the msal-mtls-pop-vanilla skill, show me how to acquire an mTLS PoP token with user-assigned managed identity using object ID ecb2ad92-3e30-4505-b79f-ac640d069f24.
```

### Test 5 — Vanilla Confidential Client PoP
```text
Using the msal-mtls-pop-vanilla skill, show me how to acquire an mTLS PoP token with a confidential client app using certificate-based authentication.
```

### Test 6 — FIC (MSI → ConfApp → Bearer)
```text
Using the msal-mtls-pop-fic-two-leg skill, show me a two-leg token exchange where Leg 1 uses MSI to get a PoP token for api://AzureADTokenExchange, and Leg 2 uses a confidential client app to exchange that token for a Bearer token to Microsoft Graph.
```

### Test 7 — FIC (MSI → ConfApp → mTLS PoP)
```text
Using the msal-mtls-pop-fic-two-leg skill, show me a two-leg token exchange where Leg 1 uses MSI to get a PoP token for api://AzureADTokenExchange, and Leg 2 uses a confidential client app to exchange that token for an mTLS PoP token to Microsoft Graph.
```

### Test 8 — FIC (ConfApp → ConfApp → Bearer)
```text
Using the msal-mtls-pop-fic-two-leg skill, show me a two-leg token exchange where both Leg 1 and Leg 2 use confidential client apps, with the final token being a Bearer token.
```

### Test 9 — FIC (ConfApp → ConfApp → mTLS PoP)
```text
Using the msal-mtls-pop-fic-two-leg skill, show me a two-leg token exchange where both legs use confidential client apps, with the final token being an mTLS PoP token.
```

### Test 10 — Shared Guidance
```text
Using the msal-mtls-pop-guidance skill, what's the difference between vanilla and FIC flows? When should I use each?
```

---

## 5) Verify responses (expected behavior)

Copilot should:

- ✅ Reference correct helper classes (e.g., `ResourceCaller.cs`, `FicLeg1Acquirer.cs`, etc.)
- ✅ Include complete code examples with proper `async/await`
- ✅ Show `.WithMtlsProofOfPossession()` **and** `.WithAttestationSupport()`
- ✅ Include required namespaces:
  - `using Microsoft.Identity.Client.AppConfig;`
  - `using Microsoft.Identity.Client.KeyAttestation;`
- ✅ Show token types and certificate binding
- ✅ Avoid **MSI Leg 2** scenarios (**Leg 2 must be ConfApp only**)
- ✅ Reference **real** UAMI IDs (client ID / resource ID / object ID)

### Red flags
- ❌ Invented helper classes or file paths
- ❌ Generic MSAL examples that ignore repo helpers
- ❌ MSI shown in Leg 2 for FIC
- ❌ Missing required namespaces or missing attestation/PoP configuration

---

## 6) Troubleshooting

### Copilot is not using the local repo
1. In Copilot Chat → **Add Context… → Workspace**
2. Use an `@workspace` prompt to force repo search:
```text
@workspace Search this repo for "msal-mtls-pop" and show the exact file paths where it appears.
```
3. Reload VS Code:
- `Ctrl+Shift+P` → **Developer: Reload Window**

### Git error: "refusing to fetch into branch ... checked out"
Use:
```bash
git fetch origin pull/[PR_NUMBER]/head
git merge FETCH_HEAD
```
(or `git reset --hard FETCH_HEAD` if you want to overwrite)

---

## 7) Quick copy/paste (end-to-end)
Example using PR 5733:

```bash
cd C:\code
git clone https://github.com/AzureAD/microsoft-authentication-library-for-dotnet.git
cd microsoft-authentication-library-for-dotnet
git fetch origin pull/5733/head:pr/5733
git checkout pr/5733
code .
git branch --show-current
git log -1 --oneline
```
