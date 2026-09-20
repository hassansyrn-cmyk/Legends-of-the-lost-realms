using System.Linq;
using UnityEditor;
using UnityEngine;

// Edit-mode diagnosis: build each chapter, list every collider within 2.2 m of
// the spawn point so a spawn-blocker is identified by name and distance.
public static class SpawnScan {
 [MenuItem("Lost Realms/Debug/Spawn Scan")]
 public static void Run(){
  var sb=new System.Text.StringBuilder();
  var game=new GameObject("ScanGame").AddComponent<LostRealms.RealmGame>();
  for(int level=1;level<=6;level++){
   game.LoadLevel(level);
   var world=Object.FindAnyObjectByType<LostRealms.RealmWorld>();
   Vector3 spawn=world.Spawn;
   sb.AppendLine("LEVEL "+level+" spawn="+spawn.ToString("0.00")+" boss="+world.IsBoss);
   Physics.SyncTransforms();
   foreach(var col in Physics.OverlapSphere(spawn,2.2f,~0,QueryTriggerInteraction.Collide)){
    float d=Vector3.Distance(col.ClosestPoint(spawn),spawn);
    sb.AppendLine(string.Format("  HIT d={0:0.00} {1} on {2} parent={3}",d,col.GetType().Name,col.transform.name,GetPath(col.transform)));
   }
   // also what sits right above the spawn (drop-in blockers)
   if(Physics.Raycast(spawn+Vector3.up*3f,Vector3.down,out var hit,4f,~0,QueryTriggerInteraction.Collide))
    sb.AppendLine("  ABOVE: "+hit.transform.name+" at h="+hit.distance.ToString("0.00"));
  }
  System.IO.File.WriteAllText("Validation/spawn-scan.txt",sb.ToString());
  Debug.Log("SPAWN_SCAN_DONE");
  Object.DestroyImmediate(game.gameObject);
 }
 static string GetPath(Transform t){
  var p=t.name;var c=t.parent;
  while(c){p=c.name+"/"+p;c=c.parent;}
  return p;
 }
}
