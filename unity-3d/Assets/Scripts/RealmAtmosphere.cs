using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LostRealms {
 /// <summary>Phase 3 lighting, horizon and low-cost ambient motion for the three realms.</summary>
 public sealed class RealmAtmosphere:MonoBehaviour {
  readonly List<Transform> motes=new List<Transform>();
   readonly List<Transform> islets=new List<Transform>();readonly List<float> isletBase=new List<float>();
   readonly List<GameObject> rain=new List<GameObject>();
   readonly List<Transform> aurora=new List<Transform>();readonly List<Color> auroraTint=new List<Color>();
  int realm;Color accent;Material moteMaterial,cloudMaterial;
  static Material[] skyMats=new Material[4];float moteTime;

  public static void Apply(RealmWorld world,int realm){
   var atmosphere=world.gameObject.AddComponent<RealmAtmosphere>();
   atmosphere.realm=realm;atmosphere.accent=RealmGame.Accents[realm];atmosphere.Build();
  }

  void Build(){
Color[] sunColors={new Color(1f,.94f,.82f),new Color(1f,.86f,.69f),new Color(.82f,.91f,1f),new Color(1f,.83f,.72f)};
    Color[] fogColors={new Color(.24f,.39f,.34f),new Color(.47f,.29f,.21f),new Color(.32f,.49f,.63f),new Color(.28f,.16f,.22f)};
    Color[] skyColors={new Color(.34f,.48f,.48f),new Color(.48f,.48f,.52f),new Color(.42f,.57f,.72f),new Color(.38f,.40f,.51f)};
   var sun=transform.Find("Realm sunlight");
   if(sun){var light=sun.GetComponent<Light>();if(light){light.color=sunColors[realm];light.intensity=1.1f;light.shadows=LightShadows.Soft;light.shadowStrength=.58f;light.shadowBias=.045f;light.shadowNormalBias=.25f;}}
   var fillObject=new GameObject("Cool realm fill");fillObject.transform.SetParent(transform,false);
   fillObject.transform.localRotation=Quaternion.Euler(35,145,0);
   var fill=fillObject.AddComponent<Light>();fill.type=LightType.Directional;fill.color=new Color(.65f,.79f,1f);fill.intensity=realm==3?.3f:.18f;fill.shadows=LightShadows.None;
   RenderSettings.ambientMode=AmbientMode.Trilight;
   RenderSettings.ambientSkyColor=Color.Lerp(skyColors[realm],Color.white,.28f);
   RenderSettings.ambientEquatorColor=Color.Lerp(fogColors[realm],Color.white,.12f);
   RenderSettings.ambientGroundColor=Color.Lerp(skyColors[realm],Color.black,.32f);
   RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogColor=fogColors[realm];RenderSettings.fogDensity=realm==2?.0073f:realm==3?.012f:.0085f;
   QualitySettings.shadowDistance=28;QualitySettings.shadowResolution=ShadowResolution.Medium;QualitySettings.shadowProjection=ShadowProjection.StableFit;
   if(Camera.main){Camera.main.clearFlags=CameraClearFlags.Skybox;Camera.main.backgroundColor=fogColors[realm];Camera.main.allowHDR=false;}
   CreateHorizon(skyColors[realm],fogColors[realm]);CreateRouteLandmarks();CreateAmbientMotes();
   CreateSky(fogColors[realm]);CreateCloudSea();CreateFloatingIslets();
   if(realm==2){CreateRain();CreateSnow();}
   if(Camera.main){
    var post=Camera.main.GetComponent<RealmPostFx>();
    Color[] postTints={new Color(.99f,1f,.99f),new Color(1.02f,1f,.97f),new Color(.97f,1f,1.03f),new Color(1.01f,.99f,1.02f)};
    float[] postBloom={.28f,.3f,.32f,.26f};
    if(post)post.Configure(postTints[realm],postBloom[realm]);
   }
   CreateAurora();CreateAmbience();
  }
  // Frozen-realm aurora ribbons: a few large translucent bands that drift and
  // pulse overhead. One material, cheap quads, no per-pixel work beyond alpha.
  void CreateAurora(){
   if(realm!=2)return;
   var shader=Shader.Find("Sprites/Default");
   for(int i=0;i<5;i++){
    var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);quad.name="Aurora ribbon";
    var col=quad.GetComponent<Collider>();if(col)Destroy(col);
    quad.transform.SetParent(transform,false);
    quad.transform.localPosition=new Vector3(-14+i*7f,17+i%2*3f,40+i*18f);
    quad.transform.localRotation=Quaternion.Euler(0,0,90+8*Mathf.Sin(i));
    quad.transform.localScale=new Vector3(34,7+2*(i%3),1);
    var r=quad.GetComponent<Renderer>();
    var m=new Material(shader){name="Aurora "+i,color=new Color(.35f+.1f*(i%3),.95f,.75f,.16f)};
    m.renderQueue=2900;
    r.sharedMaterial=m;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;
    aurora.Add(quad.transform);auroraTint.Add(m.color);
   }
  }
  // Embers (desert) and snow motes (frozen) drifting on the breeze.
  void CreateAmbience(){
   var tex=Resources.Load<Texture2D>("VFX/Textures/smoke_04");if(!tex)return;
   Color tint=realm==0?new Color(.7f,1f,.55f):realm==1?new Color(1,.55f,.18f):realm==2?new Color(.85f,.95f,1f):new Color(1f,.45f,.2f);
   var go=new GameObject("Realm ambience");go.transform.SetParent(transform,false);go.transform.localPosition=new Vector3(0,6,55);
   var ps=go.AddComponent<ParticleSystem>();
   var main=ps.main;main.playOnAwake=false;ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
   main.loop=true;main.duration=30;main.startLifetime=9f;main.startSpeed=.6f;main.startSize=realm==1?.16f:.1f;main.maxParticles=60;main.startColor=tint;main.simulationSpace=ParticleSystemSimulationSpace.World;
var em=ps.emission;em.rateOverTime=realm==0?2f:realm==1?6f:8f;
    var vel=ps.velocityOverLifetime;vel.enabled=true;
    // All three axes must share one curve mode or Unity logs "Particle Velocity
    // curves must all be in the same mode" every simulation frame (device spam).
    vel.x=new ParticleSystem.MinMaxCurve(-.35f,.35f);vel.y=new ParticleSystem.MinMaxCurve(0f,realm==1?.7f:realm==3?.8f:.35f);vel.z=new ParticleSystem.MinMaxCurve(0f,0f);
   var sh=ps.shape;sh.shapeType=ParticleSystemShapeType.Box;sh.scale=new Vector3(26,2,120);
   var pr=go.GetComponent<ParticleSystemRenderer>();var am=new Material(Shader.Find("Sprites/Default"));am.mainTexture=tex;am.color=tint;pr.material=am;
   ps.Play();
  }
  // Frozen realm weather: looping rain volumes along the route (raw
  // instantiates, no VfxKill — they live as long as the level does).
  void CreateRain(){
   var prefab=Resources.Load<GameObject>("VFX/ga_vfx_Rain_01");if(!prefab)return;
   for(int i=0;i<3;i++){
    var go=Object.Instantiate(prefab,new Vector3(0,13,20+i*40),Quaternion.identity,transform);
    go.transform.localScale=Vector3.one*2.5f;
    Vfx.Repair(go);
    rain.Add(go);
   }
  }
  void CreateSnow(){
   if(!FindAnyObjectByType<SnowFollowCamera>()){
    var target=Camera.main?Camera.main.transform:(RealmGame.I&&RealmGame.I.Player?RealmGame.I.Player.transform:null);
    SnowFollowCamera.Create(transform,target);
   }
  }

  // Gradient skybox + sun + stars so the islands read as floating in open sky.
  // The horizon band matches the fog color, hiding the seam with far geometry.
  static Material SkyFor(int r,Color horizon){
   if(skyMats[r]&&skyMats[r])return skyMats[r];
   if(r==0){
    var tex=Resources.Load<Texture2D>("Art/Realm0_Sky360");
    var panoShader=Resources.Load<Shader>("Shaders/RealmPanoramicSky")??Shader.Find("LostRealms/RealmPanoramicSky");
    if(tex&&panoShader){
     var pm=new Material(panoShader){name="Realm 0 Panoramic Sky"};
     pm.SetTexture("_MainTex",tex);
     if(pm.HasProperty("_Exposure"))pm.SetFloat("_Exposure",1.05f);
     if(pm.HasProperty("_Rotation"))pm.SetFloat("_Rotation",90f);
     skyMats[r]=pm;return pm;
    }
   }
   var shader=Shader.Find("LostRealms/RealmSkyBox");if(!shader)return null;
Color[] tops={new Color(.13f,.38f,.52f),new Color(.25f,.45f,.70f),new Color(.05f,.12f,.28f),new Color(.22f,.12f,.18f)};
    Color[] grounds={new Color(.10f,.16f,.16f),new Color(.35f,.22f,.15f),new Color(.08f,.12f,.20f),new Color(.55f,.30f,.22f)};
    Color[] suns={new Color(1,.93f,.75f),new Color(1,.8f,.55f),new Color(.85f,.92f,1f),new Color(1f,.6f,.4f)};
    float[] stars={.25f,.12f,.85f,.5f};
   var m=new Material(shader){name="Realm sky "+r};
   m.SetColor("_TopColor",tops[r]);m.SetColor("_HorizonColor",horizon);m.SetColor("_GroundColor",grounds[r]);
   m.SetColor("_SunColor",suns[r]);m.SetVector("_SunDir",Quaternion.Euler(48,-35,0)*Vector3.back);
   m.SetFloat("_StarAmount",stars[r]);skyMats[r]=m;return m;
  }
  void CreateSky(Color fog){
   var sky=SkyFor(realm,fog);
   if(sky){RenderSettings.skybox=sky;if(Camera.main)Camera.main.clearFlags=CameraClearFlags.Skybox;}
  }
  // A single drifting particle cloud sea far below the route (one draw call).
  void CreateCloudSea(){
   var tex=Resources.Load<Texture2D>("VFX/Textures/smoke_04");if(!tex)return;
   var go=new GameObject("Cloud sea");go.transform.SetParent(transform,false);go.transform.localPosition=new Vector3(0,-10,55);
   var ps=go.AddComponent<ParticleSystem>();
   var main=ps.main;main.playOnAwake=false;ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
   main.loop=true;main.duration=40;main.startLifetime=38;main.startSpeed=.5f;main.startSize=15f;main.maxParticles=34;main.simulationSpace=ParticleSystemSimulationSpace.World;
   Color[] tints={new Color(.85f,.95f,.88f),new Color(.98f,.9f,.78f),new Color(.82f,.9f,.98f),new Color(.95f,.8f,.7f)};
   main.startColor=tints[realm];
   var em=ps.emission;em.rateOverTime=1.1f;
   var sh=ps.shape;sh.shapeType=ParticleSystemShapeType.Box;sh.scale=new Vector3(80,4,140);
   var sz=ps.sizeOverLifetime;sz.enabled=true;sz.size=new ParticleSystem.MinMaxCurve(1f,new AnimationCurve(new Keyframe(0,0),new Keyframe(.25f,1),new Keyframe(1,1.4f)));
   var col=ps.colorOverLifetime;col.enabled=true;
   var grad=new Gradient();
   grad.SetKeys(new[]{new GradientColorKey(Color.white,0)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.55f,.2f),new GradientAlphaKey(.55f,.8f),new GradientAlphaKey(0,1)});
   col.color=new ParticleSystem.MinMaxGradient(grad);
   var pr=go.GetComponent<ParticleSystemRenderer>();
   cloudMaterial=new Material(Shader.Find("Sprites/Default")){name="Cloud sea"};cloudMaterial.mainTexture=tex;cloudMaterial.color=Color.white;
   pr.material=cloudMaterial;
   ps.Play();
  }
  // Distant floating rock islets ringing the route for depth.
  void CreateFloatingIslets(){
   var random=new System.Random(realm*331+7);
Color rock=realm==0?new Color(.16f,.24f,.22f):realm==1?new Color(.4f,.27f,.18f):realm==2?new Color(.25f,.36f,.48f):new Color(.2f,.15f,.17f);
    Color topC=realm==0?new Color(.2f,.42f,.3f):realm==1?new Color(.66f,.42f,.22f):realm==2?new Color(.62f,.78f,.85f):new Color(.34f,.22f,.26f);
   for(int i=0;i<5;i++){
    float a=(float)i/5f*Mathf.PI*2f+(float)random.NextDouble()*.5f;
    float r=26+(float)random.NextDouble()*18f;
    var root=new GameObject("Floating islet");root.transform.SetParent(transform,false);
    root.transform.localPosition=new Vector3(Mathf.Cos(a)*r,-7+(float)random.NextDouble()*9f,55+Mathf.Sin(a)*r);
    float s=1.5f+(float)random.NextDouble()*2.5f;
    var crag=Art.Shape("Islet rock",PrimitiveType.Cylinder,Vector3.zero,new Vector3(s*1.1f,s*2.2f,s*1.1f),rock,root.transform);SetDecorative(crag);
    var cap=Art.Shape("Islet cap",PrimitiveType.Cylinder,new Vector3(0,s*1.05f,0),new Vector3(s*1.35f,s*.3f,s*1.35f),topC,root.transform);SetDecorative(cap);
    islets.Add(root.transform);isletBase.Add(root.transform.localPosition.y);
   }
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
    Color stone=realm==0?new Color(.15f,.28f,.2f):realm==1?new Color(.35f,.21f,.14f):realm==2?new Color(.22f,.38f,.52f):new Color(.24f,.16f,.19f);
    var pillar=Art.Shape("Landmark monolith",PrimitiveType.Cylinder,Vector3.up*3.3f,new Vector3(1.15f,3.3f,1.15f),stone,root.transform);SetDecorative(pillar);
    var crown=Art.Shape("Landmark lens",PrimitiveType.Sphere,Vector3.up*6.55f,Vector3.one*.63f,Color.Lerp(accent,Color.white,.28f),root.transform);SetDecorative(crown);
    var ring=Art.Ring(Vector3.up*6.25f,1.05f,accent,root.transform);foreach(Transform piece in ring.transform)SetDecorative(piece.gameObject);
   }
  }

  void CreateAmbientMotes(){
   moteMaterial=new Material(Shader.Find("Sprites/Default"));moteMaterial.name="Realm ambient mote";moteMaterial.color=Color.Lerp(accent,Color.white,.42f);moteMaterial.renderQueue=3000;
   int count=realm==0?22:realm==1?18:realm==2?24:28;
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
    for(int i=0;i<islets.Count;i++)if(islets[i]){
     var isle=islets[i];Vector3 p=isle.localPosition;
     p.y=isletBase[i]+Mathf.Sin(moteTime*.5f+i*1.7f)*.5f;isle.localPosition=p;
    }
    for(int i=0;i<aurora.Count;i++)if(aurora[i]){
     var band=aurora[i];var r=band.GetComponent<Renderer>();
     float pulse=.5f+.5f*Mathf.Sin(moteTime*.5f+i*1.3f);
     band.localPosition=new Vector3(band.localPosition.x+Mathf.Sin(moteTime*.2f+i)*Time.deltaTime*.4f,band.localPosition.y,band.localPosition.z);
     band.localScale=new Vector3(34,7+2*(i%3)+pulse*2.5f,1);
     if(r&&r.sharedMaterial){var c=auroraTint[i];c.a=.10f+.12f*pulse;r.sharedMaterial.color=c;}
    }
   }

   void OnDestroy(){if(moteMaterial)Destroy(moteMaterial);if(cloudMaterial)Destroy(cloudMaterial);foreach(var r in rain)if(r)Destroy(r);}
 }
}
