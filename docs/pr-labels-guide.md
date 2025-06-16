# Pull-Request Label Guide

This short guide explains **when and why** to use the  workflow labels in MSAL repos.

---

## 🚫 `do not merge`

| What it means | When to apply | Who removes it |
|---------------|---------------|----------------|
| **Blocker** – the PR is **not ready to be merged** into `main`. | - Feature or fix is unfinished / in draft. <br>- External dependency (service rollout, partner library, legal sign-off) is still pending. <br>- Validation, perf, or security review is outstanding. <br>- The team has agreed to land the change in a future milestone. | The author **or** a maintainer, after all blockers are cleared and CI is green. |

> **Tip:** If you see this label on someone else’s PR, review feedback is welcome, but _do not_ press the “Merge” button even if approvals are in.

---

## ⛔ `blocked`

| What it means | When to apply | Who removes it |
|---------------|---------------|----------------|
| **Stop sign** – the PR **cannot be merged** yet. | - Feature or fix is unfinished / still in draft. <br>- External dependency (service rollout, partner library, legal sign-off) is pending. <br>- Validation, perf, or security review is outstanding. <br>- The change is slated for a future milestone. | The author **or** a maintainer, once every blocker is cleared and CI is green. |

> **Heads-up:** If you see this label on someone else’s PR, feel free to review, but **do not merge** until the label is gone.

---
