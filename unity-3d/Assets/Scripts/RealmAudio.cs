using System.Collections.Generic;
using UnityEngine;

namespace LostRealms {
 // Two music decks and a bounded voice bank keep transitions smooth and combat legible.
 public sealed class RealmAudio:MonoBehaviour {
  readonly Dictionary<string,AudioClip> clips=new Dictionary<string,AudioClip>();
  readonly Dictionary<string,float> lastPlayed=new Dictionary<string,float>();
  readonly AudioSource[] voices=new AudioSource[12];
  readonly int[] priorities=new int[12];
  readonly System.Random variation=new System.Random();
  AudioSource front,back;string track;float blend=1f,frontStart,backStart;bool suspended;
  RealmGame game;
  public int ActiveVoices { get {int count=0;foreach(var voice in voices)if(voice&&voice.isPlaying)count++;return count;} }
  public string CurrentTrack=>track;
  public void Initialize(RealmGame owner){
   game=owner;front=owner.Music;back=gameObject.AddComponent<AudioSource>();
   front.playOnAwake=back.playOnAwake=false;front.loop=back.loop=true;front.volume=back.volume=0;
   for(int i=0;i<voices.Length;i++){voices[i]=gameObject.AddComponent<AudioSource>();voices[i].playOnAwake=false;voices[i].spatialBlend=0;}
  }
    AudioClip Clip(string key){
     if(key=="sfx_boss")key="sfx_trap_crush";
     if(key=="sfx_boss_roar")key+="_"+(game&&game.Realm==3?21:8+(game?game.Realm:0));
     if(!clips.TryGetValue(key,out var clip)){
      if(key=="sfx_boss"||key=="sfx_boss_roar"||key.StartsWith("sfx_boss_roar_")){
       int r=game?game.Realm:0;
       string[] roars=new[]{"guardian_heartwood","guardian_sunscar","guardian_whiteout","guardian_warden"};
       // Kind-routed roars so each guardian keeps its own voice even when
       // two bosses share a realm; unknown kinds fall back to the realm roar.
       if(key.StartsWith("sfx_boss_roar_")&&int.TryParse(key.Substring("sfx_boss_roar_".Length),out int kind)){
        string[] byKind=new string[22];
        byKind[8]="guardian_heartwood";byKind[9]="guardian_sunscar";byKind[10]="guardian_whiteout";byKind[21]="guardian_warden";
        if(kind>=0&&kind<byKind.Length&&byKind[kind]!=null)clip=Resources.Load<AudioClip>("Audio/sfx_"+byKind[kind]);
       }
       if(!clip&&r>=0&&r<roars.Length)clip=Resources.Load<AudioClip>("Audio/sfx_"+roars[r]);
       if(!clip)clip=Resources.Load<AudioClip>("Audio/sfx_boss");
      }else{
       clip=Resources.Load<AudioClip>("Audio/"+key);
       if(!clip&&key=="sfx_weapon")clip=Resources.Load<AudioClip>("Audio/sfx_weapon_pickup");
       if(!clip&&key=="sfx_upgrade")clip=Resources.Load<AudioClip>("Audio/sfx_weapon_pickup");
       if(!clip&&key=="verdant_theme")clip=Resources.Load<AudioClip>("Audio/verdant_realm_theme");
       if(!clip&&key=="desert_exploration_theme")clip=Resources.Load<AudioClip>("Audio/sunscar_realm_theme");
       if(!clip&&key=="frozen_exploration_theme")clip=Resources.Load<AudioClip>("Audio/whiteout_realm_theme");
       if(!clip&&key=="emberfall_exploration_theme")clip=Resources.Load<AudioClip>("Audio/lava_realm_theme");
       if(!clip&&key=="lava_realm_theme")clip=Resources.Load<AudioClip>("Audio/emberfall_exploration_theme");
       if(!clip&&key=="boss_battle_theme")clip=Resources.Load<AudioClip>("Audio/boss_fight_theme");
      }
      clips[key]=clip;
     }
     return clip;
    }
   public void SetTrack(string key){
    if(track==key)return;
    var clip=Clip(key);if(!clip)return;
    track=key;var previous=front;front=back;back=previous;game.Music=front;
    backStart=back.volume;frontStart=0;front.Stop();front.clip=clip;front.volume=0;blend=0;
    front.mute=!game.Save.music;front.Play();if(suspended)front.Pause();
   }
   public void Suspend(bool value){
    if(suspended==value)return;suspended=value;
    if(value){front.Pause();back.Pause();foreach(var voice in voices)voice.Pause();}
    else {front.UnPause();back.UnPause();foreach(var voice in voices)voice.UnPause();}
   }
   public void Play(string name,float volumeScale=1f,float pitchMul=1f){
    if(!game||!game.Save.sound)return;
    float now=Time.unscaledTime;
    bool frequent=name=="step"||name=="step2"||name=="coin"||name=="impact"||name=="enemy_warning";
    float spacing=name.StartsWith("trap_")?.18f:name=="enemy_warning"?.18f:name=="step"||name=="step2"?.12f:frequent?.055f:.025f;
    if(name=="enemy_warning"){
     int activeWarn=0;
     for(int w=0;w<voices.Length;w++)if(voices[w].isPlaying&&priorities[w]==1)activeWarn++;
     if(activeWarn>=1)return;
    }
    if(lastPlayed.TryGetValue(name,out float last)&&now-last<spacing)return;
    var clip=Clip("sfx_"+name);if(!clip)return;lastPlayed[name]=now;
    int priority=name=="hurt"||name=="defeat"||name=="complete"||name=="checkpoint"||name=="upgrade"||name.StartsWith("boss")||name=="weapon_pickup"?3:frequent?1:2;
    int slot=-1;
    for(int i=0;i<voices.Length;i++)if(!voices[i].isPlaying){slot=i;break;}
    if(slot<0){for(int i=0;i<voices.Length;i++)if(priorities[i]<priority&&(slot<0||priorities[i]<priorities[slot]))slot=i;}
    if(slot<0)return;
    var voice=voices[slot];voice.Stop();voice.clip=clip;priorities[slot]=priority;
    bool vary=name.StartsWith("attack_")||name.StartsWith("trap_")||frequent||name=="blade"||name=="sword_slash"||name=="punch"||name=="enemy_dash"||name=="enemy_defeat"||name=="player_dash";
    voice.pitch=(vary?.94f+(float)variation.NextDouble()*.12f:1f)*pitchMul;
    float baseVol=(name.StartsWith("boss")?.85f:(name=="step"||name=="step2")?.32f:name=="enemy_warning"?.42f:priority==3?.78f:.62f)*(vary?.92f+(float)variation.NextDouble()*.08f:1f);
    voice.volume=baseVol*volumeScale;
    voice.priority=priority==3?48:priority==2?96:160;voice.Play();
   }
   public void PlaySpatial(string name,Vector3 worldPos,float minDistance=3f,float maxDistance=15f,float volumeMul=1f){
    if(!game||!game.Save.sound)return;
    var player=game.Player;
    Vector3 listener=player?player.transform.position:(Camera.main?Camera.main.transform.position:Vector3.zero);
    float dist=Vector3.Distance(worldPos,listener);
    if(dist>=maxDistance)return;
    float t=Mathf.Clamp01(1f-(dist-minDistance)/Mathf.Max(0.1f,maxDistance-minDistance));
    float vol=t*t*volumeMul;
    if(vol<=0.02f)return;
    Play(name,vol);
   }
   public AudioClip GetClip(string key)=>Clip(key);
  // Per-boss voice pitch so guardians never sound alike: deep colossus,
  // titan mid, crystalline whiteout higher, emberfall dread low.
  public static float BossPitch(int kind)=>kind==8?.85f:kind==9?.95f:kind==10?1.1f:kind==21?.8f:1f;
  public void ClearRunSounds(){foreach(var voice in voices)voice.Stop();lastPlayed.Clear();}
  void Update(){
   if(!game)return;
   bool pause=game.Screen==GameScreen.Paused;
   Suspend(pause);
   front.mute=back.mute=!game.Save.music;
   foreach(var voice in voices)voice.mute=!game.Save.sound;
   if(suspended)return;
   blend=Mathf.MoveTowards(blend,1f,Time.unscaledDeltaTime/1.2f);
   float t=blend*blend*(3f-2f*blend);
   float volume=game.Screen==GameScreen.Playing?.24f:.18f;
   if(game.Screen==GameScreen.Defeated||game.Screen==GameScreen.Complete)volume=.11f;
   front.volume=Mathf.Lerp(frontStart,volume,t);back.volume=backStart*(1f-t);
   if(blend>=1f&&back.isPlaying)back.Stop();
  }
 }
}
