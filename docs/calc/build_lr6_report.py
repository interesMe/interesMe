#!/usr/bin/env python3
"""Збирає PDF і DOCX звіт ЛР №6 з docs/resources.md.

Запуск: python3 docs/calc/build_lr6_report.py
PDF формується через Chromium headless (потрібен chromium), DOCX — через python-docx.
"""
import html
import re
import subprocess
import sys
from pathlib import Path

DOCS = Path(__file__).resolve().parent.parent
SRC = DOCS / "resources.md"
PDF = DOCS / "LR6_Resource_Management_Report.pdf"
DOCX = DOCS / "LR6_Resource_Management_Report.docx"

TITLE = [
    ("НАЦІОНАЛЬНИЙ УНІВЕРСИТЕТ БІОРЕСУРСІВ І ПРИРОДОКОРИСТУВАННЯ УКРАЇНИ", True),
    ("Факультет інформаційних технологій", False),
    ("Кафедра комп’ютерних наук", False),
    ("", False),
    ("Управління IT-проектами", True),
    ("Лабораторна робота № 6", True),
    ("Управління ресурсами в Jira", True),
    ("Проєкт: InteresMe (IME)", False),
    ("", False),
    ("Виконав студент групи КН-2400 2б", False),
    ("Ритченко Максим Сергійович", False),
    ("", False),
    ("Київ 2026", False),
]


def parse(md):
    lines = md.splitlines()
    blocks, i = [], 0
    while i < len(lines):
        ln = lines[i]
        if not ln.strip() or ln.strip() == "---":
            i += 1
            continue
        if ln.startswith("|"):
            rows = []
            while i < len(lines) and lines[i].startswith("|"):
                cells = [c.strip() for c in lines[i].strip().strip("|").split("|")]
                rows.append(cells)
                i += 1
            head, align = rows[0], rows[1]
            blocks.append(("table", head, rows[2:], align))
            continue
        m = re.match(r"^(#{1,3}) (.*)", ln)
        if m:
            blocks.append(("h%d" % len(m.group(1)), m.group(2)))
        elif ln.startswith("!["):
            m = re.match(r"!\[(.*)\]\((.*)\)", ln)
            blocks.append(("img", m.group(1), m.group(2)))
        elif ln.startswith(">"):
            blocks.append(("quote", ln.lstrip("> ").strip()))
        elif re.match(r"^\d+\. ", ln):
            blocks.append(("li", ln))
        else:
            blocks.append(("p", ln))
        i += 1
    return blocks


def inline_html(t):
    t = html.escape(t)
    t = re.sub(r"\*\*(.+?)\*\*", r"<b>\1</b>", t)
    t = re.sub(r"`(.+?)`", r"<code>\1</code>", t)
    t = re.sub(r"\[(.+?)\]\((.+?)\)", lambda m: m.group(1), t)
    return t


def build_html(blocks):
    css = """
@page { size: A4; margin: 18mm 15mm 18mm 20mm; }
body { font-family: 'Liberation Serif','Times New Roman',serif; font-size: 12pt; line-height: 1.35; }
h1 { font-size: 16pt; text-align:center; margin: 0 0 4pt; }
h2 { font-size: 14pt; margin: 14pt 0 6pt; page-break-after: avoid; }
code { font-family: 'Liberation Mono',monospace; font-size: 9.5pt; }
table { border-collapse: collapse; width: 100%; margin: 6pt 0 10pt; font-size: 9.5pt; page-break-inside: auto; }
tr { page-break-inside: avoid; }
th, td { border: 1px solid #000; padding: 3pt 4pt; vertical-align: top; overflow-wrap: break-word; }
th { background: #e8e8e8; }
td.r { text-align: right; }
td.nw, th.nw { white-space: nowrap; }
figure { margin: 8pt 0 0; text-align: center; page-break-inside: avoid; page-break-after: avoid; }
figure img { max-width: 100%; border: 1px solid #999; }
p.cap { text-align:center; font-size: 10.5pt; margin: 2pt 0 10pt; }
blockquote { border-left: 3px solid #777; margin: 6pt 0; padding: 2pt 8pt; font-size: 11pt; }
.title { text-align:center; page-break-after: always; padding-top: 25mm; }
.title p { margin: 4pt 0; font-size: 14pt; }
"""
    out = ["<!doctype html><html lang='uk'><head><meta charset='utf-8'><style>%s</style></head><body>" % css]
    out.append("<div class='title'>")
    for text, bold in TITLE:
        out.append("<p>%s</p>" % ("<b>%s</b>" % text if bold else text) if text else "<p>&nbsp;</p>")
    out.append("</div>")
    skip_head = 0
    for b in blocks:
        k = b[0]
        if k == "h1":
            continue  # титульна сторінка
        if k == "h2" and skip_head == 0:
            skip_head = 1
            out.append("<h2>%s</h2>" % inline_html(b[1]))
            continue
        if k == "h2":
            out.append("<h2>%s</h2>" % inline_html(b[1]))
        elif k == "p":
            out.append("<p>%s</p>" % inline_html(b[1].replace("- ", "• ", 1) if b[1].startswith("- ") else b[1]))
        elif k == "quote":
            out.append("<blockquote>%s</blockquote>" % inline_html(b[1]))
        elif k == "li":
            out.append("<p>%s</p>" % inline_html(b[1]))
        elif k == "p" and b[1].startswith("- "):
            pass
        elif k == "img":
            p = (DOCS / b[2]).resolve()
            out.append("<figure><img src='file://%s' alt='%s'></figure>" % (p, html.escape(b[1])))
        elif k == "table":
            _, head, rows, align = b
            al = ["r" if a.strip().endswith(":") and not a.strip().startswith(":") else "" for a in align]
            out.append("<table><tr>%s</tr>" % "".join("<th class='%s'>%s</th>" % ("nw" if len(h) <= 14 else "", inline_html(h)) for h in head))
            for r in rows:
                out.append("<tr>%s</tr>" % "".join("<td class='%s'>%s</td>" % ((al[j] if j < len(al) else "") + (" nw" if len(c) <= 12 else ""), inline_html(c)) for j, c in enumerate(r)))
            out.append("</table>")
    out.append("</body></html>")
    return "\n".join(out)


