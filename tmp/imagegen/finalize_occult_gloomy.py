from pathlib import Path
from PIL import Image
source = Path('tmp/imagegen/trainer_occult_club_gloomy_keyed.png')
out = Path('tmp/imagegen/trainer_occult_club_gloomy.png')
image = Image.open(source).convert('RGBA')
alpha = image.getchannel('A')
bbox = alpha.getbbox()
if bbox:
    pad = 24
    image = image.crop((max(0, bbox[0]-pad), max(0, bbox[1]-pad), min(image.width, bbox[2]+pad), min(image.height, bbox[3]+pad)))
image.save(out, optimize=True)
print(f'Wrote {out} ({image.width}x{image.height})')
