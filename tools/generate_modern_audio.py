import os
import math
import struct
import random
import wave

AUDIO_DIR = os.path.join(os.path.dirname(__file__), '..', 'unity-3d', 'Assets', 'Resources', 'Audio')

def save_wav(filename, samples, sample_rate=44100, channels=1):
    path = os.path.join(AUDIO_DIR, filename)
    if channels == 1:
        peak = max(abs(s) for s in samples) if samples else 1.0
    else:
        peak = max(max(abs(l), abs(r)) for l, r in samples) if samples else 1.0
    gain = 0.95 / peak if peak > 0.001 else 1.0
    
    with wave.open(path, 'wb') as w:
        w.setnchannels(channels)
        w.setsampwidth(2)
        w.setframerate(sample_rate)
        frames = bytearray()
        if channels == 1:
            for s in samples:
                val = int(max(-1.0, min(1.0, s * gain)) * 32767)
                frames.extend(struct.pack('<h', val))
        else:
            for l, r in samples:
                val_l = int(max(-1.0, min(1.0, l * gain)) * 32767)
                val_r = int(max(-1.0, min(1.0, r * gain)) * 32767)
                frames.extend(struct.pack('<hh', val_l, val_r))
        w.writeframes(frames)
    print(f'Generated {filename}: {len(samples)/sample_rate:.2f}s ({channels}ch, {sample_rate}Hz)')

class PinkNoise:
    def __init__(self):
        self.b0 = self.b1 = self.b2 = self.b3 = self.b4 = self.b5 = self.b6 = 0.0
    def sample(self):
        white = random.uniform(-1, 1)
        self.b0 = 0.99886 * self.b0 + white * 0.0555179
        self.b1 = 0.99332 * self.b1 + white * 0.0750759
        self.b2 = 0.96900 * self.b2 + white * 0.1538520
        self.b3 = 0.86650 * self.b3 + white * 0.3104856
        self.b4 = 0.55000 * self.b4 + white * 0.5329522
        self.b5 = -0.7616 * self.b5 - white * 0.0168980
        pink = self.b0 + self.b1 + self.b2 + self.b3 + self.b4 + self.b5 + self.b6 + white * 0.5362
        self.b6 = white * 0.115926
        return pink * 0.25

class LowPass:
    def __init__(self, cutoff, sr=44100):
        rc = 1.0 / (2.0 * math.pi * cutoff)
        dt = 1.0 / sr
        self.alpha = dt / (rc + dt)
        self.prev = 0.0
    def process(self, x):
        self.prev = self.prev + self.alpha * (x - self.prev)
        return self.prev

class BandPass:
    def __init__(self, center_freq, q=2.0, sr=44100):
        w0 = 2.0 * math.pi * center_freq / sr
        alpha = math.sin(w0) / (2.0 * q)
        b0 = alpha
        b1 = 0.0
        b2 = -alpha
        a0 = 1.0 + alpha
        a1 = -2.0 * math.cos(w0)
        a2 = 1.0 - alpha
        self.b0 = b0 / a0
        self.b1 = b1 / a0
        self.b2 = b2 / a0
        self.a1 = a1 / a0
        self.a2 = a2 / a0
        self.x1 = self.x2 = self.y1 = self.y2 = 0.0
    def process(self, x):
        y = self.b0 * x + self.b1 * self.x1 + self.b2 * self.x2 - self.a1 * self.y1 - self.a2 * self.y2
        self.x2 = self.x1
        self.x1 = x
        self.y2 = self.y1
        self.y1 = y
        return y

def generate_blade():
    sr = 44100
    dur = 0.24
    n = int(sr * dur)
    pn = PinkNoise()
    lp = LowPass(1200, sr)
    samples = []
    for i in range(n):
        t = i / sr
        env = math.sin(math.pi * (t / dur)) ** 1.8
        noise = lp.process(pn.sample()) * env * 2.5
        sw_freq = 750.0 * math.exp(-7.0 * t) + 180.0
        swoosh = math.sin(2 * math.pi * sw_freq * t) * env * 0.4
        ring = (math.sin(2 * math.pi * 3100 * t) + 0.5 * math.sin(2 * math.pi * 4400 * t)) * math.exp(-28.0 * t) * 0.28
        samples.append(noise + swoosh + ring)
    return samples

