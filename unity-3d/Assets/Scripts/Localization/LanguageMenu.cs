using UnityEngine;
namespace LostRealms {
 public partial class RealmGame {
  void LanguageButton(){
   var rect=new Rect(1010,8,240,44);
   var style=new GUIStyle(smallC){font=GameLanguage.ArabicFont};
   // Native language names make switching back possible without reading the current language.
   if(MenuButton(rect,GameLanguage.IsArabic?"English":"العربية",smallBtn:true,st:style)){
    GameLanguage.SetArabic(!GameLanguage.IsArabic);Sound("power_select");
   }
  }
 }
}
