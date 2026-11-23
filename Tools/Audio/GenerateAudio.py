import math
import os
import random
import struct
import wave

SR = 44100
ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Assets", "Audio")
SFX_DIR = os.path.join(ROOT, "Sfx")
MUSIC_DIR = os.path.join(ROOT, "Music")

rng = random.Random(2026)


def osc(kind, phase):
    p = phase % 1.0
    if kind == "square":
        return 1.0 if p < 0.5 else -1.0
    if kind == "pulse":
        return 1.0 if p < 0.25 else -1.0
    if kind == "triangle":
        return 4.0 * abs(p - 0.5) - 1.0
    if kind == "saw":
        return 2.0 * p - 1.0
    return math.sin(2 * math.pi * p)


def tone(freq_fn, seconds, kind="square", attack=0.005, release=0.05, vol=0.5, vibrato=0.0):
    n = int(seconds * SR)
    out = [0.0] * n
    phase = 0.0
    for i in range(n):
        t = i / SR
        f = freq_fn(t)
        if vibrato:
            f *= 1.0 + vibrato * math.sin(2 * math.pi * 6 * t)
        phase += f / SR
        env = min(1.0, t / attack) if attack > 0 else 1.0
        tail = seconds - t
        if tail < release:
            env *= max(0.0, tail / release)
        out[i] = osc(kind, phase) * env * vol
    return out


def noise(seconds, vol=0.5, decay=0.1, lowpass=0.5):
    n = int(seconds * SR)
    out = [0.0] * n
    prev = 0.0
    for i in range(n):
        t = i / SR
        s = rng.uniform(-1, 1)
        prev = prev + lowpass * (s - prev)
        out[i] = prev * vol * math.exp(-t / decay)
    return out


def mix(dst, src, at=0.0, g=1.0):
    start = int(at * SR)
    if start + len(src) > len(dst):
        dst.extend([0.0] * (start + len(src) - len(dst)))
    for i, s in enumerate(src):
        dst[start + i] += s * g
    return dst


def normalize(sig, peak=0.85):
    m = max(1e-9, max(abs(s) for s in sig))
    return [s * peak / m for s in sig]


def save(path, sig):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(b"".join(struct.pack("<h", int(max(-1, min(1, s)) * 32767)) for s in sig))
    print("wrote", os.path.relpath(path, ROOT))


def slide(f0, f1, dur, curve=1.0):
    return lambda t: f0 + (f1 - f0) * min(1.0, t / dur) ** curve


def note_hz(n):
    return 440.0 * 2 ** ((n - 69) / 12.0)


def sfx():
    save(os.path.join(SFX_DIR, "Jump.wav"),
         normalize(tone(slide(260, 720, 0.16, 0.6), 0.18, "square", release=0.06, vol=0.4), 0.6))

    save(os.path.join(SFX_DIR, "DoubleJump.wav"),
         normalize(mix(tone(slide(400, 900, 0.12), 0.13, "pulse", vol=0.35),
                       tone(slide(600, 1300, 0.12), 0.14, "pulse", vol=0.3), 0.06), 0.6))

    coin = tone(lambda t: 988, 0.07, "square", release=0.01, vol=0.4)
    mix(coin, tone(lambda t: 1319, 0.28, "square", release=0.25, vol=0.4), 0.07)
    save(os.path.join(SFX_DIR, "Coin.wav"), normalize(coin, 0.55))

    stomp = tone(slide(520, 90, 0.14, 0.5), 0.16, "square", release=0.05, vol=0.5)
    mix(stomp, noise(0.12, 0.6, 0.04, 0.35))
    save(os.path.join(SFX_DIR, "Stomp.wav"), normalize(stomp, 0.75))

    hurt = tone(slide(420, 140, 0.3), 0.32, "saw", release=0.1, vol=0.45, vibrato=0.08)
    mix(hurt, noise(0.15, 0.4, 0.05, 0.6))
    save(os.path.join(SFX_DIR, "Hurt.wav"), normalize(hurt, 0.75))

    boss_hit = tone(slide(180, 50, 0.35), 0.4, "square", release=0.15, vol=0.5)
    mix(boss_hit, noise(0.3, 0.7, 0.09, 0.2))
    mix(boss_hit, tone(slide(900, 300, 0.1), 0.1, "pulse", vol=0.25))
    save(os.path.join(SFX_DIR, "BossHit.wav"), normalize(boss_hit, 0.85))

    boss_down = []
    for k in range(6):
        mix(boss_down, noise(0.35, 0.8, 0.12, 0.25), k * 0.16)
        mix(boss_down, tone(slide(200 - k * 20, 40, 0.3), 0.32, "square", vol=0.35), k * 0.16)
    save(os.path.join(SFX_DIR, "BossDefeated.wav"), normalize(boss_down, 0.85))

    death = []
    for k, n in enumerate([72, 67, 64, 60, 55]):
        mix(death, tone(lambda t, f=note_hz(n): f, 0.16, "triangle", release=0.05, vol=0.5), k * 0.13)
    save(os.path.join(SFX_DIR, "PlayerDeath.wav"), normalize(death, 0.7))

    victory = []
    seq = [(60, 0.12), (64, 0.12), (67, 0.12), (72, 0.24), (67, 0.12), (72, 0.6)]
    t = 0.0
    for n, d in seq:
        mix(victory, tone(lambda _t, f=note_hz(n): f, d + 0.05, "square", release=0.08, vol=0.35), t)
        mix(victory, tone(lambda _t, f=note_hz(n - 12): f, d + 0.05, "triangle", release=0.08, vol=0.4), t)
        t += d
    save(os.path.join(SFX_DIR, "Victory.wav"), normalize(victory, 0.7))

    save(os.path.join(SFX_DIR, "ButtonClick.wav"),
         normalize(tone(slide(900, 600, 0.04), 0.06, "square", release=0.03, vol=0.3), 0.5))
    save(os.path.join(SFX_DIR, "ButtonHover.wav"),
         normalize(tone(lambda t: 1500, 0.03, "triangle", release=0.02, vol=0.3), 0.3))

    portal = []
    for k in range(4):
        mix(portal, tone(slide(300 + k * 150, 1200 + k * 200, 0.5), 0.55, "triangle",
                         release=0.2, vol=0.3, vibrato=0.03), k * 0.05)
    save(os.path.join(SFX_DIR, "Portal.wav"), normalize(portal, 0.6))


