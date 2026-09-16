# Sisonke Subscription Billing — Operations Runbook

Operational reference for the subscription billing system (`docs/billing/BRIEF.md`). Covers
configuration, webhook management, dunning recovery, refunds, and escalation. This is an
operational document, not a design document — see the brief and the Phase 1–5 code comments for
architecture rationale.

## 1. Configuration keys and where secrets live

All billing configuration binds from `appsettings.json` section names, overridden via Azure App
Service configuration (or local user-secrets in development) using the `Section__Key` env var
convention. **Secrets are never committed to source control** — `appsettings.json` /
`appsettings.Development.json` intentionally have no `Paystack` section.

| Section | Key | Purpose | Secret? |
|---|---|---|---|
| `SubscriptionPayments` | `DefaultProvider` | Provider used when a subscription has none; default `Paystack` | No |
| `SubscriptionPayments` | `TestMode` | When true, Paystack setup requires an `sk_test_` key and a verified `domain=test` transaction | No |
| `Paystack` | `SecretKey` | Paystack API bearer token and HMAC-SHA512 webhook signing key | **Yes** — Key Vault / App Service config only |
| `Paystack` | `WebhookSecret` | Legacy unused property retained for configuration compatibility; Paystack does not issue a separate webhook signing secret | Do not configure |
| `Paystack` | `PublicKey` | Safe to expose client-side; unused server-side today (card entry is Paystack-hosted) | No |
| `Paystack` | `BaseUrl` | Paystack API base (default `https://api.paystack.co`) | No |
| `Paystack` | `Currency` | Default `ZAR` | No |
| `Paystack` | `TrialDays` | Legacy option retained at default `60`; current trial lifecycle uses the centralized 60-day business rule | No |
| `Paystack` | `CardVerificationAmountMinorUnits` | Default `100` (R1.00) — real charge-then-refund used to capture a reusable authorisation | No |
| `Entitlements` | `LegacyGraceCutoverUtc` | Date legacy (`LegacyUnsubscribed`) organisations lose grace access and fall to the `NoSubscription` deny path. Defaults to `DateTime.MaxValue` (never expires) — **must be set explicitly** before the cutover is meant to bite | No |
| `Entitlements` | `LegacyEquivalentPlanCode` | Plan code legacy orgs are evaluated against during grace (default `GROWING`) | No |
| `Entitlements` | `CacheTtlMinutes` | Entitlement snapshot cache TTL (default 5) | No |
| `Entitlements` | `UsageRollupIntervalMinutes` | How often usage rollups persist (default 60) | No |
| `Entitlements` | `LegacyMigrationReminderIntervalDays` | Minimum days between reminder emails to the same org (default 7) | No |
| `Invoicing` | `VatRegistered`, `VatRatePercent`, `VatNumber`, `CompanyName`, `CompanyRegistrationNumber`, `CompanyAddress` | Invoice PDF letterhead facts — business data, not secrets | No |
| `Auth` | `RequireConfirmedAccount` | Gates whether onboarding's email-verification notice is enforced (soft) or purely informational | No |

**ASP.NET Core Data Protection keys** (used by `InvoiceDownloadTokenService` for signed,
24-hour invoice/receipt download links): uses the framework default key ring. In a
multi-instance Azure App Service deployment, confirm Data Protection keys are persisted to a
shared location (Azure Blob Storage / Key Vault) — if keys aren't shared across instances,
download links issued by one instance will fail validation on another.

Startup fails fast (outside Development) if `Paystack:SecretKey` is unset — see `Program.cs`.

## 2. Webhook URL registration

- **Endpoint:** `POST /api/billing/webhooks/paystack` (anonymous at the ASP.NET Core level —
  authentication is the HMAC-SHA512 signature check against `Paystack:SecretKey`, not a
  Sisonke user session).
- Register this URL (`https://<your-domain>/api/billing/webhooks/paystack`) in the Paystack
  dashboard under Settings → API Keys & Webhooks.
- Subscribe to at minimum: `charge.success`, `invoice.create`, `invoice.update`,
  `invoice.payment_failed`, `subscription.create`, `subscription.disable`, and the refund events
  (`refund.pending`, `refund.processing`, `refund.processed`, `refund.failed`).
- The official Paystack webhook/subscription documentation confirms
  `subscription.not_renew` and `subscription.expiring_cards`; no `subscription.enable` event is
  used. Compare signed test-mode deliveries with these mappings before live charging is enabled.

## 3. How to replay a webhook

Every signature-valid webhook is recorded in `BillingWebhookEvents` (`Provider`,
`ProviderEventId`, `EventType`, `RawPayload`, `SignatureValid`, `ProcessingStatus`,
`ErrorMessage`) **before** processing. The signed raw body is retained only while processing is
retryable; terminal rows retain a safe diagnostic summary rather than authorization/card/customer
payload data. Invalid signatures are rejected and not persisted. The handler returns `200` fast
for accepted events and replays (see `PaystackWebhookEndpoint` in `Program.cs`).

To replay a webhook that failed processing (`ProcessingStatus = Failed`) or that never got a
chance to process:

1. Find the row: `SELECT * FROM BillingWebhookEvents WHERE ProviderEventId = '<id>'` or filter by
   `EventType`/`ReceivedAt`.
2. Every webhook handler in `BillingWebhookProcessor` is idempotent by design — the simplest safe
   replay is resetting `ProcessingStatus` back to `Received` and `ProcessedAt`/`ErrorMessage` to
   `NULL` on that row; `BillingWebhookProcessingService` (the hosted service that polls for
   `Received` rows) will pick it up on its next tick.
