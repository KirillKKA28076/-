"""Repository checks, not a replacement for compilation or playtesting in Unity.

python -m pip install -r Tools/requirements-validator.txt
python Tools/validate_project.py
"""

import json
import re
import sys
from pathlib import Path

from fontTools.ttLib import TTFont
from tree_sitter import Language, Parser
import tree_sitter_c_sharp

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "Assets"
errors: list[str] = []

required_paths = [
    ROOT / "Assets/WarmBread/Scenes/Bakery_Street.unity",
    ROOT / "Assets/WarmBread/Scripts/Core/GameBootstrap.cs",
    ROOT / "Assets/WarmBread/Scripts/Core/GameSession.cs",
    ROOT / "Assets/WarmBread/Scripts/Core/CampaignRules.cs",
    ROOT / "Assets/WarmBread/Scripts/Core/SaveData.cs",
    ROOT / "Assets/WarmBread/Scripts/Core/SaveSystem.cs",
    ROOT / "Assets/WarmBread/Scripts/World/WorldArt.cs",
    ROOT / "Assets/WarmBread/Scripts/World/WorldBuilder.cs",
    ROOT / "Assets/WarmBread/Scripts/World/ModelFactory.cs",
    ROOT / "Assets/WarmBread/Scripts/World/ProceduralMeshFactory.cs",
    ROOT / "Assets/WarmBread/Scripts/World/StockDisplay.cs",
    ROOT / "Assets/WarmBread/Scripts/UI/GameUI.cs",
    ROOT / "Assets/Resources/Fonts/BreadSans.ttf",
    ROOT / "Packages/manifest.json",
    ROOT / "ProjectSettings/ProjectVersion.txt",
]
for path in required_paths:
    if not path.exists():
        errors.append(f"Required path is missing: {path.relative_to(ROOT)}")

bootstrap_files = sorted(ASSETS.rglob("GameBootstrap.cs"))
if len(bootstrap_files) != 1:
    relative = ", ".join(str(path.relative_to(ROOT)) for path in bootstrap_files)
    errors.append(
        f"Expected exactly one runtime GameBootstrap.cs, found {len(bootstrap_files)}: {relative}"
    )

parser = Parser(Language(tree_sitter_c_sharp.language()))
cs_files = sorted(ASSETS.rglob("*.cs"))
valid_unity_primitives = {"Sphere", "Capsule", "Cylinder", "Cube", "Plane", "Quad"}

for path in cs_files:
    source = path.read_bytes()
    tree = parser.parse(source)
    if tree.root_node.has_error:
        stack = [tree.root_node]
        while stack:
            node = stack.pop()
            if node.type == "ERROR" or node.is_missing:
                errors.append(
                    f"C# syntax: {path.relative_to(ROOT)}:{node.start_point.row + 1}: {node.type}"
                )
            stack.extend(node.children)

    text = source.decode("utf-8")
    for primitive in re.findall(r"\bPrimitiveType\.([A-Za-z_][A-Za-z0-9_]*)", text):
        if primitive not in valid_unity_primitives:
            errors.append(
                f"Unsupported Unity PrimitiveType.{primitive}: {path.relative_to(ROOT)}"
            )

    if "TODO: RELEASE-BLOCKER" in text or "NotImplementedException" in text:
        errors.append(f"Release blocker remains in: {path.relative_to(ROOT)}")

json_paths = [ROOT / "Packages/manifest.json", *ROOT.rglob("*.asmdef")]
for path in json_paths:
    try:
        json.loads(path.read_text(encoding="utf-8"))
    except Exception as error:
        errors.append(f"JSON: {path.relative_to(ROOT)}: {error}")

try:
    manifest = json.loads((ROOT / "Packages/manifest.json").read_text(encoding="utf-8"))
    dependencies = manifest.get("dependencies", {})
    for package in [
        "com.unity.ai.navigation",
        "com.unity.inputsystem",
        "com.unity.render-pipelines.universal",
        "com.unity.test-framework",
        "com.unity.textmeshpro",
    ]:
        if package not in dependencies:
            errors.append(f"Required Unity package is missing from manifest: {package}")
except Exception:
    pass

