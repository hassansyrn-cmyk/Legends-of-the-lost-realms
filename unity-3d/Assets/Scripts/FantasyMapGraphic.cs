using UnityEngine;
using UnityEngine.UI;

namespace LostRealms {
 // Real chapter geometry and actor positions, redrawn at 8 Hz, under a stencil mask.
 public sealed class FantasyMapGraphic : MaskableGraphic {
  public RealmGame Game;
  float nextDraw;
  void Update(){if(Time.unscaledTime>=nextDraw){nextDraw=Time.unscaledTime+.125f;SetVerticesDirty();}}
  protected override void OnPopulateMesh(VertexHelper mesh){
   mesh.Clear();if(!Game||!Game.World||!Game.Player||Game.World.Route.Count==0)return;
   var route=Game.World.Route;float minX=route[0].x,maxX=minX,minZ=route[0].z,maxZ=minZ;
   foreach(var p in route){minX=Mathf.Min(minX,p.x);maxX=Mathf.Max(maxX,p.x);minZ=Mathf.Min(minZ,p.z);maxZ=Mathf.Max(maxZ,p.z);}
   Vector3 gate=route[route.Count-1]+new Vector3(0,0,5);
   maxZ=Mathf.Max(maxZ,gate.z);
   var center=new Vector2((minX+maxX)*.5f,(minZ+maxZ)*.5f);
   float scale=(Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)-26)/Mathf.Max(1,Mathf.Sqrt((maxX-minX)*(maxX-minX)+(maxZ-minZ)*(maxZ-minZ)));
   Vector2 Project(Vector3 p)=>rectTransform.rect.center+(new Vector2(p.x,p.z)-center)*scale;
   Color gold=new Color(.77f,.64f,.36f,.9f);
   for(int i=0;i<route.Count;i++){
    var point=Project(route[i]);if(i>0)Line(mesh,Project(route[i-1]),point,1.1f,new Color(.45f,.62f,.64f,.6f));Dot(mesh,point,2.3f,gold);
   }
   Dot(mesh,Project(gate),3.4f,Game.GateOpen()?Color.cyan:Color.white);
   foreach(var foe in Game.Enemies)if(foe&&foe.Health>0)Dot(mesh,Project(foe.transform.position),foe.Boss?3:1.9f,new Color(1,.32f,.29f));
   var hero=Game.Player.transform;var hp=Project(hero.position);Dot(mesh,hp,3.2f,new Color(.35f,1,.78f));
   Line(mesh,hp,hp+new Vector2(hero.forward.x,hero.forward.z)*8,1.6f,Color.white);
  }
  static void Dot(VertexHelper mesh,Vector2 p,float radius,Color color){
   int start=mesh.currentVertCount;mesh.AddVert(p,color,Vector2.zero);
   for(int i=0;i<=12;i++){float a=i*Mathf.PI/6;mesh.AddVert(p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,color,Vector2.zero);if(i>0)mesh.AddTriangle(start,start+i,start+i+1);}
  }
  static void Line(VertexHelper mesh,Vector2 a,Vector2 b,float width,Color color){
   Vector2 n=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;int start=mesh.currentVertCount;
   mesh.AddVert(a-n,color,Vector2.zero);mesh.AddVert(a+n,color,Vector2.zero);mesh.AddVert(b+n,color,Vector2.zero);mesh.AddVert(b-n,color,Vector2.zero);
   mesh.AddTriangle(start,start+1,start+2);mesh.AddTriangle(start,start+2,start+3);
  }
 }
}