using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sisonke.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AzureSqlInitialSyncClean : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    IdNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    CellphoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ResidentialArea = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QuestionnaireSections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionnaireSections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MinMembers = table.Column<int>(type: "int", nullable: false),
                    MaxMembers = table.Column<int>(type: "int", nullable: true),
                    MonthlyPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AnnualPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuestionnaireQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionnaireSectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StokvelType = table.Column<int>(type: "int", nullable: true),
                    QuestionText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    HelpText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    QuestionType = table.Column<int>(type: "int", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionnaireQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionnaireQuestions_QuestionnaireSections_QuestionnaireSectionId",
                        column: x => x.QuestionnaireSectionId,
                        principalTable: "QuestionnaireSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConstitutionDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsUploadedDocument = table.Column<bool>(type: "bit", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    StoredFilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConstitutionDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConstitutionDocuments_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConstitutionWizardAnswers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionKey = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    QuestionText = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    StepNumber = table.Column<int>(type: "int", nullable: false),
                    AnswerValue = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    AnsweredAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConstitutionWizardAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConstitutionWizardAnswers_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ContributionCycles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContributionCycles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContributionCycles_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ContributionRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Frequency = table.Column<int>(type: "int", nullable: false),
                    DueDayOfMonth = table.Column<int>(type: "int", nullable: false),
                    AllowPartialPayments = table.Column<bool>(type: "bit", nullable: false),
                    LatePaymentFineAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GracePeriodDays = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContributionRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContributionRules_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FineTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    DefaultAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FineTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FineTypes_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Meetings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MeetingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Venue = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Meetings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Meetings_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Members",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    MemberNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CellphoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    EmailAddress = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    IdNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    JoiningDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    GovernanceStatus = table.Column<int>(type: "int", nullable: false),
                    GovernanceStatusChangedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GovernanceStatusReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LastWarningIssuedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SuspendedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DefaultRole = table.Column<int>(type: "int", nullable: false),
                    ResidentialArea = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    IsInCoolingPeriod = table.Column<bool>(type: "bit", nullable: false),
                    CoolingPeriodEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeceased = table.Column<bool>(type: "bit", nullable: false),
                    DeceasedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeathReportedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Members", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Members_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Stokvels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Province = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TownOrArea = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EstablishedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpectedMemberCount = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsSetupComplete = table.Column<bool>(type: "bit", nullable: false),
                    SetupCompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stokvels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Stokvels_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TenantSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubscriptionPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NextBillingDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsTrial = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantSubscriptions_SubscriptionPlans_SubscriptionPlanId",
                        column: x => x.SubscriptionPlanId,
                        principalTable: "SubscriptionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TenantSubscriptions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuestionnaireOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionnaireQuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OptionText = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    OptionValue = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionnaireOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionnaireOptions_QuestionnaireQuestions_QuestionnaireQuestionId",
                        column: x => x.QuestionnaireQuestionId,
                        principalTable: "QuestionnaireQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StokvelQuestionnaireAnswers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionnaireQuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnswerValue = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    AnsweredAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StokvelQuestionnaireAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StokvelQuestionnaireAnswers_QuestionnaireQuestions_QuestionnaireQuestionId",
                        column: x => x.QuestionnaireQuestionId,
                        principalTable: "QuestionnaireQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StokvelQuestionnaireAnswers_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MeetingAgendaItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingAgendaItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeetingAgendaItems_Meetings_MeetingId",
                        column: x => x.MeetingId,
                        principalTable: "Meetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MeetingVotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    VotingMethod = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Result = table.Column<int>(type: "int", nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingVotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeetingVotes_Meetings_MeetingId",
                        column: x => x.MeetingId,
                        principalTable: "Meetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Beneficiaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Relationship = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    IdNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    CellphoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Beneficiaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Beneficiaries_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MeetingApologies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApologyType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ResponseNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedByMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingApologies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeetingApologies_Meetings_MeetingId",
                        column: x => x.MeetingId,
                        principalTable: "Meetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MeetingApologies_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MeetingAttendances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsLate = table.Column<bool>(type: "bit", nullable: false),
                    LeftEarly = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MarkedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingAttendances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeetingAttendances_Meetings_MeetingId",
                        column: x => x.MeetingId,
                        principalTable: "Meetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MeetingAttendances_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MemberContributions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContributionCycleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpectedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OutstandingAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    FullyPaidDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberContributions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemberContributions_ContributionCycles_ContributionCycleId",
                        column: x => x.ContributionCycleId,
                        principalTable: "ContributionCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MemberContributions_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MemberContributions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MemberDependents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Relationship = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IdNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    CellphoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeceased = table.Column<bool>(type: "bit", nullable: false),
                    DeceasedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeathReportedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberDependents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemberDependents_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MemberFines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FineTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    FineDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PaidDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CapturedByUserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberFines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemberFines_FineTypes_FineTypeId",
                        column: x => x.FineTypeId,
                        principalTable: "FineTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MemberFines_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MemberFines_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NextOfKinRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Relationship = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    CellphoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NextOfKinRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NextOfKinRecords_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MeetingMinutes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StokvelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OpeningNotes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AttendanceSummary = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApologySummary = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MattersArising = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DecisionsTaken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActionItems = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClosingNotes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedByMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedByMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedByMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingMinutes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeetingMinutes_Meetings_MeetingId",
                        column: x => x.MeetingId,
                        principalTable: "Meetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingMinutes_Stokvels_StokvelId",
                        column: x => x.StokvelId,
                        principalTable: "Stokvels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MemberWarnings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StokvelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WarningType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AbsenceCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AcknowledgedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberWarnings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemberWarnings_Meetings_MeetingId",
                        column: x => x.MeetingId,
                        principalTable: "Meetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MemberWarnings_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MemberWarnings_Stokvels_StokvelId",
                        column: x => x.StokvelId,
                        principalTable: "Stokvels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StokvelOperatingRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StokvelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StokvelType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MonthlyContributionAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ContributionDueDay = table.Column<int>(type: "int", nullable: false),
                    GracePeriodDays = table.Column<int>(type: "int", nullable: false),
                    AllowPartialPayments = table.Column<bool>(type: "bit", nullable: false),
                    ChargeLatePaymentFine = table.Column<bool>(type: "bit", nullable: false),
                    LatePaymentFineAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EnableDependents = table.Column<bool>(type: "bit", nullable: false),
                    MaximumDependents = table.Column<int>(type: "int", nullable: false),
                    MemberWaitingPeriodMonths = table.Column<int>(type: "int", nullable: false),
                    DependentWaitingPeriodMonths = table.Column<int>(type: "int", nullable: false),
                    RequireDependentIdNumber = table.Column<bool>(type: "bit", nullable: false),
                    EnableClaims = table.Column<bool>(type: "bit", nullable: false),
                    RequireDeathCertificateForClaims = table.Column<bool>(type: "bit", nullable: false),
                    RequireClaimDocuments = table.Column<bool>(type: "bit", nullable: false),
                    BlockClaimsIfMemberInArrears = table.Column<bool>(type: "bit", nullable: false),
                    BlockClaimsIfMemberSuspended = table.Column<bool>(type: "bit", nullable: false),
                    DefaultClaimPayoutAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EnableAttendanceTracking = table.Column<bool>(type: "bit", nullable: false),
                    AbsenceReminderThreshold = table.Column<int>(type: "int", nullable: false),
                    FormalWarningThreshold = table.Column<int>(type: "int", nullable: false),
                    ExecutiveReviewThreshold = table.Column<int>(type: "int", nullable: false),
                    ApologyDeadlineHoursBeforeMeeting = table.Column<int>(type: "int", nullable: false),
                    ChargeLateApologyFine = table.Column<bool>(type: "bit", nullable: false),
                    LateApologyFineAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ChargeAbsenceWithoutApologyFine = table.Column<bool>(type: "bit", nullable: false),
                    AbsenceWithoutApologyFineAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EnableMeetings = table.Column<bool>(type: "bit", nullable: false),
                    RequireMinutesApproval = table.Column<bool>(type: "bit", nullable: false),
                    QuorumPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EnableVoting = table.Column<bool>(type: "bit", nullable: false),
                    DefaultVotingApprovalThreshold = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AllowAnonymousVoting = table.Column<bool>(type: "bit", nullable: false),
                    EnableRotationalPayouts = table.Column<bool>(type: "bit", nullable: false),
                    PayoutFrequency = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RequireTreasurerConfirmationForPayouts = table.Column<bool>(type: "bit", nullable: false),
                    EnableGroceryModule = table.Column<bool>(type: "bit", nullable: false),
                    EnableInvestmentModule = table.Column<bool>(type: "bit", nullable: false),
                    EnablePropertyModule = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedByMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "VoteMotions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StokvelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MeetingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AgendaItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    VoteType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsAnonymous = table.Column<bool>(type: "bit", nullable: false),
                    OpensAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosesAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedByMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResultSummary = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DecisionOutcome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoteMotions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VoteMotions_MeetingAgendaItems_AgendaItemId",
                        column: x => x.AgendaItemId,
                        principalTable: "MeetingAgendaItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VoteMotions_Meetings_MeetingId",
                        column: x => x.MeetingId,
                        principalTable: "Meetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VoteMotions_Stokvels_StokvelId",
                        column: x => x.StokvelId,
                        principalTable: "Stokvels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MeetingVoteResponses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MeetingVoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Choice = table.Column<int>(type: "int", nullable: false),
                    VotedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingVoteResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeetingVoteResponses_MeetingVotes_MeetingVoteId",
                        column: x => x.MeetingVoteId,
                        principalTable: "MeetingVotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MeetingVoteResponses_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ContributionPaymentAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContributionPaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StokvelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PreviousAmountPaid = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NewAmountPaid = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PreviousStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaymentReference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CapturedByMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContributionPaymentAudits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContributionPaymentAudits_MemberContributions_ContributionPaymentId",
                        column: x => x.ContributionPaymentId,
                        principalTable: "MemberContributions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContributionPaymentAudits_Members_CapturedByMemberId",
                        column: x => x.CapturedByMemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContributionPaymentAudits_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContributionPaymentAudits_Stokvels_StokvelId",
                        column: x => x.StokvelId,
                        principalTable: "Stokvels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberContributionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PaymentMethod = table.Column<int>(type: "int", nullable: false),
                    CapturedByUserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_MemberContributions_MemberContributionId",
                        column: x => x.MemberContributionId,
                        principalTable: "MemberContributions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payments_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payments_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FuneralClaims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectType = table.Column<int>(type: "int", nullable: false),
                    DependentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeceasedFullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DateOfDeath = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ClaimReference = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ClaimReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReviewNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsWaitingPeriodSatisfied = table.Column<bool>(type: "bit", nullable: false),
                    IsMemberStatusEligible = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubmittedByName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    SecretaryReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SecretaryReviewedByName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    SecretaryRecommendedApproval = table.Column<bool>(type: "bit", nullable: true),
                    SecretaryReviewNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ChairpersonDecisionAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ChairpersonDecisionByName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ChairpersonDecisionNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PayoutAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PayoutPaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PayoutReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PayoutNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PayoutCapturedByMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuneralClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FuneralClaims_MemberDependents_DependentId",
                        column: x => x.DependentId,
                        principalTable: "MemberDependents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FuneralClaims_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FuneralClaims_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VoteOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VoteMotionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OptionText = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoteOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VoteOptions_VoteMotions_VoteMotionId",
                        column: x => x.VoteMotionId,
                        principalTable: "VoteMotions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClaimPayoutAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FuneralClaimId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StokvelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PreviousPayoutAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NewPayoutAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PreviousStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PayoutReference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CapturedByMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimPayoutAudits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClaimPayoutAudits_FuneralClaims_FuneralClaimId",
                        column: x => x.FuneralClaimId,
                        principalTable: "FuneralClaims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClaimPayoutAudits_Members_CapturedByMemberId",
                        column: x => x.CapturedByMemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClaimPayoutAudits_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClaimPayoutAudits_Stokvels_StokvelId",
                        column: x => x.StokvelId,
                        principalTable: "Stokvels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FuneralClaimDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FuneralClaimId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentType = table.Column<int>(type: "int", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    StoredFilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuneralClaimDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FuneralClaimDocuments_FuneralClaims_FuneralClaimId",
                        column: x => x.FuneralClaimId,
                        principalTable: "FuneralClaims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MemberVotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VoteMotionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VoteOptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VotedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberVotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemberVotes_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MemberVotes_VoteMotions_VoteMotionId",
                        column: x => x.VoteMotionId,
                        principalTable: "VoteMotions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MemberVotes_VoteOptions_VoteOptionId",
                        column: x => x.VoteOptionId,
                        principalTable: "VoteOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Beneficiaries_MemberId",
                table: "Beneficiaries",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimPayoutAudits_CapturedByMemberId",
                table: "ClaimPayoutAudits",
                column: "CapturedByMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimPayoutAudits_FuneralClaimId",
                table: "ClaimPayoutAudits",
                column: "FuneralClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimPayoutAudits_MemberId",
                table: "ClaimPayoutAudits",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimPayoutAudits_StokvelId",
                table: "ClaimPayoutAudits",
                column: "StokvelId");

            migrationBuilder.CreateIndex(
                name: "IX_ConstitutionDocuments_TenantId",
                table: "ConstitutionDocuments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ConstitutionWizardAnswers_TenantId_QuestionKey",
                table: "ConstitutionWizardAnswers",
                columns: new[] { "TenantId", "QuestionKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContributionCycles_TenantId",
                table: "ContributionCycles",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ContributionPaymentAudits_CapturedByMemberId",
                table: "ContributionPaymentAudits",
                column: "CapturedByMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_ContributionPaymentAudits_ContributionPaymentId",
                table: "ContributionPaymentAudits",
                column: "ContributionPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_ContributionPaymentAudits_MemberId",
                table: "ContributionPaymentAudits",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_ContributionPaymentAudits_StokvelId",
                table: "ContributionPaymentAudits",
                column: "StokvelId");

            migrationBuilder.CreateIndex(
                name: "IX_ContributionRules_TenantId",
                table: "ContributionRules",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_FineTypes_TenantId",
                table: "FineTypes",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_FuneralClaimDocuments_FuneralClaimId",
                table: "FuneralClaimDocuments",
                column: "FuneralClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_FuneralClaims_ClaimReference",
                table: "FuneralClaims",
                column: "ClaimReference");

            migrationBuilder.CreateIndex(
                name: "IX_FuneralClaims_DependentId",
                table: "FuneralClaims",
                column: "DependentId");

            migrationBuilder.CreateIndex(
                name: "IX_FuneralClaims_MemberId",
                table: "FuneralClaims",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_FuneralClaims_TenantId",
                table: "FuneralClaims",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingAgendaItems_MeetingId",
                table: "MeetingAgendaItems",
                column: "MeetingId");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingApologies_MeetingId_MemberId",
                table: "MeetingApologies",
                columns: new[] { "MeetingId", "MemberId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MeetingApologies_MemberId",
                table: "MeetingApologies",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingAttendances_MeetingId_MemberId",
                table: "MeetingAttendances",
                columns: new[] { "MeetingId", "MemberId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MeetingAttendances_MemberId",
                table: "MeetingAttendances",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingMinutes_MeetingId",
                table: "MeetingMinutes",
                column: "MeetingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MeetingMinutes_StokvelId",
                table: "MeetingMinutes",
                column: "StokvelId");

            migrationBuilder.CreateIndex(
                name: "IX_Meetings_TenantId",
                table: "Meetings",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingVoteResponses_MeetingVoteId_MemberId",
                table: "MeetingVoteResponses",
                columns: new[] { "MeetingVoteId", "MemberId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MeetingVoteResponses_MemberId",
                table: "MeetingVoteResponses",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingVotes_MeetingId",
                table: "MeetingVotes",
                column: "MeetingId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberContributions_ContributionCycleId",
                table: "MemberContributions",
                column: "ContributionCycleId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberContributions_MemberId",
                table: "MemberContributions",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberContributions_TenantId",
                table: "MemberContributions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberDependents_MemberId",
                table: "MemberDependents",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberFines_FineTypeId",
                table: "MemberFines",
                column: "FineTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberFines_MemberId",
                table: "MemberFines",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberFines_TenantId",
                table: "MemberFines",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Members_ApplicationUserId",
                table: "Members",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Members_IdNumber",
                table: "Members",
                column: "IdNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Members_TenantId",
                table: "Members",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberVotes_MemberId",
                table: "MemberVotes",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberVotes_VoteMotionId_MemberId",
                table: "MemberVotes",
                columns: new[] { "VoteMotionId", "MemberId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MemberVotes_VoteOptionId",
                table: "MemberVotes",
                column: "VoteOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberWarnings_MeetingId",
                table: "MemberWarnings",
                column: "MeetingId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberWarnings_MemberId_MeetingId_WarningType",
                table: "MemberWarnings",
                columns: new[] { "MemberId", "MeetingId", "WarningType" },
                unique: true,
                filter: "[MeetingId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MemberWarnings_StokvelId",
                table: "MemberWarnings",
                column: "StokvelId");

            migrationBuilder.CreateIndex(
                name: "IX_NextOfKinRecords_MemberId",
                table: "NextOfKinRecords",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_MemberContributionId",
                table: "Payments",
                column: "MemberContributionId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_MemberId",
                table: "Payments",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TenantId",
                table: "Payments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionnaireOptions_QuestionnaireQuestionId",
                table: "QuestionnaireOptions",
                column: "QuestionnaireQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionnaireQuestions_QuestionnaireSectionId",
                table: "QuestionnaireQuestions",
                column: "QuestionnaireSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_StokvelOperatingRules_StokvelId",
                table: "StokvelOperatingRules",
                column: "StokvelId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StokvelQuestionnaireAnswers_QuestionnaireQuestionId",
                table: "StokvelQuestionnaireAnswers",
                column: "QuestionnaireQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_StokvelQuestionnaireAnswers_TenantId_QuestionnaireQuestionId",
                table: "StokvelQuestionnaireAnswers",
                columns: new[] { "TenantId", "QuestionnaireQuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stokvels_Code",
                table: "Stokvels",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_Stokvels_TenantId",
                table: "Stokvels",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Slug",
                table: "Tenants",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantSubscriptions_SubscriptionPlanId",
                table: "TenantSubscriptions",
                column: "SubscriptionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantSubscriptions_TenantId",
                table: "TenantSubscriptions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_VoteMotions_AgendaItemId",
                table: "VoteMotions",
                column: "AgendaItemId");

            migrationBuilder.CreateIndex(
                name: "IX_VoteMotions_MeetingId",
                table: "VoteMotions",
                column: "MeetingId");

            migrationBuilder.CreateIndex(
                name: "IX_VoteMotions_StokvelId",
                table: "VoteMotions",
                column: "StokvelId");

            migrationBuilder.CreateIndex(
                name: "IX_VoteOptions_VoteMotionId",
                table: "VoteOptions",
                column: "VoteMotionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "Beneficiaries");

            migrationBuilder.DropTable(
                name: "ClaimPayoutAudits");

            migrationBuilder.DropTable(
                name: "ConstitutionDocuments");

            migrationBuilder.DropTable(
                name: "ConstitutionWizardAnswers");

            migrationBuilder.DropTable(
                name: "ContributionPaymentAudits");

            migrationBuilder.DropTable(
                name: "ContributionRules");

            migrationBuilder.DropTable(
                name: "FuneralClaimDocuments");

            migrationBuilder.DropTable(
                name: "MeetingApologies");

            migrationBuilder.DropTable(
                name: "MeetingAttendances");

            migrationBuilder.DropTable(
                name: "MeetingMinutes");

            migrationBuilder.DropTable(
                name: "MeetingVoteResponses");

            migrationBuilder.DropTable(
                name: "MemberFines");

            migrationBuilder.DropTable(
                name: "MemberVotes");

            migrationBuilder.DropTable(
                name: "MemberWarnings");

            migrationBuilder.DropTable(
                name: "NextOfKinRecords");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "QuestionnaireOptions");

            migrationBuilder.DropTable(
                name: "StokvelOperatingRules");

            migrationBuilder.DropTable(
                name: "StokvelQuestionnaireAnswers");

            migrationBuilder.DropTable(
                name: "TenantSubscriptions");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "FuneralClaims");

            migrationBuilder.DropTable(
                name: "MeetingVotes");

            migrationBuilder.DropTable(
                name: "FineTypes");

            migrationBuilder.DropTable(
                name: "VoteOptions");

            migrationBuilder.DropTable(
                name: "MemberContributions");

            migrationBuilder.DropTable(
                name: "QuestionnaireQuestions");

            migrationBuilder.DropTable(
                name: "SubscriptionPlans");

            migrationBuilder.DropTable(
                name: "MemberDependents");

            migrationBuilder.DropTable(
                name: "VoteMotions");

            migrationBuilder.DropTable(
                name: "ContributionCycles");

            migrationBuilder.DropTable(
                name: "QuestionnaireSections");

            migrationBuilder.DropTable(
                name: "Members");

            migrationBuilder.DropTable(
                name: "MeetingAgendaItems");

            migrationBuilder.DropTable(
                name: "Stokvels");

            migrationBuilder.DropTable(
                name: "Meetings");

            migrationBuilder.DropTable(
                name: "Tenants");
        }
    }
}
