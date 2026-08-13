# Paystack sandbox testing — Phase 3

Manual end-to-end walkthrough for the Paystack integration built in Phase 3. Run this against a
Paystack **test-mode** account only — never live keys.

## Before you start: unresolved items from API research

Phase 3's implementation was built from documentation research (the primary `paystack.com/docs`
site blocked automated access during that work — see the Phase 3 report). Two things need
confirming against your own Paystack test dashboard before trusting this flow end-to-end:

1. **Webhook event names.** `subscription.not_renew` and `subscription.enable`
   (`PaystackWebhookEventTypes`) were low-confidence — sources disagreed. Use the dashboard's
   "Send test webhook" feature (Settings → API Keys & Webhooks → your webhook URL → Send Test
   Webhook) to fire each event and confirm the exact `event` string Paystack actually sends.
   Update `PaystackWebhookEventTypes` if they differ — a wrong string only means that event is
   silently `Ignored`, never a processing error, so this is low-risk to get wrong initially.
2. **Webhook idempotency key.** Paystack's webhook envelope has no documented dedicated delivery
   ID, so `PaystackWebhookPayload.ResolveProviderEventId` falls back to `event:{data.id}`, then
   `event:{reference}`, then a hash of the raw body. Confirm with a real payload that `data.id` is
   present and stable for the event types you rely on (`charge.success`, `invoice.create`,
   `subscription.create`, `subscription.disable`).

## Prerequisites

- A Paystack test-mode account (South Africa / ZAR).
- Test secret key and webhook secret, set via `dotnet user-secrets` — **never** in
  `appsettings.json`:
  ```
  dotnet user-secrets set "Paystack:SecretKey" "sk_test_xxxxx"
  dotnet user-secrets set "Paystack:WebhookSecret" "whsec_xxxxx"
  dotnet user-secrets set "Paystack:PublicKey" "pk_test_xxxxx"
  ```
- A tunnel (ngrok or similar) exposing your local app so Paystack can reach
  `POST /api/billing/webhooks/paystack`, since webhooks cannot target `localhost` directly.
  Register the tunnel URL + `/api/billing/webhooks/paystack` as the webhook URL in the Paystack
  dashboard.
- A Paystack test card (e.g. `4084084084084081`, any future expiry, CVV `408`, PIN `0000`, OTP
  `123456` — check the current test card list in the dashboard, these change).
- `SeedData:Enabled=true` (or the dev safety-net seed) so `Data/Seed/BillingCatalogueSeed.cs` has
  populated the four plans and feature catalogue.

## Walkthrough

1. **Select a plan.** As a stokvel office bearer (`MemberAccessService.IsOfficeBearerAsync` must
   return true for the acting user — Chairperson/Secretary/Treasurer/StokvelAdmin/Creator/
   CommitteeMember), call `SubscriptionService.SelectPlanAsync(stokvelId, "GROWING", "v1",
   actorUserId)`.
   - Expect: `OrganisationSubscription.Status == PendingPaymentMethod`, `TrialStartedAt == null`.
     Confirm the trial clock has genuinely not started yet.

2. **Authorise a card.** Call `BeginCardAuthorisationAsync(stokvelId, email, name, phone,
   callbackUrl)`. Open the returned `AuthorisationUrl` in a browser and complete payment with the
   test card. This charges a small real amount (`PaystackOptions.CardVerificationAmountMinorUnits`,
   default R1.00) — confirmed with product 2026-08-03 as the correct mechanism since Paystack has
   no documented $0 verification hold.
   - Expect: the card page redirects to your `callbackUrl` with a `?reference=...` query param.

3. **Complete authorisation.** Call `CompleteCardAuthorisationAsync(stokvelId, reference)`.
   - Expect: `Status == Trialing`, `TrialStartedAt`/`TrialEndsAt` set 60 days apart,
     `NextBillingAt == TrialEndsAt`, a `SubscriptionPaymentMethod` row with brand/last4/expiry
     only (**verify no PAN/CVV anywhere** — check the database directly), and the R1.00
     verification charge refunded (check the Paystack dashboard's Transactions tab — should show
     `success` then a linked refund).
   - Try this step with a **non-reusable** test card/flow first (if your test account has one) to
     confirm the rejection path returns a clear message and does **not** advance past
     `PendingPaymentMethod`.

4. **Simulate trial end.** Paystack's `start_date` on the subscription was set 60 days out, so
   real-time testing can't wait 60 days. Options:
   - In Paystack test mode, some accounts support triggering an immediate charge on a subscription
     for testing — check your dashboard's subscription detail page for a "charge now" / test
     action.
   - Alternatively, directly call `PaystackBillingProvider` methods against a subscription created
     with a near-immediate `startDate` (e.g. `DateTime.UtcNow.AddMinutes(2)`) purely for this test,
     then wait for the real charge to fire and the webhooks to arrive.

5. **Charge success.** When the trial-end charge succeeds, Paystack should send `charge.success`
   (and typically `invoice.create` first). Confirm via the app:
   - `BillingWebhookEvent` row(s) with `SignatureValid = true`, `ProcessingStatus = Processed`.
   - `OrganisationSubscription.Status == Active`, `LastSuccessfulPaymentAt` set.
   - One `SubscriptionInvoice` (`Status = Paid`) with one `SubscriptionInvoiceLine` for the plan.
   - One `SubscriptionPayment` (`Status = Succeeded`) linked to that invoice.
   - A receipt PDF under `wwwroot/uploads/subscription-receipts/{stokvelId}/{reference}.pdf`.
   - Re-deliver the same webhook from the Paystack dashboard (or let Paystack's own retry fire) and
     confirm no duplicate invoice/payment/receipt is created.

6. **Invoice/receipt review.** Open the generated PDF. Confirm it says "Invoice" (not "Tax
   Invoice") unless `Invoicing:VatRegistered=true` is explicitly configured — and if it is, that
   the VAT number/rate shown match your actual configuration. **The wording is a functional
   placeholder, not reviewed compliant copy — do not use for a real invoice before the South
   African commercial attorney review flagged in `InvoicingOptions` and brief rule 10.**

## What this phase does not cover (by design — flagged in code as follow-ups)

- Actually re-billing a `PastDue` subscription after a payment-method update
  (`SubscriptionService.CompletePaymentMethodUpdateAsync`) — no confirmed single Paystack call for
  "retry this subscription's last failed invoice" was found; left for the Phase 5 dunning job.
- Provider-side plan swap on `ChangePlanAsync` upgrade/downgrade — the local
  entitlement-relevant state changes immediately/at period end as designed, but Paystack-side
  billing amount sync (disable old subscription + create new one on the new plan) is not wired up.
- A scheduled job to actually apply a `PendingPlanChangePlanId` downgrade when
  `PendingPlanChangeEffectiveAt` is reached — there is no billing-cycle rollover job yet.
