# Avatar vector lettering

`lettering.json` contains normalized vector outlines and advance widths, embedded
in UI.Application. Avatars use paths rather than SVG text so indexers do not need
installed fonts. Glyphs use a baseline origin, em units, and SVG's downward Y axis.

Source fonts from Google Fonts (SIL Open Font License; full licenses alongside):

- Login: Libre Baskerville variable font, weight 600.
  https://github.com/google/fonts/tree/main/ofl/librebaskerville
- Brand: Montserrat variable font, weight 700.
  https://github.com/google/fonts/tree/main/ofl/montserrat

These subsets are named CryptoStyle Avatar Lettering. Preserve the accompanying
licenses when redistributing them. They are resources, not a runtime font or
native-library dependency.

To regenerate with Python and fonttools 4.60.2, download the source TTFs and run
from the backend repository root:

```sh
python tools/generate_avatar_outlines.py /path/to/serif.ttf /path/to/sans.ttf
```
