using UnityEngine;
namespace LostRealms {
 // Imported particles supply texture, wisps and breakup; tapered ribbons keep
 // each attack's silhouette readable while it travels between frames.
 public static class ChargedWeaponArt {
  static Texture2D ribbonTexture;
  static Texture2D RibbonTexture(){
   if(ribbonTexture)return ribbonTexture;
   ribbonTexture=new Texture2D(2,32,TextureFormat.RGBA32,false){name="Soft ribbon cross-section",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
   for(int y=0;y<32;y++){float a=Mathf.Pow(1f-Mathf.Abs(y/31f*2f-1f),1.3f);for(int x=0;x<2;x++)ribbonTexture.SetPixel(x,y,new Color(1,1,1,a));}
   ribbonTexture.Apply();return ribbonTexture;
  }
  public static Material Build(Transform root,Vector3 forward,WeaponId weapon){
   bool storm=weapon==WeaponId.WardenPike,claw=weapon==WeaponId.BrassFangs,gale=weapon==WeaponId.OrnateCurvedBlade,voidBlade=weapon==WeaponId.VoidReaper;
   Color color=gale?new Color(.2f,1f,.8f):storm?new Color(.3f,.65f,1f):claw?new Color(1f,.7f,.3f):voidBlade?new Color(.75f,.3f,1f):new Color(.6f,.35f,1f);
   var material=new Material(Shader.Find("Sprites/Default")){name="Charged weapon ribbon",color=Color.white};
   material.mainTexture=RibbonTexture();
   var pivot=new GameObject("Charged visual").transform;pivot.SetParent(root,false);pivot.rotation=Quaternion.LookRotation(forward);
   if(gale)pivot.Rotate(0,0,Vector3.Dot(forward,RealmGame.I.Player.transform.right)<0?-38f:38f,Space.Self);
   var fx=Vfx.Attach(storm?"mayker_Slash Projectile VFX Eletric":claw||voidBlade?"mayker_Slash VFX":"mayker_Slash Projectile VFX Water",pivot,Vector3.zero,Vector3.one*(claw?.32f:.55f));
   if(fx){
    // Store demos can contain movement scripts and screen distortion planes.
    foreach(var script in fx.GetComponentsInChildren<MonoBehaviour>(true))if(script&&!(script is VfxKill))script.enabled=false;
    foreach(var t in fx.GetComponentsInChildren<Transform>())if(t.name.Contains("Distortion")||t.name=="Decal")t.gameObject.SetActive(false);
    foreach(var ps in fx.GetComponentsInChildren<ParticleSystem>()){var main=ps.main;main.startColor=color;}
   }
   for(int layer=0;layer<2;layer++){
    var go=new GameObject(layer==0?"Luminous edge":"Soft colored rim");go.transform.SetParent(pivot,false);
    var line=go.AddComponent<LineRenderer>();line.useWorldSpace=false;line.sharedMaterial=material;line.positionCount=25;line.numCornerVertices=3;line.numCapVertices=3;
    line.widthCurve=new AnimationCurve(new Keyframe(0,0),new Keyframe(.5f,layer==0?.13f:.42f),new Keyframe(1,0));
    line.startColor=line.endColor=layer==0?Color.Lerp(color,Color.white,.8f):new Color(color.r,color.g,color.b,.65f);
    for(int i=0;i<25;i++){
     float t=i/24f;float a=Mathf.Lerp(-1.3f,1.3f,t);
     Vector3 p=storm?new Vector3(Mathf.Sin(t*36f)*.12f,Mathf.Sin(t*27f)*.08f,(t-.5f)*2f):claw?new Vector3(Mathf.Cos(a)*.32f-.2f,Mathf.Sin(a)*.65f,0):new Vector3(Mathf.Sin(a)*1.15f,Mathf.Cos(a)*.5f-.3f,0);
     line.SetPosition(i,p);
    }
   }
   var trail=root.gameObject.AddComponent<TrailRenderer>();trail.sharedMaterial=material;trail.time=.12f;trail.startWidth=.07f;trail.endWidth=0;trail.minVertexDistance=.07f;
   trail.startColor=new Color(color.r,color.g,color.b,.4f);trail.endColor=new Color(color.r,color.g,color.b,0);
   Vfx.Play(storm?"ga_vfx_MuzzleFlash_01":"mayker_Slash VFX",root.position,Quaternion.LookRotation(forward),.45f);
   return material;
  }
 }
}
