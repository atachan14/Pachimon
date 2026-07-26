from pathlib import Path
from PIL import Image

CANVAS = (1024, 1536)
TARGET_HEIGHT = 1420
MAX_WIDTH = 976
BOTTOM_MARGIN = 24
ALPHA_THRESHOLD = 8

paths = [
    Path('Assets/Art/Trainers/BattleGraphics/trainer_normal_leaf_female_01.png'),
    Path('Assets/Art/Trainers/BattleGraphics/trainer_normal_electric_female_02.png'),
    Path('tmp/imagegen/trainer_occult_club_gloomy.png'),
]

for path in paths:
    image = Image.open(path).convert('RGBA')
    alpha = image.getchannel('A')
    detection_alpha = alpha.point(lambda value: 255 if value > ALPHA_THRESHOLD else 0)
    bbox = detection_alpha.getbbox()
    if bbox is None:
        raise RuntimeError(f'No visible pixels: {path}')

    subject = image.crop(bbox)
    scale = min(TARGET_HEIGHT / subject.height, MAX_WIDTH / subject.width)
    size = (
        max(1, round(subject.width * scale)),
        max(1, round(subject.height * scale)),
    )
    subject = subject.resize(size, Image.Resampling.LANCZOS)

    canvas = Image.new('RGBA', CANVAS, (0, 0, 0, 0))
    x = (CANVAS[0] - size[0]) // 2
    y = CANVAS[1] - BOTTOM_MARGIN - size[1]
    canvas.alpha_composite(subject, (x, y))
    canvas.save(path, optimize=True)
    print(f'{path}: source_bbox={bbox}, placed={size[0]}x{size[1]} at ({x},{y})')
