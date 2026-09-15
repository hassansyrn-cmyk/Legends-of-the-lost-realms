using System.Text;
using UnityEditor;
using UnityEngine;
namespace LostRealms {
 // Boots the game (headless) and captures every Error/Exception logged while
 // loading several levels, to locate blockers like a menu-dead exception.
 public static class BootProbe {
  static readonly StringBuilder sb=new StringBuilder();
  public static void Diagnose(){
   Application.logMessageReceived+=(condition,stackTrace,type)=>{if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)sb.AppendLine("["+type+"] "+condition);};
   try{
    if(RealmGame.I==null){
     var go=new GameObject("BootProbe");
     var gr=go.AddComponent<RealmGame>();
     sb.AppendLine("RealmGame="+(gr?"OK":"NULL"));
     try{
      typeof(RealmGame).GetMethod("Awake",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(gr,null);
      sb.AppendLine("awake ok");
     }catch(System.Exception e){var x=e.InnerException??e;sb.AppendLine("AWAKE THREW "+x.GetType().Name+": "+x.Message);}
    }
    sb.AppendLine("game="+(RealmGame.I?"OK":"NULL"));
    var g=RealmGame.I;
    if(g){
     foreach(int lvl in new[]{1,2,4,8,12,15,16}){
      try{g.LoadLevel(lvl);sb.AppendLine("LoadLevel "+lvl+" ok, screen="+g.Screen);}
      catch(System.Exception e){sb.AppendLine("LoadLevel "+lvl+" THREW "+e.GetType().Name+": "+e.Message);}
     }
    }
   }catch(System.Exception e){sb.AppendLine("BOOT THREW "+e.GetType().Name+": "+e.Message);}
   string outDir=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../Validation"));
   System.IO.Directory.CreateDirectory(outDir);
   System.IO.File.WriteAllText(System.IO.Path.Combine(outDir,"boot-probe.txt"),sb.ToString());
   Debug.Log("BOOT_PROBE_DONE");
   EditorApplication.Exit(0);
  }
 }
}
