"""
Script tạo phần 2.3: Use case & Activity Diagrams cho báo cáo tốt nghiệp
"""

from docx import Document
from docx.shared import Pt, Cm, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH, WD_BREAK
from docx.enum.table import WD_TABLE_ALIGNMENT
from docx.oxml.ns import qn
from docx.oxml import OxmlElement
import os

# ===================== CẤU HÌNH =====================
SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
INPUT_PATH = os.path.join(SCRIPT_DIR, "BaoCao_DuAn_TotNghiep.docx")
OUTPUT_PATH = os.path.join(SCRIPT_DIR, "BaoCao_DuAn_TotNghiep.docx")

FONT_NAME = 'Times New Roman'
FONT_SIZE = 14
HEADER_BG = "1F4E79"


def set_font(run, size=FONT_SIZE, bold=False, italic=False, color=None):
    run.font.size = Pt(size)
    run.font.name = FONT_NAME
    run._element.rPr.rFonts.set(qn('w:eastAsia'), FONT_NAME)
    run.bold = bold
    run.italic = italic
    if color:
        run.font.color.rgb = color


def add_heading1(doc, text):
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.LEFT
    pf = p.paragraph_format
    pf.space_before = Pt(18)
    pf.space_after = Pt(8)
    run = p.add_run(text)
    set_font(run, size=FONT_SIZE, bold=True)
    return p


def add_heading2(doc, text):
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.LEFT
    pf = p.paragraph_format
    pf.space_before = Pt(12)
    pf.space_after = Pt(6)
    run = p.add_run(text)
    set_font(run, size=FONT_SIZE, bold=True)
    return p


def add_heading3(doc, text):
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.LEFT
    pf = p.paragraph_format
    pf.space_before = Pt(10)
    pf.space_after = Pt(4)
    run = p.add_run(text)
    set_font(run, size=FONT_SIZE, bold=True)
    return p


def add_para(doc, text, bold=False, italic=False, indent=True, space_before=3, space_after=3):
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.JUSTIFY
    pf = p.paragraph_format
    pf.space_before = Pt(space_before)
    pf.space_after = Pt(space_after)
    pf.line_spacing = Pt(22)
    if indent:
        pf.first_line_indent = Cm(1.27)
    run = p.add_run(text)
    set_font(run, bold=bold, italic=italic)
    return p


def set_cell_shading(cell, color):
    shading = OxmlElement('w:shd')
    shading.set(qn('w:fill'), color)
    shading.set(qn('w:val'), 'clear')
    cell._tc.get_or_add_tcPr().append(shading)


def set_cell(cell, text, bold=False, size=FONT_SIZE, align=WD_ALIGN_PARAGRAPH.CENTER, color=None):
    cell.text = ""
    p = cell.paragraphs[0]
    p.alignment = align
    pf = p.paragraph_format
    pf.space_before = Pt(2)
    pf.space_after = Pt(2)
    run = p.add_run(text)
    set_font(run, size=size, bold=bold, color=color)


def create_spec_table(doc, data_dict):
    """Tạo bảng đặc tả Use Case chi tiết"""
    table = doc.add_table(rows=len(data_dict), cols=2)
    table.alignment = WD_TABLE_ALIGNMENT.CENTER
    table.style = 'Table Grid'

    for i, (key, val) in enumerate(data_dict.items()):
        # Cột 1 (Tên thuộc tính)
        cell_key = table.rows[i].cells[0]
        set_cell(cell_key, key, bold=True, size=11, align=WD_ALIGN_PARAGRAPH.LEFT, color=RGBColor(255, 255, 255))
        set_cell_shading(cell_key, HEADER_BG)
        cell_key.width = Cm(3.5)

        # Cột 2 (Giá trị thuộc tính)
        cell_val = table.rows[i].cells[1]
        set_cell(cell_val, val, bold=False, size=11, align=WD_ALIGN_PARAGRAPH.LEFT)
        cell_val.width = Cm(12.5)

    return table


