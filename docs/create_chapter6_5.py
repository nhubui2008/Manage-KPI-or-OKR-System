"""
Script tạo phần 6.5: Hướng dẫn sử dụng tính năng Trợ lý AI (AI Assistant) cho báo cáo tốt nghiệp
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


def add_bullet(doc, text, bold_prefix="", level=0):
    p = doc.add_paragraph()
    p.alignment = WD_ALIGN_PARAGRAPH.JUSTIFY
    pf = p.paragraph_format
    pf.space_before = Pt(2)
    pf.space_after = Pt(2)
    pf.line_spacing = Pt(22)
    pf.left_indent = Cm(1.27 + level * 0.63)
    pf.first_line_indent = Cm(-0.63)

    if bold_prefix:
        run_b = p.add_run("● " + bold_prefix + ": ")
        set_font(run_b, bold=True)
        run_t = p.add_run(text)
        set_font(run_t)
    else:
        run = p.add_run("● " + text)
        set_font(run)
    return p


# ===================== NỘI DUNG 6.5 =====================

def write_section_6_5(doc):
    """6.5. Hướng dẫn sử dụng tính năng Trợ lý AI (AI Assistant)"""

    add_heading1(doc, "6.5. Hướng dẫn sử dụng các tính năng Trợ lý AI (Bizen AI & Gemini)")

    add_para(doc,
        "Điểm khác biệt vượt trội của hệ thống quản trị Bizen KPI/OKR so với các phần mềm truyền thống "
        "là việc tích hợp sâu Trí tuệ nhân tạo (Google Gemini 2.5 Flash API). Trợ lý AI không hoạt động độc lập "
        "mà đóng vai trò là một tác nhân hỗ trợ xuyên suốt (Co-pilot), tự động liên kết dữ liệu ngữ cảnh "
        "để tư vấn, sinh chỉ tiêu và cảnh báo rủi ro tiến độ theo thời gian thực."
    )

    # 6.5.1. Vai trò của Trợ lý AI trong hệ thống
    add_heading2(doc, "6.5.1. Vai trò của Trợ lý AI trong hệ thống")
    add_para(doc, "Trợ lý AI Gemini hỗ trợ người dùng ở các góc độ vận hành sau:")
    add_bullet(doc, "Cung cấp khung hội thoại tự nhiên trượt mở ở mọi màn hình, giúp nhân sự hỏi đáp giải quyết khó khăn ngay tại chỗ.", bold_prefix="Tư vấn ngữ cảnh thời gian thực")
    add_bullet(doc, "Giúp Trưởng phòng sinh nhanh các chỉ tiêu KPI định lượng phù hợp từ mục tiêu phòng ban, tránh tình trạng thiết lập KPI mơ hồ.", bold_prefix="Tự động hóa thiết lập mục tiêu")
    add_bullet(doc, "Giúp Giám đốc đọc hiểu lượng dữ liệu khổng lồ của doanh nghiệp, phân tích nguyên nhân chậm tiến độ và đưa ra đề xuất cải tiến.", bold_prefix="Hỗ trợ ra quyết định chiến lược")

    # 6.5.2. Hướng dẫn sử dụng các tính năng AI chính
    add_heading2(doc, "6.5.2. Hướng dẫn vận hành các tính năng AI chính")

    # Tính năng 1
    add_heading3(doc, "a) Widget Chat hỗ trợ chuyên môn (Bizen AI Chatbot)")
    add_para(doc,
        "Widget Chat hỗ trợ giải quyết khó khăn nghiệp vụ dựa trên ngữ cảnh thực tế của tài khoản đăng nhập:\n"
        "1. Click biểu tượng Chatbot màu hồng ở góc dưới bên phải màn hình để trượt mở panel chat.\n"
        "2. Nhập câu hỏi nghiệp vụ hoặc click các phím tắt nhanh được thiết kế sẵn (Ví dụ: 'Phân tích KPI của tôi', 'Tìm rủi ro tiến độ').\n"
        "3. AIDataService sẽ đóng gói context dữ liệu thực tế (danh sách KPI đang phụ trách, tiến độ hiện tại, rào cản check-in gần nhất) "
        "gửi kèm câu hỏi sang Gemini API.\n"
        "4. Panel hiển thị câu trả lời định dạng bảng biểu hoặc danh sách Markdown rõ ràng, đề xuất các hành động "
        "cụ thể để nhân viên tháo gỡ khó khăn.",
        italic=False
    )

    # Tính năng 2
    add_heading3(doc, "b) Công cụ sinh chỉ tiêu KPI thông minh (AI KPI Generator)")
    add_para(doc,
        "Giúp Trưởng phòng sinh nhanh KPI chuẩn hóa khi giao chỉ tiêu cho nhân viên:\n"
        "1. Tại biểu mẫu 'Giao KPI mới' (URL: `/KPIs/Create`), chọn OKR phòng ban cần liên kết.\n"
        "2. Click nút 'AI gợi ý KPI'. Hệ thống sẽ tự động gửi thông tin OKR phòng ban và chức danh của nhân viên lên AI.\n"
        "3. AI phản hồi đề xuất 3 KPI mẫu bao gồm: tên KPI, mô tả chi tiết, đơn vị tính và đề xuất giá trị target hợp lý. "
        "Quản lý click nút 'Áp dụng' bên cạnh KPI phù hợp nhất để tự động điền thông tin vào form giao việc.",
        italic=False
    )

    # Tính năng 3
    add_heading3(doc, "c) Phân tích hiệu suất doanh nghiệp bằng AI (AI Performance Analysis)")
    add_para(doc,
        "Giúp ban Giám đốc đánh giá nhanh sức khỏe doanh nghiệp cuối tháng hoặc cuối kỳ:\n"
        "1. Tại trang Dashboard chính của Giám đốc (URL: `/Dashboard`), click nút 'AI Performance Analysis'.\n"
        "2. AIDataService quét toàn bộ CSDL và đóng gói ngữ cảnh: tiến độ trung bình của tất cả OKR công ty, "
        "danh sách 5 phòng ban có tiến độ KPI thấp nhất, các rào cản check-in muộn được ghi nhận trong kỳ.\n"
        "3. Gemini API phân tích và kết xuất báo cáo tổng quan cấu trúc chuẩn gồm: Đánh giá sức khỏe hiện tại, "
        "Chỉ ra các điểm nghẽn (Bottlenecks) nhân sự, và Đề xuất giải pháp định hướng hành động cho Giám đốc.",
        italic=False
    )


# ===================== MAIN =====================

def main():
    print("📄 Đang mở file Word hiện có...")
    doc = Document(INPUT_PATH)

    print("    ✓ 6.5. Vai trò AI Assistant")
    print("      ✓ 6.5.1. Vai trò của AI trong hệ thống")
    print("      ✓ 6.5.2. Thao tác các tính năng AI (Widget, Generator, Analysis)")
    write_section_6_5(doc)

    doc.save(OUTPUT_PATH)
    print(f"\n✅ Đã thêm phần 6.5 vào file Word!")
    print(f"   📄 {OUTPUT_PATH}")


if __name__ == '__main__':
    main()
