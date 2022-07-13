using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;

public class Modding : MonoBehaviour
{
    public ModSave modSave;
    public Color disqualified, barely, qualified;
    public static Modding modding;
    public Manager manager;
    [System.Serializable]
    public struct Logo
    {
        public string name;
        public Sprite sprite;
        public Logo(string name, Sprite sprite) { this.name = name; this.sprite = sprite; }
    }
    public List<Logo> logos;
    public Sprite GetLogo(string name)
    {
        for (int i = 0; i < logos.Count; i++) if (logos[i].name == name) return logos[i].sprite;
        return null;
    } 
    //*notmine
    public Texture2D LoadTexture(string FilePath)
    {
        Texture2D Tex2D;
        byte[] FileData;
        //Debug.Log(FilePath);
        if (File.Exists(FilePath))
        {
            FileData = File.ReadAllBytes(FilePath);
            Tex2D = new Texture2D(2, 2);
            if (Tex2D.LoadImage(FileData))
                return Tex2D;
        }
        return null;
    }

    public Sprite LoadSprite(string FilePath)
    {
        Texture2D SpriteTexture = LoadTexture(FilePath);
        return Sprite.Create(SpriteTexture, new Rect(0, 0, SpriteTexture.width, SpriteTexture.height), new Vector2(0.5f, 0.5f));
    }//*/

    public bool HasLogos() { return logos.Count != 0; }


    private void Awake()
    {
        modding = this;
        if (!Directory.Exists(Application.persistentDataPath + "/Mods"))
            Directory.CreateDirectory(Application.persistentDataPath + "/Mods");
        logos = new List<Logo>(0);
        if (Directory.Exists(Application.persistentDataPath + "/Mods/Logos"))
        {
            var pngs = Directory.GetFiles(Application.persistentDataPath + "/Mods/Logos", "*.png", SearchOption.TopDirectoryOnly);
            var jpgs = Directory.GetFiles(Application.persistentDataPath + "/Mods/Logos", "*.jpg", SearchOption.TopDirectoryOnly);
            var jpegs = Directory.GetFiles(Application.persistentDataPath + "/Mods/Logos", "*.jpeg", SearchOption.TopDirectoryOnly);
            logos = new List<Logo>(pngs.Length + jpgs.Length + jpegs.Length);
            for (int i = 0; i < pngs.Length; i++)
            {
                string[] st = pngs[i].Split('/', '\\');
                string ne = st[st.Length - 1];
                logos.Add(new Logo(ne.Substring(0, ne.Length - 4), LoadSprite(pngs[i])));
            }
            for (int i = 0; i < jpgs.Length; i++)
            {
                string[] st = jpgs[i].Split('/', '\\');
                string ne = st[st.Length - 1];
                logos.Add(new Logo(ne.Substring(0, ne.Length - 4), LoadSprite(jpgs[i])));
            }
            for (int i = 0; i < jpegs.Length; i++)
            {
                string[] st = jpegs[i].Split('/', '\\');
                string ne = st[st.Length - 1];
                logos.Add(new Logo(ne.Substring(0, ne.Length - 4), LoadSprite(jpegs[i])));
            }
        }
        if (!File.Exists(Application.persistentDataPath + "/Mods/tournaments.json"))
        {
            FileStream file = new FileStream(Application.persistentDataPath + "/Mods/tournaments.json", FileMode.Create);
            ModSave moSave = new ModSave();
            moSave.structure = new TournamentStructure[] 
            {
                new TournamentStructure
                {
                    afterEnd = "none",
                    afterGroup = new string[] { "none" },
                    dayLength = 0,
                    followersDistribution = new float[] { 0 },
                    groupCount = 0,
                    groupLength = 0,
                    groupMatchesLength = 0,
                    groupPositions = new Vector2[] { new Vector2(0, 0) },
                    majorPointsDistribution = new float[] { 0 },
                    matchSettings = new MatchSettingInfo[] { new MatchSettingInfo() },
                    pointsDistribution = new float[] { 0 },
                    prizePoolDistribution = new float[] { 0 },
                    teamPool = 0,
                    range = new int[] { 0 },
                    stage = "none"
                }
            };
            file.Close();
            string saveText = JsonUtility.ToJson(moSave);
            File.WriteAllText(Application.persistentDataPath + "/Mods/tournaments.json", saveText);
        }
        string aveText = File.ReadAllText(Application.persistentDataPath + "/Mods/tournaments.json");
        var dSave = JsonUtility.FromJson<ModSave>(aveText);
        if (SaveManager.SaveExistsSetting("stockplayer")) SaveManager.DeleteFileSetting("stockplayer");
        if (SaveManager.SaveExistsSetting("stockteam")) SaveManager.DeleteFileSetting("stockteam");
        SaveManager.SaveSetting(manager.GetPlayers().players, "stockplayer");
        SaveManager.SaveSetting(manager.GetTeams().teams, "stockteam");
        LoadTeamsAndPlayers();
        if (dSave.structure != null)
        {
            if (dSave.structure.Length > 0)
            {
                if (dSave.structure[0].stage == "none") return;
                else
                {
                    List<TournamentStructure> tournamentStructures = modSave.structure.ToList();
                    tournamentStructures.AddRange(dSave.structure);
                    modSave.structure = tournamentStructures.ToArray();
                }
            }
        }
        if (File.Exists(Application.persistentDataPath + "/Mods/nicknames.txt"))
        {
            string saveText = File.ReadAllText(Application.persistentDataPath + "/Mods/players.txt");
            string[] nicknames = saveText.Split('\n');
            PlayerNicknames.nickname = nicknames;
        }
    }
    
