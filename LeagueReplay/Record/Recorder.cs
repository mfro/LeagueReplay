using System;
using System.Collections.Generic;
using System.IO;
using MFroehlich.Parsing.DynamicJSON;
using MFroehlich.Parsing.MFro;
using MFroehlich.League.RiotAPI;

namespace LeagueReplay.Record {
  public class Recorder {
    private const string SpectatorEndpoint = "observer-mode/rest/consumer/getSpectatorGameInfo/NA1/";
    private static readonly string Root = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\LoLGames\";

    public Recorder(string summId) {
      long l;
      if (!long.TryParse(summId, out l))
        throw new ArgumentException(summId + "is not a valid summoner id");
      try {
        var gameData = RiotAPI.CurrentGameAPI.BySummoner("NA1", long.Parse(summId));
        long gameId = gameData.gameId;
        string encrypt = gameData.observers.encryptionKey.ToString();

        Logger.WriteLine("Recording game");
        Logger.WriteLine("Summoner id {0}", summId);
        Logger.WriteLine("Game id {0}", gameId);
        Logger.WriteLine("Encryption key {0}", encrypt);
        Logger.WriteLine("Game Type {0}", gameData.gameType);

        int chunkid = 0, frameId = 0;

        List<Position> chunks = new List<Position>();
        List<Position> frames = new List<Position>();
        Position saved = null;
        using (var file = new TempFile()) {
          Logger.WriteLine("Temp file {0}", file.File.FullName);
          var tmp = file.File.Open(FileMode.Create);
          var timer = new System.Diagnostics.Stopwatch();
          bool over = false;
          #region Replay Download Loop
          while (true) {
            try {
              ChunkInfo info = (dynamic) RESTData.GetChunkInfo(gameId);
              timer.Restart();

              if (saved == null && info.chunkId > info.startGameChunkId) {
                var save = RiotAPI.CurrentGameAPI.BySummoner("NA1", long.Parse(summId));
                if (save.gameStartTime > 0) {
                  byte[] rawSave = MFroFormat.Serialize(save.RawResponse);
                  saved = new Position((int)tmp.Position, rawSave.Length);
                  tmp.Write(rawSave, 0, rawSave.Length);
                }
              }

              if (!over && info.endGameChunkId > 0) {
                Logger.WriteLine("Last chunk {0}", info.endGameChunkId);
                over = true;
              }
              #region Chunk / Frame Downloading
              for (int i = chunkid + 1; i <= info.chunkId; i++) {
                Logger.WriteLine("Fetching chunk {0}", i);
                try {
                  byte[] data = RESTData.GetChunk(gameId, i);
                  chunks.Add(new Position((int)tmp.Position, data.Length));
                  tmp.Write(data, 0, data.Length);
                  chunkid = i;
                } catch (System.Net.WebException x) {
                  var code = (x.Response as System.Net.HttpWebResponse).StatusCode;
                  if (code == System.Net.HttpStatusCode.NotFound){
                    Logger.WriteLine("It is too late to record this game, exiting");
                    return;
                  } else Logger.WriteLine("Error fetching chunk {0}, {1}", i, code);
                }
              }
              for (int i = frameId + 1; i <= info.keyFrameId; i++) {
                Logger.WriteLine("Fetching keyframe {0}", i);
                try {
                  byte[] data = RESTData.GetKeyFrame(gameId, i);
                  frames.Add(new Position((int)tmp.Position, data.Length));
                  tmp.Write(data, 0, data.Length);
                  frameId = i;
                } catch (FileNotFoundException) {
                  Logger.WriteLine("It is too late to record this game, exiting");
                  return;
                }
              }
              #endregion
              if (info.endGameChunkId == info.chunkId && info.chunkId != 0) break;

              int wait = info.nextAvailableChunk - (int)timer.ElapsedMilliseconds;
              if (wait > 0) System.Threading.Thread.Sleep(wait);
            } catch (Exception x) {
              Logger.WriteLine(Priority.Error, x.Message);
              Logger.WriteLine(Priority.Error, x.StackTrace);
            }
          }
          #endregion
          Logger.WriteLine("Saving replay...");
          #region Saving Replay
          dynamic meta = RESTData.GetMetaData(gameId);
          meta["encryptionKey"] = encrypt;
          if (meta.endGameChunkId < 0) {
            Logger.WriteLine("SHIT THE BED");
            meta["endGameChunkId"] = chunkid;
          }

          var endGame = RESTData.GetEndOfGame(gameId);

          tmp.Seek(saved.Offset, SeekOrigin.Begin);
          byte[] saveRaw = new byte[saved.Length];
          tmp.ReadFully(saveRaw, 0, saveRaw.Length);
          var saveData = MFroFormat.Deserialize(saveRaw);

          foreach (var item in saveData["participants"])
            Logger.WriteLine("  " + item["summonerName"]);
          foreach (var item in endGame["teamPlayerParticipantStats"])
            Logger.WriteLine("  " + item["summonerName"]);
          foreach (var item in endGame["otherTeamPlayerParticipantStats"])
            Logger.WriteLine("  " + item["summonerName"]);
          JSONObject combine = Combine(saveData, endGame);

          MFroReplay.Pack(tmp, new FileInfo(Root + gameId + ".lol"), chunks, frames, combine, meta, long.Parse(summId));
          #endregion
          Logger.WriteLine("Saving comblete");
        }
      } catch (Exception x) {
        Logger.WriteLine(Priority.Error, x.Message);
        Logger.WriteLine(Priority.Error, x.StackTrace);
      }
    }
	
