using System;
using System.IO;
using System.Text;

using MFroehlich.Parsing.JSON;

namespace LeagueReplay.Record {
  static class RESTData {
    private const string
      SpectatorServer = "http://spectator.na2.lol.riotgames.com/",
      Platform = "NA1",

      EndOfGameUrl = "observer-mode/rest/consumer/endOfGameStats/{0}/{1}/null",
      MetaDataUrl = "observer-mode/rest/consumer/getGameMetaData/{0}/{1}/1/token",
      ChunkInfoUrl = "observer-mode/rest/consumer/getLastChunkInfo/{0}/{1}/1/token",
      ChunkUrl = "observer-mode/rest/consumer/getGameDataChunk/{0}/{1}/{2}/token",
      KeyFrameUrl = "observer-mode/rest/consumer/getKeyFrame/{0}/{1}/{2}/token";

    public static JSONObject GetEndOfGame(long gameId) {
      var req = System.Net.HttpWebRequest.Create(SpectatorServer
        + string.Format(EndOfGameUrl, Platform, gameId));
      using (var res = req.GetResponse())
      using (var mem = new MemoryStream()){
        res.GetResponseStream().CopyTo(mem);
        return MFroehlich.Parsing.AMF.AMF.Deserialize(Convert.FromBase64String(Encoding.UTF8.GetString((mem.ToArray()))));
      }
    }

    public static JSONObject GetMetaData(long gameId) {
      var req = System.Net.HttpWebRequest.Create(SpectatorServer
        + string.Format(MetaDataUrl, Platform, gameId));
      using (var res = req.GetResponse())
      using (var mem = new MemoryStream()) {
        res.GetResponseStream().CopyTo(mem);
        return JSON.ParseObject(mem.ToArray());
      }
    }

    public static JSONObject GetChunkInfo(long gameId) {
      var req = System.Net.HttpWebRequest.Create(SpectatorServer
        + string.Format(ChunkInfoUrl, Platform, gameId));
      using (var res = req.GetResponse())
      using (var mem = new MemoryStream()){
        res.GetResponseStream().CopyTo(mem);
        return JSON.ParseObject(mem.ToArray());
      }
    }

    public static byte[] GetChunk(long gameId, int chunkId) {
      var req = System.Net.HttpWebRequest.Create(SpectatorServer
        + string.Format(ChunkUrl, Platform, gameId, chunkId));
      using (var res = req.GetResponse())
      using (var mem = new MemoryStream()) {
        res.GetResponseStream().CopyTo(mem);
        return mem.ToArray();
      }
    }

    public static byte[] GetKeyFrame(long gameId, int chunkId) {
      var req = System.Net.HttpWebRequest.Create(SpectatorServer
        + string.Format(KeyFrameUrl, Platform, gameId, chunkId));
      using (var res = req.GetResponse())
      using (var mem = new MemoryStream()){
        res.GetResponseStream().CopyTo(mem);
        return mem.ToArray();
      }
    }
  }
}
