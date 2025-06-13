# KeyGuard Attestation Proof‑of‑Concept

> **Goal**  Demonstrate how to create a KeyGuard‑protected RSA key on Windows, attest it with Azure **Microsoft Azure Attestation** (MAA), receive a JWT, and prove the key can still perform cryptographic operations.

---

## 1️⃣ Prerequisites

| Requirement | Notes |
|-------------|-------|
| Windows **Server 2022** with TPM 2.0 (KeyGuard is Windows‑only; Linux/macOS not supported) | The demo queries TPM logs & AK certs. |
| KeyGuard with **Virtualisation‑Based Security (VBS)** | Must be enabled (see below) and the **Windows Security Service** running. |
| .NET 8 SDK + Visual Studio 2022 | Any recent SDK capable of building **C# 12**. |
| `AttestationClientLib.dll` | Native library shipped by the KeyGuard team. Place it next to the executable. |
| Access to an MAA instance | This sample uses the **EUS2 shared test endpoint**: `Replace with your own.` |

> 💡 **Tip** If the DLL depends on VC++ runtimes make sure they’re installed (see [Troubleshooting](#7️⃣ troubleshooting)).

---

## 💻 Enable KeyGuard VBS

Only applicable on **Windows**. These commands must be run **once per machine** with Administrator rights.

```powershell
reg add "HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard" /v "EnableVirtualizationBasedSecurity" /t REG_DWORD /d 1 /f
reg add "HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard" /v "RequirePlatformSecurityFeatures"       /t REG_DWORD /d 1 /f
reg add "HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard" /v "Locked"                                /t REG_DWORD /d 0 /f
reg add "HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity" /v "Enabled" /t REG_DWORD /d 1 /f
reg add "HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity" /v "Locked"  /t REG_DWORD /d 0 /f
```

> **Reboot** afterwards and verify the **Windows Security Service** is running:  
> ```powershell
> sc query SecurityHealthService
> ```  
> The state must be **RUNNING** every time you launch the PoC.

---

## 2️⃣ Build

```bash
# Clone / copy the sources
git clone https://.../KeyGuardDemo.git
cd KeyGuardDemo

# Restore packages from the **internal NuGet feed**
dotnet restore    # make sure your *NuGet.Config* points to the https://msazure.visualstudio.com/One/_artifacts/feed/Official/NuGet/Microsoft.Azure.Security.KeyGuardAttestation feed

# Build the managed wrapper + demo
dotnet build -c Release

# Output
./bin/Release/net8.0/KeyGuardDemo.exe
```

The project contains two assemblies:

| Project | Purpose |
|---------|---------|
| **KeyGuard.Attestation** | Managed façade around *AttestationClientLib.dll* (initialises the native DLL, exposes `TryAttest` etc.). |
| **KeyGuardDemo** | Console driver that creates the RSA key, calls the façade, and prints the resulting JWT. |

---

## 3️⃣ Run

```bash
cd bin/Release/net8.0
KeyGuardDemo.exe
```

Expected output:

```text
Creating NEW 'KeyGuardRSAKey' with KeyGuard protection.
Key created, size 2048 bits
KeyGuard flag set.
[Info] AttestatationClientLib ... Successfully attested and decrypted response.

Attestation JWT:
eyJ... (truncated)

Signature length: 256 bytes
```

---

## 4️⃣ What Happens Internally

1. **Key creation** – `CngKey.Create()` sets `NCryptUseVirtualIsolationFlag` & `NCryptUsePerBootKeyFlag`, producing an RSA key that lives in Virtual ISO (KeyGuard) storage.  
2. **DLL initialisation** – `AttestationClient`’s constructor registers a custom DLL resolver and calls `InitAttestationLib()` with a managed log callback.  
3. **Attestation** – The native library collects TPM & KeyGuard evidence and posts it to the MAA `/attest/keyguard` endpoint. On **HTTP 200** it decrypts the inner token and returns a JWT.  
4. **JWT display** – The console prints the raw token so you can paste it into [jwt.ms](https://jwt.ms) for inspection.  

---

## 5️⃣ Adjusting the Demo

| Scenario | Change |
|----------|--------|
| **Different MAA region** | Update `MaaEndpoint` constant in `Program.cs`. |

---

## 6️⃣ Cleaning Up

```powershell
# Remove the KeyGuard key
ncrypt.exe /deletekey /machinestore My /keyname KeyGuardRSAKey
```

Or call `CngKey.Open(...)` and dispose with `Delete()` in code.

---

## 7️⃣ Troubleshooting

| Symptom | Likely Cause | Remedy |
|---------|--------------|--------|
| `System.DllNotFoundException` | VC++ runtime or other native dependency missing. | Install **x64 VC++ redistributable** (latest). |

---

