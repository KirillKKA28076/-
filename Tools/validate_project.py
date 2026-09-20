"""Repository checks, not a replacement for compilation or playtesting in Unity.

python -m pip install tree-sitter tree-sitter-c-sharp fonttools
python Tools/validate_project.py
"""
import hashlib
import json
import re
import sys
from pathlib import Path

from fontTools.ttLib import TTFont
from tree_sitter import Language, Parser
import tree_sitter_c_sharp

ROOT = Path(__file__).resolve().parents[1]
parser = Parser(Language(tree_sitter_c_sharp.language()))
errors = []
cs_files = sorted((ROOT / "Assets").rglob("*.cs"))
for path in cs_files:
    tree = parser.parse(path.read_bytes())
    if tree.root_node.has_error:
        stack = [tree.root_node]
        while stack:
            node = stack.pop()
            if node.type == "ERROR" or node.is_missing:
                errors.append(f"C# syntax: {path.relative_to(ROOT)}:{node.start_point.row + 1}: {node.type}")
            stack.extend(node.children)

for path in [ROOT / "Packages/manifest.json", *ROOT.rglob("*.asmdef")]:
    try:
        json.loads(path.read_text())
    except Exception as error:
        errors.append(f"JSON: {path}: {error}")

guids = {}
for path in (ROOT / "Assets").rglob("*.meta"):
    match = re.search(r"^guid: ([0-9a-f]{32})$", path.read_text(), re.M)
    if not match:
        errors.append(f"Invalid metadata: {path}")
        continue
    guid = match.group(1)
    if guid in guids:
        errors.append(f"Duplicate GUID: {path}, {guids[guid]}")
    guids[guid] = path

for path in (ROOT / "Assets").rglob("*"):
    if not str(path).endswith(".meta") and not Path(str(path) + ".meta").exists():
        errors.append(f"Missing metadata: {path.relative_to(ROOT)}")

scene = (ROOT / "Assets/WarmBread/Scenes/Bakery_Street.unity").read_text()
for guid in re.findall(r"guid: ([0-9a-f]{32})", scene):
    if guid not in guids:
        errors.append(f"Scene reference unresolved: {guid}")

font = TTFont(ROOT / "Assets/Resources/Fonts/BreadSans.ttf")
cmap = font.getBestCmap()
required = set("Тёплый хлеб ЙЦУКЕНГШЩЗХЪФЫВАПРОЛДЖЭЯЧСМИТЬБЮйцукенгшщзхъфывапролджэячсмитьбюё ₽•×—«»0123456789")
for char in required:
    if ord(char) not in cmap:
        errors.append(f"Missing font glyph: {char}")

print(f"Parsed {len(cs_files)} C# files; checked JSON, {len(guids)} GUIDs, scene references and Cyrillic font.")
print("Unity compiler / EditMode / PlayMode / visual QA: NOT RUN by this script.")
for error in errors:
    print(error)
if errors:
    sys.exit(1)
print("PASS: static repository validation")
