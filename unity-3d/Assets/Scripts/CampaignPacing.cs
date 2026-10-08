using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace LostRealms {
 // Local-only, bounded playtest evidence. Automated runs never touch player data.
 public sealed class CampaignPacing:MonoBehaviour {
  [Serializable] public class Run {public string startedUtc,outcome;public int chapter,startWeapon,endWeapon,deaths,kills,coins,gems,objectives,initialEnemies,livingEnemies,healthRank,arsenalRank,powerRank;public bool trialCompleted,optionalVisited,optionalCompleted;public float activeSeconds,pausedSeconds,menuSeconds,retrySeconds,scaledSeconds;}
  [Serializable] public class History {public List<Run> runs=new List<Run>();}
  public Run Current {get;private set;}public Run Last {get;private set;}
  RealmGame Game=>GetComponent<RealmGame>();
  public void Begin(){Close("restarted");Current=new Run{startedUtc=DateTime.UtcNow.ToString("o"),chapter=Game.Level,startWeapon=Game.Save.equippedWeapon,initialEnemies=Game.Enemies.Count,healthRank=Game.Save.healthRank,arsenalRank=Game.Save.arsenalRank,powerRank=Game.Save.powerRank};}
  void Update(){var g=Game;if(Current==null)return;if(g.Screen==GameScreen.Playing)Current.activeSeconds+=Time.unscaledDeltaTime;else if(g.Screen==GameScreen.Paused)Current.pausedSeconds+=Time.unscaledDeltaTime;else if(g.Screen==GameScreen.Defeated)Current.retrySeconds+=Time.unscaledDeltaTime;else {Current.menuSeconds+=Time.unscaledDeltaTime;if(g.Screen==GameScreen.Menu||g.Screen==GameScreen.Map||g.Screen==GameScreen.Settings)Close("left_chapter");}}
  public void Death(){if(Current!=null)Current.deaths++;}
  public void Close(string outcome){
   if(Current==null)return;var g=Game;Current.outcome=outcome;Current.endWeapon=g.Save.equippedWeapon;Current.kills=g.Kills;Current.coins=g.Coins;Current.gems=g.Gems;Current.scaledSeconds=g.Elapsed;Current.livingEnemies=g.Enemies.FindAll(e=>e&&e.Health>0).Count;Current.trialCompleted=g.Trial&&g.Trial.Completed;Current.objectives=g.Adventure?g.Adventure.CompletedCount:0;Current.optionalVisited=g.Adventure&&g.Adventure.BranchVisited;Current.optionalCompleted=g.Adventure&&g.Adventure.RewardClaimed;Last=Current;Current=null;
   if(RealmGame.Testing)return;
   try {string path=Path.Combine(Application.persistentDataPath,"campaign-pacing.json");var history=File.Exists(path)?JsonUtility.FromJson<History>(File.ReadAllText(path)):new History();if(history==null||history.runs==null)history=new History();history.runs.Add(Last);while(history.runs.Count>100)history.runs.RemoveAt(0);File.WriteAllText(path,JsonUtility.ToJson(history,true));}catch(Exception e){Debug.LogWarning("Pacing report could not be saved: "+e.Message);}
  }
  void OnApplicationQuit(){Close("quit");}
 }
}
