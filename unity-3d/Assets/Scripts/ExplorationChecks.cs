using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace LostRealms {
 public static class ExplorationChecks {
  static string folder;
  static void Capture(Vector3 center,string name,float size){
   var go=new GameObject("Exploration review camera");var cam=go.AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=size;cam.farClipPlane=350;cam.transform.position=center+new Vector3(20,27,-28);cam.transform.LookAt(center);
   cam.clearFlags=CameraClearFlags.Skybox;var rt=new RenderTexture(1400,900,24);cam.targetTexture=rt;var prior=RenderTexture.active;cam.Render();RenderTexture.active=rt;var image=new Texture2D(1400,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1400,900),0,0);image.Apply();File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());RenderTexture.active=prior;cam.targetTexture=null;rt.Release();UnityEngine.Object.Destroy(image);UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(go);
  }
  public static IEnumerator Run(Action<bool,string> check,Action<Vector2> move,Action jump){
   var g=RealmGame.I;string saved=JsonUtility.ToJson(g.Save);folder="Validation/Exploration/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(folder);
   g.Save=new Progress{unlocked=25,healthRank=3};Campaign.Normalize(g.Save);
   bool preview=Array.IndexOf(Environment.GetCommandLineArgs(),"-explorationPreview")>=0;
   bool layoutOnly=Array.IndexOf(Environment.GetCommandLineArgs(),"-explorationLayout")>=0;
   for(int chapter=Array.IndexOf(Environment.GetCommandLineArgs(),"-astralExploration")>=0?21:16;chapter<=25;chapter++){
    g.LoadLevel(chapter);yield return new WaitForSeconds(.7f);var world=g.World;var exp=world.Exploration;
    check(exp&&exp.Sites.Count==12,"Exploration districts exist: "+chapter);check(g.Player.Grounded,"Supported entry: "+chapter);
    var seen=new HashSet<int>{0};bool changed=true;while(changed){changed=false;foreach(var e in exp.Links){if(seen.Contains(e.x)&&seen.Add(e.y))changed=true;if(seen.Contains(e.y)&&seen.Add(e.x))changed=true;}}
    check(seen.Count==exp.Sites.Count&&exp.Links.Count>=exp.Sites.Count,"Connected exploration network contains loops: "+chapter);
    var bounds=new Bounds(world.Spawn,Vector3.zero);foreach(var site in exp.Sites)bounds.Encapsulate(site.transform.position);check(bounds.size.x>70&&bounds.size.z>70,"Routes explore two world axes: "+chapter);
    var kinds=new HashSet<int>();foreach(var enemy in g.Enemies){kinds.Add(enemy.Kind);check(enemy.Visual&&!enemy.Visual.UsesFallback&&enemy.Grounded,"Visible grounded encounter "+chapter+" / "+enemy.Kind);enemy.enabled=false;}
    check(kinds.Count>=5,"Mixed enemy roles: "+chapter);check(world.GetComponentsInChildren<MovingIsland>().Length>=6,"Moving crossings and elevators: "+chapter);
    int traps=0;foreach(var t in world.GetComponentsInChildren<Transform>())if(ChapterLayout.IsTrap(t))traps++;check(traps>=3,"Imported traps populate encounter districts: "+chapter);
    check(exp.Finds.FindAll(f=>f.Key).Count==3&&!g.GateOpen(),"Three separated keys seal the exit: "+chapter);
    foreach(var prop in world.GetComponentsInChildren<Transform>())if(prop.name=="Prop Storm tower"||prop.name=="Prop Astral Arch"||prop.name=="Prop Astral Orrery"){
     if(!ChapterLayout.BoundsOf(prop,out var a))continue;
     a.center=new Vector3(a.center.x,0,a.center.z);a.size=new Vector3(a.size.x,1,a.size.z);
     foreach(var trap in prop.parent.GetComponentsInChildren<Transform>())if(ChapterLayout.IsTrap(trap)&&ChapterLayout.BoundsOf(trap,out var b)){
      b.center=new Vector3(b.center.x,0,b.center.z);b.size=new Vector3(b.size.x,1,b.size.z);
      check(!ChapterLayout.Overlap(a,b),"Landmark clears trap footprint: "+chapter+" / "+prop.name+" / "+trap.name);
     }
    }
    Capture(exp.Sites[1].transform.position,"chapter-"+chapter+"-crossroads",27);Capture(exp.Sites[chapter<21?2:3].transform.position,"chapter-"+chapter+"-objective",16);
    if(Array.IndexOf(Environment.GetCommandLineArgs(),"-realmQuality")>=0){
     QualityLayout(check,g,chapter);
     Capture(exp.GatePosition,"chapter-"+chapter+"-gate",7);
     if(g.Storm)foreach(var c in g.Storm.Circuits)Capture(c.transform.position,"chapter-"+chapter+"-court-"+c.Index,9);
     var mat=chapter<21?StormArt.GetMaterial("Terrace"):AstralArt.GetMaterial("Terrace");
     mat.DisableKeyword("_NORMALMAP");Capture(exp.Sites[chapter<21?2:3].transform.position,"chapter-"+chapter+"-flat-normal",9);mat.EnableKeyword("_NORMALMAP");
     if(chapter==16){
      var c=g.Storm.Circuits[0];var obj=new GameObject("Arrow direction inspection");var line=obj.AddComponent<LineRenderer>();line.sharedMaterial=StormArt.Beam;line.positionCount=2;line.startWidth=line.endWidth=.025f;
      var p=c.PadPosition(0,0)+Vector3.up*.3f;line.SetPosition(0,p);line.SetPosition(1,p+c.BeamDirection(0,0)*.8f);Capture(p,"arrow-direction",1.15f);UnityEngine.Object.Destroy(obj);
      yield return QualityWind(check);
     }
    }
    if(layoutOnly)continue;
    if(preview){if(chapter==16){chapter=20;continue;}break;}
    // Navigation checks isolate traversal from combat. Hazard functionality is tested separately by the stage suites.
    foreach(var b in world.GetComponentsInChildren<MonoBehaviour>())if(ChapterLayout.IsTrap(b.transform)||b is StormWindWindow||b is AstralFracture)b.enabled=false;
    if(Array.IndexOf(Environment.GetCommandLineArgs(),"-crossingProbe")>=0){
     foreach(int route in new[]{0,2,3,4})yield return Cross(exp.Crossings[route],check,move,jump,"crossing pattern "+route);
     break;
    }
    if(chapter==16||chapter==21){
     yield return Cross(exp.Crossings[0],check,move,jump,chapter+" stepping route");
     yield return Cross(exp.Crossings.Find(c=>c.Ferry),check,move,jump,chapter+" moving route");
     if(Array.IndexOf(Environment.GetCommandLineArgs(),"-realmQuality")>=0){
      yield return Cross(exp.Crossings[3],check,move,jump,chapter+" orbit route");
      yield return Cross(exp.Crossings[4],check,move,jump,chapter+" figure-eight route");
     }
     var mover=world.GetComponentInChildren<MovingIsland>();g.Player.Warp(mover.transform.position+Vector3.up*.05f);yield return new WaitForSeconds(.3f);var rel=g.Player.transform.position-mover.transform.position;yield return new WaitForSeconds(1);check(Vector3.Distance(g.Player.transform.position-mover.transform.position,rel)<.4f,"Moving platform carries Aster without slipping: "+chapter);
     var stopped=mover.transform.position;g.Pause();yield return new WaitForSecondsRealtime(.2f);check(Vector3.Distance(stopped,mover.transform.position)<.01f,"Pause freezes new platforms");g.Resume();
    }
    foreach(var enemy in g.Enemies)if(enemy){enemy.Health=0;enemy.gameObject.SetActive(false);}
    foreach(var find in exp.Finds)if(find.Key){g.Player.Warp(find.transform.position+Vector3.back*1.1f+Vector3.up*.05f);yield return new WaitForSeconds(1);check(find.Claimed,"Physical proximity collects key: "+chapter+" / "+find.Title);}
    check(exp.Keys==3&&!g.GateOpen(),"Keys alone cannot bypass chapter puzzles: "+chapter);
    if(g.Storm)foreach(var circuit in g.Storm.Circuits){for(int round=0;round<circuit.Rounds;round++){for(int i=0;i<circuit.Relays.Count;i++)circuit.Turn(i,circuit.CorrectFor(i));yield return new WaitForSeconds(1.3f);}
check(circuit.Completed,"Conductor court completes "+chapter+" / "+circuit.Index);}
    if(g.Astral)foreach(var route in g.Astral.Mechanisms){for(int side=0;side<2;side++){g.Player.Warp(route.transform.position+Vector3.back*4);route.RequestShift(side);yield return new WaitForSeconds(1);g.Player.Warp(route.Focuses[side].position+Vector3.back*1.3f+Vector3.up*.05f);yield return new WaitForSeconds(1.3f);check(route.Attuned[side],"Physical court attunement: "+chapter+" / "+route.Index+" / "+side);}}
    check(g.GateOpen(),"Keys and completed puzzles open the gate: "+chapter);yield return new WaitForSeconds(.1f);check(exp.GateRevealed,"Completed goals reveal the exit: "+chapter);
    int coins=g.Coins;var cache=exp.Finds.Find(f=>!f.Key);g.Player.Warp(cache.transform.position+Vector3.back*1.2f+Vector3.up*.05f);yield return new WaitForSeconds(1.1f);check(cache.Claimed&&g.Coins==coins+60,"Optional cache grants a discovery reward");yield return new WaitForSeconds(.8f);check(g.Coins==coins+60,"Cache cannot be farmed by waiting");
   }
   move(Vector2.zero);g.Save=JsonUtility.FromJson<Progress>(saved);Campaign.Normalize(g.Save);g.LoadLevel(1);yield return new WaitForSeconds(.3f);check(g.Player.Grounded,"Original realm still starts grounded");
  }
  static void QualityLayout(Action<bool,string> check,RealmGame g,int chapter){
   var exp=g.World.Exploration;var styles=new HashSet<IslandMotionStyle>();
   foreach(var moving in g.World.GetComponentsInChildren<MovingIsland>()){
    styles.Add(moving.Style);var lo=moving.Origin;var hi=lo;float maxSpeed=0;var previous=moving.ExplorationPosition(0);
    for(float t=.04f;t<=2*(moving.TravelTime+moving.DwellTime);t+=.04f){var pos=moving.ExplorationPosition(t);lo=Vector3.Min(lo,pos);hi=Vector3.Max(hi,pos);maxSpeed=Mathf.Max(maxSpeed,Vector3.Distance(previous,pos)/.04f*moving.Speed);previous=pos;}
    check((hi-lo).magnitude>4&&maxSpeed<4,"Wide smooth crossing path: "+chapter+" / "+moving.Style);
    check(Vector3.Distance(moving.ExplorationPosition(.7f),moving.Origin)<.01f,"Crossing has an actual docking pause");
   }
   check(styles.Count==3,"Three distinct three-dimensional crossing patterns: "+chapter);
   var gate=g.World.GetComponentInChildren<TeleportGateController>();
   check(gate&&gate.GateModel.Find("Visual_"+(chapter<21?"Storm":"Astral")),"Realm uses its supplied gate model: "+chapter);
   check(gate&&gate.GateModel.GetComponentsInChildren<MeshCollider>().Length>0,"Gate collision follows the supplied open arch");
   if(g.Storm)foreach(var circuit in g.Storm.Circuits){
    for(int relay=0;relay<circuit.Relays.Count;relay++)for(int choice=0;choice<3;choice++){
     var pad=circuit.PadPosition(relay,choice);bool clear=true;
     foreach(var col in Physics.OverlapCapsule(pad+Vector3.up*.35f,pad+Vector3.up*1.4f,.3f,~0,QueryTriggerInteraction.Ignore)){
      if(col.GetComponentInParent<Hero>()||col.GetComponentInParent<Enemy>())continue;
      if(col.GetComponentInParent<ExplorationSite>()==null)continue;
      if(col.transform.IsChildOf(circuit.transform)&&col.bounds.max.y>pad.y+.32f)clear=false;
     }
     check(clear,"Conductor control is not covered or blocked: "+chapter+" / "+circuit.Index+" / "+relay+" / "+choice);
    }
   }
  }
  static IEnumerator QualityWind(Action<bool,string> check){
   var g=RealmGame.I;var wind=g.World.GetComponentInChildren<StormWindWindow>();if(!wind)yield break;
   var mover=wind.GetComponent<MovingIsland>();if(mover)mover.enabled=false;
   var start=wind.transform.position+Vector3.up*.08f;
   wind.PhaseOffset=.1f-g.Elapsed;g.Player.Warp(start);yield return new WaitForSeconds(.7f);
   check(Vector3.Distance(g.Player.transform.position,start)<.35f&&g.Player.Grounded,"Green wind window permits a stable crossing");
   wind.PhaseOffset=4.05f-g.Elapsed;g.Player.Warp(start);float until=g.Elapsed+1.5f,maxTravel=0;bool leftDeck=false;
   while(g.Elapsed<until&&g.Screen==GameScreen.Playing&&!leftDeck){var delta=g.Player.transform.position-start;delta.y=0;maxTravel=Mathf.Max(maxTravel,delta.magnitude);if(g.Player.transform.position.y<start.y-.4f)leftDeck=true;yield return null;}
   check(maxTravel>2.4f&&leftDeck,"Red gust can knock an idle player off the island: travel="+maxTravel+" fell="+leftDeck);
   g.Player.Warp(g.World.Spawn);if(g.Screen==GameScreen.Defeated){g.Respawn();g.Resume();}if(mover)mover.enabled=true;
  }
  public static IEnumerator Cross(ExplorationCrossing crossing,Action<bool,string> check,Action<Vector2> move,Action jump,string title){
   var g=RealmGame.I;g.Player.Warp(crossing.Stops[0].position+Vector3.up*.08f);yield return new WaitForSeconds(.25f);int jumps=0;
   for(int i=1;i<crossing.Stops.Count;i++){
    // A computer sleep or editor stall must not consume the simulated crossing window.
    var target=crossing.Stops[i];float until=g.Elapsed+30,nextJump=0;bool second=false;
    while(g.Elapsed<until&&g.Screen==GameScreen.Playing){var delta=target.position-g.Player.transform.position;delta.y=0;if(delta.magnitude<.8f&&g.Player.Grounded)break;
     var direction=delta.normalized;var input=Quaternion.Euler(0,-g.CameraRig.Yaw,0)*direction;move(new Vector2(input.x,input.z));
     bool gap=!Physics.Raycast(g.Player.transform.position+direction*1.7f+Vector3.up*.35f,Vector3.down,out var ahead,1.2f,~0,QueryTriggerInteraction.Ignore)||ahead.normal.y<.55f;
     if(g.Player.Grounded){second=false;
      var site=target.GetComponent<ExplorationSite>();float reachable=site?site.Width*.42f+5.5f:8.4f;
      if(gap&&(delta.magnitude>reachable||target.position.y-g.Player.transform.position.y>1.7f)){move(Vector2.zero);yield return null;continue;}
      if(gap&&g.Elapsed>nextJump){Debug.Log("TRAVERSAL_TAKEOFF "+title+" position="+g.Player.transform.position+" target="+target.position+" frame="+Time.deltaTime);jump();jumps++;nextJump=g.Elapsed+.32f;}}
     else if(!second&&g.Player.VerticalVelocity<1.5f&&g.Elapsed>nextJump){jump();second=true;nextJump=g.Elapsed+.5f;}
     yield return null;
    }
    move(Vector2.zero);yield return new WaitForSeconds(.15f);check(Vector3.Distance(g.Player.transform.position,target.position)<2&&g.Player.Grounded,"Physical crossing reaches landing: "+title+" / "+i+" at "+g.Player.transform.position+" target "+target.position+" jumps="+jumps+" screen="+g.Screen);
   }
   check(jumps>0,"Crossing requires actual jumps: "+title);
  }
 }
}
