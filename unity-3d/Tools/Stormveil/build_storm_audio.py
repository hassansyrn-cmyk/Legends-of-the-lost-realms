import wave,math,random,struct
from pathlib import Path
folder=Path('unity-3d/Assets/Resources/Audio');rng=random.Random(164)
def write(name,duration,sample):
 rate=22050;data=bytearray()
 for i in range(int(duration*rate)):
  t=i/rate;v=max(-.95,min(.95,sample(t,duration)));data.extend(struct.pack('<h',int(v*32767)))
 with wave.open(str(folder/(name+'.wav')),'wb') as w:w.setnchannels(1);w.setsampwidth(2);w.setframerate(rate);w.writeframes(data)
def score(t,d,boss=False):
 notes=[146.83,174.61,220,196,130.81,174.61,196,110];beat=.4 if boss else .8;step=int(t/beat);f=notes[(step//4)%8];env=math.sin(math.pi*(t%beat)/beat)**2
 bass=.12*math.sin(2*math.pi*f*.5*t)+.07*math.sin(2*math.pi*f*t)
 chime=.1*env*math.sin(2*math.pi*f*(2 if step%3 else 3)*t)*math.exp(-2*(t%beat))
 drum=(.14 if boss else .055)*math.exp(-30*(t%beat))*math.sin(2*math.pi*(55+40*math.exp(-20*(t%beat)))*t)
 return (bass+chime+drum)*min(1,t/.3,(d-t)/.3)
write('stormveil_theme',25.6,lambda t,d:score(t,d));write('storm_guardian_theme',25.6,lambda t,d:score(t,d,True))
write('sfx_storm_thunder',1.8,lambda t,d:(rng.uniform(-1,1)*.35+.25*math.sin(2*math.pi*42*t))*math.exp(-t*3)*min(1,t*40))
write('sfx_guardian_tempest',2.2,lambda t,d:(.22*math.sin(2*math.pi*(65+13*math.sin(t*6))*t)+rng.uniform(-.11,.11))*math.sin(math.pi*t/d)**2)
for name,f in [('storm_turn',440),('storm_ground',220),('storm_wind',95)]:write('sfx_'+name,1.1,lambda t,d,f=f:(.13*math.sin(2*math.pi*f*t)+.05*math.sin(2*math.pi*f*1.5*t))*math.sin(math.pi*t/d)**2)
print('STORM_AUDIO_BUILT')
