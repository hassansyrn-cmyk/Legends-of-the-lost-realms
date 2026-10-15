namespace LostRealms {
 public enum AdventureKind { Restore, Keys, Elements, Ritual, Ambush, Guardian }
 public sealed class ChapterAdventure {
  public readonly AdventureKind Kind;public readonly string Label,Instruction,Secret;public readonly int[] Route,Elements;public readonly int EliteKind,Signature;
  public ChapterAdventure(AdventureKind kind,string label,string instruction,int[] route,string secret,int elite=-1,int signature=0,int[] elements=null){Kind=kind;Label=label;Instruction=instruction;Route=route;Secret=secret;EliteKind=elite;Signature=signature;Elements=elements;}
  public bool Ordered=>Kind==AdventureKind.Elements||Kind==AdventureKind.Ritual;
  public bool Guarded=>Kind!=AdventureKind.Elements;
  public static ChapterAdventure Get(int chapter)=>All[chapter-1];
  // Authored melee/ranged pairings at objective clearings, before scenery settles.
  public static int GuardKind(int chapter,int route,int fallback){
   if(System.Array.IndexOf(Get(chapter).Route,route)<0||!Get(chapter).Guarded)return fallback;
   return chapter==1?0:chapter==2?22:chapter==3?19:chapter==5?19:chapter==6?25:chapter==11?23:chapter==12?20:chapter==13?24:fallback;
  }
  public static int PartnerKind(int chapter,int route){
   if(System.Array.IndexOf(Get(chapter).Route,route)<0||!Get(chapter).Guarded||chapter==1||Get(chapter).Kind==AdventureKind.Guardian)return -1;
   return chapter==2||chapter==3||chapter==5?7:chapter==6?23:17;
  }
  static readonly ChapterAdventure[] All={
   new ChapterAdventure(AdventureKind.Restore,"SHRINES","Jump twice between islands. Clear the grove patrol and restore the trail shrine.",new[]{3},"EXPLORER'S CACHE"),
   new ChapterAdventure(AdventureKind.Restore,"BEACONS","Clear the guards and restore both crossing beacons. Either beacon in the second clearing works.",new[]{3,7},"WATERFALL SENTINEL",22,1),
   new ChapterAdventure(AdventureKind.Keys,"KEYS","Recover the two ruin keys from their guards to unseal the exit.",new[]{3,7},"ROOTBOUND DUELIST",16,2),
   new ChapterAdventure(AdventureKind.Guardian,"SEALS","Restore the grove seals on the approach, then defeat the Heartwood guardian.",new[]{1,3},"ELDER GROVE MEMORY"),
   new ChapterAdventure(AdventureKind.Ambush,"OUTPOSTS","Clear each desert outpost, withstand its reinforcement wave, then restore the beacon.",new[]{3,7},"CARAVAN CACHE"),
   new ChapterAdventure(AdventureKind.Keys,"KEYS","Recover both guarded temple keys. Their return passage opens after the second key.",new[]{3,7},"BURIAL CHAMBER WARDEN",25,3),
   new ChapterAdventure(AdventureKind.Guardian,"SEALS","Break the desert approach seals, then defeat the lava golem.",new[]{1,3},"SUNSCAR MEMORY"),
   new ChapterAdventure(AdventureKind.Elements,"WARDS","Attune the ascent wards in order: FROST, then EMBER. Select their matching powers.",new[]{3,7},"FROZEN EXPLORER'S CACHE",elements:new[]{1,0}),
   new ChapterAdventure(AdventureKind.Elements,"CRYSTALS","Attune crystals in order: EMBER, FROST, GALE. Select the matching power beside each crystal.",new[]{3,7,10},"CRYSTAL SENTINEL",19,4,new[]{0,1,2}),
   new ChapterAdventure(AdventureKind.Guardian,"SEALS","Restore the winter approach seals, then defeat the Whiteout guardian.",new[]{1,3},"WINTER MEMORY"),
   new ChapterAdventure(AdventureKind.Ritual,"RITUALS","Defeat the ritual guards, then channel beside each focus in order to cleanse the foothills.",new[]{3,7},"EMBER PILGRIM'S CACHE"),
   new ChapterAdventure(AdventureKind.Ambush,"RAMPARTS","Clear the rampart defenders and their reinforcements before restoring each rally point.",new[]{3,7,10},"RAMPART HEXER",23,5),
   new ChapterAdventure(AdventureKind.Ritual,"RITUALS","Disrupt the gorge ritual in order. Clear its guards, then channel at all three foci.",new[]{3,7,10},"CINDERVEIN CACHE"),
   new ChapterAdventure(AdventureKind.Elements,"SIGILS","Reconnect the ascent sigils in order: GALE, EMBER, FROST. Match the selected power.",new[]{3,7,10},"OBSIDIAN EXECUTIONER",20,6,new[]{2,0,1}),
   new ChapterAdventure(AdventureKind.Guardian,"SEALS","Restore the summit approach seals, then defeat the Emberfall Warden.",new[]{1,3},"LAST WARDEN MEMORY")
  };
 }
}
