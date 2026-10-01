"""Tile a set of textures into one preview image for review."""
import os
import sys

from PIL import Image, ImageDraw

from texlib import OUT


def sheet(names, out_path, cell=384, cols=3):
    rows = (len(names) + cols - 1) // cols
    img = Image.new("RGB", (cols * cell, rows * (cell + 18)), (60, 60, 60))
    d = ImageDraw.Draw(img)
    for i, n in enumerate(names):
        p = os.path.join(OUT, n + ".png")
        t = Image.open(p).convert("RGBA")
        t.thumbnail((cell, cell))
        bg = Image.new("RGBA", t.size, (255, 0, 255, 255))
        bg.alpha_composite(t)
        x, y = (i % cols) * cell, (i // cols) * (cell + 18)
        img.paste(bg.convert("RGB"), (x, y + 18))
        d.text((x + 4, y + 3), n, fill=(255, 255, 255))
    img.save(out_path)
    return out_path


if __name__ == "__main__":
    out = sys.argv[1]
    print(sheet(sys.argv[2:], out))
