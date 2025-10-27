# Auth token behavior across **Production** and **Internal Preview** (Bearer vs PoP)

- **Production:** Only **Bearer** is supported. PoP endpoints are not deployed. If someone tries PoP, MSAL will not be able to get a POP token because source detection will not return IMDS V2. 
- **Internal Preview:** Only **PoP** is supported. If a Bearer token is present in the **MSAL** token cache (e.g., because the developer once used a Bearer flow), MSAL will always return a cached Bearer when the PoP API is called; we deliberately **throw** with a clear error telling the developer that preview only supports PoP.
- **Mixing Bearer and PoP is unsupported in all environments.** Use one or the other per environment.

---

## Environments & supported flows

### Production
- **Supported flow:** Bearer only.
- **PoP support:** Not available; PoP endpoints are not deployed.
- **Behavior:** Regardless of what the app *tries* to do, we end up giving a **Bearer** token.  
- **If someone calls a PoP endpoint:** It **fails** because the endpoint is not available.

### Internal Preview
- **Supported flow:** PoP only.
- **Bearer support:** Not supported for preview APIs.
- **Behavior today (preview enforcement):**
  - If the developer **ever** acquired a **Bearer** token first, **MSAL’s token cache** may return that Bearer token on subsequent calls—even if the code now calls the **PoP** acquisition path.
  - When a **Bearer** token is surfaced from the cache and the app then calls a **PoP** API, we **throw** with an explicit error stating that preview only supports PoP.

**Why this matters in Preview:** Preview requires PoP. If a Bearer token is pulled from the cache and used against a PoP API, we enforce policy by throwing.

---

## Behavior matrix

| Environment | First token acquired | API you call next | What SDK returns | Outcome | Notes |
|---|---|---|---|---|---|
| **Production** | Bearer | Bearer API | Bearer | ✅ Works | Normal, supported path. |
| **Production** | Bearer | **PoP** API | Bearer (from cache) | ❌ Fails | New endpoints not available in Prod. |
| **Production** | **PoP** (attempt) | PoP API | Bearer (Prod only supports Bearer) | ❌ Fails | PoP endpoints not available. |
| **Internal Preview** | **PoP** | PoP API | PoP | ✅ Works | Normal, supported path. |
| **Internal Preview** | **Bearer** | PoP API | **Bearer** (from MSAL cache) | ❌ **Throws** | Preview requires PoP; see error message below. |
| **Internal Preview** | **Bearer** | Bearer API | Bearer | ✅ Works | Normal, supported path. |

---

## Error surfaced in preview (when Bearer is found)

When a Bearer token is retrieved from the MSAL cache and a **Preview (PoP)** API is invoked, we throw with language along these lines:

> **Preview only supports PoP. A Bearer token was found in the MSAL cache.**  
> Clear your cache by restarting the application and acquire a PoP token, or switch to the PoP acquisition flow provided by the preview package.

---

## What developers should do

### Production
- **Do:** Use **Bearer** flows exclusively.
- **Don’t:** Call PoP API (they aren’t deployed and will fail). POP is in internal preview. 
- **Tip:** No special handling is needed; Production will always hand back Bearer.

### Internal Preview
- **Do:** Use **PoP** flows exclusively from the very first token acquisition.
- **Don’t:** Start with Bearer and then switch to PoP within the same app/cache.
- **If you accidentally used Bearer first (common case):**
  1. **Clear the MSAL token cache**.
  2. **Restart** and make the **first** acquisition a **PoP** token using the new preview package / PoP API.
  3. Avoid calling any Bearer acquisition APIs while testing preview.

**Production** supports **Bearer only**; PoP endpoints aren’t deployed, so PoP calls will fail. **Internal Preview** supports **PoP only**; Bearer is not supported. 

---

## FAQ

- **Can I mix Bearer and PoP in the same app?**  
  No. Mixing is unsupported. Use **Bearer** in **Prod**, **PoP** in **Preview**. 

- **Why do I still see a Bearer token in Preview after switching packages?**  
  Because **MSAL** may return a token from its cache. If you had acquired a beare token first, restart your app to clear MSALs in-memory cache and acquire a **PoP** token first.

- **What happens if I try PoP in Production?**  
  Calls to PoP endpoints **fail**—those endpoints are not available in Production. 

---

