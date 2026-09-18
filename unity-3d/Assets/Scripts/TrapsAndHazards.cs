using System.Collections.Generic;
using UnityEngine;

namespace LostRealms {
 // =========================================================================
 // 1. RETRACTABLE FLOOR SPIKE TRAP
 // =========================================================================
 public sealed class SpikeTrap:MonoBehaviour {
  Transform spikeRoot;Color trapColor;
  float timer,phaseOffset;enum State{Dormant,Telegraph,Active,Retract}State state=State.Dormant;
  Vector3 baseLocal;
  float thrustHeight=2.3f;
  static Material baseMatInst;
  public static Material GetBaseMaterial(){
   if(!baseMatInst){
    var tex=Resources.Load<Texture2D>("Props/Textures/ColorPaletteRED");
    baseMatInst=new Material(Shader.Find("Standard")){name="GrateTrap_Mat",color=Color.white};
    if(tex)baseMatInst.mainTexture=tex;
    baseMatInst.SetFloat("_Metallic",.45f);baseMatInst.SetFloat("_Glossiness",.45f);
   }
   return baseMatInst;
  }
  public static Material GetSpikesMaterial(Color accent){
   var tex=Resources.Load<Texture2D>("Props/Textures/ColorPaletteRED");
   var mat=new Material(Shader.Find("Standard")){name="SpikeTrap_Mat",color=Color.Lerp(Color.white,accent,.25f)};
   if(tex)mat.mainTexture=tex;
   mat.SetFloat("_Metallic",.75f);mat.SetFloat("_Glossiness",.6f);
   return mat;
  }

  public static SpikeTrap Place(Transform parent,Vector3 localPos,Color accent){
   var go=new GameObject("Spike Trap");go.transform.SetParent(parent,false);go.transform.localPosition=localPos;
   var gratePrefab=Resources.Load<GameObject>("Props/GrateTrapD");
   var spearPrefab=Resources.Load<GameObject>("Props/SpearD");
   var trap=go.AddComponent<SpikeTrap>();
   trap.trapColor=accent;trap.phaseOffset=Random.value*1.5f;

   if(gratePrefab&&spearPrefab){
    var grateObj=Instantiate(gratePrefab,go.transform,false);
    grateObj.name="GrateFrame";
    grateObj.transform.localPosition=Vector3.zero;
    grateObj.transform.localRotation=Quaternion.identity;
    var bMat=GetBaseMaterial();
    foreach(var r in grateObj.GetComponentsInChildren<Renderer>(true))r.sharedMaterial=bMat;

    var spikes=new GameObject("Spikes");spikes.transform.SetParent(go.transform,false);
    spikes.transform.localPosition=new Vector3(0,-2.1f,0);
    var sMat=GetSpikesMaterial(accent);

    Vector2[] spearPositions=new Vector2[]{
     new Vector2(-0.70f, 0.67f),
     new Vector2( 0.00f, 0.67f),
     new Vector2( 0.68f, 0.67f),
     new Vector2(-0.70f, 0.00f),
     new Vector2( 0.68f, 0.00f),
     new Vector2(-0.70f,-0.67f),
     new Vector2( 0.00f,-0.67f),
     new Vector2( 0.68f,-0.67f)
    };

     foreach(var p in spearPositions){
      var sp=Instantiate(spearPrefab,spikes.transform,false);
      sp.name="Spear";
      sp.transform.localPosition=new Vector3(p.x,0,p.y);
      sp.transform.localRotation=Quaternion.Euler(0,-90f,0);
      foreach(var r in sp.GetComponentsInChildren<Renderer>(true))r.sharedMaterial=sMat;
     }

    trap.spikeRoot=spikes.transform;trap.baseLocal=spikes.transform.localPosition;
    trap.thrustHeight=2.3f;
   }else{
    // Fallback if 3D assets not present
    Art.Shape("TrapRim",PrimitiveType.Cube,new Vector3(0,.04f,0),new Vector3(2.1f,.12f,2.1f),new Color(.18f,.18f,.2f),go.transform);
    Art.Shape("TrapGrate",PrimitiveType.Cube,new Vector3(0,.05f,0),new Vector3(1.75f,.1f,1.75f),new Color(.08f,.08f,.1f),go.transform);
    var spikes=new GameObject("Spikes");spikes.transform.SetParent(go.transform,false);spikes.transform.localPosition=new Vector3(0,-.35f,0);
    Color spikeCol=Color.Lerp(Color.gray,accent,.35f);
    for(int x=-1;x<=1;x++){
     for(int z=-1;z<=1;z++){
      var tip=Art.Shape("SpikeTip",PrimitiveType.Cube,new Vector3(x*.55f,.42f,z*.55f),new Vector3(.12f,.75f,.12f),spikeCol,spikes.transform);
      tip.transform.localRotation=Quaternion.Euler(4,x*15+z*10,4);
     }
    }
    trap.spikeRoot=spikes.transform;trap.baseLocal=spikes.transform.localPosition;
    trap.thrustHeight=1.05f;
   }
   return trap;
  }

