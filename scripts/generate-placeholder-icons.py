#!/usr/bin/env python3
"""Generate 32x32 placeholder class icons for paradow-sync (stdlib only)."""

from __future__ import annotations

import struct
import zlib
from pathlib import Path

# (filename stem, hex color, abbreviation)
CLASSES: list[tuple[str, str, str]] = [
    ("feca", "#1565C0", "Fe"),
    ("osamodas", "#6A1B9A", "Os"),
    ("enutrof", "#F9A825", "Eu"),
    ("sram", "#4A148C", "Sr"),
    ("xelor", "#00838F", "Xe"),
    ("ecaflip", "#AD1457", "Ec"),
    ("eniripsa", "#2E7D32", "Ei"),
    ("iop", "#C62828", "Io"),
    ("cra", "#33691E", "Cr"),
    ("sadida", "#558B2F", "Sd"),
    ("sacrieur", "#B71C1C", "Su"),
    ("pandawa", "#0277BD", "Pa"),
    ("roublard", "#4E342E", "Ro"),
    ("zobal", "#880E4F", "Zo"),
    ("steamer", "#455A64", "St"),
    ("eliotrope", "#283593", "El"),
    ("huppermage", "#E65100", "Hu"),
    ("ouginak", "#BF360C", "Ou"),
    ("forgelance", "#37474F", "Fl"),
]

SIZE = 32

# 5x7 bitmap font for A-Z and digits (uppercase glyphs only).
FONT: dict[str, list[str]] = {
    "A": ["01110", "10001", "10001", "11111", "10001", "10001", "10001"],
    "B": ["11110", "10001", "10001", "11110", "10001", "10001", "11110"],
    "C": ["01111", "10000", "10000", "10000", "10000", "10000", "01111"],
    "D": ["11110", "10001", "10001", "10001", "10001", "10001", "11110"],
    "E": ["11111", "10000", "10000", "11110", "10000", "10000", "11111"],
    "F": ["11111", "10000", "10000", "11110", "10000", "10000", "10000"],
    "G": ["01111", "10000", "10000", "10111", "10001", "10001", "01111"],
    "H": ["10001", "10001", "10001", "11111", "10001", "10001", "10001"],
    "I": ["01110", "00100", "00100", "00100", "00100", "00100", "01110"],
    "L": ["10000", "10000", "10000", "10000", "10000", "10000", "11111"],
    "M": ["10001", "11011", "10101", "10101", "10001", "10001", "10001"],
    "N": ["10001", "11001", "10101", "10011", "10001", "10001", "10001"],
    "O": ["01110", "10001", "10001", "10001", "10001", "10001", "01110"],
    "P": ["11110", "10001", "10001", "11110", "10000", "10000", "10000"],
    "R": ["11110", "10001", "10001", "11110", "10100", "10010", "10001"],
    "S": ["01111", "10000", "10000", "01110", "00001", "00001", "11110"],
    "T": ["11111", "00100", "00100", "00100", "00100", "00100", "00100"],
    "U": ["10001", "10001", "10001", "10001", "10001", "10001", "01110"],
    "W": ["10001", "10001", "10001", "10101", "10101", "11011", "10001"],
    "X": ["10001", "10001", "01010", "00100", "01010", "10001", "10001"],
    "Z": ["11111", "00001", "00010", "00100", "01000", "10000", "11111"],
}


def hex_to_rgb(hex_color: str) -> tuple[int, int, int]:
    hex_color = hex_color.lstrip("#")
    return tuple(int(hex_color[i : i + 2], 16) for i in (0, 2, 4))


def write_png(path: Path, pixels: list[tuple[int, int, int]]) -> None:
    width = height = SIZE
    raw = bytearray()
    for y in range(height):
        raw.append(0)
        for x in range(width):
            raw.extend(pixels[y * width + x])

    compressed = zlib.compress(bytes(raw), level=9)

    def chunk(tag: bytes, data: bytes) -> bytes:
        return struct.pack(">I", len(data)) + tag + data + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)

    ihdr = struct.pack(">IIBBBBB", width, height, 8, 2, 0, 0, 0)
    png = b"\x89PNG\r\n\x1a\n"
    png += chunk(b"IHDR", ihdr)
    png += chunk(b"IDAT", compressed)
    png += chunk(b"IEND", b"")
    path.write_bytes(png)


def render_icon(abbrev: str, color_hex: str) -> list[tuple[int, int, int]]:
    bg = hex_to_rgb(color_hex)
    fg = (255, 255, 255)
    pixels = [bg] * (SIZE * SIZE)

    glyph_w, glyph_h = 5, 7
    spacing = 1
    text = abbrev.upper()
    total_w = len(text) * glyph_w + (len(text) - 1) * spacing
    start_x = (SIZE - total_w) // 2
    start_y = (SIZE - glyph_h) // 2

    for index, char in enumerate(text):
        glyph = FONT.get(char)
        if glyph is None:
            continue
        offset_x = start_x + index * (glyph_w + spacing)
        for row, line in enumerate(glyph):
            for col, bit in enumerate(line):
                if bit != "1":
                    continue
                x = offset_x + col
                y = start_y + row
                if 0 <= x < SIZE and 0 <= y < SIZE:
                    pixels[y * SIZE + x] = fg

    return pixels


def main() -> None:
    repo_root = Path(__file__).resolve().parent.parent
    output_dir = repo_root / "assets" / "icons"
    output_dir.mkdir(parents=True, exist_ok=True)

    for stem, color, abbrev in CLASSES:
        pixels = render_icon(abbrev, color)
        write_png(output_dir / f"{stem}.png", pixels)
        print(f"Wrote {stem}.png")

    print(f"Generated {len(CLASSES)} icons in {output_dir}")


if __name__ == "__main__":
    main()
