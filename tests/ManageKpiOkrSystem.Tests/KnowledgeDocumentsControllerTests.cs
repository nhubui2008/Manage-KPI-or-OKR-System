using System.Security.Claims;
using Manage_KPI_or_OKR_System.Controllers;
using Manage_KPI_or_OKR_System.Models.ViewModels;
using Manage_KPI_or_OKR_System.Services.AI;
using Manage_KPI_or_OKR_System.Services.Tenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
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

    [Fact]
    public async Task Upload_WhenAdministrationThrowsKnowledgeDocumentAdministrationException_RendersIndexWithModelError()
    {
        var service = new RecordingAdministrationService
        {
            OnUpload = _ => throw new KnowledgeDocumentAdministrationException("Dung lượng vượt quá giới hạn")
        };
        var controller = Controller("Admin", service, resolvedTenant: true);

        var result = await controller.Upload(
            new KnowledgeDocumentUploadInput { Title = "Quy chế KPI" },
            CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("Index", viewResult.ViewName);
        Assert.False(controller.ModelState.IsValid);
        var error = Assert.Single(controller.ModelState[string.Empty]!.Errors);
        Assert.Equal("Dung lượng vượt quá giới hạn", error.ErrorMessage);
    }

    [Fact]
    public async Task Upload_WhenAdministrationThrowsHttpRequestException_RendersIndexWithModelError()
    {
        var service = new RecordingAdministrationService
        {
            OnUpload = _ => throw new HttpRequestException("Connection actively refused by host 127.0.0.1:9100")
        };
        var controller = Controller("Admin", service, resolvedTenant: true);

        var result = await controller.Upload(
            new KnowledgeDocumentUploadInput { Title = "Quy chế KPI" },
            CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("Index", viewResult.ViewName);
        Assert.False(controller.ModelState.IsValid);
        var error = Assert.Single(controller.ModelState[string.Empty]!.Errors);
        Assert.Contains("Không thể kết nối đến dịch vụ lưu trữ đối tượng", error.ErrorMessage);
    }

    [Fact]
    public async Task Upload_WhenAdministrationThrowsUnexpectedException_RendersIndexWithModelError()
    {
        var service = new RecordingAdministrationService
        {
            OnUpload = _ => throw new InvalidOperationException("Unexpected internal failure")
        };
        var controller = Controller("Admin", service, resolvedTenant: true);

        var result = await controller.Upload(
            new KnowledgeDocumentUploadInput { Title = "Quy chế KPI" },
            CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("Index", viewResult.ViewName);
        Assert.False(controller.ModelState.IsValid);
        var error = Assert.Single(controller.ModelState[string.Empty]!.Errors);
        Assert.Contains("Lỗi hệ thống khi tải tài liệu lên", error.ErrorMessage);
    }

    [Fact]
    public async Task Retry_WhenAdministrationThrowsException_RedirectsWithErrorMessageInTempData()
    {
        var service = new RecordingAdministrationService
        {
            OnRetry = _ => throw new InvalidOperationException("Service unavailable")
        };
        var controller = Controller("Admin", service, resolvedTenant: true);

        var result = await controller.Retry(
            new KnowledgeDocumentRetryInput { VersionId = Guid.NewGuid(), JobId = Guid.NewGuid() },
            CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Contains("Lỗi hệ thống khi yêu cầu xử lý lại", Assert.IsType<string>(controller.TempData["ErrorMessage"]));
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
        var httpContext = new DefaultHttpContext { User = principal };
        return new KnowledgeDocumentsController(service, new RecordingOutboxService(), tenantContext)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            },
            TempData = new TempDataDictionary(httpContext, new TestTempDataProvider())
        };
    }

    private sealed class TestTempDataProvider : ITempDataProvider
    {
        private readonly Dictionary<string, object> _values = new();
        public IDictionary<string, object> LoadTempData(HttpContext context) => _values;
        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
            foreach (var (k, v) in values) _values[k] = v;
        }
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
        public Func<KnowledgeDocumentUploadInput, Task<KnowledgeDocumentUploadResult>>? OnUpload { get; set; }
        public Func<KnowledgeDocumentRetryInput, Task<bool>>? OnRetry { get; set; }

        public Task<KnowledgeDocumentsIndexViewModel> BuildIndexAsync(
            KnowledgeDocumentUploadInput? upload = null,
            CancellationToken cancellationToken = default)
        {
            BuildIndexCalls++;
            return Task.FromResult(new KnowledgeDocumentsIndexViewModel());
        }

        public Task<KnowledgeDocumentUploadResult> UploadAsync(
            KnowledgeDocumentUploadInput input,
            CancellationToken cancellationToken = default) =>
            OnUpload != null ? OnUpload(input) : throw new NotSupportedException();

        public Task<bool> UpdateAccessAsync(
            KnowledgeDocumentAccessInput input,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<bool> SoftDeleteAsync(
            KnowledgeDocumentMutationInput input,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<bool> RetryAsync(
            KnowledgeDocumentRetryInput input,
            CancellationToken cancellationToken = default) =>
            OnRetry != null ? OnRetry(input) : throw new NotSupportedException();
    }
}
