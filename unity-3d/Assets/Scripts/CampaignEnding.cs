using UnityEngine;
namespace LostRealms {
 public partial class RealmGame {
  bool showEnding;
  public void ShowEnding(){if(Save.stars.Length<25||Save.stars[24]<=0)return;showEnding=true;Screen=GameScreen.Complete;SetMusic("astral_theme");}
  void EndingView(){
   Panel(180,45,920,630);Text(230,90,820,50,"The Heart Unbound",titleC);
   if(headerOrnament)GUI.DrawTexture(new Rect(440,143,400,28),headerOrnament,ScaleMode.ScaleToFit);
   Text(280,198,720,42,"ASTER  /  EPILOGUE",center);
   Text(280,258,720,120,"The Keeper's crown falls silent. The fracture closes, and the paths between all six realms shine again.\n\nAster returns the Heart to the people who kept its light alive.",smallC);
   Text(280,394,720,90,"LYRA: We will rebuild the crossings together. Your journey has given us more than a guardian's promise — it has given us a way home.",smallC);
   Text(280,492,720,40,"Explore the realms, finish your discoveries, and return to the Sanctuary.",tinyC);
   if(MenuButton(new Rect(320,555,300,52),"REALM ATLAS",iconAtlas,primary:true)){showEnding=false;Screen=GameScreen.Map;atlasPage=1;}
   if(MenuButton(new Rect(660,555,300,52),"SANCTUARY",iconSanctuary)){showEnding=false;Screen=GameScreen.Settings;}
  }
 }
}