def caption_fix(h):
    # підпис рисунка йде звичайним абзацом «Рис. N.» — робимо його по центру
    return re.sub(r"<p>(Рис\. .*?)</p>", r"<p class='cap'>\1</p>", h)


def build_docx(blocks):
    from docx import Document
    from docx.enum.text import WD_ALIGN_PARAGRAPH
    from docx.oxml import OxmlElement
    from docx.oxml.ns import qn
    from docx.shared import Cm, Pt

    d = Document()
    sec = d.sections[0]
    sec.left_margin, sec.right_margin = Cm(2.0), Cm(1.5)
    sec.top_margin = sec.bottom_margin = Cm(1.8)
    st = d.styles["Normal"]
    st.font.name = "Times New Roman"
    st.font.size = Pt(12)
    st.element.rPr.rFonts.set(qn("w:eastAsia"), "Times New Roman")

    def runs(par, text, size=None):
        for part in re.split(r"(\*\*.+?\*\*|`.+?`)", re.sub(r"\[(.+?)\]\((.+?)\)", r"\1", text)):
            if not part:
                continue
            if part.startswith("**"):
                r = par.add_run(part[2:-2]); r.bold = True
            elif part.startswith("`"):
                r = par.add_run(part[1:-1]); r.font.name = "Courier New"
            else:
                r = par.add_run(part)
            if size:
                r.font.size = Pt(size)

    for text, bold in TITLE:
        p = d.add_paragraph()
        p.alignment = WD_ALIGN_PARAGRAPH.CENTER
        r = p.add_run(text); r.bold = bold; r.font.size = Pt(14)
    d.add_page_break()

    first = True
    for b in blocks:
        k = b[0]
        if k == "h1":
            continue
        if k == "h2":
            h = d.add_paragraph()
            r = h.add_run(re.sub(r"\*\*|`", "", b[1])); r.bold = True; r.font.size = Pt(14)
            h.paragraph_format.keep_with_next = True
            h.paragraph_format.space_before = Pt(10)
        elif k in ("p", "li", "quote"):
            p = d.add_paragraph()
            if re.match(r"^Рис\. ", b[1]):
                p.alignment = WD_ALIGN_PARAGRAPH.CENTER
            runs(p, b[1], 11 if k == "quote" else None)
        elif k == "img":
            p = d.add_paragraph(); p.alignment = WD_ALIGN_PARAGRAPH.CENTER
            p.add_run().add_picture(str(DOCS / b[2]), width=Cm(16))
            p.paragraph_format.keep_with_next = True
        elif k == "table":
            _, head, rows, align = b
            t = d.add_table(rows=1, cols=len(head))
            t.style = "Table Grid"
            for j, h in enumerate(head):
                c = t.rows[0].cells[j]; c.text = ""
                runs(c.paragraphs[0], "**%s**" % h.replace("**", ""), 9)
                sh = OxmlElement("w:shd"); sh.set(qn("w:fill"), "E8E8E8"); sh.set(qn("w:val"), "clear")
                c._tc.get_or_add_tcPr().append(sh)
            for r_ in rows:
                cells = t.add_row().cells
                for j, v in enumerate(r_):
                    cells[j].text = ""
                    runs(cells[j].paragraphs[0], v, 9)
                    if j < len(align) and align[j].strip().endswith(":") and not align[j].strip().startswith(":"):
                        cells[j].paragraphs[0].alignment = WD_ALIGN_PARAGRAPH.RIGHT
            d.add_paragraph()
    d.save(DOCX)


def main():
    blocks = parse(SRC.read_text(encoding="utf-8"))
    h = caption_fix(build_html(blocks))
    tmp = Path("/tmp") / "lr6_report.html" if len(sys.argv) < 2 else Path(sys.argv[1])
    tmp.write_text(h, encoding="utf-8")
    subprocess.run([
        "chromium", "--headless", "--no-sandbox", "--disable-gpu", "--allow-file-access-from-files",
        "--no-pdf-header-footer", "--print-to-pdf=%s" % PDF, "file://%s" % tmp,
    ], check=True, stderr=subprocess.DEVNULL)
    build_docx(blocks)
    print("OK", PDF, DOCX)


if __name__ == "__main__":
    main()
