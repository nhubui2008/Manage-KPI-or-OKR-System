using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Models;

namespace Manage_KPI_or_OKR_System.Controllers
{
    [Authorize(Roles = "Admin,Administrator")]
    public class CatalogController : Controller
    {
        private readonly MiniERPDbContext _context;

        public CatalogController(MiniERPDbContext context)
        {
            _context = context;
        }

        // =========================================
        // INDEX - Trang chính quản lý danh mục
        // =========================================
        public async Task<IActionResult> Index(string tab = "kpitype")
        {
            ViewBag.ActiveTab = tab;

            ViewBag.KPITypes = await _context.KPITypes.OrderBy(x => x.Id).ToListAsync();
            ViewBag.OKRTypes = await _context.OKRTypes.OrderBy(x => x.Id).ToListAsync();
            ViewBag.KPIProperties = await _context.KPIProperties.OrderBy(x => x.Id).ToListAsync();
            ViewBag.CheckInStatuses = await _context.CheckInStatuses.OrderBy(x => x.Id).ToListAsync();
            ViewBag.FailReasons = await _context.FailReasons.OrderBy(x => x.Id).ToListAsync();
            ViewBag.GradingRanks = await _context.GradingRanks.OrderBy(x => x.MinScore).ToListAsync();
            ViewBag.Statuses = await _context.Statuses.OrderBy(x => x.StatusType).ThenBy(x => x.Id).ToListAsync();

            ViewBag.SystemParameters = await _context.SystemParameters.OrderBy(x => x.Id).ToListAsync();

            return View();
        }

        // =========================================
        // KPI TYPE CRUD
        // =========================================
        [HttpPost]
        public async Task<IActionResult> CreateKPIType(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
                return Json(new { success = false, message = "Tên loại KPI không được để trống." });

            if (await _context.KPITypes.AnyAsync(x => x.TypeName == typeName))
                return Json(new { success = false, message = "Loại KPI này đã tồn tại." });

            var entity = new KPIType { TypeName = typeName };
            _context.KPITypes.Add(entity);
            await _context.SaveChangesAsync();
            return Json(new { success = true, id = entity.Id, message = "Thêm loại KPI thành công." });
        }

