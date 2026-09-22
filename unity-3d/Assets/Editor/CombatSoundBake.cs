using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace LostRealms {
 // Original deterministic layered synthesis, requiring no service or recordings.
 public static class CombatSoundBake {
  public static readonly string[] Traps={"trap_warning","trap_crush","trap_dart","trap_boulder","trap_wind","trap_fire","trap_frost","trap_poison","trap_blade","trap_spikes","trap_saw_loop","trap_fan_loop"};
  [MenuItem("Lost Realms/Audio/Bake combat and trap sounds")]
  public static void Run(){
   Directory.CreateDirectory("Assets/Resources/Audio");var report=new StringBuilder();int seed=2409;
   foreach(string style in new[]{"slash","chop","spear","unarmed","charged"})for(int v=1;v<=3;v++)Bake("attack_"+style+"_"+v,style,seed++,report);
   foreach(string key in Traps)Bake(key,key,seed++,report);
   AssetDatabase.Refresh();Directory.CreateDirectory("Validation");File.WriteAllText("Validation/combat-audio.txt",report.ToString());
  }
  static void Bake(string key,string kind,int seed,StringBuilder report){
   const int rate=44100;var rng=new System.Random(seed);bool swing=!kind.StartsWith("trap_");
   float duration=swing?(kind=="charged"?.72f:kind=="chop"?.52f:.34f):kind=="trap_crush"?1.15f:kind=="trap_boulder"?1.4f:kind=="trap_wind"?1.2f:kind=="trap_warning"?.6f:.65f;
   bool loop=kind.EndsWith("_loop");if(loop)duration=1f;
   int count=(int)(duration*rate);var data=new float[count];float low=0,body=0,peak=0,phase=0,detune=.94f+(float)rng.NextDouble()*.12f;
   for(int i=0;i<count;i++){
    float t=(float)i/rate,u=t/duration,n=(float)rng.NextDouble()*2-1;low+=.055f*(n-low);body+=.009f*(n-body);float high=n-low,value=0;
    if(loop){
     // Integer frequencies make a continuous one-second mechanical loop.
     float hz=kind=="trap_saw_loop"?93:61;
     value=.16f*Mathf.Sin(2*Mathf.PI*hz*t)+.07f*Mathf.Sin(2*Mathf.PI*hz*3*t);
     for(int mode=0;mode<12;mode++)value+=.024f*Mathf.Sin(2*Mathf.PI*(607+mode*113)*t+mode)*(.8f+.2f*Mathf.Sin(2*Mathf.PI*12*t));
    }else if(swing){
     float rush=Mathf.Pow(Mathf.Sin(Mathf.PI*u),2.5f)*Mathf.Exp(-u*2),weight=kind=="chop"||kind=="charged"?2.2f:kind=="unarmed"?1.5f:.8f;
     value=(low*3+high*.12f)*rush+body*weight*Mathf.Exp(-t*9);
     if(kind=="slash"||kind=="spear")value+=.045f*Mathf.Sin(2*Mathf.PI*detune*(2100*t-750*t*t))*Mathf.Exp(-t*17);
     if(kind=="unarmed")value+=.18f*Mathf.Sin(2*Mathf.PI*(95*t-60*t*t))*Mathf.Exp(-t*24);
     if(kind=="charged")value+=body*3*Mathf.Sin(Mathf.PI*u);
    }else if(kind=="trap_crush"||kind=="trap_boulder"){
     float env=kind=="trap_crush"?Mathf.Exp(-t*5):Mathf.Pow(Mathf.Sin(Mathf.PI*u),.65f);
     phase+=2*Mathf.PI*(kind=="trap_crush"?55+85*Mathf.Exp(-t*24):48)/rate;
     value=(body*4+low*.7f+Mathf.Sin(phase)*.19f)*env+high*.22f*Mathf.Exp(-t*35)+low*.6f*Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*93)),12)*Mathf.Exp(-t*3);
    }else if(kind=="trap_frost"){
     value=high*.14f*Mathf.Exp(-t*16);
     foreach(float hz in new[]{1741f,2389f,3197f,4273f})value+=.08f*Mathf.Sin(2*Mathf.PI*hz*detune*t)*Mathf.Exp(-t*(7+hz/1000));
    }else if(kind=="trap_poison"){
     phase+=2*Mathf.PI*(180+550*Mathf.Exp(-t*18))/rate;value=(low*2+Mathf.Sin(phase)*.15f)*Mathf.Exp(-t*9)+high*.08f*Mathf.Exp(-t*6);
    }else if(kind=="trap_fire"||kind=="trap_wind"){
     float env=kind=="trap_wind"?Mathf.Pow(Mathf.Sin(Mathf.PI*u),.65f):Mathf.Exp(-t*5);
     value=(low*3+body*3+high*.08f)*env;
     if(kind=="trap_fire")value+=high*.15f*Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*257)),16)*Mathf.Exp(-t*4);
    }else{
     float decay=kind=="trap_warning"?7:kind=="trap_blade"?10:24;value=(low*2+high*.18f)*Mathf.Exp(-t*decay);
     foreach(float hz in new[]{413f,1097f,1831f})value+=.065f*Mathf.Sin(2*Mathf.PI*hz*detune*t)*Mathf.Exp(-t*(decay+hz/600));
     if(kind=="trap_dart")value+=high*.35f*Mathf.Exp(-t*45);
    }
    data[i]=loop?value:value*Mathf.Min(1,t/.004f)*Mathf.Min(1,(duration-t)/.04f);peak=Mathf.Max(peak,Mathf.Abs(data[i]));
   }
   using(var w=new BinaryWriter(File.Create("Assets/Resources/Audio/sfx_"+key+".wav"))){
    w.Write(Encoding.ASCII.GetBytes("RIFF"));w.Write(36+count*2);w.Write(Encoding.ASCII.GetBytes("WAVEfmt "));w.Write(16);w.Write((short)1);w.Write((short)1);w.Write(rate);w.Write(rate*2);w.Write((short)2);w.Write((short)16);w.Write(Encoding.ASCII.GetBytes("data"));w.Write(count*2);
    foreach(float sample in data)w.Write((short)(sample/Mathf.Max(peak,.001f)*.78f*32767));
   }
   report.AppendLine(key+" seconds="+duration+" peak=0.78 seed="+seed);
  }
 }
}
