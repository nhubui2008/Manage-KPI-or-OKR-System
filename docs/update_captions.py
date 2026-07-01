import os
import re

docs_dir = r"c:\Users\Cua\Desktop\proj\Manage-KPI-or-OKR-System\docs"

new_table_caption = """def add_table_caption(doc, caption):
    \"\"\"Thêm caption cho bảng (Heading style 'Caption' + SEQ field cho Word) \"\"\"
    import re
    m = re.match(r"^(Bảng|Hình)\\s+(\\d+)\\s*:\\s*(.*)$", caption, re.IGNORECASE)
    
    p = doc.add_paragraph(style='Caption')
    p.alignment = 1 # WD_ALIGN_PARAGRAPH.CENTER
    pf = p.paragraph_format
    pf.space_before = Pt(4)
    pf.space_after = Pt(12)
    pf.keep_with_next = True
    
    if m:
        label = m.group(1) # Bảng hoặc Hình
        num = m.group(2)
        desc = m.group(3)
        
        # Thêm nhãn
        r1 = p.add_run(label + " ")
        set_font(r1, size=12, italic=True)
        r1.font.color.rgb = RGBColor(0, 0, 0)
        
        # Thêm SEQ field
        run_seq = p.add_run()
        set_font(run_seq, size=12, italic=True)
        run_seq.font.color.rgb = RGBColor(0, 0, 0)
        
        fldChar1 = OxmlElement('w:fldChar')
        fldChar1.set(qn('w:fldCharType'), 'begin')
        run_seq._r.append(fldChar1)
        
        run_instr = p.add_run()
        set_font(run_instr, size=12, italic=True)
        run_instr.font.color.rgb = RGBColor(0, 0, 0)
        instrText = OxmlElement('w:instrText')
        instrText.set(qn('xml:space'), 'preserve')
        instrText.text = f'SEQ {label} \\\\* ARABIC'
        run_instr._r.append(instrText)
        
        run_sep = p.add_run()
        set_font(run_sep, size=12, italic=True)
        run_sep.font.color.rgb = RGBColor(0, 0, 0)
        fldChar2 = OxmlElement('w:fldChar')
        fldChar2.set(qn('w:fldCharType'), 'separate')
        run_sep._r.append(fldChar2)
        
        run_num = p.add_run(num)
        set_font(run_num, size=12, italic=True, bold=True)
        run_num.font.color.rgb = RGBColor(0, 0, 0)
        
        run_end = p.add_run()
        set_font(run_end, size=12, italic=True)
        run_end.font.color.rgb = RGBColor(0, 0, 0)
        fldChar3 = OxmlElement('w:fldChar')
        fldChar3.set(qn('w:fldCharType'), 'end')
        run_end._r.append(fldChar3)
        
        # Thêm mô tả
        r2 = p.add_run(": " + desc)
        set_font(r2, size=12, italic=True)
        r2.font.color.rgb = RGBColor(0, 0, 0)
    else:
        run = p.add_run(caption)
        set_font(run, size=12, italic=True)
        run.font.color.rgb = RGBColor(0, 0, 0)
    return p"""

new_figure_caption = """def add_figure_caption(doc, caption):
    \"\"\"Thêm caption cho hình (Heading style 'Caption' + SEQ field cho Word) \"\"\"
    return add_table_caption(doc, caption)"""

# Regex patterns to match helper functions
pattern_table_caption = re.compile(r"def add_table_caption\(doc, caption\):.*?set_font\(run, size=12, italic=True\)(?:\s+return p)?", re.DOTALL)
pattern_figure_caption = re.compile(r"def add_figure_caption\(doc, caption\):.*?set_font\(run, size=12, italic=True\)(?:\s+return p)?", re.DOTALL)

print("Starting caption update...")
for filename in os.listdir(docs_dir):
    if filename.startswith("create_") and filename.endswith(".py"):
        filepath = os.path.join(docs_dir, filename)
        with open(filepath, 'r', encoding='utf-8') as f:
            content = f.read()

        modified = False
        if pattern_table_caption.search(content):
            content = pattern_table_caption.sub(lambda m: new_table_caption, content)
            modified = True
            
            # Append add_figure_caption if missing
            if "def add_figure_caption" not in content:
                content = content.replace(new_table_caption, new_table_caption + "\n\n\n" + new_figure_caption)

        if pattern_figure_caption.search(content):
            content = pattern_figure_caption.sub(lambda m: new_figure_caption, content)
            modified = True

        if modified:
            with open(filepath, 'w', encoding='utf-8') as f:
                f.write(content)
            print(f"Updated captions in {filename}")

print("Caption update completed successfully!")