  void Update(){
   var g=RealmGame.I;if(!g||g.Screen!=GameScreen.Playing||!g.Player)return;
   timer+=Time.deltaTime;
   switch(state){
    case State.Dormant:
     spikeRoot.localPosition=baseLocal;
     if(timer>=2.2f+phaseOffset){state=State.Telegraph;timer=0;phaseOffset=0;}
     break;
    case State.Telegraph:
     // Rattle spikes and warn with sound/sparks
     float shake=Mathf.Sin(timer*45f)*.02f;
     spikeRoot.localPosition=baseLocal+Vector3.up*(thrustHeight*.18f+shake);
     if(timer<.05f)g.Sound("step");
     if(timer>=.65f){
      state=State.Active;timer=0;g.Sound("blade");
      HitSpark.Burst(transform.position+Vector3.up*.2f,Vector3.up,trapColor,10);
     }
     break;
    case State.Active:
     // Fully thrust upward
     spikeRoot.localPosition=baseLocal+Vector3.up*thrustHeight;
     // Damage player if in range
     Vector3 pPos=g.Player.transform.position;
     Vector3 delta=pPos-transform.position;
     if(Mathf.Abs(delta.x)<1.05f&&Mathf.Abs(delta.z)<1.05f&&delta.y>-.2f&&delta.y<1.3f){
      if(g.Player.Damage(1,transform.position))
       HitSpark.Burst(pPos+Vector3.up*.8f,Vector3.up,Color.red,12);
     }
     // Damage enemies walking into the trap
     if(g.Enemies!=null){
      foreach(var foe in g.Enemies){
       if(!foe||foe.Health<=0)continue;
       Vector3 fd=foe.transform.position-transform.position;
       if(Mathf.Abs(fd.x)<1.1f&&Mathf.Abs(fd.z)<1.1f&&Mathf.Abs(fd.y)<1.3f){
        foe.Hit(2f,0,false,Vector3.up);
       }
      }
     }
     if(timer>=1.1f){state=State.Retract;timer=0;}
     break;
    case State.Retract:
     float t=Mathf.Clamp01(timer/.35f);
     spikeRoot.localPosition=Vector3.Lerp(baseLocal+Vector3.up*thrustHeight,baseLocal,t);
     if(timer>=.35f){state=State.Dormant;timer=0;}
     break;
   }
  }
 }

 // =========================================================================
 // 2. TRAVERSING SAW BLADE TRAP
 // =========================================================================
 public sealed class SawTrap:MonoBehaviour {
  Transform carriage;
  Transform bladeSpinRoot;
  float halfLength;
  float speed=2.8f;
  bool forward=true;
  float pauseTimer;
  float curX;

