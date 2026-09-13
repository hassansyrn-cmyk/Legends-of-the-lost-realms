using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace LostRealms {
 // Facing scan for the desert buildings. The census/import root rotations flip
 // pack axes unpredictably ((270,0,0), (90,180,0), (270,270,0)…), so doorway
 // axes are never derived from the file space — they are MEASURED on the real
 // prefab instantiated with the exact runtime pose:
 //   prop.localRotation = Quaternion.AngleAxis(yaw, Vector3.up) * baseRot,
 //   prop.localScale    = uniform from Fit (height 3 m).
 // For each candidate world yaw we sample the standing geometry into
 // ground/roof occupancy grids and measure how wide the ground-level opening is
 // along the +X and -X wall lines. The yaw that puts the widest opening on +X is
 // the facing value RealmProps uses (Validation/door-facing.txt).
 public static class DoorScan {
  public static void Run(){
   var sb=new StringBuilder();
   string outDir=Path.GetFullPath(Path.Combine(Application.dataPath,"../Validation"));
   Directory.CreateDirectory(outDir);
   foreach(var f in DesertFiles){
    var go=AssetDatabase.LoadAssetAtPath<GameObject>(f);
    if(!go){sb.AppendLine(Path.GetFileName(f)+" : MISSING");continue;}
    Vector3[] all=GatherWorld(go);
    if(all==null||all.Length==0){sb.AppendLine(Path.GetFileName(f)+" : NO MESH");continue;}
    Bounds b=BoundsOf(all);
    sb.AppendLine(Path.GetFileName(f)+" | standing size "+b.size.x.ToString("0.00")+"x"+b.size.y.ToString("0.00")+"x"+b.size.z.ToString("0.00"));
   }
   File.WriteAllText(Path.Combine(outDir,"door-scan.txt"),sb.ToString());
   Debug.Log("DOOR_SCAN_DONE");
  }
  // Builds the facing table for RealmProps.DoorYawOf().
  public static void VerifyFacing(){
   var sb=new StringBuilder();
   var root=new GameObject("facing_probe");
   foreach(var f in DesertFiles){
    var src=AssetDatabase.LoadAssetAtPath<GameObject>(f);
    if(!src){continue;}
    string nm=Path.GetFileNameWithoutExtension(f);
    float bestYaw=0f,bestW=0f;
    foreach(int c in new[]{0,90,180,270}){
     var inst=Object.Instantiate(src,root.transform);
     inst.transform.localPosition=Vector3.zero;
     inst.transform.localRotation=Quaternion.AngleAxis(c,Vector3.up)*src.transform.localRotation;
     Vector3[] vv=GatherWorld(inst);
     Bounds bb=BoundsOf(vv);
     float s=bb.size.y>1e-4f?3f/bb.size.y:1f;
     if(Mathf.Abs(s-1f)>0.001f){inst.transform.localScale=Vector3.one*s;vv=GatherWorld(inst);bb=BoundsOf(vv);}
     float wP=FaceWidth(vv,bb,sideX:1f);
     float wN=FaceWidth(vv,bb,sideX:-1f);
     Object.DestroyImmediate(inst);
     if(wN>bestW){bestW=wN;bestYaw=(c+180f)%360f;}   // opening lands on -X
     if(wP>bestW){bestW=wP;bestYaw=c;}               // opening lands on +X
    }
    sb.AppendLine(nm+" facingYaw(+X)="+bestYaw.ToString("0")+" widestOpen="+bestW.ToString("0.00")+"m");
   }
   Object.DestroyImmediate(root);
   string outDir=Path.GetFullPath(Path.Combine(Application.dataPath,"../Validation"));
   Directory.CreateDirectory(outDir);
   File.WriteAllText(Path.Combine(outDir,"door-facing.txt"),sb.ToString());
   Debug.Log("FACING_VERIFY_DONE");
  }
  // Ground-level opening width along the wall line of the chosen X side.
  // A row counts as open when the outermost roof (high band) overhangs the
  // outermost ground wall (low band) by >=2 cells (~0.5 m) — i.e. wall recessed
  // behind the roofline = doorway band.
  static float FaceWidth(Vector3[] all,Bounds b,float sideX){
   float cell=0.25f;
   int nx=Mathf.Max(2,Mathf.CeilToInt(b.size.x/cell)),nz=Mathf.Max(2,Mathf.CeilToInt(b.size.z/cell));
   bool[] low=new bool[nx*nz],high=new bool[nx*nz];
   for(int i=0;i<all.Length;i+=3){
    float miny=Mathf.Min(all[i].y,Mathf.Min(all[i+1].y,all[i+2].y));
    float maxy=Mathf.Max(all[i].y,Mathf.Max(all[i+1].y,all[i+2].y));
    bool inLow=maxy>0.2f&&miny<1.4f;
    bool inHigh=maxy>1.8f&&miny<4.5f;
    if(!inLow&&!inHigh)continue;
    int xlo=Mathf.Clamp(Mathf.FloorToInt((Mathf.Min(all[i].x,Mathf.Min(all[i+1].x,all[i+2].x))-b.min.x)/cell),0,nx-1);
    int xhi=Mathf.Clamp(Mathf.FloorToInt((Mathf.Max(all[i].x,Mathf.Max(all[i+1].x,all[i+2].x))-b.min.x)/cell),0,nx-1);
    int zlo=Mathf.Clamp(Mathf.FloorToInt((Mathf.Min(all[i].z,Mathf.Min(all[i+1].z,all[i+2].z))-b.min.z)/cell),0,nz-1);
    int zhi=Mathf.Clamp(Mathf.FloorToInt((Mathf.Max(all[i].z,Mathf.Max(all[i+1].z,all[i+2].z))-b.min.z)/cell),0,nz-1);
    for(int x=xlo;x<=xhi;x++)for(int z=zlo;z<=zhi;z++){
     if(inHigh)high[x*nz+z]=true;
     if(inLow)low[x*nz+z]=true;
    }
   }
   // For sideX=+1 the outer wall sits at high x columns; for -1 at low x columns.
   bool[] door=new bool[nz];
   for(int z=0;z<nz;z++){
    int roof=-1;
    for(int x=0;x<nx;x++){
     if(!high[x*nz+z])continue;
     int d=sideX>0?x:nx-1-x;
     if(d>roof)roof=d;
    }
    if(roof<2)continue;
    int wall=-1;
    for(int x=0;x<nx;x++){
     if(!low[x*nz+z])continue;
     int d=sideX>0?x:nx-1-x;
     if(d>wall)wall=d;
    }
    door[z]=roof-wall>=2;
   }
   int best=0,run=0;
   for(int z=0;z<nz;z++){if(door[z]){run++;if(run>best)best=run;}else run=0;}
   return best*cell;
  }
  static string[] DesertFiles={"Assets/Resources/Props/Desert/Gate_01.FBX","Assets/Resources/Props/Desert/Church_01.FBX",
   "Assets/Resources/Props/Desert/House_01.FBX","Assets/Resources/Props/Desert/House_02.FBX",
   "Assets/Resources/Props/Desert/Ruin_01.FBX","Assets/Resources/Props/Desert/Tent_01.FBX",
   "Assets/Resources/Props/Desert/Tower_01.FBX","Assets/Resources/Props/Desert/Wall_01.FBX"};
  static Vector3[] GatherWorld(GameObject go){
   var list=new System.Collections.Generic.List<Vector3>();
   foreach(var mr in go.GetComponentsInChildren<MeshRenderer>(true)){
    var mf=mr.GetComponent<MeshFilter>();
    if(!mf||!mf.sharedMesh)continue;
    var mesh=mf.sharedMesh;
    Matrix4x4 toWorld=mr.transform.localToWorldMatrix;
    var v=mesh.vertices;var t=mesh.triangles;
    for(int i=0;i<t.Length;i++)list.Add(toWorld.MultiplyPoint3x4(v[t[i]]));
   }
   return list.ToArray();
  }
  static Bounds BoundsOf(Vector3[] verts){
   Bounds b=new Bounds(verts[0],Vector3.zero);
   for(int i=1;i<verts.Length;i++)b.Encapsulate(verts[i]);
   return b;
  }
 }
}