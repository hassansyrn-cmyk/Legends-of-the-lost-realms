using System;
using System.Collections;
using UnityEngine;
namespace LostRealms {
 public static class MobilePlaytestChecks {
  public static IEnumerator Run(Action<bool,string> check){
   var g=RealmGame.I;int buildings=0,gates=0;
   foreach(int stage in new[]{5,6,7}){
    g.LoadLevel(stage);yield return null;Physics.SyncTransforms();
    foreach(var t in g.World.GetComponentsInChildren<Transform>()){
     if(t.name=="Prop Gate_01"){
      gates++;check(!t.GetComponent<BoxCollider>(),"Desert arch has no solid box across opening");
      check(t.GetComponentsInChildren<MeshCollider>().Length>0,"Desert arch retains shaped collision");
     }
     if(t.name!="Prop House_01"&&t.name!="Prop House_02"&&t.name!="Prop Tower_01"&&t.name!="Prop Ruin_01"&&t.name!="Prop Tent_01")continue;
     var col=t.GetComponent<BoxCollider>();buildings++;
     check(col&&col.enabled&&!col.isTrigger,"Solid building collider: "+t.name);
     var b=col.bounds;
     check(col.Raycast(new Ray(b.center+Vector3.forward*(b.extents.z+2),Vector3.back),out _,b.size.z+4),"Building front blocks a physical ray");
    }
   }
   check(buildings>0&&gates>0,"Desert collision checks cover buildings and arches");
   g.LoadLevel(10);yield return new WaitForSeconds(.2f);
   foreach(var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))check(r.name!="Aurora ribbon","No rectangular aurora panel");
   g.EquipWeapon(WeaponId.MoonChakram);yield return new WaitForSeconds(.2f);
   var equipped=g.Player.GetComponentInChildren<EquippedWeapon>();
   var mesh=equipped.GetComponentInChildren<MeshRenderer>();var size=mesh.localBounds.size;
   var normal=size.x<=size.y&&size.x<=size.z?Vector3.right:size.y<=size.z?Vector3.up:Vector3.forward;
   check(Mathf.Abs(Vector3.Dot(mesh.transform.TransformDirection(normal).normalized,g.Player.transform.forward))>.98f,"Sheathed chakram plane is parallel to Aster's back");
   check(Vector3.Distance(mesh.bounds.center,g.Player.transform.position+Vector3.up*1.2f)<.3f,"Sheathed chakram centered at visible torso height, including imported pivot scale");
   CaptureBack(g.Player);
   var start=g.Player.transform.position+Vector3.up;
   ChakramProjectile.Throw(start,g.Player.transform.forward,1,2);yield return null;
   var projectile=UnityEngine.Object.FindAnyObjectByType<ChakramProjectile>();
   check(projectile&&equipped.InFlight,"Charged chakram throw takes equipped weapon out of hand");
   check(!mesh.gameObject.activeInHierarchy,"Carried chakram hidden during flight");
   check(projectile.GetComponentInChildren<MeshFilter>().sharedMesh==mesh.GetComponent<MeshFilter>().sharedMesh,"Flying chakram uses actual weapon mesh");
   yield return new WaitForSeconds(.2f);
   check(Vector3.Distance(projectile.transform.position,start)>1.5f,"Chakram travels outward");
   yield return new WaitForSeconds(1.4f);
   check(!projectile&&!equipped.InFlight&&mesh.gameObject.activeInHierarchy,"Chakram returns and restores carried weapon");
   ChakramProjectile.Throw(g.Player.transform.position+Vector3.up,g.Player.transform.forward,1,2);yield return null;
   g.EquipWeapon(WeaponId.Axe);yield return null;yield return null;
   check(!UnityEngine.Object.FindAnyObjectByType<ChakramProjectile>(),"Changing weapon cleans up in-flight chakram");
  }
  static void CaptureBack(Hero hero){
   var renderers=hero.GetComponentsInChildren<Renderer>();var layers=new int[renderers.Length];
   for(int i=0;i<renderers.Length;i++){layers[i]=renderers[i].gameObject.layer;renderers[i].gameObject.layer=30;}
   var cameraObject=new GameObject("Chakram fit camera");var camera=cameraObject.AddComponent<Camera>();
   camera.cullingMask=1<<30;
   camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.17f,.2f,.24f);camera.fieldOfView=35;
   camera.transform.position=hero.transform.position-hero.transform.forward*3f+hero.transform.right*1.8f+Vector3.up*1.7f;
   camera.transform.LookAt(hero.transform.position+Vector3.up*1.1f);
   var target=new RenderTexture(900,900,24);var previous=RenderTexture.active;camera.targetTexture=target;camera.Render();RenderTexture.active=target;
   var image=new Texture2D(900,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,900,900),0,0);image.Apply();
   System.IO.File.WriteAllBytes("Validation/chakram-back-fit.png",image.EncodeToPNG());
   RenderTexture.active=previous;camera.targetTexture=null;target.Release();UnityEngine.Object.Destroy(target);UnityEngine.Object.Destroy(image);UnityEngine.Object.Destroy(cameraObject);
   for(int i=0;i<renderers.Length;i++)renderers[i].gameObject.layer=layers[i];
  }
 }
}
