using Microsoft.EntityFrameworkCore;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.AI;
using Manage_KPI_or_OKR_System.Models.Tenancy;
using Manage_KPI_or_OKR_System.Services.Tenancy;

namespace Manage_KPI_or_OKR_System.Data
{
    public class MiniERPDbContext : DbContext
    {
        private readonly ITenantContext _tenantContext;

        public MiniERPDbContext(
            DbContextOptions<MiniERPDbContext> options,
            ITenantContext? tenantContext = null) : base(options)
        {
            _tenantContext = tenantContext ?? new UnresolvedTenantContext();
        }

        /// <summary>Used by EF query filters; an unresolved production request intentionally returns no tenant data.</summary>
        public int? CurrentTenantId => _tenantContext.TenantId;
        // Keep the value consumed by query filters non-nullable. Some providers (notably
        // EF Core InMemory used by the regression suite) materialize Nullable<T>.Value
        // before evaluating the other side of an expression.
        public int TenantFilterId => _tenantContext.TenantId ?? -1;
        public bool TenantAccessUnrestricted =>
            !_tenantContext.IsProductionRequest || _tenantContext.HasAuditedPlatformBypass;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.AddInterceptors(new TenantSessionContextInterceptor(_tenantContext));
            base.OnConfiguring(optionsBuilder);
        }

