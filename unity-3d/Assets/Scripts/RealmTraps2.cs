using UnityEngine;
using UnityEngine.Rendering;
namespace LostRealms {
 // Second trap generation (Sep 2026 difficulty pass): CrusherPillar, DartTurret,
 // RollingBoulder and WindVent. The first three use Blender-built low-poly
 // models under Resources/Props/Traps with named parts (Stone*/Trim*/Runes*/Mouth/
 // Rock*) tinted per realm at place time; every trap falls back to procedural
 // Art.Shape visuals when the model is missing, so nothing breaks in stripped
 // builds. All follow the TrapsAndHazards conventions: static Place(), phase
 // state machines, g.Player.Damage for the hero and foe.Hit for enemies.
 public static class TrapArt {
  // Trap spots reserve space so RealmProps never scatters a prop through one.
  public struct ReservedSpot{public Transform island;public Vector3 local;public float radius;}
  public static readonly System.Collections.Generic.List<ReservedSpot> Reserved=new System.Collections.Generic.List<ReservedSpot>();
  public static void Reserve(Transform island,Vector3 local,float radius){Reserved.Add(new ReservedSpot{island=island,local=local,radius=radius});}
  public static bool IsClear(Transform island,Vector3 local,float radius){
   for(int i=0;i<Reserved.Count;i++){
    var s=Reserved[i];
    if(!s.island||s.island!=island)continue;
    if(Vector3.Distance(s.local,local)<s.radius+radius)return false;
   }
   return true;
  }
  public static GameObject Load(string name,Transform parent,Color accent,Color stone){
   var prefab=Resources.Load<GameObject>("Props/Traps/"+name);
   if(!prefab)return null;
   var go=Object.Instantiate(prefab,parent);
   go.name=name;
   go.transform.localPosition=Vector3.zero;
   go.transform.localRotation=Quaternion.identity;
   foreach(var r in go.GetComponentsInChildren<Renderer>(true)){
    string n=r.gameObject.name;
    Color c;
    if(n.StartsWith("Trim")||n.StartsWith("Runes"))c=Color.Lerp(accent,Color.white,.2f);
    else if(n.StartsWith("Mouth"))c=new Color(.05f,.04f,.05f);
    else if(n.StartsWith("Rock"))c=stone*.92f;
    else c=stone;
    var m=new Material(Shader.Find("Standard")){name="TrapMat "+n};
    m.color=c;m.SetFloat("_Glossiness",n.StartsWith("Trim")?.45f:.2f);
    if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",n.StartsWith("Trim")?.4f:.05f);
    if(n.StartsWith("Trim")||n.StartsWith("Runes")){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",accent*.35f);}
    r.sharedMaterial=m;r.shadowCastingMode=ShadowCastingMode.On;r.receiveShadows=true;
   }
   return go;
  }
  // Loader for the textured Tripo trap models: bind the exported texture
  // files (Props/Traps/Textures/<name>_0 = basecolor, _1 = normal) explicitly —
  // Unity does not reliably auto-bind FBX textures. The imported root rotation
  // (270,0,0 on GLB→FBX models) is preserved: it IS what stands them upright.
  public static GameObject LoadTextured(string name,Transform parent){
   var prefab=Resources.Load<GameObject>("Props/Traps/"+name);
   if(!prefab)return null;
   var go=Object.Instantiate(prefab,parent);
   go.name=name;
   go.transform.localPosition=Vector3.zero;
   var albedo=Resources.Load<Texture2D>("Props/Traps/Textures/"+name+"_0");
   var normal=Resources.Load<Texture2D>("Props/Traps/Textures/"+name+"_1");
   Material skin=null;
   if(albedo){
    skin=new Material(Shader.Find("Standard")){name="TrapTex "+name};
    skin.mainTexture=albedo;skin.color=Color.white;skin.SetFloat("_Glossiness",.3f);
    if(normal){skin.SetTexture("_BumpMap",normal);skin.SetFloat("_BumpScale",1f);skin.EnableKeyword("_NORMALMAP");}
   }
   foreach(var r in go.GetComponentsInChildren<Renderer>(true)){
    if(skin){r.sharedMaterial=skin;continue;}
    var m=r.sharedMaterial;
    if(m&&m.mainTexture)continue;
    var nm=new Material(Shader.Find("Standard")){name="TrapMat "+r.gameObject.name};
    nm.color=new Color(.44f,.41f,.39f);nm.SetFloat("_Glossiness",.2f);
    r.sharedMaterial=nm;
   }
   return go;
  }
 }

