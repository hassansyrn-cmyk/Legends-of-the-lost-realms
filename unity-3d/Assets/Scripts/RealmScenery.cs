using System.Collections.Generic;
using UnityEngine;
namespace LostRealms {
 public static class RealmScenery {
  static Material Ground(int realm,bool cliff){
   var shader=Resources.Load<Shader>("Shaders/Weathered");var m=new Material(shader?shader:Shader.Find("Standard"));
   Color[] baseColors=cliff?new[]{new Color(.16f,.22f,.22f),new Color(.35f,.23f,.15f),new Color(.22f,.34f,.43f)}:new[]{new Color(.16f,.29f,.12f),new Color(.49f,.33f,.17f),new Color(.62f,.77f,.82f)};
   Color[] detailColors=cliff?new[]{new Color(.31f,.37f,.28f),new Color(.6f,.4f,.23f),new Color(.41f,.56f,.64f)}:new[]{new Color(.42f,.49f,.21f),new Color(.75f,.57f,.31f),new Color(.88f,.93f,.9f)};
   m.color=baseColors[realm];if(m.HasProperty("_Detail"))m.SetColor("_Detail",detailColors[realm]);
   if(!cliff){string[] textures={"Art/verdant_moss_tile","Art/ember_sand_tile","Art/frost_ice_tile"};var terrainTexture=Resources.Load<Texture2D>(textures[realm]);if(terrainTexture&&m.HasProperty("_MainTex"))m.SetTexture("_MainTex",terrainTexture);if(m.HasProperty("_TileScale"))m.SetFloat("_TileScale",.13f);}
   return m;
  }
  public static void Upgrade(RealmWorld world,int realm){
   var lifetime=world.gameObject.AddComponent<RealmArtLifetime>();var terrain=Ground(realm,false);var rock=Ground(realm,true);lifetime.Keep(terrain);lifetime.Keep(rock);var skyShader=Resources.Load<Shader>("Shaders/RealmSky");
   if(skyShader){var sky=new Material(skyShader);lifetime.Keep(sky);sky.SetColor("_Zenith",realm==0?new Color(.12f,.29f,.35f):realm==1?new Color(.3f,.35f,.43f):new Color(.13f,.25f,.42f));sky.SetColor("_Horizon",realm==0?new Color(.57f,.7f,.58f):realm==1?new Color(.76f,.59f,.4f):new Color(.63f,.76f,.85f));RenderSettings.skybox=sky;Camera.main.clearFlags=CameraClearFlags.Skybox;}
   RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.58f,.68f,.74f);RenderSettings.ambientEquatorColor=new Color(.43f,.5f,.43f);RenderSettings.ambientGroundColor=new Color(.2f,.22f,.25f);
   RenderSettings.fogColor=realm==0?new Color(.43f,.58f,.51f):realm==1?new Color(.63f,.47f,.31f):new Color(.46f,.61f,.73f);RenderSettings.fogDensity=.009f;
   int seed=0;
   foreach(Transform island in world.transform){
    if(island.name!="Island")continue;seed++;
    var surface=island.Find("Realm surface");if(!surface)continue;
     float width=surface.localScale.x-.08f,length=surface.localScale.z-.08f;
     surface.GetComponent<Renderer>().sharedMaterial=terrain;
     surface.GetComponent<Renderer>().enabled=false;
     var solid=island.Find("Walkable stone");if(solid)solid.GetComponent<Renderer>().enabled=false;
     foreach(Transform item in island){if(item.name=="Foliage"||item.name=="Ancient trunk"||item.name=="CliffShelf"||item.name=="RockKeel"||item.name=="FloatingKeystone"||item.name.StartsWith("Curb")){item.gameObject.SetActive(false);Object.Destroy(item.gameObject);}}
     Cliff(island,width,length,seed,rock);
     IslandTop(island,width,length,seed,terrain);
    var rng=new System.Random(seed*587+realm*1901);
    if(width>7){
     for(int side=-1;side<=1;side+=2){
      Vector3 p=new Vector3(side*(width*.5f-.6f),.1f,(seed%2==0?1:-1)*(length*.5f-1.3f));
      if(realm==0)Tree(p,island,seed+side);else if(realm==2)Pine(p,island,seed+side);
      for(int n=0;n<5;n++){float z=(float)(rng.NextDouble()-.5)*(length-1);Vector3 r=new Vector3(side*(width*.5f-.25f),.22f,z);var b=Art.Shape("Weathered edge rock",PrimitiveType.Sphere,r,new Vector3(.45f,.35f,.65f),Color.white,island);b.GetComponent<Renderer>().sharedMaterial=rock;b.AddComponent<SphereCollider>().radius=.5f;}
     }
    }
    Tufts(island,width,length,realm,rng);
    // A readable worn trail leaves the centre clear for movement and combat.
    for(int n=0;n<3;n++){var slab=Art.Shape("Ancient stepping stone",PrimitiveType.Cube,new Vector3((n%2==0?.24f:-.24f),.12f,(n-1)*2),new Vector3(1.1f,.06f,.85f),Color.white,island);slab.transform.localRotation=Quaternion.Euler(0,(n-1)*11,0);slab.GetComponent<Renderer>().sharedMaterial=rock;}
   }
   // Distant silhouettes frame the route without obstructing the playable camera corridor.
   foreach(Transform t in world.transform)if(t.name=="Distant canopy"||t.name=="Distant realm spire"){t.gameObject.SetActive(false);Object.Destroy(t.gameObject);}
   for(int i=0;i<18;i++){float side=i%2==0?-1:1;var root=new GameObject("Distant floating crag").transform;root.SetParent(world.transform,false);root.localPosition=new Vector3(side*(23+i%3*8),-14-i%4*3,-22+i*11);Cliff(root,12+i%4*3,15,i,rock);IslandTop(root,12+i%4*3,15,i,terrain);if(realm==0)Tree(Vector3.zero,root,i);}
   RealmAtmosphere.Apply(world,realm);
  }
  static void MeshObject(string name,Transform parent,Mesh mesh,Material material){var g=new GameObject(name);g.transform.SetParent(parent,false);g.AddComponent<MeshFilter>().sharedMesh=mesh;parent.GetComponentInParent<RealmArtLifetime>().Keep(mesh);g.AddComponent<MeshRenderer>().sharedMaterial=material;}
  static void Cliff(Transform parent,float width,float length,int seed,Material material){
   var verts=new List<Vector3>();var tris=new List<int>();const int sides=16;
   for(int i=0;i<sides;i++){
    float a=i*Mathf.PI*2/sides,b=(i+1)*Mathf.PI*2/sides;
    Vector3 p=Rim(a,width,length),q=Rim(b,width,length);
    Vector3 r=p*(.45f+.12f*Mathf.Sin(seed+i*2));r.y=-4.5f-.8f*Mathf.Sin(i*3+seed);
    Vector3 s=q*(.45f+.12f*Mathf.Sin(seed+(i+1)*2));s.y=-4.5f-.8f*Mathf.Sin((i+1)*3+seed);
    int k=verts.Count;verts.AddRange(new[]{p,q,r,q,s,r,r,s,new Vector3(.3f,-6.5f,.2f)});for(int j=0;j<9;j++)tris.Add(k+j);
   }
   var mesh=new Mesh{name="Faceted island cliff"};mesh.SetVertices(verts);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();MeshObject("Sculpted cliff",parent,mesh,material);
  }
  static Vector3 Rim(float a,float w,float l){float x=Mathf.Cos(a),z=Mathf.Sin(a),m=Mathf.Max(Mathf.Abs(x),Mathf.Abs(z));return new Vector3(x/m*w*.5f,-.08f,z/m*l*.5f);}
  // A rounded, irregular island top that replaces the square slab: concentric
  // rings that meet the cliff rim, with a small wobble so it reads as natural rock.
  static void IslandTop(Transform parent,float width,float length,int seed,Material material){
   var verts=new List<Vector3>();var tris=new List<int>();const int sides=24;
   float[] rings={.55f,.85f,1f};float[] ys={.11f,.09f,-.02f};
   int center=verts.Count;verts.Add(new Vector3(0,.12f,0));
   int prev=-1;
   for(int r=0;r<rings.Length;r++){
    int start=verts.Count;
    for(int i=0;i<sides;i++){
     float a=i*Mathf.PI*2/sides;
     Vector3 rim=Rim(a,width,length);
     float wob=1.0f+.09f*(.5f+.5f*Mathf.Sin(seed*1.7f+a*3f))+.04f*(.5f+.5f*Mathf.Sin(seed*2.3f+a*7f));
     verts.Add(new Vector3(rim.x*wob*rings[r],ys[r]+.02f*Mathf.Sin(seed+a*5f),rim.z*wob*rings[r]));
    }
    if(r==0){for(int i=0;i<sides;i++)tris.AddRange(new[]{center,start+(i+1)%sides,start+i});}
    else{for(int i=0;i<sides;i++){int a0=prev+i,a1=prev+(i+1)%sides,b0=start+i,b1=start+(i+1)%sides;tris.AddRange(new[]{a0,a1,b0,b0,a1,b1});}}
    prev=start;
   }
   var mesh=new Mesh{name="Natural island top"};mesh.SetVertices(verts);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
   float ny=0f;var normals=mesh.normals;for(int i=0;i<normals.Length;i++)ny+=normals[i].y;
   if(ny<0f){var tri=mesh.triangles;for(int i=0;i<tri.Length;i+=3){int tmp=tri[i+1];tri[i+1]=tri[i+2];tri[i+2]=tmp;}mesh.triangles=tri;mesh.RecalculateNormals();}
   MeshObject("Natural island top",parent,mesh,material);
  }
  static void Cone(Vector3 p,float radius,float height,Color c,Transform parent,int sides=7){
   var vs=new List<Vector3>();var ts=new List<int>();
   for(int i=0;i<sides;i++){float a=i*Mathf.PI*2/sides,b=(i+1)*Mathf.PI*2/sides;int k=vs.Count;vs.Add(p+new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius));vs.Add(p+Vector3.up*height);vs.Add(p+new Vector3(Mathf.Cos(b)*radius,0,Mathf.Sin(b)*radius));ts.AddRange(new[]{k,k+1,k+2});}
   var m=new Mesh();m.SetVertices(vs);m.SetTriangles(ts,0);m.RecalculateNormals();MeshObject("Faceted leaves",parent,m,Art.Material(c));
  }
  static void Tree(Vector3 p,Transform parent,int seed){
   // Keep the camera corridor open: the prior oversized canopy hid the route
   // whenever an island edge crossed the near field.
   float height=2.2f+(seed%3)*.24f;var trunk=Art.Shape("Crooked oak",PrimitiveType.Cylinder,p+Vector3.up*height*.45f,new Vector3(.25f,height*.5f,.25f),new Color(.23f,.18f,.115f),parent);trunk.transform.localRotation=Quaternion.Euler(0,0,7*Mathf.Sin(seed));var trunkCol=trunk.AddComponent<CapsuleCollider>();trunkCol.direction=1;trunkCol.radius=.5f;trunkCol.height=2f;
   for(int i=0;i<4;i++){float a=i*2.4f+seed;Vector3 at=p+new Vector3(Mathf.Sin(a)*.5f,height+i*.17f,Mathf.Cos(a)*.5f);Cone(at,1.02f-i*.1f,.92f,new Color(.2f+i*.04f,.34f+i*.035f,.13f+i*.02f),parent,8);}
   for(int i=0;i<3;i++){float a=i*2.1f;var root=Art.Shape("Exposed root",PrimitiveType.Cube,p+new Vector3(Mathf.Sin(a)*.35f,.12f,Mathf.Cos(a)*.35f),new Vector3(.16f,.16f,.85f),new Color(.23f,.18f,.115f),parent);root.transform.localRotation=Quaternion.Euler(0,a*Mathf.Rad2Deg,0);}
  }
  static void Pine(Vector3 p,Transform parent,int seed){var trunk=Art.Shape("Pine trunk",PrimitiveType.Cylinder,p+Vector3.up*1.5f,new Vector3(.23f,1.5f,.23f),new Color(.22f,.23f,.23f),parent);var trunkCol=trunk.AddComponent<CapsuleCollider>();trunkCol.direction=1;trunkCol.radius=.5f;trunkCol.height=2f;for(int i=0;i<4;i++)Cone(p+Vector3.up*(1+i*.6f),1.25f-i*.22f,1.4f,i%2==0?new Color(.32f,.48f,.48f):new Color(.78f,.86f,.84f),parent);}
  static void Tufts(Transform parent,float width,float length,int realm,System.Random rng){
   var verts=new List<Vector3>();var tris=new List<int>();var colors=new List<Color>();
   for(int i=0;i<65;i++){float x=(float)(rng.NextDouble()-.5)*(width-.4f),z=(float)(rng.NextDouble()-.5)*(length-.4f);if(Mathf.Abs(x)<1.6f)continue;float h=.16f+(float)rng.NextDouble()*.28f;Color c=realm==0?Color.Lerp(new Color(.28f,.43f,.12f),new Color(.62f,.61f,.25f),(float)rng.NextDouble()):realm==1?new Color(.66f,.5f,.24f):new Color(.66f,.82f,.84f);
    for(int n=0;n<3;n++){float a=n*1.05f;Vector3 d=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*.09f;Vector3 p=new Vector3(x,.12f,z);int k=verts.Count;verts.AddRange(new[]{p-d,p+Vector3.up*h+d*.4f,p+d});colors.AddRange(new[]{c,c,c});tris.AddRange(new[]{k,k+1,k+2,k+2,k+1,k});}
   }
   var mesh=new Mesh{name="Batched undergrowth"};mesh.SetVertices(verts);mesh.SetTriangles(tris,0);mesh.SetColors(colors);mesh.RecalculateNormals();var mat=new Material(Shader.Find("Sprites/Default"));parent.GetComponentInParent<RealmArtLifetime>().Keep(mat);mat.color=realm==0?new Color(.57f,.7f,.38f):Color.white;MeshObject("Undergrowth",parent,mesh,mat);
  }
 }
 public sealed class RealmArtLifetime:MonoBehaviour { readonly List<Object> owned=new List<Object>();public void Keep(Object asset){owned.Add(asset);}void OnDestroy(){foreach(var asset in owned)if(asset)Destroy(asset);} }
}
