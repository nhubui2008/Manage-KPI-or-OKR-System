"""
Script vẽ sơ đồ tuần tự (Sequence Diagram) bằng Pillow (PIL)
Bản vẽ thể hiện luồng giao tiếp giữa các đối tượng khi gọi Trợ lý AI Gemini
"""

from PIL import Image, ImageDraw, ImageFont
import os

def draw_sequence(font_path):
    w, h = 1100, 800
    img = Image.new('RGB', (w, h), '#F8FAFC')
    draw = ImageDraw.Draw(img)

    try:
        font_title = ImageFont.truetype(font_path, 20)
        font_obj = ImageFont.truetype(font_path, 14)
        font_text = ImageFont.truetype(font_path, 11)
    except:
        font_title = ImageFont.load_default()
        font_obj = ImageFont.load_default()
        font_text = ImageFont.load_default()

    # Tiêu đề sơ đồ
    draw.text((w // 2, 35), "SƠ ĐỒ TUẦN TỰ: QUY TRÌNH YÊU CẦU TƯ VẤN & GỢI Ý KPI BẰNG AI GEMINI", fill='#1E293B', font=font_title, anchor="mm")

    # Định nghĩa các đối tượng (Objects/Lifelines)
    # Cấu trúc: name, x
    objects = [
        {"name": "User (Browser)", "x": 100, "bg": '#E2E8F0'},
        {"name": "AIController", "x": 280, "bg": '#F1F5F9'},
        {"name": "AIDataService", "x": 480, "bg": '#F1F5F9'},
        {"name": "MiniERPDbContext", "x": 680, "bg": '#F1F5F9'},
        {"name": "GeminiService", "x": 860, "bg": '#FDF2F8'},
        {"name": "Gemini API (Google)", "x": 1020, "bg": '#FDF2F8'}
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
        (480, 180, 310),
        (680, 200, 270),
        (860, 350, 620),
        (1020, 390, 580)
    ]
    for x, y_start, y_end in activations:
        draw.rectangle([x - 8, y_start, x + 8, y_end], fill='#FFFFFF', outline='#475569', width=2)

    # 3. Vẽ các mũi tên thông điệp (Messages)
    # Message 1: Ajax gửi prompt & params
    draw_msg_arrow(draw, 108, 280, 160, "1. Gửi Ajax Request (prompt, context)", font_text)
    
    # Message 2: Gọi dựng context
    draw_msg_arrow(draw, 288, 480, 190, "2. GetKpiSuggestionsContextAsync()", font_text)

    # Message 3: Query DB
    draw_msg_arrow(draw, 488, 680, 210, "3. Query OKRs/KPIs/Employees", font_text)
    
    # Message 4: Trả dữ liệu raw
    draw_reply_arrow(draw, 672, 488, 250, "4. Trả về thực thể data", font_text)

    # Message 5: Trả về context string đã format
    draw_reply_arrow(draw, 472, 288, 290, "5. Trả về Context String (XML/JSON)", font_text)

    # Message 6: Kiểm tra rate limit
    draw_msg_arrow(draw, 288, 860, 350, "6. Cấu hình Prompt & Gọi Gemini", font_text)

    # Message 7: Post sang Google Server
    draw_msg_arrow(draw, 868, 1020, 390, "7. HTTP POST /v1beta/models", font_text)

    # Message 8: Phản hồi kết quả AI
    draw_reply_arrow(draw, 1012, 868, 560, "8. JSON Response (AI gợi ý)", font_text)

    # Message 9: Trả chuỗi text
    draw_reply_arrow(draw, 852, 288, 600, "9. Trả về kết quả tư vấn", font_text)

    # Message 10: Lưu log CSDL
    draw_msg_arrow(draw, 288, 680, 635, "10. Lưu log AIGenerationHistories", font_text)

    # Message 11: Trả Json kết quả về UI
    draw_reply_arrow(draw, 272, 108, 665, "11. Trả JSON dữ liệu AI", font_text)

    # Message 12: Hiển thị giao diện Chat
    draw_msg_self(draw, 100, 680, 710, "12. Render HTML & biểu đồ", font_text)

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
        font_path = "C:\\Windows\\Fonts\\times.ttf"
    draw_sequence(font_path)

if __name__ == '__main__':
    main()
