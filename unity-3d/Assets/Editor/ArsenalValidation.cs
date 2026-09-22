using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace LostRealms {
 public static class ArsenalValidation {
  // Launch with -realmTest and WITHOUT -quit: runtime checks own the exit code.
  public static void Run(){
   WeaponIconProbe.Run();var report=new StringBuilder();
   foreach(string guid in AssetDatabase.FindAssets("t:AnimationClip",new[]{"Assets/Resources/Animations/Aster"})){
    var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(guid));
    foreach(var binding in AnimationUtility.GetCurveBindings(clip))if(binding.propertyName.StartsWith("RootT.")){
     var curve=AnimationUtility.GetEditorCurve(clip,binding);
     foreach(var key in curve.keys)if(Mathf.Abs(key.value)>.00001f)throw new Exception("Aster root translation regression: "+clip.name);
    }
    report.AppendLine("ASTER_ROOT_OK "+clip.name);
   }
   foreach(string path in Directory.GetFiles("Assets/Resources/Audio","sfx_*.wav")){
    if(!Path.GetFileName(path).StartsWith("sfx_attack_")&&!Path.GetFileName(path).StartsWith("sfx_trap_"))continue;
    var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path);if(!clip||clip.samples<1000)throw new Exception("Empty audio: "+path);
    clip.LoadAudioData();var samples=new float[clip.samples*clip.channels];if(!clip.GetData(samples,0))throw new Exception("Cannot inspect audio: "+path);
    float peak=0;double energy=0;foreach(float s in samples){if(float.IsNaN(s)||float.IsInfinity(s))throw new Exception("Invalid audio: "+path);peak=Mathf.Max(peak,Mathf.Abs(s));energy+=s*s;}
    if(peak<.01f||peak>=.999f)throw new Exception("Silent/clipped audio: "+path);
    report.AppendLine("AUDIO_OK "+clip.name+" peak="+peak+" rms="+Math.Sqrt(energy/samples.Length));
   }
   foreach(string path in Directory.GetFiles("Assets/Resources/Weapons/Icons","*.png")){
    var tex=new Texture2D(2,2);tex.LoadImage(File.ReadAllBytes(path));int visible=0,magenta=0,lit=0;
    foreach(var c in tex.GetPixels32()){if(c.a>32){visible++;if(Mathf.Max(c.r,c.g,c.b)>30)lit++;}if(c.a>32&&c.r>220&&c.b>220&&c.g<35)magenta++;}
    UnityEngine.Object.DestroyImmediate(tex);
    if(visible<30||visible>15000||lit<30||magenta>visible*.1f)throw new Exception("Blank, opaque or magenta icon: "+path);
    report.AppendLine("ICON_PIXELS_OK "+Path.GetFileName(path)+" visible="+visible);
   }
   File.WriteAllText("Validation/arsenal-assets.txt",report.ToString());
   QualityValidation.Run();
  }
 }
}
