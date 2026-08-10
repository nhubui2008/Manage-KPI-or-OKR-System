"""
Script vẽ các sơ đồ hoạt động (Activity Diagrams) bằng Pillow (PIL)
Thiết kế trực quan, phân làn Swimlanes rõ ràng, chữ tiếng Việt chuẩn xác
"""

from PIL import Image, ImageDraw, ImageFont
import os

def draw_activity_kpi_okr(font_path):
    # Kích thước ảnh
    w, h = 1000, 800
    img = Image.new('RGB', (w, h), '#F8FAFC')
    draw = ImageDraw.Draw(img)

    try:
        font_title = ImageFont.truetype(font_path, 22)
        font_lane = ImageFont.truetype(font_path, 18)
        font_text = ImageFont.truetype(font_path, 13)
    except:
        font_title = ImageFont.load_default()
        font_lane = ImageFont.load_default()
        font_text = ImageFont.load_default()

    # Tiêu đề sơ đồ
    draw.text((w // 2, 35), "QUY TRÌNH THIẾT LẬP & GIAO KPI/OKR ĐA CẤP", fill='#1E293B', font=font_title, anchor="mm")

    # 1. Vẽ các Làn phân vai trò (Swimlanes)
    # Làn 1: Giám đốc (Director) [80 -> 360]
    # Làn 2: Trưởng phòng (Manager) [360 -> 680]
    # Làn 3: Hệ thống (System) [680 -> 960]
    ly = 80
    lh = 700
    
    # Kẻ các đường phân làn dọc
    draw.line([360, ly, 360, ly + lh], fill='#94A3B8', width=2)
    draw.line([680, ly, 680, ly + lh], fill='#94A3B8', width=2)
    draw.rectangle([80, ly, 960, ly + lh], outline='#64748B', width=2)

    # Tiêu đề các làn
    draw.text((220, ly + 25), "BAN GIÁM ĐỐC (DIRECTOR)", fill='#0F766E', font=font_lane, anchor="mm")
    draw.text((520, ly + 25), "TRƯỞNG PHÒNG (MANAGER)", fill='#1D4ED8', font=font_lane, anchor="mm")
    draw.text((820, ly + 25), "HỆ THỐNG (SYSTEM)", fill='#374151', font=font_lane, anchor="mm")
    
    draw.line([80, ly + 50, 960, ly + 50], fill='#64748B', width=2)

    # 2. Vẽ các bước hoạt động (Nodes) và kết nối (Arrows)
    # Start node
    draw.ellipse([205, 150, 235, 180], fill='#64748B') # Start
    draw.line([220, 180, 220, 210], fill='#475569', width=2)
    draw.polygon([(216, 204), (220, 210), (224, 204)], fill='#475569')

    # Step 1: Thiết lập OKR Công ty
    draw_box(draw, "Thiết lập Mục tiêu &\nOKR Công ty", 120, 210, 200, 60, font_text)
    draw_arrow(draw, 220, 270, 220, 310)

    # Step 2: Phân bổ OKR xuống Phòng ban
    draw_box(draw, "Phân bổ OKR xuống\ncác Phòng ban", 120, 310, 200, 60, font_text)
    # Arrow chuyển sang làn Trưởng phòng
    draw_line_arrow(draw, [220, 370, 220, 390, 520, 390, 520, 410])

    # Step 3: Nhận OKR & Tạo KPI nhân viên
    draw_box(draw, "Thiết lập KPI nhân viên\n(Có trọng số & target)", 420, 410, 200, 60, font_text)
    draw_arrow(draw, 520, 470, 520, 500)

    # Step 4: Gửi duyệt KPI
    draw_box(draw, "Gửi duyệt KPI\nxuống hệ thống", 420, 500, 200, 60, font_text)
    # Chuyển sang làn Hệ thống
    draw_line_arrow(draw, [520, 560, 520, 580, 820, 580, 820, 600])

    # Step 5: Lưu nháp và gửi thông báo cho Director
    draw_box(draw, "Lưu trạng thái chờ duyệt\n& gửi thông báo email", 720, 600, 200, 60, font_text)
    # Quay lại làn Director để duyệt
    draw_line_arrow(draw, [820, 660, 820, 680, 300, 680, 300, 500, 220, 500, 220, 520])

    # Decision Node ở làn Director
    draw.polygon([(220, 520), (250, 540), (220, 560), (190, 540)], fill='#FEF3C7', outline='#D97706', width=2)
    draw.text((220, 540), "Duyệt?", fill='#B45309', font=font_text, anchor="mm")
    
    # Yes -> Chuyển sang làn Hệ thống
    draw_line_arrow(draw, [250, 540, 320, 540, 320, 480, 820, 480, 820, 500]) # Link to Step 6
    draw.text((280, 525), "Có", fill='#16A34A', font=font_text, anchor="mm")

    # Step 6: Kích hoạt KPI (Đang thực hiện)
    draw_box(draw, "Cập nhật trạng thái\n'Đang thực hiện'", 720, 500, 200, 60, font_text)
    draw_arrow(draw, 820, 560, 820, 570)
    
    # End node 1
    draw.ellipse([805, 570, 835, 600], fill='#1E293B')
    draw.ellipse([810, 575, 830, 595], fill='#F8FAFC')
    draw.ellipse([814, 579, 826, 591], fill='#1E293B')

    # No -> Quay lại làn Manager chỉnh sửa
    draw_line_arrow(draw, [190, 540, 100, 540, 100, 440, 420, 440])
    draw.text((140, 525), "Không", fill='#DC2626', font=font_text, anchor="mm")

    script_dir = os.path.dirname(os.path.abspath(__file__))
    output_path = os.path.join(script_dir, "activity_kpi_okr.png")
    img.save(output_path, "PNG")
    print(f"🎨 Đã vẽ xong activity_kpi_okr.png")


def draw_activity_checkin(font_path):
    w, h = 1000, 800
    img = Image.new('RGB', (w, h), '#F8FAFC')
    draw = ImageDraw.Draw(img)

    try:
        font_title = ImageFont.truetype(font_path, 22)
        font_lane = ImageFont.truetype(font_path, 18)
        font_text = ImageFont.truetype(font_path, 13)
    except:
        font_title = ImageFont.load_default()
        font_lane = ImageFont.load_default()
        font_text = ImageFont.load_default()

    draw.text((w // 2, 35), "QUY TRÌNH CHECK-IN VÀ PHÊ DUYỆT TIẾN ĐỘ", fill='#1E293B', font=font_title, anchor="mm")

    # Làn 1: Nhân viên (Employee) [80 -> 360]
    # Làn 2: Hệ thống (System) [360 -> 680]
    # Làn 3: Trưởng phòng (Manager) [680 -> 960]
    ly = 80
    lh = 700
    
    draw.line([360, ly, 360, ly + lh], fill='#94A3B8', width=2)
    draw.line([680, ly, 680, ly + lh], fill='#94A3B8', width=2)
    draw.rectangle([80, ly, 960, ly + lh], outline='#64748B', width=2)

    draw.text((220, ly + 25), "NHÂN VIÊN (EMPLOYEE)", fill='#4F46E5', font=font_lane, anchor="mm")
    draw.text((520, ly + 25), "HỆ THỐNG (SYSTEM)", fill='#374151', font=font_lane, anchor="mm")
    draw.text((820, ly + 25), "TRƯỞNG PHÒNG (MANAGER)", fill='#1D4ED8', font=font_lane, anchor="mm")
    
    draw.line([80, ly + 50, 960, ly + 50], fill='#64748B', width=2)

    # Start
    draw.ellipse([205, 150, 235, 180], fill='#64748B')
    draw.arrow = draw.line([220, 180, 220, 210], fill='#475569', width=2)
    draw.polygon([(216, 204), (220, 210), (224, 204)], fill='#475569')

    # Step 1: Xem KPI được giao
    draw_box(draw, "Xem danh sách KPI\n& yêu cầu check-in", 120, 210, 200, 60, font_text)
    draw_arrow(draw, 220, 270, 220, 300)

    # Step 2: Nhập giá trị check-in
    draw_box(draw, "Nhập kết quả đạt được\n& nội dung giải trình", 120, 300, 200, 60, font_text)
    # Chuyển sang làn Hệ thống
    draw_line_arrow(draw, [220, 360, 220, 380, 520, 380, 520, 400])

    # Step 3: Hệ thống tính toán
    draw_box(draw, "Auto-calculate % tiến độ,\nso sánh schedule progress", 420, 400, 200, 60, font_text)
    draw_arrow(draw, 520, 460, 520, 490)

    # Step 4: Đẩy vào Review Queue & báo email
    draw_box(draw, "Đẩy vào Review Queue\ngửi mail thông báo", 420, 490, 200, 60, font_text)
    # Chuyển sang làn Trưởng phòng
    draw_line_arrow(draw, [520, 550, 520, 570, 820, 570, 820, 590])

    # Step 5: Manager xem xét và đánh giá
    draw_box(draw, "Xem xét hồ sơ check-in,\nchấm điểm & viết nhận xét", 720, 590, 200, 60, font_text)
    draw_arrow(draw, 820, 650, 820, 670)

    # Decision Node ở làn Manager
    draw.polygon([(820, 670), (850, 690), (820, 710), (790, 690)], fill='#FEF3C7', outline='#D97706', width=2)
    draw.text((820, 690), "Duyệt?", fill='#B45309', font=font_text, anchor="mm")

    # Yes -> Chuyển sang làn Hệ thống để cập nhật tiến độ
    draw_line_arrow(draw, [790, 690, 680, 690, 680, 630, 520, 630, 520, 650])
    draw.text((740, 675), "Có", fill='#16A34A', font=font_text, anchor="mm")

    # Step 6: Cập nhật tiến độ KPI & OKR
    draw_box(draw, "Cập nhật tiến độ KPI,\nđồng bộ OKR liên quan", 420, 650, 200, 60, font_text)
    draw_arrow(draw, 520, 710, 520, 725)
    
    # End node
    draw.ellipse([505, 725, 535, 755], fill='#1E293B')
    draw.ellipse([510, 730, 530, 750], fill='#F8FAFC')
    draw.ellipse([514, 734, 526, 746], fill='#1E293B')

    # No -> Trả lại Employee để giải trình/nhập lại
    draw_line_arrow(draw, [820, 710, 820, 750, 220, 750, 220, 360])
    draw.text((520, 735), "Không (Từ chối)", fill='#DC2626', font=font_text, anchor="mm")

    script_dir = os.path.dirname(os.path.abspath(__file__))
    output_path = os.path.join(script_dir, "activity_checkin.png")
    img.save(output_path, "PNG")
    print(f"🎨 Đã vẽ xong activity_checkin.png")


def draw_activity_ai(font_path):
    w, h = 1000, 780
    img = Image.new('RGB', (w, h), '#F8FAFC')
    draw = ImageDraw.Draw(img)

    try:
        font_title = ImageFont.truetype(font_path, 22)
        font_lane = ImageFont.truetype(font_path, 18)
        font_text = ImageFont.truetype(font_path, 13)
    except:
        font_title = ImageFont.load_default()
        font_lane = ImageFont.load_default()
        font_text = ImageFont.load_default()

    draw.text((w // 2, 35), "QUY TRÌNH CHAT ADVISOR CÓ NGUỒN", fill='#1E293B', font=font_title, anchor="mm")

    # Làn 1: Người dùng (User) [80 -> 360]
    # Làn 2: Hệ thống (System) [360 -> 680]
    # Làn 3: Model gateway [680 -> 960]
    ly = 80
    lh = 660
    
    draw.line([360, ly, 360, ly + lh], fill='#94A3B8', width=2)
    draw.line([680, ly, 680, ly + lh], fill='#94A3B8', width=2)
    draw.rectangle([80, ly, 960, ly + lh], outline='#64748B', width=2)

    draw.text((220, ly + 25), "NGƯỜI DÙNG (USER)", fill='#1D4ED8', font=font_lane, anchor="mm")
    draw.text((520, ly + 25), "HỆ THỐNG / SERVICES", fill='#374151', font=font_lane, anchor="mm")
    draw.text((820, ly + 25), "MODEL GATEWAY", fill='#DB2777', font=font_lane, anchor="mm")
    
    draw.line([80, ly + 50, 960, ly + 50], fill='#64748B', width=2)

    # Start
    draw.ellipse([205, 150, 235, 180], fill='#64748B')
    draw.line([220, 180, 220, 210], fill='#475569', width=2)
    draw.polygon([(216, 204), (220, 210), (224, 204)], fill='#475569')

    # Step 1: Nhập yêu cầu chat / Đề xuất
    draw_box(draw, "Gửi câu hỏi / yêu cầu\ngợi ý KPI qua Chat widget", 120, 210, 200, 60, font_text)
    # Chuyển sang làn Hệ thống
    draw_line_arrow(draw, [220, 270, 220, 290, 520, 290, 520, 310])

    # Step 2: Thu thập dữ liệu ngữ cảnh
    draw_box(draw, "Xác thực tenant/scope; nạp\nSQL + RAG theo ACL", 420, 310, 200, 60, font_text)
    draw_arrow(draw, 520, 370, 520, 400)

    # Step 3: Tạo request dữ liệu tạm thời
    draw_box(draw, "Tạo request strict schema\nvới source ID do server cấp", 420, 400, 200, 60, font_text)
    # Chuyển sang làn model gateway
    draw_line_arrow(draw, [520, 460, 520, 480, 820, 480, 820, 500])

    # Step 4: Xử lý và phản hồi
    draw_box(draw, "Sinh JSON tư vấn\nkèm source IDs", 720, 500, 200, 60, font_text)
    # Quay lại làn Hệ thống
    draw_line_arrow(draw, [820, 560, 820, 580, 520, 580, 520, 600])

    # Step 5: Kiểm tra nguồn và lưu metadata tối thiểu
    draw_box(draw, "Recheck nguồn/quyền; lưu\nAgentRun + citation metadata", 420, 600, 200, 60, font_text)
    # Trả kết quả về cho Người dùng
    draw_line_arrow(draw, [420, 630, 320, 630, 320, 600, 220, 600, 220, 620])

    # Step 6: Xem kết quả AI tư vấn
    draw_box(draw, "Xem tư vấn và nguồn;\ncon người tự quyết định", 120, 620, 200, 60, font_text)
    draw_arrow(draw, 220, 680, 220, 700)
    
    # End node
    draw.ellipse([205, 700, 235, 730], fill='#1E293B')
    draw.ellipse([210, 705, 230, 725], fill='#F8FAFC')
    draw.ellipse([214, 709, 226, 721], fill='#1E293B')

    script_dir = os.path.dirname(os.path.abspath(__file__))
    output_path = os.path.join(script_dir, "activity_ai.png")
    img.save(output_path, "PNG")
    print(f"🎨 Đã vẽ xong activity_ai.png")


# ===================== HELPERS =====================

def draw_box(draw, text, x, y, w, h, font, bg='#F1F5F9', border='#475569'):
    # Bo góc nhẹ
    draw.rounded_rectangle([x, y, x + w, y + h], radius=6, fill=bg, outline=border, width=2)
    # Vẽ chữ
    draw.text((x + w // 2, y + h // 2), text, fill='#1E293B', font=font, anchor="mm", align="center")


def draw_arrow(draw, x1, y1, x2, y2, fill='#475569'):
    draw.line([x1, y1, x2, y2], fill=fill, width=2)
    # Vẽ mũi tên hướng xuống
    draw.polygon([(x2 - 4, y2 - 6), (x2, y2), (x2 + 4, y2 - 6)], fill=fill)


def draw_line_arrow(draw, points, fill='#475569'):
    # points: list [x1, y1, x2, y2, ...]
    draw.line(points, fill=fill, width=2)
    # Vẽ mũi tên ở điểm cuối cùng hướng xuống
    xe, ye = points[-2], points[-1]
    draw.polygon([(xe - 4, ye - 6), (xe, ye), (xe + 4, ye - 6)], fill=fill)


def main():
    script_dir = os.path.dirname(os.path.abspath(__file__))
    font_path = "C:\\Windows\\Fonts\\arial.ttf"
    if not os.path.exists(font_path):
        font_path = "C:\\Windows\\Fonts\\times.ttf"
        
    draw_activity_kpi_okr(font_path)
    draw_activity_checkin(font_path)
    draw_activity_ai(font_path)
    print("🚀 Đã hoàn thành vẽ toàn bộ sơ đồ hoạt động!")

if __name__ == '__main__':
    main()
