using System.Collections.Generic;
using UnityEngine;
namespace LostRealms {
 // A finger owns its initial control until release, including when it crosses another control.
 public sealed class TouchRouter {
  public enum Role { Ignored, Move, Camera, Dodge, Power, Blade, Jump }
  readonly Dictionary<int,Role> owners=new Dictionary<int,Role>();
  Vector2 origin;
  public Vector2 Move; public float Yaw; public bool Jump,Dodge,Power,BladePressed,BladeReleased,BladeHeld;
  public static Rect ActionRect(int index)=>new Rect(685+index*145,570,132,108);
  public void BeginFrame(){Move=Vector2.zero;Yaw=0;Jump=Dodge=Power=BladePressed=BladeReleased=BladeHeld=false;}
  public void Reset(){owners.Clear();BeginFrame();}
  public void Sample(int id,Vector2 p,Vector2 delta,TouchPhase phase){
   if(phase==TouchPhase.Began){
    Role role=Role.Ignored;
    for(int i=0;i<4;i++)if(ActionRect(i).Contains(p))role=(Role)((int)Role.Dodge+i);
    if(role==Role.Ignored&&p.x<420&&p.y>380&&!owners.ContainsValue(Role.Move)){role=Role.Move;origin=p;}
    if(role==Role.Ignored&&p.x>440&&p.y>180&&p.y<530&&!owners.ContainsValue(Role.Camera))role=Role.Camera;
    owners[id]=role;
   }
   if(!owners.TryGetValue(id,out var owned))return;
   bool ended=phase==TouchPhase.Ended||phase==TouchPhase.Canceled,began=phase==TouchPhase.Began;
   switch(owned){
    case Role.Move: if(!ended)Move=Vector2.ClampMagnitude(new Vector2(p.x-origin.x,origin.y-p.y)/65,1);break;
    case Role.Camera: if(phase==TouchPhase.Moved)Yaw+=Mathf.Clamp(delta.x*.13f,-8,8);break;
    case Role.Jump: Jump|=began;break;
    case Role.Dodge: Dodge|=began;break;
    case Role.Power: Power|=began;break;
    case Role.Blade: BladePressed|=began;BladeHeld|=!ended;BladeReleased|=phase==TouchPhase.Ended;break;
   }
   if(ended)owners.Remove(id);
  }
 }
}
