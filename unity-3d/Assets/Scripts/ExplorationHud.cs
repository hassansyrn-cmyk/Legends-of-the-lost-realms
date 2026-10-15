using UnityEngine;
namespace LostRealms {
 public partial class RealmGame {
  bool fieldMap;
  void ExplorationHud(){
   var explore=World?World.Exploration:null;if(!explore)return;
   if(HudButton(new Rect(1035,78,170,35),fieldMap?"CLOSE FIELD MAP":"FIELD MAP"))fieldMap=!fieldMap;
   if(!fieldMap)return;
   Rect map=new Rect(855,131,350,238);Panel(839,117,382,292);
   Text(854,378,350,19,"GOLD: GOAL   BLUE: VISITED   WHITE: YOU",tinyC);
   float minX=float.MaxValue,maxX=float.MinValue,minZ=float.MaxValue,maxZ=float.MinValue;
   foreach(var site in explore.Sites){var p=site.transform.position;minX=Mathf.Min(minX,p.x);maxX=Mathf.Max(maxX,p.x);minZ=Mathf.Min(minZ,p.z);maxZ=Mathf.Max(maxZ,p.z);}
   float scale=Mathf.Min(map.width/(maxX-minX+20),map.height/(maxZ-minZ+20));
   Vector2 Project(Vector3 p)=>new Vector2(map.center.x+(p.x-(minX+maxX)*.5f)*scale,map.center.y-(p.z-(minZ+maxZ)*.5f)*scale);
   foreach(var link in explore.Links){var a=explore.Sites[link.x];var b=explore.Sites[link.y];if(!a.Visited&&!b.Visited)continue;
    Vector2 from=Project(a.transform.position),to=Project(b.transform.position);int dots=Mathf.CeilToInt(Vector2.Distance(from,to)/5);for(int i=0;i<=dots;i++){var p=Vector2.Lerp(from,to,i/(float)Mathf.Max(1,dots));Box(new Rect(p.x-1,p.y-1,2,2),new Color(.3f,.53f,.66f,.75f));}
   }
   foreach(var site in explore.Sites){if(!site.Visited)continue;var p=Project(site.transform.position);Box(new Rect(p.x-5,p.y-5,10,10),site.Objective?new Color(.95f,.76f,.35f):new Color(.3f,.7f,.86f));}
   foreach(var find in explore.Finds){if(find.Claimed)continue;var site=find.GetComponentInParent<ExplorationSite>();if(!site||!site.Visited)continue;var p=Project(find.transform.position);Text(p.x-10,p.y-15,20,22,find.Key?"K":"+",tinyC);}
   if(explore.GateRevealed){var p=Project(explore.GatePosition);Text(p.x-25,p.y-15,50,22,"GATE",tinyC);}
   var hero=Project(Player.transform.position);Box(new Rect(hero.x-3,hero.y-3,6,6),Color.white);
   var tip=Project(Player.transform.position+Player.transform.forward*4);Box(new Rect(tip.x-1,tip.y-1,2,2),Color.white);
  }
 }
}
