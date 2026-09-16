# Sisonke Subscription Billing & Plan Entitlements — Implementation Brief

**How to use these files:** paste `00-OVERVIEW-AND-GUARDRAILS.md` into Claude Code first (or save it as `docs/billing/BRIEF.md` in the repo and tell Claude to read it). Then run phases 1–5 one at a time, in order. Each phase file is a self-contained prompt. Do not run two phases in one session.

**Stack:** ASP.NET Core, EF Core, SQL Server. Payment provider: Paystack (ZAR).

---

## Context for Claude

Sisonke is a stokvel (South African savings-society) management platform by PEO Capital Holdings. We are adding a paid subscription model with four plans, a 60-day free trial that starts only on explicit authorisation, Paystack recurring card billing, a central entitlement system that gates features per plan, and a staged dunning process for failed payments.

Before writing code, read the existing solution and match its conventions: project layout, DI registration, EF `DbContext` and migration style, background-job runner (Hangfire / `IHostedService` / Quartz — use whatever already exists), notification/email service, authorisation policies, and the existing `Stokvel` (organisation) aggregate and its ID type. **Report what you found and confirm the integration points before generating files.**

---

## Non-negotiable rules

1. **Sisonke never stores card numbers, expiry dates or CVVs.** Card authorisation is hosted by Paystack. We persist only the returned customer code, authorisation code, subscription code, card brand, last-4 and expiry month/year for display.
2. **The backend entitlement check is authoritative.** The UI may hide or disable actions, but every gated operation must also be checked in the application service and the API endpoint. Background jobs check before executing paid functionality.
3. **No scattered plan-name conditionals.** `if (plan == "Professional")` anywhere in feature code is a defect. All gating goes through `IEntitlementService` and `FeatureCodes` constants.
4. **The trial never starts implicitly.** It starts only after an authorised office bearer selects a plan, accepts the subscription terms, and Paystack confirms a successful card authorisation. Existing organisations are not auto-enrolled.
5. **Sisonke subscription revenue and member contribution payments are separate ledgers.** Payer/payee, merchant account and accounting records must not be co-mingled. Never post a subscription invoice into stokvel contribution accounting, or vice versa.
6. **Recurring billing uses tokenised cards only.** Ozow EFT and Capitec Pay are once-off methods; they are available for member contributions but must not be offered as a subscription payment method.
7. **Never delete or hard-purge organisation data because a subscription failed.** Suspension is read-only access, not deletion.
8. **All money is `decimal(18,2)`, currency `ZAR`, all timestamps UTC (`DateTimeOffset` or `datetime2` UTC), stored with explicit precision.**
9. **Every webhook handler is idempotent** and verifies the Paystack HMAC-SHA512 signature before processing.
10. Terms, cancellation wording and renewal notices are drafted as configurable content — flag in code comments that they require review by a South African commercial attorney (CPA implications) before launch. Do not hard-code final legal copy.

---

## Plan matrix (source of truth)

| | Starter | Growing Society | Professional Society | Enterprise & Federations |
|---|---|---|---|---|
| Code | `STARTER` | `GROWING` | `PROFESSIONAL` | `ENTERPRISE` |
| Monthly (ZAR) | 99 | 249 | 499 | from 999 (custom) |
| Annual (later phase) | 990 | 2490 | 4990 | contract |
| Max active members | 30 | 100 | 300 | unlimited (`null`) |
| Max administrators | 1 | 5 | 15 | custom |
| Max schemes | 1 | 1 | multiple | multiple/branches |
| Custom pricing | false | false | false | true |

### Feature catalogue

Feature codes and per-plan values. `DataType` is `Boolean`, `Numeric` or `Enum`. Numeric `null` limit = unlimited.

