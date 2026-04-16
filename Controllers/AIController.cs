using System.Text;
using System.Text.Json;
using Manage_KPI_or_OKR_System.Models.AI;
using Manage_KPI_or_OKR_System.Services;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize]
    public class AIController : Controller
    {
        private readonly IAIDataService _dataService;
        private readonly IAIAlertService _alertService;
        private readonly IGeminiService _geminiService;
        private readonly Manage_KPI_or_OKR_System.Data.MiniERPDbContext _context;
        private readonly ILogger<AIController> _logger;
        private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

        public AIController(
            IAIDataService dataService,
            IAIAlertService alertService,
            IGeminiService geminiService,
            Manage_KPI_or_OKR_System.Data.MiniERPDbContext context,
            ILogger<AIController> logger)
        {
            _dataService = dataService;
            _alertService = alertService;
            _geminiService = geminiService;
            _context = context;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Chat([FromBody] AIChatRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new AITextResponse { Success = false, Warnings = { "Vui long nhap cau hoi cho AI." } });
            }

            try
            {
                var context = await _dataService.BuildChatContextAsync(User, request.PeriodId);
                var prompt = new StringBuilder();
                prompt.AppendLine(context);
                prompt.AppendLine("Lich su chat gan day:");
                foreach (var message in request.History?.TakeLast(8) ?? Enumerable.Empty<AIChatMessage>())
                {
                    prompt.AppendLine($"- {message.Role}: {message.Text}");
                }
                prompt.AppendLine("Cau hoi hien tai:");
                prompt.AppendLine(request.Message);
                if (IsImprovementSuggestionRequest(request.Message))
                {
                    prompt.AppendLine("Yeu cau dinh dang bat buoc cho cau hoi nay:");
                    prompt.AppendLine("- Tra ve dung 3 hanh dong cai thien, danh so 1, 2, 3 tren 3 dong rieng.");
                    prompt.AppendLine("- Moi hanh dong gom ten hanh dong ngan gon va 1 cau cach lam cu the dua tren context.");
                    prompt.AppendLine("- Khong dung loi mo dau dai; khong chi tra ve 1 muc.");
                }

                var text = await _geminiService.GenerateTextAsync(
                    "Ban la VietMach AI Assistant cho he thong KPI/OKR. Tra loi bang tieng Viet, ngan gon, thuc te, chi dua vao context duoc cap. Neu thieu du lieu, noi ro thieu du lieu. Luon ton trong dung so luong muc ma nguoi dung yeu cau.",
                    prompt.ToString(),
                    new GeminiGenerationOptions { Temperature = 0.35 },
                    cancellationToken);

                return Ok(new AITextResponse { Text = text });
            }
            catch (Exception ex)
            {
                return HandleAIException(ex, "chatbot AI");
            }
        }

        [HttpPost]
        [HasPermission("KPIS_CREATE")]
        public async Task<IActionResult> SuggestKPI([FromBody] SuggestKpiRequest request, CancellationToken cancellationToken)
        {
            if (User.IsInRole("Employee") || User.IsInRole("employee") || User.IsInRole("Sales") || User.IsInRole("sales"))
            {
                return StatusCode(403, new SuggestKpiResponse { Success = false, Warnings = { "Vai tro hien tai khong duoc phep dung AI de goi y KPI." } });
            }

            try
            {
                var context = await _dataService.BuildKpiSuggestionContextAsync(User, request);
                var prompt = context + "\nHay goi y 3-5 KPI phu hop. Chi tra ve JSON array hop le voi field: name, targetValue, unit, passThreshold, failThreshold, rationale.";
                var text = await _geminiService.GenerateTextAsync(
                    "Ban la chuyen gia KPI/OKR. Tao KPI do luong duoc, gan voi vi tri/phong ban/OKR, khong trung lap voi KPI da co.",
                    prompt,
                    new GeminiGenerationOptions { Temperature = 0.35, ResponseMimeType = "application/json" },
                    cancellationToken);

                var suggestions = ParseSuggestedKpis(text);
                if (!suggestions.Any())
                {
                    return StatusCode(502, new SuggestKpiResponse { Success = false, Warnings = { "Gemini chua tra ve danh sach KPI hop le." } });
                }

                await SaveAIHistoryAsync("SuggestKPI", null, prompt, text);

                return Ok(new SuggestKpiResponse { Suggestions = suggestions });
            }
            catch (Exception ex)
            {
                return HandleSuggestException(ex);
            }
        }

        [HttpGet]
        [HasPermission("KPIS_CREATE")]
        public async Task<IActionResult> SuggestKpiOptions([FromQuery] SuggestKpiOptionsRequest request)
        {
            if (User.IsInRole("Employee") || User.IsInRole("employee") || User.IsInRole("Sales") || User.IsInRole("sales"))
            {
                return StatusCode(403, new SuggestKpiOptionsResponse { Success = false, Warnings = { "Vai tro hien tai khong duoc phep dung AI de goi y KPI." } });
            }

            try
            {
                var response = await _dataService.GetKpiSuggestionOptionsAsync(User, request);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new SuggestKpiOptionsResponse { Success = false, Warnings = { ex.Message } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load KPI suggestion options");
                return StatusCode(500, new SuggestKpiOptionsResponse { Success = false, Warnings = { "Khong the tai danh sach lua chon cho AI goi y KPI." } });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AnalyzePerformance([FromBody] AnalyzePerformanceRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var context = await _dataService.BuildPerformanceContextAsync(User, request);
                var text = await _geminiService.GenerateTextAsync(
                    "Ban la AI phan tich hieu suat KPI/OKR. Viet bang tieng Viet, cau truc gom: Tong quan, Diem manh, Rui ro, Goi y hanh dong. Khong bia so lieu.",
                    context,
                    new GeminiGenerationOptions { Temperature = 0.3 },
                    cancellationToken);

                await SaveAIHistoryAsync("AnalyzePerformance", request.PeriodId, context, text);

                return Ok(new AITextResponse { Text = text });
            }
            catch (Exception ex)
            {
                return HandleAIException(ex, "phan tich hieu suat AI");
            }
        }

        [HttpPost]
        public async Task<IActionResult> GenerateReview([FromBody] GenerateReviewRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var reviewContext = await _dataService.BuildReviewContextAsync(User, request.EvaluationResultId);
                if (!reviewContext.IsAllowed)
                {
                    return StatusCode(403, new AITextResponse { Success = false, Warnings = { "Ban khong co quyen tao nhan xet danh gia cho ket qua nay." } });
                }

                var text = await _geminiService.GenerateTextAsync(
                    "Ban la tro ly HR viet nhan xet danh gia 1-1. Viet 1-2 doan tieng Viet chuyen nghiep, can bang thanh tich/rui ro/goi y cai thien, dua tren du lieu thuc te.",
                    reviewContext.ContextText,
                    new GeminiGenerationOptions { Temperature = 0.35 },
                    cancellationToken);

                await SaveAIHistoryAsync("GenerateReview", request.EvaluationResultId, reviewContext.ContextText, text);

                return Ok(new AITextResponse { Text = text });
            }
            catch (Exception ex)
            {
                return HandleAIException(ex, "viet nhan xet AI");
            }
        }

        [HttpGet]
        public async Task<IActionResult> SmartAlerts()
        {
            var response = await _alertService.GetVisibleSmartAlertsAsync(User);
            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> RefreshSmartAlerts([FromBody] AnalyzePerformanceRequest? request, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _alertService.RefreshSmartAlertsAsync(User, request?.PeriodId, cancellationToken);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return HandleAIException(ex, "AI alerts");
            }
        }

        private List<SuggestedKpi> ParseSuggestedKpis(string text)
        {
            var json = ExtractJsonArray(text);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<SuggestedKpi>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<SuggestedKpi>>(json, _jsonOptions)?
                    .Where(s => !string.IsNullOrWhiteSpace(s.Name))
                    .Take(5)
                    .ToList() ?? new List<SuggestedKpi>();
            }
            catch (JsonException)
            {
                return new List<SuggestedKpi>();
            }
        }

        private static bool IsImprovementSuggestionRequest(string? message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return false;
            }

            var normalized = message.ToLowerInvariant();
            return (normalized.Contains("goi y") || normalized.Contains("gợi ý")) &&
                   (normalized.Contains("cai thien") || normalized.Contains("cải thiện"));
        }

        private static string ExtractJsonArray(string text)
        {
            var trimmed = text.Trim();
            if (trimmed.StartsWith("```"))
            {
                trimmed = trimmed.Replace("```json", string.Empty, StringComparison.OrdinalIgnoreCase)
                    .Replace("```", string.Empty)
                    .Trim();
            }

            var start = trimmed.IndexOf('[');
            var end = trimmed.LastIndexOf(']');
            return start >= 0 && end > start ? trimmed[start..(end + 1)] : trimmed;
        }

        private IActionResult HandleSuggestException(Exception ex)
        {
            var result = HandleAIException(ex, "goi y KPI AI");
            return result;
        }

        private async Task SaveAIHistoryAsync(string feature, int? targetId, string prompt, string response)
        {
            try
            {
                var suIdClaim = User.Claims.FirstOrDefault(c => c.Type == "SystemUserId");
                if (suIdClaim != null && int.TryParse(suIdClaim.Value, out int suId))
                {
                    var hist = new AIGenerationHistory
                    {
                        FeatureName = feature,
                        TargetId = targetId,
                        Prompt = prompt,
                        Response = response,
                        SystemUserId = suId,
                        CreatedAt = DateTime.Now
                    };
                    _context.AIGenerationHistories.Add(hist);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save AI generation history.");
            }
        }

        private IActionResult HandleAIException(Exception ex, string featureName)
        {
            if (ex is GeminiConfigurationException or GeminiRateLimitException)
            {
                return StatusCode(503, new AITextResponse { Success = false, Warnings = { ex.Message } });
            }

            if (ex is UnauthorizedAccessException)
            {
                return StatusCode(403, new AITextResponse { Success = false, Warnings = { ex.Message } });
            }

            _logger.LogError(ex, "Failed to execute {FeatureName}", featureName);
            return StatusCode(500, new AITextResponse { Success = false, Warnings = { $"Khong the thuc hien {featureName}. Vui long thu lai sau." } });
        }

        [HttpGet]
        public async Task<IActionResult> History(string featureName, int? targetId)
        {
            var systemUserIdClaim = User.Claims.FirstOrDefault(c => c.Type == "SystemUserId");
            if (systemUserIdClaim == null || !int.TryParse(systemUserIdClaim.Value, out int systemUserId))
            {
                return Unauthorized();
            }

            var query = _context.AIGenerationHistories.AsQueryable()
                                .Where(h => h.FeatureName == featureName);

            if (targetId.HasValue)
            {
                query = query.Where(h => h.TargetId == targetId.Value);
            }

            if (User.IsInRole("Admin") || User.IsInRole("Administrator") || User.IsInRole("Director") || User.IsInRole("HR"))
            {
                // Full access to see all history
            }
            else if (User.IsInRole("Manager") || User.IsInRole("manager"))
            {
                var currentEmployee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.SystemUserId == systemUserId);

                if (currentEmployee != null)
                {
                    var managedDeptIds = await _context.Departments
                        .Where(d => d.ManagerId == currentEmployee.Id && d.IsActive == true)
                        .Select(d => d.Id).ToListAsync();

                    var employeeIdsInDepts = await _context.EmployeeAssignments
                        .Where(ea => ea.DepartmentId.HasValue && managedDeptIds.Contains(ea.DepartmentId.Value) && ea.IsActive == true)
                        .Select(ea => ea.EmployeeId)
                        .Where(eId => eId.HasValue)
                        .Select(eId => eId!.Value)
                        .Distinct()
                        .ToListAsync();

                    var systemUserIdsInDepts = await _context.Employees
                        .Where(e => employeeIdsInDepts.Contains(e.Id) && e.SystemUserId.HasValue)
                        .Select(e => e.SystemUserId!.Value)
                        .ToListAsync();
                    
                    systemUserIdsInDepts.Add(systemUserId);

                    query = query.Where(h => systemUserIdsInDepts.Contains(h.SystemUserId));
                }
            }
            else
            {
                // Employee, Sales - Only see their own history
                query = query.Where(h => h.SystemUserId == systemUserId);
            }

            var historyRaw = await query.OrderByDescending(h => h.CreatedAt)
                                     .Select(h => new
                                     {
                                        h.Id,
                                        h.FeatureName,
                                        h.TargetId,
                                        h.CreatedAt,
                                        h.Response,
                                        h.SystemUserId,
                                        Username = h.SystemUser != null ? h.SystemUser.Username : "N/A"
                                     })
                                     .ToListAsync();

            var sysUserIds = historyRaw.Select(h => h.SystemUserId).Distinct().ToList();
            var employeeDict = await _context.Employees
                .Where(e => e.SystemUserId.HasValue && sysUserIds.Contains(e.SystemUserId.Value))
                .ToDictionaryAsync(e => e.SystemUserId!.Value, e => e.FullName);

            var history = historyRaw.Select(h => new
            {
                h.Id,
                h.FeatureName,
                h.TargetId,
                h.CreatedAt,
                h.Response,
                CreatorName = employeeDict.ContainsKey(h.SystemUserId) ? employeeDict[h.SystemUserId] : h.Username
            });

            return Ok(new { success = true, history });
        }
    }
}