def generate_impact():
    sr = 44100
    dur = 0.26
    n = int(sr * dur)
    bp = BandPass(650, 1.8, sr)
    samples = []
    for i in range(n):
        t = i / sr
        punch_freq = 95.0 * math.exp(-18.0 * t) + 38.0
        punch = math.sin(2 * math.pi * punch_freq * t) * math.exp(-16.0 * t) * 0.85
        crunch = bp.process(random.uniform(-1, 1)) * math.exp(-22.0 * t) * 1.2
        edge = math.sin(2 * math.pi * 2400 * t) * math.exp(-45.0 * t) * 0.35
        samples.append(punch + crunch + edge)
    return samples

def generate_hurt():
    sr = 44100
    dur = 0.30
    n = int(sr * dur)
    bp = BandPass(320, 1.5, sr)
    samples = []
    for i in range(n):
        t = i / sr
        thump = math.sin(2 * math.pi * (80 * math.exp(-12 * t) + 35) * t) * math.exp(-10 * t) * 0.9
        armor = bp.process(random.uniform(-1, 1)) * math.exp(-18 * t) * 0.7
        groan = math.sin(2 * math.pi * 140 * t) * math.exp(-8 * t) * 0.3
        samples.append(thump + armor + groan)
    return samples

def generate_jump():
    sr = 44100
    dur = 0.22
    n = int(sr * dur)
    pn = PinkNoise()
    lp = LowPass(800, sr)
    samples = []
    for i in range(n):
        t = i / sr
        env = math.sin(math.pi * (t / dur)) ** 1.5
        whoosh = lp.process(pn.sample()) * env * 1.5
        rise = math.sin(2 * math.pi * (220 + 260 * (t / dur)) * t) * env * 0.35
        samples.append(whoosh + rise)
    return samples

def generate_double_jump():
    sr = 44100
    dur = 0.35
    n = int(sr * dur)
    pn = PinkNoise()
    lp = LowPass(1100, sr)
    samples = []
    for i in range(n):
        t = i / sr
        env = math.sin(math.pi * (t / dur)) ** 1.4
        air = lp.process(pn.sample()) * env * 1.2
        chime1 = math.sin(2 * math.pi * 659.25 * t) * math.exp(-9.0 * t) * 0.35
        chime2 = math.sin(2 * math.pi * 987.77 * t) * math.exp(-11.0 * t) * 0.25
        chime3 = math.sin(2 * math.pi * 1318.5 * t) * math.exp(-14.0 * t) * 0.20
        samples.append(air + chime1 + chime2 + chime3)
    return samples

def generate_land_soft():
    sr = 44100
    dur = 0.18
    n = int(sr * dur)
    bp = BandPass(280, 1.4, sr)
    samples = []
    for i in range(n):
        t = i / sr
        thump = math.sin(2 * math.pi * (110 * math.exp(-22 * t) + 45) * t) * math.exp(-18 * t) * 0.75
        rustle = bp.process(random.uniform(-1, 1)) * math.exp(-25 * t) * 0.45
        samples.append(thump + rustle)
    return samples

def generate_land_hard():
    sr = 44100
    dur = 0.28
    n = int(sr * dur)
    bp = BandPass(400, 1.6, sr)
    samples = []
    for i in range(n):
        t = i / sr
        sub = math.sin(2 * math.pi * (75 * math.exp(-14 * t) + 30) * t) * math.exp(-10 * t) * 0.95
        crunch = bp.process(random.uniform(-1, 1)) * math.exp(-16 * t) * 0.75
        samples.append(sub + crunch)
    return samples