3. Alternatively, use Paystack's dashboard to manually resend the original webhook — this arrives
   as a fresh HTTP request; the endpoint's `ProviderEventId` idempotency check means resending a
   *successfully processed* event is always safe (it's just recorded and ignored), so this is the
   lower-risk option when in doubt.
4. **Never** hand-edit `OrganisationSubscription`/`SubscriptionPayment`/`SubscriptionInvoice` rows
   to "simulate" what a webhook would have done — always go through the replay path so
   `ISubscriptionStateMachine` and the audit trail (`SubscriptionEvent`) stay consistent.

## 4. How to resolve a stuck dunning state

The dunning timeline (`DunningJob`, ticks every 30 min via `SubscriptionJobsHostedService`) drives
entirely off `OrganisationSubscription.DunningStartedAt` — day 1/3/5 retries while `PastDue`, day 7
→ `Restricted`, daily retries through day 13, day 14 → `Suspended`.

**A subscription is "stuck" if**: it's sat in `PastDue`/`Restricted` well past when a retry should
have fired, or dunning fields look inconsistent (e.g. `DunningStartedAt` set but `Status` already
`Active`).

Diagnosis:
1. Check `JobExecutionLocks` for a row with `JobName = 'DunningJob'` and a `LockExpiresAt` in the
   past but `LockToken` still set — indicates a crashed run that didn't release its lock. The lock
   is self-expiring (conditional `UPDATE ... WHERE LockExpiresAt < now`), so this resolves itself
   on the next tick; it is not something to manually clear.
2. Check `SubscriptionEvents` for the subscription, ordered by `OccurredAt` — the `Notes` field on
   each `RetryScheduled`/`PaymentFailed` event explains why a retry did or didn't fire (e.g. "last
   decline looked permanent" from `CardDeclineClassifier.IsPermanentDecline` — an unverified
   phrase-matcher on Paystack decline text; if it's incorrectly classifying a recoverable decline
   as permanent, that's a real bug to check first).
3. Check `SubscriptionPayments` for the current dunning episode (`CreatedAt >=
   DunningStartedAt`) — if the max attempt count was already reached, no further automatic retries
   fire until the day-7/14 status transitions take over; this is expected, not stuck.

**Manual recovery** (once the underlying cause — expired card, permanent decline, customer asked
to be reinstated — is understood): use the internal admin dashboard
(`/platform-admin/subscriptions/{id}` → Manual Actions):
- **Retry Charge** — forces one immediate charge attempt against the stored default payment
  method, independent of the job's schedule. Requires a reason; writes a `ManualRetryTriggered`
  `SubscriptionEvent`.
- **Reinstate** — only valid from `Suspended`; moves straight to `Active` without a charge (use
  when the customer has resolved things out-of-band, e.g. paid via EFT with proof).
- If the card itself is dead, the customer must update their payment method from `/subscription`
  first (`UpdatePaymentMethodAsync`) — a manual retry against a dead card will just fail again.

## 5. How to issue a manual refund

There is **no self-service refund UI** — `AdminBillingService` deliberately has no
`RefundPaymentAsync`-style method (refunds move real money and need a second set of eyes).

1. Identify the transaction reference: `SubscriptionPayments.ProviderReference` for the payment
   being refunded (or the verification-charge reference logged when a card was authorised, if
   refunding the R1 verification charge specifically — see `IBillingProvider.RefundTransactionAsync`,
   already used internally by `SubscriptionService.RefundVerificationChargeAsync` for the routine
   post-authorisation refund; this is a *different* case — a full/partial refund of an actual
   subscription charge).
2. Issue the refund directly from the **Paystack dashboard** (Transactions → find by reference →
   Refund), not through the Sisonke app.
3. Paystack sends `refund.pending` → `refund.processed` (or `refund.failed`) webhooks;
   `BillingWebhookProcessor` records these automatically — confirm the refund shows up against the
   right `SubscriptionPayment`/`SubscriptionEvent` after processing.
4. If the refund relates to a specific invoice that should no longer be collected, use the admin
   dashboard's **Waive Invoice** action afterward so the invoice's `Status` reflects reality
   (`Waived`) rather than sitting `Issued`/`Failed` indefinitely.
5. Record the reason and ticket/case reference in the admin dashboard's manual-action reason field
   even when the actual refund happened in Paystack — the `SubscriptionEvent` audit trail should
   explain *why*, not just log system-generated events.

## 6. Escalation

| Situation | Escalate to |
|---|---|
| Paystack API outage / webhook delivery failure | Paystack support (dashboard → Support), then confirm recovery via the replay procedure above |
| Suspected unauthorised/incorrect charge, customer dispute | Whoever holds the Sisonke Paystack merchant account (finance/product owner) — refunds are a business decision, not an engineering one |
| Legal question about the 60-day introductory period, CPA applicability, or terms wording | A South African commercial attorney — every place terms/trial-period wording is drafted in this codebase (`BillingTermsVersion.cs`, `InvoicingOptions.cs`, onboarding Step 3 copy) is explicitly flagged as pending that review; do not treat any of it as final |
| Data Protection key mismatch across instances (invoice download links failing intermittently) | Platform/infra owner — needs a shared key-ring storage location configured in Azure |
| Suspected PAN/CVV leak or any card-data-shaped field appearing anywhere | Treat as a security incident immediately — Sisonke's entire design assumes zero card data ever reaches this codebase (see `PlanCodeLiteralArchitectureTests`-style architecture tests and this repo's `CLAUDE.md` rules) |
