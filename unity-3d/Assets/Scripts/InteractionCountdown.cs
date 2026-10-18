using UnityEngine;

namespace LostRealms {
 // Displays the interaction's own timer; it never advances gameplay independently.
 public sealed class InteractionCountdown:MonoBehaviour {
  public float RemainingSeconds {get;private set;}
  public float RemainingFraction {get;private set;}
  public string Caption {get;private set;}
  int reportedFrame=-10;Vector3 localAnchor;GUIStyle label;
  public bool Visible=>gameObject.activeInHierarchy&&Time.frameCount-reportedFrame<=1&&RemainingSeconds>0;
  public static float Fraction(float elapsed,float duration)=>duration>0?1-Mathf.Clamp01(elapsed/duration):0;
  public static void Report(Transform target,float elapsed,float duration,string caption="ACTIVATING"){
   if(!target||elapsed<=0||duration<=0||elapsed>=duration)return;
   var display=target.GetComponent<InteractionCountdown>();
   if(!display){display=target.gameObject.AddComponent<InteractionCountdown>();display.FindAnchor();}
   display.RemainingSeconds=Mathf.Max(0,duration-elapsed);display.RemainingFraction=Fraction(elapsed,duration);
   display.Caption=caption;display.reportedFrame=Time.frameCount;
  }
  void FindAnchor(){
   Bounds bounds=new Bounds(transform.position,Vector3.zero);bool found=false;
   foreach(var renderer in GetComponentsInChildren<Renderer>()){
    if(renderer.GetComponent<TextMesh>()||(!(renderer is MeshRenderer)&&!(renderer is SkinnedMeshRenderer)))continue;
    if(!found){bounds=renderer.bounds;found=true;}else bounds.Encapsulate(renderer.bounds);
   }
   localAnchor=transform.InverseTransformPoint(new Vector3(bounds.center.x,found?bounds.max.y+.35f:transform.position.y+1.5f,bounds.center.z));
  }
  void OnGUI(){
   var game=RealmGame.I;var camera=Camera.main;
   if(!Visible||!game||game.Screen!=GameScreen.Playing||!camera)return;
   Vector3 anchor=transform.TransformPoint(localAnchor);
   if(Vector3.Distance(camera.transform.position,anchor)>38)return;
   Vector3 point=camera.WorldToScreenPoint(anchor);
   if(point.z<=0||point.x<0||point.x>Screen.width||point.y<0||point.y>Screen.height)return;
   var previous=GUI.matrix;var previousColor=GUI.color;GUI.matrix=Matrix4x4.identity;
   float scale=Mathf.Max(.7f,Screen.width/1280f),width=160*scale,height=12*scale;
   // Reserve the HUD and active hint strip so they cannot cover the time label.
   float topSafe=(game.NoticeActive?284f:190f)*Screen.height/720f;
   var bar=new Rect(point.x-width*.5f,Mathf.Max(topSafe,Screen.height-point.y),width,height);
   try{
    if(label==null)label=new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.Bold};
    label.fontSize=Mathf.RoundToInt(13*scale);label.normal.textColor=Color.white;
    var text=new Rect(bar.x-25*scale,bar.y-24*scale,bar.width+50*scale,22*scale);
    GUI.color=new Color(.015f,.03f,.05f,.95f);GUI.DrawTexture(new Rect(text.x,text.y,text.width,text.height+height+4*scale),Texture2D.whiteTexture);
    GUI.color=Color.white;GUI.Label(text,Caption+"  "+RemainingSeconds.ToString("0.0")+"s",label);
    GUI.color=new Color(.9f,.73f,.38f);GUI.DrawTexture(bar,Texture2D.whiteTexture);
    var inner=new Rect(bar.x+2*scale,bar.y+2*scale,bar.width-4*scale,bar.height-4*scale);
    GUI.color=new Color(.06f,.12f,.17f);GUI.DrawTexture(inner,Texture2D.whiteTexture);
    inner.width*=RemainingFraction;GUI.color=new Color(.35f,.88f,1);GUI.DrawTexture(inner,Texture2D.whiteTexture);
   }finally{GUI.color=previousColor;GUI.matrix=previous;}
  }
 }
}
