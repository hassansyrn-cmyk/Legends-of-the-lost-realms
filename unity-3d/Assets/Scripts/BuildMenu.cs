using UnityEngine;
namespace LostRealms {
 public partial class RealmGame {
  bool showBuild;int buildTab,relicPage;
  public void OpenBuild(int tab=0){if(Screen==GameScreen.Playing)Pause();showArsenal=showSkins=showVault=showCodex=showAchievements=false;showBuild=true;buildTab=Mathf.Clamp(tab,0,2);relicPage=0;}
  void BuildText(Rect rect,string message,int size=16,Color? color=null){var style=new GUIStyle(small){fontSize=size,wordWrap=true,alignment=TextAnchor.UpperLeft};style.normal.textColor=color??new Color(.93f,.9f,.79f);GUI.Label(rect,message,style);}
  void BuildView(){
   Panel(120,30,1040,660);Text(160,48,960,38,"Mastery, Relics & Wayfinder",titleC);
   Text(160,90,960,20,"Choose a build in Sanctuary  /  "+Save.coins+" Gold",smallC);
   for(int tab=0;tab<3;tab++)if(MenuButton(new Rect(160+tab*320,122,300,42),new[]{"MASTERY PATHS","RELIC COLLECTION","LYRA'S COMMISSION"}[tab],smallBtn:true,st:smallC)){buildTab=tab;Sound("power_select");}
   if(buildTab==0){
    for(int path=0;path<4;path++){
     float x=160+(path%2)*490,y=184+(path/2)*185;MenuCard(new Rect(x,y,470,170),BuildProgression.Path(CurrentWeapon)==path);
     Text(x+20,y+14,430,26,BuildProgression.Paths[path],smallC);
     for(int option=1;option<=2;option++){
      float bx=x+18+(option-1)*224;BuildText(new Rect(bx,y+48,206,60),BuildProgression.Descriptions[path,option-1],15);
      bool unlocked=BuildProgression.MasteryUnlocked(Save);bool was=GUI.enabled;GUI.enabled=unlocked;
      string caption=(Save.masteryChoices[path]==option?"✓ ":"")+BuildProgression.Perks[path,option-1];
      if(MenuButton(new Rect(bx,y+119,206,35),caption,smallBtn:true,st:smallC)){if(BuildProgression.Choose(Save,path,option)){Persist();Sound("upgrade");}else{Tell("Switching a learned perk costs 25 Gold.");Sound("power_fail");}}
      GUI.enabled=was;
     }
    }
    BuildText(new Rect(180,568,920,46),BuildProgression.MasteryUnlocked(Save)?"First choice in each path is free. Switching its perk costs 25 Gold. Only the path matching your current weapon applies.":"Finish Whispering Falls (chapter 2) to unlock the four paths. Existing chapter completion is recognized.",16);
   }else if(buildTab==1){
    for(int row=0;row<4;row++){
     int id=relicPage*4+row;float x=160+(row%2)*490,y=184+(row/2)*185;MenuCard(new Rect(x,y,470,170),BuildProgression.Has(Save,id));
     bool owned=Save.relics.Contains(id);var tex=RelicArt.RelicTexture(id);
     if(tex)GUI.DrawTexture(new Rect(x+16,y+12,34,34),tex,ScaleMode.ScaleToFit);
     Text(tex?x+56:x+20,y+12,tex?394:430,24,BuildProgression.RelicNames[id]+(owned?"":" — LOCKED"),smallC);
     BuildText(new Rect(x+22,y+47,426,39),BuildProgression.RelicEffects[id],15);BuildText(new Rect(x+22,y+87,426,36),owned?"Slot I or II — each relic can occupy only one slot.":BuildProgression.RelicSources[id],14,new Color(.57f,.73f,.84f));
     bool was=GUI.enabled;GUI.enabled=owned;for(int slot=0;slot<2;slot++)if(MenuButton(new Rect(x+24+slot*218,y+126,200,31),(slot==0?Save.relicA:Save.relicB)==id?"EQUIPPED "+(slot==0?"I":"II"):"EQUIP "+(slot==0?"I":"II"),smallBtn:true,st:smallC)){if(BuildProgression.Equip(Save,id,slot)){Persist();Sound("upgrade");}else Tell("That relic already occupies the other slot.");}GUI.enabled=was;
    }
    if(MenuButton(new Rect(160,568,205,35),"COLLECTION "+(relicPage+1)+"/2",smallBtn:true,st:smallC))relicPage=1-relicPage;
    for(int slot=0;slot<2;slot++){int id=slot==0?Save.relicA:Save.relicB;if(MenuButton(new Rect(385+slot*367,568,345,35),"CLEAR "+(slot==0?"I":"II")+": "+(id>=0?BuildProgression.RelicNames[id]:"EMPTY"),smallBtn:true,st:tinyC)){BuildProgression.Equip(Save,-1,slot);Persist();}}
   }else{
    MenuCard(new Rect(170,190,940,340));Text(200,214,880,32,"Archivist Lyra — The Wayfinder's Oath",smallC);
    BuildText(new Rect(215,260,850,62),"A wayfinder vanished while tracing the fractured realm. Lyra asks Aster to find his map, decipher the temple inscriptions, and restore the oath hidden beneath Rootbound Ruins.",18);
    BuildText(new Rect(215,342,850,66),BuildProgression.QuestObjective(Save),19,new Color(1f,.86f,.48f));
    BuildText(new Rect(215,430,850,52),"Guaranteed rewards: Wayfinder Heart + 200 Gold.\nDungeon: Oathkeeper's Crypt — chapter 3's optional clearing. Room progress is saved.",16);
    bool was=GUI.enabled;GUI.enabled=Save.wayfinderQuest==0||Save.wayfinderQuest==4;if(MenuButton(new Rect(400,550,480,44),Save.wayfinderQuest==0?"ACCEPT LYRA'S COMMISSION":Save.wayfinderQuest==4?"CLAIM WAYFINDER REWARD":Save.wayfinderQuest==5?"COMMISSION COMPLETE":"FOLLOW THE WAYFINDER'S TRAIL",smallBtn:true,st:smallC)){BuildProgression.QuestAction(this);Sound("upgrade");}GUI.enabled=was;
   }
   if(MenuButton(new Rect(480,628,320,42),"BACK TO SANCTUARY",iconBack,smallBtn:true,st:smallC)){showBuild=false;}
  }
 }
}