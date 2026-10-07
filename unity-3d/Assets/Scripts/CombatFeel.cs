using UnityEngine;

namespace LostRealms {
 // Weight-scaled impact feedback (hit-stop, camera kick, sparks) and the soft lock-on.
 // Purely a feedback/assist layer: it never moves Aster, never touches her animation
 // clips and never changes enemy AI or stats.
 public static class CombatFeel {
  // Heavier blows (more damage relative to the target's toughness) freeze longer and kick harder.
  public static void OnEnemyHit(Enemy e,float damage,bool weak,bool guardBreak,Vector3 dir){
   var g=RealmGame.I;
   if(!e||g==null||g.Screen!=GameScreen.Playing||!g.Player||!g.CameraRig)return;
   if((e.transform.position-g.Player.transform.position).sqrMagnitude>18f*18f)return; // trap hits far away stay silent
   bool kill=e.Health<=0;
   float w=Mathf.Clamp01(damage/(e.Boss?7f:3.2f));
   float stop=Mathf.Lerp(.026f,.062f,w);
   if(weak)stop+=.012f;
   if(guardBreak)stop+=.01f;
   float scale=Mathf.Lerp(.32f,.12f,w);
   if(kill){stop=e.Boss?.12f:.075f;scale=e.Boss?.03f:.06f;}
   else if(e.Boss)stop*=.7f; // boss fights keep their pace
   g.HitStopFeel(stop,scale);
   if(dir.sqrMagnitude<.001f)dir=e.transform.position-g.Player.transform.position;
   dir.y=0;
   g.CameraRig.Kick(dir,Mathf.Lerp(.04f,.2f,w)*(kill?1.5f:1f));
   if(kill)g.CameraRig.Shake=Mathf.Max(g.CameraRig.Shake,e.Boss?.45f:.2f);
   int sparks=4+Mathf.RoundToInt(10f*w)+(kill?8:0);
   HitSpark.Burst(e.transform.position+Vector3.up*(e.Boss?1.5f:.95f),-dir,weak?new Color(1f,.95f,.55f):new Color(1f,.9f,.75f),sparks);
   if(w>.6f||kill)g.Haptic();
  }

  public static void OnEnemyKilled(Enemy e){
   var g=RealmGame.I;if(!e||g==null||g.Save==null)return;
   if(Codex.Record(g.Save,e.Kind,e.Boss)){
    int k=Mathf.Clamp(e.Kind,0,Codex.Kinds-1);
    g.Tell("Bestiary updated: "+Codex.Entries[k].Name,3f);
   }
  }
 }

 // Soft lock-on: picks a sticky target in front of Aster, draws a reticle, and widens the
 // existing attack facing assist so swings connect with the marked enemy. It never steers
 // movement or the camera.
 public sealed class SoftLock:MonoBehaviour {
  public static Enemy Target{get;private set;}
  public const float AcquireRange=13f,KeepRange=16f,AssistRange=5.5f;
  float nextPick;
  static Texture2D tex;

  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
  static void Boot(){var go=new GameObject("Soft lock");DontDestroyOnLoad(go);go.AddComponent<SoftLock>();}

  static bool Active(RealmGame g)=>g!=null&&g.Save!=null&&g.Save.softLock&&g.Screen==GameScreen.Playing&&g.Player&&g.Player.Health>0;

  // Target Aster should face when she swings, or null for the old nearest-in-front assist.
  public static Enemy Assist(Hero hero){
   var g=RealmGame.I;var t=Target;
   if(!Active(g)||!t||t.Health<=0)return null;
   Vector3 d=t.transform.position-hero.transform.position;d.y=0;
   if(d.magnitude>AssistRange||d.sqrMagnitude<.0001f)return null;
   return Vector3.Dot(hero.transform.forward,d.normalized)>-.1f?t:null;
  }

  // Pure scoring helper (kept static so the headless probe can exercise it).
  public static float Score(Vector3 heroPos,Vector3 heroFwd,Vector3 enemyPos,bool current){
   Vector3 d=enemyPos-heroPos;d.y=0;float dist=d.magnitude;
   if(dist>AcquireRange)return float.MaxValue;
   float dot=dist<.01f?1f:Vector3.Dot(heroFwd,d/dist);
   if(dot<.15f&&dist>2.5f)return float.MaxValue; // behind Aster and not close
   float score=dist*(1.6f-dot*.6f);
   return current?score*.75f:score; // stickiness: don't flicker between similar targets
  }

  void Update(){
   var g=RealmGame.I;
   if(!Active(g)){Target=null;return;}
   if(Target&&(Target.Health<=0||(Target.transform.position-g.Player.transform.position).magnitude>KeepRange))Target=null;
   if(Time.unscaledTime<nextPick)return;
   nextPick=Time.unscaledTime+.15f;
   Vector3 pos=g.Player.transform.position;Vector3 fwd=g.Player.transform.forward;fwd.y=0;
   fwd=fwd.sqrMagnitude<.01f?Vector3.forward:fwd.normalized;
   Enemy best=null;float bestScore=float.MaxValue;
   foreach(var e in g.Enemies){
    if(!e||e.Health<=0)continue;
    float s=Score(pos,fwd,e.transform.position,e==Target);
    if(s<bestScore){bestScore=s;best=e;}
   }
   Target=best;
  }

  void OnGUI(){
   if(Event.current.type!=EventType.Repaint)return;
   var g=RealmGame.I;var t=Target;
   if(!Active(g)||!t||t.Health<=0)return;
   var cam=Camera.main;if(!cam)return;
   Vector3 p=cam.WorldToScreenPoint(t.transform.position+Vector3.up*(t.Boss?2.4f:1.5f));
   if(p.z<=0)return;
   if(!tex){tex=Texture2D.whiteTexture;}
   float x=p.x,y=Screen.height-p.y;
   float half=Mathf.Clamp(Screen.height*.028f,14f,30f)*(1f+.06f*Mathf.Sin(Time.unscaledTime*6f));
   float th=Mathf.Max(2f,Screen.height*.004f);
   var old=GUI.color;var oldMatrix=GUI.matrix;
   GUI.color=t.Boss?new Color(1f,.85f,.3f,.95f):new Color(1f,.38f,.28f,.92f);
   GUIUtility.RotateAroundPivot(45f,new Vector2(x,y));
   GUI.DrawTexture(new Rect(x-half,y-half,half*2,th),tex);
   GUI.DrawTexture(new Rect(x-half,y+half-th,half*2,th),tex);
   GUI.DrawTexture(new Rect(x-half,y-half,th,half*2),tex);
   GUI.DrawTexture(new Rect(x+half-th,y-half,th,half*2),tex);
   GUI.matrix=oldMatrix;GUI.color=old;
  }
 }
}
