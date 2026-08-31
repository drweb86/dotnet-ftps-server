#!/usr/bin/env python3
"""Convert Avalonia Strings*.resx files into Android strings.xml resources."""
from __future__ import annotations

import re
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
RESX_DIR = ROOT / "sources" / "FtpsServerAvalonia" / "FtpsServerAvalonia" / "Resources"
OUT_DIR = ROOT / "sources" / "android" / "app" / "src" / "main" / "res"

LOCALE_TO_VALUES = {
    "": "values",
    "de": "values-de",
    "es": "values-es",
    "fr": "values-fr",
    "ja": "values-ja",
    "ko": "values-ko",
    "ru": "values-ru",
    "zh-Hans": "values-zh-rCN",
    "pt-BR": "values-pt-rBR",
    "it": "values-it",
    "pl": "values-pl",
    "uk": "values-uk",
    "tr": "values-tr",
    "ar": "values-ar",
    "fa": "values-fa",
    "hi": "values-hi",
    "id": "values-in",
    "th": "values-th",
    "vi": "values-vi",
    "bn": "values-bn",
    "ta": "values-ta",
    "te": "values-te",
    "mr": "values-mr",
    "pa": "values-pa",
    "ur": "values-ur",
    "ne": "values-ne",
    "my": "values-my",
    "kk": "values-kk",
    "uz": "values-uz",
    "am": "values-am",
    "sw": "values-sw",
    "ha": "values-ha",
    "yo": "values-yo",
    "ig": "values-ig",
    "om": "values-om",
    "ps": "values-ps",
    "yue": "values-b+yue",
    "pcm": "values-b+pcm",
}


def to_snake(name: str) -> str:
    s1 = re.sub(r"(.)([A-Z][a-z]+)", r"\1_\2", name)
    return re.sub(r"([a-z0-9])([A-Z])", r"\1_\2", s1).lower()


def android_escape(text: str) -> str:
    text = re.sub(r"\{(\d+)\}", lambda m: f"%{int(m.group(1)) + 1}$s", text)
    text = text.replace("\r\n", "\n").replace("\r", "\n").replace("\n", r"\n")
    text = (
        text.replace("&", "&amp;")
        .replace("<", "&lt;")
        .replace(">", "&gt;")
        .replace("'", r"\'")
        .replace('"', r"\"")
    )
    return text


def parse_resx(path: Path) -> dict[str, str]:
    tree = ET.parse(path)
    out: dict[str, str] = {}
    for data in tree.getroot().findall("data"):
        name = data.get("name")
        if not name:
            continue
        value_el = data.find("value")
        if value_el is None or value_el.text is None:
            continue
        out[name] = value_el.text
    return out


def write_strings(folder: Path, items: dict[str, str]) -> None:
    folder.mkdir(parents=True, exist_ok=True)
    lines = ['<?xml version="1.0" encoding="utf-8"?>', "<resources>"]
    for key, value in items.items():
        lines.append(f'    <string name="{to_snake(key)}">{android_escape(value)}</string>')
    lines.append("</resources>")
    (folder / "strings.xml").write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> None:
    for resx in sorted(RESX_DIR.glob("Strings*.resx")):
        name = resx.name
        if name == "Strings.resx":
            locale = ""
        elif name.startswith("Strings.") and name.endswith(".resx"):
            locale = name[len("Strings.") : -len(".resx")]
        else:
            continue
        values_dir = LOCALE_TO_VALUES.get(locale)
        if not values_dir:
            print(f"skip unknown locale {locale}")
            continue
        write_strings(OUT_DIR / values_dir, parse_resx(resx))
        print(f"wrote {values_dir}/strings.xml from {name}")


if __name__ == "__main__":
    main()