 // 1. CRUSHER PILLAR — ancient ruin crusher. Hovers, telegraphs with a ring and
 // dust, slams down (damage + knockdown), then grinds back up. Chapters 4+.
 public sealed class CrusherPillar:MonoBehaviour {
  Transform head;float timer;Vector3 rest;bool down;Color accent;
  enum Phase{Hover,Warning,Slammed,Rising}Phase phase=Phase.Hover;
  public static CrusherPillar Place(Transform parent,Vector3 localPos,Color accent,Color stone){
   var go=new GameObject("Crusher pillar");go.transform.SetParent(parent,false);go.transform.localPosition=localPos;
   var model=TrapArt.Load("Trap_Crusher",go.transform,accent,stone);
   Transform head;
   if(model){
    head=model.transform;head.localPosition=new Vector3(0,3.1f,0);
   }else{
    head=Art.Shape("Stone Head",PrimitiveType.Cube,new Vector3(0,3.1f,0),new Vector3(2.3f,1.4f,2.3f),stone,go.transform).transform;
    Art.Shape("Trim Band",PrimitiveType.Cylinder,new Vector3(0,3.1f-.8f,0),new Vector3(2.5f,.16f,2.5f),Color.Lerp(accent,Color.white,.2f),go.transform);
   }
   var c=go.AddComponent<CrusherPillar>();c.head=head;c.rest=head.localPosition;c.accent=accent;
   // Physical head: blocks and crushes for real instead of passing through
   // props and enemies. The collider rides the head transform.
   var box=head.gameObject.AddComponent<BoxCollider>();
   box.size=new Vector3(2.3f,1.5f,2.3f);box.center=new Vector3(0,-.2f,0);
   TrapArt.Reserve(parent,localPos,2.2f);
   return c;
  }
  void Update(){
   var g=RealmGame.I;if(!g||g.Screen!=GameScreen.Playing||!g.Player)return;
   timer+=Time.deltaTime;
   switch(phase){
    case Phase.Hover:
     head.localPosition=rest+Vector3.up*Mathf.Sin(timer*2.1f)*.14f;
     if(timer>=2.3f){phase=Phase.Warning;timer=0;
      g.TrapSound("enemy_warning",transform.position,4f,16f,.8f);
      CombatTelegraph.Create(transform.position,1.7f,.65f,g.World.transform);
     }
     break;
    case Phase.Warning:
     head.localPosition=rest+new Vector3(Mathf.Sin(timer*60f)*.05f,0,Mathf.Cos(timer*53f)*.05f);
     if(timer>=.65f){phase=Phase.Slammed;timer=0;down=true;
      head.localPosition=new Vector3(rest.x,.8f,rest.z);
      g.TrapSound("land_hard",transform.position,5f,20f,1f);
      g.CameraRig.Shake=.32f;g.HitStop(.05f,.35f);
      KenneyPuff.Burst(transform.position+Vector3.up*.15f,new Color(.62f,.56f,.46f),14,1.3f);
      Vfx.Play("ga_vfx_Impact_01",transform.position+Vector3.up*.4f,Quaternion.identity,1.1f);
      Vector3 pd=g.Player.transform.position-transform.position;pd.y=0;
      if(pd.magnitude<1.55f&&g.Player.transform.position.y<transform.position.y+1.6f){
       if(g.Player.Damage(1,transform.position))g.Player.Bounce(7.5f);
      }
      if(g.Enemies!=null)foreach(var foe in g.Enemies){
       if(!foe||foe.Health<=0)continue;
       Vector3 fd=foe.transform.position-transform.position;fd.y=0;
       if(fd.magnitude<1.7f&&!foe.Boss)foe.Hit(4f,0,false,Vector3.down);
      }
     }
     break;
    case Phase.Slammed:
     if(timer>=.9f){phase=Phase.Rising;timer=0;}
     break;
    case Phase.Rising:
     head.localPosition=Vector3.MoveTowards(head.localPosition,rest,Time.deltaTime*2.6f);
     if(head.localPosition==rest){phase=Phase.Hover;timer=0;down=false;}
     break;
   }
  }
 }

