using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LostRealms {
 /// <summary>Phase 3 lighting, horizon and low-cost ambient motion for the three realms.</summary>
 public sealed class RealmAtmosphere:MonoBehaviour {
  readonly List<Transform> motes=new List<Transform>();
  int realm;Color accent;Material moteMaterial;float moteTime;

  public static void Apply(RealmWorld world,int realm){
   var atmosphere=world.gameObject.AddComponent<RealmAtmosphere>();
   atmosphere.realm=realm;atmosphere.accent=RealmGame.Accents[realm];atmosphere.Build();
  }

  void Build(){
   Color[] sunColors={new Color(1f,.9f,.67f),new Color(1f,.68f,.39f),new Color(.68f,.86f,1f)};
   Color[] fogColors={new Color(.24f,.39f,.34f),new Color(.47f,.29f,.21f),new Color(.32f,.49f,.63f)};
   Color[] skyColors={new Color(.27f,.48f,.42f),new Color(.64f,.39f,.26f),new Color(.37f,.57f,.72f)};
   var sun=transform.Find("Realm sunlight");
   if(sun){var light=sun.GetComponent<Light>();if(light){light.color=sunColors[realm];light.intensity=1.18f;light.shadows=LightShadows.Soft;light.shadowStrength=.72f;}}
   RenderSettings.ambientMode=AmbientMode.Trilight;
   RenderSettings.ambientSkyColor=Color.Lerp(skyColors[realm],Color.white,.28f);
   RenderSettings.ambientEquatorColor=Color.Lerp(fogColors[realm],Color.white,.12f);
   RenderSettings.ambientGroundColor=Color.Lerp(fogColors[realm],Color.black,.48f);
   RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogColor=fogColors[realm];RenderSettings.fogDensity=realm==2?.0073f:.0085f;
   QualitySettings.shadowDistance=28;QualitySettings.shadowResolution=ShadowResolution.Medium;QualitySettings.shadowProjection=ShadowProjection.StableFit;
   if(Camera.main){Camera.main.clearFlags=CameraClearFlags.Skybox;Camera.main.backgroundColor=fogColors[realm];Camera.main.allowHDR=false;}
   CreateHorizon(skyColors[realm],fogColors[realm]);CreateRouteLandmarks();CreateAmbientMotes();
  }

  void CreateHorizon(Color sky,Color fog){
   Color silhouette=Color.Lerp(fog,Color.black,.36f);
   for(int i=0;i<15;i++){
    float side=i%2==0?-1f:1f;float z=-25+i*10.5f;float x=side*(18+(i%4)*5.3f);float height=7+(i%5)*3.1f;
    var crag=Art.Shape("Atmospheric distant crag",PrimitiveType.Cylinder,new Vector3(x,-4-(i%3)*1.7f,z),new Vector3(4.5f+(i%3),height,4.5f+(i%3)),silhouette,transform);
    crag.transform.localRotation=Quaternion.Euler((i%3)*4,i*23,7*side);SetDecorative(crag);
    if(i%3==0){var beacon=Art.Shape("Distant realm beacon",PrimitiveType.Cylinder,new Vector3(x,1.2f,z),new Vector3(.26f,1.6f,.26f),Color.Lerp(accent,sky,.42f),transform);SetDecorative(beacon);}
   }
  }

  void CreateRouteLandmarks(){
   for(int i=0;i<3;i++){
    float z=20+i*32;float side=i%2==0?-1:1;var root=new GameObject("Realm route landmark");root.transform.SetParent(transform,false);root.transform.localPosition=new Vector3(side*12.5f,-.2f,z);
    Color stone=realm==0?new Color(.15f,.28f,.2f):realm==1?new Color(.35f,.21f,.14f):new Color(.22f,.38f,.52f);
    var pillar=Art.Shape("Landmark monolith",PrimitiveType.Cylinder,Vector3.up*3.3f,new Vector3(1.15f,3.3f,1.15f),stone,root.transform);SetDecorative(pillar);
    var crown=Art.Shape("Landmark lens",PrimitiveType.Sphere,Vector3.up*6.55f,Vector3.one*.63f,Color.Lerp(accent,Color.white,.28f),root.transform);SetDecorative(crown);
    var ring=Art.Ring(Vector3.up*6.25f,1.05f,accent,root.transform);foreach(Transform piece in ring.transform)SetDecorative(piece.gameObject);
   }
  }

  void CreateAmbientMotes(){
   moteMaterial=new Material(Shader.Find("Sprites/Default"));moteMaterial.name="Realm ambient mote";moteMaterial.color=Color.Lerp(accent,Color.white,.42f);moteMaterial.renderQueue=3000;
   int count=realm==0?22:realm==1?18:24;
   var random=new System.Random(realm*977+41);
   for(int i=0;i<count;i++){
    float x=(float)(random.NextDouble()-.5)*24f;float y=.45f+(float)random.NextDouble()*4.7f;float z=-9+(float)random.NextDouble()*126f;
    var mote=Art.Shape("Ambient mote",PrimitiveType.Sphere,new Vector3(x,y,z),Vector3.one*(realm==1?.045f:.065f),Color.white,transform);
    var renderer=mote.GetComponent<Renderer>();renderer.sharedMaterial=moteMaterial;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
    motes.Add(mote.transform);
   }
  }

  static void SetDecorative(GameObject go){var renderer=go.GetComponent<Renderer>();if(renderer){renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;}}

  void Update(){
   if(!RealmGame.I||RealmGame.I.Screen!=GameScreen.Playing||!RealmGame.I.Player)return;
   moteTime+=Time.deltaTime;
   for(int i=0;i<motes.Count;i++)if(motes[i]){
    var mote=motes[i];float phase=i*.73f+moteTime*(realm==1?.42f:.6f);Vector3 p=mote.localPosition;
    p.x+=Mathf.Sin(phase)*Time.deltaTime*(realm==1?.72f:.24f);p.y+=Mathf.Cos(phase*1.7f)*Time.deltaTime*.09f;
    if(p.x>14)p.x=-14;if(p.x<-14)p.x=14;if(p.y>5.7f)p.y=.35f;mote.localPosition=p;
    float pulse=.55f+Mathf.Sin(phase*2.2f)*.32f;mote.localScale=Vector3.one*(realm==1?.035f:.047f)*(1f+pulse);
   }
  }

  void OnDestroy(){if(moteMaterial)Destroy(moteMaterial);}
 }
}
