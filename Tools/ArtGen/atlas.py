"""Atlas layouts shared by the texture scripts (which draw into these rectangles)
and the Blender scripts (which map UVs onto them).

Rectangles are in texture pixels, (x0, y0, x1, y1) with y pointing down, the way
PIL draws. `region()` converts one to Blender/Unity UVs (u0, v0, u1, v1), v up.
"""

SIZES = {
    "notes": (1024, 1024),
    "photos": (1024, 1024),
    "labels": (1024, 1024),
    "posters": (2048, 1024),
    "walls": (2048, 2048),
}

NOTES = {
    "numbers": (0, 0, 512, 512),        # important numbers, lined paper
    "notice": (512, 0, 1024, 512),      # building management notice
    "sticky_y": (0, 512, 256, 768),
    "sticky_p": (256, 512, 512, 768),
    "ticket": (0, 768, 512, 1024),      # concert ticket stub
    "strip": (512, 512, 768, 1024),     # photo-booth strip
    "receipt": (768, 512, 1024, 1024),  # convenience-store receipt
}

PHOTOS = {
    "graduation": (0, 0, 512, 512),
    "wedding": (512, 0, 1024, 512),     # the parents' wedding, 1990s
    "family": (0, 512, 512, 1024),
    "id": (576, 512, 960, 1024),        # 3:4 ID photo in the middle of its quadrant
}

LABELS = {
    "ramyun": (0, 0, 512, 512),         # wraps around the cup
    "laptop": (512, 0, 1024, 256),      # lock screen
    "thermostat": (512, 256, 1024, 512),
    "remote": (0, 512, 256, 768),
    "card_front": (264, 560, 504, 712),
    "card_back": (520, 560, 760, 712),
    "rulebook": (800, 598, 992, 698),   # label on the cover
    "tissue": (0, 768, 512, 1024),
    "aircon": (512, 768, 1024, 1024),
}

POSTERS = {
    "concert": (0, 0, 512, 1024),
    "movie": (512, 0, 1024, 1024),
    "jeju": (1024, 0, 1536, 1024),
    "timetable": (1536, 0, 2048, 880),
}

# Painted walls: one band per wall, left to right as seen from inside the room,
# floor at the bottom of each band.
WALLS = {
    "west": (0, 0, 2048, 512),
    "north": (0, 512, 2048, 1024),
    "east": (0, 1024, 2048, 1536),
    "south": (0, 1536, 2048, 2048),
}

ATLASES = {"notes": NOTES, "photos": PHOTOS, "labels": LABELS, "posters": POSTERS, "walls": WALLS}


def region(atlas, key):
    """UV rectangle (u0, v0, u1, v1) of an atlas entry."""
    w, h = SIZES[atlas]
    x0, y0, x1, y1 = ATLASES[atlas][key]
    return (x0 / w, 1.0 - y1 / h, x1 / w, 1.0 - y0 / h)


def aspect(atlas, key):
    """Width / height of an atlas entry, for sizing the object it goes on."""
    x0, y0, x1, y1 = ATLASES[atlas][key]
    return (x1 - x0) / (y1 - y0)
