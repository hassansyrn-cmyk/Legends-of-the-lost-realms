using UnityEditor;
using UnityEngine;
namespace LostRealms {
 public static class ProgressReviewValidation {
  // Run with -realmTest, without -quit. The runtime suite exits when finished.
  public static void Run(){
   if(!RealmGame.Testing)throw new System.InvalidOperationException("Progress review requires -realmTest to protect player saves.");
   for(int id=0;id<=40;id++){
    var definition=WeaponCatalog.Get(id);
    if(!Resources.Load<GameObject>(definition.Resource))throw new System.InvalidOperationException("Missing weapon model: "+definition.Resource);
   }
   Debug.Log("PROGRESS_REVIEW_WEAPON_MODELS_PASSED 41");
   OrganizeSweep.Run();
   QualityValidation.Run();
  }
 }
}