    public void LoadTeamsAndPlayers()
    {
        // now players
        string[] roles = new string[] { "Entry", "IGL", "Sniper", "Support", "Lurk" };
        if (File.Exists(Application.persistentDataPath + "/Mods/players.txt") &&
            File.Exists(Application.persistentDataPath + "/Mods/teams.txt"))
        {
            //players
            string saveText = File.ReadAllText(Application.persistentDataPath + "/Mods/players.txt");
            string[] players = saveText.Split('\n');
            List<Player> realPlayers = new List<Player>();
            for (int i = 0; i < players.Length; i++)
            {
                string[] stats = players[i].Split(", ");
                if (stats.Length != 6) continue;
                stats[5] = stats[5].Substring(0, stats[5].Length - 1);
                if (stats[5][stats[5].Length - 1] == ' ') stats[5].TrimEnd(' ');
                Player player = new Player();
                player.nickname = stats[0];
                player.activity = Manager.Activity.O_NOT_SET;
                player.stats = new List<int>();
                player.playerStats = new List<Stat>();
                for (int j = 1; j < 6; j++)
                {
                    if (stats[j].Length == 2)
                    {
                        int t;
                        if (System.Int32.TryParse(stats[j], out t))
                        {
                            if (t < 40) player.age = (byte)t;
                            else player.strength = (byte)t;
                            continue;
                        }
                        else
                        {
                            NationID[] nations = manager.GetNations();
                            bool s = false;
                            for (int z = 0; z < nations.Length; z++)
                            {
                                if (nations[z].name == stats[j])
                                {
                                    player.nationality = nations[z].nation;
                                    s = true;
                                    break;
                                }
                            }
                            if (s) continue;
                        }
                    }
                    int x = 0;
                    for (; x < 5; x++)
                    {
                        if (roles[x] == stats[j])
                        {
                            player.role = (Manager.Role)x;
                            break;
                        }
                    }
                    if (stats[j] == "Role")
                    {
                        player.role = Manager.Role.O_NOT_SET;
                        continue;
                    }
                    if (x != 5) continue;
                    player.teamName = stats[j];
                }
                realPlayers.Add(player);
            }
            manager.GetPlayers().players = realPlayers;
            //teams
            saveText = File.ReadAllText(Application.persistentDataPath + "/Mods/teams.txt");
            string[] teams = saveText.Split('\n');
            List<Team> realTeams = new List<Team>();
            for (int i = 0; i < teams.Length; i++)
            {
                string[] stats = teams[i].Split(", ");
                if (stats.Length < 4) continue;
                Team player = new Team();
                player.teamName = stats[0];
                player.academy = new Academy();
                player.currentTournament = -1;
                //player.taskSponsor = new TaskSponsor();
                for (int j = 1; j < stats.Length; j++)
                {
                    if (stats[j][0] == 'p')
                    {
                        int t;
                        if (System.Int32.TryParse(stats[j].Substring(1, stats[j].Length - 1), out t)) player.ReportTournament(new PointsEvent(2, (short)t, 0));
                        continue;
                    }
                    else if (stats[j][0] == 'f')
                    {
                        int t;
                        if (System.Int32.TryParse(stats[j].Substring(1, stats[j].Length - 1), out t)) player.followers = t;
                        continue;
                    }
                    else if (stats[j][0] == 'm')
                    {
                        int t;
                        if (System.Int32.TryParse(stats[j].Substring(1, stats[j].Length - 1), out t)) player.AddMoney(t);
                        continue;
                    }
                    else if (stats[j][0] == 'a')
                    {
                        int t;
                        if (System.Int32.TryParse(stats[j].Substring(1, stats[j].Length - 1), out t))
                            player.academy.SetLevel((byte)Mathf.Clamp(t, 1, 25));
                        continue;
                    }
                }
                realTeams.Add(player);
            }
            manager.GetTeams().teams = realTeams;
        }
        else
        {
            manager.GetPlayers().players = SaveManager.LoadSetting<List<Player>>("stockplayer");
            manager.GetTeams().teams = SaveManager.LoadSetting<List<Team>>("stockteam");
        }
    }

