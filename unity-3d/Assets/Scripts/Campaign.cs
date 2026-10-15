using UnityEngine;
namespace LostRealms {
 public static class Campaign {
  public const int Chapters=25,Stars=75;
  public static int RealmFor(int chapter)=>chapter<=4?0:chapter<=7?1:chapter<=10?2:chapter<=15?3:chapter<=20?4:5;
  public static void Normalize(Progress save){
   if(save.stars==null||save.stars.Length!=Chapters){var old=save.stars;save.stars=new int[Chapters];if(old!=null)System.Array.Copy(old,save.stars,Mathf.Min(old.Length,Chapters));}
   if(save.best==null||save.best.Length!=Chapters){var old=save.best;save.best=new float[Chapters];if(old!=null)System.Array.Copy(old,save.best,Mathf.Min(old.Length,Chapters));}
   save.unlocked=Mathf.Clamp(save.unlocked,1,Chapters);
   // Older completed campaigns capped the next unlock at 15.
   if(save.stars[14]>0)save.unlocked=Mathf.Max(16,save.unlocked);
   if(save.stars[19]>0)save.unlocked=Mathf.Max(21,save.unlocked);
  }
 }
}
