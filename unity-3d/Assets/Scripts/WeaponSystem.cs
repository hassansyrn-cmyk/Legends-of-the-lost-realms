using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace LostRealms {
 public enum WeaponId { AstersBlade=0, LongSword=1, Axe=2, CurvedSword=3, BlinkAxe=4, BlinkMace=5, BlinkSpear=6, AxeIron=7, AxeBattle=8, AxeBearded=9, AxeCleaver=10, SwordShort=11, SwordFalchion=12, SwordSabre=13, SwordClaymore=14, SwordZweihander=15, SwordGreat=16, HovlMagic=17, HovlMoon=18, HovlMoon2=19, PureAxe2H=20, PureHammer=21, PureScythe=22, PureSword2H=23, PureSpear=24, PureSword1H=25, HalberdA=26, HalberdB=27, HalberdC=28 }

  public readonly struct WeaponDefinition {
   public readonly WeaponId Id;public readonly string Name,Resource,Summary;public readonly float Damage,Reach,Tempo,ModelScale;public readonly Vector3 EquipEuler;
  public WeaponDefinition(WeaponId id,string name,string resource,float damage,float reach,float tempo,float modelScale,Vector3 equipEuler,string summary){Id=id;Name=name;Resource=resource;Damage=damage;Reach=reach;Tempo=tempo;ModelScale=modelScale;EquipEuler=equipEuler;Summary=summary;}
 }

public static class WeaponCatalog {
    static readonly WeaponDefinition Fists=new WeaponDefinition(WeaponId.AstersBlade,"FISTS","",.85f,.92f,1.12f,1f,Vector3.zero,"fists and kicks — find a weapon to draw arms");
    static readonly WeaponDefinition[] Definitions={
    new WeaponDefinition(WeaponId.AstersBlade,"ASTER'S BLADE","Weapons/Aster_LongSword",1f,1f,1f,1f,Vector3.zero,"Balanced starter blade"),
    new WeaponDefinition(WeaponId.LongSword,"LONGSWORD","Weapons/Aster_LongSword",1.25f,1.28f,.9f,1.15f,new Vector3(10,90,-90),"+25% damage  •  +28% reach  •  measured tempo"),
    new WeaponDefinition(WeaponId.Axe,"WAR AXE","Weapons/Aster_Axe",1.6f,.88f,.76f,.95f,new Vector3(8,0,0),"+60% damage  •  heavy recovery  •  close range"),
    new WeaponDefinition(WeaponId.CurvedSword,"CURVED SWORD","Weapons/Aster_CurvedSword",.98f,.94f,1.22f,1.05f,new Vector3(12,90,-90),"rapid strikes  •  swift recovery  •  agile reach"),
    new WeaponDefinition(WeaponId.BlinkAxe,"GREATAXE","Weapons/BlinkAxe",1.45f,.92f,.82f,1.2f,new Vector3(8,0,0),"heavy strikes  •  wide cleave  •  slow recovery"),
    new WeaponDefinition(WeaponId.BlinkMace,"WARHAMMER","Weapons/BlinkMace",1.72f,.86f,.72f,1.15f,new Vector3(8,0,0),"crushing blows  •  high damage  •  slow recovery"),
    new WeaponDefinition(WeaponId.BlinkSpear,"PIKE","Weapons/BlinkSpear",1.15f,1.4f,.96f,1.55f,new Vector3(10,0,0),"long reach  •  measured damage  •  piercing"),
    new WeaponDefinition(WeaponId.AxeIron,"IRON AXE","Weapons/AxeIron",1.35f,.86f,.8f,1.15f,new Vector3(8,0,0),"reliable chopper  •  balanced recovery"),
    new WeaponDefinition(WeaponId.AxeBattle,"BATTLE AXE","Weapons/AxeBattle",1.55f,.9f,.74f,1.2f,new Vector3(8,0,0),"heavy swings  •  slower recovery"),
    new WeaponDefinition(WeaponId.AxeBearded,"BEARDED AXE","Weapons/AxeBearded",1.5f,.95f,.78f,1.2f,new Vector3(8,0,0),"hooked head  •  wide cleave"),
    new WeaponDefinition(WeaponId.AxeCleaver,"CLEAVER","Weapons/AxeCleaver",1.65f,.88f,.7f,1.25f,new Vector3(8,0,0),"brutal damage  •  slow recovery"),
    new WeaponDefinition(WeaponId.SwordShort,"SHORT SWORD","Weapons/SwordShort",1.05f,.95f,1.12f,.95f,new Vector3(10,90,-90),"fast strikes  •  short reach"),
    new WeaponDefinition(WeaponId.SwordFalchion,"FALCHION","Weapons/SwordFalchion",1.2f,1.05f,1f,1f,new Vector3(10,90,-90),"balanced blade  •  steady tempo"),
    new WeaponDefinition(WeaponId.SwordSabre,"SABRE","Weapons/SwordSabre",1.1f,1.1f,1.15f,1.05f,new Vector3(12,90,-90),"swift arcs  •  agile reach"),
    new WeaponDefinition(WeaponId.SwordClaymore,"CLAYMORE","Weapons/SwordClaymore",1.5f,1.3f,.78f,1.35f,new Vector3(10,90,-90),"great blade  •  long reach  •  heavy"),
    new WeaponDefinition(WeaponId.SwordZweihander,"ZWELHANDER","Weapons/SwordZweihander",1.6f,1.35f,.72f,1.4f,new Vector3(10,90,-90),"massive damage  •  very slow"),
    new WeaponDefinition(WeaponId.SwordGreat,"GREATSWORD","Weapons/SwordGreat",1.45f,1.28f,.8f,1.32f,new Vector3(10,90,-90),"wide sweeps  •  strong reach"),
    new WeaponDefinition(WeaponId.HovlMagic,"ARCANE BLADE","Weapons/HovlMagic",1.4f,1.2f,.9f,1.3f,new Vector3(10,90,-90),"glowing edge  •  strong reach"),
    new WeaponDefinition(WeaponId.HovlMoon,"MOON SWORD","Weapons/HovlMoon",1.25f,1.1f,1.05f,1.2f,new Vector3(12,90,-90),"crescent strikes  •  quick tempo"),
    new WeaponDefinition(WeaponId.HovlMoon2,"LUNAR TALON","Weapons/HovlMoon2",1.3f,1.15f,1f,1.25f,new Vector3(12,90,-90),"curved blade  •  steady arcs"),
    new WeaponDefinition(WeaponId.PureAxe2H,"TITAN AXE","Weapons/PureAxe2H",1.5f,.92f,.78f,1.32f,new Vector3(8,0,0),"two-handed cleave  •  heavy"),
    new WeaponDefinition(WeaponId.PureHammer,"CRUSHER","Weapons/PureHammer",1.62f,.86f,.74f,1.22f,new Vector3(8,0,0),"blunt force  •  slow recovery"),
    new WeaponDefinition(WeaponId.PureScythe,"REAPER'S SCYTHE","Weapons/PureScythe",1.1f,1.25f,.88f,1.42f,new Vector3(12,90,-90),"sweeping arc  •  deceptive reach"),
    new WeaponDefinition(WeaponId.PureSword2H,"TEMPEST BLADE","Weapons/PureSword2H",1.35f,1.2f,.82f,1.36f,new Vector3(10,90,-90),"two-handed tempo  •  wide swing"),
    new WeaponDefinition(WeaponId.PureSpear,"REAPER'S SPEAR","Weapons/PureSpear",1.05f,1.35f,.95f,1.5f,new Vector3(10,0,0),"long thrust  •  probing reach"),
    new WeaponDefinition(WeaponId.PureSword1H,"DUELING BLADE","Weapons/PureSword1H",1.1f,1f,1.08f,1.05f,new Vector3(10,90,-90),"balanced one-hand  •  quick"),
    new WeaponDefinition(WeaponId.HalberdA,"HALBERD","Weapons/HalberdA",1.2f,1.3f,.88f,1.42f,new Vector3(10,0,0),"pole cleave  •  long reach"),
    new WeaponDefinition(WeaponId.HalberdB,"POLE AXE","Weapons/HalberdB",1.3f,1.32f,.84f,1.45f,new Vector3(10,0,0),"heavier head  •  long reach"),
    new WeaponDefinition(WeaponId.HalberdC,"GLAIVE","Weapons/HalberdC",1.18f,1.35f,.92f,1.48f,new Vector3(10,0,0),"sweeping blade  •  extended reach")
   };
  public static WeaponDefinition Get(int rawId){return rawId<0?Fists:Definitions[Mathf.Clamp(rawId,0,Definitions.Length-1)];}
  public static WeaponDefinition Get(WeaponId id)=>Get((int)id);
  public static GameObject CreateModel(WeaponId id,Transform parent){
   var definition=Get(id);if(string.IsNullOrEmpty(definition.Resource))return null;
   var prefab=Resources.Load<GameObject>(definition.Resource);if(!prefab){Debug.LogError("WEAPON_MODEL_MISSING: "+definition.Resource);return null;}
   var model=UnityEngine.Object.Instantiate(prefab,parent);model.name=definition.Name+" model";model.transform.localPosition=Vector3.zero;model.transform.localRotation=Quaternion.identity;model.transform.localScale=Vector3.one;
   foreach(var collider in model.GetComponentsInChildren<Collider>(true))UnityEngine.Object.Destroy(collider);
   var renderers=model.GetComponentsInChildren<Renderer>(true);
   float longest=0f;Renderer blade=null;
   foreach(var renderer in renderers){renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;var size=renderer.bounds.size;float m=Mathf.Max(size.x,Mathf.Max(size.y,size.z));if(m>longest){longest=m;blade=renderer;}}
   // ModelScale is the real-world target length in meters: normalize the
   // imported mesh so the blade reads at any FBX unit scale.
   if(longest>1e-4f)model.transform.localScale=Vector3.one*(definition.ModelScale/longest);
   // Prefer the decimated model's real textures from Resources/Weapons/Textures.
   // Keep any material the FBX importer already textured; otherwise bind the
   // albedo/normal maps, falling back to steel+grip only if none are present.
   var skin=WeaponSkin(definition.Resource);
    foreach(var renderer in renderers){var m=renderer.sharedMaterial;renderer.sharedMaterial=skin!=null?skin:(m!=null&&m.mainTexture!=null?m:(renderer==blade?BladeMaterial():GripMaterial()));}
   return model;
  }
  static readonly System.Collections.Generic.Dictionary<string,Material> skins=new System.Collections.Generic.Dictionary<string,Material>();
  static Material WeaponSkin(string resource){
   string name=resource.Substring(resource.LastIndexOf('/')+1);
   if(skins.TryGetValue(name,out var cached))return cached;
   var albedo=Resources.Load<Texture2D>("Weapons/Textures/"+name+"_basecolor");
   if(!albedo){skins[name]=null;return null;}
   var mat=new Material(Shader.Find("Standard")){name=name,color=Color.white};
   mat.mainTexture=albedo;mat.SetFloat("_Metallic",.35f);mat.SetFloat("_Glossiness",.42f);
   var normal=Resources.Load<Texture2D>("Weapons/Textures/"+name+"_normal");
   if(normal){mat.SetTexture("_BumpMap",normal);mat.SetFloat("_BumpScale",1f);mat.EnableKeyword("_NORMALMAP");}
   skins[name]=mat;return mat;
  }
  static Material bladeMat,gripMat;
  static Material BladeMaterial(){if(!bladeMat){bladeMat=new Material(Shader.Find("Standard"));bladeMat.color=new Color(.78f,.81f,.86f);bladeMat.SetFloat("_Metallic",.35f);bladeMat.SetFloat("_Glossiness",.5f);}return bladeMat;}
  static Material GripMaterial(){if(!gripMat){gripMat=new Material(Shader.Find("Standard"));gripMat.color=new Color(.16f,.12f,.09f);gripMat.SetFloat("_Metallic",.15f);gripMat.SetFloat("_Glossiness",.3f);}return gripMat;}
 }

 public sealed class WeaponDrop:MonoBehaviour {
  public WeaponId Id;public Vector3 Origin;Transform model;float phase;
  public void Configure(WeaponId id,Vector3 origin){Id=id;Origin=origin;phase=((int)id+1)*1.37f;var definition=WeaponCatalog.Get(id);var created=WeaponCatalog.CreateModel(id,transform);model=created?created.transform:null;if(!model)Debug.LogError("WEAPON_DROP_SETUP_FAILED: "+definition.Name);else Art.Ring(new Vector3(0,-.55f,0),.7f,new Color(1f,.85f,.3f),transform);}
  void Update(){
   var game=RealmGame.I;if(!game||game.Screen!=GameScreen.Playing||!game.Player)return;
   float time=game.Elapsed+phase;transform.position=Origin+Vector3.up*(.75f+Mathf.Sin(time*2.4f)*.12f);transform.Rotate(0,Time.deltaTime*42f,0,Space.World);
   if(model)model.localRotation=Quaternion.Euler(0,0,Mathf.Sin(time*1.8f)*7f);
   if(Vector3.Distance(game.Player.transform.position+Vector3.up*.8f,transform.position)<1.3f){
    game.EquipWeapon(Id);HitSpark.Burst(transform.position,Vector3.up,game.Accent,22);Destroy(gameObject);
   }
  }
 }

 public sealed class EquippedWeapon:MonoBehaviour {
  public WeaponId Id;GameObject model;
  Hero hero;Transform hand;float drawnUntil,draw;
  Vector3 bladeDir=Vector3.up,flatDir=Vector3.forward,center;bool measured;
  static readonly float BackGap=.15f;
  public static void Equip(Hero hero,WeaponId id){
   var old=hero.GetComponentInChildren<EquippedWeapon>();if(old)UnityEngine.Object.Destroy(old.gameObject);
   var definition=WeaponCatalog.Get(id);
   var hand=FindHand(hero.Visual);if(!hand){Debug.LogError("WEAPON_EQUIP_FAILED: no Aster right hand");return;}
   var root=new GameObject("Equipped "+definition.Name);root.transform.SetParent(hero.Visual.transform,false);var equipped=root.AddComponent<EquippedWeapon>();equipped.Id=id;equipped.hero=hero;equipped.hand=hand;
   equipped.model=WeaponCatalog.CreateModel(id,root.transform);
   if(!equipped.model){UnityEngine.Object.Destroy(root);return;}
   equipped.model.transform.localPosition=Vector3.zero;equipped.model.transform.localRotation=Quaternion.Euler(definition.EquipEuler);
   equipped.Measure();
   equipped.Sheath(out var pos,out var rot);equipped.transform.SetPositionAndRotation(pos,rot);
  }
  // The FBX pivots are arbitrary, so measure where the blade and the visual
  // center really are: the sheathed pose then puts the actual mesh against
  // Aster's back instead of guessing from the model origin.
  void Measure(){
   var renderer=model?model.GetComponentInChildren<Renderer>():null;
   if(!renderer)return;
   // localBounds is rotation-independent, so the longest axis is the blade
   // even when the weapon is held diagonally at equip time.
   var size=renderer.localBounds.size;
   Vector3 longAxis=size.x>=size.y&&size.x>=size.z?Vector3.right:size.y>=size.x&&size.y>=size.z?Vector3.up:Vector3.forward;
   bladeDir=transform.InverseTransformDirection(renderer.transform.TransformDirection(longAxis));
   flatDir=transform.InverseTransformDirection(renderer.transform.TransformDirection(Vector3.forward));
   center=transform.InverseTransformPoint(renderer.bounds.center);
   measured=true;
  }
  void Sheath(out Vector3 pos,out Quaternion rot){
   var anim=hero&&hero.Visual?hero.Visual.animator:null;
   Vector3 socket=hero?hero.transform.position+Vector3.up*1.3f:Vector3.zero;
   var chest=anim?anim.GetBoneTransform(HumanBodyBones.Chest):null;
   if(chest)socket=chest.position;
   socket+=hero.transform.forward*(-BackGap)+hero.transform.up*.04f;
   // Longest local axis (the blade) stands up the spine; twist so the flat of
   // the weapon lies against the back rather than its edge.
   rot=Quaternion.FromToRotation(measured?bladeDir:Vector3.up,hero.transform.up);
   Vector3 flat=rot*flatDir;
   if(Vector3.Dot(flat,hero.transform.forward)>=0)rot=Quaternion.AngleAxis(180f,hero.transform.up)*rot;
   pos=socket-rot*center;
  }
  void LateUpdate(){
   var game=RealmGame.I;if(!game||game.Screen!=GameScreen.Playing)return;
   if(!hero||!hero.Visual||!hand||!model)return;
   string state=hero.Visual.CurrentState;
   if(state.StartsWith("attack_")||state=="charged")drawnUntil=Time.time+1.1f;
   bool shouldDraw=Time.time<drawnUntil;
   draw=Mathf.MoveTowards(draw,shouldDraw?1f:0f,Time.deltaTime*(shouldDraw?9f:3.5f));
   if(draw>=1f){transform.SetPositionAndRotation(hand.position,hand.rotation);return;}
   Sheath(out var pos,out var rot);
   transform.position=Vector3.Lerp(pos,hand.position,draw);
   transform.rotation=Quaternion.Slerp(rot,hand.rotation,draw);
  }
  static Transform FindHand(CharacterVisual visual){
   if(visual&&visual.animator){var hand=visual.animator.GetBoneTransform(HumanBodyBones.RightHand);if(hand)return hand;}
   if(visual)foreach(var transform in visual.GetComponentsInChildren<Transform>(true))if(transform.name.Equals("hand_R",StringComparison.OrdinalIgnoreCase)||transform.name.EndsWith("hand_r",StringComparison.OrdinalIgnoreCase)||transform.name.EndsWith("RightHand",StringComparison.OrdinalIgnoreCase))return transform;
   return null;
  }
 }
}
