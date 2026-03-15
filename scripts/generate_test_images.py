#!/usr/bin/env python3
"""Generate large, colorful bitmap samples for repository roundtrip tests."""

from __future__ import annotations

from pathlib import Path

from PIL import Image


def build_colorful_rgb(width: int, height: int, seed: int) -> Image.Image:
    data = bytearray(width * height * 3)
    i = 0
    for y in range(height):
        y1 = (y + seed * 31) & 0xFF
        y2 = (y * 3 + seed * 17) & 0xFF
        for x in range(width):
            # Deterministic high-entropy pattern with many unique colors.
            r = (x * 7 + y1) & 0xFF
            g = (x * 3 + y * 5 + seed * 13) & 0xFF
            b = (x ^ y2 ^ ((x * y) >> 3) ^ (seed * 29)) & 0xFF
            data[i] = r
            data[i + 1] = g
            data[i + 2] = b
            i += 3
    return Image.frombytes("RGB", (width, height), bytes(data))


def build_1bit(width: int, height: int) -> Image.Image:
    image = Image.new("1", (width, height), 0)
    pixels = image.load()
    for y in range(height):
        for x in range(width):
            pixels[x, y] = 255 if ((x // 8) ^ (y // 8)) & 1 else 0
    return image


def build_palette(width: int, height: int, color_count: int) -> Image.Image:
    image = Image.new("P", (width, height))
    palette = []
    for i in range(256):
        if i < color_count:
            palette.extend([(i * 53) % 256, (i * 97) % 256, (i * 193) % 256])
        else:
            palette.extend([0, 0, 0])
    image.putpalette(palette)
    pixels = image.load()
    for y in range(height):
        for x in range(width):
            pixels[x, y] = (x * 7 + y * 11) % color_count
    return image


def build_grayscale_8bit(width: int, height: int) -> Image.Image:
    image = Image.new("L", (width, height))
    pixels = image.load()
    for y in range(height):
        for x in range(width):
            pixels[x, y] = (x * 5 + y * 3) & 0xFF
    return image


def build_grayscale_16bit(width: int, height: int) -> Image.Image:
    image = Image.new("I;16", (width, height))
    pixels = image.load()
    for y in range(height):
        for x in range(width):
            pixels[x, y] = (x * 257 + y * 389) & 0xFFFF
    return image


def build_rgba(width: int, height: int, seed: int) -> Image.Image:
    image = Image.new("RGBA", (width, height))
    pixels = image.load()
    for y in range(height):
        for x in range(width):
            r = (x * 11 + y * 17 + seed * 23) & 0xFF
            g = (x * 5 + y * 13 + seed * 29) & 0xFF
            b = (x * 19 + y * 7 + seed * 31) & 0xFF
            a = (x ^ y ^ (seed * 41)) & 0xFF
            pixels[x, y] = (r, g, b, a)
    return image


def save_image(target: Path, image: Image.Image, png_bits: int | None = None) -> None:
    suffix = target.suffix.lower()
    if suffix in {".jpg", ".jpeg"}:
        image.convert("RGB").save(target, format="JPEG", quality=95, optimize=True)
        return

    if suffix == ".bmp":
        image.save(target, format="BMP")
        return

    if suffix == ".png":
        save_kwargs = {"format": "PNG", "optimize": True}
        if png_bits is not None:
            save_kwargs["bits"] = png_bits
        image.save(target, **save_kwargs)
        return

    raise ValueError(f"Unsupported output format: {target}")


def main() -> None:
    repo_root = Path(__file__).resolve().parents[1]

    outputs = [
        ("image1.jpg", build_colorful_rgb(2400, 1600, 1), None),
        ("image2.png", build_colorful_rgb(2048, 2048, 2), None),
        ("image3.jpg", build_colorful_rgb(3200, 1800, 3), None),
        ("image_depth_1bit.png", build_1bit(1024, 1024), None),
        ("image_depth_4bit_palette.png", build_palette(1536, 1024, 16), 4),
        ("image_depth_8bit_palette.png", build_palette(1536, 1024, 256), None),
        ("image_depth_8bit_gray.png", build_grayscale_8bit(2048, 1536), None),
        ("image_depth_16bit_gray.png", build_grayscale_16bit(1400, 1000), None),
        ("image_depth_24bit.bmp", build_colorful_rgb(1800, 1200, 5), None),
        ("image_depth_32bit_rgba.png", build_rgba(1920, 1080, 7), None),
    ]

    for file_name, image, png_bits in outputs:
        target = repo_root / file_name
        save_image(target, image, png_bits)
        width, height = image.size
        print(f"wrote {target} ({width}x{height})")


if __name__ == "__main__":
    main()

