"""
Script vẽ sơ đồ tổ chức giao diện (Site Map) bằng Pillow (PIL)
Bản vẽ phân cấp sơ đồ trang web và luồng màn hình hiện đại
"""

from PIL import Image, ImageDraw, ImageFont
import os

def draw_sitemap(font_path):
    w, h = 1500, 850
    img = Image.new('RGB', (w, h), '#F8FAFC')
    draw = ImageDraw.Draw(img)

    try:
        font_title = ImageFont.truetype(font_path, 22)
        font_level1 = ImageFont.truetype(font_path, 16)
        font_level2 = ImageFont.truetype(font_path, 13)
    except:
        font_title = ImageFont.load_default()
        font_level1 = ImageFont.load_default()
        font_level2 = ImageFont.load_default()

    # Tiêu đề sơ đồ
    draw.text((w // 2, 40), "SƠ ĐỒ CẤU TRÚC PHÂN CẤP GIAO DIỆN HỆ THỐNG", fill='#1E293B', font=font_title, anchor="mm")

    # 1. Vẽ Root: TRANG CHỦ / LOGIN PORTAL
    draw_node(draw, "Cổng đăng nhập (Auth)\n[Google OAuth / OTP]", 150, 100, 240, 60, font_level1, bg='#475569', color='#FFFFFF')
    draw_node(draw, "DASHBOARD CHÍNH\n(Director / Manager / Employee / HR / Admin)", 600, 95, 450, 70, font_level1, bg='#1E3A8A', color='#FFFFFF')

    # Đường nối Root
    draw.line([390, 130, 600, 130], fill='#94A3B8', width=2)

    # Các phân hệ chính (Level 1)
    # y = 250
    levels = [
        {"title": "1. Chiến lược & OKR", "x": 100, "bg": '#E0F2FE', "border": '#0284C7',
         "subs": ["Định hướng Sứ mệnh", "Mục tiêu OKR 3 Cấp", "Kết quả KR chi tiết"]},
        
        {"title": "2. Quản lý KPI", "x": 380, "bg": '#E0F2FE', "border": '#0284C7',
         "subs": ["Thiết lập KPI", "Giao chỉ tiêu & Trọng số", "Review Queue (Duyệt)"]},
        
        {"title": "3. Check-in & Họp", "x": 660, "bg": '#EEF2F6', "border": '#475569',
         "subs": ["Lịch sử check-in", "Check-in tiến độ", "Lịch họp 1-on-1"]},
         
        {"title": "4. Công việc Kanban", "x": 940, "bg": '#F3F4F6', "border": '#4B5563',
         "subs": ["Bảng Kanban dự án", "Tạo mới & phân việc", "Theo dõi tiến độ"]},
         
        {"title": "5. Đánh giá & HR", "x": 1220, "bg": '#FEF3C7', "border": '#D97706',
         "subs": ["Kỳ đánh giá hiệu suất", "Xếp hạng Rank (S->D)", "Bảng tính thưởng & Excel"]}
    ]

    # Vẽ các đường nối xuống Level 1
    # Trực dọc trung tâm từ Dashboard xuống trục ngang
    draw.line([825, 165, 825, 210], fill='#94A3B8', width=2)
    # Trực ngang chính kết nối các cột
    draw.line([210, 210, 1330, 210], fill='#94A3B8', width=2)

    for item in levels:
        # Đường dọc xuống từng node cột
        cx = item['x'] + 110
        draw.line([cx, 210, cx, 250], fill='#94A3B8', width=2)
        
        # Vẽ node cột Level 1
        draw_node(draw, item['title'], item['x'], 250, 220, 50, font_level1, bg=item['bg'], border=item['border'])
        
        # Vẽ đường dọc xuống các node con
        sy = 300
        draw.line([cx, 300, cx, 300 + len(item['subs']) * 90 - 45], fill='#CBD5E1', width=2)
        
        # Vẽ các sub nodes (Level 2)
        for i, sub in enumerate(item['subs']):
            sub_y = sy + i * 90
            draw.line([cx, sub_y + 25, cx + 15, sub_y + 25], fill='#CBD5E1', width=2)
            draw_node(draw, sub, item['x'] + 20, sub_y, 190, 50, font_level2, bg='#FFFFFF', border='#CBD5E1')

    # Trực quan hóa Phân hệ Trợ lý AI có nguồn (Floating Node)
    # y = 600
    draw_node(draw, "Trợ lý AI có nguồn (Bizen AI Widget)\n[Phân tích / Gợi ý / Chat tư vấn]", 550, 710, 400, 60, font_level1, bg='#FDF2F8', border='#DB2777')
    # Đường nối chéo từ Dashboard và các nơi khác
    draw.line([825, 680, 825, 710], fill='#DB2777', width=2, joint="round")
    draw.ellipse([820, 675, 830, 685], fill='#DB2777')

    script_dir = os.path.dirname(os.path.abspath(__file__))
    output_path = os.path.join(script_dir, "site_map.png")
    img.save(output_path, "PNG")
    print(f"🎨 Đã vẽ xong site_map.png")


def draw_node(draw, text, x, y, w, h, font, bg='#FFFFFF', border=None, color='#1E293B'):
    if border:
        draw.rounded_rectangle([x, y, x + w, y + h], radius=5, fill=bg, outline=border, width=2)
    else:
        draw.rounded_rectangle([x, y, x + w, y + h], radius=5, fill=bg)
    draw.text((x + w // 2, y + h // 2), text, fill=color, font=font, anchor="mm", align="center")


def main():
    font_path = "C:\\Windows\\Fonts\\arial.ttf"
    if not os.path.exists(font_path):
        font_path = "C:\\Windows\\Fonts\\times.ttf"
    draw_sitemap(font_path)

if __name__ == '__main__':
    main()
