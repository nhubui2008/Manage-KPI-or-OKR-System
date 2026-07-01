import os
import re

docs_dir = r"c:\Users\Cua\Desktop\proj\Manage-KPI-or-OKR-System\docs"

new_chapter_title = """def add_chapter_title(doc, text):
    \"\"\"Tiêu đề chương (CHƯƠNG X: ...) - căn giữa, in đậm, viết hoa (Heading 1)\"\"\"
    p = doc.add_paragraph(style='Heading 1')
    p.alignment = 1 # WD_ALIGN_PARAGRAPH.CENTER
    pf = p.paragraph_format
    pf.space_before = Pt(24)
    pf.space_after = Pt(18)
    pf.keep_with_next = True
    run = p.add_run(text.upper())
    set_font(run, size=14, bold=True)
    run.font.color.rgb = RGBColor(0, 0, 0)
    return p"""

new_section_title = """def add_section_title(doc, title, font_size=16):
    \"\"\"Thêm tiêu đề phần (căn giữa, in hoa, đậm) (Heading 1)\"\"\"
    p = doc.add_paragraph(style='Heading 1')
    p.alignment = 1 # WD_ALIGN_PARAGRAPH.CENTER
    pf = p.paragraph_format
    pf.space_before = Pt(24)
    pf.space_after = Pt(18)
    pf.keep_with_next = True
    run = p.add_run(title.upper())
    set_font(run, size=font_size, bold=True)
    run.font.color.rgb = RGBColor(0, 0, 0)
    return p"""

new_heading1 = """def add_heading1(doc, text):
    \"\"\"Tiêu đề cấp 1 (1.1. ...) - in đậm (Heading 2)\"\"\"
    p = doc.add_paragraph(style='Heading 2')
    p.alignment = 0 # WD_ALIGN_PARAGRAPH.LEFT
    pf = p.paragraph_format
    pf.space_before = Pt(18)
    pf.space_after = Pt(8)
    pf.keep_with_next = True
    run = p.add_run(text)
    set_font(run, size=14, bold=True)
    run.font.color.rgb = RGBColor(0, 0, 0)
    return p"""

new_heading2 = """def add_heading2(doc, text):
    \"\"\"Tiêu đề cấp 2 (1.1.1. ...) - in đậm (Heading 3)\"\"\"
    p = doc.add_paragraph(style='Heading 3')
    p.alignment = 0 # WD_ALIGN_PARAGRAPH.LEFT
    pf = p.paragraph_format
    pf.space_before = Pt(12)
    pf.space_after = Pt(6)
    pf.keep_with_next = True
    run = p.add_run(text)
    set_font(run, size=13, bold=True)
    run.font.color.rgb = RGBColor(0, 0, 0)
    return p"""

new_heading3 = """def add_heading3(doc, text):
    \"\"\"Tiêu đề cấp 3 (1.1.1.1. ...) - in đậm (Heading 4)\"\"\"
    p = doc.add_paragraph(style='Heading 4')
    p.alignment = 0 # WD_ALIGN_PARAGRAPH.LEFT
    pf = p.paragraph_format
    pf.space_before = Pt(10)
    pf.space_after = Pt(4)
    pf.keep_with_next = True
    run = p.add_run(text)
    set_font(run, size=13, bold=True)
    run.font.color.rgb = RGBColor(0, 0, 0)
    return p"""

# Regex patterns for matching
pattern_chapter = re.compile(r"def add_chapter_title\(doc, text\):.*?return p", re.DOTALL)
pattern_section = re.compile(r"def add_section_title\(doc, title,.*?\):.*?return p", re.DOTALL)
pattern_heading1 = re.compile(r"def add_heading1\(doc, text\):.*?return p", re.DOTALL)
pattern_heading2 = re.compile(r"def add_heading2\(doc, text\):.*?return p", re.DOTALL)
pattern_heading3 = re.compile(r"def add_heading3\(doc, text\):.*?return p", re.DOTALL)

print("Starting heading update...")
for filename in os.listdir(docs_dir):
    if filename.startswith("create_") and filename.endswith(".py"):
        filepath = os.path.join(docs_dir, filename)
        with open(filepath, 'r', encoding='utf-8') as f:
            content = f.read()

        modified = False
        if pattern_chapter.search(content):
            content = pattern_chapter.sub(new_chapter_title, content)
            modified = True
        if pattern_section.search(content):
            content = pattern_section.sub(new_section_title, content)
            modified = True
        if pattern_heading1.search(content):
            content = pattern_heading1.sub(new_heading1, content)
            modified = True
        if pattern_heading2.search(content):
            content = pattern_heading2.sub(new_heading2, content)
            modified = True
        if pattern_heading3.search(content):
            content = pattern_heading3.sub(new_heading3, content)
            modified = True

        if modified:
            # Check if Pt, Cm, RGBColor are defined or we just need to ensure the variables are correct
            with open(filepath, 'w', encoding='utf-8') as f:
                f.write(content)
            print(f"Updated headings in {filename}")

print("Heading update completed successfully!")