 // 2. DART TURRET — ruined-temple sentry. Glows, tracks the player, then fires
 // a straight dart; strafing or dashing dodges it. Chapters 5+.
 public sealed class DartTurret:MonoBehaviour {
  Transform aim;Renderer mouth;float timer;Vector3 rest;Color accent;
  public static DartTurret Place(Transform parent,Vector3 localPos,Color accent,Color stone){
   var go=new GameObject("Dart turret");go.transform.SetParent(parent,false);go.transform.localPosition=localPos;
   var model=TrapArt.Load("Trap_Turret",go.transform,accent,stone);
   Transform aim;Renderer mouth=null;
   if(model){
    aim=model.transform.Find("Stone Face");if(!aim)aim=model.transform;
    foreach(var r in model.GetComponentsInChildren<Renderer>(true))if(r.gameObject.name.StartsWith("Mouth"))mouth=r;
   }else{
    Art.Shape("Stone Base",PrimitiveType.Cube,new Vector3(0,.3f,0),new Vector3(1.5f,.6f,1.5f),stone,go.transform);
    aim=Art.Shape("Stone Face",PrimitiveType.Cube,new Vector3(0,1.6f,0),new Vector3(.9f,.9f,.9f),stone,go.transform).transform;
    var m=Art.Shape("Mouth",PrimitiveType.Cube,new Vector3(0,1.55f,.42f),new Vector3(.26f,.26f,.2f),new Color(.05f,.04f,.05f),go.transform);
    mouth=m.GetComponent<Renderer>();
   }
   var t=go.AddComponent<DartTurret>();t.aim=aim;t.mouth=mouth;t.rest=aim?aim.localPosition:Vector3.zero;t.accent=accent;
   // Pedestal collider sized to the visible statue (~1.6m wide after the
   // Blender rescale) — an oversized box read as an invisible wall.
   var box=go.AddComponent<BoxCollider>();
   box.size=new Vector3(1.1f,3.4f,1.1f);box.center=new Vector3(0,1.7f,0);
   TrapArt.Reserve(parent,localPos,1.3f);
   return t;
  }
  void Update(){
   var g=RealmGame.I;if(!g||g.Screen!=GameScreen.Playing||!g.Player)return;
   timer+=Time.deltaTime;
   if(mouth){
    var c=mouth.material;c.SetColor("_EmissionColor",timer>1.6f&&timer<2.2f?accent*1.6f:accent*.1f);
   }
   if(timer>=2.2f){
    timer=0;
    Vector3 origin=aim?aim.position:transform.position+Vector3.up*1.6f;
    Vector3 dir=g.Player.transform.position+Vector3.up*.9f-origin;
    dir.y*=.4f;dir.Normalize();
    if(aim){Vector3 flat=dir;flat.y=0;if(flat.sqrMagnitude>.01f)aim.rotation=Quaternion.LookRotation(flat.normalized,Vector3.up);}
    Fire(origin,dir);
    g.TrapSound("impact",transform.position,3f,14f,.5f);
    HitSpark.Burst(origin,dir,accent,6);
   }else if(timer>1.6f&&aim){
    // track the player during the glow window so a dodge must be timed late
    Vector3 to=g.Player.transform.position+Vector3.up*.9f-aim.position;to.y=0;
    if(to.sqrMagnitude>.01f)aim.rotation=Quaternion.Slerp(aim.rotation,Quaternion.LookRotation(to.normalized,Vector3.up),Time.deltaTime*4f);
   }
  }
  void Fire(Vector3 origin,Vector3 dir){
   var dart=Art.Shape("Dart",PrimitiveType.Cube,origin,new Vector3(.09f,.09f,.62f),accent,null);
   dart.transform.rotation=Quaternion.LookRotation(dir,Vector3.up);
   var r=dart.GetComponent<Renderer>();var m=new Material(Shader.Find("Standard")){name="Dart"};
   m.color=accent;m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",accent*1.4f);
   r.sharedMaterial=m;r.shadowCastingMode=ShadowCastingMode.Off;
   darts.Add(new Dart{go=dart.transform,dir=dir,age=0});
  }
  class Dart{public Transform go;public Vector3 dir;public float age;}
  readonly System.Collections.Generic.List<Dart> darts=new System.Collections.Generic.List<Dart>();
  void LateUpdate(){
   var g=RealmGame.I;
   for(int i=darts.Count-1;i>=0;i--){
    var d=darts[i];bool dead=d.age>1.8f;
    if(!dead){
     d.age+=Time.deltaTime;
     d.go.position+=d.dir*13f*Time.deltaTime;
     if(g&&g.Screen==GameScreen.Playing&&g.Player){
      if(Vector3.Distance(d.go.position,g.Player.transform.position+Vector3.up*.9f)<.55f){
       if(g.Player.Damage(1,transform.position))HitSpark.Burst(d.go.position,-d.dir,new Color(1f,.3f,.2f),10);
       dead=true;
      }else if(g.Enemies!=null)foreach(var foe in g.Enemies){
       if(!foe||foe.Health<=0||foe.transform.IsChildOf(transform))continue;
       if(!foe.Boss&&Vector3.Distance(d.go.position,foe.transform.position+Vector3.up*.9f)<.7f){foe.Hit(2f,0,false,d.dir);dead=true;break;}
      }
     }
     if(d.go.position.y<transform.position.y-2f)dead=true;
    }
    if(dead){if(d.go)Destroy(d.go.gameObject);darts.RemoveAt(i);}
   }
  }
 }

