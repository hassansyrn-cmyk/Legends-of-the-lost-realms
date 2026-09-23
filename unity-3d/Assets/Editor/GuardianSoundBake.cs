using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace LostRealms {
 public static class GuardianSoundBake {
  [MenuItem("Lost Realms/Audio/Bake distinct guardian voices")]
  public static void Run(){
   var report=new StringBuilder();
   string[] names={"heartwood","sunscar","whiteout","warden"};
   float[] lengths={2.7f,1.85f,3.1f,2.35f};
   for(int voice=0;voice<4;voice++){
    const int rate=44100;int count=(int)(lengths[voice]*rate);var samples=new float[count];
    var rng=new System.Random(5229+voice);float low=0,rumble=0,phase=0,peak=0;
    for(int i=0;i<count;i++){
     float t=(float)i/rate,u=t/lengths[voice],noise=(float)rng.NextDouble()*2-1;
     low+=.045f*(noise-low);rumble+=.004f*(noise-rumble);
     float env=Mathf.Pow(Mathf.Sin(Mathf.PI*u),.7f),v=0;
     if(voice==0){ // Hollow timber groan, two creaking breaths, deep chest.
      phase+=2*Mathf.PI*(57+17*Mathf.Sin(u*Mathf.PI)+3*Mathf.Sin(t*19))/rate;
      float breath=.55f+.45f*Mathf.Pow(Mathf.Sin(t*4.2f),2);
      v=(.32f*Mathf.Sin(phase)+.12f*Mathf.Sin(phase*2)+low*1.8f+rumble*3)*breath;
      v+=.12f*Mathf.Sin(phase*5+Mathf.Sin(t*31))*Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*14)),6);
     }else if(voice==1){ // Short, abrasive bellow with a second gravel bark.
      phase+=2*Mathf.PI*(108-38*u+9*Mathf.Sin(t*37))/rate;
      float bark=.35f+.65f*Mathf.Exp(-Mathf.Pow((u-.23f)*7,2))+.5f*Mathf.Exp(-Mathf.Pow((u-.66f)*10,2));
      v=(.22f*(float)Math.Tanh(3*Mathf.Sin(phase))+.15f*Mathf.Sin(phase*3.02f)+low*2.8f)*bark;
     }else if(voice==2){ // Rising icy howl with brittle, inharmonic overtones.
      phase+=2*Mathf.PI*(230+180*Mathf.Sin(u*Mathf.PI*.85f)+12*Mathf.Sin(t*8))/rate;
      v=.25f*Mathf.Sin(phase)+.09f*Mathf.Sin(phase*2.73f)+.055f*Mathf.Sin(phase*4.17f)+low*.8f;
      v+=(noise-low)*.09f*Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*71)),10);
     }else{ // Three guttural armored snarls, breath and hot crackles.
      phase+=2*Mathf.PI*(76+11*Mathf.Sin(t*11)-22*u)/rate;
      float pulse=.3f+.7f*Mathf.Pow(Mathf.Sin(u*Mathf.PI*3),2);
      v=(.32f*Mathf.Sin(phase)+.15f*Mathf.Sin(phase*1.49f)+low*2+rumble*2)*pulse;
      v+=(noise-low)*.16f*Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*163)),18);
     }
     // A short damped reflection adds size without a long muddy tail.
     int delay=(int)(rate*(voice==2?.137f:.083f));
     samples[i]=v*env*Mathf.Min(1,t/.025f)*Mathf.Min(1,(lengths[voice]-t)/.12f)+(i>delay?samples[i-delay]*.13f:0);
     peak=Mathf.Max(peak,Mathf.Abs(samples[i]));
    }
    string path="Assets/Resources/Audio/sfx_guardian_"+names[voice]+".wav";
    using(var w=new BinaryWriter(File.Create(path))){
     w.Write(Encoding.ASCII.GetBytes("RIFF"));w.Write(36+count*2);w.Write(Encoding.ASCII.GetBytes("WAVEfmt "));w.Write(16);w.Write((short)1);w.Write((short)1);w.Write(rate);w.Write(rate*2);w.Write((short)2);w.Write((short)16);w.Write(Encoding.ASCII.GetBytes("data"));w.Write(count*2);
     foreach(float sample in samples)w.Write((short)(sample/Mathf.Max(peak,.001f)*.8f*32767));
    }
    report.AppendLine(names[voice]+" duration="+lengths[voice]+" peak=0.8 original synthesis");
   }
   AssetDatabase.Refresh();Directory.CreateDirectory("Validation");File.WriteAllText("Validation/guardian-audio.txt",report.ToString());
  }
 }
}