  public static SawTrap Place(Transform parent,Vector3 startLocal,Vector3 endLocal,Color accent){
   Vector3 mid=(startLocal+endLocal)*.5f;
   // If parent is World root, find the island under mid so the trap is parented to the moving platform
   if(parent&&parent.GetComponent<RealmWorld>()!=null){
    RaycastHit rh;
    Vector3 worldMid=parent.TransformPoint(mid);
    if(Physics.Raycast(worldMid+Vector3.up*8f,Vector3.down,out rh,16f,~0,QueryTriggerInteraction.Ignore)){
     Transform t=rh.transform;
     while(t!=null&&t!=parent){
      if(t.name=="Island"){
       startLocal=t.InverseTransformPoint(parent.TransformPoint(startLocal));
       endLocal=t.InverseTransformPoint(parent.TransformPoint(endLocal));
       mid=(startLocal+endLocal)*.5f;
       parent=t;
       break;
      }
      t=t.parent;
     }
    }
   }

   var go=new GameObject("Saw Trap");
   go.transform.SetParent(parent,false);
   go.transform.localPosition=mid;

   Vector3 dir=endLocal-startLocal;
   float length=dir.magnitude;
   if(length<0.2f){length=3.2f;dir=Vector3.right;}
   Vector3 normDir=dir.normalized;
   go.transform.localRotation=Quaternion.FromToRotation(Vector3.right,normDir);

   // 1. Runic rail/track along local X
   var trackPrefab=Resources.Load<GameObject>("Props/RunicTrack");
   if(trackPrefab){
    var tr=Instantiate(trackPrefab,go.transform,false);
    tr.name="RunicTrack";
    tr.transform.localPosition=Vector3.zero;
    tr.transform.localRotation=Quaternion.identity;
    tr.transform.localScale=new Vector3(length/4.4f,1f,1f);
    var tTex=Resources.Load<Texture2D>("Props/Textures/RunicTrack_basecolor");
    if(tTex){
     var tmat=new Material(Shader.Find("Standard")){name="RunicTrack_Mat"};
     tmat.mainTexture=tTex;tmat.SetFloat("_Glossiness",.3f);
     foreach(var r in tr.GetComponentsInChildren<Renderer>(true))r.sharedMaterial=tmat;
    }
   }else{
    Art.Shape("SawTrack",PrimitiveType.Cube,Vector3.zero,new Vector3(length,.08f,.35f),new Color(.12f,.12f,.15f),go.transform);
   }
   var trackCol=go.GetComponent<BoxCollider>()??go.AddComponent<BoxCollider>();
   trackCol.center=new Vector3(0,.13f,0);
   trackCol.size=new Vector3(length,.26f,.95f);

   // 2. Carriage traversing along local X inside track groove (local Z = 0)
   // Formula: minPos = -halfRail + bladeRadius + safetyMargin; maxPos = halfRail - bladeRadius - safetyMargin
   float halfRail=length*.5f;
   float bladeRadius=.925f;
   float safetyMargin=.15f;
   float halfLen=Mathf.Max(.2f,halfRail-bladeRadius-safetyMargin);

   var carriageObj=new GameObject("SawCarriage");
   carriageObj.transform.SetParent(go.transform,false);
   carriageObj.transform.localPosition=new Vector3(-halfLen,.40f,0f);

   // 3. Saw blade spinning root (spins around local Z axis - its true axle)
   var bladeSpin=new GameObject("BladeSpinRoot");
   bladeSpin.transform.SetParent(carriageObj.transform,false);
   bladeSpin.transform.localPosition=Vector3.zero;
   bladeSpin.transform.localRotation=Quaternion.identity;

   var sawPrefab=Resources.Load<GameObject>("Props/RunicSaw")??Resources.Load<GameObject>("Props/SawBlade");
   if(sawPrefab){
    var sawObj=Instantiate(sawPrefab,bladeSpin.transform,false);
    sawObj.name="RunicSawMesh";
    sawObj.transform.localPosition=Vector3.zero;
    sawObj.transform.localRotation=Quaternion.identity;
    sawObj.transform.localScale=Vector3.one;
    var sTex=Resources.Load<Texture2D>("Props/Textures/RunicSaw_basecolor");
    if(sTex){
     var smat=new Material(Shader.Find("Standard")){name="RunicSaw_Mat"};
     smat.mainTexture=sTex;smat.SetFloat("_Glossiness",.45f);
     foreach(var r in sawObj.GetComponentsInChildren<Renderer>(true))r.sharedMaterial=smat;
    }
   }else{
    var disc=Art.Shape("BladeDisc",PrimitiveType.Cylinder,Vector3.zero,new Vector3(1.35f,.05f,1.35f),Color.Lerp(Color.white,accent,.25f),bladeSpin.transform);
    disc.transform.localRotation=Quaternion.Euler(90f,0,0);
    var hub=Art.Shape("BladeHub",PrimitiveType.Cylinder,Vector3.zero,new Vector3(.45f,.12f,.45f),new Color(.2f,.2f,.22f),bladeSpin.transform);
    hub.transform.localRotation=Quaternion.Euler(90f,0,0);
    for(int i=0;i<6;i++){
     float ang=i*60f*Mathf.Deg2Rad;
     var tooth=Art.Shape("Tooth",PrimitiveType.Cube,new Vector3(Mathf.Cos(ang)*.65f,Mathf.Sin(ang)*.65f,0f),new Vector3(.24f,.04f,.24f),Color.white,bladeSpin.transform);
     tooth.transform.localRotation=Quaternion.Euler(0,0,i*60f+25f);
    }
   }

   var st=go.AddComponent<SawTrap>();
   st.carriage=carriageObj.transform;
   st.bladeSpinRoot=bladeSpin.transform;
   st.halfLength=halfLen;
   st.curX=-halfLen;
   return st;
  }

  void Update(){
   var g=RealmGame.I;if(!g||g.Screen!=GameScreen.Playing||!g.Player)return;
   // Spin around local Z (the circular blade's axle)
   float spinDir=forward?-1f:1f;
   if(bladeSpinRoot)bladeSpinRoot.Rotate(0,0,spinDir*840f*Time.deltaTime,Space.Self);

   if(pauseTimer>0){pauseTimer-=Time.deltaTime;return;}

   // Move carriage strictly along local X between -halfLength and +halfLength
   float targetX=forward?halfLength:-halfLength;
   curX=Mathf.MoveTowards(curX,targetX,speed*Time.deltaTime);
   if(carriage)carriage.localPosition=new Vector3(curX,.40f,0f);

   if(Mathf.Abs(curX-targetX)<.02f){
    forward=!forward;pauseTimer=.35f;
    Vector3 sparkPos=carriage?carriage.position:transform.position;
    HitSpark.Burst(sparkPos+Vector3.up*.2f,Vector3.up,new Color(1f,.85f,.3f),8);
   }

   // Hazard damage in world space
   Vector3 hitPos=carriage?carriage.position:transform.position;
   Vector3 pPos=g.Player.transform.position;
   if(Vector3.Distance(hitPos,pPos+Vector3.up*.8f)<1.2f){
    Vector3 knock=pPos-hitPos;knock.y=0;
    if(g.Player.Damage(1,hitPos))
     HitSpark.Burst(pPos+Vector3.up*.8f,knock.normalized,new Color(1f,.9f,.4f),12);
   }
   if(g.Enemies!=null){
    foreach(var foe in g.Enemies){
     if(!foe||foe.Health<=0)continue;
     Vector3 fPos=foe.transform.position;
     if(Vector3.Distance(hitPos,fPos+Vector3.up*.8f)<1.2f){
      Vector3 fKnock=fPos-hitPos;fKnock.y=0;
      foe.Hit(2f,0,false,fKnock.normalized);
     }
    }
   }
  }
 }

