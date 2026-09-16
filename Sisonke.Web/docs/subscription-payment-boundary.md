# Subscription payment boundary

Sisonke's Paystack and Netcash integrations collect only fees owed directly to the Sisonke
operating business for use of the Sisonke SaaS platform.

Stokvels retain their own bank accounts and remain responsible for their own funds. Sisonke is
not a custodian, payment facilitator, intermediary, pooled account, beneficiary-selection service
or settlement account for stokvel/member money.

The subscription billing domain (`OrganisationSubscription`, `SubscriptionPayment`,
`SubscriptionPaymentMethod`, `SubscriptionInvoice`, `ISubscriptionPaymentProvider`,
`IBillingProvider` and provider adapters) must never process or represent:

- member contributions or savings;
- loan advances or repayments;
- rotational contributions or payouts;
- funeral benefits, claims or claim payouts;
- surplus-wallet or reserve movements;
- member-to-member transfers; or
- any other stokvel operational funds.

Operational financial modules remain workflow and ledger-recording facilities. Their records do
not authorize provider transactions and must not depend on subscription-provider services.

Subscription mandate payer details may be accepted transiently only when required to establish a
mandate for Sisonke platform subscription fees. Full account and identity data must not be logged,
rendered back, persisted, or generalized into a stokvel treasury/banking feature. The debit amount
must be derived server-side from the selected `SubscriptionPlan`; browsers cannot choose it. The
provider merchant/service key must belong to the Sisonke operating business, and subscription APIs
must expose no beneficiary selection, transfer or payout capability.

Any future feature that moves real member or stokvel money requires a separate architecture,
regulatory, legal, security and compliance review before code or provider reuse is considered.
