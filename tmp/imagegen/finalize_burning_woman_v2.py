from pathlib import Path
from PIL import Image, ImageDraw, ImageChops

source = Path('tmp/imagegen/trainer_burning_woman_v2_keyed.png')
out = Path('tmp/imagegen/trainer_burning_woman_v2.png')
image = Image.open(source).convert('RGBA')
alpha = image.getchannel('A')
binary = alpha.point(lambda value: 255 if value > 8 else 0)
cx, cy = image.width // 2, image.height // 2
seed = None
for radius in range(0, max(image.width, image.height), 4):
    for x, y in ((cx + radius, cy), (cx - radius, cy), (cx, cy + radius), (cx, cy - radius)):
        if 0 <= x < image.width and 0 <= y < image.height and binary.getpixel((x, y)) == 255:
            seed = (x, y)
            break
    if seed:
        break
if seed is None:
    raise RuntimeError('Could not find foreground seed.')
connected = binary.copy()
ImageDraw.floodfill(connected, seed, 128, thresh=0)
component = connected.point(lambda value: 255 if value == 128 else 0)
image.putalpha(ImageChops.multiply(alpha, component))
bbox = component.getbbox()
subject = image.crop(bbox)
scale = min(1420 / subject.height, 976 / subject.width)
size = (round(subject.width * scale), round(subject.height * scale))
subject = subject.resize(size, Image.Resampling.LANCZOS)
canvas = Image.new('RGBA', (1024, 1536), (0, 0, 0, 0))
x = (1024 - size[0]) // 2
y = 1536 - 24 - size[1]
canvas.alpha_composite(subject, (x, y))
canvas.save(out, optimize=True)
print(f'Wrote {out}: source_bbox={bbox}, placed={size} at {(x, y)}')
