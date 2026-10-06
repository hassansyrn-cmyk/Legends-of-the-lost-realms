using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace LostRealms {
 public enum WeaponId { AstersBlade=0, LongSword=1, Axe=2, CurvedSword=3, BlinkAxe=4, BlinkMace=5, BlinkSpear=6, AxeIron=7, AxeBattle=8, AxeBearded=9, AxeCleaver=10, SwordShort=11, SwordFalchion=12, SwordSabre=13, SwordClaymore=14, SwordZweihander=15, SwordGreat=16, HovlMagic=17, HovlMoon=18, HovlMoon2=19, PureAxe2H=20, PureHammer=21, PureScythe=22, PureSword2H=23, PureSpear=24, PureSword1H=25, HalberdA=26, HalberdB=27, HalberdC=28, RangerAxe=29, Executioner=30, SquireBlade=31, Juggernaut=32, HuntsmanSpear=33, MoonChakram=34, RiftDagger=35, WardenPike=36, Maul=37, BrassFangs=38, EmberTorch=39, SageStaff=40, FantasyGreatsword=41, FierySword=42, OrnateCurvedBlade=43, AstralStaff=44, GlacierMaul=45, VoidReaper=46, FrostHalberd=47, VerdantFang=48 }

  public readonly struct WeaponDefinition {
   public readonly WeaponId Id;public readonly string Name,Resource,Summary;public readonly float Damage,Reach,Tempo,ModelScale;public readonly Vector3 EquipEuler;
  public WeaponDefinition(WeaponId id,string name,string resource,float damage,float reach,float tempo,float modelScale,Vector3 equipEuler,string summary){Id=id;Name=name;Resource=resource;Damage=damage;Reach=reach;Tempo=tempo;ModelScale=modelScale;EquipEuler=equipEuler;Summary=summary;}
 }

public static class WeaponCatalog {
    public const int MaxId=(int)WeaponId.VerdantFang;
    // Explicit identities: spear-shaped weapons remain physical polearms.
    public static int ChargedShot(WeaponId id)=>id==WeaponId.EmberTorch?0:id==WeaponId.AstralStaff||id==WeaponId.FrostHalberd?1:id==WeaponId.SageStaff||id==WeaponId.WardenPike?2:-1;
    public static bool ChargedSlash(WeaponId id)=>id==WeaponId.PureScythe||id==WeaponId.BrassFangs||id==WeaponId.OrnateCurvedBlade||id==WeaponId.VoidReaper;
    public static bool HasSpecialHold(WeaponId id)=>id==WeaponId.FantasyGreatsword||id==WeaponId.FierySword||id==WeaponId.OrnateCurvedBlade||id==WeaponId.GlacierMaul||ChargedShot(id)>=0||ChargedSlash(id);
    // Keep one normal chapter drop, but discover the collection before repeating it.
    // Reuse the existing roll so selecting loot does not reshuffle the level layout.
    public static WeaponId DiscoveryDrop(int roll,Progress progress){
     var missing=new System.Collections.Generic.List<int>();
     for(int id=1;id<=MaxId;id++)if(progress==null||((progress.weapons==null||!progress.weapons.Contains(id))&&progress.equippedWeapon!=id))missing.Add(id);
     int index=Mathf.Clamp(roll,1,MaxId)-1;
     return (WeaponId)(missing.Count>0?missing[index%missing.Count]:index+1);
    }
    static readonly WeaponDefinition Fists=new WeaponDefinition(WeaponId.AstersBlade,"FISTS","",.85f,.92f,1.12f,1f,Vector3.zero,"fists and kicks — find a weapon to draw arms");
    static readonly WeaponDefinition[] Definitions={
    new WeaponDefinition(WeaponId.AstersBlade,"ASTER'S BLADE","Weapons/Aster_LongSword",1f,1f,1f,1f,new Vector3(10,0,0),"Balanced starter blade"),
    new WeaponDefinition(WeaponId.LongSword,"LONGSWORD","Weapons/Aster_LongSword",1.25f,1.28f,.9f,1.15f,new Vector3(10,0,0),"+25% damage  •  +28% reach  •  measured tempo"),
    new WeaponDefinition(WeaponId.Axe,"WAR AXE","Weapons/Aster_Axe",1.6f,.88f,.76f,.95f,new Vector3(-82,0,0),"+60% damage  •  heavy recovery  •  close range"),
    new WeaponDefinition(WeaponId.CurvedSword,"CURVED SWORD","Weapons/Aster_CurvedSword",.98f,.94f,1.22f,1.05f,new Vector3(12,0,0),"rapid strikes  •  swift recovery  •  agile reach"),
    new WeaponDefinition(WeaponId.BlinkAxe,"GREATAXE","Weapons/BlinkAxe",1.45f,.92f,.82f,1.2f,new Vector3(8,0,0),"heavy strikes  •  wide cleave  •  slow recovery"),
    new WeaponDefinition(WeaponId.BlinkMace,"WARHAMMER","Weapons/BlinkMace",1.72f,.86f,.72f,1.15f,new Vector3(8,0,0),"crushing blows  •  high damage  •  slow recovery"),
    new WeaponDefinition(WeaponId.BlinkSpear,"PIKE","Weapons/BlinkSpear",1.15f,1.4f,.96f,1.55f,new Vector3(10,0,0),"long reach  •  measured damage  •  piercing"),
    new WeaponDefinition(WeaponId.AxeIron,"IRON AXE","Weapons/AxeIron",1.35f,.86f,.8f,1.15f,new Vector3(8,0,0),"reliable chopper  •  balanced recovery"),
    new WeaponDefinition(WeaponId.AxeBattle,"BATTLE AXE","Weapons/AxeBattle",1.55f,.9f,.74f,1.2f,new Vector3(8,0,0),"heavy swings  •  slower recovery"),
    new WeaponDefinition(WeaponId.AxeBearded,"BEARDED AXE","Weapons/AxeBearded",1.5f,.95f,.78f,1.2f,new Vector3(8,0,0),"hooked head  •  wide cleave"),
    new WeaponDefinition(WeaponId.AxeCleaver,"CLEAVER","Weapons/AxeCleaver",1.65f,.88f,.7f,1.25f,new Vector3(8,0,0),"brutal damage  •  slow recovery"),
    new WeaponDefinition(WeaponId.SwordShort,"SHORT SWORD","Weapons/SwordShort",1.05f,.95f,1.12f,.95f,new Vector3(10,90,90),"fast strikes  •  short reach"),
    new WeaponDefinition(WeaponId.SwordFalchion,"FALCHION","Weapons/SwordFalchion",1.2f,1.05f,1f,1f,new Vector3(10,90,90),"balanced blade  •  steady tempo"),
    new WeaponDefinition(WeaponId.SwordSabre,"SABRE","Weapons/SwordSabre",1.1f,1.1f,1.15f,1.05f,new Vector3(12,90,90),"swift arcs  •  agile reach"),
    new WeaponDefinition(WeaponId.SwordClaymore,"CLAYMORE","Weapons/SwordClaymore",1.5f,1.3f,.78f,1.35f,new Vector3(10,90,90),"great blade  •  long reach  •  heavy"),
    new WeaponDefinition(WeaponId.SwordZweihander,"ZWELHANDER","Weapons/SwordZweihander",1.6f,1.35f,.72f,1.4f,new Vector3(10,90,90),"massive damage  •  very slow"),
    new WeaponDefinition(WeaponId.SwordGreat,"GREATSWORD","Weapons/SwordGreat",1.45f,1.28f,.8f,1.32f,new Vector3(10,90,90),"wide sweeps  •  strong reach"),
    new WeaponDefinition(WeaponId.HovlMagic,"ARCANE BLADE","Weapons/HovlMagic",1.4f,1.2f,.9f,1.3f,new Vector3(10,0,0),"glowing edge  •  strong reach"),
    new WeaponDefinition(WeaponId.HovlMoon,"MOON SWORD","Weapons/HovlMoon",1.25f,1.1f,1.05f,1.2f,new Vector3(12,90,90),"crescent strikes  •  quick tempo"),
    new WeaponDefinition(WeaponId.HovlMoon2,"LUNAR TALON","Weapons/HovlMoon2",1.3f,1.15f,1f,1.25f,new Vector3(12,90,90),"curved blade  •  steady arcs"),
    new WeaponDefinition(WeaponId.PureAxe2H,"TITAN AXE","Weapons/PureAxe2H",1.5f,.92f,.78f,1.32f,new Vector3(8,0,0),"two-handed cleave  •  heavy"),
    new WeaponDefinition(WeaponId.PureHammer,"CRUSHER","Weapons/PureHammer",1.62f,.86f,.74f,1.22f,new Vector3(8,0,0),"blunt force  •  slow recovery"),
    new WeaponDefinition(WeaponId.PureScythe,"REAPER'S SCYTHE","Weapons/PureScythe",1.1f,1.25f,.88f,1.42f,new Vector3(12,90,90),"hold attack: spectral crescent slash"),
    new WeaponDefinition(WeaponId.PureSword2H,"TEMPEST BLADE","Weapons/PureSword2H",1.35f,1.2f,.82f,1.36f,new Vector3(10,90,90),"two-handed tempo  •  wide swing"),
    new WeaponDefinition(WeaponId.PureSpear,"REAPER'S SPEAR","Weapons/PureSpear",1.05f,1.35f,.95f,1.5f,new Vector3(10,0,0),"long thrust  •  probing reach"),
    new WeaponDefinition(WeaponId.PureSword1H,"DUELING BLADE","Weapons/PureSword1H",1.1f,1f,1.08f,1.05f,new Vector3(10,90,90),"balanced one-hand  •  quick"),
    new WeaponDefinition(WeaponId.HalberdA,"HALBERD","Weapons/HalberdA",1.2f,1.3f,.88f,1.42f,new Vector3(10,0,0),"pole cleave  •  long reach"),
    new WeaponDefinition(WeaponId.HalberdB,"POLE AXE","Weapons/HalberdB",1.3f,1.32f,.84f,1.45f,new Vector3(10,0,0),"heavier head  •  long reach"),
    new WeaponDefinition(WeaponId.HalberdC,"GLAIVE","Weapons/HalberdC",1.18f,1.35f,.92f,1.48f,new Vector3(10,0,0),"sweeping blade  •  extended reach"),
    new WeaponDefinition(WeaponId.RangerAxe,"RANGER AXE","Weapons/RangerAxe",1.3f,.95f,1.05f,1.15f,new Vector3(8,0,0),"swift chops  •  balanced recovery"),
    new WeaponDefinition(WeaponId.Executioner,"EXECUTIONER","Weapons/Executioner",1.7f,1f,.68f,1.3f,new Vector3(8,0,0),"massive cleave  •  very slow"),
    new WeaponDefinition(WeaponId.SquireBlade,"SQUIRE BLADE","Weapons/SquireBlade",1.05f,1f,1.15f,1.05f,new Vector3(10,90,90),"quick recruit blade  •  agile"),
    new WeaponDefinition(WeaponId.Juggernaut,"JUGGERNAUT","Weapons/Juggernaut",1.55f,1.3f,.75f,1.35f,new Vector3(10,90,90),"colossal sweeps  •  heavy"),
    new WeaponDefinition(WeaponId.HuntsmanSpear,"HUNTSMAN SPEAR","Weapons/HuntsmanSpear",1.1f,1.35f,1f,1.5f,new Vector3(10,0,0),"long thrust  •  steady tempo"),
    new WeaponDefinition(WeaponId.MoonChakram,"MOON CHAKRAM","Weapons/MoonChakram",1.15f,1.05f,1.2f,1.1f,new Vector3(12,90,90),"whirling disc  •  rapid arcs"),
    new WeaponDefinition(WeaponId.RiftDagger,"RIFT DAGGER","Weapons/RiftDagger",1f,.85f,1.25f,.9f,new Vector3(10,90,90),"piercing fang  •  very fast"),
    new WeaponDefinition(WeaponId.WardenPike,"WARDEN PIKE","Weapons/WardenPike",1.15f,1.4f,.92f,1.5f,new Vector3(10,0,0),"hold attack: storm lance  •  chaining thunder"),
    new WeaponDefinition(WeaponId.Maul,"MAUL","Weapons/Maul",1.75f,.9f,.66f,1.25f,new Vector3(8,0,0),"siege hammer  •  slowest swing"),
    new WeaponDefinition(WeaponId.BrassFangs,"BRASS FANGS","Weapons/BrassFangs",1.2f,.8f,1.3f,.9f,new Vector3(8,0,0),"hold attack: three-claw slash volley"),
    new WeaponDefinition(WeaponId.EmberTorch,"EMBER TORCH","Weapons/EmberTorch",1.2f,.9f,1f,1.1f,new Vector3(8,0,0),"hold attack: fireball  •  small flame splash"),
    new WeaponDefinition(WeaponId.SageStaff,"SAGE STAFF","Weapons/SageStaff",1.05f,1.35f,.95f,1.5f,new Vector3(10,0,0),"hold attack: thunder bolt  •  arcs to a nearby foe"),
    new WeaponDefinition(WeaponId.FantasyGreatsword,"FANTASY GREATSWORD","Weapons/FantasyGreatsword",1.65f,1.38f,.74f,1.45f,new Vector3(10,90,90),"hold attack: aether quake  •  cascading seismic rift"),
    new WeaponDefinition(WeaponId.FierySword,"INFERNO BLADE","Weapons/FierySword",1.45f,1.22f,1.08f,1.28f,new Vector3(10,90,90),"hold attack: inferno crescent  •  exploding flame wave"),
    new WeaponDefinition(WeaponId.OrnateCurvedBlade,"ORNATE CURVED BLADE","Weapons/OrnateCurvedBlade",1.24f,1.16f,1.32f,1.18f,new Vector3(12,90,90),"hold attack: gale crosscut  •  twin wind crescents"),
    new WeaponDefinition(WeaponId.AstralStaff,"ASTRAL STAFF","Weapons/AstralStaff",1.08f,1.34f,.98f,1.48f,new Vector3(10,0,0),"hold attack: frost bolt  •  freezes on impact"),
    new WeaponDefinition(WeaponId.GlacierMaul,"GLACIER MAUL","Weapons/GlacierMaul",1.82f,.94f,.62f,1.35f,new Vector3(8,0,0),"hold attack: glacier break  •  freezing ground rift"),
    new WeaponDefinition(WeaponId.VoidReaper,"VOID REAPER","Weapons/VoidReaper",1.28f,1.27f,.86f,1.48f,new Vector3(10,90,90),"hold attack: shadow crescent  •  long-range cut"),
    new WeaponDefinition(WeaponId.FrostHalberd,"FROST HALBERD","Weapons/FrostHalberd",1.18f,1.42f,.9f,1.52f,new Vector3(10,0,0),"hold attack: ice lance  •  freezes on impact"),
    new WeaponDefinition(WeaponId.VerdantFang,"VERDANT FANG","Weapons/VerdantFang",1.28f,1.12f,.96f,1.32f,new Vector3(10,90,90),"leaf-forged blade  •  gale affinity")
    };
  public static WeaponDefinition Get(int rawId){return rawId<0?Fists:Definitions[Mathf.Clamp(rawId,0,Definitions.Length-1)];}
  public static WeaponDefinition Get(WeaponId id)=>Get((int)id);
  // Attack animation style per weapon family, driven by grip orientation so
  // every weapon swings like its shape: overhead grips chop, pole grips poke
  // and slam, blades slash, fists brawl. Unknown grips fall back to slash.
  public static string AttackStyle(WeaponDefinition def){
   if(string.IsNullOrEmpty(def.Resource)||def.Id==WeaponId.BrassFangs)return "unarmed";
   switch(def.Id){
    case WeaponId.Axe:case WeaponId.BlinkAxe:case WeaponId.BlinkMace:case WeaponId.AxeIron:case WeaponId.AxeBattle:
    case WeaponId.AxeBearded:case WeaponId.AxeCleaver:case WeaponId.PureAxe2H:case WeaponId.PureHammer:
    case WeaponId.RangerAxe:case WeaponId.Executioner:case WeaponId.Maul:case WeaponId.EmberTorch:case WeaponId.GlacierMaul:
     return "chop";
    case WeaponId.BlinkSpear:case WeaponId.PureSpear:case WeaponId.HalberdA:case WeaponId.HalberdB:case WeaponId.HalberdC:
    case WeaponId.HuntsmanSpear:case WeaponId.WardenPike:case WeaponId.SageStaff:case WeaponId.AstralStaff:case WeaponId.FrostHalberd:
     return "spear";
    default:
     return "slash";
   }
  }
  // Elemental infusion: weapons hit harder against foes weak to their element
  // (Hero.Attack applies the bonus). -1 = mundane.
  public static int Affinity(WeaponDefinition def){
   switch(def.Id){
    case WeaponId.EmberTorch:case WeaponId.FierySword:return 0;
    case WeaponId.AstralStaff:case WeaponId.GlacierMaul:case WeaponId.FrostHalberd:case WeaponId.HovlMagic:case WeaponId.HovlMoon:case WeaponId.HovlMoon2:case WeaponId.SageStaff:return 1;
    case WeaponId.MoonChakram:case WeaponId.PureScythe:case WeaponId.HalberdC:case WeaponId.OrnateCurvedBlade:case WeaponId.VerdantFang:return 2;
    default:return -1;
   }
  }
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
  readonly RewardAnchor anchor=new RewardAnchor();
  public void Configure(WeaponId id,Vector3 origin){
   Id=id;Origin=origin;phase=((int)id+1)*1.37f;
   Physics.SyncTransforms();
   var hits=Physics.RaycastAll(origin+Vector3.up*6f,Vector3.down,12f,~0,QueryTriggerInteraction.Ignore);
   float bestY=float.NegativeInfinity;
   for(int i=0;i<hits.Length;i++){if(hits[i].normal.y>=.5f&&!hits[i].transform.name.StartsWith("Prop ")&&hits[i].point.y>bestY)bestY=hits[i].point.y;}
   if(bestY>float.NegativeInfinity)Origin=new Vector3(origin.x,bestY+.15f,origin.z);
   transform.position=Origin+Vector3.up*.75f;
   anchor.Bind(transform,Origin);
   var definition=WeaponCatalog.Get(id);var created=WeaponCatalog.CreateModel(id,transform);model=created?created.transform:null;if(!model)Debug.LogError("WEAPON_DROP_SETUP_FAILED: "+definition.Name);else Art.Ring(new Vector3(0,-.55f,0),.7f,new Color(1f,.85f,.3f),transform);Vfx.Play("ga_vfx_LootDrop_01",Origin+Vector3.up*.6f,Quaternion.identity,.9f);
  }
  void Start(){
   Origin=anchor.Position;
   Physics.SyncTransforms();
   var hits=Physics.RaycastAll(Origin+Vector3.up*6f,Vector3.down,12f,~0,QueryTriggerInteraction.Ignore);
   float bestY=float.NegativeInfinity;
   for(int i=0;i<hits.Length;i++){if(hits[i].normal.y>=.5f&&!hits[i].transform.name.StartsWith("Prop ")&&hits[i].point.y>bestY)bestY=hits[i].point.y;}
   if(bestY>float.NegativeInfinity){float minSafe=bestY+.15f;if(Origin.y<minSafe)Origin.y=minSafe;}
   transform.position=Origin+Vector3.up*.75f;
   anchor.Bind(transform,Origin);
  }
  void Update(){
   var game=RealmGame.I;if(!game||game.Screen!=GameScreen.Playing||!game.Player)return;
   Origin=anchor.Position;
   float time=game.Elapsed+phase;transform.position=Origin+Vector3.up*(.75f+Mathf.Sin(time*1.4f)*.09f);transform.Rotate(0,Time.deltaTime*24f,0,Space.World);
   if(model)model.localRotation=Quaternion.Euler(0,0,Mathf.Sin(time*1.8f)*7f);
   if(Vector3.Distance(game.Player.transform.position+Vector3.up*.8f,transform.position)<1.3f){
    game.EquipWeapon(Id);Destroy(gameObject);
   }
  }
 }

 public sealed class EquippedWeapon:MonoBehaviour {
  public static bool HasAuthoredGrip(WeaponId id)=>id==WeaponId.FantasyGreatsword||id==WeaponId.FierySword||id==WeaponId.OrnateCurvedBlade||id==WeaponId.VerdantFang;
  public static bool HasGripAnchor(WeaponId id)=>true;
  // Mesh-local handle centers measured from the textured front views in WeaponPoseProbe.
  public static Vector3 GripPoint(WeaponId id)=>id==WeaponId.FantasyGreatsword?new Vector3(.247f,.799f,0):id==WeaponId.FierySword?new Vector3(-.334f,.819f,0):id==WeaponId.OrnateCurvedBlade?new Vector3(.020f,.728f,0):new Vector3(0.2370f,0.0961f,0.2156f);
  public static Vector3 GripAnchorPoint(WeaponId id,Bounds bounds){
   if(HasAuthoredGrip(id))return GripPoint(id);
   if(id==WeaponId.AstersBlade||id==WeaponId.LongSword)return new Vector3(0f,0f,.12f);
   if(id==WeaponId.Axe)return new Vector3(0f,0f,.18f);
   if(id==WeaponId.CurvedSword)return new Vector3(0f,0f,.10f);
   if(id==WeaponId.HovlMagic)return new Vector3(0f,0f,-.15f);
   if(id==WeaponId.MoonChakram||id==WeaponId.BrassFangs)return bounds.center;
   float fraction;
   switch(id){
    case WeaponId.BlinkSpear:case WeaponId.PureSpear:case WeaponId.HuntsmanSpear:fraction=.25f;break;
    case WeaponId.HalberdA:fraction=.26f;break;
    case WeaponId.HalberdB:case WeaponId.FrostHalberd:fraction=.28f;break;
    case WeaponId.HalberdC:case WeaponId.WardenPike:fraction=.27f;break;
    case WeaponId.SageStaff:fraction=.31f;break;
    case WeaponId.AstralStaff:fraction=.33f;break;
    case WeaponId.PureAxe2H:case WeaponId.Executioner:case WeaponId.RangerAxe:fraction=.23f;break;
    case WeaponId.PureHammer:case WeaponId.Maul:fraction=.26f;break;
    case WeaponId.GlacierMaul:case WeaponId.PureScythe:fraction=.30f;break;
    case WeaponId.BlinkAxe:case WeaponId.AxeCleaver:fraction=.18f;break;
    case WeaponId.BlinkMace:fraction=.16f;break;
    case WeaponId.AxeIron:case WeaponId.EmberTorch:fraction=.22f;break;
    case WeaponId.AxeBattle:fraction=.21f;break;
    case WeaponId.AxeBearded:fraction=.20f;break;
    case WeaponId.VoidReaper:fraction=.38f;break;
    case WeaponId.PureSword2H:case WeaponId.Juggernaut:fraction=.11f;break;
    case WeaponId.SwordZweihander:case WeaponId.SwordGreat:fraction=.10f;break;
    case WeaponId.SwordClaymore:fraction=.09f;break;
    case WeaponId.SwordShort:case WeaponId.SwordSabre:fraction=.08f;break;
    case WeaponId.SwordFalchion:case WeaponId.PureSword1H:case WeaponId.SquireBlade:fraction=.07f;break;
    case WeaponId.HovlMoon:case WeaponId.HovlMoon2:fraction=.06f;break;
    case WeaponId.RiftDagger:fraction=.12f;break;
    default:fraction=.25f;break;
   }
   return new Vector3(0f,bounds.min.y+bounds.size.y*fraction,0f);
  }
  static Vector3 BladeAxis(WeaponId id)=>id==WeaponId.FantasyGreatsword?new Vector3(-.64f,-.768f,0):id==WeaponId.FierySword?new Vector3(.707f,-.707f,0):id==WeaponId.OrnateCurvedBlade?Vector3.down:new Vector3(-0.48027f,0.76787f,-0.42393f).normalized;
  static Quaternion ModelCorrection(WeaponId id){
   if(id==WeaponId.FantasyGreatsword)return Quaternion.Inverse(Quaternion.LookRotation(Vector3.forward,new Vector3(-.64f,-.768f,0)));
   if(id==WeaponId.FierySword)return Quaternion.Inverse(Quaternion.LookRotation(Vector3.forward,new Vector3(.707f,-.707f,0)));
   if(id==WeaponId.OrnateCurvedBlade)return Quaternion.Inverse(Quaternion.LookRotation(Vector3.forward,Vector3.down));
   if(id==WeaponId.VerdantFang)return Quaternion.Inverse(Quaternion.LookRotation(new Vector3(-0.04481f,0.46121f,0.88616f).normalized,new Vector3(-0.48027f,0.76787f,-0.42393f).normalized));
   return Quaternion.identity;
  }
  public WeaponId Id;GameObject model;
  Hero hero;Transform hand;float drawnUntil,draw;
  Vector3 bladeDir=Vector3.up,flatDir=Vector3.forward,center;bool measured;
  TrailRenderer trail;static readonly float BackGap=.15f;
  public bool InFlight {get;private set;}
  public Vector3 CatchPosition=>hand?hand.position:transform.position;
  public bool BeginThrow(){if(InFlight||!model)return false;InFlight=true;model.SetActive(false);if(trail){trail.emitting=false;trail.Clear();}return true;}
  public void EndThrow(){InFlight=false;if(model)model.SetActive(true);draw=1f;drawnUntil=Time.time+.3f;}
  public static void Equip(Hero hero,WeaponId id){
   // Destroy is deferred: detach immediately so another pickup this frame
   // cannot find the same pending-destruction model and leave its replacement.
   foreach(var old in hero.GetComponentsInChildren<EquippedWeapon>(true)){
    old.gameObject.SetActive(false);old.transform.SetParent(null);UnityEngine.Object.Destroy(old.gameObject);
   }
   var definition=WeaponCatalog.Get(id);
   var hand=FindHand(hero.Visual);if(!hand){Debug.LogError("WEAPON_EQUIP_FAILED: no Aster right hand");return;}
   var root=new GameObject("Equipped "+definition.Name);root.transform.SetParent(hero.Visual.transform,false);var equipped=root.AddComponent<EquippedWeapon>();equipped.Id=id;equipped.hero=hero;equipped.hand=hand;
   equipped.model=WeaponCatalog.CreateModel(id,root.transform);
   if(!equipped.model){UnityEngine.Object.Destroy(root);return;}
   equipped.model.transform.localPosition=Vector3.zero;equipped.model.transform.localRotation=Quaternion.Euler(definition.EquipEuler);
   if(HasAuthoredGrip(id)){
    equipped.model.transform.localRotation=Quaternion.Euler(definition.EquipEuler)*ModelCorrection(id);
   }
   if(HasGripAnchor(id)){
    var mesh=equipped.model.GetComponentInChildren<Renderer>();
    if(mesh)equipped.model.transform.localPosition-=root.transform.InverseTransformPoint(mesh.transform.TransformPoint(GripAnchorPoint(id,mesh.localBounds)));
   }
   equipped.Measure();
   equipped.BuildTrail(definition);
   equipped.Sheath(out var pos,out var rot);equipped.transform.SetPositionAndRotation(pos,rot);
  }
  // Elemental ribbon trail anchored to the blade tip: sampled while the weapon
  // is drawn and swinging, tinted by the weapon's affinity (or Aster's current
  // element for mundane steel), so every swing reads with its element.
  void BuildTrail(WeaponDefinition definition){
   var renderer=model.GetComponentInChildren<Renderer>();if(!renderer)return;
   var size=renderer.localBounds.size;
   Vector3 axis=size.x>=size.y&&size.x>=size.z?Vector3.right:size.y>=size.x&&size.y>=size.z?Vector3.up:Vector3.forward;
   var tip=renderer.transform.Find("Blade tip");
   if(!tip){tip=new GameObject("Blade tip").transform;tip.SetParent(renderer.transform,false);}
   tip.localPosition=renderer.localBounds.center+Vector3.Scale(axis,renderer.localBounds.extents);
   int affinity=WeaponCatalog.Affinity(definition);
   Color color=affinity>=0?RealmGame.ElementColors[affinity]:new Color(.85f,.92f,1f);
   trail=tip.gameObject.AddComponent<TrailRenderer>();
   trail.time=.13f;trail.numCapVertices=2;trail.numCornerVertices=2;trail.alignment=LineAlignment.View;
   trail.minVertexDistance=.06f;trail.emitting=false;
   trail.widthCurve=new AnimationCurve(new Keyframe(0f,.05f),new Keyframe(1f,0f));
   var mat=new Material(Shader.Find("Sprites/Default")){name="Weapon trail"};
   mat.color=new Color(color.r,color.g,color.b,.85f);
   trail.material=mat;trail.shadowCastingMode=ShadowCastingMode.Off;trail.receiveShadows=false;
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
   if(HasAuthoredGrip(Id))bladeDir=transform.InverseTransformDirection(renderer.transform.TransformDirection(BladeAxis(Id))).normalized;
   Vector3 thinAxis=size.x<=size.y&&size.x<=size.z?Vector3.right:size.y<=size.z?Vector3.up:Vector3.forward;
   Vector3 flatVector=Id==WeaponId.VerdantFang?new Vector3(-0.04481f,0.46121f,0.88616f).normalized:thinAxis;
   flatDir=transform.InverseTransformDirection(renderer.transform.TransformDirection(flatVector));
   if(Id==WeaponId.MoonChakram||Id==WeaponId.BrassFangs){
    flatDir=transform.InverseTransformDirection(renderer.transform.TransformDirection(thinAxis));
   }
   center=transform.InverseTransformPoint(renderer.bounds.center);
   measured=true;
  }
  void Sheath(out Vector3 pos,out Quaternion rot){
   Transform facing=hero&&hero.Visual?hero.Visual.transform:hero.transform;
   if(HasAuthoredGrip(Id)){
    // Handle above the right shoulder, blade down across the back, flat against the torso.
    Vector3 down=(-facing.up-facing.right*.28f).normalized;
    rot=Quaternion.LookRotation(-facing.forward,down)*Quaternion.Inverse(Quaternion.LookRotation(flatDir,bladeDir));
    float y=Id==WeaponId.OrnateCurvedBlade?1.12f:Id==WeaponId.VerdantFang?1.10f:1.02f;
    Vector3 back=facing.TransformPoint(new Vector3(0,y,Id==WeaponId.VerdantFang?-.35f:-.28f));
    pos=back-rot*Vector3.Scale(center,transform.lossyScale);return;
   }
   float zSocket=Id==WeaponId.GlacierMaul?-.33f:-.29f;
   Vector3 socket=facing.TransformPoint(new Vector3(0,1.2f,zSocket));
   rot=Quaternion.LookRotation(-facing.forward,facing.up)*Quaternion.Inverse(Quaternion.LookRotation(flatDir,bladeDir));
   if(Id==WeaponId.MoonChakram||Id==WeaponId.BrassFangs){
    rot=Quaternion.LookRotation(-facing.forward,Id==WeaponId.BrassFangs?facing.right:facing.up)*Quaternion.Inverse(Quaternion.LookRotation(flatDir,bladeDir));
    socket=facing.TransformPoint(new Vector3(0,1.2f,Id==WeaponId.BrassFangs?-.35f:-.28f));
   }
   pos=socket-rot*Vector3.Scale(center,transform.lossyScale);
  }
  void LateUpdate(){
   var game=RealmGame.I;if(!game||game.Screen!=GameScreen.Playing)return;
   if(!hero||!hero.Visual||!hand||!model)return;
   if(InFlight)return;
   string state=hero.Visual.CurrentState;
   if(state.StartsWith("attack_")||state=="charged")drawnUntil=Time.time+1.1f;
   bool shouldDraw=Time.time<drawnUntil;
   if(trail)trail.emitting=shouldDraw&&draw>=.9f;
   draw=Mathf.MoveTowards(draw,shouldDraw?1f:0f,Time.deltaTime*(shouldDraw?9f:3.5f));
   Vector3 handPosition=hand.position;
   if(HasGripAnchor(Id)&&hero.Visual.animator){
    var knuckle=hero.Visual.animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal);
    if(knuckle)handPosition=Vector3.Lerp(hand.position,knuckle.position,.65f);
   }
   if(draw>=1f){transform.SetPositionAndRotation(handPosition,hand.rotation);return;}
   Sheath(out var pos,out var rot);
   transform.position=Vector3.Lerp(pos,handPosition,draw);
   transform.rotation=Quaternion.Slerp(rot,hand.rotation,draw);
  }
  static Transform FindHand(CharacterVisual visual){
   if(visual&&visual.animator){var hand=visual.animator.GetBoneTransform(HumanBodyBones.RightHand);if(hand)return hand;}
   if(visual)foreach(var transform in visual.GetComponentsInChildren<Transform>(true))if(transform.name.Equals("hand_R",StringComparison.OrdinalIgnoreCase)||transform.name.EndsWith("hand_r",StringComparison.OrdinalIgnoreCase)||transform.name.EndsWith("RightHand",StringComparison.OrdinalIgnoreCase))return transform;
   return null;
  }
 }
}
