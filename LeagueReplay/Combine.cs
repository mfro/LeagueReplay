using System;
using System.Collections.Generic;
using MFroehlich.Parsing.JSON;
using MFroehlich.RiotAPI;

[JSONStructure]
public class CombineData {
  public JSONObject RawResponse { get; set; }
  public long gameId;
  public string gameType;
  public long gameStartTime;
  public Dictionary<string, PlayerData> players;
}

[JSONStructure]
public class PlayerData {
  public int championId;
  public int profileIconId;
  public List<RuneData> runes;
  public bool bot;
  public int teamId;
  public string summonerName;
  public int spell1Id;
  public int spell2Id;
}

[JSONStructure]
public class RuneData {
  public int count;
  public int runeId;
}