def add_table_caption(doc, caption):
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    pf = p.paragraph_format
    pf.space_before = Pt(4)
    pf.space_after = Pt(12)
    run = p.add_run(caption)
    set_font(run, size=12, italic=True)


# ===================== NỘI DUNG 2.3 =====================

def write_section_2_3(doc):
    """2.3. Use case"""

    add_heading1(doc, "2.3. Use case")

    add_para(doc,
        "Để làm rõ cách thức tương tác giữa các tác nhân và hệ thống, mục này trình bày các sơ đồ "
        "Use Case tổng quan, danh sách Use Case chi tiết, đặc tả các Use Case cốt lõi và sơ đồ hoạt động "
        "(Activity Diagrams) mô tả luồng quy trình vận hành chính của hệ thống."
    )

    # ============================================================
    # 2.3.1. SƠ ĐỒ USE CASE VÀ DANH SÁCH USE CASE
    # ============================================================
    add_heading2(doc, "2.3.1. Sơ đồ Use Case tổng quan")
    
    add_para(doc,
        "Sơ đồ Use Case tổng quan của hệ thống mô tả mối quan hệ giữa 5 tác nhân (Admin, Director, HR, "
        "Manager, Employee) và các nhóm chức năng cốt lõi được trình bày chi tiết tại Hình 1 (Mục 2.1). "
        "Đồng thời, danh sách 28 Use Case chi tiết cùng mô tả tóm tắt được trình bày tại Bảng 9 (Mục 2.1) "
        "là cơ sở phân rã để thực hiện thiết kế chi tiết dưới đây."
    )

    # ============================================================
    # 2.3.2. ĐẶC TẢ CÁC USE CASE CỐT LÕI
    # ============================================================
    add_heading2(doc, "2.3.2. Đặc tả các Use Case cốt lõi")

    add_para(doc,
        "Hệ thống bao gồm nhiều chức năng, dưới đây là đặc tả chi tiết của 4 Use Case cốt lõi "
        "đóng vai trò xương sống cho hoạt động quản lý hiệu suất và tích hợp AI của dự án:"
    )

    # --- UC 1: Giao KPI cho nhân viên ---
    add_heading3(doc, "a) Đặc tả Use Case UC_MA_02 – Giao KPI nhân viên")
    
    spec_uc_ma_02 = {
        "Mã Use Case": "UC_MA_02",
        "Tên Use Case": "Giao KPI cho nhân viên",
        "Độ ưu tiên": "Cao (Trọng yếu)",
        "Tác nhân chính": "Manager (Trưởng phòng ban)",
        "Mô tả": "Cho phép Trưởng phòng ban tạo chỉ tiêu KPI cho nhân viên trực thuộc trong kỳ đánh giá. KPI bao gồm target cụ thể, trọng số đóng góp, kỳ hạn check-in và liên kết trực tiếp với mục tiêu OKR/Key Result tương ứng.",
        "Điều kiện tiên quyết": "Kỳ đánh giá đang hoạt động (Mở); Trưởng phòng và Nhân viên đã được HR gán vào cùng phòng ban; OKR phòng ban đã được thiết lập.",
        "Luồng sự kiện chính (Basic Flow)": 
            "1. Trưởng phòng truy cập vào module 'Quản lý KPI', chọn 'Tạo mới KPI'.\n"
            "2. Hệ thống hiển thị biểu mẫu yêu cầu nhập: Tên KPI, loại KPI (Định lượng/Định tính/Hành vi), thuộc tính (Tăng trưởng/Giảm thiểu).\n"
            "3. Trưởng phòng nhập các tham số: Giá trị Target, Pass/Fail Threshold, đơn vị đo, tần suất check-in và deadline.\n"
            "4. Trưởng phòng gán KPI cho nhân viên cụ thể, thiết lập Trọng số (Weight) và chọn liên kết với một Key Result cụ thể của OKR phòng ban.\n"
            "5. Trưởng phòng bấm 'Gửi duyệt'. Hệ thống kiểm tra tổng trọng số KPI của nhân viên (không vượt quá 100%), lưu trạng thái là 'Chờ duyệt' và gửi email thông báo cho Director.",
        "Luồng phụ / Ngoại lệ (Alternative Flow)":
            "Luồng 4a (Chọn gợi ý từ AI):\n"
            "   - Trưởng phòng click vào nút 'Gợi ý từ AI'.\n"
            "   - Hệ thống gọi AIDataService.Suggestions gửi ngữ cảnh (chức danh nhân viên, OKR phòng ban) sang AI Gemini.\n"
            "   - Gemini đề xuất 3 KPI mẫu kèm target phù hợp. Trưởng phòng click chọn một KPI mẫu để auto-fill vào biểu mẫu.\n"
            "Luồng 5a (Vượt quá tổng trọng số):\n"
            "   - Hệ thống hiển thị thông báo lỗi 'Tổng trọng số KPI của nhân viên trong kỳ vượt quá 100%'. Yêu cầu Trưởng phòng điều chỉnh lại.",
        "Điều kiện sau (Post-condition)": "KPI được lưu vào bảng KPIs và KPIDetails với trạng thái 'Chờ duyệt' (Pending Decision)."
    }
    create_spec_table(doc, spec_uc_ma_02)
    add_table_caption(doc, "Bảng 11: Đặc tả Use Case UC_MA_02 – Giao KPI nhân viên")

    # --- UC 2: Check-in tiến độ KPI ---
    add_heading3(doc, "b) Đặc tả Use Case UC_EM_02 – Check-in tiến độ KPI")
    
    spec_uc_em_02 = {
        "Mã Use Case": "UC_EM_02",
        "Tên Use Case": "Check-in tiến độ KPI",
        "Độ ưu tiên": "Cao (Trọng yếu)",
        "Tác nhân chính": "Employee (Nhân viên)",
        "Mô tả": "Cho phép nhân viên báo cáo kết quả thực hiện KPI định kỳ theo lịch check-in đã giao, gửi kèm ghi chú giải trình tiến độ để gửi lên cấp quản lý phê duyệt.",
        "Điều kiện tiên quyết": "KPI của nhân viên ở trạng thái 'Đang thực hiện' (Active); Đến lịch check-in định kỳ hoặc nhân viên chủ động cập nhật.",
        "Luồng sự kiện chính (Basic Flow)": 
            "1. Nhân viên truy cập trang cá nhân, chọn KPI cần check-in và click 'Báo cáo tiến độ'.\n"
            "2. Hệ thống hiển thị form check-in gồm: Giá trị đạt được thực tế (Achieved Value) và Ghi chú/Ý kiến giải trình.\n"
            "3. Nhân viên nhập giá trị thực tế mới nhất và viết nội dung giải trình tiến độ thực hiện.\n"
            "4. Nhân viên bấm 'Gửi báo cáo'.\n"
            "5. Hệ thống ghi nhận giá trị, tự động tính toán % hoàn thành thực tế và so sánh với tiến độ kỳ vọng tại thời điểm hiện tại (schedule progress).\n"
            "6. Hệ thống tạo bản ghi trong bảng KPICheckIns với trạng thái 'Chờ duyệt' (Pending Review), đẩy bản ghi vào Review Queue của Trưởng phòng và gửi thông báo nhắc nhở.",
        "Luồng phụ / Ngoại lệ (Alternative Flow)":
            "Luồng 3a (Nhân viên đính kèm link tài liệu minh chứng):\n"
            "   - Nhân viên nhập link tài liệu chứng minh kết quả check-in trong phần ghi chú.\n"
            "Luồng 5a (Check-in muộn quá hạn):\n"
            "   - Hệ thống tự động ghi nhận thuộc tính 'Quá hạn check-in' và gửi cảnh báo rủi ro (Smart Alerts) lên cho Trưởng phòng.",
        "Điều kiện sau (Post-condition)": "Tiến độ check-in được lưu tạm thời, chờ Trưởng phòng duyệt duyệt trong Review Queue."
    }
    create_spec_table(doc, spec_uc_em_02)
    add_table_caption(doc, "Bảng 12: Đặc tả Use Case UC_EM_02 – Check-in tiến độ KPI")

    # --- UC 3: Phê duyệt Check-in ---
    add_heading3(doc, "c) Đặc tả Use Case UC_MA_03 – Phê duyệt Check-in")
    
    spec_uc_ma_03 = {
        "Mã Use Case": "UC_MA_03",
        "Tên Use Case": "Phê duyệt Check-in",
        "Độ ưu tiên": "Cao",
        "Tác nhân chính": "Manager (Trưởng phòng ban)",
        "Mô tả": "Cho phép Trưởng phòng phê duyệt hoặc từ chối kết quả check-in tiến độ của nhân viên trực thuộc từ hàng đợi Review Queue, ghi nhận nhận xét và chấm điểm.",
        "Điều kiện tiên quyết": "Nhân viên đã thực hiện check-in và gửi báo cáo tiến độ (ở trạng thái Pending Review).",
        "Luồng sự kiện chính (Basic Flow)": 
            "1. Trưởng phòng truy cập vào 'Review Queue' của phòng ban quản lý.\n"
            "2. Hệ thống hiển thị danh sách các bản ghi check-in đang chờ duyệt của nhân viên.\n"
            "3. Trưởng phòng click xem chi tiết một bản ghi, xem xét giá trị báo cáo, so sánh biểu đồ tiến độ thực tế vs tiến độ kỳ vọng và đọc giải trình.\n"
            "4. Trưởng phòng viết nhận xét đánh giá và chọn nút 'Duyệt' (Approve).\n"
            "5. Hệ thống gọi OKRProgressService để tự động cập nhật tiến độ phần trăm (%) của KPI và đồng bộ hóa tiến độ lên các Key Results và OKR phòng ban/công ty liên quan.\n"
            "6. Trạng thái bản ghi check-in chuyển thành 'Đã duyệt' (Approved) và hệ thống gửi thông báo kết quả cho nhân viên.",
        "Luồng phụ / Ngoại lệ (Alternative Flow)":
            "Luồng 4a (Trưởng phòng chọn 'Từ chối' - Reject):\n"
            "   - Trưởng phòng bắt buộc phải nhập lý do từ chối (Fail Reason) và viết nhận xét yêu cầu nhân viên làm rõ.\n"
            "   - Trưởng phòng click 'Từ chối'. Hệ thống chuyển trạng thái check-in thành 'Từ chối' (Rejected) và gửi thông báo yêu cầu nhân viên cập nhật lại.",
        "Điều kiện sau (Post-condition)": "Tiến độ KPI được cập nhật chính thức vào cơ sở dữ liệu nếu được duyệt."
    }
    create_spec_table(doc, spec_uc_ma_03)
    add_table_caption(doc, "Bảng 13: Đặc tả Use Case UC_MA_03 – Phê duyệt Check-in")

    # --- UC 4: Trợ lý AI Gemini ---
    add_heading3(doc, "d) Đặc tả Use Case UC_EM_06 – Trợ lý AI Gemini")
    
    spec_uc_em_06 = {
        "Mã Use Case": "UC_EM_06",
        "Tên Use Case": "Tương tác với Trợ lý AI Gemini",
        "Độ ưu tiên": "Trung bình (Tính năng giá trị gia tăng)",
        "Tác nhân chính": "Employee, Manager, Director (Người dùng hệ thống)",
        "Mô tả": "Cung cấp chatbot AI Gemini (Gemini API) nhận biết ngữ cảnh thực tế của người dùng (Context-aware), hỗ trợ tư vấn thiết lập mục tiêu, phân tích hiệu suất và đề xuất các giải pháp cải thiện công việc.",
        "Điều kiện tiên quyết": "Tài khoản doanh nghiệp đang sử dụng gói dịch vụ có tính năng AI Insight; Hệ thống đã cấu hình API Key cho Gemini Service.",
        "Luồng sự kiện chính (Basic Flow)": 
            "1. Người dùng click mở 'Chatbot AI' từ thanh công cụ hoặc trang cá nhân.\n"
            "2. Người dùng nhập câu hỏi hoặc yêu cầu (Ví dụ: 'Phòng Công nghệ của tôi đang chậm KPI X, làm sao để khắc phục?').\n"
            "3. Hệ thống gọi AIDataService để tự động trích xuất dữ liệu ngữ cảnh (Role, phòng ban, danh sách KPI đang chậm tiến độ, lịch sử check-in liên quan).\n"
            "4. Hệ thống đóng gói dữ liệu ngữ cảnh kết hợp với prompt của người dùng gửi sang Gemini API (gemini-2.5-flash).\n"
            "5. Gemini xử lý và phản hồi câu trả lời có cấu trúc và có tính cá nhân hóa cao cho người dùng.\n"
            "6. Hệ thống lưu lịch sử hội thoại vào bảng AIGenerationHistories và hiển thị câu trả lời trực quan trên widget chat.",
        "Luồng phụ / Ngoại lệ (Alternative Flow)":
            "Luồng 4a (Vượt quá Rate Limit của AI):\n"
            "   - Hệ thống phát hiện số lượng request trong phút vượt quá giới hạn (15 req/phút).\n"
            "   - Hệ thống hiển thị thông báo: 'Trợ lý AI đang quá tải, vui lòng thử lại sau 1 phút' và ngắt kết nối tạm thời.\n"
            "Luồng 4b (Lỗi kết nối API):\n"
            "   - Không kết nối được với server Google Gemini. Hệ thống chuyển sang sử dụng bộ quy tắc rule-based dự phòng để đưa ra các tư vấn cơ bản.",
        "Điều kiện sau (Post-condition)": "Lịch sử cuộc hội thoại được lưu lại và tự động xóa sau 30 ngày qua dịch vụ background AIHistoryCleanupService."
    }
    create_spec_table(doc, spec_uc_em_06)
    add_table_caption(doc, "Bảng 14: Đặc tả Use Case UC_EM_06 – Trợ lý AI Gemini")

    # ============================================================
    # 2.3.3. SƠ ĐỒ HOẠT ĐỘNG (ACTIVITY DIAGRAMS)
    # ============================================================
    add_heading2(doc, "2.3.3. Sơ đồ hoạt động cho các luồng quy trình chính")

    add_para(doc,
        "Sơ đồ hoạt động (Activity Diagram) thể hiện trình tự luồng xử lý nghiệp vụ đi qua các làn tác nhân "
        "(Swimlanes) khác nhau. Dưới đây là 3 sơ đồ hoạt động cho các quy trình cốt lõi của hệ thống:"
    )

    # --- Sơ đồ 1 ---
    add_heading3(doc, "a) Quy trình thiết lập & giao KPI/OKR đa cấp")
    add_para(doc,
        "Sơ đồ dưới đây mô tả chuỗi hoạt động từ lúc Ban Giám đốc thiết lập chiến lược công ty, phân bổ OKR "
        "xuống phòng ban, Trưởng phòng tạo và giao KPI cụ thể cho nhân viên, cho đến khi KPI được duyệt và "
        "kích hoạt chính thức:"
    )
    
    script_dir = os.path.dirname(os.path.abspath(__file__))
    
    p_act1 = doc.add_paragraph()
    p_act1.alignment = WD_ALIGN_PARAGRAPH.CENTER
    img_path1 = os.path.join(script_dir, "activity_kpi_okr.png")
    if os.path.exists(img_path1):
        p_act1.add_run().add_picture(img_path1, width=Cm(14.5))
    else:
        p_act1.add_run("[SƠ ĐỒ HOẠT ĐỘNG GIAO KPI/OKR - LỖI HÌNH ẢNH]")
    add_table_caption(doc, "Hình 2: Sơ đồ hoạt động quy trình thiết lập & giao KPI/OKR đa cấp")

    # --- Sơ đồ 2 ---
    add_heading3(doc, "b) Quy trình check-in và phê duyệt tiến độ")
    add_para(doc,
        "Sơ đồ dưới đây mô tả luồng báo cáo tiến độ (check-in) của nhân viên, hệ thống tự động tính toán %, "
        "đẩy vào Review Queue của Trưởng phòng để duyệt hoặc từ chối, và tự động đồng bộ hóa ngược lên OKR liên quan:"
    )
    
    p_act2 = doc.add_paragraph()
    p_act2.alignment = WD_ALIGN_PARAGRAPH.CENTER
    img_path2 = os.path.join(script_dir, "activity_checkin.png")
    if os.path.exists(img_path2):
        p_act2.add_run().add_picture(img_path2, width=Cm(14.5))
    else:
        p_act2.add_run("[SƠ ĐỒ HOẠT ĐỘNG CHECK-IN - LỖI HÌNH ẢNH]")
    add_table_caption(doc, "Hình 3: Sơ đồ hoạt động quy trình check-in và phê duyệt tiến độ")

    # --- Sơ đồ 3 ---
    add_heading3(doc, "c) Quy trình trợ lý AI Gemini tư vấn & hỗ trợ quyết định")
    add_para(doc,
        "Sơ đồ dưới đây mô tả luồng gọi dịch vụ AI Gemini, từ khi người dùng đặt câu hỏi, hệ thống thu thập "
        "ngữ cảnh thông qua AIDataService để tối ưu hóa Prompt, gửi gọi Gemini API và trả kết quả hiển thị cho người dùng:"
    )
    
    p_act3 = doc.add_paragraph()
    p_act3.alignment = WD_ALIGN_PARAGRAPH.CENTER
    img_path3 = os.path.join(script_dir, "activity_ai.png")
    if os.path.exists(img_path3):
        p_act3.add_run().add_picture(img_path3, width=Cm(14.5))
    else:
        p_act3.add_run("[SƠ ĐỒ HOẠT ĐỘNG AI GEMINI - LỖI HÌNH ẢNH]")
    add_table_caption(doc, "Hình 4: Sơ đồ hoạt động quy trình trợ lý AI Gemini")

    # Kết luận mục
    add_para(doc, "", space_before=6, space_after=0, indent=False)
    add_para(doc,
        "Tóm lại, việc phân tích chi tiết sơ đồ Use Case và các sơ đồ hoạt động giúp nhóm nắm rõ luồng xử lý "
        "nghiệp vụ và cơ chế tương tác đa bên trong hệ thống. Đây là cơ sở cốt lõi để nhóm tiến hành "
        "thiết kế cấu trúc dữ liệu quan hệ thực thể (ERD) ở phần tiếp theo.",
        italic=True, space_before=6, space_after=12
    )


# ===================== MAIN =====================

def main():
    print("📄 Đang mở file Word hiện có...")
    doc = Document(INPUT_PATH)

    print("    ✓ 2.3. Use case")
    print("      ✓ 2.3.1. Sơ đồ và danh sách Use Case")
    print("      ✓ 2.3.2. Đặc tả 4 Use Case cốt lõi (Bảng 11 - 14)")
    print("      ✓ 2.3.3. Sơ đồ hoạt động (Hình 2 - 4)")
    write_section_2_3(doc)

    doc.save(OUTPUT_PATH)
    print(f"\n✅ Đã thêm phần 2.3 vào file Word!")
    print(f"   📄 {OUTPUT_PATH}")


if __name__ == '__main__':
    main()
