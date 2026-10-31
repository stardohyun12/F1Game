"""Print title / author / license / description snippets from saved OGA, Freesound, BSB pages. Usage: inspect_page.py HTMLFILE"""
import html
import re
import sys

import glob
import os

if os.path.isdir(sys.argv[1]):
    for p in sorted(glob.glob(os.path.join(sys.argv[1], "*.html"))):
        print("=====", os.path.basename(p))
        os.system(f'python -I "{os.path.abspath(__file__)}" "{p}"')
    sys.exit(0)

txt = open(sys.argv[1], "rb").read().decode("utf-8", "replace")


def strip(s):
    return re.sub(r"\s+", " ", html.unescape(re.sub(r"<[^>]+>", " ", s))).strip()


t = re.search(r"<title>(.*?)</title>", txt, re.S)
print("TITLE:", strip(t.group(1)) if t else "?")
# OGA
m = re.search(r'field-name-field-art-licenses.*?</div></div></div>', txt, re.S)
if m:
    print("OGA LICENSE:", strip(m.group(0)))
m = re.search(r'class="username"[^>]*>(.*?)</a>', txt, re.S)
if m:
    print("OGA AUTHOR:", strip(m.group(1)))
m = re.search(r'field-name-body.*?</div></div></div>', txt, re.S)
if m:
    print("OGA BODY:", strip(m.group(0))[:900])
# Freesound
for pat in (r'creativecommons\.org/[^"\' <]+', r'(?i)Attribution[^<]{0,60}', r'(?i)Creative Commons 0[^<]{0,60}'):
    for mm in list(re.finditer(pat, txt))[:3]:
        print("FS/CC:", mm.group(0))
m = re.search(r'<meta property="og:description" content="(.*?)"', txt, re.S)
if m:
    print("OG:", html.unescape(m.group(1))[:600])
for mm in list(re.finditer(r'(?i)(?:sound-license|license-name|sound_license)[^>]{0,200}>[^<]{0,200}', txt))[:3]:
    print("LICENSE-EL:", strip(mm.group(0))[:200])
for mm in list(re.finditer(r'(?i)(licen[sc]e[^<]{0,150}<[^>]*>[^<]{0,150})', txt))[:6]:
    print("LIC:", strip(mm.group(0))[:250])
for mm in list(re.finditer(r'(?:previews|cdn\.freesound)[^"\' ]+\.(?:mp3|ogg)', txt))[:4]:
    print("PREVIEW:", mm.group(0))