def generate_step():
    sr = 44100
    dur = 0.10
    n = int(sr * dur)
    bp = BandPass(380, 2.0, sr)
    samples = []
    for i in range(n):
        t = i / sr
        thump = math.sin(2 * math.pi * (130 * math.exp(-35 * t) + 50) * t) * math.exp(-30 * t) * 0.65
        click = bp.process(random.uniform(-1, 1)) * math.exp(-40 * t) * 0.4
        samples.append(thump + click)
    return samples

def generate_player_dash():
    sr = 44100
    dur = 0.28
    n = int(sr * dur)
    pn = PinkNoise()
    lp = LowPass(1400, sr)
    samples = []
    for i in range(n):
        t = i / sr
        env = math.sin(math.pi * (t / dur)) ** 1.6
        whoosh = lp.process(pn.sample()) * env * 2.2
        pitch_sweep = math.sin(2 * math.pi * (800 * math.exp(-8 * t) + 160) * t) * env * 0.4
        samples.append(whoosh + pitch_sweep)
    return samples

def generate_coin():
    sr = 44100
    dur = 0.35
    n = int(sr * dur)
    samples = []
    for i in range(n):
        t = i / sr
        c1 = math.sin(2 * math.pi * 2637.0 * t) * math.exp(-12.0 * t) * 0.45
        c2 = math.sin(2 * math.pi * 3951.0 * t) * math.exp(-16.0 * t) * 0.30
        c3 = math.sin(2 * math.pi * 5274.0 * t) * math.exp(-24.0 * t) * 0.15
        tap = math.sin(2 * math.pi * 1400.0 * t) * math.exp(-50.0 * t) * 0.25
        samples.append(c1 + c2 + c3 + tap)
    return samples

def generate_gem():
    sr = 44100
    dur = 0.55
    n = int(sr * dur)
    samples = []
    notes = [1046.5, 1318.5, 1567.98, 2093.0]
    for i in range(n):
        t = i / sr
        val = 0.0
        for idx, freq in enumerate(notes):
            delay = idx * 0.025
            if t >= delay:
                dt = t - delay
                val += math.sin(2 * math.pi * freq * dt) * math.exp(-6.5 * dt) * 0.25
                val += math.sin(2 * math.pi * freq * 2 * dt) * math.exp(-12.0 * dt) * 0.08
        samples.append(val)
    return samples

def generate_heal():
    sr = 44100
    dur = 0.55
    n = int(sr * dur)
    samples = []
    chord = [349.23, 440.0, 523.25, 698.46, 880.0]
    for i in range(n):
        t = i / sr
        val = 0.0
        for idx, freq in enumerate(chord):
            delay = idx * 0.03
            if t >= delay:
                dt = t - delay
                val += math.sin(2 * math.pi * freq * dt) * math.exp(-5.0 * dt) * 0.22
        val += math.sin(2 * math.pi * 174.61 * t) * math.exp(-4.0 * t) * 0.25
        samples.append(val)
    return samples

def generate_checkpoint():
    sr = 44100
    dur = 0.85
    n = int(sr * dur)
    samples = []
    overtones = [(220.0, 0.45, 3.5), (440.0, 0.30, 4.5), (659.25, 0.25, 5.5), (880.0, 0.18, 6.5), (1320.0, 0.10, 8.0)]
    for i in range(n):
        t = i / sr
        val = 0.0
        for freq, amp, decay in overtones:
            val += math.sin(2 * math.pi * freq * t) * math.exp(-decay * t) * amp
        strike = math.sin(2 * math.pi * 120 * t) * math.exp(-25 * t) * 0.3
        samples.append(val + strike)
    return samples

