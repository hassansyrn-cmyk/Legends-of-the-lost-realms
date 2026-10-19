using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace LostRealms {
 public static class RealmPerformanceProbe {
  // A repeatable CPU/allocation microbenchmark, not a phone FPS estimate.
  public static void Measure(RealmGame game){
   var report=new StringBuilder("New realm CPU microbenchmark (Editor; target-device FPS not measured)\n");
   foreach(int chapter in new[]{16,21}){
    game.LoadLevel(chapter);
    var renderers=game.World.GetComponentsInChildren<MeshRenderer>();
    var groups=renderers.Where(r=>r.GetComponent<MeshFilter>()&&r.sharedMaterial).GroupBy(r=>new {mesh=r.GetComponent<MeshFilter>().sharedMesh,material=r.sharedMaterial});
    report.AppendLine("Chapter "+chapter+": mesh renderers="+renderers.Length+", repeated mesh/material instances="+groups.Where(g=>g.Count()>1).Sum(g=>g.Count())+", instancing enabled="+renderers.Count(r=>r.sharedMaterial&&r.sharedMaterial.enableInstancing));
    CompareInstancing(game,renderers,report);
    if(chapter==16){
     var circuit=game.Storm.Circuits[0];var refresh=typeof(StormCircuit).GetMethod("RefreshCurrent",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
     refresh.Invoke(circuit,null);var arm=circuit.Relays[0].Find("Copper directional arm");
     circuit.Orientations[0]=(circuit.Orientations[0]+1)%3;refresh.Invoke(circuit,null);
     if(Vector3.Angle(arm.forward,circuit.BeamDirection(0,circuit.Orientations[0]))>.01f)throw new Exception("Cached relay visual did not follow a direction change");
     var wire=circuit.GetComponentsInChildren<LineRenderer>()[0];var start=wire.GetPosition(0);circuit.transform.position+=Vector3.right;refresh.Invoke(circuit,null);
     if(Vector3.Distance(wire.GetPosition(0),start+Vector3.right)>.001f)throw new Exception("Cached relay visual did not follow its parent");
     circuit.transform.position-=Vector3.right;refresh.Invoke(circuit,null);report.AppendLine("PASS cached Storm relay tracks orientation and parent motion");
    }
    if(chapter!=21)continue;
    var focus=game.World.GetComponentsInChildren<AstralRoute>().SelectMany(r=>r.Focuses).ToArray();
    Action oldScan=()=>{foreach(var f in focus)foreach(var r in f.GetComponentsInChildren<Renderer>()){var m=r.material;m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",Color.cyan*.2f);}};
    var cached=focus.SelectMany(f=>f.GetComponentsInChildren<Renderer>()).Select(r=>r.material).ToArray();
    Action cachedScan=()=>{foreach(var m in cached)m.SetColor("_EmissionColor",Color.cyan*.2f);};
    long counter=GC.GetAllocatedBytesForCurrentThread();GC.KeepAlive(new byte[4096]);bool allocationsAvailable=GC.GetAllocatedBytesForCurrentThread()>counter;
    foreach(var pair in new[]{("Previous renderer/material update",oldScan),("Cached material update",cachedScan)}){
     pair.Item2();long allocated=GC.GetAllocatedBytesForCurrentThread();var watch=Stopwatch.StartNew();
     for(int i=0;i<2000;i++)pair.Item2();watch.Stop();allocated=GC.GetAllocatedBytesForCurrentThread()-allocated;
     report.AppendLine(pair.Item1+": 2000 iterations "+watch.Elapsed.TotalMilliseconds.ToString("F2")+"ms, managed bytes="+(allocationsAvailable?allocated.ToString():"counter unavailable"));
    }
   }
   Directory.CreateDirectory("Validation");File.WriteAllText("Validation/realm-performance.txt",report.ToString());UnityEngine.Debug.Log(report);
  }
  static void CompareInstancing(RealmGame game,MeshRenderer[] renderers,StringBuilder report){
   var materials=renderers.Select(r=>r.sharedMaterial).Where(m=>m).Distinct().ToArray();var enabled=materials.Select(m=>m.enableInstancing).ToArray();
   var obj=new GameObject("Instancing visual equivalence camera");var cam=obj.AddComponent<Camera>();cam.transform.position=game.World.Exploration.Sites[1].transform.position+new Vector3(16,22,-28);cam.transform.LookAt(game.World.Exploration.Sites[1].transform.position);cam.farClipPlane=220;
   var rt=new RenderTexture(640,360,24);cam.targetTexture=rt;var image=new Texture2D(640,360,TextureFormat.RGB24,false);var previous=RenderTexture.active;
   try{
    foreach(var mat in materials)mat.enableInstancing=false;cam.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,640,360),0,0);image.Apply();var before=image.GetPixels32();
    for(int i=0;i<materials.Length;i++)materials[i].enableInstancing=enabled[i];cam.Render();image.ReadPixels(new Rect(0,0,640,360),0,0);image.Apply();var after=image.GetPixels32();
    int changed=0;for(int i=0;i<before.Length;i++)if(Math.Abs(before[i].r-after[i].r)>3||Math.Abs(before[i].g-after[i].g)>3||Math.Abs(before[i].b-after[i].b)>3)changed++;
    report.AppendLine("Instancing image comparison: pixels differing by >3/255="+changed+"/"+before.Length);
    if(changed>before.Length*.001f)throw new Exception("Instancing changed the rendered scene");
   }finally{for(int i=0;i<materials.Length;i++)materials[i].enableInstancing=enabled[i];RenderTexture.active=previous;cam.targetTexture=null;rt.Release();UnityEngine.Object.Destroy(obj);UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(image);}
  }
 }
}
