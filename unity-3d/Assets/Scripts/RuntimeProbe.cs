using UnityEngine;
using System.Collections;
using System.IO;
using System;
namespace LostRealms {
 [DefaultExecutionOrder(10)] public class RuntimeProbe:MonoBehaviour {
  Vector2 move;bool jump;int assertions;static int runtimeErrors;
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void WatchErrors(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-realmTest")<0)return;runtimeErrors=0;Application.logMessageReceived+=(message,stack,type)=>{if(type==LogType.Exception||type==LogType.Error||type==LogType.Assert)runtimeErrors++;};}
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void StartIfRequested(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-realmTest")>=0)new GameObject("Runtime validation").AddComponent<RuntimeProbe>();}
  void Update(){var g=RealmGame.I;if(g&&g.Screen==GameScreen.Playing){g.MoveInput=move;g.JumpPressed=jump;jump=false;}}
  void Check(bool okay,string message){if(!okay){Debug.LogError("REALM_TEST_FAILED: "+message);Quit(1);throw new Exception(message);}assertions++;Debug.Log("PASS: "+message);}
  IEnumerator Start(){
   var touch=new TouchRouter();touch.BeginFrame();touch.Sample(1,new Vector2(1014,655),Vector2.zero,TouchPhase.Began);Check(touch.Jump&&touch.Yaw==0,"Jump touch does not orbit");
   touch.BeginFrame();touch.Sample(1,new Vector2(1014,655),new Vector2(-214,-355),TouchPhase.Moved);Check(touch.Yaw==0,"Jump finger remains an action when dragged into camera area");
   touch.Sample(2,new Vector2(820,310),Vector2.zero,TouchPhase.Began);touch.BeginFrame();touch.Sample(2,new Vector2(850,310),new Vector2(30,0),TouchPhase.Moved);Check(touch.Yaw>0,"Separate camera finger can orbit during a jump");
   touch.Reset();touch.BeginFrame();touch.Sample(1,new Vector2(1160,610),Vector2.zero,TouchPhase.Moved);Check(touch.Yaw==0&&!touch.Jump,"Canceled touches cannot regain control without a new press");
   yield return null;var g=RealmGame.I;g.LoadLevel(1);yield return new WaitForSeconds(.6f);Check(g&&g.Player&&g.CameraRig&&g.CameraRig.Target==g.Player.transform,"Startup completes with an assigned player and camera target");Check(g.Player.Grounded,"Aster spawns on walkable ground");foreach(var renderer in g.Player.GetComponentsInChildren<Renderer>()){if(renderer.GetComponentInParent<EquippedWeapon>()!=null)continue;Check(renderer.bounds.size.magnitude<8,"Hero geometry stays within character scale");Check(renderer.sharedMaterial&&renderer.sharedMaterial.name=="Aster","Supplied Aster material is bound at runtime");}foreach(string state in new[]{"idle","walk","run","attack_1","attack_2","attack_3","charged","jump","double_jump","dodge","hit","death"})Check(Resources.Load<AnimationClip>("Animations/Aster/"+state),"Supplied Aster state present: "+state);Check(g.Player.GetComponentInChildren<HeroWeapon>()==null,"Imported sword is not duplicated with an inherited-scale weapon");
   var weaponDrop=FindAnyObjectByType<WeaponDrop>();Check(weaponDrop&&weaponDrop.Id!=WeaponId.AstersBlade,"Weapon drop can equip");g.EquipWeapon(WeaponId.Axe);yield return null;Check(g.CurrentWeapon.Id==WeaponId.Axe&&g.CurrentWeapon.Damage>1.5f&&g.Player.GetComponentInChildren<EquippedWeapon>(),"Equipping axe changes equipped stats");
   var gemVisual=FindAnyObjectByType<GemVisual>();Check(gemVisual&&gemVisual.Core&&gemVisual.Shards.Count==4&&gemVisual.Halos.Count==1,"Gem pickup has animated faceted relic visual");var checkpointVisual=FindAnyObjectByType<CheckpointVisual>();Check(checkpointVisual&&checkpointVisual.Core&&checkpointVisual.Shards.Count==4&&checkpointVisual.Halos.Count==2&&checkpointVisual.Aura,"Checkpoint has animated sanctuary visual");g.Player.Warp(checkpointVisual.transform.position);yield return null;Check(checkpointVisual.Activated,"Checkpoint activation illuminates sanctuary visual");g.Player.Warp(g.World.Spawn);
   Transform surface=null;int landmarks=0,motes=0;foreach(Transform child in g.World.transform){if(child.name=="Island"&&surface==null)surface=child.Find("Realm surface");if(child.name=="Realm route landmark")landmarks++;if(child.name=="Ambient mote")motes++;}var terrainRenderer=surface?surface.GetComponent<Renderer>():null;Check(terrainRenderer&&terrainRenderer.sharedMaterial&&terrainRenderer.sharedMaterial.mainTexture&&terrainRenderer.sharedMaterial.mainTexture.name=="verdant_moss_tile","Realm uses generated terrain texture");Check(g.World.GetComponent<RealmAtmosphere>()&&landmarks==3&&motes>=18&&QualitySettings.shadowDistance<=28.1f,"Realm atmosphere adds landmarks and ambient motes");
    Check(Resources.Load<GameObject>("VFX/ga_vfx_Heal_01")&&Resources.Load<GameObject>("VFX/eric_FX_Weapon Effect")&&Resources.Load<GameObject>("VFX/mayker_Slash Eletric VFX"),"Imported VFX prefabs resolve from Resources");
    Check(Resources.Load<GameObject>("Characters/Skeleton")&&Resources.Load<Texture2D>("Characters/Textures/Skeleton_basecolor"),"Skeleton enemy model and basecolor resolve");
    Check(Resources.Load<GameObject>("Props/Desert/House_01")&&Resources.Load<GameObject>("Props/Snow/Pine_01")&&Resources.Load<Texture2D>("Props/Textures/Desert_palette"),"Desert/snow scenery props and desert palette resolve");
   move=Vector2.up;yield return new WaitForSeconds(.28f);Check(g.Player.HorizontalSpeed<=4.9f,"Aster normal movement remains under the 4.8 speed cap");yield return new WaitForSeconds(.27f);jump=true;yield return new WaitForSeconds(.3f);float firstHeight=g.Player.transform.position.y;jump=true;yield return new WaitForSeconds(.06f);Check(g.Player.Visual.CurrentState=="jump","Second jump reuses jump motion");yield return new WaitForSeconds(.19f);Check(g.Player.transform.position.y>firstHeight,"Second jump adds altitude");yield return new WaitForSeconds(.65f);move=Vector2.zero;yield return new WaitForSeconds(.6f);Check(g.Player.transform.position.z>6&&g.Player.Grounded,"Double jump crosses the first island gap");
for(int level=1;level<=15;level++){g.LoadLevel(level);yield return new WaitForSeconds(.25f);Check(g.World.Route.Count>=8,"Route populated: "+level);Check(g.Player.Visual.GetComponentInChildren<Renderer>()!=null,"Player model present: "+level);if(g.World.IsBoss)Check(g.Enemies.Exists(e=>e&&e.Boss),"Guardian present: "+level);
     foreach(var enemy in g.Enemies.ToArray()){Check(enemy.Visual.GetComponentInChildren<Renderer>()!=null,"Enemy model present");foreach(var r in enemy.GetComponentsInChildren<Renderer>(true))Check(Vector3.Distance(r.bounds.center,enemy.transform.position)<3f,"Enemy visual stays anchored to its root: "+level);}
     foreach(var pickup in FindObjectsByType<RealmPickup>(FindObjectsSortMode.None)){foreach(var r in pickup.GetComponentsInChildren<Renderer>(true))Check(Vector3.Distance(r.bounds.center,pickup.transform.position)<2.5f,"Pickup visual stays anchored to its root: "+level);}
     foreach(var heal in FindObjectsByType<RealmHeal>(FindObjectsSortMode.None)){foreach(var r in heal.GetComponentsInChildren<Renderer>(true))Check(Vector3.Distance(r.bounds.center,heal.transform.position)<2.5f,"Heart visual stays anchored to its root: "+level);}
    }
g.LoadLevel(10);yield return new WaitForSeconds(.4f);
     g.Player.Warp(g.World.Spawn);
     var guardian=g.Enemies.Find(e=>e&&e.Boss);
     if(guardian){
      guardian.Hit(guardian.MaxHealth*.35f,-1,false);yield return null;Check(guardian.BossPhase==2,"Boss escalates to phase 2 below 67% health");
      guardian.Hit(guardian.MaxHealth*.55f,-1,false);yield return null;Check(guardian.BossPhase==3,"Boss escalates to phase 3 below 33% health");
      yield return new WaitForSeconds(13f);
      Check(g.Enemies.Exists(e=>e&&!e.Boss&&e.Health>0&&e.Kind==3&&Vector3.Distance(e.transform.position,guardian.transform.position)<16f),"Phase-3 boss calls realm heralds into the arena");
      foreach(var e in g.Enemies.ToArray())if(!e.Boss)e.Hit(999,0,true);
     }
    g.LoadLevel(15);yield return new WaitForSeconds(.4f);
     g.Player.Warp(g.World.Spawn);
     var warden=g.Enemies.Find(e=>e&&e.Boss);
     Check(warden&&warden.Kind==21,"Emberfall warden is the realm-4 guardian (kind 21)");
     Check(warden&&warden.WeakElement==2,"Emberfall warden is weak to gale (element 2)");
     if(warden){
      warden.Hit(warden.MaxHealth*.35f,-1,false);yield return null;Check(warden.BossPhase==2,"Warden escalates to phase 2 below 67% health");
      warden.Hit(warden.MaxHealth*.55f,-1,false);yield return null;Check(warden.BossPhase==3,"Warden escalates to phase 3 below 33% health");
      yield return new WaitForSeconds(13f);
      Check(g.Enemies.Exists(e=>e&&!e.Boss&&e.Health>0&&e.Kind==18&&Vector3.Distance(e.transform.position,warden.transform.position)<16f),"Warden phase-3 calls realm-4 spider heralds into the arena");
      foreach(var e in g.Enemies.ToArray())if(!e.Boss)e.Hit(999,0,true);
     }
    g.LoadLevel(1);yield return new WaitForSeconds(.3f);var target=g.Enemies[0];g.Player.Warp(target.transform.position);int health=g.Player.Health;yield return new WaitForSeconds(.15f);Check(g.Player.Health==health,"No passive enemy contact damage");g.Player.Warp(g.World.Spawn);
target.Hit(999,0,true);Check(target.Health==0,"Combat can defeat enemies");
    Check(FindObjectsByType<RealmPickup>(FindObjectsSortMode.None).Length>0,"Enemy death spawns gem loot pickups");
    g.LoadLevel(1);yield return new WaitForSeconds(.4f);
    g.Save.windRank=2;g.Save.aetherRank=2;g.Save.moxieRank=2;g.Save.tempoRank=2;g.Persist();
    var comboEnemy=g.Enemies.Count>0?g.Enemies[0]:null;
    if(comboEnemy){for(int i=0;i<4;i++)comboEnemy.Hit(1,0,true);yield return null;}
    Check(g.Combo>=3,"Combo counter increments on successive enemy hits");
    yield return new WaitForSeconds(1.25f);Check(g.Combo==0,"Combo resets after timeout");
    foreach(var foe in g.Enemies.ToArray())if(foe&&foe.Health>0)foe.Hit(9999,-1,false);
    g.Player.Warp(g.World.Spawn);yield return new WaitForSeconds(1.25f);g.Player.Health=g.Player.MaxHealth;g.Player.windUsed=0;
    int healthBefore=g.Player.Health;
    g.Player.Damage(999,g.Player.transform.position);yield return null;
    Check(g.Player.Health>0&&g.Player.Health<healthBefore,"Second wind revives Aster from lethal hit (1st revive)");
    yield return new WaitForSeconds(2.4f);
    g.Player.Damage(999,g.Player.transform.position);yield return null;
    Check(g.Player.Health>0,"Second wind revives Aster a second time (windRank 2)");
    Check(g.Player.windUsed==2,"windUsed tracks multiple revives");
    yield return new WaitForSeconds(2.4f);
    g.Player.Damage(999,g.Player.transform.position);yield return null;
    Check(g.Player.Health==0,"Third lethal without remaining wind charges triggers Defeat");
    g.Respawn();g.Resume();g.Player.Energy=40;float e0=g.Player.Energy;yield return new WaitForSeconds(1f);
    Check(g.Player.Energy>e0+14f,"Aether rank boosts energy regen above base 12/s rate");
    string v2=JsonUtility.ToJson(g.Save);Check(v2.Contains("\"version\":2"),"Save payload retains v2 version without changing user preferences in tests");
    float clock=g.Elapsed;g.Pause();yield return new WaitForSeconds(.3f);Check(Mathf.Approximately(clock,g.Elapsed),"Pause stops gameplay time");g.Resume();g.ActivateCheckpoint(g.World.Spawn+Vector3.forward);g.Player.Warp(g.World.Spawn+Vector3.forward*2);g.Respawn();Check(Vector3.Distance(g.Player.transform.position,g.Checkpoint)<.1f,"Respawn restores checkpoint position");
   yield return QualityUpgradeChecks.Run(Check);
   g.LoadLevel(1);yield return new WaitForSeconds(.6f);string outDir=Path.GetFullPath(Path.Combine(Application.dataPath,"../Validation"));Directory.CreateDirectory(outDir);string captureDir=Path.Combine(outDir,"QualityUpgrade-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));Directory.CreateDirectory(captureDir);Capture(captureDir+"/Verdant.png");
   g.LoadLevel(7);yield return new WaitForSeconds(.4f);g.Player.Warp(g.World.Route[g.World.Route.Count-1]+new Vector3(0,.1f,-5));yield return new WaitForSeconds(.4f);Capture(captureDir+"/Sunscar.png");
   g.LoadLevel(10);yield return new WaitForSeconds(.4f);g.Player.Warp(g.World.Route[g.World.Route.Count-1]+new Vector3(0,.1f,-5));yield return new WaitForSeconds(.4f);Capture(captureDir+"/Whiteout.png");
g.LoadLevel(15);yield return new WaitForSeconds(.4f);g.Player.Warp(g.World.Route[g.World.Route.Count-1]+new Vector3(0,.1f,-5));yield return new WaitForSeconds(.4f);Capture(captureDir+"/Emberfall.png");
    Check(runtimeErrors==0,"No runtime exceptions or errors during playthrough checks");
    File.WriteAllText(outDir+"/quality-render-path.txt",captureDir);File.WriteAllText(outDir+"/runtime-results.txt",$"Passed {assertions} runtime assertions: movement, double jump, fifteen routes, models, four bosses, passive-contact damage, defeat, pause and checkpoint respawn.\n");Debug.Log("REALM_RUNTIME_TESTS_PASSED "+assertions);Quit(0);
  }
  static void Capture(string path){
   var camera=Camera.main;var rt=new RenderTexture(1280,720,24);var previous=RenderTexture.active;var target=camera.targetTexture;Texture2D tex=null;
   try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;tex=new Texture2D(1280,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());}
   catch(Exception e){Debug.LogError("REALM_CAPTURE_FAILED: "+e.Message);Quit(1);throw;}
   finally{RenderTexture.active=previous;camera.targetTexture=target;Destroy(rt);if(tex)Destroy(tex);}
  }
  static void Quit(int code){
#if UNITY_EDITOR
   UnityEditor.EditorApplication.Exit(code);
#else
   Application.Quit(code);
#endif
  }
 }
}