def generate_upgrade():
    sr = 44100
    dur = 0.65
    n = int(sr * dur)
    samples = []
    arpeggio = [(261.63, 0.0), (329.63, 0.10), (392.0, 0.20), (523.25, 0.30)]
    for i in range(n):
        t = i / sr
        val = 0.0
        for freq, start in arpeggio:
            if t >= start:
                dt = t - start
                val += math.sin(2 * math.pi * freq * dt) * math.exp(-4.5 * dt) * 0.28
                val += math.sin(2 * math.pi * freq * 2 * dt) * math.exp(-7.0 * dt) * 0.10
        samples.append(val)
    return samples

def generate_power_select():
    sr = 44100
    dur = 0.16
    n = int(sr * dur)
    samples = []
    for i in range(n):
        t = i / sr
        click = math.sin(2 * math.pi * 1800 * t) * math.exp(-45 * t) * 0.5
        chime = math.sin(2 * math.pi * 880 * t) * math.exp(-18 * t) * 0.4
        samples.append(click + chime)
    return samples

def generate_power_fail():
    sr = 44100
    dur = 0.24
    n = int(sr * dur)
    samples = []
    for i in range(n):
        t = i / sr
        tone = (math.sin(2 * math.pi * 160 * t) + 0.5 * math.sin(2 * math.pi * 115 * t)) * math.exp(-20 * t) * 0.6
        samples.append(tone)
    return samples

def generate_menu():
    sr = 44100
    dur = 0.08
    n = int(sr * dur)
    samples = []
    for i in range(n):
        t = i / sr
        click = math.sin(2 * math.pi * (1400 * math.exp(-60 * t) + 300) * t) * math.exp(-45 * t) * 0.7
        samples.append(click)
    return samples

def generate_complete():
    sr = 44100
    dur = 1.30
    n = int(sr * dur)
    samples = []
    notes = [261.63, 329.63, 392.0, 523.25, 659.25, 783.99, 1046.5]
    for i in range(n):
        t = i / sr
        val = 0.0
        for idx, f in enumerate(notes):
            delay = min(0.15, idx * 0.02)
            if t >= delay:
                dt = t - delay
                val += math.sin(2 * math.pi * f * dt) * math.exp(-2.5 * dt) * 0.16
        val += math.sin(2 * math.pi * 130.81 * t) * math.exp(-2.0 * t) * 0.30
        samples.append(val)
    return samples

def generate_defeat():
    sr = 44100
    dur = 1.20
    n = int(sr * dur)
    samples = []
    for i in range(n):
        t = i / sr
        f = 65.41 * math.exp(-0.8 * t) + 40.0
        drone = math.sin(2 * math.pi * f * t) * math.exp(-2.0 * t) * 0.7
        minor = math.sin(2 * math.pi * (f * 1.189) * t) * math.exp(-2.5 * t) * 0.35
        samples.append(drone + minor)
    return samples

def generate_respawn():
    sr = 44100
    dur = 0.95
    n = int(sr * dur)
    samples = []
    chords = [220.0, 277.18, 329.63, 440.0, 554.37, 659.25]
    for i in range(n):
        t = i / sr
        val = 0.0
        for idx, f in enumerate(chords):
            delay = idx * 0.05
            if t >= delay:
                dt = t - delay
                val += math.sin(2 * math.pi * f * dt) * math.exp(-3.2 * dt) * 0.18
        sw = math.sin(2 * math.pi * (200 + 400 * (t / dur)) * t) * math.exp(-3.0 * t) * 0.20
        samples.append(val + sw)
    return samples

def generate_enemy_warning():
    sr = 44100
    dur = 0.40
    n = int(sr * dur)
    samples = []
    for i in range(n):
        t = i / sr
        horn = (math.sin(2 * math.pi * 110 * t) + 0.5 * math.sin(2 * math.pi * 165 * t) + 0.3 * math.sin(2 * math.pi * 220 * t)) * math.exp(-5.5 * t) * 0.75
        growl = math.sin(2 * math.pi * 55 * t) * math.exp(-6.0 * t) * 0.5
        samples.append(horn + growl)
    return samples

