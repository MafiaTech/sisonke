# Sisonke.Web — Project Rules

These rules are permanent and non-negotiable. Any change touching them requires explicit user sign-off before implementation.

## 1. Approval workflow is immutable
The Member → Secretary → Chairperson → Treasurer approval chain must never be changed, reordered, bypassed, or short-circuited. New features (notifications, reports, admin tooling, etc.) may observe or read from this workflow but must never alter its state transitions or authorization order.

## 2. Schema changes: additive and guarded only
- All schema changes are additive and production-safe — no destructive drops/renames/type-narrowing.
- Guard schema changes against production with `DB_NAME()` checks so scripts/migrations are safe to run across environments.
- Never let EF generate destructive migrations. Where a raw SQL script is the source of truth, keep EF migrations aligned to it (empty migration + history-row pattern) rather than letting EF diff and rewrite the schema.

## 3. Never loosen authorization
Do not weaken, skip, or add bypasses to existing authorization/visibility checks. New features must reuse existing authorization rules (e.g., meeting visibility, member access) rather than introducing parallel or looser checks.

## 4. Burial stokvels: no loans/wallets
Burial stokvels must continue to hide and block all loan and wallet functionality/UI/APIs. Any new feature must preserve this exclusion — never surface loans or wallets for burial stokvels.

## 5. Email: Azure Communication Services only
All outbound email goes through Azure Communication Services (ACS `EmailClient`), not SMTP. Connection strings/secrets come from Azure App Settings/Key Vault — never committed to source control.

## 6. WhatsApp: disabled until Meta approval
WhatsApp messaging stays behind the `WhatsApp:Enabled=false` config flag until Meta message templates are approved. Do not flip this to `true` or bypass the flag.
