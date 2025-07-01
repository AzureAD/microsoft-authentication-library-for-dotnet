# Managed Identity – SLC Prototype Demo

This sample showcases **certificate rotation** and **token acquisition** against the **prototype SLC endpoint** using MSAL’s *experimental* Managed-Identity flow.

---

## ✨ What the demo does

1. **Detects the Managed-Identity source** (should be `Credential` when the SLC endpoint is live).  
2. **Creates an in-memory binding certificate (`CN=devicecert.mtlsauth.local`).  
3. **Raises `BindingCertificateUpdated`** whenever MSAL generates a fresh cert (7-day lifetime in this prototype).  
4. **Calls the SLC `/credential` endpoint** over mTLS and exchanges the credential for an **AAD access token** (default scope `https://vault.azure.net/`).  

---

## 🛠️ Prerequisites

| Requirement        | Version / Notes                            |
|--------------------|--------------------------------------------|
| .NET SDK           | **8.0.100** or later                      |
| MSAL .NET          | **4.72.3 preview** (included via `Microsoft.Identity.Client`) |
| Access to SLC host | VM/VMSS image with the prototype **SLC agent** enabled |
| Managed Identity   | System-assigned (or override in code)      |

> **Local run tip**: launch from an Azure VM/VMSS that already has the SLC agent bits; the demo will fail on a dev box without the endpoint.

---

## 🚀 Running the sample

```bash
dotnet restore
dotnet run --configuration Release
