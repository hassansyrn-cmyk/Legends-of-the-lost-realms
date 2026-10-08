using System;using System.Collections;using System.IO;using System.Linq;using UnityEngine;
namespace LostRealms {
 public static class NewEpicChecks {
  static string Folder=>Path.Combine(Application.dataPath,"../Validation/NewEpic");
  static void Layer(Transform t){t.gameObject.layer=31;var animator=t.GetComponent<Animator>();if(animator)animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;var skin=t.GetComponent<SkinnedMeshRenderer>();if(skin)skin.updateWhenOffscreen=true;foreach(Transform c in t)Layer(c);}
  static void Render(Camera cam,Vector3 anchor,string file,bool back=false){
   // Coroutine captures run before LateUpdate. Synchronize attachments and
   // grounding to the same current skeleton pose before rendering.
   foreach(var w in UnityEngine.Object.FindObjectsByType<EquippedWeapon>(FindObjectsSortMode.None)){w.SendMessage("LateUpdate");var r=w.GetComponentInChildren<MeshRenderer>();if(r)File.AppendAllText(Path.Combine(Folder,"capture-alignment.txt"),file+" state="+RealmGame.I.Player.Visual.CurrentState+" screen="+RealmGame.I.Screen+" weapon="+w.Id+" gripDistance="+Vector3.Distance(r.transform.TransformPoint(EquippedWeapon.GripPoint(w.Id)),w.CatchPosition)+"\n");}
   foreach(var ground in UnityEngine.Object.FindObjectsByType<EnemyGroundPose>(FindObjectsSortMode.None))ground.SnapPose();
   bool guardian=file.StartsWith("DesertGuardian");cam.transform.position=anchor+new Vector3(back?-3:3,guardian?3.5f:1.75f,back?-4:4);cam.transform.LookAt(anchor+Vector3.up*(guardian?2.2f:1.0f));
   var rt=new RenderTexture(700,700,24,RenderTextureFormat.ARGB32);var prev=RenderTexture.active;cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
   var tex=new Texture2D(700,700,TextureFormat.RGBA32,false);tex.ReadPixels(new Rect(0,0,700,700),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(Folder,file+".png"),tex.EncodeToPNG());RenderTexture.active=prev;cam.targetTexture=null;rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(tex);
  }
  public static IEnumerator Run(Action<bool,string> check){
   Directory.CreateDirectory(Folder);var g=RealmGame.I;g.LoadLevel(1);yield return new WaitForSeconds(.3f);
   foreach(var e in g.Enemies)e.enabled=false;g.Player.enabled=false;RenderSettings.fog=false;
   var camObj=new GameObject("New assets review camera");var cam=camObj.AddComponent<Camera>();cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.045f,.065f,.095f);cam.cullingMask=1<<31;cam.orthographic=true;cam.orthographicSize=1.9f;cam.nearClipPlane=.01f;cam.farClipPlane=20;
   var lightObj=new GameObject("Review key");var light=lightObj.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.1f;light.cullingMask=1<<31;lightObj.transform.rotation=Quaternion.Euler(35,-35,0);
   var report=new System.Text.StringBuilder();
   string[] roles={"IronGoblin","AshDemon","StoneBrute"};
   for(int k=0;k<roles.Length;k++){
    var host=new GameObject(roles[k]+" review");var v=CharacterVisual.Create(roles[k],host.transform,k==2?2.4f:k==1?1.95f:1.5f,Color.white);Layer(host.transform);
    check(v.animator&&v.animator.avatar&&v.animator.avatar.isHuman&&v.animator.avatar.isValid,roles[k]+" valid humanoid avatar");check(!v.UsesFallback,roles[k]+" uses the supplied mesh");
    var skin=v.GetComponentInChildren<SkinnedMeshRenderer>();check(skin&&skin.sharedMaterial&&skin.sharedMaterial.mainTexture,roles[k]+" textured skinned mesh");
    foreach(string state in new[]{"idle","walk","run","attack","hit","death"}){
     var clip=Resources.Load<AnimationClip>("Animations/"+roles[k]+"_"+state);check(clip&&clip.isHumanMotion&&clip.length>.1f,roles[k]+" retargeted clip "+state);
     v.Restart(state);yield return new WaitForSeconds(.3f);v.GetComponent<EnemyGroundPose>().SnapPose();var mesh=new Mesh();skin.BakeMesh(mesh,false);var a=mesh.vertices;
     Render(cam,host.transform.position,roles[k]+"-"+state);yield return new WaitForSeconds(.3f);v.GetComponent<EnemyGroundPose>().SnapPose();skin.BakeMesh(mesh,false);var b=mesh.vertices;float motion=0;
     for(int j=0;j<a.Length;j+=Mathf.Max(1,a.Length/200))motion=Mathf.Max(motion,Vector3.Distance(a[j],b[j]));
     check(motion>.00005f,roles[k]+" "+state+" visibly deforms the mesh");
     float min=float.PositiveInfinity;if(state=="death"){foreach(var vertex in b)min=Mathf.Min(min,skin.transform.TransformPoint(vertex).y-host.transform.position.y);}else foreach(int index in v.GetComponent<EnemyGroundPose>().SoleVertices)min=Mathf.Min(min,skin.transform.TransformPoint(b[index]).y-host.transform.position.y);check(min>-.08f&&min<.10f,roles[k]+" "+state+" feet stay on the actor ground plane: "+min);report.AppendLine(roles[k]+" "+state+" motion="+motion+" feet="+min);
     UnityEngine.Object.Destroy(mesh);
    }
    UnityEngine.Object.Destroy(host);yield return null;
   }
   Layer(g.Player.Visual.transform);
   foreach(var id in new[]{WeaponId.Duskblade,WeaponId.Soulreaper}){
    check(WeaponCatalog.IsEpic(id)&&WeaponCatalog.HasSpecialHold(id),id+" Epic special-hold weapon");check(!ChestSystem.GetStandardWeapons().Contains(id)&&!Array.Exists(ChestSystem.RareHoldWeapons,w=>w==id)&&Array.Exists(ChestSystem.EpicHoldWeapons,w=>w==id),id+" exclusive to Epic chest pool");
    g.EquipWeapon(id);g.Player.Visual.Restart("idle");Layer(g.Player.Visual.transform);yield return new WaitForSeconds(.2f);
    var w=g.Player.GetComponentInChildren<EquippedWeapon>();check(w&&w.Id==id,id+" equips without duplicates");var r=w.GetComponentInChildren<Renderer>();check(r&&r.sharedMaterial&&r.sharedMaterial.mainTexture,id+" texture is bound");report.AppendLine(id+" localbounds="+r.localBounds+" sheath="+r.bounds);
    Render(cam,g.Player.transform.position,id+"-sheathed",true);
    if(id==WeaponId.Soulreaper){var filter=r.GetComponent<MeshFilter>();var point=EquippedWeapon.GripPoint(id);float nearest=filter.sharedMesh.vertices.Min(vertex=>Vector3.Distance(vertex,point));check(nearest<.04f,"Soulreaper grip lies inside actual shaft geometry: "+nearest);check(point.y<-.15f,"Soulreaper grips the lower shaft, away from the blade head");}
    g.Player.Visual.PlayAttack(1,false,1,WeaponCatalog.AttackStyle(WeaponCatalog.Get(id)));yield return new WaitForSeconds(.24f);Render(cam,g.Player.transform.position,id+"-attack-1");
    yield return new WaitForSeconds(.2f);var grip=r.transform.TransformPoint(EquippedWeapon.GripPoint(id));check(Vector3.Distance(grip,w.CatchPosition)<.14f,id+" grip remains in Aster's hand");
    g.Player.Visual.PlayAttack(2,false);yield return new WaitForSeconds(.26f);Render(cam,g.Player.transform.position,id+"-attack-2");
    g.Player.Visual.PlayAttack(3,true);yield return new WaitForSeconds(.3f);Render(cam,g.Player.transform.position,id+"-charged");
   }
   g.Player.enabled=true;g.LoadLevel(2);yield return new WaitForSeconds(.25f);check(g.Enemies.Exists(e=>e&&e.Kind==22),"New goblin appears in Verdant chapter");
   g.LoadLevel(5);yield return new WaitForSeconds(.25f);check(g.Enemies.Exists(e=>e&&e.Kind==23),"New demon appears in desert chapter");
   g.LoadLevel(11);yield return new WaitForSeconds(.25f);check(g.Enemies.Exists(e=>e&&e.Kind==24),"Lava golem appears in Emberfall chapter");
   foreach(var enemy in g.Enemies.Where(e=>e&&e.Kind>=22).ToArray()){check(enemy.Controller&&enemy.Grounded,"New enemy uses grounded collision physics");check(enemy.Visual.animator&&enemy.Visual.animator.enabled,"New enemy animator stays active");enemy.Stun(2f);enemy.Hit(enemy.MaxHealth*.6f,-1,false);check(enemy.Visual.CurrentState=="hit","New enemy plays a hit response: "+enemy.Visual.CurrentState+" health="+enemy.Health);enemy.Hit(enemy.MaxHealth*4,-1,false);check(enemy.Health<=0&&enemy.Visual.CurrentState=="death","New enemy plays its death animation");}
   g.LoadLevel(1);yield return new WaitForSeconds(.3f);check(UnityEngine.Object.FindObjectsByType<WeaponDrop>(FindObjectsSortMode.None).Any(w=>w.Id==WeaponId.Duskblade)==(Array.IndexOf(Environment.GetCommandLineArgs(),"-epicPlaytest")>=0),"Chapter 1 Epic test pickup requires explicit playtest mode");
   g.LoadLevel(2);yield return new WaitForSeconds(.3f);check(UnityEngine.Object.FindObjectsByType<WeaponDrop>(FindObjectsSortMode.None).Any(w=>w.Id==WeaponId.Soulreaper)==(Array.IndexOf(Environment.GetCommandLineArgs(),"-epicPlaytest")>=0),"Chapter 2 Epic test pickup requires explicit playtest mode");
   cam.orthographicSize=3.4f;g.LoadLevel(7);yield return new WaitForSeconds(.4f);var guardian=g.Enemies.Find(e=>e&&e.Boss);
   check(guardian&&guardian.IsDesertLavaGuardian&&guardian.Visual.name.Contains("StoneBrute"),"Desert guardian uses supplied lava golem");
   check(guardian.Controller.height>4f,"Lava guardian has a matching enlarged collision body");
   check(g.Enemies.Exists(e=>e&&e.Kind==25&&!e.Boss&&e.Controller.height<2.5f),"Previous desert guardian appears as smaller regular foe");
   guardian.enabled=false;foreach(var state in new[]{"attack_2","victory","gethit","cast"}){var clip=Resources.Load<AnimationClip>("Animations/StoneBrute_"+state);check(clip&&clip.isHumanMotion,"Lava guardian special animation "+state);guardian.Visual.Restart(state);yield return new WaitForSeconds(.3f);Layer(guardian.Visual.transform);Render(cam,guardian.transform.position,"DesertGuardian-"+state);var skin=guardian.Visual.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=new Mesh();skin.BakeMesh(mesh,false);var before=mesh.vertices;yield return new WaitForSeconds(.25f);skin.BakeMesh(mesh,false);var after=mesh.vertices;float motion=0;for(int j=0;j<before.Length;j+=50)motion=Mathf.Max(motion,Vector3.Distance(before[j],after[j]));check(motion>.00005f,"Lava guardian special pose deforms mesh: "+state);UnityEngine.Object.Destroy(mesh);}
   foreach(var state in new[]{"attack","attack_2","cast","victory","slam","sweep","stomp","eruption","roar","charge_ready"}){
    var clip=Resources.Load<AnimationClip>("Animations/StoneBrute_"+state);var skin=guardian.Visual.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=new Mesh();
    guardian.Visual.Play("idle");yield return new WaitForSeconds(.5f);
    guardian.Visual.PlayTimed(state,1.2f);float lowest=float.PositiveInfinity,hipLow=float.PositiveInfinity,soleHigh=float.NegativeInfinity;
    for(int frame=0;frame<16;frame++){
     yield return new WaitForSeconds(.1f);guardian.Visual.GetComponent<EnemyGroundPose>().SnapPose();skin.BakeMesh(mesh,false);
     foreach(var vertex in mesh.vertices)lowest=Mathf.Min(lowest,skin.transform.TransformPoint(vertex).y-guardian.transform.position.y);
     float hip=guardian.Visual.animator.GetBoneTransform(HumanBodyBones.Hips).position.y-guardian.transform.position.y;hipLow=Mathf.Min(hipLow,hip);
     float sole=float.PositiveInfinity;var vertices=mesh.vertices;foreach(int index in guardian.Visual.GetComponent<EnemyGroundPose>().SoleVertices)sole=Mathf.Min(sole,skin.transform.TransformPoint(vertices[index]).y-guardian.transform.position.y);soleHigh=Mathf.Max(soleHigh,sole);
     if(frame==3||frame==7||frame==11){Layer(guardian.Visual.transform);Render(cam,guardian.transform.position,"DesertGuardian-cycle-"+state+"-"+frame);}
    }
    guardian.Visual.Play("idle");yield return new WaitForSeconds(.3f);
    check(lowest>-.03f,"Lava guardian full "+state+" cycle has no body penetration: "+lowest);
    check(hipLow>.8f&&soleHigh<.15f,"Lava guardian full "+state+" cycle keeps a planted foot and upright body: hip="+hipLow+" sole="+soleHigh);
    UnityEngine.Object.Destroy(mesh);
   }
   guardian.enabled=true;g.Player.Warp(guardian.transform.position-guardian.transform.forward*6f);g.Player.Health=g.Player.MaxHealth;int eruptions=guardian.LavaEruptionCount;
   typeof(Enemy).GetMethod("BeginDesertPattern",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(guardian,new object[]{3});
   yield return new WaitForSeconds(3f);check(guardian.LavaEruptionCount>eruptions,"Lava guardian executes its telegraphed eruption attack");
   float health=guardian.Health;guardian.HitByTrap(999,0,true,Vector3.zero);check(guardian.Health==health,"Desert lava guardian remains immune to traps");
   var save=new Progress();save.bestiary=new int[22];save.bestiary[0]=7;Codex.Normalize(save);check(save.bestiary.Length==Codex.Kinds&&save.bestiary[0]==7,"Bestiary expands without losing discoveries");
   File.WriteAllText(Path.Combine(Folder,"pose-motion.txt"),report.ToString());UnityEngine.Object.Destroy(camObj);UnityEngine.Object.Destroy(lightObj);
  }
 }
}
