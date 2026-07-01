"""
Script vẽ sơ đồ ERD tổng (Core Entities) bằng Pillow (PIL)
Bản vẽ thiết kế cơ sở dữ liệu quan hệ chuyên nghiệp, rõ ràng
"""

from PIL import Image, ImageDraw, ImageFont
import os

def draw_erd(font_path):
    w, h = 1650, 1150
    img = Image.new('RGB', (w, h), '#F8FAFC')
    draw = ImageDraw.Draw(img)

    try:
        font_title = ImageFont.truetype(font_path, 22)
        font_ent = ImageFont.truetype(font_path, 14)
        font_field = ImageFont.truetype(font_path, 12)
    except:
        font_title = ImageFont.load_default()
        font_ent = ImageFont.load_default()
        font_field = ImageFont.load_default()

    # Tiêu đề sơ đồ ERD
    draw.text((w // 2, 35), "SƠ ĐỒ CƠ SỞ DỮ LIỆU QUAN HỆ CỐT LÕI (CORE ERD)", fill='#1E293B', font=font_title, anchor="mm")

    # Định nghĩa các thực thể (Entities)
    # Cấu trúc: name, bg_header, fields, x, y, w, h
    entities = {
        'systemuser': {
            'title': "SystemUser (Tài khoản)", 'bg': '#475569',
            'fields': ["Id (PK) [int]", "Username [string]", "PasswordHash [string]", "Email [string]", "RoleId (FK) [int]", "IsActive [bool]"],
            'x': 550, 'y': 80, 'w': 220, 'h': 160
        },
        'employee': {
            'title': "Employee (Nhân viên)", 'bg': '#0F766E',
            'fields': ["Id (PK) [int]", "EmployeeCode [string]", "FullName [string]", "Phone [string]", "Email [string]", "DepartmentId (FK) [int]", "PositionId (FK) [int]", "SystemUserId (FK) [int]", "IsActive [bool]"],
            'x': 550, 'y': 330, 'w': 230, 'h': 210
        },
        'department': {
            'title': "Department (Phòng ban)", 'bg': '#0F766E',
            'fields': ["Id (PK) [int]", "DepartmentCode [string]", "DepartmentName [string]", "ManagerId (FK) [int]", "ParentId (FK) [int]"],
            'x': 180, 'y': 150, 'w': 240, 'h': 140
        },
        'position': {
            'title': "Position (Chức vụ)", 'bg': '#0F766E',
            'fields': ["Id (PK) [int]", "PositionCode [string]", "PositionName [string]", "RankLevel [int]"],
            'x': 180, 'y': 380, 'w': 220, 'h': 120
        },
        'okr': {
            'title': "OKR (Mục tiêu OKR)", 'bg': '#1D4ED8',
            'fields': ["Id (PK) [int]", "OKRName [string]", "OKRType (Company/Dept/Pers)", "Progress [decimal]", "YearlyGoalId (FK) [int]"],
            'x': 950, 'y': 100, 'w': 240, 'h': 140
        },
        'keyresult': {
            'title': "OKRKeyResult (Kết quả then chốt)", 'bg': '#1D4ED8',
            'fields': ["Id (PK) [int]", "OKRId (FK) [int]", "KRName [string]", "TargetValue [decimal]", "CurrentValue [decimal]", "Unit [string]", "IsInverse [bool]"],
            'x': 950, 'y': 340, 'w': 250, 'h': 170
        },
        'kpi': {
            'title': "KPI (Chỉ tiêu KPI)", 'bg': '#1D4ED8',
            'fields': ["Id (PK) [int]", "KPIName [string]", "KPIType (Đ.Lượng/Đ.Tính)", "PeriodId (FK) [int]", "Status [string]", "LinkedOKRId (FK) [int]", "LinkedKRId (FK) [int]"],
            'x': 950, 'y': 600, 'w': 250, 'h': 170
        },
        'kpidetail': {
            'title': "KPIDetail (Đặc tả KPI)", 'bg': '#1D4ED8',
            'fields': ["Id (PK) [int]", "KPIId (FK) [int]", "TargetValue [decimal]", "PassThreshold [decimal]", "CheckInFrequencyDays [int]"],
            'x': 950, 'y': 860, 'w': 240, 'h': 140
        },
        'checkin': {
            'title': "KPICheckIn (Báo cáo check-in)", 'bg': '#8B5CF6',
            'fields': ["Id (PK) [int]", "KPIId (FK) [int]", "EmployeeId (FK) [int]", "CheckInDate [datetime]", "AchievedValue [decimal]", "Status (Chờ duyệt/Đã duyệt)"],
            'x': 550, 'y': 640, 'w': 230, 'h': 160
        },
        'evalresult': {
            'title': "EvaluationResult (Đánh giá kỳ)", 'bg': '#B45309',
            'fields': ["Id (PK) [int]", "EmployeeId (FK) [int]", "PeriodId (FK) [int]", "TotalScore [decimal]", "RankString (S->D)", "BonusAmount [decimal]", "Status [string]"],
            'x': 180, 'y': 640, 'w': 240, 'h': 170
        },
        'project': {
            'title': "WorkProject (Dự án)", 'bg': '#091E42',
            'fields': ["Id (PK) [int]", "ProjectCode [string]", "ProjectName [string]", "Status [string]", "ProgressPercentage [decimal]", "LinkedOKRId (FK) [int]"],
            'x': 1330, 'y': 220, 'w': 230, 'h': 160
        },
        'workitem': {
            'title': "WorkItem (Công việc Kanban)", 'bg': '#091E42',
            'fields': ["Id (PK) [int]", "WorkProjectId (FK) [int]", "Title [string]", "AssigneeId (FK) [int]", "KPIId (FK) [int]", "KanbanStatus [string]", "ProgressPercentage [decimal]"],
            'x': 1330, 'y': 550, 'w': 240, 'h': 180
        }
    }

    # Vẽ các đường quan hệ trước để nằm dưới
    # Cấu trúc: ent_start, ent_end, points, label_start, label_end
    relations = [
        # SystemUser 1 - 1 Employee
        ('systemuser', 'employee', [660, 240, 660, 330], '1', '1'),
        # Department 1 - N Employee
        ('department', 'employee', [420, 220, 480, 220, 480, 400, 550, 400], '1', 'N'),
        # Position 1 - N Employee
        ('position', 'employee', [400, 440, 550, 440], '1', 'N'),
        # Department 1 - N OKR
        ('department', 'okr', [300, 150, 300, 120, 950, 120], '1', 'N'),
        # Employee 1 - N OKR
        ('employee', 'okr', [780, 360, 880, 360, 880, 220, 950, 220], '1', 'N'),
        # OKR 1 - N KeyResult
        ('okr', 'keyresult', [1070, 240, 1070, 340], '1', 'N'),
        # KeyResult 1 - N KPI
        ('keyresult', 'kpi', [1070, 510, 1070, 600], '1', 'N'),
        # KPI 1 - 1 KPIDetail
        ('kpi', 'kpidetail', [1070, 770, 1070, 860], '1', '1'),
        # KPI 1 - N KPICheckIn
        ('kpi', 'checkin', [950, 690, 780, 690], '1', 'N'),
        # Employee 1 - N KPICheckIn
        ('employee', 'checkin', [660, 540, 660, 640], '1', 'N'),
        # Employee 1 - N EvaluationResult
        ('employee', 'evalresult', [550, 480, 480, 480, 480, 725, 420, 725], '1', 'N'),
        # OKR 1 - N WorkProject
        ('okr', 'project', [1190, 180, 1260, 180, 1260, 300, 1330, 300], '1', 'N'),
        # WorkProject 1 - N WorkItem
        ('project', 'workitem', [1450, 380, 1450, 550], '1', 'N'),
        # WorkItem N - 1 KPI
        ('workitem', 'kpi', [1330, 680, 1200, 680], 'N', '1'),
        # WorkItem N - 1 Employee
        ('workitem', 'employee', [1330, 600, 1280, 600, 1280, 530, 780, 530], 'N', '1')
    ]

    # Vẽ đường nối quan hệ
    for start_key, end_key, pts, l_start, l_end in relations:
        draw.line(pts, fill='#94A3B8', width=2)
        # Vẽ các nhãn quan hệ
        xs, ys = pts[0], pts[1]
        xe, ye = pts[-2], pts[-1]
        
        # Nhãn start
        draw.text((xs + 8, ys + 8), l_start, fill='#475569', font=font_field)
        # Nhãn end
        draw.text((xe - 15, ye - 15), l_end, fill='#475569', font=font_field)

    # Vẽ các thực thể (Entity Boxes)
    for key, ent in entities.items():
        x, y, w_box, h_box = ent['x'], ent['y'], ent['w'], ent['h']
        # 1. Vẽ viền & bóng mờ nhẹ
        draw.rectangle([x, y, x + w_box, y + h_box], fill='#FFFFFF', outline='#94A3B8', width=2)
        # 2. Vẽ header
        draw.rectangle([x, y, x + w_box, y + 32], fill=ent['bg'])
        draw.text((x + w_box // 2, y + 16), ent['title'], fill='#FFFFFF', font=font_ent, anchor="mm")
        # 3. Vẽ các fields
        fy = y + 42
        for field in ent['fields']:
            # Đánh dấu in đậm cho khoá chính (PK) hoặc khoá ngoại (FK)
            is_key = "(PK)" in field or "(FK)" in field
            f_font = font_field
            f_color = '#0F172A'
            if is_key:
                f_color = '#1E3A8A' if "(PK)" in field else '#6B21A8'
            
            draw.text((x + 10, fy), field, fill=f_color, font=f_font)
            fy += 17

    script_dir = os.path.dirname(os.path.abspath(__file__))
    output_path = os.path.join(script_dir, "erd_diagram.png")
    img.save(output_path, "PNG")
    print(f"🎨 Đã vẽ xong erd_diagram.png")

def main():
    font_path = "C:\\Windows\\Fonts\\arial.ttf"
    if not os.path.exists(font_path):
        font_path = "C:\\Windows\\Fonts\\times.ttf"
    draw_erd(font_path)

if __name__ == '__main__':
    main()
