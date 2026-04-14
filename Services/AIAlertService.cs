using System.Security.Claims;
using System.Text.Json;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;
using Manage_KPI_or_OKR_System.Models.AI;
using Microsoft.EntityFrameworkCore;

namespace Manage_KPI_or_OKR_System.Services
{
    public interface IAIAlertService
    {
        Task<SmartAlertsResponse> GetVisibleSmartAlertsAsync(ClaimsPrincipal user);
        Task<SmartAlertsResponse> RefreshSmartAlertsAsync(ClaimsPrincipal user, int? periodId, CancellationToken cancellationToken = default);
    }

    public class AIAlertService : IAIAlertService
    {
        private readonly MiniERPDbContext _context;
        private readonly IAIDataService _dataService;
        private readonly IGeminiService _geminiService;
        private readonly ILogger<AIAlertService> _logger;

        public AIAlertService(
            MiniERPDbContext context,
            IAIDataService dataService,
            IGeminiService geminiService,
            ILogger<AIAlertService> logger)
        {
            _context = context;
            _dataService = dataService;
            _geminiService = geminiService;
            _logger = logger;
        }

        public async Task<SmartAlertsResponse> GetVisibleSmartAlertsAsync(ClaimsPrincipal user)
        {
            var alerts = await _dataService.GetVisibleSmartAlertsAsync(user);
            return new SmartAlertsResponse { Alerts = alerts.ToList() };
        }

        public async Task<SmartAlertsResponse> RefreshSmartAlertsAsync(ClaimsPrincipal user, int? periodId, CancellationToken cancellationToken = default)
        {
            var warnings = new List<string>();
            var candidates = (await _dataService.GetRiskCandidatesAsync(user, periodId)).ToList();
            var alerts = candidates.Select(ToFallbackDto).ToList();

            if (candidates.Any())
            {
                try
                {
                    var prompt = BuildAlertPrompt(candidates);
                    var text = await _geminiService.GenerateTextAsync(
                        "Ban la AI phan tich KPI/OKR. Viet canh bao ngan gon bang tieng Viet, thuc te, khong bia them so lieu. Chi tra ve JSON array hop le.",
                        prompt,
                        new GeminiGenerationOptions { Temperature = 0.2, MaxOutputTokens = 1200, ResponseMimeType = "application/json" },
                        cancellationToken);
                    alerts = ParseAlertDtos(text, candidates);
                }
                catch (GeminiConfigurationException ex)
                {
                    warnings.Add(ex.Message);
                }
                catch (GeminiRateLimitException ex)
                {
                    warnings.Add(ex.Message);
                }
                catch (Exception ex)
                {
                    warnings.Add("Khong the goi Gemini cho AI alerts, he thong dang dung noi dung rule-based.");
                    _logger.LogWarning(ex, "Failed to generate AI alert wording.");
                }
            }

            var currentEmployee = await _dataService.GetCurrentEmployeeAsync(user);
            if (currentEmployee != null)
            {
                await UpsertAlertsAsync(currentEmployee.Id, alerts, cancellationToken);
                alerts = (await _dataService.GetVisibleSmartAlertsAsync(user)).ToList();
            }
            else if (alerts.Any())
            {
                warnings.Add("Tai khoan hien tai chua lien ket Employee nen AI alerts chi hien thi tam thoi, chua luu vao SystemAlerts.");
            }

            return new SmartAlertsResponse
            {
                Alerts = alerts,
                Warnings = warnings
            };
        }

        private async Task UpsertAlertsAsync(int receiverId, List<SmartAlertDto> alerts, CancellationToken cancellationToken)
        {
            var now = DateTime.Now;
            foreach (var alert in alerts)
            {
                var existing = await _context.SystemAlerts.FirstOrDefaultAsync(a =>
                    a.ReceiverId == receiverId &&
                    a.AlertType == "AI Insight" &&
                    a.SourceType == alert.SourceType &&
                    a.SourceRefId == alert.SourceRefId &&
                    a.PeriodId == alert.PeriodId,
                    cancellationToken);

                var content = Trim($"{alert.Title}: {alert.Content}", 255);
                if (existing == null)
                {
                    _context.SystemAlerts.Add(new SystemAlert
                    {
                        AlertType = "AI Insight",
                        Content = content,
                        ReceiverId = receiverId,
                        Severity = alert.Severity,
                        SourceType = alert.SourceType,
                        SourceRefId = alert.SourceRefId,
                        PeriodId = alert.PeriodId,
                        CreateDate = now,
                        ExpiresAt = now.AddDays(14),
                        IsRead = false
                    });
                }
                else
                {
                    existing.Content = content;
                    existing.Severity = alert.Severity;
                    existing.CreateDate = now;
                    existing.ExpiresAt = now.AddDays(14);
                    existing.IsRead = false;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        private static string BuildAlertPrompt(List<AIRiskCandidate> candidates)
        {
            var input = candidates.Select(c => new
            {
                sourceType = c.SourceType,
                sourceRefId = c.SourceRefId,
                severity = c.Severity,
                title = c.Title,
                content = c.Content,
                evidence = c.Evidence
            });

            return "Hay bien cac risk candidates sau thanh JSON array. Moi item co dung cac field: severity, title, content, sourceType, sourceRefId. " +
                   "Content toi da 180 ky tu, neu co so lieu thi giu nguyen so lieu trong evidence.\n" +
                   JsonSerializer.Serialize(input, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }

        private static List<SmartAlertDto> ParseAlertDtos(string text, List<AIRiskCandidate> fallback)
        {
            var json = ExtractJsonArray(text);
            if (string.IsNullOrWhiteSpace(json))
            {
                return fallback.Select(ToFallbackDto).ToList();
            }

            try
            {
                var parsed = JsonSerializer.Deserialize<List<SmartAlertDto>>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                if (parsed == null || !parsed.Any())
                {
                    return fallback.Select(ToFallbackDto).ToList();
                }

                foreach (var item in parsed)
                {
                    var match = fallback.FirstOrDefault(c => c.SourceType == item.SourceType && c.SourceRefId == item.SourceRefId);
                    item.Severity = string.IsNullOrWhiteSpace(item.Severity) ? match?.Severity ?? "medium" : item.Severity;
                    item.Title = string.IsNullOrWhiteSpace(item.Title) ? match?.Title ?? "AI Insight" : item.Title;
                    item.Content = string.IsNullOrWhiteSpace(item.Content) ? match?.Content ?? string.Empty : item.Content;
                    item.SourceType = string.IsNullOrWhiteSpace(item.SourceType) ? match?.SourceType : item.SourceType;
                    item.SourceRefId ??= match?.SourceRefId;
                    item.PeriodId ??= match?.PeriodId;
                    item.CreatedAt = DateTime.Now;
                }

                return parsed;
            }
            catch
            {
                return fallback.Select(ToFallbackDto).ToList();
            }
        }

        private static SmartAlertDto ToFallbackDto(AIRiskCandidate candidate)
        {
            return new SmartAlertDto
            {
                Severity = candidate.Severity,
                Title = candidate.Title,
                Content = candidate.Content,
                SourceType = candidate.SourceType,
                SourceRefId = candidate.SourceRefId,
                PeriodId = candidate.PeriodId,
                CreatedAt = DateTime.Now
            };
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

        private static string Trim(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value.Length <= maxLength ? value : value[..maxLength];
        }
    }
}
