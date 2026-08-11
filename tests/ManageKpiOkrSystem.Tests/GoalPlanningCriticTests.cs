using Manage_KPI_or_OKR_System.Controllers;
using Manage_KPI_or_OKR_System.Models.AI;
using Manage_KPI_or_OKR_System.Services.AI;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class GoalPlanningCriticTests
{
    [Fact]
    public void Review_PassesConcreteCandidateWithCurrentDirectEvidence()
    {
        var candidate = Candidate(
            "Validate the retention cohort",
            "Confirm the measurable cohort and publish the reviewed baseline before execution.",
            CurrentEvidence(),
            new EvidenceConfidence(.85, EvidenceConfidenceBand.High, false, 1));

        var critique = Assert.Single(new GoalPlanningCritic().Review(
            sourceHasMeasurableTarget: true,
            new[] { candidate }));

        Assert.Equal(GoalPlanningCritiqueVerdict.Pass, critique.Verdict);
        Assert.NotEmpty(critique.Reasons);
        Assert.Null(candidate.Critique);
    }

    [Fact]
    public void Review_RequiresHumanReviewWhenMeasurableTargetIsMissing()
    {
        var critique = Assert.Single(new GoalPlanningCritic().Review(
            sourceHasMeasurableTarget: false,
            new[]
            {
                Candidate(
                    "Prepare a reviewed execution milestone",
                    "Document one concrete deliverable and its review checkpoint before starting work.",
                    CurrentEvidence(),
                    new EvidenceConfidence(.8, EvidenceConfidenceBand.High, false, 1))
            }));

        Assert.Equal(GoalPlanningCritiqueVerdict.NeedsHumanReview, critique.Verdict);
        Assert.Contains(critique.Reasons, reason => reason.Contains("đo lường", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Review_AbstainsWithoutEvidence()
    {
        var critique = Assert.Single(new GoalPlanningCritic().Review(
            sourceHasMeasurableTarget: true,
            new[]
            {
                Candidate(
                    "Prepare a reviewed execution milestone",
                    "Document one concrete deliverable and its review checkpoint before starting work.",
                    Array.Empty<EvidenceRef>(),
                    new EvidenceConfidence(0, EvidenceConfidenceBand.Abstain, true, 0))
            }));

        Assert.Equal(GoalPlanningCritiqueVerdict.Abstain, critique.Verdict);
        Assert.NotEmpty(critique.Reasons);
    }

    [Fact]
    public void CreateGoalPlanningDraft_RequiresSameWritePermissionsAsConfirmation()
    {
        var method = typeof(AIController).GetMethod(nameof(AIController.CreateGoalPlanningDraft));
        var attribute = Assert.Single(method!.GetCustomAttributes(typeof(HasPermissionAttribute), true)
            .Cast<HasPermissionAttribute>());

        var permissions = Assert.IsType<string[]>(Assert.Single(attribute.Arguments!));
        Assert.Equal(new[] { "WORKITEMS_CREATE", "WORKPROJECTS_EDIT" }, permissions);
    }

    [Fact]
    public void ViewGoalPlanningDraft_RequiresWritePermissionsAndAntiforgery()
    {
        var method = typeof(AIController).GetMethod(nameof(AIController.ViewGoalPlanningDraft));
        var permission = Assert.Single(method!.GetCustomAttributes(typeof(HasPermissionAttribute), true)
            .Cast<HasPermissionAttribute>());
        var permissions = Assert.IsType<string[]>(Assert.Single(permission.Arguments!));

        Assert.Equal(new[] { "WORKITEMS_CREATE", "WORKPROJECTS_EDIT" }, permissions);
        Assert.NotEmpty(method.GetCustomAttributes(
            typeof(Microsoft.AspNetCore.Mvc.ValidateAntiForgeryTokenAttribute),
            true));
    }

    [Fact]
    public void GoalPlanningModal_WiresAuthorizedProjectOptionsAndCritiqueFromDraftResponse()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(root, "Views", "Shared", "_AITaskDecomposeModal.cshtml"));

        Assert.Contains(
            "availableProjects: data.availableProjects || data.AvailableProjects || []",
            source,
            StringComparison.Ordinal);
        Assert.Contains(
            "critique: fieldValue(task, 'critique', 'Critique', null)",
            source,
            StringComparison.Ordinal);
        Assert.Contains(
            "outcomeHistory: fieldValue(task, 'outcomeHistory', 'OutcomeHistory', null)",
            source,
            StringComparison.Ordinal);
        Assert.Contains("historyCompletedCount", source, StringComparison.Ordinal);
        Assert.DoesNotContain("outcomeLikelihood", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("'probability'", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(
            "state.availableAssignees = data.availableAssignees || data.AvailableAssignees || []",
            source,
            StringComparison.Ordinal);
        Assert.Contains("data-field=\"dueDate\"", source, StringComparison.Ordinal);
        Assert.Contains("Fit ${escapeHtml(fitSummary)}", source, StringComparison.Ordinal);
        Assert.Contains("sourceOKRId: state.sourceOkrId", source, StringComparison.Ordinal);
        Assert.Contains("approvalToken: state.approvalToken", source, StringComparison.Ordinal);
        Assert.Contains("idempotencyKey: state.rejectionIdempotencyKey", source, StringComparison.Ordinal);
        Assert.Contains("/AI/RejectGoalPlanningDraft", source, StringComparison.Ordinal);
        Assert.Contains("/AI/ViewGoalPlanningDraft", source, StringComparison.Ordinal);
        var okrScript = File.ReadAllText(Path.Combine(root, "wwwroot", "js", "okrs-index.js"));
        var objectiveCard = File.ReadAllText(Path.Combine(root, "Views", "OKRs", "_OkrObjectiveCard.cshtml"));
        Assert.Contains("ai-decompose-kr", objectiveCard, StringComparison.Ordinal);
        Assert.Contains("case 'ai-decompose-kr'", okrScript, StringComparison.Ordinal);
        Assert.Contains("window.openAiTaskDecomposeModal(", okrScript, StringComparison.Ordinal);
    }

    private static GoalPlanningTaskCandidate Candidate(
        string title,
        string description,
        IReadOnlyList<EvidenceRef> evidence,
        EvidenceConfidence confidence) =>
        new(
            title,
            description,
            new GoalTaskFitBreakdown(
                80,
                80,
                80,
                80,
                80,
                80,
                80,
                FitScoreBand.GoodFit,
                HasSufficientEvidence: true),
            confidence,
            evidence);

    private static IReadOnlyList<EvidenceRef> CurrentEvidence() =>
        new[]
        {
            new EvidenceRef(
                "KPI",
                "1",
                DateTimeOffset.UtcNow,
                .8,
                IsDirectlyRelevant: true,
                IsCurrent: true)
        };

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Manage-KPI-or-OKR-System.csproj")))
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Repository root was not found from the test output directory.");
    }
}
