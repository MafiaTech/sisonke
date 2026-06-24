using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Services;

public sealed class LoansWalletService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    ILogger<LoansWalletService> logger)
{
    private static readonly MemberLoanStatus[] BlockingLoanStatuses =
        [MemberLoanStatus.Submitted, MemberLoanStatus.PendingApproval, MemberLoanStatus.Approved,
         MemberLoanStatus.DisbursementPending, MemberLoanStatus.Active, MemberLoanStatus.Overdue];
    private static readonly SurplusWithdrawalStatus[] PendingWithdrawalStatuses =
        [SurplusWithdrawalStatus.Submitted, SurplusWithdrawalStatus.PendingApproval,
         SurplusWithdrawalStatus.Approved, SurplusWithdrawalStatus.PaymentPending];

    public async Task<LoansWalletPageState?> GetPageStateAsync(Guid stokvelId, string currentUserId)
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        var stokvel = await context.Stokvels.AsNoTracking()
            .Where(x => x.Id == stokvelId && x.IsActive && !x.IsDeleted)
            .Select(x => new Stokvel { Id = x.Id, TenantId = x.TenantId, Name = x.Name, Archetype = x.Archetype, Type = x.Type, EnableLending = x.EnableLending, IsActive = x.IsActive })
            .SingleOrDefaultAsync();
        if (stokvel is null) return null;

        var member = await context.Members.AsNoTracking()
            .Where(x => x.TenantId == stokvel.TenantId && x.ApplicationUserId == currentUserId)
            .OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        if (member is null) return new(stokvel, null, null, [], [], null, [], [], false, false, false, false, "No linked membership was found.");

        var allowed = IsFeatureAllowed(stokvel);
        var role = member.DefaultRole;
        var canConfigure = IsConfigurationRole(role);
        var canViewAll = IsOfficeBearer(role);
        var canApprove = role == SisonkeRole.Chairperson;
        var canConfirm = role == SisonkeRole.Treasurer;
        var config = await context.StokvelLoanConfigurations.AsNoTracking()
            .Where(x => x.StokvelId == stokvelId && x.IsActive).OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync();