 // =========================================================================
 // 3. VOLCANIC MAGMA / STEAM FIRE GEYSER
 // =========================================================================
 public sealed class FireGeyser:MonoBehaviour {
  GameObject plume;Light plumeLight;float timer,cycleOffset;
  enum Phase{Dormant,Warning,Erupting}Phase phase=Phase.Dormant;
  Color geyserCol;
  public static FireGeyser Place(Transform parent,Vector3 localPos,Color accent){
   var go=new GameObject("Fire Geyser");go.transform.SetParent(parent,false);go.transform.localPosition=localPos;
    // Volcanic stone crater
    var ventPrefab=Resources.Load<GameObject>("Props/LavaVent");
    if(ventPrefab){
     var vent=Instantiate(ventPrefab,go.transform,false);
     vent.name="LavaVentVisual";vent.transform.localPosition=Vector3.zero;
     vent.transform.localScale=Vector3.one;
     var vTex=Resources.Load<Texture2D>("Props/Textures/LavaVent_basecolor");
     if(vTex){
      var vmat=new Material(Shader.Find("Standard")){name="LavaVent_Mat"};
      vmat.mainTexture=vTex;
      foreach(var vr in vent.GetComponentsInChildren<Renderer>(true))vr.sharedMaterial=vmat;
     }
    }else{
     Art.Shape("CraterRim",PrimitiveType.Cylinder,new Vector3(0,.06f,0),new Vector3(1.8f,.14f,1.8f),new Color(.22f,.18f,.16f),go.transform);
     Art.Shape("VentCenter",PrimitiveType.Cylinder,new Vector3(0,.08f,0),new Vector3(1.1f,.12f,1.1f),new Color(.4f,.12f,.05f),go.transform);
    }
   // Flame column (deactivated by default)
   var p=Art.Shape("FlameColumn",PrimitiveType.Cylinder,Vector3.up*1.75f,new Vector3(.85f,3.5f,.85f),new Color(1f,.45f,.12f),go.transform);
   var r=p.GetComponent<Renderer>();
   var mat=new Material(Shader.Find("Sprites/Default"));mat.color=new Color(1f,.55f,.15f,.65f);r.sharedMaterial=mat;
   r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
   var lgo=new GameObject("FlameLight");lgo.transform.SetParent(go.transform,false);lgo.transform.localPosition=Vector3.up*1.8f;
   var light=lgo.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1f,.5f,.1f);light.range=6f;light.intensity=0f;
   p.SetActive(false);
   var fg=go.AddComponent<FireGeyser>();
   fg.plume=p;fg.plumeLight=light;fg.geyserCol=accent;fg.cycleOffset=Random.value*1.8f;
   return fg;
  }