def song(bpm, bars, chords, melody, lead="pulse", bass="triangle", drums=True, lead_vol=0.22):
    beat = 60.0 / bpm
    total = bars * 4 * beat
    out = [0.0] * int(total * SR + SR * 0.01)


    for b in range(bars):
        chord = chords[b % len(chords)]
        root = chord[0] - 24
        for e in range(8):
            n = root + (12 if e % 2 else 0)
            mix(out, tone(lambda t, f=note_hz(n): f, beat / 2 * 0.95, bass, release=0.02, vol=0.28),
                (b * 4 + e / 2) * beat)

        for e in range(16):
            n = chord[e % len(chord)]
            mix(out, tone(lambda t, f=note_hz(n): f, beat / 4 * 0.9, "square", release=0.03, vol=0.05),
                (b * 4 + e / 4) * beat)


    t = 0.0
    for n, beats in melody:
        dur = beats * beat
        if n is not None:
            mix(out, tone(lambda _t, f=note_hz(n): f, dur * 0.92, lead, attack=0.01, release=0.05,
                          vol=lead_vol, vibrato=0.006), t)
        t += dur
        if t >= total:
            break


    if drums:
        for b in range(bars * 4):
            at = b * beat
            kick = tone(slide(140, 45, 0.08), 0.12, "sine", release=0.04, vol=0.5)
            mix(out, kick, at)
            if b % 2 == 1:
                mix(out, noise(0.12, 0.35, 0.05, 0.6), at)
            for h in (0.0, 0.5):
                mix(out, noise(0.03, 0.12, 0.01, 0.95), at + h * beat)


    out = out[:int(total * SR)]
    fade = int(0.02 * SR)
    for i in range(fade):
        out[i] *= i / fade
        out[-1 - i] *= i / fade
    return normalize(out, 0.6)


def music():
    chords = [(60, 64, 67), (57, 60, 64), (53, 57, 60), (55, 59, 62)]
    mel = [(72, 1), (74, 0.5), (76, 1.5), (79, 1), (76, 2), (74, 1), (72, 1),
           (71, 1), (72, 0.5), (74, 1.5), (67, 1), (69, 2), (71, 2)]
    mel = mel * 2
    save(os.path.join(MUSIC_DIR, "MenuTheme.wav"), song(100, 8, chords, mel, lead="triangle", lead_vol=0.3))


    chords = [(67, 71, 74), (64, 67, 71), (60, 64, 67), (62, 66, 69)]
    mel = [(79, 0.5), (79, 0.5), (81, 0.5), (83, 1), (81, 0.5), (79, 1),
           (76, 0.5), (79, 0.5), (83, 1), (86, 1), (84, 0.5), (83, 0.5), (81, 1),
           (79, 0.5), (76, 0.5), (72, 1), (74, 0.5), (76, 0.5), (79, 1),
           (78, 0.5), (76, 0.5), (74, 1), (78, 1), (79, 2)]
    mel = mel * 2
    save(os.path.join(MUSIC_DIR, "LevelTheme.wav"), song(132, 8, chords, mel))


    chords = [(57, 60, 64), (53, 57, 60), (55, 58, 62), (52, 56, 59)]
    mel = [(69, 0.5), (72, 0.5), (76, 0.5), (75, 0.5), (76, 1), (None, 0.5), (69, 0.5),
           (77, 0.5), (76, 0.5), (74, 0.5), (72, 0.5), (74, 1), (None, 1),
           (67, 0.5), (70, 0.5), (74, 0.5), (73, 0.5), (74, 1), (None, 0.5), (67, 0.5),
           (68, 0.5), (71, 0.5), (74, 0.5), (76, 0.5), (80, 2)]
    mel = mel * 2
    save(os.path.join(MUSIC_DIR, "BossTheme.wav"), song(150, 8, chords, mel, lead="saw", bass="square", lead_vol=0.16))


if __name__ == "__main__":
    sfx()
    music()
