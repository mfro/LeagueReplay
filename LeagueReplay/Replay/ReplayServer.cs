using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using MFroehlich.Parsing.JSON;

namespace LeagueReplay.Replay {
  public class ReplayServer {
    #region Http Codes
    public const int HTTP_OK = 200;
    public const int HTTP_NOTFOUND = 404;
    #endregion

    private const int port = 19632;

    private HttpListener server;
    private volatile bool alive;
    private int counter;
    private MFroReplay replay;
    private static string[] SpectateArgs = { "\"8394\"", "\"LoLLauncher.exe\"", "\"\"",
                                             "\"spectator 127.0.0.1:"+port+" {0} {1} NA1\"" };

    public ReplayServer(MFroReplay replay) {
      this.replay = replay;
      this.alive = true;

      try {
        server = new HttpListener();
        server.Prefixes.Add("http://127.0.0.1:" + port + "/observer-mode/rest/consumer/");
        server.Start();
      } catch {
        try { AddAddress("http://127.0.0.1:" + port + "/"); } catch { return; }
        server = new HttpListener();
        server.Prefixes.Add("http://127.0.0.1:" + port + "/observer-mode/rest/consumer/");
        server.Start();
      }
      new Thread(this.Stopper) { IsBackground = true, Name = "Server Stopper" }.Start();
      new Thread(() => {
        Logger.WriteLine("Starting Spectator Server [ID: {0}]... ".Format(replay.GameId));
        try {
          while (alive) { Handle(server.GetContext()); }
        } catch { }
        server.Close();
        Logger.WriteLine("Closing Spectator Server");
      }) { IsBackground = true, Name = "ServerHandler" }.Start();

      var dir = new System.IO.DirectoryInfo(@"C:\Riot Games\League of Legends\RADS\"
        + @"solutions\lol_game_client_sln\releases\");
      var versions = dir.EnumerateDirectories().ToList();
      versions.Sort((a, b) => b.Name.CompareTo(a.Name));

      ProcessStartInfo info = new ProcessStartInfo(versions[0].FullName + @"\deploy\League of Legends.exe",
        String.Join(" ", SpectateArgs).Format(replay.MetaData["encryptionKey"], replay.GameId));
      info.WorkingDirectory = versions[0].FullName + @"\deploy";
      Process.Start(info);
    }

    private static void AddAddress(string url, string user = "everyone") {
      System.Windows.MessageBox.Show("Spectating requires administrator permission to register the HTTP server, "
          + "please grant permission to the NetSH command to watch replays.\nYou only have to do this once",
          "Requires Admin Permissions", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
      string cmd = string.Format(@"http add urlacl url={0} user={1}", url, user);
      ProcessStartInfo proc = new ProcessStartInfo("netsh", cmd);
      proc.Verb = "runas";
      proc.CreateNoWindow = true;
      proc.WindowStyle = ProcessWindowStyle.Hidden;
      proc.UseShellExecute = true;
      Process.Start(proc).WaitForExit();
    }
    
    private void Stopper() {
      while (true) {
        Thread.Sleep(10000);
        Process[] open = Process.GetProcessesByName("League of Legends");
        if (open.Length == 0 && server.IsListening) {
          Logger.WriteLine("League Client Closed");
          server.Stop();
        }
      }
    }

    private void Handle(HttpListenerContext context) {
      try {
        var request = context.Request;
        var response = context.Response;
        var stream = response.OutputStream;
        var target = request.Url.AbsolutePath;
        Logger.WriteLine(target, Priority.LOW);

        response.ContentType = "text/plain";
        string[] path = target.Split('/');
        switch (path[4]) {
          case "version":
            response.StatusCode = HTTP_OK;
            stream.Write("1.82.89");
            break;
          case "getGameMetaData":
            response.StatusCode = HTTP_OK;
            stream.Write(JSON.Stringify(replay.MetaData));
            break;
          case "getLastChunkInfo":
            response.StatusCode = HTTP_OK;
            if (path[7].Equals("0")) counter++;
            JSONObject meta = replay.MetaData;
            JSONObject data = new JSONObject() {
              {"duration", 30000},
              {"availableSince", 30000},
              {"nextAvailableChunk", 30000},
              {"endStartupChunkId", meta.Get<int>("endStartupChunkId")},
              {"startGameChunkId", meta.Get<int>("startGameChunkId")} };
            if (counter > 1) {
              data.Add("chunkId", meta.Get<int>("endGameChunkId"));
              data.Add("keyFrameId", meta.Get<int>("lastKeyFrameId"));
              data.Add("nextChunkId", meta.Get<int>("endGameChunkId"));
              data.Add("endGameChunkId", meta.Get<int>("endGameChunkId"));
            } else {
              data.Add("chunkId", meta.Get<int>("startGameChunkId"));
              data.Add("keyFrameId", 1);
              data.Add("nextChunkId", meta.Get<int>("startGameChunkId"));
              data.Add("endGameChunkId", 0);
            }
            stream.Write(JSON.Stringify(data));
            break;
          case "getLastKeyFrameInfo":
          case "endOfGameStats": response.StatusCode = HTTP_NOTFOUND; break;
          case "getGameDataChunk":
            response.StatusCode = HTTP_OK;
            byte[] chunk = replay.GetChunk(int.Parse(path[7]));
            stream.Write(chunk, 0, chunk.Length);
            break;
          case "getKeyFrame":
            response.StatusCode = HTTP_OK;
            byte[] frame = replay.GetFrame(int.Parse(path[7]));
            stream.Write(frame, 0, frame.Length);
            break;
        }
        stream.Close();
        response.Close();
      } catch (Exception x) {
        Logger.WriteLine(x.GetType() + ": " + x.Message, Priority.ERROR);
        Logger.WriteLine(x.StackTrace, Priority.ERROR);
      }
    }
  }
}