def generate_enemy_dash():
    sr = 44100
    dur = 0.32
    n = int(sr * dur)
    pn = PinkNoise()
    lp = LowPass(1000, sr)
    samples = []
    for i in range(n):
        t = i / sr
        env = math.sin(math.pi * (t / dur)) ** 1.5
        roar = lp.process(pn.sample()) * env * 2.0
        pounce = math.sin(2 * math.pi * (350 * math.exp(-8 * t) + 120) * t) * env * 0.5
        samples.append(roar + pounce)
    return samples

def generate_enemy_defeat():
    sr = 44100
    dur = 0.45
    n = int(sr * dur)
    pn = PinkNoise()
    lp = LowPass(600, sr)
    samples = []
    for i in range(n):
        t = i / sr
        thud = math.sin(2 * math.pi * (90 * math.exp(-12 * t) + 35) * t) * math.exp(-8 * t) * 0.75
        dissolve = lp.process(pn.sample()) * math.exp(-5 * t) * 0.8
        samples.append(thud + dissolve)
    return samples

def generate_boss_warning():
    sr = 44100
    dur = 0.75
    n = int(sr * dur)
    samples = []
    for i in range(n):
        t = i / sr
        sub = math.sin(2 * math.pi * (50 * math.exp(-3 * t) + 35) * t) * math.exp(-3 * t) * 0.85
        horn = (math.sin(2 * math.pi * 82.41 * t) + 0.6 * math.sin(2 * math.pi * 123.47 * t) + 0.4 * math.sin(2 * math.pi * 164.81 * t)) * math.exp(-3.5 * t) * 0.7
        samples.append(sub + horn)
    return samples

def generate_boss_attack():
    sr = 44100
    dur = 0.55
    n = int(sr * dur)
    bp = BandPass(300, 1.2, sr)
    samples = []
    for i in range(n):
        t = i / sr
        earthquake = math.sin(2 * math.pi * (65 * math.exp(-8 * t) + 28) * t) * math.exp(-5 * t) * 0.95
        crunch = bp.process(random.uniform(-1, 1)) * math.exp(-12 * t) * 0.9
        samples.append(earthquake + crunch)
    return samples

def generate_air_strike():
    sr = 44100
    dur = 0.50
    n = int(sr * dur)
    bp = BandPass(450, 1.4, sr)
    samples = []
    for i in range(n):
        t = i / sr
        whistle = math.sin(2 * math.pi * (1800 * math.exp(-12 * t) + 300) * t) * math.exp(-14 * t) * 0.35
        blast = bp.process(random.uniform(-1, 1)) * math.exp(-9 * t) * 0.85
        thud = math.sin(2 * math.pi * 55 * t) * math.exp(-7 * t) * 0.65
        samples.append(whistle + blast + thud)
    return samples

def generate_ember_cast():
    sr = 44100
    dur = 0.42
    n = int(sr * dur)
    pn = PinkNoise()
    lp = LowPass(1200, sr)
    samples = []
    for i in range(n):
        t = i / sr
        env = math.sin(math.pi * min(1.0, t / dur)) ** 1.3
        roar = lp.process(pn.sample()) * env * 2.2
        flame = math.sin(2 * math.pi * (320 * math.exp(-6 * t) + 90) * t) * env * 0.6
        samples.append(roar + flame)
    return samples

def generate_frost_cast():
    sr = 44100
    dur = 0.45
    n = int(sr * dur)
    samples = []
    for i in range(n):
        t = i / sr
        ice1 = math.sin(2 * math.pi * 2800 * t) * math.exp(-18 * t) * 0.35
        ice2 = math.sin(2 * math.pi * 4200 * t) * math.exp(-25 * t) * 0.25
        wind = (random.uniform(-1, 1) * 0.4) * math.sin(math.pi * (t / dur)) ** 1.6
        samples.append(ice1 + ice2 + wind)
    return samples

