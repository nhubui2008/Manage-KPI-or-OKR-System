using System.ComponentModel.DataAnnotations;
using Manage_KPI_or_OKR_System.Controllers;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class AuthControllerRegistrationTests
{
    [Fact]
    public async Task Register_ValidModelNormalizesAndCreatesRolelessActiveCustomer()
    {
        await using var context = CreateContext();
        var controller = CreateController(context);
        var model = ValidModel(
            username: "  Mixed.User  ",
            email: "  Mixed.User@Example.COM  ");
        Assert.True(Validator.TryValidateObject(
            model,
            new ValidationContext(model),
            new List<ValidationResult>(),
            validateAllProperties: true));

        var result = await controller.Register(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Login", redirect.ActionName);

        var user = Assert.Single(await context.SystemUsers.ToListAsync());
        Assert.Equal("Mixed.User", user.Username);
        Assert.Equal("mixed.user@example.com", user.Email);
        Assert.True(PasswordHelper.VerifyPassword(model.Password, user.PasswordHash!));
        Assert.Null(user.RoleId);
        Assert.True(user.IsActive);
        Assert.NotNull(user.CreatedAt);
        Assert.NotNull(user.LastPasswordChange);
        Assert.Null(user.CreatedById);
        Assert.Null(user.TrialEndTime);
        Assert.Equal("Tiếng Việt", user.PreferredLanguage);
        Assert.Equal("Mixed.User", model.Username);
        Assert.Equal("mixed.user@example.com", model.Email);
    }

    [Fact]
    public void RegisterViewModel_RejectsInvalidRegistrationData()
    {
        var invalidModels = new[]
        {
            ValidModel(username: string.Empty),
            ValidModel(username: new string('u', 256)),
            ValidModel(email: "not-an-email"),
            ValidModel(email: $"{new string('e', 244)}@example.com"),
            ValidModel(password: "12345", confirmPassword: "12345"),
            ValidModel(password: new string('p', 129), confirmPassword: new string('p', 129)),
            ValidModel(password: "abc 123", confirmPassword: "abc 123"),
            ValidModel(password: "abcdef", confirmPassword: "different")
        };

        Assert.All(invalidModels, model =>
            Assert.False(Validator.TryValidateObject(
                model,
                new ValidationContext(model),
                new List<ValidationResult>(),
                validateAllProperties: true)));
    }

    [Fact]
    public async Task Register_InvalidModelReturnsSameModelWithoutSaving()
    {
        await using var context = CreateContext();
        var controller = CreateController(context);
        var model = ValidModel(email: "invalid-email");
        AddValidationErrors(controller, model);

        var result = await controller.Register(model);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(model, view.Model);
        Assert.False(controller.ModelState.IsValid);
        Assert.Empty(await context.SystemUsers.ToListAsync());
    }

    [Theory]
    [InlineData(" EXISTING.USER ", "new.user@example.com")]
    [InlineData("new.user", " OWNER@EXAMPLE.COM ")]
    public async Task Register_CaseInsensitiveDuplicateReturnsSameModelWithoutSaving(
        string username,
        string email)
    {
        await using var context = CreateContext();
        context.SystemUsers.Add(new SystemUser
        {
            Username = "Existing.User",
            Email = "owner@example.com",
            PasswordHash = PasswordHelper.HashPassword("Existing1!"),
            IsActive = true
        });
        await context.SaveChangesAsync();
        var controller = CreateController(context);
        var model = ValidModel(username, email);

        var result = await controller.Register(model);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(model, view.Model);
        Assert.False(controller.ModelState.IsValid);
        Assert.Contains(
            controller.ModelState[string.Empty]!.Errors,
            error => error.ErrorMessage.Contains("đã tồn tại", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(1, await context.SystemUsers.CountAsync());
    }

    private static RegisterViewModel ValidModel(
        string username = "new.user",
        string email = "new.user@example.com",
        string password = "Secure1!",
        string? confirmPassword = null)
    {
        return new RegisterViewModel
        {
            Username = username,
            Email = email,
            Password = password,
            ConfirmPassword = confirmPassword ?? password
        };
    }

    private static void AddValidationErrors(Controller controller, object model)
    {
        var validationResults = new List<ValidationResult>();
        Validator.TryValidateObject(
            model,
            new ValidationContext(model),
            validationResults,
            validateAllProperties: true);

        foreach (var validationResult in validationResults)
        {
            var memberNames = validationResult.MemberNames.Any()
                ? validationResult.MemberNames
                : new[] { string.Empty };

            foreach (var memberName in memberNames)
            {
                controller.ModelState.AddModelError(
                    memberName,
                    validationResult.ErrorMessage ?? "Giá trị không hợp lệ.");
            }
        }
    }

    private static AuthController CreateController(MiniERPDbContext context)
    {
        var httpContext = new DefaultHttpContext();
        return new AuthController(context, null!, null!, null!, null!, null!, null!, null!)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            },
            TempData = new TempDataDictionary(httpContext, new TestTempDataProvider())
        };
    }

    private static MiniERPDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MiniERPDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new MiniERPDbContext(options);
    }

    private sealed class TestTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context)
        {
            return new Dictionary<string, object>();
        }

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }
}
