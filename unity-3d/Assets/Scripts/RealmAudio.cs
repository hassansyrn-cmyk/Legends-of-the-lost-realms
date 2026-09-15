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
  AudioClip Clip(string key){if(!clips.TryGetValue(key,out var clip)){clip=Resources.Load<AudioClip>("Audio/"+key);clips[key]=clip;}return clip;}
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
  public void Play(string name){
   if(!game.Save.sound)return;
   float now=Time.unscaledTime;
   bool frequent=name=="step"||name=="coin"||name=="impact"||name=="enemy_warning";
   float spacing=name=="step"?.12f:frequent?.055f:.025f;
   if(lastPlayed.TryGetValue(name,out float last)&&now-last<spacing)return;
   var clip=Clip("sfx_"+name);if(!clip)return;lastPlayed[name]=now;
   int priority=name=="hurt"||name=="defeat"||name=="complete"||name=="checkpoint"||name=="upgrade"?3:frequent?1:2;
   int slot=-1;
   for(int i=0;i<voices.Length;i++)if(!voices[i].isPlaying){slot=i;break;}
   if(slot<0){for(int i=0;i<voices.Length;i++)if(priorities[i]<priority&&(slot<0||priorities[i]<priorities[slot]))slot=i;}
   if(slot<0)return;
   var voice=voices[slot];voice.Stop();voice.clip=clip;priorities[slot]=priority;
   bool vary=frequent||name=="blade"||name=="enemy_dash"||name=="enemy_defeat";
   voice.pitch=vary?.94f+(float)variation.NextDouble()*.12f:1f;
   voice.volume=(name=="step"?.30f:name=="enemy_warning"?.42f:priority==3?.78f:.62f)*(vary?.92f+(float)variation.NextDouble()*.08f:1f);
   voice.priority=priority==3?48:priority==2?96:160;voice.Play();
  }
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
