using System;
using System.Collections.Generic;
using UnityEngine;
namespace Playground {
// Win/loss records across sessions, from the left (cyan) player's point of view, keyed by matchup,
// e.g. "Laya · english" or "Player 1 vs Player 2". Stored in PlayerPrefs; batch runs stay in memory.
public static class MatchRecords {
 [Serializable] public class Entry {public string key;public int wins,losses;}
 [Serializable] class Store {public List<Entry> entries=new();}
 const string Pref="match_records_v1";static Store store;
 public static bool Persist=!Application.isBatchMode;
 static Store Data{get{if(store==null){try{store=Persist&&PlayerPrefs.HasKey(Pref)?JsonUtility.FromJson<Store>(PlayerPrefs.GetString(Pref)):null;}catch{store=null;}store??=new Store();}return store;}}
 public static Entry Get(string key)=>string.IsNullOrEmpty(key)?null:Data.entries.Find(e=>e.key==key);
 public static Entry Add(string key,bool leftWon){if(string.IsNullOrEmpty(key))return null;var e=Get(key);if(e==null){e=new Entry{key=key};Data.entries.Add(e);}if(leftWon)e.wins++;else e.losses++;if(Persist){PlayerPrefs.SetString(Pref,JsonUtility.ToJson(Data));PlayerPrefs.Save();}return e;}
 public static string Describe(string key,bool versusLabel=true){var e=Get(key);return e==null?null:versusLabel?$"{key}: {e.wins}–{e.losses}":$"{e.wins}–{e.losses}";}
 public static void ResetForTests(){store=new Store();}
}
}
