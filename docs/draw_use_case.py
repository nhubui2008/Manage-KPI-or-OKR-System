"""
Script vẽ sơ đồ Use Case bằng Pillow (PIL)
Bản thiết kế hiện đại, độ phân giải cao, chữ tiếng Việt chuẩn xác
"""

from PIL import Image, ImageDraw, ImageFont
import os

def draw_use_case():
    # Kích thước ảnh
    w, h = 1600, 1150
    img = Image.new('RGB', (w, h), '#F8FAFC')
    draw = ImageDraw.Draw(img)

    # Thử load font Times New Roman hoặc Arial, nếu lỗi dùng default
    font_path = "C:\\Windows\\Fonts\\arial.ttf"
    if not os.path.exists(font_path):
        font_path = "C:\\Windows\\Fonts\\times.ttf"
    
    try:
        font_actor = ImageFont.truetype(font_path, 20)
        font_uc = ImageFont.truetype(font_path, 16)
        font_title = ImageFont.truetype(font_path, 24)
    except:
        font_actor = ImageFont.load_default()
        font_uc = ImageFont.load_default()
        font_title = ImageFont.load_default()

    # 1. Vẽ System Boundary Box
    # Toạ độ box
    bx1, by1, bx2, by2 = 320, 80, 1280, 1100
    # Viền box
    draw.rectangle([bx1, by1, bx2, by2], outline='#94A3B8', width=2)
    # Tiêu đề hệ thống
    title_text = "HỆ THỐNG VẬN HÀNH THÔNG MINH (KPI - OKR - AI)"
    draw.text((w // 2, 45), title_text, fill='#1E293B', font=font_title, anchor="mm")

    # 2. Định nghĩa các Actor (Stick Figures)
    # Cấu trúc: name, x, y, connections (id_uc)
    actors = {
        'director': {
            'name': "Ban Giám Đốc\n(Director)", 'x': 140, 'y': 280,
            'color': '#0F766E', 'conns': [1, 2, 7, 8]
        },
        'manager': {
            'name': "Trưởng Phòng\n(Manager)", 'x': 140, 'y': 580,
            'color': '#1D4ED8', 'conns': [3, 4, 5, 7, 8]
        },
        'employee': {
            'name': "Nhân Viên\n(Employee)", 'x': 140, 'y': 880,
            'color': '#4F46E5', 'conns': [5, 6, 8]
        },
        'hr': {
            'name': "Nhân Sự\n(HR)", 'x': 1460, 'y': 480,
            'color': '#B45309', 'conns': [9, 10]
        },
        'admin': {
            'name': "Quản Trị\n(Admin)", 'x': 1460, 'y': 800,
            'color': '#374151', 'conns': [11]
        }
    }

    # 3. Định nghĩa các Use Case
    # Cấu trúc: id, text, x, y (tâm)
    use_cases = {
        1: {'text': "Thiết lập Chiến lược\n& OKR Công ty", 'x': 520, 'y': 180, 'bg': '#E0F2FE', 'border': '#0284C7'},
        2: {'text': "Duyệt Đánh giá\n& Quỹ thưởng", 'x': 520, 'y': 340, 'bg': '#E0F2FE', 'border': '#0284C7'},
        3: {'text': "Phân bổ OKR\n& Giao KPI phòng ban", 'x': 520, 'y': 500, 'bg': '#E0F2FE', 'border': '#0284C7'},
        4: {'text': "Phê duyệt Check-in\n(Review Queue)", 'x': 520, 'y': 660, 'bg': '#E0F2FE', 'border': '#0284C7'},
        5: {'text': "Quản lý Dự án\n& Công việc Kanban", 'x': 520, 'y': 820, 'bg': '#E2E8F0', 'border': '#475569'},
        6: {'text': "Check-in tiến độ\nKPI cá nhân", 'x': 520, 'y': 980, 'bg': '#EEF2F6', 'border': '#64748B'},
        
        7: {'text': "Cảnh báo rủi ro\n(Smart Alerts AI)", 'x': 1050, 'y': 240, 'bg': '#FDF2F8', 'border': '#DB2777'},
        8: {'text': "Trợ lý AI có nguồn\nChat & Bản nháp", 'x': 1050, 'y': 420, 'bg': '#FDF2F8', 'border': '#DB2777'},
        9: {'text': "Cơ cấu Tổ chức\n& Hồ sơ Nhân sự", 'x': 1050, 'y': 600, 'bg': '#FEF3C7', 'border': '#D97706'},
        10: {'text': "Cấu hình Thưởng\n& Kỳ đánh giá", 'x': 1050, 'y': 780, 'bg': '#FEF3C7', 'border': '#D97706'},
        11: {'text': "Quản lý gói SaaS\n& Hệ thống", 'x': 1050, 'y': 960, 'bg': '#F3F4F6', 'border': '#4B5563'}
    }

    # 4. Vẽ các đường kết nối trước (nằm phía dưới ovals)
    for act_name, act_info in actors.items():
        ax, ay = act_info['x'], act_info['y']
        
        # Điểm mốc vẽ đường nối (tùy thuộc Actor nằm bên trái hay phải)
        if ax < w // 2:
            anchor_x = ax + 50
        else:
            anchor_x = ax - 50
            
        anchor_y = ay - 10
        
        for uc_id in act_info['conns']:
            uc = use_cases[uc_id]
            # Điểm nối vào viền Use Case (phía trái hoặc phải)
            if uc['x'] < w // 2:
                uc_edge_x = uc['x'] - 110 if ax < w // 2 else uc['x'] + 110
            else:
                uc_edge_x = uc['x'] - 110 if ax < w // 2 else uc['x'] + 110
            
            # Vẽ đường nối mềm mại
            draw.line([anchor_x, anchor_y, uc_edge_x, uc['y']], fill='#CBD5E1', width=2)

    # 5. Vẽ hình Actor (Stick Figure) và Nhãn
    for act_name, act_info in actors.items():
        ax, ay = act_info['x'], act_info['y']
        color = act_info['color']
        
        # Vẽ đầu
        draw.ellipse([ax - 15, ay - 60, ax + 15, ay - 30], outline=color, width=3)
        # Vẽ thân
        draw.line([ax, ay - 30, ax, ay + 10], fill=color, width=3)
        # Vẽ tay
        draw.line([ax - 25, ay - 15, ax + 25, ay - 15], fill=color, width=3)
        # Vẽ chân
        draw.line([ax, ay + 10, ax - 20, ay + 40], fill=color, width=3)
        draw.line([ax, ay + 10, ax + 20, ay + 40], fill=color, width=3)
        
        # Nhãn Actor
        draw.text((ax, ay + 65), act_info['name'], fill='#1E293B', font=font_actor, anchor="mm", align="center")

    # 6. Vẽ các hình Oval Use Case và text
    ow, oh = 230, 95  # Rộng và cao của Use Case
    for uc_id, uc in use_cases.items():
        x, y = uc['x'], uc['y']
        # Vẽ Oval nền
        draw.ellipse([x - ow//2, y - oh//2, x + ow//2, y + oh//2], fill=uc['bg'], outline=uc['border'], width=2)
        # Vẽ text bên trong
        draw.text((x, y), uc['text'], fill='#1E293B', font=font_uc, anchor="mm", align="center")

    # Lưu ảnh ra thư mục docs
    script_dir = os.path.dirname(os.path.abspath(__file__))
    output_img_path = os.path.join(script_dir, "use_case_diagram.png")
    img.save(output_img_path, "PNG")
    print(f"🎨 Đã tạo xong hình ảnh sơ đồ Use Case tại: {output_img_path}")

if __name__ == '__main__':
    draw_use_case()