| Feature code | Category | Type | Starter | Growing | Professional | Enterprise |
|---|---|---|---|---|---|---|
| `MAX_MEMBERS` | Limits | Numeric | 30 | 100 | 300 | null |
| `MAX_ADMINISTRATORS` | Limits | Numeric | 1 | 5 | 15 | null |
| `MAX_SCHEMES` | Limits | Numeric | 1 | 1 | 5 | null |
| `STORAGE_MB` | Limits | Numeric | 100 | 1000 | 5000 | null |
| `MEMBER_MANAGEMENT` | Core | Boolean | true | true | true | true |
| `DEPENDENT_MANAGEMENT` | Core | Boolean | true | true | true | true |
| `MEETINGS` | Governance | Boolean | true | true | true | true |
| `CONTRIBUTION_TRACKING` | Finance | Boolean | true | true | true | true |
| `TASK_MANAGEMENT` | Core | Boolean | true | true | true | true |
| `CLAIMS_BASIC` | Claims | Boolean | true | true | true | true |
| `CLAIMS_FULL_WORKFLOW` | Claims | Boolean | false | true | true | true |
| `ROTATIONAL_STOKVEL` | Core | Boolean | false | true | true | true |
| `LOANS_AND_WITHDRAWALS` | Finance | Boolean | false | true | true | true |
| `VOTING` | Governance | Boolean | false | true | true | true |
| `ATTENDANCE_WARNINGS` | Governance | Boolean | false | true | true | true |
| `AI_MEETING_MINUTES` | Governance | Boolean | false | true | true | true |
| `EMAIL_NOTIFICATIONS` | Notifications | Boolean | true | true | true | true |
| `WHATSAPP_NOTIFICATIONS` | Notifications | Boolean | false | true | true | true |
| `DOCUMENT_STORAGE` | Core | Boolean | false | true | true | true |
| `EXPORT_EXCEL_PDF` | Reporting | Boolean | false | true | true | true |
| `STANDARD_REPORTING` | Reporting | Boolean | true | true | true | true |
| `FINANCE_SUMMARIES_ARREARS` | Finance | Boolean | false | true | true | true |
| `ADVANCED_REPORTING` | Reporting | Boolean | false | false | true | true |
| `MANUAL_PAYMENT_RECORDING` | Payments | Boolean | true | true | true | true |
| `PAYMENT_LINKS` | Payments | Boolean | false | true | true | true |
| `TREASURER_RECONCILIATION_TOOLS` | Payments | Boolean | false | true | true | true |
| `ONLINE_MEMBER_PAYMENTS` | Payments | Enum | `None` | `Limited` | `Full` | `Full` |
| `AUTOMATED_RECURRING_COLLECTIONS` | Payments | Boolean | false | false | true | true |
| `AUTOMATED_RECONCILIATION` | Payments | Boolean | false | false | true | true |
| `FAILED_PAYMENT_FOLLOWUP` | Payments | Boolean | false | false | true | true |
| `SETTLEMENT_REPORTS` | Payments | Boolean | false | false | true | true |
| `CONTROLLED_PAYOUTS` | Finance | Boolean | false | false | true | true |
| `MULTIPLE_CONTRIBUTION_STRUCTURES` | Finance | Boolean | false | false | true | true |
| `SURPLUS_WALLET` | Finance | Boolean | false | false | true | true |
| `CONFIGURABLE_APPROVAL_WORKFLOWS` | Governance | Boolean | false | false | true | true |
| `API_ACCESS` | Platform | Boolean | false | false | true (ready) | true |
| `MULTIPLE_SCHEMES` | Platform | Boolean | false | false | false | true |
| `FUNERAL_PARLOUR_MANAGEMENT` | Platform | Boolean | false | false | false | true |
| `CONSOLIDATED_REPORTING` | Reporting | Boolean | false | false | false | true |
| `CUSTOM_BRANDING` | Platform | Boolean | false | false | false | true |
| `CUSTOM_DOMAIN` | Platform | Boolean | false | false | false | true |
| `ROLE_CUSTOMISATION` | Platform | Boolean | false | false | false | true |
| `DATA_MIGRATION_SUPPORT` | Support | Boolean | false | false | false | true |
| `WHITE_LABEL` | Platform | Boolean | false | false | false | true |
| `SUPPORT_TIER` | Support | Enum | `Community` | `Priority` | `PriorityWhatsApp` | `Dedicated` |

Gateway transaction fees for member collections are billed separately from the subscription and must be disclosed on the pricing page — they are never bundled into the plan price.

---

## Subscription status machine

`LegacyUnsubscribed → PendingPaymentMethod → Trialing → Active → PastDue → Restricted → Suspended`, plus `Cancelled` and `Expired`.

- **LegacyUnsubscribed** — existing organisation that has not yet opted into a plan. Full legacy access until the migration cutover date (configurable); shows the opt-in banner.
- **PendingPaymentMethod** — plan selected, terms accepted, card authorisation not yet confirmed. No trial clock running.
- **Trialing** — 60 days, full access to the selected plan's features.
- **Active** — billed successfully, in period.
- **PastDue** — days 0–6 after a failed debit. Full access + warning banner. Automatic retries day 1, 3, 5.
- **Restricted** — days 7–13. Read/export/billing only; creating new records is blocked.
- **Suspended** — day 14+. Billing page, subscription page, data export, support and read-only access for authorised administrators only.
- **Cancelled** — cancelled by the customer; runs to period end if `CancelAtPeriodEnd`.
- **Expired** — trial ended with no successful first debit and dunning exhausted.

### Restricted-state operation matrix

Allowed in `Restricted`: view existing records, update payment details, download reports, access invoices, contact support.
Blocked in `Restricted` and `Suspended`: add members, create claims, start loans, create meetings, send bulk notifications, process payouts, create new financial transactions.

---

## Dunning timeline

| Day | Action |
|---|---|
| 0 | Debit fails → `PastDue`, notify billing admin, show banner |
| 1 | Automatic retry |
| 3 | Second retry |
| 5 | Final retry |
| 7 | → `Restricted` |
| 14 | → `Suspended` |

## Trial notification schedule

Trial start (confirmation, plan, first billing date) · 30 days remaining · 14 days · 7 days · 3 days · 1 day · debit successful (invoice + receipt) · debit failed (retry instructions).

---

## Definition of done for the whole milestone

- Four plans and the full feature catalogue are seeded and idempotently re-seedable.
- A new stokvel can complete the 4-step onboarding and end in `Trialing` with a Paystack subscription code stored.
- An existing stokvel sits in `LegacyUnsubscribed` and only moves to `Trialing` after explicit opt-in and card authorisation.
- Every gated feature is enforced in the application service layer, with unit tests proving the API rejects a request the UI would have hidden.
- Paystack webhooks are signature-verified, idempotent, and drive status transitions, invoices and receipts.
- The dunning state machine is covered by tests at day 0, 1, 3, 5, 7 and 14.
- No card PAN/CVV appears in any entity, log, or database column.