  void Update(){
   var g=RealmGame.I;if(!g||g.Screen!=GameScreen.Playing||!g.Player)return;
   timer+=Time.deltaTime;
   switch(phase){
    case Phase.Dormant:
     if(plume.activeSelf)plume.SetActive(false);
     if(plumeLight.intensity>0)plumeLight.intensity=0;
     if(timer>=2.6f+cycleOffset){phase=Phase.Warning;timer=0;cycleOffset=0;}
     break;
    case Phase.Warning:
     // Smoke/rumble telegraph
     if(timer<.05f){g.Sound("impact");}
     HitSpark.Burst(transform.position+Vector3.up*.2f,Vector3.up,new Color(1f,.6f,.1f),3);
     if(timer>=.8f){
      phase=Phase.Erupting;timer=0;
      plume.SetActive(true);plumeLight.intensity=1.8f;
      g.Sound("ember_cast");
      Vfx.Play("ga_vfx_FireBall_01",transform.position+Vector3.up*1.5f,Quaternion.Euler(-90,0,0),1.1f);
     }
     break;
    case Phase.Erupting:
     // Swirling column and pulse
     plume.transform.localScale=new Vector3(.85f+Mathf.Sin(timer*25f)*.15f,3.5f,.85f+Mathf.Cos(timer*25f)*.15f);
     plume.transform.Rotate(0,360f*Time.deltaTime,0,Space.Self);
     Vector3 pPos=g.Player.transform.position;
     Vector3 d=pPos-transform.position;
     if(Mathf.Abs(d.x)<1.1f&&Mathf.Abs(d.z)<1.1f&&d.y>-0.2f&&d.y<3.8f){
      if(g.Player.Damage(1,transform.position)){
       g.Player.Bounce(9.2f);
       HitSpark.Burst(pPos+Vector3.up*.9f,Vector3.up,new Color(1f,.4f,.1f),14);
      }
     }
     if(g.Enemies!=null){
      foreach(var foe in g.Enemies){
       if(!foe||foe.Health<=0)continue;
       Vector3 fd=foe.transform.position-transform.position;
       if(Mathf.Abs(fd.x)<1.2f&&Mathf.Abs(fd.z)<1.2f&&fd.y>-0.2f&&fd.y<3.8f){
        foe.Hit(3f,0,false,Vector3.up);
       }
      }
     }
     if(timer>=1.2f){phase=Phase.Dormant;timer=0;}
     break;
   }
  }
 }

 // =========================================================================
 // 4. SWINGING PENDULUM GUILLOTINE
 // =========================================================================
 public sealed class PendulumTrap:MonoBehaviour {
  Transform pivot,blade;float phase,speed=2.4f,maxAngle=55f;
  public static PendulumTrap Place(Transform parent,Vector3 localPos,float width,Color accent){
   var go=new GameObject("Pendulum Trap");go.transform.SetParent(parent,false);go.transform.localPosition=localPos;
   // Overhead archway placed on Ignore Raycast layer (2) so FollowCamera is never blocked!
   var archRoot=new GameObject("Archway");archRoot.transform.SetParent(go.transform,false);
   archRoot.layer=2;
   var archPrefab=Resources.Load<GameObject>("Props/CelticArch");
   if(archPrefab){
    var arch=Instantiate(archPrefab,archRoot.transform,false);
    arch.name="CelticArch";arch.transform.localPosition=Vector3.zero;
    arch.transform.localScale=new Vector3(width/4.6f,1f,1f);
    var aTex=Resources.Load<Texture2D>("Props/Textures/CelticArch_basecolor");
    if(aTex){
     var amat=new Material(Shader.Find("Standard")){name="CelticArch_Mat"};
     amat.mainTexture=aTex;amat.SetFloat("_Glossiness",.25f);
     foreach(var r in arch.GetComponentsInChildren<Renderer>(true)){r.sharedMaterial=amat;r.gameObject.layer=2;}
    }
   }else{
    for(int side=-1;side<=1;side+=2){
     var col=Art.Shape("ArchPillar",PrimitiveType.Cube,new Vector3(side*(width*.5f),2.5f,0),new Vector3(.65f,5.0f,.65f),new Color(.2f,.22f,.25f),archRoot.transform);
     col.layer=2;
    }
    var lintel=Art.Shape("ArchLintel",PrimitiveType.Cube,new Vector3(0,5.0f,0),new Vector3(width+1.1f,.6f,.8f),new Color(.16f,.18f,.2f),archRoot.transform);
    lintel.layer=2;
   }
   // Swinging pendulum rod & blade mounted at top pivot
   var pivotGo=new GameObject("Pivot");pivotGo.transform.SetParent(go.transform,false);pivotGo.transform.localPosition=new Vector3(0,4.2f,0);
   Transform bladeHitPoint=null;
   var bladePrefab=Resources.Load<GameObject>("Props/GuillotineBlade");
   if(bladePrefab){
    var bladeObj=Instantiate(bladePrefab,pivotGo.transform,false);
    bladeObj.name="GuillotineBladeVisual";
    bladeObj.transform.localPosition=Vector3.zero;
    bladeObj.transform.localRotation=Quaternion.identity;
    bladeObj.transform.localScale=Vector3.one;
    var bTex=Resources.Load<Texture2D>("Props/Textures/GuillotineBlade_basecolor");
    if(bTex){
     var bmat=new Material(Shader.Find("Standard")){name="Guillotine_Mat"};
     bmat.mainTexture=bTex;bmat.SetFloat("_Glossiness",.35f);
     foreach(var r in bladeObj.GetComponentsInChildren<Renderer>(true))r.sharedMaterial=bmat;
    }
    var anchor=new GameObject("BladeHitAnchor");anchor.transform.SetParent(pivotGo.transform,false);
    anchor.transform.localPosition=new Vector3(0,-3.2f,0);
    bladeHitPoint=anchor.transform;
   }else{
    Art.Shape("PivotRod",PrimitiveType.Cylinder,new Vector3(0,-2.1f,0),new Vector3(.14f,4.2f,.14f),new Color(.3f,.3f,.34f),pivotGo.transform);
    var bladeGo=Art.Shape("PendulumBlade",PrimitiveType.Cube,new Vector3(0,-4.15f,0),new Vector3(1.8f,.75f,.16f),Color.Lerp(Color.white,accent,.35f),pivotGo.transform);
    bladeGo.transform.localRotation=Quaternion.Euler(0,0,15);
    bladeHitPoint=bladeGo.transform;
   }
   var pt=go.AddComponent<PendulumTrap>();
   pt.pivot=pivotGo.transform;pt.blade=bladeHitPoint;pt.phase=Random.value*6.28f;
   return pt;
  }

  void Update(){
   var g=RealmGame.I;if(!g||g.Screen!=GameScreen.Playing||!g.Player)return;
   float angle=Mathf.Sin((Time.time+phase)*speed)*maxAngle;
   if(pivot)pivot.localRotation=Quaternion.Euler(0,0,angle);
   // Hazardous at lowest point of swing
   if(blade&&Mathf.Abs(angle)<32f){
    Vector3 bPos=blade.position;
    // Check Aster
    if(g.Player){
     Vector3 pPos=g.Player.transform.position+Vector3.up*.9f;
     if(Vector3.Distance(bPos,pPos)<1.65f){
      Vector3 knock=pPos-bPos;knock.y=0;
      if(g.Player.Damage(1,bPos)){
       HitSpark.Burst(pPos,knock.normalized,new Color(1f,.85f,.4f),14);
       g.Sound("impact");
      }
     }
    }
    // Check nearby enemies crossing the bridge
    if(g.Enemies!=null){
     foreach(var foe in g.Enemies){
      if(!foe||foe.Health<=0)continue;
      Vector3 fPos=foe.transform.position+Vector3.up*.8f;
      if(Vector3.Distance(bPos,fPos)<1.8f){
       Vector3 knock=fPos-bPos;knock.y=0.25f;
       foe.Hit(3f,0,false,knock.normalized);
       HitSpark.Burst(fPos,knock.normalized,new Color(1f,.4f,.2f),14);
      }
     }
    }
   }
  }
 }

 // =========================================================================
 // 5. RUNIC BOUNCE PAD (LAUNCHER)
 // =========================================================================
 public sealed class BouncePad:MonoBehaviour {
  Transform padVisual;Vector3 baseScale=Vector3.one;float squish;Color padColor;
  public static BouncePad Place(Transform parent,Vector3 localPos,Color accent){
   var go=new GameObject("Bounce Pad");go.transform.SetParent(parent,false);go.transform.localPosition=localPos;
   // Ancient runic celestial dais
   var daisPrefab=Resources.Load<GameObject>("Props/CelestialDais");
   Transform padCore=null;
   if(daisPrefab){
    var dais=Instantiate(daisPrefab,go.transform,false);
    dais.name="CelestialDaisVisual";dais.transform.localPosition=Vector3.zero;
    dais.transform.localScale=Vector3.one;
    var dTex=Resources.Load<Texture2D>("Props/Textures/CelestialDais_basecolor");
    if(dTex){
     var dmat=new Material(Shader.Find("Standard")){name="CelestialDais_Mat"};
     dmat.mainTexture=dTex;dmat.SetFloat("_Glossiness",.35f);
     foreach(var r in dais.GetComponentsInChildren<Renderer>(true))r.sharedMaterial=dmat;
    }
    padCore=dais.transform;
   }else{
    Art.Shape("DaisBase",PrimitiveType.Cylinder,new Vector3(0,.05f,0),new Vector3(2.3f,.1f,2.3f),new Color(.18f,.2f,.24f),go.transform);
    var core=Art.Shape("DaisCore",PrimitiveType.Cylinder,new Vector3(0,.09f,0),new Vector3(1.7f,.08f,1.7f),accent*.85f,go.transform);
    padCore=core.transform;
    Art.Ring(new Vector3(0,.12f,0),.7f,Color.white,go.transform);
    for(int i=0;i<4;i++){
     float ang=i*90f*Mathf.Deg2Rad;
     Art.Crystal(new Vector3(Mathf.Cos(ang)*.95f,.35f,Mathf.Sin(ang)*.95f),.45f,accent,go.transform);
    }
   }
   var bp=go.AddComponent<BouncePad>();
   bp.padVisual=padCore;bp.padColor=accent;
   return bp;
  }

  void Update(){
   var g=RealmGame.I;if(!g||g.Screen!=GameScreen.Playing||!g.Player)return;
   // Spring bounce back
   if(squish>0){
    squish-=Time.deltaTime*5f;
    float s=1f-Mathf.Sin(squish*Mathf.PI)*.35f;
    padVisual.localScale=new Vector3(baseScale.x*s,baseScale.y,baseScale.z*s);
   }
   Vector3 pPos=g.Player.transform.position;
   Vector3 d=pPos-transform.position;
   if(Mathf.Abs(d.x)<1.1f&&Mathf.Abs(d.z)<1.1f&&d.y>-.2f&&d.y<1.1f){
    // Trigger bounce
    squish=1f;
    g.Player.Bounce(15.2f);
    g.Sound("double_jump");
    Vfx.Play("ga_vfx_Portal_01",transform.position+Vector3.up*.5f,Quaternion.identity,1.2f);
    HitSpark.Burst(transform.position+Vector3.up*.4f,Vector3.up,padColor,18);
    KenneyPuff.Burst(transform.position+Vector3.up*.2f,padColor,12,1.2f);
   }
  }
 }

 // =========================================================================
 // 6. CRUMBLING FALLING PLATFORM
 // =========================================================================
 public sealed class CrumblePlatform:MonoBehaviour {
  Vector3 origin;Quaternion originRot;float shakeTimer,respawnTimer;
  enum State{Idle,Shaking,Falling,Respawning}State state=State.Idle;
  Renderer[] renderers;Collider[] colliders;float fallVelocity;
  public static CrumblePlatform Attach(GameObject island){
   var cp=island.AddComponent<CrumblePlatform>();
   cp.origin=island.transform.position;cp.originRot=island.transform.rotation;
   cp.renderers=island.GetComponentsInChildren<Renderer>();
   cp.colliders=island.GetComponentsInChildren<Collider>();
   return cp;
  }

  void Update(){
   var g=RealmGame.I;if(!g||g.Screen!=GameScreen.Playing||!g.Player)return;
   switch(state){
    case State.Idle:
     // Detect player landing on platform
     bool onIt=g.Player.Grounded&&Physics.Raycast(g.Player.transform.position+Vector3.up*.2f,Vector3.down,out var hit,.65f,~0,QueryTriggerInteraction.Ignore)&&hit.transform.IsChildOf(transform);
     if(onIt){
      state=State.Shaking;shakeTimer=0;
      g.Sound("step");
      HitSpark.Burst(transform.position+Vector3.up*.2f,Vector3.up,new Color(.8f,.7f,.5f),8);
     }
     break;
    case State.Shaking:
     shakeTimer+=Time.deltaTime;
     float sx=Mathf.Sin(shakeTimer*50f)*.06f;float sz=Mathf.Cos(shakeTimer*45f)*.06f;
     transform.position=origin+new Vector3(sx,0,sz);
     if(shakeTimer>=.75f){
      state=State.Falling;fallVelocity=0;
      g.Sound("impact");
      KenneyPuff.Burst(transform.position+Vector3.up*.2f,new Color(.6f,.55f,.45f),12,1.2f);
     }
     break;
    case State.Falling:
     fallVelocity+=28f*Time.deltaTime;
     transform.position+=Vector3.down*fallVelocity*Time.deltaTime;
     if(transform.position.y<origin.y-8f){
      SetVisible(false);state=State.Respawning;respawnTimer=0;
     }
     break;
    case State.Respawning:
     respawnTimer+=Time.deltaTime;
     if(respawnTimer>=3.2f){
      transform.position=origin;transform.rotation=originRot;
      SetVisible(true);state=State.Idle;
      Vfx.Play("ga_vfx_Portal_02",transform.position+Vector3.up*.5f,Quaternion.identity,.9f);
      HitSpark.Burst(transform.position+Vector3.up*.5f,Vector3.up,g.Accent,14);
     }
     break;
   }
  }

  void SetVisible(bool v){
   if(renderers!=null)foreach(var r in renderers)if(r)r.enabled=v;
   if(colliders!=null)foreach(var c in colliders)if(c)c.enabled=v;
  }
 }

 // =========================================================================
 // 7. AETHER SPEED RING (MID-AIR BOOST)
 // =========================================================================
 public sealed class SpeedRing:MonoBehaviour {
  Vector3 boostDir;float cooldown;
  public static SpeedRing Place(Transform parent,Vector3 localPos,Vector3 direction,Color accent){
   var go=new GameObject("Aether Speed Ring");go.transform.SetParent(parent,false);go.transform.localPosition=localPos;
   go.transform.rotation=Quaternion.LookRotation(direction);
   var ringPrefab=Resources.Load<GameObject>("Props/RunicSpeedRing");
   if(ringPrefab){
    var ring=Instantiate(ringPrefab,go.transform,false);
    ring.name="RunicRingVisual";ring.transform.localPosition=Vector3.zero;
    ring.transform.localScale=Vector3.one;
    var rTex=Resources.Load<Texture2D>("Props/Textures/RunicSpeedRing_basecolor");
    if(rTex){
     var rmat=new Material(Shader.Find("Standard")){name="RunicRing_Mat"};
     rmat.mainTexture=rTex;rmat.SetFloat("_Glossiness",.35f);
     foreach(var r in ring.GetComponentsInChildren<Renderer>(true))r.sharedMaterial=rmat;
    }
   }else{
    Art.Ring(Vector3.zero,1.4f,accent,go.transform);
    Art.Ring(Vector3.zero,1.6f,Color.white*.8f,go.transform);
   }
   var sr=go.AddComponent<SpeedRing>();sr.boostDir=direction.normalized;
   return sr;
  }

  void Update(){
   var g=RealmGame.I;if(!g||g.Screen!=GameScreen.Playing||!g.Player)return;
   transform.Rotate(0,0,45f*Time.deltaTime,Space.Self);
   if(cooldown>0){cooldown-=Time.deltaTime;return;}
   if(Vector3.Distance(transform.position,g.Player.transform.position+Vector3.up*.9f)<1.6f){
    cooldown=1.5f;
    g.Player.Boost(boostDir*13f);
    g.Player.Energy=Mathf.Min(100,g.Player.Energy+35f);
    g.Sound("player_dash");
    Vfx.Play("ga_vfx_Hyperdrive_01",transform.position,Quaternion.LookRotation(boostDir),1.1f);
    HitSpark.Burst(transform.position,boostDir,new Color(.3f,.9f,1f),16);
   }
  }
 }

 // =========================================================================
 // 8. EXPLOSIVE ELEMENTAL BARREL
 // =========================================================================
 public sealed class ExplosiveBarrel:MonoBehaviour {
  public static readonly List<ExplosiveBarrel> All=new List<ExplosiveBarrel>();
  Color barrelColor;bool exploded;
  public static void Place(Transform parent,Vector3 localPos,Color color){
   var go=new GameObject("Explosive barrel");go.transform.SetParent(parent,false);go.transform.localPosition=localPos;
   var b=go.AddComponent<ExplosiveBarrel>();b.barrelColor=color;
   Art.Shape("BarrelBody",PrimitiveType.Cylinder,Vector3.up*.4f,new Vector3(.48f,.4f,.48f),new Color(.55f,.2f,.12f),go.transform);
   Art.Shape("IronBand",PrimitiveType.Cylinder,Vector3.up*.4f,new Vector3(.52f,.09f,.52f),new Color(.2f,.2f,.2f),go.transform);
   Art.Ring(Vector3.up*.42f,.3f,new Color(1f,.6f,.1f),go.transform);
   var col=go.AddComponent<BoxCollider>();col.center=Vector3.up*.4f;col.size=new Vector3(.7f,.8f,.7f);
   All.Add(b);
  }
  void OnDestroy(){All.Remove(this);}
  void Update(){
   var g=RealmGame.I;if(!g||g.Screen!=GameScreen.Playing||!g.Player||exploded)return;
   // Explode on weapon blade slash near barrel
   if(g.Player.Attacking&&Vector3.Distance(transform.position,g.Player.transform.position)<2.2f){
    Explode();
   }
  }
  public void Explode(){
   if(exploded)return;exploded=true;All.Remove(this);
   var g=RealmGame.I;
   g.Sound("impact");
   Vfx.Play("ga_vfx_FireBall_01",transform.position+Vector3.up*.5f,Quaternion.identity,1.4f);
   HitSpark.Burst(transform.position+Vector3.up*.5f,Vector3.up,new Color(1f,.5f,.1f),24);
   ImpactMarks.Place(transform.position,1.4f,new Color(.15f,.1f,.08f));
   // AoE blast to enemies
   if(g&&g.Enemies!=null){
    foreach(var foe in g.Enemies){
     if(!foe||foe.Health<=0)continue;
     float dist=Vector3.Distance(transform.position,foe.transform.position);
     if(dist<4.5f){
      foe.Hit(3.5f,0,false,(foe.transform.position-transform.position).normalized);
     }
    }
   }
   // Push player back if too close
   if(g&&g.Player){
    float pdist=Vector3.Distance(transform.position,g.Player.transform.position);
    if(pdist<3.2f)g.Player.Damage(1,transform.position);
   }
   Destroy(gameObject);
  }
 }

 // =========================================================================
 // 9. ROTATING FLOOR BLADE TRAP (FALSE WISP)
 // =========================================================================
 public sealed class FloorBladeTrap:MonoBehaviour {
  Transform bladeRoot;Color trapColor;float timer;
  public static FloorBladeTrap Place(Transform parent,Vector3 localPos,Color accent){
   var go=new GameObject("Floor Blade Trap");go.transform.SetParent(parent,false);go.transform.localPosition=localPos;
   var prefab=Resources.Load<GameObject>("Props/FloorBladeTrap");
   var fb=go.AddComponent<FloorBladeTrap>();
   fb.trapColor=accent;
   if(prefab){
    var obj=Instantiate(prefab,go.transform,false);
    obj.transform.localPosition=Vector3.zero;
    obj.transform.localRotation=Quaternion.identity;
    obj.transform.localScale=new Vector3(1.2f,1.2f,1.2f);
    var tex=Resources.Load<Texture2D>("Props/Textures/FloorBladeTrap_basecolor");
    if(tex){
     var mat=new Material(Shader.Find("Standard")){name="FloorBlade_Mat",color=Color.white};
     mat.mainTexture=tex;mat.SetFloat("_Metallic",.4f);mat.SetFloat("_Glossiness",.5f);
     foreach(var r in obj.GetComponentsInChildren<Renderer>(true))r.sharedMaterial=mat;
    }
    fb.bladeRoot=obj.transform;
   }
   return fb;
  }
  void Update(){
   var g=RealmGame.I;if(!g||g.Screen!=GameScreen.Playing||!g.Player)return;
   timer+=Time.deltaTime;
   if(bladeRoot)bladeRoot.Rotate(0,360f*Time.deltaTime,0,Space.Self);
   Vector3 pPos=g.Player.transform.position;
   Vector3 d=pPos-transform.position;
   if(Mathf.Abs(d.x)<1.1f&&Mathf.Abs(d.z)<1.1f&&d.y>-.2f&&d.y<1.2f){
    if(g.Player.Damage(1,transform.position)){
     HitSpark.Burst(pPos+Vector3.up*.8f,Vector3.up,Color.red,12);
    }
   }
   if(g.Enemies!=null){
    foreach(var foe in g.Enemies){
     if(!foe||foe.Health<=0)continue;
     Vector3 fd=foe.transform.position-transform.position;
     if(Mathf.Abs(fd.x)<1.1f&&Mathf.Abs(fd.z)<1.1f&&Mathf.Abs(fd.y)<1.2f){
      foe.Hit(1.5f,0,false,Vector3.up);
     }
    }
   }
  }
 }
}