	  private static JSONObject Combine(JSONObject gameInfo, JSONObject endGame){
		  dynamic combo = new JSONObject();
		
		  String[] toCopy = {"gameId", "gameType", "gameStartTime", "mapId", "platformId", "gameLength", "gameMode", "gameQueueConfigId"};
      foreach(string key in toCopy)
        combo.Add(key, gameInfo[key]);
		  combo.Add("encryptionKey", gameInfo["observers"]["encryptionKey"]);
		
		  JSONObject comboPlayers = new JSONObject();
		
		  dynamic endGamePlayers = endGame["teamPlayerParticipantStats"];
		  endGamePlayers.AddRange(endGame["otherTeamPlayerParticipantStats"]);
		
		  List<JSONObject> infoPlayers = gameInfo["participants"];
		  for(int i=0;i<infoPlayers.Count;i++){
			  JSONObject player = infoPlayers[i];
        player["statistics"] = FormatStats(endGamePlayers[i].statistics);
			  comboPlayers[JSON.Stringify(player["summonerId"])] = player;
		  }

		  combo.Add("players", comboPlayers);
      return combo;
    }
	
	  private static JSONObject FormatStats(JSONArray stats){
		  JSONObject jsonStats = new JSONObject();
		  foreach(var item in stats){
			  jsonStats[Format(item["statTypeName"].ToString())] = item["value"];
		  }
		  return jsonStats;
	  }

    private static String Format(String capsUnderscoredString) {
      String formatted = "";
      for (int i = 0; i < capsUnderscoredString.Length; i++) {
        if (capsUnderscoredString[i] != '_')
          formatted += char.ToLower(capsUnderscoredString[i]);
        else {
          i++;
          formatted += char.ToUpper(capsUnderscoredString[i]);
        }
      }
      return formatted;
    }
  }
#pragma warning disable 0649
  class ChunkInfo {
    public int chunkId;
    public int keyFrameId;
    public int startGameChunkId;
    public int endGameChunkId;
    public int nextAvailableChunk;
  }
#pragma warning restore 0649
  class TempFile : IDisposable{
    private FileInfo file;
    public TempFile() : this(Path.GetTempFileName()) { }
    public TempFile(string path) {
      if(string.IsNullOrEmpty(path)) throw new ArgumentNullException("path");
      this.file = new FileInfo(path);
    }

    public FileInfo File {
      get {
        if (file == null) throw new ObjectDisposedException(GetType().Name);
        else return file;
      }
    }

    ~TempFile() {Dispose(false);}
    public void Dispose() { Dispose(true); }
    public void Dispose(bool disposing){
      if (disposing)
        GC.SuppressFinalize(this);
      if (file != null) {
        try { file.Delete(); } catch { }
        file = null;
      }
    }
  }
}
