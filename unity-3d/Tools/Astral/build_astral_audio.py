import wave,math,random,struct
from pathlib import Path
folder=Path('unity-3d/Assets/Resources/Audio');rng=random.Random(251)
def write(name,duration,sample):
 rate=22050;data=bytearray()
 for i in range(int(duration*rate)):
  t=i/rate;v=max(-.95,min(.95,sample(t,duration)));data.extend(struct.pack('<h',int(v*32767)))
 with wave.open(str(folder/(name+'.wav')),'wb') as w:w.setnchannels(1);w.setsampwidth(2);w.setframerate(rate);w.writeframes(data)
def score(t,d,boss=False):
 notes=[164.81,246.94,220,196,174.61,261.63,246.94,130.81];beat=.36 if boss else .9;step=int(t/beat);f=notes[(step//4)%8];env=math.sin(math.pi*(t%beat)/beat)**2
 bass=.12*math.sin(2*math.pi*f*.5*t)+.07*math.sin(2*math.pi*f*t)
 chime=.1*env*math.sin(2*math.pi*f*(2 if step%3 else 3)*t)*math.exp(-2*(t%beat))
 drum=(.14 if boss else .055)*math.exp(-30*(t%beat))*math.sin(2*math.pi*(55+40*math.exp(-20*(t%beat)))*t)
 return (bass+chime+drum)*min(1,t/.3,(d-t)/.3)
write('astral_theme',28.8,lambda t,d:score(t,d));write('astral_guardian_theme',28.8,lambda t,d:score(t,d,True))
write('sfx_guardian_astral',2.4,lambda t,d:(.19*math.sin(2*math.pi*(83+8*math.sin(t*5))*t)+.12*math.sin(2*math.pi*124*t)+rng.uniform(-.08,.08))*math.sin(math.pi*t/d)**2)
write('sfx_astral_shift',1.2,lambda t,d:(.12*math.sin(2*math.pi*(145+180*t)*t)+rng.uniform(-.04,.04))*math.sin(math.pi*t/d)**2)
write('sfx_astral_align',1.6,lambda t,d:(.14*math.sin(2*math.pi*528*t)+.07*math.sin(2*math.pi*792*t))*math.exp(-2*t)*min(1,t*40))
write('sfx_astral_pulse',1.8,lambda t,d:(.16*math.sin(2*math.pi*(100+45*t)*t)+rng.uniform(-.07,.07))*math.sin(math.pi*t/d)**2)
print('ASTRAL_AUDIO_AUTHORED')