        // MODULE 1 & 2: FOUNDATION, ORGANIZATION & HR
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<Role_Permission> Role_Permissions { get; set; }
        public DbSet<Status> Statuses { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Position> Positions { get; set; }
        public DbSet<SystemUser> SystemUsers { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<SystemParameter> SystemParameters { get; set; }
        public DbSet<EmployeeAssignment> EmployeeAssignments { get; set; }
        public DbSet<GradingRank> GradingRanks { get; set; }

        // MODULE 3: OKR & MỤC TIÊU
        public DbSet<MissionVision> MissionVisions { get; set; }
        public DbSet<OKRType> OKRTypes { get; set; }
        public DbSet<OKR> OKRs { get; set; }
        public DbSet<OKRKeyResult> OKRKeyResults { get; set; }
        public DbSet<OKR_Mission_Mapping> OKR_Mission_Mappings { get; set; }
        public DbSet<OKR_Department_Allocation> OKR_Department_Allocations { get; set; }
        public DbSet<OKR_Employee_Allocation> OKR_Employee_Allocations { get; set; }

        // MODULE 4: KPI SETUP
        public DbSet<EvaluationPeriod> EvaluationPeriods { get; set; }
        public DbSet<KPIType> KPITypes { get; set; }
        public DbSet<KPIProperty> KPIProperties { get; set; }
        public DbSet<KPI> KPIs { get; set; }
        public DbSet<KPIDetail> KPIDetails { get; set; }
        public DbSet<KPI_Department_Assignment> KPI_Department_Assignments { get; set; }
        public DbSet<KPI_Employee_Assignment> KPI_Employee_Assignments { get; set; }
        public DbSet<AdhocTask> AdhocTasks { get; set; }
        public DbSet<WorkProject> WorkProjects { get; set; }
        public DbSet<WorkProjectDepartment> WorkProjectDepartments { get; set; }
        public DbSet<WorkItem> WorkItems { get; set; }
        public DbSet<WorkItemComment> WorkItemComments { get; set; }

        // MODULE 5: CHECK-IN & EXECUTION
        public DbSet<CheckInStatus> CheckInStatuses { get; set; }
        public DbSet<FailReason> FailReasons { get; set; }
        public DbSet<KPICheckIn> KPICheckIns { get; set; }
        public DbSet<CheckInDetail> CheckInDetails { get; set; }
        public DbSet<CheckInHistoryLog> CheckInHistoryLogs { get; set; }
        public DbSet<GoalComment> GoalComments { get; set; }
        public DbSet<OneOnOneMeeting> OneOnOneMeetings { get; set; }
        public DbSet<KPI_Result_Comparison> KPI_Result_Comparisons { get; set; }

        // MODULE 6: EVALUATION & HR
        public DbSet<EvaluationResult> EvaluationResults { get; set; }
        public DbSet<KPIAdjustmentHistory> KPIAdjustmentHistories { get; set; }
        public DbSet<BonusRule> BonusRules { get; set; }
        public DbSet<RealtimeExpectedBonus> RealtimeExpectedBonuses { get; set; }
        public DbSet<HRExportReport> HRExportReports { get; set; }
        public DbSet<EvaluationReportSummary> EvaluationReportSummaries { get; set; }
        public DbSet<EvaluationReportIncident> EvaluationReportIncidents { get; set; }

        // SYSTEM
        public DbSet<SystemAlert> SystemAlerts { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<AIGenerationHistory> AIGenerationHistories { get; set; }
        public DbSet<PurchaseRegistration> PurchaseRegistrations { get; set; }
        public DbSet<SaaSPackage> SaaSPackages { get; set; }
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<TenantMembership> TenantMemberships { get; set; }
        public DbSet<AgentRunRecord> AgentRuns { get; set; }
        public DbSet<AiHistorySession> AiHistorySessions { get; set; }
        public DbSet<AiHistoryEntry> AiHistoryEntries { get; set; }
        public DbSet<AgentApproval> AgentApprovals { get; set; }
        public DbSet<AgentDraftAction> AgentDraftActions { get; set; }
        public DbSet<AiEvaluationProposal> AiEvaluationProposals { get; set; }
        public DbSet<AiEvaluationCriterionResult> AiEvaluationCriterionResults { get; set; }
        public DbSet<EvidenceReferenceMetadata> EvidenceReferenceMetadata { get; set; }
        public DbSet<CheckInAiEvaluationOutbox> CheckInAiEvaluationOutbox { get; set; }
        public DbSet<EvaluationRubric> EvaluationRubrics { get; set; }
        public DbSet<EvaluationCriterion> EvaluationCriteria { get; set; }
        public DbSet<KnowledgeDocument> KnowledgeDocuments { get; set; }
        public DbSet<KnowledgeDocumentVersion> KnowledgeDocumentVersions { get; set; }
        public DbSet<KnowledgeChunk> KnowledgeChunks { get; set; }
        public DbSet<DocumentIngestionJob> DocumentIngestionJobs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // TenantId is intentionally a shadow property on legacy domain models so existing MVC binding
            // cannot post a tenant boundary. Every tenant-owned table is filtered and stamped below.
            ConfigureTenantScope<Status>(modelBuilder);
            ConfigureTenantScope<Department>(modelBuilder);
            ConfigureTenantScope<Position>(modelBuilder);
            ConfigureTenantScope<Employee>(modelBuilder);
            ConfigureTenantScope<SystemParameter>(modelBuilder);
            ConfigureTenantScope<EmployeeAssignment>(modelBuilder);
            ConfigureTenantScope<GradingRank>(modelBuilder);
            ConfigureTenantScope<MissionVision>(modelBuilder);
            ConfigureTenantScope<OKRType>(modelBuilder);
            ConfigureTenantScope<OKR>(modelBuilder);
            ConfigureTenantScope<OKRKeyResult>(modelBuilder);
            ConfigureTenantScope<OKR_Mission_Mapping>(modelBuilder);
            ConfigureTenantScope<OKR_Department_Allocation>(modelBuilder);
            ConfigureTenantScope<OKR_Employee_Allocation>(modelBuilder);
            ConfigureTenantScope<EvaluationPeriod>(modelBuilder);
            ConfigureTenantScope<KPIType>(modelBuilder);
            ConfigureTenantScope<KPIProperty>(modelBuilder);
            ConfigureTenantScope<KPI>(modelBuilder);
            ConfigureTenantScope<KPIDetail>(modelBuilder);
            ConfigureTenantScope<KPI_Department_Assignment>(modelBuilder);
            ConfigureTenantScope<KPI_Employee_Assignment>(modelBuilder);
            ConfigureTenantScope<AdhocTask>(modelBuilder);
            ConfigureTenantScope<WorkProject>(modelBuilder);
            ConfigureTenantScope<WorkProjectDepartment>(modelBuilder);
            ConfigureTenantScope<WorkItem>(modelBuilder);
            ConfigureTenantScope<WorkItemComment>(modelBuilder);
            ConfigureTenantScope<CheckInStatus>(modelBuilder);
            ConfigureTenantScope<FailReason>(modelBuilder);
            ConfigureTenantScope<KPICheckIn>(modelBuilder);
            ConfigureTenantScope<CheckInDetail>(modelBuilder);
            ConfigureTenantScope<CheckInHistoryLog>(modelBuilder);
            ConfigureTenantScope<GoalComment>(modelBuilder);
            ConfigureTenantScope<OneOnOneMeeting>(modelBuilder);
            ConfigureTenantScope<KPI_Result_Comparison>(modelBuilder);
            ConfigureTenantScope<EvaluationResult>(modelBuilder);
            ConfigureTenantScope<KPIAdjustmentHistory>(modelBuilder);
            ConfigureTenantScope<BonusRule>(modelBuilder);
            ConfigureTenantScope<RealtimeExpectedBonus>(modelBuilder);
            ConfigureTenantScope<HRExportReport>(modelBuilder);
            ConfigureTenantScope<EvaluationReportSummary>(modelBuilder);
            ConfigureTenantScope<EvaluationReportIncident>(modelBuilder);
            ConfigureTenantScope<SystemAlert>(modelBuilder);
            ConfigureTenantScope<AuditLog>(modelBuilder);
            ConfigureTenantScope<AIGenerationHistory>(modelBuilder);
            ConfigureTenantScope<AgentRunRecord>(modelBuilder);
            ConfigureTenantScope<AiHistorySession>(modelBuilder);
            ConfigureTenantScope<AiHistoryEntry>(modelBuilder);
            ConfigureTenantScope<AgentApproval>(modelBuilder);
            ConfigureTenantScope<AgentDraftAction>(modelBuilder);
            ConfigureTenantScope<AiEvaluationProposal>(modelBuilder);
            ConfigureTenantScope<AiEvaluationCriterionResult>(modelBuilder);
            ConfigureTenantScope<EvidenceReferenceMetadata>(modelBuilder);
            ConfigureTenantScope<CheckInAiEvaluationOutbox>(modelBuilder);
            ConfigureTenantScope<EvaluationRubric>(modelBuilder);
            ConfigureTenantScope<EvaluationCriterion>(modelBuilder);
            ConfigureTenantScope<KnowledgeDocument>(modelBuilder);
            ConfigureTenantScope<KnowledgeDocumentVersion>(modelBuilder);
            ConfigureTenantScope<KnowledgeChunk>(modelBuilder);
            ConfigureTenantScope<DocumentIngestionJob>(modelBuilder);

            modelBuilder.Entity<Tenant>().HasIndex(t => t.Code).IsUnique();
            modelBuilder.Entity<TenantMembership>().HasIndex(m => new { m.TenantId, m.SystemUserId }).IsUnique();
            modelBuilder.Entity<TenantMembership>().HasOne(m => m.Tenant).WithMany(t => t.Memberships)
                .HasForeignKey(m => m.TenantId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<TenantMembership>().HasOne(m => m.SystemUser).WithMany(u => u.TenantMemberships)
                .HasForeignKey(m => m.SystemUserId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<TenantMembership>().HasOne(m => m.Role).WithMany()
                .HasForeignKey(m => m.RoleId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AgentRunRecord>().HasIndex(r => new { r.TenantId, r.CorrelationId });
            modelBuilder.Entity<AgentRunRecord>().HasAlternateKey(r => new { r.TenantId, r.Id });
            modelBuilder.Entity<AiHistorySession>().HasAlternateKey(session => new { session.TenantId, session.Id });
            modelBuilder.Entity<AiHistorySession>()
                .HasIndex(session => new
                { session.TenantId, session.OwnerSystemUserId, session.ContentDeletedAtUtc, session.UpdatedAtUtc });
            modelBuilder.Entity<AiHistorySession>().ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_AiHistorySessions_Status",
                    "[Status] IN ('Pending','Completed','Abstained','AwaitingReview','Applied','Rejected','Conflict','Failed','ContentDeleted')");
                table.HasCheckConstraint(
                    "CK_AiHistorySessions_Title",
                    "[Title] IS NULL OR LEN(LTRIM(RTRIM([Title]))) BETWEEN 1 AND 200");
            });
            modelBuilder.Entity<AiHistoryEntry>()
                .HasIndex(entry => new { entry.TenantId, entry.SessionId, entry.Sequence })
                .IsUnique();
            modelBuilder.Entity<AiHistoryEntry>()
                .HasIndex(entry => new { entry.TenantId, entry.SessionId, entry.OperationId, entry.EntryKind })
                .IsUnique();
            modelBuilder.Entity<AiHistoryEntry>()
                .HasIndex(entry => new { entry.TenantId, entry.AgentRunId });
            modelBuilder.Entity<AiHistoryEntry>().ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_AiHistoryEntries_EntryKind",
                    "[EntryKind] IN ('Input','Output','Warning','Decision','LegacyMetadata')");
                table.HasCheckConstraint(
                    "CK_AiHistoryEntries_Status",
                    "[Status] IN ('Pending','Completed','Abstained','AwaitingReview','Applied','Rejected','Conflict','Failed','ContentDeleted')");
                table.HasCheckConstraint(
                    "CK_AiHistoryEntries_Sequence",
                    "[Sequence] > 0 AND [PayloadSchemaVersion] > 0");
            });
            modelBuilder.Entity<AiHistoryEntry>()
                .HasOne(entry => entry.Session)
                .WithMany(session => session.Entries)
                .HasForeignKey(entry => new { entry.TenantId, entry.SessionId })
                .HasPrincipalKey(session => new { session.TenantId, session.Id })
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<AiHistoryEntry>()
                .HasOne<AgentRunRecord>()
                .WithMany()
                .HasForeignKey(entry => new { entry.TenantId, entry.AgentRunId })
                .HasPrincipalKey(run => new { run.TenantId, run.Id })
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<KPI>().HasAlternateKey("TenantId", nameof(KPI.Id));
            modelBuilder.Entity<EvaluationPeriod>().HasAlternateKey("TenantId", nameof(EvaluationPeriod.Id));
            modelBuilder.Entity<EvaluationResult>().HasAlternateKey("TenantId", nameof(EvaluationResult.Id));
            modelBuilder.Entity<AgentApproval>().HasIndex(a => new { a.TenantId, a.AgentRunId }).IsUnique();
            modelBuilder.Entity<AgentApproval>()
                .HasIndex(a => new { a.TenantId, a.IdempotencyKey })
                .IsUnique()
                .HasFilter("[IdempotencyKey] IS NOT NULL");
            modelBuilder.Entity<AgentApproval>().HasOne<AgentRunRecord>().WithMany().HasForeignKey(a => a.AgentRunId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<AgentDraftAction>()
                .HasIndex(action => new
                { action.TenantId, action.SourceEntityType, action.SourceEntityId, action.SourceVersion, action.ActionType })
                .IsUnique();
            modelBuilder.Entity<AgentDraftAction>()
                .HasIndex(action => new { action.TenantId, action.AgentRunId })
                .IsUnique();
            modelBuilder.Entity<AgentDraftAction>().ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_AgentDraftActions_Source",
                    "[SourceEntityId] > 0 AND LEN(LTRIM(RTRIM([SourceEntityType]))) > 0 AND LEN(LTRIM(RTRIM([ActionType]))) > 0 AND LEN(LTRIM(RTRIM([DraftText]))) > 0");
                table.HasCheckConstraint(
                    "CK_AgentDraftActions_Status",
                    "[Status] IN ('AwaitingHumanReview','AppliedToHumanDraft','RejectedByHuman','Superseded')");
            });
            modelBuilder.Entity<AgentDraftAction>().HasOne<AgentRunRecord>().WithMany()
                .HasForeignKey(action => new { action.TenantId, action.AgentRunId })
                .HasPrincipalKey(run => new { run.TenantId, run.Id })
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<AgentDraftAction>().HasOne<EvaluationResult>().WithMany()
                .HasForeignKey(nameof(AgentDraftAction.TenantId), nameof(AgentDraftAction.EvaluationResultId))
                .HasPrincipalKey("TenantId", nameof(EvaluationResult.Id))
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<AiEvaluationProposal>().HasIndex(p => new
            { p.TenantId, p.SourceEntityType, p.SourceEntityId, p.SourceVersion }).IsUnique();
            modelBuilder.Entity<AiEvaluationProposal>().HasAlternateKey(p => new { p.TenantId, p.Id });
            modelBuilder.Entity<AiEvaluationProposal>().HasOne<AgentRunRecord>().WithMany().HasForeignKey(p => p.AgentRunId)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<AiEvaluationProposal>().HasOne<KPICheckIn>().WithMany().HasForeignKey(p => p.KPICheckInId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<AiEvaluationProposal>().HasOne<EvaluationResult>().WithMany().HasForeignKey(p => p.EvaluationResultId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<AiEvaluationProposal>().HasOne<EvaluationRubric>().WithMany()
                .HasForeignKey(nameof(AiEvaluationProposal.TenantId), nameof(AiEvaluationProposal.EvaluationRubricId))
                .HasPrincipalKey(nameof(EvaluationRubric.TenantId), nameof(EvaluationRubric.Id))
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<AiEvaluationProposal>().ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_AiEvaluationProposals_Scores",
                    "([OfficialBaselineScore] IS NULL OR [OfficialBaselineScore] BETWEEN 0 AND 100) AND ([ProjectedScore] IS NULL OR [ProjectedScore] BETWEEN 0 AND 100) AND ([HumanReviewScore] IS NULL OR [HumanReviewScore] BETWEEN 0 AND 100) AND ([HumanScoreDelta] IS NULL OR [HumanScoreDelta] BETWEEN -100 AND 100)");
                table.HasCheckConstraint(
                    "CK_AiEvaluationProposals_Confidence",
                    "[ConfidenceScore] BETWEEN 0 AND 1 AND [EvidenceCoverageScore] BETWEEN 0 AND 1 AND [SourceAuthorityScore] BETWEEN 0 AND 1 AND [ConsistencyScore] BETWEEN 0 AND 1 AND [FreshnessScore] BETWEEN 0 AND 1");
            });
            modelBuilder.Entity<AiEvaluationCriterionResult>()
                .HasIndex(result => new
                { result.TenantId, result.AiEvaluationProposalId, result.EvaluationCriterionId })
                .IsUnique();
            modelBuilder.Entity<AiEvaluationCriterionResult>().ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_AiEvaluationCriterionResults_Values",
                    "[RubricVersion] > 0 AND [ConfidenceScore] BETWEEN 0 AND 1 AND [CitationCount] >= 0 AND ([ProposedScorePercent] IS NULL OR [ProposedScorePercent] BETWEEN 0 AND 100)");
            });
            modelBuilder.Entity<AiEvaluationCriterionResult>().HasOne(result => result.Proposal).WithMany()
                .HasForeignKey(result => new { result.TenantId, result.AiEvaluationProposalId })
                .HasPrincipalKey(proposal => new { proposal.TenantId, proposal.Id })
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<AiEvaluationCriterionResult>().HasOne(result => result.Criterion).WithMany()
                .HasForeignKey(nameof(AiEvaluationCriterionResult.TenantId), nameof(AiEvaluationCriterionResult.EvaluationCriterionId))
                .HasPrincipalKey(nameof(EvaluationCriterion.TenantId), nameof(EvaluationCriterion.Id))
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<EvidenceReferenceMetadata>().HasIndex(e => new { e.TenantId, e.AgentRunId, e.AiEvaluationProposalId });
            modelBuilder.Entity<EvidenceReferenceMetadata>().HasOne<AgentRunRecord>().WithMany().HasForeignKey(e => e.AgentRunId)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<EvidenceReferenceMetadata>().HasOne(e => e.Proposal).WithMany().HasForeignKey(e => e.AiEvaluationProposalId)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<CheckInAiEvaluationOutbox>()
                .HasIndex(item => new { item.TenantId, item.CheckInId, item.SourceVersion })
                .IsUnique();
            modelBuilder.Entity<CheckInAiEvaluationOutbox>()
                .HasIndex(item => new { item.State, item.AvailableAtUtc, item.LeaseExpiresAtUtc });
            modelBuilder.Entity<CheckInAiEvaluationOutbox>().HasOne<KPICheckIn>().WithMany()
                .HasForeignKey(item => item.CheckInId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<EvaluationRubric>()
                .HasIndex(rubric => new { rubric.TenantId, rubric.KPIId, rubric.Version })
                .IsUnique();
            modelBuilder.Entity<EvaluationRubric>()
                .HasIndex(rubric => new { rubric.TenantId, rubric.KPIId })
                .IsUnique()
                .HasFilter("[IsActive] = 1");
            modelBuilder.Entity<EvaluationRubric>()
                .HasAlternateKey(rubric => new { rubric.TenantId, rubric.Id });
            modelBuilder.Entity<EvaluationRubric>().ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_EvaluationRubrics_Thresholds",
                    "[Version] > 0 AND [OnTrackPercent] BETWEEN 0 AND 100 AND [AtRiskPercent] BETWEEN 0 AND 100 AND [AtRiskPercent] <= [OnTrackPercent] AND [MinimumConfidenceToPropose] BETWEEN 0 AND 1");
            });
            modelBuilder.Entity<EvaluationRubric>().HasOne<KPI>().WithMany()
                .HasForeignKey(nameof(EvaluationRubric.TenantId), nameof(EvaluationRubric.KPIId))
                .HasPrincipalKey("TenantId", nameof(KPI.Id))
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<EvaluationRubric>().HasOne<EvaluationPeriod>().WithMany()
                .HasForeignKey(nameof(EvaluationRubric.TenantId), nameof(EvaluationRubric.PeriodId))
                .HasPrincipalKey("TenantId", nameof(EvaluationPeriod.Id))
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<EvaluationCriterion>()
                .HasIndex(criterion => new { criterion.TenantId, criterion.EvaluationRubricId, criterion.Ordinal })
                .IsUnique();
            modelBuilder.Entity<EvaluationCriterion>()
                .HasAlternateKey(criterion => new { criterion.TenantId, criterion.Id });
            modelBuilder.Entity<EvaluationCriterion>().ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_EvaluationCriteria_Weights",
                    "[Ordinal] >= 0 AND [WeightPercent] BETWEEN 0 AND 100 AND [MinimumConfidenceToScore] BETWEEN 0.6 AND 1 AND [MinimumScorePercent] BETWEEN 0 AND 100 AND [MaximumScorePercent] BETWEEN 0 AND 100 AND [MinimumScorePercent] <= [MaximumScorePercent] AND LEN(LTRIM(RTRIM([Name]))) > 0");
                table.HasCheckConstraint(
                    "CK_EvaluationCriteria_MeasurementType",
                    "[MeasurementType] IN ('Quantitative','Qualitative','Behavioral')");
            });
            modelBuilder.Entity<EvaluationCriterion>().HasOne(criterion => criterion.EvaluationRubric)
                .WithMany(rubric => rubric.Criteria)
                .HasForeignKey(criterion => new { criterion.TenantId, criterion.EvaluationRubricId })
                .HasPrincipalKey(rubric => new { rubric.TenantId, rubric.Id })
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<KnowledgeDocument>()
                .HasIndex(document => new { document.TenantId, document.OwnerSystemUserId, document.IsDeleted });
            modelBuilder.Entity<KnowledgeDocument>().ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_KnowledgeDocuments_AccessPolicyVersion",
                    "[AccessPolicyVersion] > 0");
                table.HasCheckConstraint(
                    "CK_KnowledgeDocuments_AccessPrincipalsJson",
                    "ISJSON([AccessPrincipalsJson]) = 1");
            });
            modelBuilder.Entity<KnowledgeDocument>()
                .HasOne<SystemUser>()
                .WithMany()
                .HasForeignKey(document => document.OwnerSystemUserId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<KnowledgeDocumentVersion>()
                .HasIndex(version => new { version.TenantId, version.DocumentId, version.VersionNumber })
                .IsUnique();
            modelBuilder.Entity<KnowledgeDocumentVersion>().ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_KnowledgeDocumentVersions_PositiveValues",
                    "[VersionNumber] > 0 AND [FileSizeBytes] > 0");
                table.HasCheckConstraint(
                    "CK_KnowledgeDocumentVersions_Status",
                    "[Status] IN ('Stored','Queued','Processing','Indexed','Failed','Superseded','Cancelled')");
                table.HasCheckConstraint(
                    "CK_KnowledgeDocumentVersions_SourceBlobUri",
                    "[SourceBlobUri] LIKE 'https://%' AND CHARINDEX('?', [SourceBlobUri]) = 0 AND CHARINDEX('#', [SourceBlobUri]) = 0");
            });
            modelBuilder.Entity<KnowledgeDocumentVersion>()
                .HasIndex(version => new { version.TenantId, version.DocumentId, version.ContentSha256 })
                .IsUnique();
            modelBuilder.Entity<KnowledgeDocumentVersion>()
                .HasOne(version => version.Document)
                .WithMany(document => document.Versions)
                .HasForeignKey(version => version.DocumentId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<KnowledgeChunk>()
                .HasIndex(chunk => new
                { chunk.TenantId, chunk.DocumentVersionId, chunk.PipelineVersion, chunk.AccessPolicyVersion, chunk.Ordinal })
                .IsUnique();
            modelBuilder.Entity<KnowledgeChunk>().ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_KnowledgeChunks_NonNegativeValues",
                    "[AccessPolicyVersion] > 0 AND [Ordinal] >= 0 AND [TokenCount] >= 0 AND ([Page] IS NULL OR [Page] > 0)");
                table.HasCheckConstraint(
                    "CK_KnowledgeChunks_ContentBlobUri",
                    "[ContentBlobUri] LIKE 'https://%' AND CHARINDEX('?', [ContentBlobUri]) = 0 AND CHARINDEX('#', [ContentBlobUri]) = 0");
            });
            modelBuilder.Entity<KnowledgeChunk>()
                .HasIndex(chunk => new { chunk.TenantId, chunk.SearchIndexKey })
                .IsUnique();
            modelBuilder.Entity<KnowledgeChunk>()
                .HasOne(chunk => chunk.DocumentVersion)
                .WithMany(version => version.Chunks)
                .HasForeignKey(chunk => chunk.DocumentVersionId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<DocumentIngestionJob>()
                .HasIndex(job => new
                { job.TenantId, job.DocumentVersionId, job.Operation, job.PipelineVersion, job.AccessPolicyVersion })
                .IsUnique();
            modelBuilder.Entity<DocumentIngestionJob>().ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_DocumentIngestionJobs_NonNegativeValues",
                    "[AccessPolicyVersion] > 0 AND [AttemptCount] >= 0");
                table.HasCheckConstraint(
                    "CK_DocumentIngestionJobs_State",
                    "[State] IN ('Pending','Leased','WaitingForMinerU','Indexing','Completed','DeadLetter','Cancelled')");
                table.HasCheckConstraint(
                    "CK_DocumentIngestionJobs_Operation",
                    "[Operation] IN ('Index','Delete')");
                table.HasCheckConstraint(
                    "CK_DocumentIngestionJobs_ParserResultBlobUri",
                    "[ParserResultBlobUri] IS NULL OR ([ParserResultBlobUri] LIKE 'https://%' AND CHARINDEX('?', [ParserResultBlobUri]) = 0 AND CHARINDEX('#', [ParserResultBlobUri]) = 0)");
            });
            modelBuilder.Entity<DocumentIngestionJob>()
                .HasIndex(job => new { job.State, job.AvailableAtUtc, job.LeaseExpiresAtUtc });
            modelBuilder.Entity<DocumentIngestionJob>()
                .HasOne(job => job.DocumentVersion)
                .WithMany(version => version.IngestionJobs)
                .HasForeignKey(job => job.DocumentVersionId)
                .OnDelete(DeleteBehavior.Restrict);

            // ==========================================
            // 1. CẤU HÌNH KHÓA CHÍNH KÉP (COMPOSITE KEYS)
            // ==========================================
            modelBuilder.Entity<Role_Permission>().HasKey(rp => new { rp.RoleId, rp.PermissionId });
            modelBuilder.Entity<OKR_Mission_Mapping>().HasKey(om => new { om.OKRId, om.MissionId });
            modelBuilder.Entity<OKR_Department_Allocation>().HasKey(od => new { od.OKRId, od.DepartmentId });
            modelBuilder.Entity<OKR_Employee_Allocation>().HasKey(oe => new { oe.OKRId, oe.EmployeeId });
            modelBuilder.Entity<KPI_Department_Assignment>().HasKey(kd => new { kd.KPIId, kd.DepartmentId });
            modelBuilder.Entity<KPI_Employee_Assignment>().HasKey(ke => new { ke.KPIId, ke.EmployeeId });

            // ==========================================
            // 2. CẤU HÌNH UNIQUE CONSTRAINTS
            // ==========================================
            modelBuilder.Entity<Status>().HasIndex("TenantId", nameof(Status.StatusType), nameof(Status.StatusName)).IsUnique();
            modelBuilder.Entity<Department>().HasIndex("TenantId", nameof(Department.DepartmentCode)).IsUnique();
            modelBuilder.Entity<Position>().HasIndex("TenantId", nameof(Position.PositionCode)).IsUnique();
            modelBuilder.Entity<SystemUser>().HasIndex(u => u.Username).IsUnique();
            modelBuilder.Entity<SystemUser>().HasIndex(u => u.Email).IsUnique();
            modelBuilder.Entity<SystemUser>()
                .HasIndex(u => new { u.ExternalProvider, u.ExternalSubject })
                .IsUnique()
                .HasFilter("[ExternalProvider] IS NOT NULL AND [ExternalSubject] IS NOT NULL");
            modelBuilder.Entity<Employee>().HasIndex("TenantId", nameof(Employee.EmployeeCode)).IsUnique();
            modelBuilder.Entity<Employee>().HasIndex("TenantId", nameof(Employee.SystemUserId)).IsUnique();
            modelBuilder.Entity<OKRType>().HasIndex("TenantId", nameof(OKRType.TypeName)).IsUnique();
            modelBuilder.Entity<KPIType>().HasIndex("TenantId", nameof(KPIType.TypeName)).IsUnique();
            modelBuilder.Entity<CheckInStatus>().HasIndex("TenantId", nameof(CheckInStatus.StatusName)).IsUnique();

            // ==========================================
            // 3. CẤU HÌNH FOREIGN KEYS (FLUENT API)
            // ==========================================

            // === A. NHỮNG BẢNG LIÊN KẾT ĐẾN CỘT CreatedById CỦA EMPLOYEE ===
            var entitiesWithCreatedBy = new[] {
                typeof(Role), typeof(SystemUser), typeof(MissionVision), typeof(OKR),
                typeof(KPI), typeof(Department), typeof(Employee)
            };

            foreach (var type in entitiesWithCreatedBy)
            {
                modelBuilder.Entity(type).HasOne(typeof(Employee)).WithMany().HasForeignKey("CreatedById").OnDelete(DeleteBehavior.NoAction);
            }

            // === B. CORE SYSTEM (MODULE 1 & 2) ===
            modelBuilder.Entity<Employee>().HasOne<SystemUser>().WithMany().HasForeignKey(e => e.SystemUserId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<SystemUser>().HasOne<Role>().WithMany().HasForeignKey(u => u.RoleId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<PasswordResetToken>().HasOne(token => token.SystemUser).WithMany()
                .HasForeignKey(token => token.SystemUserId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<PasswordResetToken>().HasIndex(token => token.TokenHash).IsUnique();

            modelBuilder.Entity<Role_Permission>().HasOne<Role>().WithMany().HasForeignKey(rp => rp.RoleId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Role_Permission>().HasOne<Permission>().WithMany().HasForeignKey(rp => rp.PermissionId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Department>().HasOne<Department>().WithMany().HasForeignKey(d => d.ParentDepartmentId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Department>().HasOne<Employee>().WithMany().HasForeignKey(d => d.ManagerId).OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<EmployeeAssignment>().HasOne<Employee>().WithMany().HasForeignKey(ea => ea.EmployeeId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<EmployeeAssignment>().HasOne<Position>().WithMany().HasForeignKey(ea => ea.PositionId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<EmployeeAssignment>().HasOne<Department>().WithMany().HasForeignKey(ea => ea.DepartmentId).OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<SystemParameter>().HasOne<Employee>().WithMany().HasForeignKey(sp => sp.UpdatedById).OnDelete(DeleteBehavior.NoAction);

            // === C. OKR MODULE (MODULE 3) ===
            modelBuilder.Entity<OKR>().HasOne<OKRType>().WithMany().HasForeignKey(o => o.OKRTypeId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<OKR>().HasOne<Status>().WithMany().HasForeignKey(o => o.StatusId).OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<OKRKeyResult>().HasOne<OKR>().WithMany(okr => okr.KeyResults).HasForeignKey(okr => okr.OKRId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<OKR_Mission_Mapping>().HasOne<OKR>().WithMany().HasForeignKey(omm => omm.OKRId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<OKR_Mission_Mapping>().HasOne<MissionVision>().WithMany().HasForeignKey(omm => omm.MissionId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OKR_Department_Allocation>().HasOne<OKR>().WithMany().HasForeignKey(oda => oda.OKRId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<OKR_Department_Allocation>().HasOne<Department>().WithMany().HasForeignKey(oda => oda.DepartmentId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<OKR_Employee_Allocation>().HasOne<OKR>().WithMany().HasForeignKey(oea => oea.OKRId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<OKR_Employee_Allocation>().HasOne<Employee>().WithMany().HasForeignKey(oea => oea.EmployeeId).OnDelete(DeleteBehavior.NoAction);

            // === D. KPI SETUP (MODULE 4) ===
            modelBuilder.Entity<EvaluationPeriod>().HasOne<Status>().WithMany().HasForeignKey(ep => ep.StatusId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<KPI>().HasOne<EvaluationPeriod>().WithMany().HasForeignKey(k => k.PeriodId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<KPI>().HasOne<KPIProperty>().WithMany().HasForeignKey(k => k.PropertyId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<KPI>().HasOne<KPIType>().WithMany().HasForeignKey(k => k.KPITypeId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<KPI>().HasOne<OKR>().WithMany().HasForeignKey(k => k.OKRId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<KPI>().HasOne<OKRKeyResult>().WithMany().HasForeignKey(k => k.OKRKeyResultId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<KPI>().HasOne<Employee>().WithMany().HasForeignKey(k => k.AssignerId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<KPI>().HasOne<Status>().WithMany().HasForeignKey(k => k.StatusId).OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<KPIDetail>().HasOne<KPI>().WithMany().HasForeignKey(kd => kd.KPIId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<KPI_Department_Assignment>().HasOne<KPI>().WithMany().HasForeignKey(kda => kda.KPIId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<KPI_Department_Assignment>().HasOne<Department>().WithMany().HasForeignKey(kda => kda.DepartmentId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<KPI_Employee_Assignment>().HasOne<KPI>().WithMany().HasForeignKey(kea => kea.KPIId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<KPI_Employee_Assignment>().HasOne<Employee>().WithMany().HasForeignKey(kea => kea.EmployeeId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<AdhocTask>().HasOne<Employee>().WithMany().HasForeignKey(at => at.EmployeeId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<WorkProject>().HasIndex("TenantId", nameof(WorkProject.ProjectCode)).IsUnique();
            modelBuilder.Entity<WorkProject>().HasIndex("TenantId", nameof(WorkProject.SourceOKRId));
            modelBuilder.Entity<WorkProject>().HasOne<Employee>().WithMany().HasForeignKey(p => p.OwnerId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<WorkProject>().HasOne<Employee>().WithMany().HasForeignKey(p => p.CreatedById).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<WorkProject>().HasOne(p => p.SourceOKR).WithMany(o => o.WorkProjects)
                .HasForeignKey(p => p.SourceOKRId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<WorkProjectDepartment>().HasOne<WorkProject>().WithMany(p => p.Departments).HasForeignKey(pd => pd.WorkProjectId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<WorkProjectDepartment>().HasOne<Department>().WithMany().HasForeignKey(pd => pd.DepartmentId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<WorkProjectDepartment>().HasIndex("TenantId", nameof(WorkProjectDepartment.WorkProjectId), nameof(WorkProjectDepartment.DepartmentId)).IsUnique();
            modelBuilder.Entity<WorkItem>().HasOne<WorkProject>().WithMany(p => p.WorkItems).HasForeignKey(t => t.WorkProjectId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<WorkItem>().HasOne<Employee>().WithMany().HasForeignKey(t => t.AssigneeId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<WorkItem>().HasOne<Employee>().WithMany().HasForeignKey(t => t.ReporterId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<WorkItem>().HasOne<Department>().WithMany().HasForeignKey(t => t.DepartmentId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<WorkItem>().HasOne<KPI>().WithMany().HasForeignKey(t => t.KPIId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<WorkItem>().HasOne<OKRKeyResult>().WithMany().HasForeignKey(t => t.OKRKeyResultId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<WorkItem>()
                .HasIndex(t => t.OKRKeyResultId)
                .HasFilter("[OKRKeyResultId] IS NOT NULL AND [IsActive] = 1");
            modelBuilder.Entity<WorkItemComment>().HasOne<WorkItem>().WithMany(t => t.Comments).HasForeignKey(c => c.WorkItemId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<WorkItemComment>().HasOne<Employee>().WithMany().HasForeignKey(c => c.CommenterId).OnDelete(DeleteBehavior.NoAction);

            // === E. EXECUTION & CHECK-IN (MODULE 5) ===
            modelBuilder.Entity<KPICheckIn>().HasOne<Employee>().WithMany().HasForeignKey(c => c.EmployeeId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<KPICheckIn>().HasOne<KPI>().WithMany().HasForeignKey(c => c.KPIId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<KPICheckIn>().HasOne<Employee>().WithMany().HasForeignKey(c => c.SubmittedById).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<KPICheckIn>().HasOne<Employee>().WithMany().HasForeignKey(c => c.ReviewedById).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<KPICheckIn>().HasOne<CheckInStatus>().WithMany().HasForeignKey(c => c.StatusId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<KPICheckIn>().HasOne<FailReason>().WithMany().HasForeignKey(c => c.FailReasonId).OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<CheckInDetail>().HasOne<KPICheckIn>().WithMany().HasForeignKey(cd => cd.CheckInId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<CheckInDetail>().HasIndex("TenantId", nameof(CheckInDetail.CheckInId)).IsUnique().HasFilter("[CheckInId] IS NOT NULL");
            modelBuilder.Entity<KPICheckIn>().HasIndex("TenantId", nameof(KPICheckIn.SubmissionId)).IsUnique().HasFilter("[SubmissionId] IS NOT NULL");
            modelBuilder.Entity<CheckInHistoryLog>().HasOne<KPICheckIn>().WithMany().HasForeignKey(cl => cl.CheckInId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GoalComment>().HasOne<KPI>().WithMany().HasForeignKey(gc => gc.KPIId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<GoalComment>().HasOne<KPICheckIn>().WithMany().HasForeignKey(gc => gc.CheckInId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<GoalComment>().HasOne<Employee>().WithMany().HasForeignKey(gc => gc.CommenterId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<OneOnOneMeeting>().HasOne<Employee>().WithMany().HasForeignKey(om => om.ManagerId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<OneOnOneMeeting>().HasOne<Employee>().WithMany().HasForeignKey(om => om.EmployeeId).OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<KPI_Result_Comparison>().HasOne<Employee>().WithMany().HasForeignKey(rc => rc.EmployeeId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<KPI_Result_Comparison>().HasOne<KPI>().WithMany().HasForeignKey(rc => rc.KPIId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<KPI_Result_Comparison>().HasOne<EvaluationPeriod>().WithMany().HasForeignKey(rc => rc.PeriodId).OnDelete(DeleteBehavior.NoAction);

            // === F. EVALUATION & HR (MODULE 6) ===
            modelBuilder.Entity<EvaluationResult>().HasOne<Employee>().WithMany().HasForeignKey(er => er.EmployeeId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<EvaluationResult>().HasOne<Employee>().WithMany().HasForeignKey(er => er.SubmittedById).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<EvaluationResult>().HasOne<Employee>().WithMany().HasForeignKey(er => er.DirectorReviewedById).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<EvaluationResult>().HasOne<EvaluationPeriod>().WithMany().HasForeignKey(er => er.PeriodId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<EvaluationResult>().HasOne<GradingRank>().WithMany().HasForeignKey(er => er.RankId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<EvaluationResult>().HasIndex("TenantId", nameof(EvaluationResult.EmployeeId), nameof(EvaluationResult.PeriodId)).IsUnique()
                .HasFilter("[EmployeeId] IS NOT NULL AND [PeriodId] IS NOT NULL");

            modelBuilder.Entity<KPIAdjustmentHistory>().HasOne<KPI>().WithMany().HasForeignKey(ka => ka.KPIId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<KPIAdjustmentHistory>().HasOne<Employee>().WithMany().HasForeignKey(ka => ka.AdjusterId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<BonusRule>().HasOne<GradingRank>().WithMany().HasForeignKey(br => br.RankId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<BonusRule>()
                .HasIndex("TenantId", nameof(BonusRule.RankId))
                .IsUnique()
                .HasFilter("[RankId] IS NOT NULL");

            modelBuilder.Entity<RealtimeExpectedBonus>().HasOne<Employee>().WithMany().HasForeignKey(rb => rb.EmployeeId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<RealtimeExpectedBonus>().HasOne<EvaluationPeriod>().WithMany().HasForeignKey(rb => rb.PeriodId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<RealtimeExpectedBonus>()
                .HasIndex("TenantId", nameof(RealtimeExpectedBonus.EmployeeId), nameof(RealtimeExpectedBonus.PeriodId))
                .IsUnique()
                .HasFilter("[EmployeeId] IS NOT NULL AND [PeriodId] IS NOT NULL");
            modelBuilder.Entity<HRExportReport>().HasOne<EvaluationPeriod>().WithMany().HasForeignKey(hr => hr.PeriodId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<EvaluationReportIncident>().HasOne<Department>().WithMany().HasForeignKey(i => i.DepartmentId).OnDelete(DeleteBehavior.NoAction);

            // === G. SYSTEM ===
            modelBuilder.Entity<SystemAlert>().HasOne<Employee>().WithMany().HasForeignKey(sa => sa.ReceiverId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<SystemAlert>().HasOne<EvaluationPeriod>().WithMany().HasForeignKey(sa => sa.PeriodId).OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<AuditLog>().HasOne(al => al.SystemUser).WithMany().HasForeignKey(al => al.SystemUserId).OnDelete(DeleteBehavior.NoAction);

            // Precision and Scale settings
            modelBuilder.Entity<PaymentTransaction>().Property(p => p.Amount).HasPrecision(18, 2);
            modelBuilder.Entity<SaaSPackage>().Property(s => s.PricePerMonth).HasPrecision(18, 2);
        }

        private void ConfigureTenantScope<TEntity>(ModelBuilder modelBuilder) where TEntity : class
        {
            modelBuilder.Entity<TEntity>().Property<int>("TenantId").IsRequired();
            modelBuilder.Entity<TEntity>().HasOne<Tenant>().WithMany().HasForeignKey("TenantId")
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<TEntity>().HasQueryFilter(entity =>
                TenantAccessUnrestricted ||
                EF.Property<int>(entity, "TenantId") == TenantFilterId);
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            ApplyTenantWriteRules();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyTenantWriteRules();
            return base.SaveChangesAsync(cancellationToken);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            ApplyTenantWriteRules();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void ApplyTenantWriteRules()
        {
            ValidateKnowledgePersistenceUris();
            // The compatibility context is used only by design-time tooling and existing unit tests.
            // Real HTTP requests set IsProductionRequest and therefore never take this path.
            if (!_tenantContext.IsProductionRequest)
            {
                foreach (var entry in ChangeTracker.Entries()
                             .Where(entry => entry.State == EntityState.Added &&
                                             entry.Metadata.FindProperty("TenantId") is not null))
                {
                    var tenantProperty = entry.Property("TenantId");
                    if (tenantProperty.IsTemporary)
                    {
                        tenantProperty.CurrentValue = 0;
                        tenantProperty.IsTemporary = false;
                    }
                }
                return;
            }

            var tenantEntries = ChangeTracker.Entries()
                .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                .Where(entry => entry.Metadata.FindProperty("TenantId") is not null)
                .ToList();

            if (tenantEntries.Count == 0)
            {
                return;
            }

            if (!_tenantContext.TenantId.HasValue && !_tenantContext.HasAuditedPlatformBypass)
            {
                throw new InvalidOperationException("A production write to tenant data requires a resolved tenant. The operation was rejected.");
            }

            foreach (var entry in tenantEntries)
            {
                var tenantProperty = entry.Property("TenantId");
                var entityTenantId = GetTenantId(tenantProperty.CurrentValue);

                if (_tenantContext.HasAuditedPlatformBypass)
                {
                    // Cross-tenant platform operations must name their target tenant and carry middleware audit evidence.
                    if (entityTenantId <= 0)
                    {
                        throw new InvalidOperationException("An audited platform bypass must explicitly set TenantId on every tenant-owned write.");
                    }
                }
                else
                {
                    var currentTenantId = _tenantContext.TenantId!.Value;
                    if (entry.State == EntityState.Added)
                    {
                        // EF assigns temporary negative values when a shadow TenantId
                        // participates in an alternate key. Only an EF-marked temporary
                        // value is treated as unassigned; posted/persisted mismatches still fail.
                        if (tenantProperty.IsTemporary)
                        {
                            tenantProperty.CurrentValue = currentTenantId;
                            tenantProperty.IsTemporary = false;
                        }
                        else if (entityTenantId != 0 && entityTenantId != currentTenantId)
                        {
                            throw new InvalidOperationException("The supplied TenantId does not match the current tenant.");
                        }
                        else
                        {
                            tenantProperty.CurrentValue = currentTenantId;
                        }
                        entityTenantId = currentTenantId;
                    }
                    else if (entityTenantId != currentTenantId)
                    {
                        throw new InvalidOperationException("A tenant-owned entity cannot be modified or deleted from another tenant.");
                    }

                    if (entry.State == EntityState.Modified && tenantProperty.IsModified)
                    {
                        if (tenantProperty.OriginalValue != null &&
                            !Equals(tenantProperty.OriginalValue, tenantProperty.CurrentValue))
                        {
                            throw new InvalidOperationException("TenantId is immutable after an entity is created.");
                        }

                        tenantProperty.IsModified = false;
                    }
                }

                ValidateTenantForeignKeys(entry, entityTenantId);
            }
        }

        private void ValidateKnowledgePersistenceUris()
        {
            foreach (var entry in ChangeTracker.Entries<KnowledgeDocumentVersion>()
                         .Where(entry => entry.State is EntityState.Added or EntityState.Modified))
            {
                if (!KnowledgeDocumentSourcePolicy.IsStableHttpsUri(entry.Entity.SourceBlobUri))
                {
                    throw new InvalidOperationException(
                        "Knowledge document source URI must be stable HTTPS metadata without credentials.");
                }
            }

            foreach (var entry in ChangeTracker.Entries<KnowledgeChunk>()
                         .Where(entry => entry.State is EntityState.Added or EntityState.Modified))
            {
                if (!KnowledgeDocumentSourcePolicy.IsStableHttpsUri(entry.Entity.ContentBlobUri))
                {
                    throw new InvalidOperationException(
                        "Knowledge chunk URI must be stable HTTPS metadata without credentials.");
                }
            }

            foreach (var entry in ChangeTracker.Entries<DocumentIngestionJob>()
                         .Where(entry => entry.State is EntityState.Added or EntityState.Modified))
            {
                if (entry.Entity.ParserResultBlobUri != null &&
                    !KnowledgeDocumentSourcePolicy.IsStableHttpsUri(entry.Entity.ParserResultBlobUri))
                {
                    throw new InvalidOperationException(
                        "MinerU result URI must be stable HTTPS metadata without credentials.");
                }
            }
        }

        private void ValidateTenantForeignKeys(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry, int dependentTenantId)
        {
            if (entry.State == EntityState.Deleted)
            {
                return;
            }

            foreach (var foreignKey in entry.Metadata.GetForeignKeys()
                         .Where(fk => fk.PrincipalEntityType.FindProperty("TenantId") is not null))
            {
                var foreignKeyValues = foreignKey.Properties
                    .Select(property => entry.Property(property.Name).CurrentValue)
                    .ToArray();

                if (foreignKeyValues.Any(value => value is null))
                {
                    continue;
                }

                var principalType = foreignKey.PrincipalEntityType.ClrType;
                if (principalType is null)
                {
                    continue;
                }

                var principalKeyProperties = foreignKey.PrincipalKey.Properties;
                var principalTenantIndex = principalKeyProperties
                    .Select((property, index) => new { property.Name, index })
                    .Where(item => item.Name == "TenantId")
                    .Select(item => item.index)
                    .DefaultIfEmpty(-1)
                    .First();
                if (principalTenantIndex >= 0 &&
                    GetTenantId(foreignKeyValues[principalTenantIndex]) != dependentTenantId)
                {
                    throw new InvalidOperationException(
                        $"Cross-tenant reference from {entry.Metadata.ClrType.Name} to {principalType.Name} was rejected.");
                }

                var primaryKey = foreignKey.PrincipalEntityType.FindPrimaryKey()
                    ?? throw new InvalidOperationException(
                        $"Tenant-owned principal {principalType.Name} has no primary key.");
                var primaryKeyValues = new object?[primaryKey.Properties.Count];
                for (var index = 0; index < primaryKey.Properties.Count; index++)
                {
                    var primaryPropertyName = primaryKey.Properties[index].Name;
                    var principalIndex = principalKeyProperties
                        .Select((property, propertyIndex) => new { property.Name, propertyIndex })
                        .Where(item => item.Name == primaryPropertyName)
                        .Select(item => item.propertyIndex)
                        .DefaultIfEmpty(-1)
                        .First();
                    if (principalIndex < 0)
                    {
                        throw new InvalidOperationException(
                            $"Tenant-owned reference to {principalType.Name} must include its primary key.");
                    }
                    primaryKeyValues[index] = foreignKeyValues[principalIndex];
                }

                var principal = Find(principalType, primaryKeyValues);
                if (principal is null)
                {
                    throw new InvalidOperationException(
                        $"Tenant-owned reference from {entry.Metadata.ClrType.Name} could not be resolved in the current tenant.");
                }

                var principalTenantId = GetTenantId(Entry(principal).Property("TenantId").CurrentValue);
                if (principalTenantId != dependentTenantId)
                {
                    throw new InvalidOperationException(
                        $"Cross-tenant reference from {entry.Metadata.ClrType.Name} to {principalType.Name} was rejected.");
                }
            }
        }

        private static int GetTenantId(object? value) => value is int tenantId ? tenantId : 0;
    }
}
