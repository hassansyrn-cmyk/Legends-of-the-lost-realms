using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
namespace LostRealms {
 public static class EpicPolishChecks {
  static void Capture(EpicChargedAttack effect,string file){
   foreach(var t in effect.GetComponentsInChildren<Transform>())t.gameObject.layer=31;
   var obj=new GameObject("Epic effect review camera");var cam=obj.AddComponent<Camera>();cam.cullingMask=1<<31;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.025f,.035f,.055f);cam.orthographic=true;cam.orthographicSize=2.2f;
   cam.transform.position=effect.transform.position+new Vector3(3,2,3);cam.transform.LookAt(effect.transform.position);
   var rt=new RenderTexture(700,700,24);var old=RenderTexture.active;cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var tex=new Texture2D(700,700,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,700,700),0,0);tex.Apply();System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath,"../Validation/NewEpic/"+file+".png"),tex.EncodeToPNG());RenderTexture.active=old;cam.targetTexture=null;rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(tex);UnityEngine.Object.Destroy(obj);
  }

  public static IEnumerator Run(Action<bool,string> check){
   var g=RealmGame.I;g.LoadLevel(7);yield return new WaitForSeconds(.4f);
   var boss=g.Enemies.Find(e=>e&&e.Boss);foreach(var e in g.Enemies)if(e!=boss)e.enabled=false;
   g.Player.enabled=false;g.Player.Health=10000;g.Player.Warp(boss.transform.position+Vector3.right*6f);
   var start=boss.transform.position;float before=Vector3.Distance(start,g.Player.transform.position);
   yield return new WaitForSeconds(2f);
   check(Vector3.Distance(boss.transform.position,g.Player.transform.position)<before-.5f,"Large lava guardian advances toward Aster on its actual arena");
   var skin=boss.Visual.GetComponentInChildren<SkinnedMeshRenderer>();check(skin.sharedMesh.vertexCount<40000,"Welded lava mesh reduces duplicate skin vertices");
   boss.WarpBody(start);var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="Boss steering regression obstacle";wall.transform.SetParent(g.World.transform);wall.transform.position=start+Vector3.right*2.5f+Vector3.up*1.5f;wall.transform.localScale=new Vector3(.5f,3f,2f);Physics.SyncTransforms();
   yield return new WaitForSeconds(5f);
   check(Vector3.Distance(boss.transform.position,start)>.6f,"Lava guardian moves around a solid obstacle instead of walking in place");
   check(boss.Grounded,"Lava guardian remains grounded while steering");UnityEngine.Object.Destroy(wall);
   g.LoadLevel(1);yield return new WaitForSeconds(.3f);foreach(var e in g.Enemies)e.enabled=false;g.Player.enabled=false;
   var deck=new GameObject("Island");deck.transform.SetParent(g.World.transform);deck.transform.position=new Vector3(1000,30,0);Art.Shape("Epic effect test floor",PrimitiveType.Cube,Vector3.down*.25f,new Vector3(30,.5f,30),Color.gray,deck.transform,true);
   var foes=new Enemy[2];for(int i=0;i<2;i++){var obj=new GameObject("Epic effect target");obj.transform.SetParent(deck.transform);obj.transform.position=deck.transform.position+Vector3.forward*(4+i*2)+Vector3.up*.03f;foes[i]=obj.AddComponent<Enemy>();foes[i].Configure(22,false,obj.transform.position,new Vector2(20,20));foes[i].enabled=false;}
   Physics.SyncTransforms();float h0=foes[0].Health,h1=foes[1].Health;
   var lance=EpicChargedAttack.Fire(deck.transform.position+Vector3.up*1.05f,Vector3.forward,1,WeaponId.Duskblade);
   check(lance&&lance.Weapon==WeaponId.Duskblade,"Duskblade creates its dedicated star lance");Capture(lance,"Duskblade-star-lance");yield return new WaitForSeconds(.45f);
   check(foes[0].Health<h0&&foes[1].Health<h1,"Duskblade charge pierces more than one foe");
   if(lance)UnityEngine.Object.Destroy(lance.gameObject);h0=foes[0].Health;
   var vortex=EpicChargedAttack.Fire(deck.transform.position+Vector3.up*1.05f,Vector3.forward,1,WeaponId.Soulreaper);
   check(vortex&&vortex.Weapon==WeaponId.Soulreaper,"Soulreaper creates its dedicated soul vortex");Capture(vortex,"Soulreaper-soul-vortex");yield return new WaitForSeconds(.45f);check(foes[0].Health<h0,"Soul vortex applies pulsed damage");
   var ribbon=vortex.GetComponentInChildren<LineRenderer>();Vector3 point=ribbon.GetPosition(10);g.Pause();yield return new WaitForSecondsRealtime(.25f);check(Vector3.Distance(point,ribbon.GetPosition(10))<.001f,"Soul vortex animation freezes during pause");g.Resume();
   UnityEngine.Object.Destroy(vortex.gameObject);UnityEngine.Object.Destroy(deck);g.Player.enabled=true;
  }
 }
}