 // 3. ROLLING BOULDER — a rumbling boulder careens down the lane when the player
 // closes in. Dodge sideways or dash past it. Chapters 6+.
  public sealed class RollingBoulder:MonoBehaviour {
  Transform rock;Vector3 start,dir;float timer,traveled,maxTravel;bool rolling;Color accent;
  const float Speed=7.2f,Range=10f,Radius=.95f;
  public static RollingBoulder Place(Transform parent,Vector3 localPos,Vector3 rollDir,Color accent,Color stone,float extent=8.3f){
   if(!TrapArt.IsClear(parent,localPos,1.8f))return null;
   var go=new GameObject("Rolling boulder");go.transform.SetParent(parent,false);go.transform.localPosition=localPos;
   var model=TrapArt.Load("Trap_Boulder",go.transform,accent,stone);
   Transform rock;
   if(model){rock=model.transform;rock.localPosition=Vector3.up*Radius;}
   else rock=Art.Shape("Rock",PrimitiveType.Sphere,Vector3.up*Radius,Vector3.one*Radius*2f,stone*.92f,go.transform).transform;
   var b=go.AddComponent<RollingBoulder>();b.rock=rock;b.start=rock.localPosition;b.dir=rollDir.sqrMagnitude>.01f?rollDir.normalized:new Vector3(0,0,-1);b.accent=accent;
   // Roll stops just past the island's far edge: start distance along the roll
   // axis plus the half-extent and a little overhang.
   b.maxTravel=-Vector3.Dot(localPos,b.dir)+extent*.5f+1f;
   // The boulder is a solid rolling body: it shoves and blocks for real.
   var ball=rock.gameObject.AddComponent<SphereCollider>();
   ball.radius=1f;ball.center=Vector3.zero;
   TrapArt.Reserve(parent,localPos,1.4f);
   TrapArt.Reserve(parent,localPos+b.dir*(b.maxTravel*.5f),1.6f);
   return b;
  }
  void Update(){
   var g=RealmGame.I;if(!g||g.Screen!=GameScreen.Playing||!g.Player)return;
   timer+=Time.deltaTime;
   if(!rolling){
    if(rock.localPosition!=start)rock.localPosition=Vector3.MoveTowards(rock.localPosition,start,Time.deltaTime*4f);
    if(timer>=5f){
     Vector3 to=g.Player.transform.position-transform.position;to.y=0;
     float along=Vector3.Dot(to,dir);
     if(along>.5f&&along<Range&&Mathf.Abs(Vector3.Dot(to,Vector3.Cross(dir,Vector3.up)))<3.6f){
      rolling=true;traveled=0;timer=0;
      g.TrapSound("boss_warning",transform.position,4f,18f,.7f);
      KenneyPuff.Burst(transform.position+Vector3.up*.3f,new Color(.62f,.56f,.46f),10,1.1f);
     }
    }
   }else{
    float step=Speed*Time.deltaTime;
    rock.localPosition+=dir*step;traveled+=step;
    rock.Rotate(Vector3.Cross(Vector3.up,dir),Speed*Time.deltaTime*Mathf.Rad2Deg/Radius*.5f,Space.Self);
    if(Random.value<.35f)KenneyPuff.Burst(rock.position+Vector3.down*(Radius-.2f),new Color(.62f,.56f,.46f),1,.7f);
    if(Vector3.Distance(rock.position,g.Player.transform.position+Vector3.up*.8f)<Radius+.55f){
     if(g.Player.Damage(1,rock.position))g.Player.Bounce(6.5f);
    }
    if(g.Enemies!=null)foreach(var foe in g.Enemies){
     if(!foe||foe.Health<=0)continue;
     if(!foe.Boss&&Vector3.Distance(rock.position,foe.transform.position+Vector3.up*.9f)<Radius+.5f)foe.Hit(5f,0,false,dir);
    }
    if(traveled>maxTravel){rolling=false;timer=0;rock.localPosition=start;KenneyPuff.Burst(rock.position+Vector3.down*(Radius-.2f),new Color(.62f,.56f,.46f),8,1f);}
   }
  }
 }

