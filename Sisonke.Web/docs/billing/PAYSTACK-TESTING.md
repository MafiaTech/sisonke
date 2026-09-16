# Paystack test-mode payment-method setup

This walkthrough verifies Paystack card setup for fees owed directly to Sisonke. It stops after
capturing a reusable authorization and refunding the ZAR 1 verification transaction. It must not
be used for stokvel/member funds or to trigger a monthly subscription charge.

Contract references (verified 2026-08-14):

- https://paystack.com/docs/api/transaction/
- https://paystack.com/docs/payments/recurring-charges/
- https://paystack.com/docs/payments/webhooks/
- https://paystack.com/docs/payments/subscriptions/

Paystack requires an initial successful card transaction before an authorization can be reused.
Its documented recommended minimum in South Africa is ZAR 1.00. Sisonke initializes that controlled
amount, verifies the transaction server-side, requires `authorization.reusable == true`, then asks
Paystack to refund the setup transaction. It does not create a `SubscriptionPayment` or collect a
monthly fee.

## Secure configuration

Run from `Sisonke.Web` using placeholders replaced with test credentials. Paystack signs webhooks
with the integration secret key; there is no separate webhook-signing secret in the current
Paystack contract.

```powershell
dotnet user-secrets set "SubscriptionPayments:DefaultProvider" "Paystack"
dotnet user-secrets set "SubscriptionPayments:TestMode" "true"
dotnet user-secrets set "Paystack:SecretKey" "sk_test_REPLACE_ME"
dotnet user-secrets set "Paystack:PublicKey" "pk_test_REPLACE_ME"
```

`Paystack:BaseUrl` defaults to `https://api.paystack.co`, `Paystack:Currency` defaults to `ZAR`,
and `Paystack:CardVerificationAmountMinorUnits` defaults to `100`. Do not override these for the
controlled test unless the integration code and official contract are reviewed again.

Expose the local HTTPS application through an approved development tunnel and register:

`POST https://YOUR-TUNNEL/api/billing/webhooks/paystack`

Localhost cannot receive Paystack webhooks.

## Controlled walkthrough

1. Start Sisonke with its normal Development profile and authenticate using a controlled stokvel
   office-bearer account.
2. Open a stokvel whose selected subscription is `Trialing`, then open `/subscription`.
3. Record the plan, status, `TrialStartedAt`, `TrialEndsAt`, payment count and current entitlements.
4. Select **Set up payment method**. Confirm Paystack Checkout opens in test mode.
5. Use only a currently documented Paystack test card from
   https://paystack.com/docs/payments/test-payments/ and complete its simulated authentication.
6. On return, Sisonke must verify the reference through `GET /transaction/verify/:reference`.
7. Verification must match the server-generated reference, ZAR 1.00 amount, ZAR currency, card
   channel, billing email, Paystack customer and test domain, and return a reusable authorization.
8. Confirm Billing & Subscription shows Paystack, readiness and masked card details only. It must
   not show the authorization code, customer code, access code, full card number, CVV, PIN, OTP or
   raw provider payload.
9. Confirm the subscription remains `Trialing`, both trial timestamps and plan are unchanged, and
   no `SubscriptionPayment` was created by setup.
10. Confirm the ZAR 1 setup transaction is refunded in the Paystack test dashboard.
11. Confirm the actual `charge.success` setup webhook was accepted with a valid HMAC-SHA512
    signature and then ignored as a subscription charge. Its terminal stored payload must contain
    only the safe diagnostic summary.
12. Replay that webhook. Confirm one `BillingWebhookEvent` exists for its provider event identity,
    no `SubscriptionPayment` exists for it, and the trial remains unchanged.
13. Repeat setup with another test card. Confirm the old method remains as non-default history,
    the new reusable authorization becomes the sole default, and no monthly charge occurs.

## Expected event mappings

Officially documented mappings used by Sisonke include `charge.success`, `invoice.create`,
`invoice.update`, `invoice.payment_failed`, `subscription.create`, `subscription.disable`,
`subscription.not_renew`, `subscription.expiring_cards`, `refund.pending`, `refund.processing`,
`refund.processed`, and `refund.failed`.

Capture the actual test-mode event names and safe structural evidence during the walkthrough. Only
change mappings when the signed test delivery or current official documentation proves a mismatch.

## Stop conditions

Stop without enabling live charging if the callback cannot be correlated, verification fields do
not match, authorization is not reusable, the domain is not `test`, the setup webhook changes the
trial, a monthly payment record is created, or secrets/provider tokens appear in UI or logs.
