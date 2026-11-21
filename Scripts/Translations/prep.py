import re
import shutil
from pathlib import Path

# Variables
exports = Path(input("Path of folder of exported Crowdin files: "))
dest = Path(input("Destination resources folder: "))
icu_codes = {"zh-CN": "zh-Hans-CN", "zh-HK": "zh-Hant-HK", "zh-TW": "zh-Hant-TW"}

for source in exports.rglob("Strings.resx"):
    locale = source.parent.name
    if not re.fullmatch(r"[a-zA-Z]{2,3}(?:-[a-zA-Z0-9]{2,8})*", locale):
        continue

    dest.mkdir(parents=True, exist_ok=True)
    print(f"Copying {source}")
    shutil.copyfile(source, dest / f"Strings.{icu_codes.get(locale, locale)}.resx")