    [ContextMenu("Save")]
    void Save()
    {
        if (!Directory.Exists(Application.persistentDataPath + "/Mods"))
            Directory.CreateDirectory(Application.persistentDataPath + "/Mods");
        if (!File.Exists(Application.persistentDataPath + "/Mods/tournaments.json"))
        {
            FileStream file = new FileStream(Application.persistentDataPath + "/Mods/tournaments.json", FileMode.Create);
            file.Close();
        }
        string saveText = JsonUtility.ToJson(modSave);
        File.WriteAllText(Application.persistentDataPath + "/Mods/tournaments.json", saveText);
    }

    [ContextMenu("Load")]
    void Load()
    {
        string saveText = File.ReadAllText(Application.persistentDataPath + "/Mods/tournaments.json");
        modSave = JsonUtility.FromJson<ModSave>(saveText);
    }
}

[System.Serializable]
public struct ModSave
{
    public TournamentStructure[] structure;
}

[System.Serializable]
public struct TournamentStructure
{
    public string stage;
    public int dayLength;
    public int teamPool;
    public float[] prizePoolDistribution, pointsDistribution, majorPointsDistribution, followersDistribution;
    /// <summary>
    /// each team range in <0, range) in top that can participate
    /// range is set for tier 0 and 1, range x2, tier 3 is for all and is obsolete
    /// </summary>
    public int[] range;
    public Vector2[] groupPositions;
    public int groupCount;
    public int groupLength;
    public int groupMatchesLength;
    public string[] afterGroup;
    public string afterEnd;
    public MatchSettingInfo[] matchSettings;
}

[System.Serializable]
public struct ModLogic
{
    private void SetInt(string a, string b)
    {
        int aa = ints.FindIndex(x => x.name == a);
        ints[aa] = new TInt(ints[aa].name, ints[ints.FindIndex(x => x.name == b)].value);
    }

    private void SetInt(string a, int b)
    {
        int aa = ints.FindIndex(x => x.name == a);
        ints[aa] = new TInt(ints[aa].name, b);
    }

    private void AddInt(string a, string b)
    {
        int aa = ints.FindIndex(x => x.name == a);
        ints[aa] = new TInt(ints[aa].name, ints[aa].value + ints[ints.FindIndex(x => x.name == b)].value);
    }

    private void AddInt(string a, int b)
    {
        int aa = ints.FindIndex(x => x.name == a);
        ints[aa] = new TInt(ints[aa].name, ints[aa].value + b);
    }

    private List<TInt> ints;
    private List<TAction> func;

    public void Compile(string code)
    {
        //string space;
        List<System.Action<object, object, object, object>> actions;
        string currentAction;
        List<TArgs> args;
        string[] lines = code.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            lines[i].TrimEnd('\n');
            lines[i].TrimEnd('\r');
            lines[i].TrimStart('\t');
            string[] instructions = lines[i].Split(' ');
            for (int j = 0; j < instructions.Length; j++)
            {
                switch (instructions[j])
                {
                    //case "namespace":
                    //    space = instructions[++j];
                    //    break;
                    case "action":
                        currentAction = instructions[++j];
                        if (instructions[++j] == "params") { args = new List<TArgs>(); }
                        else { args = null; }
                        ++j;
                        for (int k = 0; k * 2 + j < instructions.Length; k++)
                        {
                            args.Add(new TArgs(instructions[k * 2 + j], instructions[k * 2 + j + 1]));
                        }
                        j += byte.MaxValue;
                        break;
                    case "begin":
                        actions = new List<System.Action<object, object, object, object>>();
                        ++i;
                        for (; i < lines.Length; i++)
                        {
                            lines[i].TrimEnd('\n');
                            lines[i].TrimEnd('\r');
                            lines[i].TrimStart('\t');
                            string[] instr = lines[i].Split(' ');
                            for (j = 0; j < instr.Length; j++)
                            {
                                //what
                            }
                        }
                        break;
                    case "end":

                        break;
                    case "global":

                        break;
                }
            }
        }
    }

    public System.Action<string> LibraryLookForString(string toFind)
    {
        switch (toFind)
        {
            case "error":
                System.Action<string> s = Manager.mainInstance.DebugError;
                return s;
        }
        return null;
    }
}

[System.Serializable]
public struct TInt
{
    public readonly string name;
    public int value;

    public TInt(string name, int value)
    {
        this.name = name;
        this.value = value;
    }
}

[System.Serializable]
public struct TAction
{
    public readonly string name;
    public List<TArgs> args;
    public List<System.Action<object, object, object, object>> actions;

    public TAction(string name, List<TArgs> args, List<System.Action<object, object, object, object>> actions)
    {
        this.name = name;
        this.args = args;
        this.actions = actions;
    }
}

[System.Serializable]
public struct TArgs
{
    public readonly string name, type;
    public object value;

    public TArgs(string name, string type, object value = null)
    {
        this.name = name;
        this.type = type;
        this.value = value;
    }
}