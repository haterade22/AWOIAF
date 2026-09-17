"""Shared builder for synthetic Bannerlord material metadata (TpacTool Material.cs layout)."""
import struct
import uuid


def sized(s: str) -> bytes:
    b = s.encode("utf-8")
    return struct.pack("<i", len(b)) + b


def material_meta(tex, flags=("two_sided",), smf=("use_specular",)) -> bytes:
    m = struct.pack("<I", 0) + b"\x00" * 16 + struct.pack("<II", 2, 0)
    m += struct.pack("<I", len(flags)) + b"".join(sized(f) for f in flags)
    m += struct.pack("<I", 0) + struct.pack("<I", 1) + sized("bumpmap") + sized("no_alpha_blend")
    m += uuid.UUID("328d3572-5e9e-4183-b49e-f451a19213d0").bytes_le
    m += struct.pack("<i", len(tex))
    for slot, g in tex:
        m += struct.pack("<i", slot) + g
    m += struct.pack("<f", 0.0)
    m += struct.pack("<I", len(smf)) + b"".join(sized(f) for f in smf)
    m += struct.pack("<15f", 1, 0.65, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0) + struct.pack("<i", 0)
    m += struct.pack("<6f", 0, 1, 1, 0, 0.5, 1) + struct.pack("<f", 1)
    return m
