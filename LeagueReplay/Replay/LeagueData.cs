using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Windows.Media.Imaging;
using MFroehlich.Parsing.JSON;
using MFroehlich.RiotAPI;

namespace LeagueReplay.Replay {
  public static class LeagueData {
    private static string DataPath = App.DataPath + @"LeagueAssets\";
    private const string
      ItemsSave = "items\\",
      ChampSave = "champs\\",
      SpellSave = "spells\\",
      ChampDataSave = "champdata",
      ChampSprite = "http://ddragon.leagueoflegends.com/cdn/{0}/img/champion/",
      SpellSprite = "http://ddragon.leagueoflegends.com/cdn/{0}/img/spell/",
      ItemsSprite = "http://ddragon.leagueoflegends.com/cdn/{0}/img/item/";

    private static Dictionary<int, BitmapImage> Items = new Dictionary<int, BitmapImage>();
    private static Dictionary<int, BitmapImage> Champs = new Dictionary<int, BitmapImage>();
    private static Dictionary<int, BitmapImage> Spells = new Dictionary<int, BitmapImage>();

    public static MFroehlich.RiotAPI.RiotAPI.StaticDataAPI.ChampionListDto ChampData { get; private set; }

    public static BitmapImage GetItem(int id) {
      if (Items.ContainsKey(id))
        return Items[id];
      var img = new BitmapImage(new Uri(Path.Combine(DataPath, ItemsSave + id + ".png")));
      Items[id] = img;
      return img;
    }

    public static BitmapImage GetChamp(int id) {
      if (Champs.ContainsKey(id))
        return Champs[id];
      var img = new BitmapImage(new Uri(Path.Combine(DataPath, ChampSave + id + ".png")));
      Champs[id] = img;
      return img;
    }

    public static BitmapImage GetSpell(int id) {
      if (Spells.ContainsKey(id))
        return Spells[id];
      var img = new BitmapImage(new Uri(Path.Combine(DataPath, SpellSave + id + ".png")));
      Spells[id] = img;
      return img;
    }

    #region Map and Queue types
    public static readonly Dictionary<int, string> QueueTypes = new MyLookup {
      {"Custom", 0 },
      {"Normal 5v5", 2, 14 },
      {"Bots 5v5", 7, 31, 32, 33, 25 },
      {"Normal 3v3", 8 },
      {"Bots 3v3", 52 },
      {"Dominion", 16, 17 },
      {"Ranked SoloQueue", 4 },
      {"Ranked Teams 3v3", 9, 41 },
      {"Ranked Teams 5v5", 6, 42 },
      {"Teambuilder", 61 },
      {"ARAM", 65 },
      {"One For All", 70 },
      {"Snowdown", 72, 73 },
      {"Hexakill", 75, 98 },
      {"URF", 76 },
      {"URF Bots", 83 },
      {"Nightmare Bots", 91, 92, 93 },
      {"Ascension", 96 },
      {"King Poro", 300 },
      {"Nemesis Draft", 310 }
    };

    public static readonly Dictionary<int, string> GameMaps = new MyLookup {
      {"Summoner's Rift", 1, 2, 11},
      {"Proving Grounds", 3},
      {"Twisted Treeline", 4, 10},
      {"Crystal Scar", 8},
      {"Howling Abyss", 12}
    };
    #endregion

    public static void Init() {
      Directory.CreateDirectory(DataPath);

      Logger.WriteLine("Mark 1");

      string latest = RiotAPI.StaticDataAPI.Realm().dd;
      if (new FileInfo(DataPath + ChampDataSave).Exists) {
        using (var load = new FileInfo(DataPath + ChampDataSave).OpenRead())
        using (var mem = new MemoryStream()) {
          load.CopyTo(mem);
          ChampData = MFroehlich.Parsing.MFro.MFroFormat.Deserialize(mem.ToArray()).Save<MFroehlich.RiotAPI.RiotAPI.StaticDataAPI.ChampionListDto>();
        }
        if (ChampData.version.Equals(latest) || latest == null)
          return;
      } else if (latest == null) {
        System.Windows.MessageBox.Show("The riot API request did not work, and no cached data is "
          + "available. Please try again later", "Riot API Request Error",
          System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
      }

      Logger.WriteLine("Mark 2");

      Action<JSONObject, string, string> download = (data, dst, src) => {
        foreach (Object obj in data.Get<JSONObject>("data").Values) {
          JSONObject item = obj as JSONObject;
          FileInfo file = new FileInfo(DataPath + dst + item.Get<int>("id") + ".png");
          if (file.Exists)
            continue;
          Directory.CreateDirectory(file.DirectoryName);
          DownloadImage(string.Format(src, data["version"]) + item["image", "full"]).Save(file.FullName, System.Drawing.Imaging.ImageFormat.Png);
          Logger.WriteLine("Download " + item["name"], Priority.LOW);
        }
      };

      download(RiotAPI.StaticDataAPI.ChampionList(champData: "image").RawResponse, ChampSave, ChampSprite);
      download(RiotAPI.StaticDataAPI.ItemList(itemListData: "image").RawResponse, ItemsSave, ItemsSprite);
      download(RiotAPI.StaticDataAPI.SpellList(spellData: "image").RawResponse, SpellSave, SpellSprite);

      ChampData = RiotAPI.StaticDataAPI.ChampionList(null, null, true);
      byte[] champRaw = MFroehlich.Parsing.MFro.MFroFormat.Serialize(ChampData.RawResponse);
      using (var save = new FileInfo(DataPath + ChampDataSave).OpenWrite())
        save.Write(champRaw, 0, champRaw.Length);

      LeagueReplay.Properties.Resources.Gray.Save(DataPath + ItemsSave + "0.png");
    }

    private static System.Drawing.Image DownloadImage(string url) {
      HttpWebRequest req = HttpWebRequest.Create(url) as HttpWebRequest;
      using (var res = req.GetResponse()) return System.Drawing.Image.FromStream(res.GetResponseStream());
    }
  }

  public class MyLookup : Dictionary<int, string> {
    public void Add(string value, params int[] keys) {
      foreach (var i in keys) Add(i, value);
    }
  }
}
