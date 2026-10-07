using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
namespace LostRealms {
 public static class NewWeaponPoseChecks {
  public static IEnumerator Run(Action<bool,string> check){
   var g=RealmGame.I;var hero=g.Player;var flags=BindingFlags.Instance|BindingFlags.NonPublic;
   foreach(var id in new[]{WeaponId.FantasyGreatsword,WeaponId.FierySword,WeaponId.OrnateCurvedBlade,WeaponId.AstralStaff,WeaponId.GlacierMaul,WeaponId.VoidReaper,WeaponId.FrostHalberd}){
    g.EquipWeapon(id);hero.Visual.Play("idle");yield return new WaitForSeconds(1.5f);
    var eq=hero.GetComponentInChildren<EquippedWeapon>();var mesh=eq.GetComponentInChildren<Renderer>();
    var hand=hero.Visual.animator.GetBoneTransform(HumanBodyBones.RightHand);var finger=hero.Visual.animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal);
    Vector3 rest=Vector3.zero,normal=Vector3.zero;
    for(int i=0;i<4;i++){
     hero.transform.rotation=Quaternion.Euler(0,90*i,0);yield return null;yield return null;
     Vector3 c=hero.Visual.transform.InverseTransformPoint(mesh.bounds.center),n=hero.Visual.transform.InverseTransformDirection(mesh.transform.forward);
     if(i==0){rest=c;normal=n;Capture(hero,id+"-sheathed",true);}
     check(Vector3.Distance(c,rest)<.02f&&Vector3.Distance(n,normal)<.02f,id+" sheath follows facing "+i);
     check(c.z<-.18f&&c.z>-.38f&&c.y>.8f&&c.y<1.3f,id+" sheath stays behind torso");
    }
    typeof(EquippedWeapon).GetField("draw",flags).SetValue(eq,1f);
    typeof(EquippedWeapon).GetField("drawnUntil",flags).SetValue(eq,Time.time+2f);
    hero.Visual.PlayAttack(1,false,1,WeaponCatalog.AttackStyle(WeaponCatalog.Get(id)));yield return new WaitForSeconds(.2f);
    for(int i=0;i<4;i++){
     hero.transform.rotation=Quaternion.Euler(0,90*i,0);yield return null;yield return null;
     Vector3 palm=finger?Vector3.Lerp(hand.position,finger.position,.65f):hand.position;
     check(Vector3.Distance(mesh.transform.TransformPoint(EquippedWeapon.GripAnchorPoint(id,mesh.localBounds)),palm)<.015f,id+" handle remains in palm at yaw "+i);
     if(i==0)Capture(hero,id+"-held",false);
    }
   }
  }
  static void Capture(Hero hero,string name,bool back){
   var renderers=hero.GetComponentsInChildren<Renderer>();var layers=new int[renderers.Length];
   for(int i=0;i<renderers.Length;i++){layers[i]=renderers[i].gameObject.layer;renderers[i].gameObject.layer=30;}
   var go=new GameObject("Pose camera");var camera=go.AddComponent<Camera>();camera.cullingMask=1<<30;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.18f,.2f,.24f);camera.orthographic=true;camera.orthographicSize=1.25f;
   Vector3 center=hero.transform.position+Vector3.up*.95f;camera.transform.position=center+hero.transform.forward*(back?-4:4)+hero.transform.right*1.5f;camera.transform.LookAt(center);
   var lamp=new GameObject("Pose light");var light=lamp.AddComponent<Light>();light.type=LightType.Directional;light.intensity=2;lamp.transform.rotation=camera.transform.rotation;
   var rt=new RenderTexture(600,600,24);var old=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
   var image=new Texture2D(600,600,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,600,600),0,0);image.Apply();System.IO.File.WriteAllBytes("Validation/pose-"+name+".png",image.EncodeToPNG());
   RenderTexture.active=old;camera.targetTexture=null;rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(image);UnityEngine.Object.Destroy(go);UnityEngine.Object.Destroy(lamp);
   for(int i=0;i<renderers.Length;i++)if(renderers[i])renderers[i].gameObject.layer=layers[i];
  }
 }
}
