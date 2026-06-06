using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sisonke.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddStokvelOperatingRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
           // migrationBuilder.DropTable(
            //    name: "ConstitutionRuleSets");

            migrationBuilder.Sql("DROP TABLE IF EXISTS \"ConstitutionRuleSets\";");

            migrationBuilder.CreateTable(
                name: "StokvelOperatingRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    StokvelId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StokvelType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    MonthlyContributionAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    ContributionDueDay = table.Column<int>(type: "INTEGER", nullable: false),
                    GracePeriodDays = table.Column<int>(type: "INTEGER", nullable: false),
                    AllowPartialPayments = table.Column<bool>(type: "INTEGER", nullable: false),
                    ChargeLatePaymentFine = table.Column<bool>(type: "INTEGER", nullable: false),
                    LatePaymentFineAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    EnableDependents = table.Column<bool>(type: "INTEGER", nullable: false),
                    MaximumDependents = table.Column<int>(type: "INTEGER", nullable: false),
                    MemberWaitingPeriodMonths = table.Column<int>(type: "INTEGER", nullable: false),
                    DependentWaitingPeriodMonths = table.Column<int>(type: "INTEGER", nullable: false),
                    RequireDependentIdNumber = table.Column<bool>(type: "INTEGER", nullable: false),
                    EnableClaims = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequireDeathCertificateForClaims = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequireClaimDocuments = table.Column<bool>(type: "INTEGER", nullable: false),
                    BlockClaimsIfMemberInArrears = table.Column<bool>(type: "INTEGER", nullable: false),
                    BlockClaimsIfMemberSuspended = table.Column<bool>(type: "INTEGER", nullable: false),
                    DefaultClaimPayoutAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    EnableAttendanceTracking = table.Column<bool>(type: "INTEGER", nullable: false),
                    AbsenceReminderThreshold = table.Column<int>(type: "INTEGER", nullable: false),
                    FormalWarningThreshold = table.Column<int>(type: "INTEGER", nullable: false),
                    ExecutiveReviewThreshold = table.Column<int>(type: "INTEGER", nullable: false),
                    ApologyDeadlineHoursBeforeMeeting = table.Column<int>(type: "INTEGER", nullable: false),
                    ChargeLateApologyFine = table.Column<bool>(type: "INTEGER", nullable: false),
                    LateApologyFineAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    ChargeAbsenceWithoutApologyFine = table.Column<bool>(type: "INTEGER", nullable: false),
                    AbsenceWithoutApologyFineAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    EnableMeetings = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequireMinutesApproval = table.Column<bool>(type: "INTEGER", nullable: false),
                    QuorumPercentage = table.Column<decimal>(type: "TEXT", nullable: false),
                    EnableVoting = table.Column<bool>(type: "INTEGER", nullable: false),
                    DefaultVotingApprovalThreshold = table.Column<decimal>(type: "TEXT", nullable: false),
                    AllowAnonymousVoting = table.Column<bool>(type: "INTEGER", nullable: false),
                    EnableRotationalPayouts = table.Column<bool>(type: "INTEGER", nullable: false),
                    PayoutFrequency = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    RequireTreasurerConfirmationForPayouts = table.Column<bool>(type: "INTEGER", nullable: false),
                    EnableGroceryModule = table.Column<bool>(type: "INTEGER", nullable: false),
                    EnableInvestmentModule = table.Column<bool>(type: "INTEGER", nullable: false),
                    EnablePropertyModule = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedByMemberId = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedByMemberId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StokvelOperatingRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StokvelOperatingRules_Stokvels_StokvelId",
                        column: x => x.StokvelId,
                        principalTable: "Stokvels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StokvelOperatingRules_StokvelId",
                table: "StokvelOperatingRules",
                column: "StokvelId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StokvelOperatingRules");

            migrationBuilder.CreateTable(
                name: "ConstitutionRuleSets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    StokvelId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AbsenceReminderThreshold = table.Column<int>(type: "INTEGER", nullable: false),
                    AbsenceWithoutApologyFineAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    AllowDependents = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowMemberVoting = table.Column<bool>(type: "INTEGER", nullable: false),
                    AllowPartialPayments = table.Column<bool>(type: "INTEGER", nullable: false),
                    ApologyDeadlineHoursBeforeMeeting = table.Column<int>(type: "INTEGER", nullable: false),
                    BlockClaimsIfMemberInArrears = table.Column<bool>(type: "INTEGER", nullable: false),
                    BlockClaimsIfMemberSuspended = table.Column<bool>(type: "INTEGER", nullable: false),
                    ChargeAbsenceWithoutApologyFine = table.Column<bool>(type: "INTEGER", nullable: false),
                    ChargeLateApologyFine = table.Column<bool>(type: "INTEGER", nullable: false),
                    ChargeLatePaymentFine = table.Column<bool>(type: "INTEGER", nullable: false),
                    ContributionDueDay = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByMemberId = table.Column<Guid>(type: "TEXT", nullable: true),
                    DefaultClaimPayoutAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    DefaultVotingApprovalThreshold = table.Column<decimal>(type: "TEXT", nullable: false),
                    DependentWaitingPeriodMonths = table.Column<int>(type: "INTEGER", nullable: false),
                    ExecutiveReviewThreshold = table.Column<int>(type: "INTEGER", nullable: false),
                    FormalWarningThreshold = table.Column<int>(type: "INTEGER", nullable: false),
                    GracePeriodDays = table.Column<int>(type: "INTEGER", nullable: false),
                    LateApologyFineAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    LatePaymentFineAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    MaximumDependents = table.Column<int>(type: "INTEGER", nullable: false),
                    MemberWaitingPeriodMonths = table.Column<int>(type: "INTEGER", nullable: false),
                    MonthlyContributionAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    QuorumPercentage = table.Column<decimal>(type: "TEXT", nullable: false),
                    RequireClaimDocuments = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequireDeathCertificateForClaims = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequireDependentIdNumber = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequireMinutesApproval = table.Column<bool>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UpdatedByMemberId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConstitutionRuleSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConstitutionRuleSets_Stokvels_StokvelId",
                        column: x => x.StokvelId,
                        principalTable: "Stokvels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConstitutionRuleSets_StokvelId",
                table: "ConstitutionRuleSets",
                column: "StokvelId",
                unique: true);
        }
    }
}