def generate_gale_cast():
    sr = 44100
    dur = 0.48
    n = int(sr * dur)
    pn = PinkNoise()
    lp = LowPass(1600, sr)
    samples = []
    for i in range(n):
        t = i / sr
        env = math.sin(math.pi * (t / dur)) ** 1.4
        cyclone = lp.process(pn.sample()) * env * 2.4
        whistle = math.sin(2 * math.pi * (700 + 350 * math.sin(18 * t)) * t) * env * 0.3
        samples.append(cyclone + whistle)
    return samples

def generate_power():
    sr = 44100
    dur = 0.55
    n = int(sr * dur)
    samples = []
    for i in range(n):
        t = i / sr
        implosion = math.sin(2 * math.pi * (180 + 300 * t) * t) * math.exp(-12 * (0.55 - t)) if t < 0.15 else 0.0
        burst_t = max(0.0, t - 0.12)
        thump = math.sin(2 * math.pi * (80 * math.exp(-10 * burst_t) + 35) * burst_t) * math.exp(-6 * burst_t) * 0.8
        shockwave = random.uniform(-1, 1) * math.exp(-10 * burst_t) * 0.5
        samples.append(implosion + thump + shockwave)
    return samples

def generate_emberfall_theme():
    sr = 44100
    bpm = 60.0
    bar_dur = 4.0 * (60.0 / bpm)
    total_bars = 12
    total_dur = bar_dur * total_bars
    n = int(sr * total_dur)
    
    chords_bars = [
        [65.41, 98.0, 130.81, 155.56, 196.0],
        [65.41, 98.0, 130.81, 155.56, 196.0],
        [51.91, 77.78, 130.81, 155.56, 196.0],
        [51.91, 77.78, 130.81, 155.56, 196.0],
        [43.65, 65.41, 103.83, 130.81, 155.56, 196.0],
        [43.65, 65.41, 103.83, 130.81, 155.56, 196.0],
        [49.00, 73.42, 98.00, 130.81, 146.83],
        [49.00, 73.42, 98.00, 123.47, 146.83],
        [65.41, 98.0, 146.83, 155.56, 196.0],
        [65.41, 98.0, 146.83, 155.56, 196.0],
        [51.91, 77.78, 130.81, 155.56, 207.65],
        [49.00, 73.42, 87.31, 123.47, 174.61]
    ]
    
    melody = [
        (2.0, 392.00, 2.0),
        (4.0, 349.23, 2.0),
        (6.0, 311.13, 2.0),
        (8.0, 293.66, 4.0),
        (12.0, 261.63, 2.0),
        (14.0, 311.13, 2.0),
        (16.0, 349.23, 3.0),
        (19.0, 392.00, 1.0),
        (20.0, 466.16, 2.0),
        (22.0, 523.25, 2.0),
        (24.0, 493.88, 4.0),
        (28.0, 392.00, 4.0),
        (32.0, 523.25, 2.0),
        (34.0, 466.16, 2.0),
        (36.0, 415.30, 2.0),
        (38.0, 392.00, 2.0),
        (40.0, 349.23, 2.0),
        (42.0, 311.13, 2.0),
        (44.0, 293.66, 2.0),
        (46.0, 246.94, 2.0),
    ]
    
    stereo_samples = []
    beat_dur = 60.0 / bpm
    pn = PinkNoise()
    lp = LowPass(300, sr)
    
    for i in range(n):
        t = i / sr
        bar_idx = int(t / bar_dur) % total_bars
        chord = chords_bars[bar_idx]
        
        pad_l = 0.0
        pad_r = 0.0
        for f_idx, freq in enumerate(chord):
            f_l = freq * (1.0 - 0.0015 * (f_idx % 2))
            f_r = freq * (1.0 + 0.0015 * ((f_idx + 1) % 2))
            s_l = math.sin(2 * math.pi * f_l * t) * 0.12 + math.sin(4 * math.pi * f_l * t) * 0.03
            s_r = math.sin(2 * math.pi * f_r * t) * 0.12 + math.sin(4 * math.pi * f_r * t) * 0.03
            pad_l += s_l
            pad_r += s_r
            
        sub = math.sin(2 * math.pi * 32.7 * t) * 0.25
        pad_l += sub
        pad_r += sub
        
        drum_l = 0.0
        drum_r = 0.0
        t_in_bar = t % bar_dur
        for hit_beat in [0.0, 2.5]:
            hit_t = hit_beat * beat_dur
            if t_in_bar >= hit_t:
                dt = t_in_bar - hit_t
                drum_sub = math.sin(2 * math.pi * (70 * math.exp(-15 * dt) + 38) * dt) * math.exp(-5.0 * dt) * 0.45
                drum_l += drum_sub
                drum_r += drum_sub
                
        mel_l = 0.0
        mel_r = 0.0
        for m_beat, m_freq, m_len in melody:
            m_start = m_beat * beat_dur
            m_dur_s = m_len * beat_dur
            if m_start <= t < (m_start + m_dur_s + 1.5):
                dt = t - m_start
                env = math.exp(-2.5 * dt) if dt >= 0 else 0.0
                tone = (math.sin(2 * math.pi * m_freq * dt) + 0.3 * math.sin(4 * math.pi * m_freq * dt)) * env * 0.22
                mel_l += tone * 0.85
                mel_r += tone * 1.15
                
        amb = lp.process(pn.sample()) * 0.08
        
        left = pad_l + drum_l + mel_l + amb
        right = pad_r + drum_r + mel_r + amb
        stereo_samples.append((left, right))
        
    return stereo_samples

