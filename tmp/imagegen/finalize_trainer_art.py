from pathlib import Path
from PIL import Image, ImageFilter

source = Path('tmp/imagegen/trainer_forest_girl_sharp.png')
out = Path('Assets/Art/Trainers/BattleGraphics/trainer_normal_leaf_female_01.png')
out.parent.mkdir(parents=True, exist_ok=True)

image = Image.open(source).convert('RGBA')
rgb = image.convert('RGB')
alpha = image.getchannel('A')
transparent = alpha.point(lambda value: 255 if value == 0 else 0)
edge_ring = transparent.filter(ImageFilter.MaxFilter(5))

pixels = image.load()
ring = edge_ring.load()
for y in range(image.height):
    for x in range(image.width):
        if ring[x, y] == 0:
            continue
        r, g, b, a = pixels[x, y]
        magenta = min(r, b) - g
        if a > 0 and magenta > 6 and abs(r - b) < 80:
            neutral = min(r, g, b)
            pixels[x, y] = (neutral, neutral, neutral, a)

bbox = alpha.getbbox()
if bbox:
    padding = 24
    left = max(0, bbox[0] - padding)
    top = max(0, bbox[1] - padding)
    right = min(image.width, bbox[2] + padding)
    bottom = min(image.height, bbox[3] + padding)
    image = image.crop((left, top, right, bottom))

image.save(out, optimize=True)
print(f'Wrote {out} ({image.width}x{image.height})')
