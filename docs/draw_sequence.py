"""
Script vẽ sơ đồ tuần tự (Sequence Diagram) bằng Pillow (PIL)
Bản vẽ thể hiện luồng giao tiếp của KPI Suggestion Advisor có nguồn
"""

from PIL import Image, ImageDraw, ImageFont
import os

def draw_sequence(font_path):
    w, h = 1100, 800
    img = Image.new('RGB', (w, h), '#F8FAFC')
    draw = ImageDraw.Draw(img)

    try:
        font_title = ImageFont.truetype(font_path, 18)
        font_obj = ImageFont.truetype(font_path, 12)
        font_text = ImageFont.truetype(font_path, 10)
    except:
        font_title = ImageFont.load_default()
        font_obj = ImageFont.load_default()
        font_text = ImageFont.load_default()

    # Tiêu đề sơ đồ
    draw.text((w // 2, 35), "SƠ ĐỒ TUẦN TỰ: KPI SUGGESTION ADVISOR CÓ NGUỒN", fill='#1E293B', font=font_title, anchor="mm")

    # Định nghĩa các đối tượng (Objects/Lifelines)
    # Cấu trúc: name, x
    objects = [
        {"name": "User (Browser)", "x": 100, "bg": '#E2E8F0'},
        {"name": "AIController", "x": 280, "bg": '#F1F5F9'},
        {"name": "KpiSuggestionAdvisor", "x": 460, "bg": '#F1F5F9'},
        {"name": "AIDataService", "x": 640, "bg": '#F1F5F9'},
        {"name": "MiniERPDbContext", "x": 820, "bg": '#F1F5F9'},
        {"name": "IAIModelClient", "x": 1000, "bg": '#FDF2F8'}
    ]

    # Vẽ các hình chữ nhật đối tượng ở đầu và đường nét đứt (Lifeline)
    ly1 = 90
    ly2 = 720
    for obj in objects:
        x = obj['x']
        # Hộp đối tượng
        draw.rectangle([x - 70, ly1, x + 70, ly1 + 40], fill=obj['bg'], outline='#475569', width=2)
        draw.text((x, ly1 + 20), obj['name'], fill='#0F172A', font=font_obj, anchor="mm")
        
        # Đường nét đứt (Lifeline) dọc
        draw_dashed_line(draw, x, ly1 + 40, ly2, fill='#94A3B8')

    # Vẽ các khối kích hoạt (Activation Boxes)
    # (x_center, y_start, y_end)
    activations = [
        (100, 150, 700),
        (280, 150, 680),
        (460, 180, 650),
        (640, 210, 540),
        (820, 230, 620),
        (1000, 350, 430)
    ]
    for x, y_start, y_end in activations:
        draw.rectangle([x - 8, y_start, x + 8, y_end], fill='#FFFFFF', outline='#475569', width=2)

    # 3. Vẽ các mũi tên thông điệp (Messages)
    # Message 1: Ajax chỉ gửi các ID phạm vi
    draw_msg_arrow(draw, 108, 280, 160, "1. POST /AI/SuggestKPI (scope IDs)", font_text)
    
    # Message 2: Controller đã kiểm tra anti-forgery và permission
    draw_msg_arrow(draw, 288, 460, 190, "2. SuggestAsync sau CSRF/quyền", font_text)

    # Message 3: Advisor dựng snapshot được cấp quyền
    draw_msg_arrow(draw, 468, 640, 220, "3. Build authorized snapshot", font_text)
    
    # Message 4: Data service truy vấn tenant/scope
    draw_msg_arrow(draw, 648, 820, 250, "4. Query period/OKR/KPI scope", font_text)

    # Message 5: Trả snapshot tối thiểu và fingerprint đầu
    draw_reply_arrow(draw, 812, 468, 300, "5. Snapshot tối thiểu", font_text)

    # Message 6: Gọi model gateway
    draw_msg_arrow(draw, 468, 1000, 350, "6. Strict JSON + allowed source IDs", font_text)

    # Message 7: Model trả bản nháp
    draw_reply_arrow(draw, 992, 468, 420, "7. 3-5 drafts hoặc abstain", font_text)

    # Message 8: Validate schema/source/business rules
    draw_msg_self(draw, 460, 440, 475, "8. Validate schema/ngưỡng/nguồn", font_text)

    # Message 9: Dựng lại snapshot trong transaction
    draw_msg_arrow(draw, 468, 640, 510, "9. Rebuild snapshot (Serializable)", font_text)

    # Message 10: Chỉ commit metadata
    draw_msg_arrow(draw, 468, 820, 570, "10. Commit run + citation metadata", font_text)

    # Message 11: Trả draft có nguồn
    draw_reply_arrow(draw, 452, 288, 630, "11. Cited advisory drafts", font_text)

    # Message 12: Người dùng áp dụng vào form, chưa tạo KPI
    draw_reply_arrow(draw, 272, 108, 680, "12. Điền form (chưa lưu)", font_text)

    # Lưu ảnh ra thư mục docs
    script_dir = os.path.dirname(os.path.abspath(__file__))
    output_path = os.path.join(script_dir, "sequence_ai.png")
    img.save(output_path, "PNG")
    print(f"🎨 Đã vẽ xong sequence_ai.png")


# ===================== HELPERS =====================

def draw_dashed_line(draw, x, y1, y2, fill='#94A3B8', dash_len=8, gap_len=6):
    curr_y = y1
    while curr_y < y2:
        draw.line([x, curr_y, x, min(curr_y + dash_len, y2)], fill=fill, width=1)
        curr_y += dash_len + gap_len


def draw_msg_arrow(draw, x1, x2, y, text, font, fill='#0F172A'):
    # Vẽ đường thẳng mũi tên gọi
    draw.line([x1, y, x2, y], fill=fill, width=1)
    # Đầu mũi tên hướng sang phải (nếu x2 > x1)
    if x2 > x1:
        draw.polygon([(x2 - 5, y - 4), (x2, y), (x2 - 5, y + 4)], fill=fill)
    else:
        draw.polygon([(x2 + 5, y - 4), (x2, y), (x2 + 5, y + 4)], fill=fill)
        
    # Ghi nhãn text nằm trên đường thẳng
    draw.text(((x1 + x2) // 2, y - 10), text, fill='#334155', font=font, anchor="mm")


def draw_reply_arrow(draw, x1, x2, y, text, font, fill='#475569'):
    # Vẽ đường nét đứt mũi tên phản hồi
    draw_dashed_line_horiz(draw, min(x1, x2), max(x1, x2), y, fill=fill)
    # Đầu mũi tên hướng sang trái (nếu x2 < x1)
    if x2 < x1:
        draw.polygon([(x2 + 5, y - 4), (x2, y), (x2 + 5, y + 4)], fill=fill)
    else:
        draw.polygon([(x2 - 5, y - 4), (x2, y), (x2 - 5, y + 4)], fill=fill)
        
    # Ghi nhãn text nằm trên đường thẳng
    draw.text(((x1 + x2) // 2, y - 10), text, fill='#334155', font=font, anchor="mm")


def draw_dashed_line_horiz(draw, x1, x2, y, fill='#475569', dash_len=8, gap_len=6):
    curr_x = x1
    while curr_x < x2:
        draw.line([curr_x, y, min(curr_x + dash_len, x2), y], fill=fill, width=1)
        curr_x += dash_len + gap_len


def draw_msg_self(draw, x, y_start, y_end, text, font, fill='#0F172A'):
    # Vẽ đường gấp khúc tự gọi
    draw.line([x + 8, y_start, x + 35, y_start], fill=fill, width=1)
    draw.line([x + 35, y_start, x + 35, y_end], fill=fill, width=1)
    draw.line([x + 35, y_end, x + 8, y_end], fill=fill, width=1)
    # Đầu mũi tên quay về
    draw.polygon([(x + 13, y_end - 4), (x + 8, y_end), (x + 13, y_end + 4)], fill=fill)
    
    # Ghi nhãn text
    draw.text((x + 40, (y_start + y_end) // 2), text, fill='#334155', font=font, anchor="lm")


def main():
    font_path = "C:\\Windows\\Fonts\\arial.ttf"
    if not os.path.exists(font_path):
        font_path = "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf"
    if not os.path.exists(font_path):
        font_path = "C:\\Windows\\Fonts\\times.ttf"
    draw_sequence(font_path)

if __name__ == '__main__':
    main()
