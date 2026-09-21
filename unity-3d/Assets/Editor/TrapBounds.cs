using System.Linq;
using UnityEditor;
using UnityEngine;

// One-shot: full renderer bounds of every trap prefab + the prop-building slots
// so colliders can be sized to visible reality.
public static class TrapBounds {
 [MenuItem("Lost Realms/Debug/Trap Bounds")]
 public static void Run(){
  var sb=new System.Text.StringBuilder();
  foreach(var name in new[]{"Trap_Crusher","Trap_Turret","Trap_Boulder","Trap_Brazier","Trap_Totem","Trap_Serpent"}){
   var go=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Props/Traps/"+name+".fbx");
   if(!go){sb.AppendLine(name+" MISSING");continue;}
   var renderers=go.GetComponentsInChildren<Renderer>(true);
   var b=renderers[0].bounds;
   for(int i=1;i<renderers.Length;i++)b.Encapsulate(renderers[i].bounds);
   sb.AppendLine(string.Format("{0}: renderers={1} FULL bounds size={2:0.00},{3:0.00},{4:0.00} minY={5:0.00} pivotY={6:0.00}",
    name,renderers.Length,b.size.x,b.size.y,b.size.z,b.min.y,go.transform.position.y));
  }
  System.IO.File.WriteAllText("Validation/trap-bounds.txt",sb.ToString());
  Debug.Log("TRAP_BOUNDS_DONE");
 }
}
