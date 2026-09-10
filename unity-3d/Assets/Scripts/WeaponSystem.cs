using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace LostRealms {
 public enum WeaponId { AstersBlade=0, LongSword=1, Axe=2, CurvedSword=3 }

 public readonly struct WeaponDefinition {
  public readonly WeaponId Id;public readonly string Name,Resource,Summary;public readonly float Damage,Reach,Tempo,ModelScale;public readonly Vector3 EquipEuler;
  public WeaponDefinition(WeaponId id,string name,string resource,float damage,float reach,float tempo,float modelScale,Vector3 equipEuler,string summary){Id=id;Name=name;Resource=resource;Damage=damage;Reach=reach;Tempo=tempo;ModelScale=modelScale;EquipEuler=equipEuler;Summary=summary;}
 }

 public static class WeaponCatalog {
  static readonly WeaponDefinition[] Definitions={
   new WeaponDefinition(WeaponId.AstersBlade,"ASTER'S BLADE","",1f,1f,1f,0,Vector3.zero,"Balanced starter blade"),
   new WeaponDefinition(WeaponId.LongSword,"LONGSWORD","Weapons/Aster_LongSword",1.25f,1.28f,.9f,.015f,new Vector3(10,90,-90),"+25% damage  •  +28% reach  •  measured tempo"),
   new WeaponDefinition(WeaponId.Axe,"WAR AXE","Weapons/Aster_Axe",1.6f,.88f,.76f,.015f,new Vector3(8,90,-90),"+60% damage  •  heavy recovery  •  close range"),
   new WeaponDefinition(WeaponId.CurvedSword,"CURVED SWORD","Weapons/Aster_CurvedSword",.98f,.94f,1.22f,.015f,new Vector3(12,90,-90),"rapid strikes  •  swift recovery  •  agile reach")
  };
  public static WeaponDefinition Get(int rawId){return Definitions[Mathf.Clamp(rawId,0,Definitions.Length-1)];}
  public static WeaponDefinition Get(WeaponId id)=>Get((int)id);
  public static GameObject CreateModel(WeaponId id,Transform parent,float scale){
   var definition=Get(id);if(string.IsNullOrEmpty(definition.Resource))return null;
   var prefab=Resources.Load<GameObject>(definition.Resource);if(!prefab){Debug.LogError("WEAPON_MODEL_MISSING: "+definition.Resource);return null;}
   var model=UnityEngine.Object.Instantiate(prefab,parent);model.name=definition.Name+" model";model.transform.localPosition=Vector3.zero;model.transform.localRotation=Quaternion.identity;model.transform.localScale=Vector3.one*scale;
   foreach(var renderer in model.GetComponentsInChildren<Renderer>(true)){renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;}
   return model;
  }
 }

 public sealed class WeaponDrop:MonoBehaviour {
  public WeaponId Id;public Vector3 Origin;Transform model;float phase;
  public void Configure(WeaponId id,Vector3 origin){Id=id;Origin=origin;phase=((int)id+1)*1.37f;var definition=WeaponCatalog.Get(id);var created=WeaponCatalog.CreateModel(id,transform,.018f);model=created?created.transform:null;if(!model)Debug.LogError("WEAPON_DROP_SETUP_FAILED: "+definition.Name);}
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
  public static void Equip(Hero hero,WeaponId id){
   var old=hero.GetComponentInChildren<EquippedWeapon>();if(old)UnityEngine.Object.Destroy(old.gameObject);
   var definition=WeaponCatalog.Get(id);if(id==WeaponId.AstersBlade)return;
   var hand=FindHand(hero.Visual);if(!hand){Debug.LogError("WEAPON_EQUIP_FAILED: no Aster right hand");return;}
   var root=new GameObject("Equipped "+definition.Name);root.transform.SetParent(hand,false);var equipped=root.AddComponent<EquippedWeapon>();equipped.Id=id;
   equipped.model=WeaponCatalog.CreateModel(id,root.transform,definition.ModelScale);
   if(!equipped.model){UnityEngine.Object.Destroy(root);return;}
   equipped.model.transform.localPosition=new Vector3(0,.73f,0);equipped.model.transform.localRotation=Quaternion.Euler(definition.EquipEuler);
  }
  static Transform FindHand(CharacterVisual visual){
   if(visual&&visual.animator){var hand=visual.animator.GetBoneTransform(HumanBodyBones.RightHand);if(hand)return hand;}
   if(visual)foreach(var transform in visual.GetComponentsInChildren<Transform>(true))if(transform.name.Equals("hand_R",StringComparison.OrdinalIgnoreCase)||transform.name.EndsWith("hand_r",StringComparison.OrdinalIgnoreCase)||transform.name.EndsWith("RightHand",StringComparison.OrdinalIgnoreCase))return transform;
   return null;
  }
 }
}