        [HttpPost]
        public async Task<IActionResult> EditKPIType(int id, string typeName)
        {
            var entity = await _context.KPITypes.FindAsync(id);
            if (entity == null) return Json(new { success = false, message = "Không tìm thấy dữ liệu." });
            if (string.IsNullOrWhiteSpace(typeName))
                return Json(new { success = false, message = "Tên loại KPI không được để trống." });

            entity.TypeName = typeName;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Cập nhật thành công." });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteKPIType(int id)
        {
            var entity = await _context.KPITypes.FindAsync(id);
            if (entity == null) return Json(new { success = false, message = "Không tìm thấy dữ liệu." });
            _context.KPITypes.Remove(entity);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Xóa thành công." });
        }

        // =========================================
        // OKR TYPE CRUD
        // =========================================
        [HttpPost]
        public async Task<IActionResult> CreateOKRType(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
                return Json(new { success = false, message = "Tên loại OKR không được để trống." });

            if (await _context.OKRTypes.AnyAsync(x => x.TypeName == typeName))
                return Json(new { success = false, message = "Loại OKR này đã tồn tại." });

            var entity = new OKRType { TypeName = typeName };
            _context.OKRTypes.Add(entity);
            await _context.SaveChangesAsync();
            return Json(new { success = true, id = entity.Id, message = "Thêm loại OKR thành công." });
        }

        [HttpPost]
        public async Task<IActionResult> EditOKRType(int id, string typeName)
        {
            var entity = await _context.OKRTypes.FindAsync(id);
            if (entity == null) return Json(new { success = false, message = "Không tìm thấy dữ liệu." });
            if (string.IsNullOrWhiteSpace(typeName))
                return Json(new { success = false, message = "Tên loại OKR không được để trống." });

            entity.TypeName = typeName;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Cập nhật thành công." });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteOKRType(int id)
        {
            var entity = await _context.OKRTypes.FindAsync(id);
            if (entity == null) return Json(new { success = false, message = "Không tìm thấy dữ liệu." });
            _context.OKRTypes.Remove(entity);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Xóa thành công." });
        }

        // =========================================
        // KPI PROPERTY CRUD
        // =========================================
        [HttpPost]
        public async Task<IActionResult> CreateKPIProperty(string propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                return Json(new { success = false, message = "Tên thuộc tính không được để trống." });

            var entity = new KPIProperty { PropertyName = propertyName };
            _context.KPIProperties.Add(entity);
            await _context.SaveChangesAsync();
            return Json(new { success = true, id = entity.Id, message = "Thêm thuộc tính KPI thành công." });
        }

        [HttpPost]
        public async Task<IActionResult> EditKPIProperty(int id, string propertyName)
        {
            var entity = await _context.KPIProperties.FindAsync(id);
            if (entity == null) return Json(new { success = false, message = "Không tìm thấy dữ liệu." });
            entity.PropertyName = propertyName;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Cập nhật thành công." });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteKPIProperty(int id)
        {
            var entity = await _context.KPIProperties.FindAsync(id);
            if (entity == null) return Json(new { success = false, message = "Không tìm thấy dữ liệu." });
            _context.KPIProperties.Remove(entity);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Xóa thành công." });
        }

        // =========================================
        // CHECK-IN STATUS CRUD
        // =========================================
        [HttpPost]
        public async Task<IActionResult> CreateCheckInStatus(string statusName)
        {
            if (string.IsNullOrWhiteSpace(statusName))
                return Json(new { success = false, message = "Tên trạng thái không được để trống." });

            if (await _context.CheckInStatuses.AnyAsync(x => x.StatusName == statusName))
                return Json(new { success = false, message = "Trạng thái này đã tồn tại." });

            var entity = new CheckInStatus { StatusName = statusName };
            _context.CheckInStatuses.Add(entity);
            await _context.SaveChangesAsync();
            return Json(new { success = true, id = entity.Id, message = "Thêm trạng thái Check-in thành công." });
        }

        [HttpPost]
        public async Task<IActionResult> EditCheckInStatus(int id, string statusName)
        {
            var entity = await _context.CheckInStatuses.FindAsync(id);
            if (entity == null) return Json(new { success = false, message = "Không tìm thấy dữ liệu." });
            entity.StatusName = statusName;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Cập nhật thành công." });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCheckInStatus(int id)
        {
            var entity = await _context.CheckInStatuses.FindAsync(id);
            if (entity == null) return Json(new { success = false, message = "Không tìm thấy dữ liệu." });
            _context.CheckInStatuses.Remove(entity);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Xóa thành công." });
        }

        // =========================================
        // FAIL REASON CRUD
        // =========================================
        [HttpPost]
        public async Task<IActionResult> CreateFailReason(string reasonName)
        {
            if (string.IsNullOrWhiteSpace(reasonName))
                return Json(new { success = false, message = "Tên lý do không được để trống." });

            var entity = new FailReason { ReasonName = reasonName };
            _context.FailReasons.Add(entity);
            await _context.SaveChangesAsync();
            return Json(new { success = true, id = entity.Id, message = "Thêm lý do thất bại thành công." });
        }

        [HttpPost]
        public async Task<IActionResult> EditFailReason(int id, string reasonName)
        {
            var entity = await _context.FailReasons.FindAsync(id);
            if (entity == null) return Json(new { success = false, message = "Không tìm thấy dữ liệu." });
            entity.ReasonName = reasonName;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Cập nhật thành công." });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteFailReason(int id)
        {
            var entity = await _context.FailReasons.FindAsync(id);
            if (entity == null) return Json(new { success = false, message = "Không tìm thấy dữ liệu." });
            _context.FailReasons.Remove(entity);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Xóa thành công." });
        }

        // =========================================
        // GRADING RANK CRUD
        // =========================================
        [HttpPost]
        public async Task<IActionResult> CreateGradingRank(string rankCode, decimal? minScore, string description)
        {
            if (string.IsNullOrWhiteSpace(rankCode))
                return Json(new { success = false, message = "Mã hạng không được để trống." });

            if (await _context.GradingRanks.AnyAsync(x => x.RankCode == rankCode))
                return Json(new { success = false, message = "Mã hạng này đã tồn tại." });

            var entity = new GradingRank { RankCode = rankCode, MinScore = minScore, Description = description };
            _context.GradingRanks.Add(entity);
            await _context.SaveChangesAsync();
            return Json(new { success = true, id = entity.Id, message = "Thêm bậc xếp hạng thành công." });
        }

        [HttpPost]
        public async Task<IActionResult> EditGradingRank(int id, string rankCode, decimal? minScore, string description)
        {
            var entity = await _context.GradingRanks.FindAsync(id);
            if (entity == null) return Json(new { success = false, message = "Không tìm thấy dữ liệu." });
            entity.RankCode = rankCode;
            entity.MinScore = minScore;
            entity.Description = description;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Cập nhật thành công." });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteGradingRank(int id)
        {
            var entity = await _context.GradingRanks.FindAsync(id);
            if (entity == null) return Json(new { success = false, message = "Không tìm thấy dữ liệu." });
            _context.GradingRanks.Remove(entity);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Xóa thành công." });
        }

        // =========================================
        // STATUS CRUD
        // =========================================
        [HttpPost]
        public async Task<IActionResult> CreateStatus(string statusType, string statusName)
        {
            if (string.IsNullOrWhiteSpace(statusType) || string.IsNullOrWhiteSpace(statusName))
                return Json(new { success = false, message = "Loại trạng thái và tên trạng thái không được để trống." });

            if (await _context.Statuses.AnyAsync(x => x.StatusType == statusType && x.StatusName == statusName))
                return Json(new { success = false, message = "Trạng thái này đã tồn tại." });

            var entity = new Status { StatusType = statusType, StatusName = statusName };
            _context.Statuses.Add(entity);
            await _context.SaveChangesAsync();
            return Json(new { success = true, id = entity.Id, message = "Thêm trạng thái thành công." });
        }

        [HttpPost]
        public async Task<IActionResult> EditStatus(int id, string statusType, string statusName)
        {
            var entity = await _context.Statuses.FindAsync(id);
            if (entity == null) return Json(new { success = false, message = "Không tìm thấy dữ liệu." });
            entity.StatusType = statusType;
            entity.StatusName = statusName;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Cập nhật thành công." });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteStatus(int id)
        {
            var entity = await _context.Statuses.FindAsync(id);
            if (entity == null) return Json(new { success = false, message = "Không tìm thấy dữ liệu." });
            _context.Statuses.Remove(entity);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Xóa thành công." });
        }



        // =========================================
        // SYSTEM PARAMETER CRUD
        // =========================================
        [HttpPost]
        public async Task<IActionResult> CreateSystemParameter(string parameterCode, string value, string description)
        {
            if (string.IsNullOrWhiteSpace(parameterCode))
                return Json(new { success = false, message = "Mã tham số không được để trống." });

            if (await _context.SystemParameters.AnyAsync(x => x.ParameterCode == parameterCode))
                return Json(new { success = false, message = "Mã tham số này đã tồn tại." });

            var entity = new SystemParameter { ParameterCode = parameterCode, Value = value, Description = description };
            _context.SystemParameters.Add(entity);
            await _context.SaveChangesAsync();
            return Json(new { success = true, id = entity.Id, message = "Thêm tham số hệ thống thành công." });
        }

        [HttpPost]
        public async Task<IActionResult> EditSystemParameter(int id, string parameterCode, string value, string description)
        {
            var entity = await _context.SystemParameters.FindAsync(id);
            if (entity == null) return Json(new { success = false, message = "Không tìm thấy dữ liệu." });
            entity.ParameterCode = parameterCode;
            entity.Value = value;
            entity.Description = description;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Cập nhật thành công." });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSystemParameter(int id)
        {
            var entity = await _context.SystemParameters.FindAsync(id);
            if (entity == null) return Json(new { success = false, message = "Không tìm thấy dữ liệu." });
            _context.SystemParameters.Remove(entity);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Xóa thành công." });
        }
    }
}