 // 4. WIND VENT — gale-realm vent. Harmless itself, but its gust shoves anyone
 // standing in the lane sideways: deadly next to a bridge edge. Chapters 3+.
 public sealed class WindVent:MonoBehaviour {
  Vector3 gust;float timer;Transform rune;
  public static WindVent Place(Transform parent,Vector3 localPos,Vector3 pushDir,Color accent){
   var go=new GameObject("Wind vent");go.transform.SetParent(parent,false);go.transform.localPosition=localPos;
   Art.Shape("VentGrate",PrimitiveType.Cylinder,new Vector3(0,.05f,0),new Vector3(1.5f,.1f,1.5f),new Color(.2f,.22f,.24f),go.transform,true);
   var runeT=Art.Shape("VentRune",PrimitiveType.Cylinder,new Vector3(0,.12f,0),new Vector3(.7f,.06f,.7f),accent,go.transform).transform;
   var v=go.AddComponent<WindVent>();v.rune=runeT;v.gust=pushDir.sqrMagnitude>.01f?pushDir.normalized:Vector3.right;v.timer=Random.value*2f;
   TrapArt.Reserve(parent,localPos,1.6f);
   return v;
  }
  void Update(){
   var g=RealmGame.I;if(!g||g.Screen!=GameScreen.Playing||!g.Player)return;
   timer+=Time.deltaTime;
   bool blowing=timer%3.4f>1.2f&&timer%3.4f<2.4f;
   if(rune)rune.GetComponent<Renderer>().material.color=blowing?Color.Lerp(gustColor,Color.white,.3f):gustColor;
   if(blowing){
    if(timer%3.4f<1.28f)g.TrapSound("gale_cast",transform.position,3f,14f,.6f);
    Vector3 to=g.Player.transform.position-transform.position;to.y=0;
    if(to.magnitude<2.6f&&Mathf.Abs(to.y)<2.2f)g.Player.ApplyForce(gust*10f*Time.deltaTime);
    if(Random.value<.5f)KenneyPuff.Burst(transform.position+Vector3.up*.2f+gust*Random.Range(0f,1.6f),new Color(.85f,.95f,1f,.5f),1,.5f);
   }
  }
  Color gustColor=>RealmGame.I?RealmGame.I.Accent:new Color(.5f,.8f,1f);
 }
}