def generate_all():
    print('Synthesizing modern RPG sound effects suite...')
    save_wav('sfx_blade.wav', generate_blade())
    save_wav('sfx_impact.wav', generate_impact())
    save_wav('sfx_hurt.wav', generate_hurt())
    save_wav('sfx_jump.wav', generate_jump())
    save_wav('sfx_double_jump.wav', generate_double_jump())
    save_wav('sfx_land_soft.wav', generate_land_soft())
    save_wav('sfx_land_hard.wav', generate_land_hard())
    save_wav('sfx_step.wav', generate_step())
    save_wav('sfx_player_dash.wav', generate_player_dash())
    save_wav('sfx_coin.wav', generate_coin())
    save_wav('sfx_gem.wav', generate_gem())
    save_wav('sfx_heal.wav', generate_heal())
    save_wav('sfx_checkpoint.wav', generate_checkpoint())
    save_wav('sfx_upgrade.wav', generate_upgrade())
    save_wav('sfx_power_select.wav', generate_power_select())
    save_wav('sfx_power_fail.wav', generate_power_fail())
    save_wav('sfx_menu.wav', generate_menu())
    save_wav('sfx_complete.wav', generate_complete())
    save_wav('sfx_defeat.wav', generate_defeat())
    save_wav('sfx_respawn.wav', generate_respawn())
    save_wav('sfx_enemy_warning.wav', generate_enemy_warning())
    save_wav('sfx_enemy_dash.wav', generate_enemy_dash())
    save_wav('sfx_enemy_defeat.wav', generate_enemy_defeat())
    save_wav('sfx_boss_warning.wav', generate_boss_warning())
    save_wav('sfx_boss.wav', generate_boss_attack())
    save_wav('sfx_air_strike.wav', generate_air_strike())
    save_wav('sfx_ember_cast.wav', generate_ember_cast())
    save_wav('sfx_frost_cast.wav', generate_frost_cast())
    save_wav('sfx_gale_cast.wav', generate_gale_cast())
    save_wav('sfx_power.wav', generate_power())
    
    print('Synthesizing Emberfall exploration theme...')
    ember_samples = generate_emberfall_theme()
    save_wav('emberfall_exploration_theme.wav', ember_samples, sample_rate=44100, channels=2)
    print('All modern audio files generated successfully!')

if __name__ == '__main__':
    generate_all()
