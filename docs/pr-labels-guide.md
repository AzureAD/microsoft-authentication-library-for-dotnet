# Pull-Request Label Guide

> **Purpose**  
> Quick reference for **when** and **why** to apply the workflow-blocking labels used in MSAL repositories.

---

## 🚫 `do not merge`

| What it means | When to apply | Who removes it |
|---------------|---------------|----------------|
| **Prod-validation hold** – the PR **has passed pre-prod** checks and CI but still needs a final **smoke test in PROD** (or a scheduled production deployment window). | **Examples**<br>• Awaiting the next prod flight / ring deployment.<br>• Holding merge until live-site sanity tests succeed.<br>• Release manager requests a coordinated go-live. | **Author** or a **maintainer** after the prod test passes and CI is green. |

> **Tip:** Review feedback is welcome while this label is on, but **do not press “Merge.”**

---

## ⛔ `blocked`

| What it means | When to apply | Who removes it |
|---------------|---------------|----------------|
| **Pre-prod failure / external blocker** – the PR **cannot progress** because something discovered in pre-prod (or earlier) must be fixed or become available **before** prod validation can even start. | **Examples**<br>• Bugs surfaced in pre-prod that need code changes.<br>• Missing or mis-configured test environment.<br>• External dependency (partner library, service rollout, legal sign-off) not ready.<br>• Perf or security review uncovered issues. | **Author** or a **maintainer** once all blockers are resolved **and** CI is green. |

> **Heads-up:** Feel free to review the code, but **merging is off-limits** until this label is removed.

---

### Quick decision matrix

| Scenario | Label to apply |
|----------|----------------|
| All tests green in pre-prod; waiting for prod smoke test ✔️ | `do not merge` |
| Bugs reproduced in pre-prod; fix pending 🐞 | `blocked` |
| Service endpoint not rolled out yet ⏳ | `blocked` |
| Holding until next week’s coordinated release window 📆 | `do not merge` |

---

### Steps to remove a label

1. **Verify** CI is green.  
2. **Confirm** either  
   * prod smoke test succeeded (`do not merge`), **or**  
   * blocking issue is resolved (`blocked`).  
3. **Remove** the label and proceed with merge.
