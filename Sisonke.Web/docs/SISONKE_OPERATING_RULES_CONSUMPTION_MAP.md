# Sisonke Operating Rules Consumption Map

## Purpose

This document maps how StokvelOperatingRules will be consumed by each Sisonke module.

The aim is to avoid hardcoding rules in individual modules.

## Claims Module

Rules to consume:
- EnableClaims
- RequireDeathCertificateForClaims
- RequireClaimDocuments
- BlockClaimsIfMemberInArrears
- BlockClaimsIfMemberSuspended
- DefaultClaimPayoutAmount
- MemberWaitingPeriodMonths
- DependentWaitingPeriodMonths

Used for:
- Claim eligibility assessment
- Secretary review support
- Chairperson approval decision support
- Default payout amount suggestion

## Contributions Module

Rules to consume:
- MonthlyContributionAmount
- ContributionDueDay
- GracePeriodDays
- AllowPartialPayments
- ChargeLatePaymentFine
- LatePaymentFineAmount

Used for:
- Monthly expected contribution generation
- Overdue detection
- Arrears calculation
- Late payment fine suggestion

## Dependents Module

Rules to consume:
- EnableDependents
- MaximumDependents
- DependentWaitingPeriodMonths
- RequireDependentIdNumber

Used for:
- Dependent capture validation
- Waiting period calculation
- Dependent eligibility for claims

## Attendance and Apologies Module

Rules to consume:
- EnableAttendanceTracking
- AbsenceReminderThreshold
- FormalWarningThreshold
- ExecutiveReviewThreshold
- ApologyDeadlineHoursBeforeMeeting
- ChargeLateApologyFine
- LateApologyFineAmount
- ChargeAbsenceWithoutApologyFine
- AbsenceWithoutApologyFineAmount

Used for:
- Warning generation
- Executive review escalation
- Late apology fine suggestion
- Absence without apology fine suggestion

## Meetings and Minutes Module

Rules to consume:
- EnableMeetings
- RequireMinutesApproval
- QuorumPercentage

Used for:
- Minutes approval workflow
- Quorum calculation
- Meeting governance validation

## Voting Module

Rules to consume:
- EnableVoting
- DefaultVotingApprovalThreshold
- AllowAnonymousVoting

Used for:
- Vote creation availability
- Vote result outcome calculation
- Anonymous voting behaviour

## Payouts

Rules to consume:
- RequireTreasurerConfirmationForPayouts
- DefaultClaimPayoutAmount
- EnableRotationalPayouts
- PayoutFrequency

Used for:
- Treasurer payout task control
- Default claim payout amount
- Future savings stokvel payout rotation

## Future Modules

### Grocery Module
Rules:
- EnableGroceryModule

### Investment Module
Rules:
- EnableInvestmentModule

### Property Module
Rules:
- EnablePropertyModule