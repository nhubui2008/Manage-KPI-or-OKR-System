using System.Security.Claims;
using Manage_KPI_or_OKR_System.Controllers;
using Manage_KPI_or_OKR_System.Models.ViewModels;
using Manage_KPI_or_OKR_System.Services.AI;
using Manage_KPI_or_OKR_System.Services.Tenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class KnowledgeDocumentsControllerTests
{
    [Theory]
    [InlineData("Employee")]
    [InlineData("Manager")]
    public async Task Index_RejectsRolesWithoutKnowledgeAdministrationAuthority(string role)
    {
        var service = new RecordingAdministrationService();
        var controller = Controller(role, service, resolvedTenant: true);

        Assert.IsType<ForbidResult>(await controller.Index(CancellationToken.None));
        Assert.Equal(0, service.BuildIndexCalls);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Administrator")]
    [InlineData("Director")]
    [InlineData("HR")]
    public async Task Index_AllowsExplicitTenantKnowledgeAdministratorRoles(string role)
    {
        var service = new RecordingAdministrationService();
        var controller = Controller(role, service, resolvedTenant: true);

        var view = Assert.IsType<ViewResult>(await controller.Index(CancellationToken.None));
        Assert.IsType<KnowledgeDocumentsIndexViewModel>(view.Model);
        Assert.Equal(1, service.BuildIndexCalls);
    }

    [Fact]
    public async Task Index_RejectsPlatformStyleRoleWhenTenantIsUnresolved()
    {
        var service = new RecordingAdministrationService();
        var controller = Controller("Admin", service, resolvedTenant: false);

        Assert.IsType<ForbidResult>(await controller.Index(CancellationToken.None));
        Assert.Equal(0, service.BuildIndexCalls);
    }

    [Fact]
    public async Task EveryMutation_RejectsRoleWithoutKnowledgeAdministrationAuthority()
    {
        var controller = Controller("Employee", new RecordingAdministrationService(), resolvedTenant: true);

        Assert.IsType<ForbidResult>(await controller.Upload(
            new KnowledgeDocumentUploadInput(), CancellationToken.None));
        Assert.IsType<ForbidResult>(await controller.UpdateAccess(
            new KnowledgeDocumentAccessInput(), CancellationToken.None));
        Assert.IsType<ForbidResult>(await controller.Delete(
            new KnowledgeDocumentMutationInput(), CancellationToken.None));
        Assert.IsType<ForbidResult>(await controller.Retry(
            new KnowledgeDocumentRetryInput(), CancellationToken.None));
        Assert.IsType<ForbidResult>(await controller.RetryCheckInAi(
            new CheckInAiOutboxRetryInput(), CancellationToken.None));
    }

    private static KnowledgeDocumentsController Controller(
        string role,
        RecordingAdministrationService service,
        bool resolvedTenant)
    {
        var tenantContext = new TenantContext();
        tenantContext.SetRequest(resolvedTenant ? 1 : null, resolvedTenant ? 99 : null);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "99"),
            new Claim(ClaimTypes.Role, role)
        }, "Test"));
        return new KnowledgeDocumentsController(service, new RecordingOutboxService(), tenantContext)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            }
        };
    }

    private sealed class RecordingOutboxService : ICheckInAiEvaluationOutboxAdministrationService
    {
        public Task<CheckInAiOutboxOverview> BuildOverviewAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(CheckInAiOutboxOverview.Empty);

        public Task<bool> RetryDeadLetterAsync(
            CheckInAiOutboxRetryInput input,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class RecordingAdministrationService : IKnowledgeDocumentAdministrationService
    {
        public int BuildIndexCalls { get; private set; }

        public Task<KnowledgeDocumentsIndexViewModel> BuildIndexAsync(
            KnowledgeDocumentUploadInput? upload = null,
            CancellationToken cancellationToken = default)
        {
            BuildIndexCalls++;
            return Task.FromResult(new KnowledgeDocumentsIndexViewModel());
        }

        public Task<KnowledgeDocumentUploadResult> UploadAsync(
            KnowledgeDocumentUploadInput input,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<bool> UpdateAccessAsync(
            KnowledgeDocumentAccessInput input,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<bool> SoftDeleteAsync(
            KnowledgeDocumentMutationInput input,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<bool> RetryAsync(
            KnowledgeDocumentRetryInput input,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
