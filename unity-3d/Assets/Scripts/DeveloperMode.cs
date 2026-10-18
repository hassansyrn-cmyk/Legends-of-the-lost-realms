using UnityEngine;

namespace LostRealms {
 public partial class RealmGame {
  Progress developerOriginalSave;
  bool showDeveloperPrompt;
  string developerCode="",developerError="";
  public bool DeveloperMode=>developerOriginalSave!=null;

  public bool TryEnableDeveloperMode(string password){
   if(password!="3258"){developerCode="";developerError="Incorrect password. Try again.";return false;}
   if(DeveloperMode)return true;
   var testingSave=JsonUtility.FromJson<Progress>(JsonUtility.ToJson(Save));
   Persist();developerOriginalSave=Save;Save=testingSave;
   EnsureDeveloperUnlocks();CloseDeveloperPrompt();return true;
  }

  void EnsureDeveloperUnlocks(){
   if(!DeveloperMode)return;
   Save.NormalizeWeapons();Save.NormalizeSkins();
   Save.unlocked=Campaign.Chapters;
   for(int id=0;id<=WeaponCatalog.MaxId;id++)if(!Save.weapons.Contains(id))Save.weapons.Add(id);
   for(int id=0;id<SkinCatalog.Count;id++)if(!Save.skins.Contains(id))Save.skins.Add(id);
  }

  public void DisableDeveloperMode(){
   if(!DeveloperMode)return;
   Save=developerOriginalSave;developerOriginalSave=null;CloseDeveloperPrompt();
   showArsenal=showSkins=showVault=false;wardrobePreviewSkin=-1;
   if(wardrobePreview){Destroy(wardrobePreview);wardrobePreview=null;}
   if(Player){
    foreach(var old in Player.GetComponentsInChildren<EquippedWeapon>(true)){
     old.gameObject.SetActive(false);old.transform.SetParent(null);Destroy(old.gameObject);
    }
    Player.ApplySkin(CurrentSkin,false);
    if(Save.equippedWeapon>=0)EquippedWeapon.Equip(Player,(WeaponId)Save.equippedWeapon);
   }
  }

  public void OpenDeveloperPrompt(){developerCode="";developerError="";showDeveloperPrompt=true;}
  void CloseDeveloperPrompt(){showDeveloperPrompt=false;developerCode="";developerError="";}
  void DeveloperModeButton(Rect rect){
   if(!MenuButton(rect,DeveloperMode?"DEVELOPER MODE: ON / TURN OFF":"DEVELOPER MODE",smallBtn:true,st:smallC))return;
   if(DeveloperMode){DisableDeveloperMode();Tell("Developer mode off. Normal journey restored.",4f);}
   else OpenDeveloperPrompt();
   Sound("power_select");
  }
  void EnterDeveloperDigit(char digit){if(developerCode.Length<4){developerCode+=digit;developerError="";}}
  void SubmitDeveloperCode(){
   if(TryEnableDeveloperMode(developerCode)){Tell("Developer mode on. All chapters, weapons and outfits unlocked for this test session.",5f);Sound("upgrade");}
   else Sound("power_fail");
  }
  void DeveloperModeView(){
   Box(new Rect(0,0,1280,720),new Color(.01f,.025f,.045f,.98f));Panel(390,50,500,620);
   Text(420,78,440,36,"Developer Mode",titleC);
   Text(430,126,420,48,"Unlock every chapter, weapon and outfit for testing.\nYour normal journey stays saved.",smallC);
   BoxOutline(new Rect(440,190,400,44),new Color(.02f,.05f,.09f,1),MenuGold,1f);
   Text(450,198,380,30,developerCode.Length==0?"ENTER 4-DIGIT PASSWORD":new string('*',developerCode.Length),smallC);
   Text(430,236,420,20,developerError,tinyC);
   for(int i=0;i<9;i++)if(MenuButton(new Rect(468+(i%3)*120,266+(i/3)*64,104,52),(i+1).ToString(),smallBtn:true,st:smallC))EnterDeveloperDigit((char)('1'+i));
   if(MenuButton(new Rect(468,458,104,52),"CLEAR",smallBtn:true,st:tinyC)){developerCode="";developerError="";}
   if(MenuButton(new Rect(588,458,104,52),"0",smallBtn:true,st:smallC))EnterDeveloperDigit('0');
   if(MenuButton(new Rect(708,458,104,52),"DELETE",smallBtn:true,st:tinyC)&&developerCode.Length>0)developerCode=developerCode.Substring(0,developerCode.Length-1);
   if(MenuButton(new Rect(440,530,400,52),"ACTIVATE TEST MODE",primary:true))SubmitDeveloperCode();
   if(MenuButton(new Rect(440,598,400,48),"CANCEL",iconBack,smallBtn:true,st:smallC))CloseDeveloperPrompt();
   var input=Event.current;
   if(input.type==EventType.KeyDown){
    if(input.character>='0'&&input.character<='9'){EnterDeveloperDigit(input.character);input.Use();}
    else if(input.keyCode==KeyCode.Backspace){if(developerCode.Length>0)developerCode=developerCode.Substring(0,developerCode.Length-1);input.Use();}
    else if(input.keyCode==KeyCode.Return||input.keyCode==KeyCode.KeypadEnter){SubmitDeveloperCode();input.Use();}
   }
  }
 }
}