save_data_path = ROOT / "Assets/WarmBread/Scripts/Core/SaveData.cs"
if save_data_path.exists():
    save_data = save_data_path.read_text(encoding="utf-8")
    version = re.search(r"CurrentVersion\s*=\s*(\d+)", save_data)
    if version is None or int(version.group(1)) < 3:
        errors.append("SaveData.CurrentVersion must be at least 3 for campaign saves.")
    for field in [
        "totalSales",
        "totalRevenue",
        "goalsCompleted",
        "campaignCompleted",
        "endingId",
    ]:
        if not re.search(rf"\b{re.escape(field)}\b", save_data):
            errors.append(f"Campaign save field is missing: {field}")

campaign_path = ROOT / "Assets/WarmBread/Scripts/Core/CampaignRules.cs"
if campaign_path.exists():
    campaign = campaign_path.read_text(encoding="utf-8")
    days = re.search(r"CampaignDays\s*=\s*(\d+)", campaign)
    if days is None or int(days.group(1)) != 7:
        errors.append("CampaignRules must define a seven-day campaign.")
    for ending in ["home", "warm-light", "hard-autumn"]:
        if ending not in campaign:
            errors.append(f"Campaign ending is missing: {ending}")

model_factory_path = ROOT / "Assets/WarmBread/Scripts/World/ModelFactory.cs"
if model_factory_path.exists():
    model_factory = model_factory_path.read_text(encoding="utf-8")
    for method in [
        "BuildCustomer",
        "BuildCar",
        "BuildCashRegister",
        "BuildRadio",
        "BuildKettle",
        "BuildPhone",
        "BuildCat",
    ]:
        if not re.search(rf"\b{method}\s*\(", model_factory):
            errors.append(f"Detailed procedural model method is missing: {method}")

guids: dict[str, Path] = {}
for path in ASSETS.rglob("*.meta"):
    try:
        text = path.read_text(encoding="utf-8")
    except UnicodeDecodeError as error:
        errors.append(f"Metadata is not UTF-8: {path.relative_to(ROOT)}: {error}")
        continue

    match = re.search(r"^guid: ([0-9a-f]{32})$", text, re.MULTILINE)
    if not match:
        errors.append(f"Invalid metadata: {path.relative_to(ROOT)}")
        continue

    guid = match.group(1)
    if guid in guids:
        errors.append(
            f"Duplicate GUID: {path.relative_to(ROOT)}, {guids[guid].relative_to(ROOT)}"
        )
    guids[guid] = path

for path in ASSETS.rglob("*"):
    if path.name.endswith(".meta"):
        continue
    if not Path(str(path) + ".meta").exists():
        errors.append(f"Missing metadata: {path.relative_to(ROOT)}")

scene_path = ROOT / "Assets/WarmBread/Scenes/Bakery_Street.unity"
if scene_path.exists():
    scene = scene_path.read_text(encoding="utf-8")
    for guid in re.findall(r"guid: ([0-9a-f]{32})", scene):
        if guid not in guids:
            errors.append(f"Scene reference unresolved: {guid}")

font_path = ROOT / "Assets/Resources/Fonts/BreadSans.ttf"
if font_path.exists():
    font = TTFont(font_path)
    cmap = font.getBestCmap()
    required = set(
        "Тёплый хлеб ЙЦУКЕНГШЩЗХЪФЫВАПРОЛДЖЭЯЧСМИТЬБЮ"
        "йцукенгшщзхъфывапролджэячсмитьбюё ₽•×—«»0123456789"
    )
    for char in sorted(required):
        if ord(char) not in cmap:
            errors.append(f"Missing font glyph: {char}")

readme_path = ROOT / "README.md"
if readme_path.exists():
    readme = readme_path.read_text(encoding="utf-8")
    if "git clone --branch warm-bread-playable-v01" in readme:
        errors.append("README still instructs users to clone the obsolete playable branch.")

print(
    f"Parsed {len(cs_files)} C# files; checked JSON, {len(guids)} GUIDs, "
    "required paths, campaign/save schema, procedural model API, package manifest, "
    "scene references and Cyrillic font."
)
print("Unity compiler / EditMode / PlayMode / visual QA: NOT RUN by this script.")

for error in errors:
    print(error)

if errors:
    sys.exit(1)

print("PASS: static repository validation")