        var loansQuery = context.MemberLoans.AsNoTracking().Include(x => x.Member)
            .Where(x => x.StokvelId == stokvelId && x.IsActive);
        if (!canViewAll) loansQuery = loansQuery.Where(x => x.MemberId == member.Id);
        var loans = await loansQuery.OrderByDescending(x => x.RequestedAt).ToListAsync();
        var loanIds = loans.Select(x => x.Id).ToArray();
        var repayments = await context.MemberLoanRepayments.AsNoTracking()
            .Where(x => loanIds.Contains(x.LoanId) && x.IsActive).OrderBy(x => x.DueDate).ToListAsync();
        var wallet = await context.MemberSurplusWallets.AsNoTracking()
            .FirstOrDefaultAsync(x => x.StokvelId == stokvelId && x.MemberId == member.Id && x.IsActive);
        var transactions = wallet is null ? [] : await context.MemberSurplusWalletTransactions.AsNoTracking()
            .Where(x => x.WalletId == wallet.Id).OrderByDescending(x => x.CreatedAt).Take(20).ToListAsync();
        var withdrawalsQuery = context.MemberSurplusWithdrawalRequests.AsNoTracking().Include(x => x.Member)
            .Where(x => x.StokvelId == stokvelId && x.IsActive);
        if (!canViewAll) withdrawalsQuery = withdrawalsQuery.Where(x => x.MemberId == member.Id);
        var withdrawals = await withdrawalsQuery.OrderByDescending(x => x.RequestedAt).ToListAsync();
        var eligibility = await GetEligibilityMessageAsync(context, stokvel, member, config);
        return new(stokvel, member, config, loans, repayments, wallet, transactions, withdrawals,
            canConfigure, canViewAll, canApprove, canConfirm, allowed ? eligibility : "Loans and surplus wallets are not available for Burial Societies.");
    }

    public async Task<LoanConfigurationResult> SaveConfigurationAsync(
        Guid stokvelId, StokvelLoanConfiguration model, string currentUserId)
    {
        var errors = ValidateConfiguration(model);
        await using var context = await dbFactory.CreateDbContextAsync();
        var access = await GetAccessAsync(context, stokvelId, currentUserId);
        if (access.Stokvel is null || !IsFeatureAllowed(access.Stokvel)) errors.Add("Loans cannot be configured for a Burial Society.");
        if (!IsConfigurationRole(access.Role)) errors.Add("You are not authorised to configure loan rules.");
        if (errors.Count > 0) return LoanConfigurationResult.Failed(errors);
        var now = DateTime.UtcNow;
        var active = await context.StokvelLoanConfigurations.Where(x => x.StokvelId == stokvelId && x.IsActive).ToListAsync();
        foreach (var old in active) { old.IsActive = false; old.UpdatedAt = now; old.UpdatedBy = currentUserId; }
        var created = new StokvelLoanConfiguration
        {
            Id = Guid.NewGuid(),
            StokvelId = stokvelId,
            LoansEnabled = model.LoansEnabled,
            MinLoanAmount = model.MinLoanAmount,
            MaxLoanAmount = model.MaxLoanAmount,
            MaxRepaymentMonths = model.MaxRepaymentMonths,
            DefaultRepaymentMonths = model.DefaultRepaymentMonths,
            LoanInterestType = model.LoanInterestType,
            LoanInterestRate = model.LoanInterestRate,
            LateRepaymentFineType = model.LateRepaymentFineType,
            LateRepaymentFineAmount = model.LateRepaymentFineAmount,
            GracePeriodDays = model.GracePeriodDays,
            FreezePeriodAfterFullRepaymentDays = model.FreezePeriodAfterFullRepaymentDays <= 0 ? 30 : model.FreezePeriodAfterFullRepaymentDays,
            RequireChairpersonApproval = model.RequireChairpersonApproval,
            RequireTreasurerDisbursementConfirmation = model.RequireTreasurerDisbursementConfirmation,
            IsActive = true,
            CreatedAt = now,
            CreatedBy = currentUserId
        };
        context.StokvelLoanConfigurations.Add(created);
        await context.SaveChangesAsync();
        return LoanConfigurationResult.Succeeded(created);
    }

    public async Task<FinanceOperationResult> RequestLoanAsync(
        Guid stokvelId, string currentUserId, decimal amount, int repaymentMonths, string reason)
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        var access = await GetAccessAsync(context, stokvelId, currentUserId);
        if (access.Stokvel is null || access.Member is null) return FinanceOperationResult.Failed("Membership was not found.");
        var config = await context.StokvelLoanConfigurations.AsNoTracking().Where(x => x.StokvelId == stokvelId && x.IsActive).OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync();
        var errors = new List<string>();
        if (!IsFeatureAllowed(access.Stokvel)) errors.Add("Loans are not available for Burial Societies.");
        if (config?.LoansEnabled != true) errors.Add("Loans are not enabled for this stokvel.");
        if (!IsEligibleMember(access.Member)) errors.Add("Suspended, expelled, deceased, or inactive members cannot request loans.");
        if (config is not null && (amount < config.MinLoanAmount || amount > config.MaxLoanAmount)) errors.Add("Requested amount is outside the configured loan limits.");
        if (config is not null && (repaymentMonths <= 0 || repaymentMonths > config.MaxRepaymentMonths)) errors.Add("Repayment period is outside the configured limit.");
        if (string.IsNullOrWhiteSpace(reason)) errors.Add("Loan reason is required.");
        if (await context.MemberLoans.AnyAsync(x => x.StokvelId == stokvelId && x.MemberId == access.Member.Id && x.IsActive && BlockingLoanStatuses.Contains(x.LoanStatus))) errors.Add("You already have an active or pending loan.");
        var nextDate = await context.MemberLoans.Where(x => x.StokvelId == stokvelId && x.MemberId == access.Member.Id && x.LoanStatus == MemberLoanStatus.FullyRepaid)
            .OrderByDescending(x => x.FullyRepaidAt).Select(x => x.NextEligibleLoanDate).FirstOrDefaultAsync();
        if (nextDate > DateTime.UtcNow) errors.Add($"You are in a loan freeze period until {nextDate:dd MMM yyyy}.");
        if (errors.Count > 0) return FinanceOperationResult.Failed(errors);
        var now = DateTime.UtcNow;
        var loan = new MemberLoan
        {
            Id = Guid.NewGuid(),
            StokvelId = stokvelId,
            MemberId = access.Member.Id,
            RequestedAmount = amount,
            RepaymentMonths = repaymentMonths,
            RequestReason = reason.Trim(),
            LoanStatus = config!.RequireChairpersonApproval
                ? MemberLoanStatus.PendingApproval
                : config.RequireTreasurerDisbursementConfirmation
                    ? MemberLoanStatus.DisbursementPending
                    : MemberLoanStatus.Active,
            RequestedAt = now,
            RequestedBy = currentUserId,
            IsActive = true,
            CreatedAt = now,
            CreatedBy = currentUserId
        };
        if (!config.RequireChairpersonApproval)
        {
            ApplyLoanApproval(loan, amount, repaymentMonths, config, now, currentUserId);
        }
        if (loan.LoanStatus == MemberLoanStatus.Active)
        {
            ActivateLoanAndCreateSchedule(context, loan, now, currentUserId, "Loan activated automatically by stokvel loan rules.");
        }
        context.MemberLoans.Add(loan);
        await context.SaveChangesAsync();
        return FinanceOperationResult.Succeeded();
    }

    public async Task<FinanceOperationResult> ApproveLoanAsync(Guid loanId, string currentUserId, decimal approvedAmount, int months)
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        var loan = await context.MemberLoans.FirstOrDefaultAsync(x => x.Id == loanId && x.IsActive);
        if (loan is null) return FinanceOperationResult.Failed("Loan not found.");
        if (await GetRoleAsync(context, loan.StokvelId, currentUserId) != SisonkeRole.Chairperson) return FinanceOperationResult.Failed("Only the Chairperson can approve loans.");
        var config = await context.StokvelLoanConfigurations.AsNoTracking().Where(x => x.StokvelId == loan.StokvelId && x.IsActive).OrderByDescending(x => x.CreatedAt).FirstAsync();
        if (loan.LoanStatus != MemberLoanStatus.PendingApproval) return FinanceOperationResult.Failed("Loan is not awaiting approval.");
        if (approvedAmount < config.MinLoanAmount || approvedAmount > config.MaxLoanAmount) return FinanceOperationResult.Failed("Approved amount is outside configured limits.");
        if (months <= 0 || months > config.MaxRepaymentMonths) return FinanceOperationResult.Failed("Repayment period is outside configured limits.");
        var now = DateTime.UtcNow;
        ApplyLoanApproval(loan, approvedAmount, months, config, now, currentUserId);
        loan.LoanStatus = config.RequireTreasurerDisbursementConfirmation ? MemberLoanStatus.DisbursementPending : MemberLoanStatus.Active;
        if (loan.LoanStatus == MemberLoanStatus.Active)
        {
            ActivateLoanAndCreateSchedule(context, loan, now, currentUserId, "Loan activated automatically after Chairperson approval.");
        }
        await context.SaveChangesAsync(); return FinanceOperationResult.Succeeded();
    }

    public async Task<FinanceOperationResult> RejectLoanAsync(Guid loanId, string currentUserId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason)) return FinanceOperationResult.Failed("Rejection reason is required.");
        await using var context = await dbFactory.CreateDbContextAsync();
        var loan = await context.MemberLoans.FirstOrDefaultAsync(x => x.Id == loanId && x.IsActive);
        if (loan is null) return FinanceOperationResult.Failed("Loan not found.");
        if (await GetRoleAsync(context, loan.StokvelId, currentUserId) != SisonkeRole.Chairperson) return FinanceOperationResult.Failed("Only the Chairperson can reject loans.");
        if (loan.LoanStatus != MemberLoanStatus.PendingApproval) return FinanceOperationResult.Failed("Loan is not awaiting approval.");
        loan.LoanStatus = MemberLoanStatus.Rejected; loan.RejectionReason = reason.Trim(); loan.RejectedAt = DateTime.UtcNow;
        loan.RejectedByChairpersonId = currentUserId; loan.UpdatedAt = DateTime.UtcNow; loan.UpdatedBy = currentUserId;
        await context.SaveChangesAsync(); return FinanceOperationResult.Succeeded();
    }

    public async Task<FinanceOperationResult> ConfirmDisbursementAsync(Guid loanId, string currentUserId, PaymentMethod? method, string? reference, DateTime? date, string? notes)
    {
        if (method is null || date is null || string.IsNullOrWhiteSpace(reference)) return FinanceOperationResult.Failed("Method, reference, and disbursement date are required.");
        await using var context = await dbFactory.CreateDbContextAsync(); await using var tx = await context.Database.BeginTransactionAsync();
        var loan = await context.MemberLoans.Include(x => x.Repayments).FirstOrDefaultAsync(x => x.Id == loanId && x.IsActive);
        if (loan is null) return FinanceOperationResult.Failed("Loan not found.");
        if (await GetRoleAsync(context, loan.StokvelId, currentUserId) != SisonkeRole.Treasurer) return FinanceOperationResult.Failed("Only the Treasurer can confirm loan disbursement.");
        if (loan.LoanStatus is not (MemberLoanStatus.DisbursementPending or MemberLoanStatus.Approved)) return FinanceOperationResult.Failed("Loan is not awaiting disbursement.");
        var now = DateTime.UtcNow;
        loan.LoanStatus = MemberLoanStatus.Active; loan.DisbursementMethod = method; loan.DisbursementReference = reference.Trim();
        loan.DisbursedAt = date; loan.DisbursedByTreasurerId = currentUserId; loan.Notes = Trim(notes);
        ActivateLoanAndCreateSchedule(context, loan, date.Value, currentUserId, null);
        await context.SaveChangesAsync(); await tx.CommitAsync(); return FinanceOperationResult.Succeeded();
    }

    public async Task<FinanceOperationResult> ConfirmRepaymentAsync(Guid repaymentId, string currentUserId, decimal paidAmount, DateTime? paymentDate, PaymentMethod? method, string? reference, bool waive, bool applyFine, string? notes)
    {
        if (!waive && (paidAmount < 0 || paymentDate is null || method is null)) return FinanceOperationResult.Failed("Amount, payment date, and method are required.");
        await using var context = await dbFactory.CreateDbContextAsync(); await using var tx = await context.Database.BeginTransactionAsync();
        var repayment = await context.MemberLoanRepayments.Include(x => x.Loan).FirstOrDefaultAsync(x => x.Id == repaymentId && x.IsActive);
        if (repayment is null) return FinanceOperationResult.Failed("Repayment not found.");
        if (await GetRoleAsync(context, repayment.StokvelId, currentUserId) != SisonkeRole.Treasurer) return FinanceOperationResult.Failed("Only the Treasurer can confirm loan repayments.");
        if (repayment.Loan.LoanStatus is not (MemberLoanStatus.Active or MemberLoanStatus.Overdue)) return FinanceOperationResult.Failed("Loan is not active.");
        var config = await context.StokvelLoanConfigurations.AsNoTracking().Where(x => x.StokvelId == repayment.StokvelId && x.IsActive).OrderByDescending(x => x.CreatedAt).FirstAsync();
        var late = paymentDate.HasValue && paymentDate.Value.Date > repayment.DueDate.AddDays(config.GracePeriodDays).Date;
        var fine = late && applyFine ? CalculateFine(repayment.ExpectedAmount, config) : 0;
        repayment.PaidAmount = waive ? 0 : paidAmount; repayment.PaymentDate = paymentDate; repayment.PaymentMethod = waive ? null : method;
        repayment.PaymentReference = Trim(reference); repayment.FineAmount = fine; repayment.Notes = Trim(notes);
        repayment.PaymentStatus = waive ? LoanRepaymentStatus.Waived : paidAmount >= repayment.ExpectedAmount + fine ? (late ? LoanRepaymentStatus.Late : LoanRepaymentStatus.Paid) : paidAmount > 0 ? LoanRepaymentStatus.PartiallyPaid : LoanRepaymentStatus.Pending;
        repayment.ConfirmedAt = DateTime.UtcNow; repayment.ConfirmedByTreasurerId = currentUserId; repayment.UpdatedAt = DateTime.UtcNow; repayment.UpdatedBy = currentUserId;
        await context.SaveChangesAsync();
        var schedule = await context.MemberLoanRepayments.Where(x => x.LoanId == repayment.LoanId && x.IsActive).ToListAsync();
        var settled = schedule.Sum(x => x.PaymentStatus == LoanRepaymentStatus.Waived ? x.ExpectedAmount : Math.Min(x.PaidAmount, x.ExpectedAmount));
        repayment.Loan.OutstandingBalance = Math.Max(0, repayment.Loan.TotalRepayableAmount - settled);
        if (schedule.All(x => x.PaymentStatus is LoanRepaymentStatus.Paid or LoanRepaymentStatus.Late or LoanRepaymentStatus.Waived) && repayment.Loan.OutstandingBalance <= 0)
        {
            var repaid = DateTime.UtcNow; repayment.Loan.LoanStatus = MemberLoanStatus.FullyRepaid; repayment.Loan.FullyRepaidAt = repaid;
            repayment.Loan.NextEligibleLoanDate = repaid.AddDays(config.FreezePeriodAfterFullRepaymentDays);
        }
        repayment.Loan.UpdatedAt = DateTime.UtcNow; repayment.Loan.UpdatedBy = currentUserId;
        await context.SaveChangesAsync(); await tx.CommitAsync(); return FinanceOperationResult.Succeeded();
    }

    public async Task CreditContributionOverpaymentAsync(Guid stokvelId, Guid memberId, Guid paymentId, decimal amount, string currentUserId)
    {
        if (amount < 0) return;
        await using var context = await dbFactory.CreateDbContextAsync(); await using var tx = await context.Database.BeginTransactionAsync();
        var stokvel = await context.Stokvels.AsNoTracking().FirstOrDefaultAsync(x => x.Id == stokvelId);
        if (stokvel is null || !IsFeatureAllowed(stokvel)) return;
        var existingAmount = await GetContributionOverpaymentBalanceAsync(context, paymentId);
        var delta = amount - existingAmount;
        if (delta == 0) return;
        var wallet = await context.MemberSurplusWallets.FirstOrDefaultAsync(x => x.StokvelId == stokvelId && x.MemberId == memberId && x.IsActive);
        if (wallet is null) { wallet = new() { Id = Guid.NewGuid(), StokvelId = stokvelId, MemberId = memberId, IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = currentUserId }; context.MemberSurplusWallets.Add(wallet); }
        if (delta < 0 && wallet.AvailableBalance < Math.Abs(delta)) return;
        wallet.AvailableBalance += delta;
        wallet.TotalCredits = Math.Max(0, wallet.TotalCredits + delta);
        wallet.UpdatedAt = DateTime.UtcNow; wallet.UpdatedBy = currentUserId;
        context.MemberSurplusWalletTransactions.Add(new() { Id = Guid.NewGuid(), StokvelId = stokvelId, WalletId = wallet.Id, MemberId = memberId, TransactionType = delta > 0 ? WalletTransactionType.Credit : WalletTransactionType.Debit, Amount = Math.Abs(delta), BalanceAfterTransaction = wallet.AvailableBalance, SourceType = WalletTransactionSourceType.ContributionOverpayment, SourceReferenceId = paymentId, Description = delta > 0 ? "Contribution overpayment" : "Contribution overpayment adjustment", CreatedAt = DateTime.UtcNow, CreatedBy = currentUserId });
        await context.SaveChangesAsync(); await tx.CommitAsync();
        logger.LogInformation("Surplus wallet adjusted from contribution payment {PaymentId}", paymentId);
    }

    public async Task<FinanceOperationResult> RequestWithdrawalAsync(Guid stokvelId, string currentUserId, decimal amount, string reason)
    {
        await using var context = await dbFactory.CreateDbContextAsync(); var access = await GetAccessAsync(context, stokvelId, currentUserId);
        if (access.Stokvel is null || access.Member is null) return FinanceOperationResult.Failed("Membership was not found.");
        if (!IsFeatureAllowed(access.Stokvel)) return FinanceOperationResult.Failed("Surplus withdrawals are not available for Burial Societies.");
        if (!IsEligibleMember(access.Member)) return FinanceOperationResult.Failed("Your membership is not eligible for withdrawals.");
        var wallet = await context.MemberSurplusWallets.FirstOrDefaultAsync(x => x.StokvelId == stokvelId && x.MemberId == access.Member.Id && x.IsActive);
        if (wallet is null || amount <= 0 || amount > wallet.AvailableBalance) return FinanceOperationResult.Failed("Requested amount exceeds the available surplus balance.");
        if (string.IsNullOrWhiteSpace(reason)) return FinanceOperationResult.Failed("Withdrawal reason is required.");
        if (await context.MemberSurplusWithdrawalRequests.AnyAsync(x => x.StokvelId == stokvelId && x.MemberId == access.Member.Id && x.IsActive && PendingWithdrawalStatuses.Contains(x.WithdrawalStatus))) return FinanceOperationResult.Failed("You already have a pending withdrawal request.");
        var now = DateTime.UtcNow; var request = new MemberSurplusWithdrawalRequest { Id = Guid.NewGuid(), StokvelId = stokvelId, MemberId = access.Member.Id, WalletId = wallet.Id, RequestedAmount = amount, RequestReason = reason.Trim(), WithdrawalStatus = SurplusWithdrawalStatus.PendingApproval, RequestedAt = now, RequestedBy = currentUserId, IsActive = true, CreatedAt = now, CreatedBy = currentUserId };
        context.MemberSurplusWithdrawalRequests.Add(request);
        context.MemberSurplusWalletTransactions.Add(new() { Id = Guid.NewGuid(), StokvelId = stokvelId, WalletId = wallet.Id, MemberId = wallet.MemberId, TransactionType = WalletTransactionType.WithdrawalRequested, Amount = amount, BalanceAfterTransaction = wallet.AvailableBalance, SourceType = WalletTransactionSourceType.Withdrawal, SourceReferenceId = request.Id, Description = "Withdrawal requested", CreatedAt = now, CreatedBy = currentUserId });
        await context.SaveChangesAsync(); return FinanceOperationResult.Succeeded();
    }

    public Task<FinanceOperationResult> ApproveWithdrawalAsync(Guid id, string userId) => DecideWithdrawalAsync(id, userId, true, null);
    public Task<FinanceOperationResult> RejectWithdrawalAsync(Guid id, string userId, string reason) => DecideWithdrawalAsync(id, userId, false, reason);

    public async Task<FinanceOperationResult> ConfirmWithdrawalPaidAsync(Guid id, string userId, PaymentMethod? method, string? reference, DateTime? paidAt, string? notes)
    {
        if (method is null || paidAt is null || string.IsNullOrWhiteSpace(reference)) return FinanceOperationResult.Failed("Method, reference, and paid date are required.");
        await using var context = await dbFactory.CreateDbContextAsync(); await using var tx = await context.Database.BeginTransactionAsync();
        var request = await context.MemberSurplusWithdrawalRequests.Include(x => x.Wallet).FirstOrDefaultAsync(x => x.Id == id && x.IsActive);
        if (request is null) return FinanceOperationResult.Failed("Withdrawal request not found.");
        if (await GetRoleAsync(context, request.StokvelId, userId) != SisonkeRole.Treasurer) return FinanceOperationResult.Failed("Only the Treasurer can confirm withdrawal payment.");
        if (request.WithdrawalStatus != SurplusWithdrawalStatus.PaymentPending) return FinanceOperationResult.Failed("Withdrawal is not approved for payment.");
        if (request.Wallet.AvailableBalance < request.RequestedAmount) return FinanceOperationResult.Failed("The wallet no longer has sufficient balance.");
        request.Wallet.AvailableBalance -= request.RequestedAmount; request.Wallet.TotalWithdrawals += request.RequestedAmount; request.Wallet.UpdatedAt = DateTime.UtcNow; request.Wallet.UpdatedBy = userId;
        request.WithdrawalStatus = SurplusWithdrawalStatus.Paid; request.PaidAt = paidAt; request.PaidByTreasurerId = userId; request.PaymentMethod = method; request.PaymentReference = reference.Trim(); request.Notes = Trim(notes); request.UpdatedAt = DateTime.UtcNow; request.UpdatedBy = userId;
        context.MemberSurplusWalletTransactions.Add(new() { Id = Guid.NewGuid(), StokvelId = request.StokvelId, WalletId = request.WalletId, MemberId = request.MemberId, TransactionType = WalletTransactionType.WithdrawalPaid, Amount = request.RequestedAmount, BalanceAfterTransaction = request.Wallet.AvailableBalance, SourceType = WalletTransactionSourceType.Withdrawal, SourceReferenceId = request.Id, Description = "Withdrawal paid", CreatedAt = DateTime.UtcNow, CreatedBy = userId });
        await context.SaveChangesAsync(); await tx.CommitAsync(); return FinanceOperationResult.Succeeded();
    }

    public async Task<LoansWalletTaskCounts> GetTaskCountsAsync(Guid stokvelId)
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        var today = DateTime.UtcNow.Date;
        return new(
            await context.MemberLoans.CountAsync(x => x.StokvelId == stokvelId && x.IsActive && x.LoanStatus == MemberLoanStatus.PendingApproval),
            await context.MemberLoans.CountAsync(x => x.StokvelId == stokvelId && x.IsActive && x.LoanStatus == MemberLoanStatus.DisbursementPending),
            await context.MemberLoanRepayments.CountAsync(x => x.StokvelId == stokvelId && x.IsActive &&
                x.DueDate <= today &&
                (x.PaymentStatus == LoanRepaymentStatus.Pending ||
                 x.PaymentStatus == LoanRepaymentStatus.PartiallyPaid ||
                 x.PaymentStatus == LoanRepaymentStatus.Missed)),
            await context.MemberSurplusWithdrawalRequests.CountAsync(x => x.StokvelId == stokvelId && x.IsActive && x.WithdrawalStatus == SurplusWithdrawalStatus.PendingApproval),
            await context.MemberSurplusWithdrawalRequests.CountAsync(x => x.StokvelId == stokvelId && x.IsActive && x.WithdrawalStatus == SurplusWithdrawalStatus.PaymentPending));
    }

    public async Task<StokvelLoanConfiguration?> GetActiveLoanConfigurationAsync(Guid stokvelId)
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        return await context.StokvelLoanConfigurations.AsNoTracking()
            .Where(x => x.StokvelId == stokvelId && x.IsActive)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> IsLoanFeatureAvailableForStokvelAsync(Guid stokvelId)
    {
        await using var context = await dbFactory.CreateDbContextAsync();
        var archetype = await context.Stokvels.AsNoTracking()
            .Where(x => x.Id == stokvelId && x.IsActive && !x.IsDeleted)
            .Select(x => (StokvelArchetype?)x.Archetype)
            .FirstOrDefaultAsync();
        return archetype is not null && archetype != StokvelArchetype.BurialSociety;
    }

    private async Task<FinanceOperationResult> DecideWithdrawalAsync(Guid id, string userId, bool approve, string? reason)
    {
        if (!approve && string.IsNullOrWhiteSpace(reason)) return FinanceOperationResult.Failed("Rejection reason is required.");
        await using var context = await dbFactory.CreateDbContextAsync(); var request = await context.MemberSurplusWithdrawalRequests.Include(x => x.Wallet).FirstOrDefaultAsync(x => x.Id == id && x.IsActive);
        if (request is null) return FinanceOperationResult.Failed("Withdrawal request not found.");
        if (await GetRoleAsync(context, request.StokvelId, userId) != SisonkeRole.Chairperson) return FinanceOperationResult.Failed("Only the Chairperson can approve or reject withdrawals.");
        if (request.WithdrawalStatus != SurplusWithdrawalStatus.PendingApproval) return FinanceOperationResult.Failed("Withdrawal is not awaiting approval.");
        var now = DateTime.UtcNow;
        if (approve) { if (request.Wallet.AvailableBalance < request.RequestedAmount) return FinanceOperationResult.Failed("Wallet balance is insufficient."); request.WithdrawalStatus = SurplusWithdrawalStatus.PaymentPending; request.ApprovedAt = now; request.ApprovedByChairpersonId = userId; }
        else { request.WithdrawalStatus = SurplusWithdrawalStatus.Rejected; request.RejectedAt = now; request.RejectedByChairpersonId = userId; request.RejectionReason = reason!.Trim(); context.MemberSurplusWalletTransactions.Add(new() { Id = Guid.NewGuid(), StokvelId = request.StokvelId, WalletId = request.WalletId, MemberId = request.MemberId, TransactionType = WalletTransactionType.WithdrawalRejected, Amount = request.RequestedAmount, BalanceAfterTransaction = request.Wallet.AvailableBalance, SourceType = WalletTransactionSourceType.Withdrawal, SourceReferenceId = request.Id, Description = "Withdrawal rejected", CreatedAt = now, CreatedBy = userId }); }
        request.UpdatedAt = now; request.UpdatedBy = userId; await context.SaveChangesAsync(); return FinanceOperationResult.Succeeded();
    }

    private static async Task<(Stokvel? Stokvel, Member? Member, SisonkeRole? Role)> GetAccessAsync(ApplicationDbContext context, Guid stokvelId, string userId)
    {
        var stokvel = await context.Stokvels.AsNoTracking().FirstOrDefaultAsync(x => x.Id == stokvelId && x.IsActive && !x.IsDeleted);
        if (stokvel is null) return (null, null, null);
        var member = await context.Members.AsNoTracking().FirstOrDefaultAsync(x => x.TenantId == stokvel.TenantId && x.ApplicationUserId == userId);
        return (stokvel, member, member?.DefaultRole);
    }
    private static async Task<SisonkeRole?> GetRoleAsync(ApplicationDbContext context, Guid stokvelId, string userId) => (await GetAccessAsync(context, stokvelId, userId)).Role;
    private static bool IsFeatureAllowed(Stokvel stokvel) => stokvel.Archetype != StokvelArchetype.BurialSociety;
    private static bool IsOfficeBearer(SisonkeRole? role) => role is SisonkeRole.Creator or SisonkeRole.StokvelAdmin or SisonkeRole.Chairperson or SisonkeRole.Secretary or SisonkeRole.Treasurer;
    private static bool IsConfigurationRole(SisonkeRole? role) => IsOfficeBearer(role) || role is SisonkeRole.PlatformSuperAdmin or SisonkeRole.PlatformSupportAdmin;
    private static bool IsEligibleMember(Member member) => member.Status == MemberStatus.Active && member.GovernanceStatus == MemberGovernanceStatus.Active && !member.IsDeceased && member.SuspendedAt is null && member.ExpelledAt is null;
    private static decimal CalculateTotal(decimal amount, StokvelLoanConfiguration config) => config.LoanInterestType switch { LoanInterestType.FixedAmount => amount + config.LoanInterestRate, LoanInterestType.Percentage => Math.Round(amount * (1 + config.LoanInterestRate / 100), 2), _ => amount };
    private static decimal CalculateFine(decimal expected, StokvelLoanConfiguration config) => config.LateRepaymentFineType switch { LatePenaltyType.FixedAmount => config.LateRepaymentFineAmount ?? 0, LatePenaltyType.Percentage => Math.Round(expected * (config.LateRepaymentFineAmount ?? 0) / 100, 2), _ => 0 };
    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static void ApplyLoanApproval(MemberLoan loan, decimal approvedAmount, int months, StokvelLoanConfiguration config, DateTime approvedAt, string approvedBy)
    {
        var total = CalculateTotal(approvedAmount, config);
        loan.ApprovedAmount = approvedAmount;
        loan.RepaymentMonths = months;
        loan.TotalRepayableAmount = total;
        loan.MonthlyRepaymentAmount = Math.Round(total / months, 2);
        loan.OutstandingBalance = total;
        loan.ApprovedAt = approvedAt;
        loan.ApprovedByChairpersonId = approvedBy;
        loan.UpdatedAt = approvedAt;
        loan.UpdatedBy = approvedBy;
    }

    private static void ActivateLoanAndCreateSchedule(ApplicationDbContext context, MemberLoan loan, DateTime disbursedAt, string currentUserId, string? note)
    {
        var now = DateTime.UtcNow;
        var dueStart = disbursedAt.AddMonths(1).Date;
        loan.LoanStatus = MemberLoanStatus.Active;
        loan.DisbursedAt ??= disbursedAt;
        loan.DueStartDate = dueStart;
        loan.ExpectedFinalPaymentDate = dueStart.AddMonths(loan.RepaymentMonths - 1);
        loan.Notes = Trim(note ?? loan.Notes);
        loan.UpdatedAt = now;
        loan.UpdatedBy = currentUserId;

        if (loan.Repayments.Count > 0)
        {
            return;
        }

        for (var i = 0; i < loan.RepaymentMonths; i++)
        {
            var expected = i == loan.RepaymentMonths - 1
                ? loan.TotalRepayableAmount - loan.MonthlyRepaymentAmount * (loan.RepaymentMonths - 1)
                : loan.MonthlyRepaymentAmount;

            context.MemberLoanRepayments.Add(new MemberLoanRepayment
            {
                Id = Guid.NewGuid(),
                StokvelId = loan.StokvelId,
                LoanId = loan.Id,
                MemberId = loan.MemberId,
                ExpectedAmount = expected,
                DueDate = dueStart.AddMonths(i),
                PaymentStatus = LoanRepaymentStatus.Pending,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = currentUserId
            });
        }
    }

    private static async Task<decimal> GetContributionOverpaymentBalanceAsync(ApplicationDbContext context, Guid paymentId)
        => await context.MemberSurplusWalletTransactions
            .Where(x => x.SourceReferenceId == paymentId && x.SourceType == WalletTransactionSourceType.ContributionOverpayment)
            .SumAsync(x => x.TransactionType == WalletTransactionType.Credit ? x.Amount : -x.Amount);

    private static List<string> ValidateConfiguration(StokvelLoanConfiguration x)
    {
        var e = new List<string>(); if (!x.LoansEnabled) return e;
        if (x.MaxLoanAmount <= 0) e.Add("Maximum loan amount must be greater than zero."); if (x.MinLoanAmount < 0 || x.MaxLoanAmount < x.MinLoanAmount) e.Add("Loan amount limits are invalid.");
        if (x.MaxRepaymentMonths <= 0 || x.DefaultRepaymentMonths <= 0 || x.DefaultRepaymentMonths > x.MaxRepaymentMonths) e.Add("Repayment month limits are invalid.");
        if (x.LoanInterestType == LoanInterestType.Percentage && (x.LoanInterestRate < 1 || x.LoanInterestRate > 100)) e.Add("Interest percentage must be between 1 and 100.");
        if (x.LateRepaymentFineType != LatePenaltyType.None && (!x.LateRepaymentFineAmount.HasValue || x.LateRepaymentFineAmount <= 0)) e.Add("Late repayment fine amount is required.");
        if (x.LateRepaymentFineType == LatePenaltyType.Percentage && x.LateRepaymentFineAmount > 100) e.Add("Late fine percentage cannot exceed 100."); return e;
    }
    private static async Task<string> GetEligibilityMessageAsync(ApplicationDbContext context, Stokvel stokvel, Member member, StokvelLoanConfiguration? config)
    {
        if (!IsFeatureAllowed(stokvel)) return "Loans are not available for Burial Societies."; if (config?.LoansEnabled != true) return "Loans are disabled or have not been configured."; if (!IsEligibleMember(member)) return "Your membership is not eligible for a loan.";
        if (await context.MemberLoans.AnyAsync(x => x.StokvelId == stokvel.Id && x.MemberId == member.Id && x.IsActive && BlockingLoanStatuses.Contains(x.LoanStatus))) return "You already have an active or pending loan.";
        var date = await context.MemberLoans.Where(x => x.StokvelId == stokvel.Id && x.MemberId == member.Id && x.LoanStatus == MemberLoanStatus.FullyRepaid).OrderByDescending(x => x.FullyRepaidAt).Select(x => x.NextEligibleLoanDate).FirstOrDefaultAsync();
        return date > DateTime.UtcNow ? $"You are in a loan freeze period until {date:dd MMM yyyy}." : "Eligible to request a loan.";
    }
}

