"""Generate embedded vector lettering. Requires fonttools; see avatar Assets/README.md."""
import json
import sys
from pathlib import Path
from fontTools.ttLib import TTFont
from fontTools.varLib.instancer import instantiateVariableFont
from fontTools.pens.svgPathPen import SVGPathPen
from fontTools.pens.transformPen import TransformPen


def outlines(path, weight, characters):
    font = TTFont(path)
    if 'fvar' in font:
        font = instantiateVariableFont(font, {'wght': weight})
    scale = 1 / font['head'].unitsPerEm
    glyphs = font.getGlyphSet()
    cmap = font.getBestCmap()
    result = {}
    for char in characters:
        name = cmap[ord(char)]
        pen = SVGPathPen(glyphs, ntos=lambda n: format(n, '.5f').rstrip('0').rstrip('.'))
        glyphs[name].draw(TransformPen(pen, (scale, 0, 0, -scale, 0, 0)))
        result[char] = {'Path': pen.getCommands(), 'Advance': round(glyphs[name].width * scale, 5)}
    return result


output = Path(__file__).resolve().parents[1] / 'src/Modules/UI/UI.Application/Features/Avatars/Assets'
(output / 'lettering.json').write_text(json.dumps({
    'Login': outlines(sys.argv[1], 600, 'abcdefghijklmnopqrstuvwxyz0123456789-'),
    'Brand': outlines(sys.argv[2], 700, 'CRYPTO SLE'),
}, separators=(',', ':')) + '\n')
