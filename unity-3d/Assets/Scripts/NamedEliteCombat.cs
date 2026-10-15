using System.Collections.Generic;
using UnityEngine;
namespace LostRealms {
 public partial class Enemy {
  public int NamedSignature;public bool SignaturePending=>signatureActive;public int SignatureAttacks {get;private set;}
  float signatureReady,signatureTimer;bool signatureActive;readonly List<GameObject> signatureWarnings=new List<GameObject>();readonly List<Vector3> signaturePoints=new List<Vector3>();float signatureRadius;
  void CancelNamedSignature(){
   if(!signatureActive)return;signatureActive=false;signatureReady=RealmGame.I.Elapsed+5f;foreach(var w in signatureWarnings)if(w)Destroy(w);signatureWarnings.Clear();ReleaseToken();state=State.Recover;timer=.8f;
  }
  bool UpdateNamedSignature(float dt){
   if(NamedSignature==0||Boss)return false;var g=RealmGame.I;
   if(signatureActive){
    if(!Grounded){CancelNamedSignature();return true;}
    signatureTimer-=dt;Visual.Play("attack_1");if(signatureTimer>0)return true;
    foreach(var w in signatureWarnings)if(w)Destroy(w);signatureWarnings.Clear();SignatureAttacks++;
    for(int i=0;i<signaturePoints.Count;i++)StrikeZone.Create(signaturePoints[i],signatureRadius,2,NamedSignature==6?.01f+i*.18f:.01f);
    g.Sound("boss");signatureActive=false;signatureReady=g.Elapsed+8f;ReleaseToken();state=State.Recover;timer=1.1f;return true;
   }
   Vector3 delta=g.Player.transform.position-transform.position;delta.y=0;
   if(state!=State.Notice||timer>0||g.Elapsed<signatureReady||delta.magnitude>6f||!Grounded||Mathf.Abs(g.Player.transform.position.y-transform.position.y)>1.5f||!TryAcquireToken())return false;
   Face(g.Player.transform.position,.3f);signaturePoints.Clear();Vector3 forward=delta.sqrMagnitude>.01f?delta.normalized:transform.forward,right=Vector3.Cross(Vector3.up,forward);signatureRadius=NamedSignature==3?2.6f:1.15f;
   if(NamedSignature==1){signaturePoints.Add(g.Player.transform.position);signaturePoints.Add(g.Player.transform.position+right*2f);signaturePoints.Add(g.Player.transform.position-right*2f);}
   else if(NamedSignature==2){signaturePoints.Add(transform.position+forward*1.8f+right*1.6f);signaturePoints.Add(transform.position+forward*1.8f-right*1.6f);}
   else if(NamedSignature==3)signaturePoints.Add(transform.position+forward*1.5f);
   else if(NamedSignature==4){for(int i=0;i<6;i++){float a=i*Mathf.PI/3;signaturePoints.Add(transform.position+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*3.3f);}}
   else if(NamedSignature==5){signaturePoints.Add(g.Player.transform.position);for(int i=0;i<4;i++){float a=i*Mathf.PI*.5f+.785f;signaturePoints.Add(g.Player.transform.position+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*2.4f);}}
   else for(int i=0;i<3;i++)signaturePoints.Add(transform.position+forward*(1.4f+i*1.35f));
   for(int i=0;i<signaturePoints.Count;i++){var p=Clamp(signaturePoints[i]);p.y=transform.position.y;signaturePoints[i]=p;signatureWarnings.Add(CombatTelegraph.Create(p,signatureRadius,1.15f+(NamedSignature==6?i*.18f:0),g.World.transform));}
   signatureActive=true;signatureTimer=1.15f;state=State.Windup;comboLeft=0;Visual.PlayTimed("attack_1",1.55f);g.Sound("enemy_warning");return true;
  }
 }
}