public sealed record LoansWalletPageState(Stokvel Stokvel, Member? CurrentMember, StokvelLoanConfiguration? Configuration, List<MemberLoan> Loans, List<MemberLoanRepayment> Repayments, MemberSurplusWallet? Wallet, List<MemberSurplusWalletTransaction> Transactions, List<MemberSurplusWithdrawalRequest> Withdrawals, bool CanConfigure, bool CanViewAll, bool CanApprove, bool CanConfirmMoney, string EligibilityMessage);
public sealed record FinanceOperationResult(bool Success, List<string> Errors) { public static FinanceOperationResult Succeeded() => new(true, []); public static FinanceOperationResult Failed(string error) => new(false, [error]); public static FinanceOperationResult Failed(List<string> errors) => new(false, errors); }
public sealed record LoanConfigurationResult(bool Success, StokvelLoanConfiguration? Configuration, List<string> Errors) { public static LoanConfigurationResult Succeeded(StokvelLoanConfiguration x) => new(true, x, []); public static LoanConfigurationResult Failed(List<string> e) => new(false, null, e); }
public sealed record LoansWalletTaskCounts(int PendingLoanApprovals, int LoansAwaitingDisbursement, int RepaymentsAwaitingConfirmation, int PendingWithdrawalApprovals, int WithdrawalsAwaitingPayment);
