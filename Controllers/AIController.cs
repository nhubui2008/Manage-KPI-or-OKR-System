using System.Text;
using System.Text.Json;
using Manage_KPI_or_OKR_System.Models.AI;
using Manage_KPI_or_OKR_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize]
    public class AIController : Controller
    {
        private readonly IAIDataService _dataService;
        private readonly IAIAlertService _alertService;
        private readonly IGeminiService _geminiService;
        private readonly ILogger<AIController> _logger;
        private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

        public AIController(
            IAIDataService dataService,
            IAIAlertService alertService,
            IGeminiService geminiService,
            ILogger<AIController> logger)
        {
            _dataService = dataService;
            _alertService = alertService;
            _geminiService = geminiService;
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

                var text = await _geminiService.GenerateTextAsync(
                    "Ban la VietMach AI Assistant cho he thong KPI/OKR. Tra loi bang tieng Viet, ngan gon, thuc te, chi dua vao context duoc cap. Neu thieu du lieu, noi ro thieu du lieu.",
                    prompt.ToString(),
                    new GeminiGenerationOptions { Temperature = 0.35, MaxOutputTokens = 1000 },
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
                    new GeminiGenerationOptions { Temperature = 0.35, MaxOutputTokens = 1400, ResponseMimeType = "application/json" },
                    cancellationToken);

                var suggestions = ParseSuggestedKpis(text);
                if (!suggestions.Any())
                {
                    return StatusCode(502, new SuggestKpiResponse { Success = false, Warnings = { "Gemini chua tra ve danh sach KPI hop le." } });
                }

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
                    new GeminiGenerationOptions { Temperature = 0.3, MaxOutputTokens = 1200 },
                    cancellationToken);

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
                    new GeminiGenerationOptions { Temperature = 0.35, MaxOutputTokens = 900 },
                    cancellationToken);

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
    }
}
