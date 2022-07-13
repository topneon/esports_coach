using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;
using System.Linq;
using Random = UnityEngine.Random;

public class Manager : MonoBehaviour
{
    public static Manager mainInstance;
    public PlayGames playGames;
    public GameObject matchGrid, singleColumn;
    public Transform evprGroup, evprBracket;
    private GameObject[] evBracketMatches, evGroupSingle;
    public enum Nation
    {
        O_NOT_SET = -1, Russian, French, Danish, Swedish, Serbian, Bosnian, Ukrainian, American, Canadian,
        Australian, German, Turkish, Brit, Finnish, Polish, Brazilian, Bulgarian, Latvian, Norwegian, Slovakian,
        Netherlandish, Lithuanian, Kazakh, Mexican, Estonian, Belarusian, Israelian, Montenegrish, African,
        Portugese, Spanish, Chinese, Czech, Indonesian, Romanian, Uzbek, Albanian, Irish, Luxembourgish,
        Uruguayish, Indian, Switzerlandish, Hungarian, Belgian
    }
    public enum Role { O_NOT_SET = -1, Entry, IGL, Sniper, Support, Lurk }
    public enum Activity { O_NOT_SET = -1, Agressive, Neutral, Passive }
    public enum MatchType { BO1, BO3, BO5 }
    public enum Map { O_NOT_SET = -1, Mirage, Dust, Inferno, Nuke, Overpass, Vertigo, Ancient }
    public enum Weapon
    {
        DefaultPistol = 0, Deagle = 700, MP9 = 1250, MAC10 = 1100, SSG = 1700, GALIL = 1800, AUG = 3300,
        SG = 3000, AK47 = 2700, M4A4 = 3100, M4A1S = 2900, AWP = 4750, AUTOSNIPER = 5000, XM = 2000,
        Famas = 2050, Molotov = 600, HEGrenade = 300
    }
    public static int influence = 25000, day = 1, tournamentList = 1, save = 1, stage = 0, sponsorKarma = 50,
        stagexp = 0, difficultyOverall = 200, eventPast = 0, describedPlayerPrice = 0, groupCounter = 1,
        eventViewer = -1;
    private static int adaptiveDifficulty = 200;
    //private byte[] winsloses = new byte[20];
    //private byte winslosesIndex = 0;
    public static JobHandle trackHandle = default;
    public byte showAds = 1;
    [SerializeField] private RectTransform saveWindow, saveBarProgress;
    [SerializeField] private Text saveProgress;
    [HideInInspector] public List<MapResult> mapResults, tempResults;
    [HideInInspector] public List<TournamentInfo> tourInfo;
    public Save saveState;

    [System.Serializable]
    public struct Save
    {
        public List<Player> players;
        public List<Team> teams;
        public List<MapResult> mapResults;
        public List<TournamentInfo> tourInfo;
        public List<byte> weaponOffsets;
        public Queue<EventRecord> eventRecords;
        public Bootcamp bootcamp;
        public AcademyInfo academy;
        public int day, stage, stagexp, sponsorKarma, difficultyOverall, influence, adaptiveDifficulty;
        public double buyPlayerMP;
        public string myteam;
    }

    [SerializeField] InputField[] edTeam, edPlayer;
    [SerializeField] Dropdown[] edPlayerD;
    [SerializeField] Image edImage;
    //[SerializeField] Text[] edTextTeam;

    private Team editorTeam;
    private Player editorPlayer;
    public void EditorTeam(string text)
    {
        editorTeam = EditorTeamFind(text);
        MenuEditor();
    }

    public void EditTeamTeamname(string text)
    {
        for (int i = 0; i < editorTeam.players.Count; i++)
        {
            editorTeam.players[i].teamName = text;
        }
        editorTeam.teamName = text;
        MenuEditor();
    }

    public void EditTeamMoney(string text)
    {
        int res;
        if (System.Int32.TryParse(text, out res))
        {
            editorTeam.ReduceMoney(editorTeam.GetMoney());
            editorTeam.AddMoney(res);
        }
        MenuEditor();
    }

    /*public void EditTeamPoints(string text)
    {
        if (editorTeam == null)
        {
            for (int i = 0; i < edTeam.Length; i++)
            {
                edTeam[i].SetTextWithoutNotify(string.Empty);
            }
            return;
        }
        short res;
        if (System.Int16.TryParse(text, out res))
            editorTeam.points = res;
    }*/

    public void EditTeamFollowers(string text)
    {
        int res;
        if (System.Int32.TryParse(text, out res))
            editorTeam.followers = res;
        MenuEditor();
    }

    private Team EditorTeamFind(string text)
    {
        List<Team> teamsList = GetTeams().teams;
        List<string> list = new List<string>(teamsList.Count);
        for (int i = 0; i < teamsList.Count; i++) list.Add(teamsList[i].teamName);
        for (byte i = 0; i < text.Length; i++)
        {
            char c1 = text[i], c2 = text[i];
            if (c1 <= 'Z' && c1 >= 'A') c2 += (char)32;
            if (c1 <= 'z' && c1 >= 'a') c2 -= (char)32;
            for (int j = 0; j < list.Count; j++)
            {
                if (list[j][i] != c1 && list[j][i] != c2)
                {
                    list.RemoveAt(j--);
                }
            }
        }
        if (list.Count == 0) return null;
        return GetTeams().GetTeam(list[0]);
    }

    public void EditorPlayer(string text)
    {
        editorPlayer = EditorPlayerFind(text);
        MenuEditor();
    }

    public void EditPlayerNickname(string text)
    {
        editorPlayer.nickname = text;
        MenuEditor();
    }

    public void EditPlayerStrength(string text)
    {
        int res;
        if (System.Int32.TryParse(text, out res))
        {
            editorPlayer.strength = (byte)Mathf.Clamp(res, 40, 99);
            editorPlayer.GeneratePlayer();
            MenuEditor();
        }
    }

    public void EditPlayerAge(string text)
    {
        int res;
        if (System.Int32.TryParse(text, out res))
            editorPlayer.age = (byte)Mathf.Clamp(res, 16, 36);
        MenuEditor();
    }
    public void EditPlayerRole(object o) { EditPlayerRole((int)o); }
    public void EditPlayerRole(int value)
    {
        editorPlayer.role += value;
        if (editorPlayer.role == (Role)(-1)) editorPlayer.role = Role.Lurk;
        if (editorPlayer.role == (Role)(5)) editorPlayer.role = 0;
         // -1 or +1
        editorPlayer.GeneratePlayer();
        MenuEditor();
    }
    byte ednation;
    public void EditPlayerNationality(object o) { EditPlayerNationality((int)o); }
    public void EditPlayerNationality(int value)
    {
        ednation += (byte)value; // -1 or +1
        if (ednation == nations.Length) ednation = 0;
        if (ednation == 255) ednation = (byte)(nations.Length - 1);
        //edImage.sprite = nations[ednation].sprite;
        editorPlayer.nationality = nations[ednation].nation;
        editorPlayer.GeneratePlayer();
        MenuEditor();
    }

    private Player EditorPlayerFind(string text)
    {
        List<Player> teamsList = GetPlayers().players;
        List<string> list = new List<string>(teamsList.Count);
        for (int i = 0; i < teamsList.Count; i++) list.Add(teamsList[i].nickname);
        for (byte i = 0; i < text.Length; i++)
        {
            char c1 = text[i], c2 = text[i];
            if (c1 <= 'Z' && c1 >= 'A') c2 += (char)32;
            if (c1 <= 'z' && c1 >= 'a') c2 -= (char)32;
            for (int j = 0; j < list.Count; j++)
            {
                if (list[j][i] != c1 && list[j][i] != c2)
                {
                    list.RemoveAt(j--);
                }
            }
        }
        if (list.Count == 0) return null;
        if (list.Count > 1)
        {
            for (int x = 0; x < list.Count; x++)
            {
                if (GetTeams().GetTeam(GetPlayers().GetPlayer(list[x]).teamName) == editorTeam)
                    return GetPlayers().GetPlayer(list[x]);
            }
        }
        return GetPlayers().GetPlayer(list[0]);
    }

    public void SetEventViewer(int what) { eventViewer = what; }

    public void BuyPlayerPrice(string text) { describedPlayerPrice = System.Int32.Parse(text); }

    Player describePlayer = null;

    public static byte[] arrOfRands = new byte[]
    {
        2, 1, 3, 4, 0,
        2, 3, 4, 1, 0,
        0, 1, 2, 3, 4,
        2, 0, 1, 3, 4,
        1, 4, 2, 3, 0,
        0, 2, 4, 3, 1,
        4, 1, 3, 2, 0,
        3, 2, 0, 4, 1,
        0, 1, 3, 2, 4,
        3, 2, 4, 1, 0,
        2, 1, 0, 3, 4,
        1, 2, 4, 3, 0,
        2, 0, 4, 3, 1,
        4, 1, 2, 3, 0,
        3, 0, 4, 1, 2,
        0, 1, 2, 4, 3,
        2, 0, 1, 4, 3,
        1, 0, 2, 3, 4,
        1, 4, 2, 0, 3,
        1, 0, 2, 4, 3,
        4, 2, 0, 3, 1,
        3, 0, 2, 1, 4,
        0, 1, 4, 3, 2,
        2, 1, 4, 3, 0,
        3, 2, 1, 0, 4,
        0, 4, 2, 3, 1,
        0, 3, 2, 1, 4,
        1, 0, 3, 2, 4,
        4, 2, 3, 1, 0,
        4, 0, 1, 3, 2,
        0, 2, 1, 4, 3,
        2, 4, 3, 1, 0,
        1, 3, 2, 0, 4,
        0, 4, 1, 3, 2,
        4, 0, 1, 2, 3,
        0, 4, 2, 1, 3,
        3, 0, 2, 4, 1,
        3, 4, 2, 0, 1,
        0, 2, 3, 1, 4,
        1, 0, 4, 3, 2,
        2, 0, 4, 1, 3,
        1, 4, 0, 3, 2,
        4, 0, 2, 1, 3,
        3, 4, 0, 1, 2,
        1, 2, 0, 4, 3,
        4, 3, 2, 1, 0,
        1, 2, 3, 0, 4,
        2, 4, 1, 0, 3,
        3, 4, 2, 1, 0,
        4, 1, 0, 3, 2,
        3, 0, 1, 2, 4,
        4, 0, 2, 3, 1,
        2, 4, 0, 3, 1,
        3, 1, 2, 0, 4,
        4, 2, 1, 0, 3
    };

    public Queue<EventRecord> eventRecords;
    [SerializeField] private Text[] boardText;
    [SerializeField] private Text[] gsTeam, gsTitle;
    public void GrandSlam(EventRecord r)
    {
        if (eventRecords == null) eventRecords = new Queue<EventRecord>(8);
        if (eventRecords.Count == 8) eventRecords.Dequeue();
        eventRecords.Enqueue(r);
    }
    public bool GrandSlamCheck()
    {
        if (eventRecords == null) return false;
        byte j = 0;
        foreach (var i in eventRecords)
        {
            if (i.team == myTeam.teamName) j++;
        }
        if (j >= 5) return true;
        return false;
    }
    // thats the guy who started premium
    public void CanadianGamer(string pass)
    {
        if (pass == "DEBUG1510")
        {
            ShowPopUp("To get help you should go to our discord.");
            //DisableAds();
        }
        if (pass == "T3REX")
        {
            ShowPopUp("Looks like you know the code. Start new game for the feature to work.");
            DisableAds();
        }
    }

    public void DebugError(string text)
    {
        debugText.text = text + "\n" + "Day: " + day.ToString() + ", Current Menu: " + currentMenu + "\n";
        if (myTeam != null)
            if (myTeam.currentTournament != -1)
                if (Events.events.GetTournaments().Count > myTeam.currentTournament)
                    debugText.text += "Latest event: " + Events.events.GetTournaments()[myTeam.currentTournament].title + "\n" +
                        "Event type: " + Events.events.GetTournaments()[myTeam.currentTournament].btype.ToString() + "\n";
        if (match != null) debugText.text += "Match bugged out: " + match.team1.teamName + " vs " + match.team2.teamName;
        debugPopup.SetActive(true);
    }

    public void DisableAds()
    {
        showAds = 202;
        SaveManager.SaveSetting(showAds, "firsthelp");
    }
    public int AdaptiveDifficulty() { return adaptiveDifficulty; }
    public void AdaptiveDifficultyChange(byte win)
    {
        if (win > 1) return;
        if (win == 1) adaptiveDifficulty += 15;
        else if (win == 0) adaptiveDifficulty -= 15;
        if (adaptiveDifficulty > difficultyOverall + 150) adaptiveDifficulty = difficultyOverall + 150;
        if (adaptiveDifficulty < difficultyOverall - 150) adaptiveDifficulty = difficultyOverall - 150;
    }
    [SerializeField] private Teams definedTeams; public Teams GetTeams() { return definedTeams; }
    [SerializeField] private Players definedPlayers; public Players GetPlayers() { return definedPlayers; }
    [SerializeField] private Bootcamps definedBootcamps; public Bootcamps GetBootcamps() { return definedBootcamps; }

    [SerializeField] private GameObject[] kills, mtHelmet, mtKevlar;
    [SerializeField] private Text[] player1s, player2s;
    [SerializeField] private Image[] sprites, players;
    [SerializeField] private WeaponID[] weapons;
    public Match match;
    [SerializeField] private Text[] kd, balance, nicknames;
    [SerializeField] private Image[] boughtDevice;
    [SerializeField] private Text teamname1, teamname2, rounds1, rounds2, roundNumber, alive1, alive2;
    [SerializeField] private GameObject emptyPrefab;
    [SerializeField] private Positions[] positionsTT, positionsCT, positionsANY;
    [SerializeField] private GameObject ctDown, ttDown;
    [SerializeField] private GameObject[] killsRadar = new GameObject[9];
    [SerializeField] private GameObject[] mapBackground, mapRadars;

    private byte u1 = 0, u2 = 0; //desc because name is stupid, counter for player preview on pickbans
    [SerializeField] private NationID[] nations; public NationID[] GetNations() { return nations; }
    [SerializeField] private RoleID[] roles;
    [SerializeField] private TournamentID[] tournamentIDs;

    [SerializeField] private GameObject matchUI, pickbansUI, menuUI;

    private Team myTeam = null;
    public Team GetMyTeam() { return myTeam; }

    //[SerializeField] private Camera mainCamera;
    [SerializeField] private Text popupText, popupAskText;
    [SerializeField] private RectTransform popupWindow, popupWindowAc, popupWindowAsk, dpBuy;
    [SerializeField] private Image loadingOverlay, stTS, stPS, dpFlag;

    [SerializeField] private TranslateObject translateObject;
    [SerializeField] private AudioClip switchClip;
    private Bootcamp myBootcamp; public Bootcamp GetMyBootcamp() { return myBootcamp; }
    public string GetTranslate(int indexer)
    { return translateObject.defaultTexts[(indexer * 5) + (int)TranslateObject.language]; }

    private void SetDifficulty(object o) { SetDifficulty((int)o); }
    public void SetDifficulty(int diff)
    {
        difficultyOverall = diff;
        adaptiveDifficulty = diff;
        if (showAds == 202) MenuChoosePro();
        else MenuChooseNotPro();
    }

    public void RentBootcamp()
    {
        if (definedBootcamps.bootcamps[rentb].pricePerMonth > myTeam.GetMoney())
        {
            string[] table = new string[]
            {
                "Not enough money",
                "Не хватает денег",
                "Nicht genug Geld",
                "Dinheiro insuficiente",
                "Pas assez d'argent"
            };
            ShowPopUp(table[(int)TranslateObject.language]); return;
        }
        if (myBootcamp == null)
        {
            myBootcamp = definedBootcamps.bootcamps[rentb];
            myBootcamp.scout = new Scout(string.Empty, 0);
            myBootcamp.psychologist = new Psychologist(string.Empty, 0, 0);
            myBootcamp.assistantCoach = new AssistantCoach(string.Empty, 0, 0);
            myTeam.ReduceMoney(myBootcamp.pricePerMonth);
            myTeam.AddMapPoints((byte)(myBootcamp.mapPoints * 5 - 7));
            playGames.UnlockAchievement(7);
        }
        else
        {
            Scout scout = myBootcamp.scout;
            Psychologist ps = myBootcamp.psychologist;
            AssistantCoach ac = myBootcamp.assistantCoach;
            myBootcamp.scout = scout;
            myBootcamp.psychologist = ps;
            myBootcamp.assistantCoach = ac;
            myBootcamp = definedBootcamps.bootcamps[rentb];
            myTeam.ReduceMoney(myBootcamp.pricePerMonth);
            byte used = myTeam.GetMapPoints();
            for (byte i = 0; i < 7; i++) used += myTeam.GetMap((Map)i);
            int e = ((myBootcamp.assistantCoach.formAdd + myBootcamp.formAdd * 5) - used);
            if (e > 0) myTeam.AddMapPoints((byte)e);
        }
        payBootDay = (byte)(day % 30);
        //PopDownDesc();
        MenuBootcamp();
    }

    int rentb;
    public void ChooseBootcamp(object o) { ChooseBootcamp((int)o); }
    public void ChooseBootcamp(int i)
    {
        rentb = i; RentBootcamp();
        /*
        string[] table = new string[]
        {
            "City: ",
            "Город: ",
            "Stadt: ",
            "Cidade: ",
            "Ville: "
        };
        bcText[0].text = table[(int)TranslateObject.language] + definedBootcamps.bootcamps[i].city;
        table = new string[]
        {
            "Comfort: ",
            "Комфорт: ",
            "Kompfort: ",
            "Conforto: ",
            "Confort: "
        };
        bcText[1].text = table[(int)TranslateObject.language] +
            (definedBootcamps.bootcamps[i].energyAdd +
            definedBootcamps.bootcamps[i].chemistryAdd +
            definedBootcamps.bootcamps[i].mapPoints +
            definedBootcamps.bootcamps[i].formAdd).ToString() + "/80";
        table = new string[]
        {
            "Rent: ",
            "Аренда: ",
            "Miete: ",
            "Renda: ",
            "Loyer: "
        };
        bcText[2].text = table[(int)TranslateObject.language] +
            definedBootcamps.bootcamps[i].pricePerMonth.ToString() + "$";*/
    }

    private RectTransform animatedRect;

    [SerializeField] private RectTransform describeTransform, describeTTransform;
    Team describeTeam = null;
    public void PlayerDescriber(Text text)
    {
        //PlayerDescriber(text.text);
    }
    public void DemandPD() { demandPD = true; }
    private bool demandPD = false;
    /*public void PlayerDescriber(string text)
    {
        if (sttp == 0 && !demandPD)
        {
            describeTeam = definedTeams.GetTeam(text);
            DescribeTeamUpdate();
            PopUpDesc(describeTTransform);
            describeTransform.localScale = new Vector3(0, 0, 0);
        }
        else
        {
            describePlayer = definedPlayers.GetPlayer(text);
            DescribePlayerUpdate();
            PopUpDesc(describeTransform);
            describeTTransform.localScale = new Vector3(0, 0, 0);
        }
        demandPD = false;
    }*/

    public void PopUpDesc(RectTransform rt) { animatedRect = rt; StartCoroutine(PopUpDescEnum()); }
    public void PopDownDesc() { StartCoroutine(PopDownDescEnum()); }
    private IEnumerator PopUpDescEnum()
    {
        yield return new WaitWhile(() => transition);
        transition = true;
        while (animatedRect.localScale.x < 1.0f)
        {
            animatedRect.localScale += new Vector3(frameTime * 4, frameTime * 4);
            yield return new WaitForSeconds(frameTime);
        }
        transition = false;
    }
    private IEnumerator PopDownDescEnum()
    {
        yield return new WaitWhile(() => transition);
        transition = true;
        while (animatedRect.localScale.x > 0.0f)
        {
            animatedRect.localScale -= new Vector3(frameTime * 4, frameTime * 4);
            yield return new WaitForSeconds(frameTime);
        }
        transition = false;
    }


    public static List<byte> weaponOffsets;

    private void Awake()
    {
        mainInstance = this;
        tempResults = new List<MapResult>(16);
        saveState = new Save();
        mapResults = new List<MapResult>();
        eventRecords = new Queue<EventRecord>(8);
        weaponOffsets = new List<byte>(15);
        for (byte i = 0; i < 15; i++) weaponOffsets.Add((byte)Random.Range(0, 16));
        //welcomeUI.SetActive(true);
        investors = new List<InvestorUnit>();
        sponsors = new List<SponsorUnit>();
        tsetting.role = roles[5].sprite;
        tsetting.flag = euSprite;
        string[] table = new string[] { "ANY", "ЛЮБОЙ", "ALLE", "ALGUM", "TOUT" };
        tsetting.age = table[(int)TranslateObject.language];
        tsetting.level = table[(int)TranslateObject.language];
        showMatch = new ShowMatch();
        showMatch.Init();
    }
    public void HyperLinkURL(object o) { HyperLinkURL((string)o); }
    public void HyperLinkURL(string link) { Application.OpenURL(link); }
    /*
    IEnumerator CheckNetwork()
    {
        while (showAds != 202)
        {
            StartCoroutine(GetNetRequest("https://www.google.com"));
            yield return new WaitForSeconds(1.0f);
        }
    }

    IEnumerator GetNetRequest(string uri)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
        {
            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            string[] pages = uri.Split('/');
            int page = pages.Length - 1;
            if (webRequest.result != UnityWebRequest.Result.Success &&
                webRequest.result != UnityWebRequest.Result.InProgress)
            {
                
            }
        }
    }*/
    /*
    public List<string> GetTheFuckingInfo(string webData, string match = "href=\"/player/", char endmatching = '\"')
    {
        int a = 0;
        int size = webData.Length;
        int target = match.Length;
        List<string> result = new List<string>(150);
        for (int i = 0; i < size; i++)
        {
            if (match[a] != webData[i])
            {
                a = 0;
                continue;
            }
            else
            {
                //if (a == 0) indexer = i;
                ++a;
                if (a == target)
                {
                    ++i;
                    int from = i;
                    a = 0;
                    for (; webData[i] != endmatching; i++)
                    {
                        ++a;
                    }
                    result.Add(webData.Substring(from, a));
                    a = 0;
                    ++i;
                    continue;
                }
            }
        }
        return result;
    }

    public string GetTheFuckingInfoStop(string webData, string match = "href=\"/player/", char endmatching = '\"')
    {
        int a = 0;
        int size = webData.Length;
        int target = match.Length;
        //List<string> result = new List<string>(150);
        for (int i = 0; i < size; i++)
        {
            if (match[a] != webData[i])
            {
                a = 0;
                continue;
            }
            else
            {
                //if (a == 0) indexer = i;
                ++a;
                if (a == target)
                {
                    ++i;
                    int from = i;
                    a = 0;
                    for (; webData[i] != endmatching; i++)
                    {
                        ++a;
                    }
                    return webData.Substring(from, a);
                    a = 0;
                    ++i;
                    continue;
                }
            }
        }
        return string.Empty;
    }

    public string GetTheFuckingInfoYear(string webData)
    {
        int a = 0;
        string match = "** years";
        int indexer = 0;
        int size = webData.Length;
        const int offset = 2;
        int target = match.Length - 2;
        //List<string> result = new List<string>(150);
        for (int i = 0; i < size; i++)
        {
            if (match[a + offset] != webData[i])
            {
                a = 0;
                continue;
            }
            else
            {
                if (a == 0) indexer = i;
                ++a;
                if (a == target)
                {
                    indexer -= 2;
                    return webData.Substring(indexer, offset);
                }
            }
        }
        return string.Empty;
    }

    private struct HLTVPlayer
    {
        public string link, team, nickname;
        public int teamposition;
        public double rating;
    }

    private struct HLTVTeam
    {
        public string link, name;
        public int position;
    }
    public PlayerNicknames pnicknames;
    private IEnumerator FullfillPlayerNicknames()
    {
        const string lookfor = "class=\"summaryNickname text-ellipsis\">";
        int a = 1111;
        string nickname, str, webData;
        for (; a < 1611; a++)
        {
            yield return new WaitForSeconds(Random.Range(0.5f, 1.0f));
            System.Net.WebClient wc = new System.Net.WebClient();
            str = $"https://www.hltv.org/stats/players/{a}/a";
            try
            {
                webData = wc.DownloadString(str);
                nickname = GetTheFuckingInfoStop(webData, lookfor, '<');
                Debug.Log(nickname);
                PlayerNicknames.Add(nickname);
            }
            catch (System.Net.WebException e) { webData = string.Empty; }
        }
        pnicknames.End();
        SaveManager.Save(pnicknames._nicknames, "sosuvoliku");
    }

    public IEnumerator GetAllTheInfo()
    {
        List<HLTVPlayer> hltvPlayers = new List<HLTVPlayer>(320);
        List<HLTVTeam> hltvTeams = new List<HLTVTeam>(64);
        int[] lobby = new int[]
        {
            // NA
            //10948, // REST
            8038, 9799, 11075, 10258, 11298, 11358, 5973, 5005, // USA (NO LIQUID)
            // SA
            9996, // REST
            10786, // ARGENTINA
            8297, 6902, 4773, 6947, 9215, 7653, 9455, 11309, 11420, 11588, 11561, 11555, 11249, // BRAZIL
            // EU
            6667, 4869, 10503, 4494, 10697, 11251, 5347, 5995, 11518, // REST
            10386, 9863, // BULGARIA
            10577, 5412, 7441, 11147, // CZECH REPUBLIC
            7175, 6665, 7461, 11419, 4602, 10867, 8704, 11414, 10970, // DENMARK
            10885, // ESTONIA
            10116, 9648, 7434, // FINLAND
            11501, 4674, 9565, // FRANCE (NO VITALITY NO SHOX)
            7532, 10968, 11282, 5540, 11375, 9789, // GERMANY
            11243, // NORWAY,
            8068, 10426, 8248, 10737, 11387, 7681, 10973, // POLAND
            10567, 8515, 7379, // PORTUGAL
            10993, 7187, // ROMANIA
            7718, 10978, // SPAIN
            4411, 9928, 5422, 11401, 10960, 9375, 11321, // SWEDEN
            4991, 11278, // UK
            // CIS
            4608, 6651, 5378, 10831, 8135, 7244, 10621, 7969, 9287, 11292,
            11113, 11219, 10888, 11385, 5310, 6978, 11241, 11342, 5752, 11595, 7020,
            // ASIA
            4863, 8840, 7606, 10796, 8607, 11585,
            // OCE
            11300, 8668, 10672, 7983, 10711, 11193
        };
        for (int i = 0; i < lobby.Length; i++)
        {
            yield return new WaitForSeconds(Random.Range(0.5f, 1.0f));
            System.Net.WebClient wc = new System.Net.WebClient();
            Debug.Log("https://www.hltv.org/team/" + lobby[i] + "/a");
            string webData = wc.DownloadString("https://www.hltv.org/team/" + lobby[i] + "/a");
            //is the team full
            string check = GetTheFuckingInfoStop(webData, "class=\"text-ellipsis bold\">?", '/');
            if (check != string.Empty) continue;
            //top150 check
            string position = GetTheFuckingInfoStop(webData, "\">#", '<');
            int t;
            if (position == string.Empty) t = 10;//continue;
            else t = System.Int32.Parse(position);
            //coach check
            //int t = 6;
            //string coach = GetTheFuckingInfoStop(webData, "<b>", '<');
            //if (coach != "Coach") t = 5;
            List<string> party = GetTheFuckingInfo(webData);
            string teamname = GetTheFuckingInfoStop(webData, "h1 class=\"profile-team-name text-ellipsis\">", '<');
            for (int j = 0; j < 5; j++)
            {
                HLTVPlayer player = new HLTVPlayer
                {
                    link = "https://www.hltv.org/player/" + party[j],
                    team = teamname,
                    teamposition = t
                };
                hltvPlayers.Add(player);
            }
            HLTVTeam team = new HLTVTeam
            {
                link = webData,
                name = teamname,
                position = t
            };
            hltvTeams.Add(team);
        }
        List<Player> players = new List<Player>(160);
        for (int i = 0; i < hltvPlayers.Count; i++)
        {
            yield return new WaitForSeconds(Random.Range(0.5f, 1.0f));
            System.Net.WebClient wc = new System.Net.WebClient();
            //Debug.Log(hltvPlayers[i].link);
            string webData = wc.DownloadString(hltvPlayers[i].link);
            string nickname = GetTheFuckingInfoStop(webData, "class=\"playerNickname\" itemprop=\"alternateName\">", '<');
            if (nickname == string.Empty) nickname =
                    GetTheFuckingInfoStop(webData, "class=\"player-nick text-ellipsis\" itemprop=\"alternateName\">", '<');
            string nation = GetTheFuckingInfoStop(webData, "src=\"/img/static/flags/30x20/", '.');
            Nation nationality = Nation.Brit;
            for (int j = 0; j < nations.Length; j++)
                if (nations[j].name == nation)
                { nationality = nations[j].nation; break; }
            string ages = GetTheFuckingInfoYear(webData);
            string ratings = GetTheFuckingInfoStop(webData, "<span class=\"statsVal\">", '<');
            if (ratings == string.Empty) ratings = "1.00";
            Debug.Log(hltvPlayers[i].link);
            double rating = System.Double.Parse(ratings);
            byte age;
            if (ages != string.Empty) age = (byte)System.Int32.Parse(ages);
            else age = (byte)Random.Range(18, 25);
            Player player = new Player();
            player.nickname = nickname;
            player.nationality = nationality;
            player.role = Role.O_NOT_SET;
            player.age = age;
            player.activity = Activity.O_NOT_SET;
            player.teamName = hltvPlayers[i].team;
            int spot = hltvPlayers[i].teamposition;
            if (spot > 60) spot = 60;
            if (rating > 1.25) rating = 1.25;
            player.strength = (byte)Mathf.CeilToInt((float)(0.25 * spot + (117 - spot) * 0.6825 * rating));
            players.Add(player);
        }
        players = players.OrderByDescending(x => x.strength).ToList();
        Player pl = new Player();
        pl.nickname = "0";
        pl.nationality = Nation.Russian;
        pl.language = Nation.Russian;
        pl.role = Role.Entry;
        pl.age = 0;
        pl.activity = Activity.Agressive;
        pl.teamName = string.Empty;
        players.Insert(0, pl);
        List<Team> teams = new List<Team>(32);
        for (int i = 0; i < hltvTeams.Count; i++)
        {
            Team team = new Team();
            team.academy = new Academy();
            //team.taskSponsor = new TaskSponsor();
            team.teamName = hltvTeams[i].name;
            team.AddMoney(5000000 - (hltvTeams[i].position * 100000));
            if (team.GetMoney() < 100000) team.AddMoney(100000);
            team.ReportTournament(new PointsEvent(2, (short)Mathf.RoundToInt(1000.0f / (i + 1)), 0));
            teams.Add(team);
        }
        teams = teams.OrderByDescending(x => x.GetACHPoints()).ToList();
        definedPlayers.players = players;
        definedTeams.teams = teams;
        gettingati = false;
    }
    bool gettingati = true;

    [ContextMenu("click")]
    public void LoadFromDisk()
    {
        definedTeams.teams = SaveManager.Load<List<Team>>("TLIST");
        definedPlayers.players = SaveManager.Load<List<Player>>("PLIST");
    }
    [ContextMenu("me")]
    public void LoadFromDiskD()
    {
        pnicknames._nicknames = SaveManager.Load<List<string>>("sosuvoliku");
    }*/

    private void Start()
    {
        //StartCoroutine(FullfillPlayerNicknames());
        /*gettingati = true;
        StartCoroutine(GetAllTheInfo());
        yield return new WaitWhile(() => gettingati);
        SaveManager.Save(definedPlayers.players, "PLIST");
        SaveManager.Save(definedTeams.teams, "TLIST");*/
        /*if (SaveManager.SaveExistsSetting("firsthelp")) showAds = SaveManager.LoadSetting<byte>("firsthelp");
        if (SaveManager.SaveExistsSetting("resu"))
        {
            rateSetting = (byte)SaveManager.LoadSetting<byte>("refresher");
            FrameLangugage();
            skipResults = (byte)SaveManager.LoadSetting<byte>("resu");
            ResultsLangugage();
            TranslateObject.language = (TranslateObject.Language)SaveManager.LoadSetting<int>("lang");
            UpdateLangugage();
            audioSource.volume = (float)(SaveManager.LoadSetting<int>("volume") * 0.01f);
            volumeSlider.value = (float)(SaveManager.LoadSetting<int>("volume") * 0.01f);
        }
        Vector2 vector2 = new Vector2(0, 10);
        for (byte i = 0; i < 60; i++)
        {
            animBarStart[0].anchoredPosition -= vector2;
            animBarStart[1].anchoredPosition -= vector2;
            yield return new WaitForSeconds(0.01667f);
        }
        for (byte i = 0; i <= 30; i++)
        {
            animImage[0].color = new Color(0, 0, 0, (30 - i) * 0.03333f);
            animText[0].color = new Color(0, 0, 0, (30 - i) * 0.03333f);
            animText[1].color = new Color(0, 0, 0, (30 - i) * 0.03333f);
            yield return new WaitForSeconds(0.01667f);
        }
        animImage[0].gameObject.SetActive(false);
        animText[0].gameObject.SetActive(false);
        animText[1].gameObject.SetActive(false);*/
        SaveManager.DirectoryExistsOrCreate();
        //SaveManager.DeleteAll(); PlayerPrefs.DeleteAll();
        //throw new System.Exception();
        if (!MatchSave.HasKey("saver7_1"))
        {
            if (SaveManager.SaveExistsSetting("firsthelp")) showAds = SaveManager.LoadSetting<byte>("firsthelp");
            byte s = showAds;
            SaveManager.save = "save1";
            SaveManager.DeleteAll(); PlayerPrefs.DeleteAll();
            SaveManager.save = "save2";
            SaveManager.DeleteAll(); PlayerPrefs.DeleteAll();
            SaveManager.save = "save3";
            SaveManager.DeleteAll(); PlayerPrefs.DeleteAll();
            showAds = s;
            SaveManager.SaveSetting(s, "firsthelp");
            MatchSave.SetInt("saver7_1", 1);
            PlayerPrefs.Save();
            switch (Application.systemLanguage)
            {
                case SystemLanguage.English:
                    TranslateObject.language = TranslateObject.Language.English;
                    break;
                case SystemLanguage.Russian:
                    TranslateObject.language = TranslateObject.Language.Russian;
                    break;
                case SystemLanguage.German:
                    TranslateObject.language = TranslateObject.Language.German;
                    break;
                case SystemLanguage.Portuguese:
                    TranslateObject.language = TranslateObject.Language.Portuguese;
                    break;
                case SystemLanguage.French:
                    TranslateObject.language = TranslateObject.Language.French;
                    break;
                default:
                    TranslateObject.language = TranslateObject.Language.English;
                    break;
            }
            int i = Screen.currentResolution.refreshRate;
            if (i > 64) rateSetting = 0;
            else rateSetting = 1;
            ResultsLangugage();
            FrameLangugage(false);
            UpdateLangugage();
        }
        if (SaveManager.SaveExistsSetting("firsthelp")) showAds = SaveManager.LoadSetting<byte>("firsthelp");
        SaveManager.SaveSetting(showAds, "firsthelp");
        //Debug.Log(Application.targetFrameRate.ToString());
        //mainInstance = this;
        //mm = MovingMenu();
        //StartCoroutine(mm);
        //StartCoroutine(PopUpCloseSim());
        //yield return new WaitForSeconds(0.25f);
        /*LoadGame();
        yield return new WaitWhile(() => loading);
        League.GeneratePlayers();
        definedTeams.UpdateStats();*/
        //for (int i = 0; i < 25; i++) inventory.AddPerk(definedPerks.perks[i & 7]);
        //SwitchMenu("perks");
        MenuWelcome();
    }

    public void NewGame()
    {
        StartCoroutine(NewGameEnum());
    }
    bool dontsaveprogress = false;
    public IEnumerator NewGameEnum()
    {
        byte s = showAds;
        string[] table = new string[]
        {
            "Game will be restarted to reset the progress",
            "Игра будет перезапущенна для того чтобы обнулить прогресс",
            "Das Spiel wird neu gestartet, um den Fortschritt zurückzusetzen",
            "O jogo será reiniciado para reiniciar o progresso",
            "Le jeu doit etre relancé pour recommencer une partie"
        };
        ShowPopUp(table[(int)TranslateObject.language]);
        definedTeams.StopAllCoroutines();
        SaveManager.DeleteAll();
        dontsaveprogress = true;
        showAds = s;
        SaveManager.SaveSetting(showAds, "firsthelp");
        yield return new WaitForSeconds(2.0f); Application.Quit();
    }

    public void DeleteGame(object o) { DeleteGame((int)o); }

    public void DeleteGame(int i)
    {
        SaveManager.save = "save" + i.ToString();
        popupQuest = DeleteGame;
        string[] table = new string[] { "Are you sure?", "Вы уверенны?", "Bist du sicher?", "Tem certeza?", "Etes-vous sure?" };
        ShowPopUpAsk(table[(int)TranslateObject.language], false);
    }

    private byte DeleteGame(bool b)
    {
        if (!b) return 0;
        SaveManager.DeleteAll();
        MenuChooseSave(); // ?
        return 0;
    }

    //private void OnApplicationQuit() { if (!dontsaveprogress) SaveGame();
    int saveCounter = 0;
    bool saving = false;
    private void QuickSave()
    {
        MatchSave.SaveTournaments();
        //saveState
        saveState.academy = myTeam.academy.Save();
        saveState.adaptiveDifficulty = adaptiveDifficulty;
        saveState.bootcamp = myBootcamp;
        saveState.day = day;
        saveState.eventRecords = eventRecords;
        saveState.influence = influence;
        saveState.mapResults = mapResults;
        saveState.players = definedPlayers.players;
        saveState.teams = definedTeams.teams;
        saveState.tourInfo = tourInfo;
        if (saveCounter++ == 0)
        {
            saveState.buyPlayerMP = buyPlayerMP;
            saveState.difficultyOverall = difficultyOverall;
            saveState.myteam = myTeam.teamName;
            saveState.stagexp = stagexp;
            saveState.stage = stage;
            saveState.sponsorKarma = sponsorKarma;
            saveState.weaponOffsets = weaponOffsets;
        }
        SaveManager.Save(saveState, "save");
    }
    private void SaveGame()
    {
        saving = true;
        //[SerializeField] private RectTransform saveWindow, saveBarProgress;
        //[SerializeField] private Text saveProgress;
        //saveBarProgress.anchoredPosition = new Vector2(0, 0);
        //saveProgress.text = "1%";
        if (SaveManager.SaveExistsSetting("firsthelp")) showAds = SaveManager.LoadSetting<byte>("firsthelp");
        SaveManager.SaveSetting(showAds, "firsthelp");
        playGames.AddScoreToLeaderboard(stagexp, 0);
        SaveManager.SaveSetting(rateSetting, "refresher");
        SaveManager.SaveSetting(skipResults, "resu");
        SaveManager.Save(myTeam.teamName, "myteam");
        SaveManager.SaveSetting((int)TranslateObject.language, "lang");
        SaveManager.SaveSetting((int)(audioSource.volume * 100.0f), "volume");
        //saveBarProgress.anchoredPosition = new Vector2(14, 0);
        saveProgress.text = "2%";
        //MatchSave.RewritePlayerData();
        saveBarProgress.anchoredPosition = new Vector2(21, 0);
        saveProgress.text = "3%";
        saveBarProgress.anchoredPosition = new Vector2(371, 0);
        saveProgress.text = "53%";
        saveBarProgress.anchoredPosition = new Vector2(441, 0);
        saveProgress.text = "63%";
        MatchSave.SaveTournaments(); // tourinfo updates
        saveBarProgress.anchoredPosition = new Vector2(693, 0);
        saveProgress.text = "99%";
        //myTeam.academy.Save();
        PlayerPrefs.Save();
        saveState = new Save
        {
            academy = myTeam.academy.Save(),
            adaptiveDifficulty = adaptiveDifficulty,
            bootcamp = myBootcamp,
            buyPlayerMP = buyPlayerMP,
            day = day,
            difficultyOverall = difficultyOverall,
            eventRecords = eventRecords,
            influence = influence,
            mapResults = mapResults,
            myteam = myTeam.teamName,
            players = definedPlayers.players,
            sponsorKarma = sponsorKarma,
            stage = stage,
            stagexp = stagexp,
            teams = definedTeams.teams,
            tourInfo = tourInfo,
            weaponOffsets = weaponOffsets
        };
        SaveManager.Save(saveState, "save");
        saveBarProgress.anchoredPosition = new Vector2(700, 0);
        saveProgress.text = "100%";
        saving = false;
        /*try
        {
            
        }
        catch (System.Exception e)
        {
            DebugError("ERROR: " + e.Message + "\n" + e.StackTrace);
            dontsaveprogress = true;
        }*/
    }

    bool loading = false;
    private void LoadGame(object o) { LoadGame((int)o); }
    public void LoadGame(int value)
    {
        loading = true;
        save = value;
        SaveManager.save = "save" + value.ToString();
        if (SaveManager.SaveExistsSetting("firsthelp")) showAds = SaveManager.LoadSetting<byte>("firsthelp");
        SaveManager.SaveSetting(showAds, "firsthelp");
        if (SaveManager.SaveExistsSetting("resu"))
        {
            rateSetting = (byte)SaveManager.LoadSetting<byte>("refresher");
            FrameLangugage();
            skipResults = (byte)SaveManager.LoadSetting<byte>("resu");
            ResultsLangugage();
            TranslateObject.language = (TranslateObject.Language)SaveManager.LoadSetting<int>("lang");
            UpdateLangugage();
            audioSource.volume = (float)(SaveManager.LoadSetting<int>("volume") * 0.01f);
            volumeSlider.value = (float)(SaveManager.LoadSetting<int>("volume") * 0.01f);
        }
        if (!SaveManager.SaveExists("save"))
        {
            Modding.modding.LoadTeamsAndPlayers();
            influence = 25000; day = 1; tournamentList = 1; save = 1; stage = 0; sponsorKarma = 50;
            stagexp = 0; difficultyOverall = 200; eventPast = 0; describedPlayerPrice = 0; groupCounter = 16384;
            eventViewer = -1;
            buyPlayerMP = 1.0f;
            myBootcamp = null;
            tourInfo = new List<TournamentInfo>();
            tempResults = new List<MapResult>(16);
            saveState = new Save();
            mapResults = new List<MapResult>();
            eventRecords = new Queue<EventRecord>(8);
            weaponOffsets = new List<byte>(15);
            for (byte i = 0; i < 15; i++) weaponOffsets.Add((byte)Random.Range(0, 16));
            definedTeams.AbsoluteStart();
            definedTeams.GeneratePlayers();
            League.GeneratePlayers();
            definedTeams.UpdateStats();
            Events.events.GenerateTournaments();
            for (short i = 0; i < definedTeams.teams.Count; i++)
            {
                if (definedTeams.teams[i].GetMoney() == 0)
                    definedTeams.teams[i].BetWin(Mathf.Clamp(5100000 - 100000 * definedTeams.
                        GetTeamPlacement(definedTeams.teams[i].teamName), 100000, 5000000));
                definedTeams.teams[i].followers = (int)(definedTeams.teams[i].GetMoney() * 0.4f);
            }
            SaveManager.Save<int>(0, "index");
            MenuChooseDifficulty();
            loading = false;
            return;
        }
        //resu
        try
        {
            Save saveState = SaveManager.Load<Save>("save");
            difficultyOverall = saveState.difficultyOverall;
            adaptiveDifficulty = saveState.adaptiveDifficulty;
            weaponOffsets = saveState.weaponOffsets;
            eventRecords = saveState.eventRecords;
            day = saveState.day;
            stage = saveState.stage;
            stagexp = saveState.stagexp;
            sponsorKarma = saveState.sponsorKarma;
            influence = saveState.influence;
            buyPlayerMP = (float)saveState.buyPlayerMP;
            //tournamentList = SaveManager.Load<int>("listt");
            //setts
            //definedPlayers.CreateNewPlayersArray(defp);
            myBootcamp = saveState.bootcamp;//MatchSave.LoadBootcamp();
            //for (int i = 1; SaveManager.SaveExists("player" + i.ToString()); i++) definedPlayers.LoadPlayer(i);
            //for (int i = 1; SaveManager.SaveExists("team" + i.ToString()); i++) definedTeams.LoadTeam(i);
            //definedPlayers.players = SaveManager.Load<List<Player>>("playerslist"); //5.1.5
            //definedTeams.teams = SaveManager.Load<List<Team>>("teamslist");
            definedPlayers.players = saveState.players;
            definedTeams.teams = saveState.teams;
            byte forceme = 0;
            for (int i = 0; i < definedTeams.teams.Count; i++)
            {
                if (definedTeams.teams[i].teamName == saveState.myteam)
                {
                    if (forceme > 0) definedTeams.teams.RemoveAt(i--);
                    forceme++;
                }
            }
            definedTeams.GroupTeams();
            definedTeams.GeneratePlayers();
            tourInfo = saveState.tourInfo;
            MatchSave.LoadTournaments();
            string s = saveState.myteam; //SaveManager.Load<string>("myteam");
            //definedTeams.GroupTeams();
            for (int i = 0; i < definedTeams.teams.Count; i++)
                if (definedTeams.teams[i].teamName == s)
                {
                    myTeam = definedTeams.GetTeam(s);
                    if (myTeam == null) DebugError("Developer is idiot");
                    myTeam.academy.Load(saveState.academy);
                    loading = false;
                    break;
                }
        }
        catch (System.Exception e)
        {
            DebugError(e.Message + "\n" + e.StackTrace);
            Debug.LogError(e.Message + "\n" + e.StackTrace);
        }
        League.GeneratePlayers();
        definedTeams.UpdateStats();
        //Debug.Log(stage.ToString());
        if (SaveManager.SaveExists("save")) MenuTeam();
        else
        {
            Events.events.GenerateTournaments();
            for (short i = 0; i < definedTeams.teams.Count; i++)
            {
                if (definedTeams.teams[i].GetMoney() == 0)
                    definedTeams.teams[i].BetWin(Mathf.Clamp(5100000 - 100000 * definedTeams.
                        GetTeamPlacement(definedTeams.teams[i].teamName), 100000, 5000000));
                definedTeams.teams[i].followers = (int)(definedTeams.teams[i].GetMoney() * 0.4f);
            }
            SaveManager.Save<int>(0, "index");
            MenuChooseDifficulty();
            //SwitchMenu("teamchoice");
            //Tutorial();
        }
    }

    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Text languageText, framerateText, resultsText, autosText, frenchNotify;

    private IEnumerator FrenchMan()
    {
        float f = (rateSetting == 0 ? 0.0078125f : (rateSetting == 1 ? 0.015625f : 0.03125f));
        while (TranslateObject.language == TranslateObject.Language.French)
        {
            yield return new WaitForSeconds(2.0f);
            while (frenchNotify.color.a < 1.0f)
            {
                frenchNotify.color += new Color(0, 0, 0, f);
                yield return new WaitForSeconds(frameTime);
            }
            while (frenchNotify.color.a > 0.0f)
            {
                frenchNotify.color -= new Color(0, 0, 0, f);
                yield return new WaitForSeconds(frameTime);
            }
        }
    }

    public void SoundSetting(float val) { audioSource.volume = val; }
    public void LanguageSettingLeft()
    {
        if ((int)TranslateObject.language != 0) TranslateObject.language -= 1;
        else TranslateObject.language = TranslateObject.Language.French;
        UpdateLangugage();
    }

    private void UpdateLangugage()
    {
        string[] lang = new string[] { "English", "Русский", "Deutsch", "Português", "Français" };
        languageText.text = lang[(byte)TranslateObject.language];
        if (TranslateObject.language == TranslateObject.Language.French) StartCoroutine(FrenchMan());
    }

    public void LanguageSettingRight()
    {
        if ((int)TranslateObject.language != 4) TranslateObject.language += 1;
        else TranslateObject.language = TranslateObject.Language.English;
        UpdateLangugage();
    }

    public void FrameSettingLeft()
    {
        if (rateSetting != 0) rateSetting -= 1;
        else rateSetting = 2;
        FrameLangugage(true);
    }

    private void FrameLangugage(bool restart = true)
    {
        string[] lang = new string[] { "128", "64", "32" };
        frameTime = rateSetting == 0 ? 0.0078125f : (rateSetting == 1 ? 0.015625f : 0.03125f);
        Application.targetFrameRate = 256;
        Time.fixedDeltaTime = frameTime;
        framerateText.text = lang[rateSetting];
    }

    public void FrameSettingRight()
    {
        if (rateSetting != 2) rateSetting += 1;
        else rateSetting = 0;
        FrameLangugage(true);
    }

    public void ResultsSettingLeft()
    {
        if (skipResults != 0) skipResults -= 1;
        else skipResults = 2;
        ResultsLangugage();
    }

    private void ResultsLangugage()
    {
        string[] lang = new string[] { "Everything", "Все", "Alle", "Tudo", "Tout" };
        switch (skipResults)
        {
            case 0:
                resultsText.text = lang[(int)TranslateObject.language];
                break;
            case 1:
                lang = new string[] { "Only Majors", "Только Мейджеры", "Nur Majors", "Apenas Majors", "Majeurs seulement" };
                resultsText.text = lang[(int)TranslateObject.language];
                break;
            case 2:
                lang = new string[] { "Only Mine", "Только мои", "Nur meins", "Só meu", "Seulement le mien" };
                resultsText.text = lang[(int)TranslateObject.language];
                break;
        }
    }

    public void ResultsSettingRight()
    {
        if (skipResults != 2) skipResults += 1;
        else skipResults = 0;
        ResultsLangugage();
    }

    public string GetDayText() { return (((day - 1) % 30) + 1).ToString("00"); }

    public string GetDateText(int p = 0)
    {
        int _day = day + p;
        int d = ((_day - 1) % 30) + 1;
        int y = ((_day - 1) / 360) + 2022;
        string mt = string.Empty;
        int m = (((_day - 1) / 30) % 12) + 1;
        switch (m)
        {
            case 1:
                mt = "Jan";
                break;
            case 2:
                mt = "Feb";
                break;
            case 3:
                mt = "Mar";
                break;
            case 4:
                mt = "Apr";
                break;
            case 5:
                mt = "May";
                break;
            case 6:
                mt = "Jun";
                break;
            case 7:
                mt = "Jul";
                break;
            case 8:
                mt = "Aug";
                break;
            case 9:
                mt = "Sep";
                break;
            case 10:
                mt = "Oct";
                break;
            case 11:
                mt = "Nov";
                break;
            case 12:
                mt = "Dec";
                break;
        }
        return d.ToString("00") + " " + mt + " " + y.ToString();
    }

    public byte rateSetting = 1;
    public float frameTime = 0.015625f; // 0.03125 0.015625 0.0078125

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip enterGameClip;

    public static float matchImportance = 1.0f;
    public void NextDay(object o) { NextDay(); }
    public void NextDay()
    {
        if (nextDay) return;
        nextDay = true;
        StartCoroutine(NextDayTime());
    }
    private bool nextDay = false;
    private byte skipResults = 1;
    private bool skipTournament = false, chooosingT = false;
    [SerializeField] private GameObject debugPopup;
    [SerializeField] private Text debugText;
    [SerializeField] private GameObject nextDayBlock;

    private byte AttendOrNot(bool c) { skipTournament = !c; chooosingT = false; return 0; }

    private IEnumerator NextDayTime()
    {
        nextDayBlock.SetActive(true);
        int k = myTeam.currentTournament;
        int h = myTeam.GetMoney();
        day++;
        List<Tournament> ts = Events.events.GetTournaments();
        string[] table = new string[] 
        { 
            "No registered events",
            "Нет зарегистрированных турниров",
            "Keine registrierten Turniere",
            "Nenhum evento registrado",
            "Tournois non-enregistrés"
        };
        bool bbul = false;
        for (int i = 0; i < ts.Count; i++)
        {
            if (ts[i].day <= day)
            {
                if (ts[i].day == day && !ts[i].isCalled)
                {
                    ts[i].GenerateTournament(false, null, ts[i].btype, ts[i].tier);
                    MatchSetting ms = ts[i].AreThereAnyMatches(myTeam, day);
                    if (ms != new MatchSetting())
                    {
                        skipTournament = false;
                        chooosingT = true;
                        bbul = true;
                        table = new string[]
                        {
                            "Do you want to accept the invitation to a ",
                            "Вы хотите принять приглашение на ",
                            "Möchten Sie die Einladung zu einem ... ",
                            "Quer aceitar o convite para um ",
                            "Voulez-vous accepter l'invitation à un "
                        };
                        popupQuest = AttendOrNot;
                        ShowPopUpAsk(table[(int)TranslateObject.language] + ts[i].title + " (" + ts[i].type + ')' +
                            (TranslateObject.language != TranslateObject.Language.German ? "?" : " annehmen?"), false);
                        yield return new WaitWhile(() => chooosingT);
                        if (skipTournament)
                        {
                            ts[i] = new Tournament(
                                ts[i].title, // title
                                ts[i].type, // type
                                ts[i].tier, // tier
                                ts[i].prizePool, // prizepool
                                ts[i].day, // day
                                ts[i].btype, // btype
                                ts[i].logo, // logo
                                ts[i].isMajor, // isMajor
                                ts[i].majorPoints, // majorPoints
                                ts[i].open, // open
                                ts[i].unlockEvent);
                            ts[i].GenerateTournament(false, null, ts[i].btype, ts[i].tier, false);
                        }
                    }
                }
                if (ts[i].AreThereAnyMatches())
                {
                    /*table = new string[]
                    {
                        "Simulating day...",
                        "Симуляция дня...",
                        "Tag simulieren...",
                        "Simulando dia..."
                    };*/
                    List<MatchSetting> ms = ts[i].GetMatchesIfThereAreAny();
                    tempResults = new List<MapResult>(ms.Count);
                    matchImportance = 2.0f / ms.Count;
                    if (ts[i].tier == 3) matchImportance *= 0.25f;
                    else if (ts[i].tier == 2) matchImportance *= 0.5f;
                    else if (ts[i].tier == 1) matchImportance *= 1.0f;
                    else matchImportance *= 2.0f;
                    for (int j = 0; j < ms.Count; j++)
                    {
                        GameObject go = Instantiate(emptyPrefab);
                        match = go.AddComponent<Match>();
                        match.type = ms[j].type;
                        if (ms[j].team1.players.Count < 5) ms[j].team1.Check5MenMatch();
                        if (ms[j].team2.players.Count < 5) ms[j].team2.Check5MenMatch();
                        match.team1 = ms[j].team1;
                        match.team2 = ms[j].team2;
                        match.StartPick(day, i);
                        /*if (!(skipResults == 0 || (skipResults == 1 && ts[i].isMajor) ||
                            match.team2.teamName == myTeam.teamName ||
                            match.team1.teamName == myTeam.teamName))
                            ShowPopUp(table[(int)TranslateObject.language]);
                        else HidePopUp();*/
                        if (match.team1.teamName == myTeam.teamName) { StartPick(); match.playerTeam = 1; }
                        else if (match.team2.teamName == myTeam.teamName) { StartPick(); match.playerTeam = 2; }
                        else
                        {
                            if (match.type == MatchType.BO1)
                            {
                                byte r;
                                do { r = (byte)Random.Range(0, 7); } while (match.pickbans.Contains(r));
                                match.BanMap(r);
                                do { r = (byte)Random.Range(0, 7); } while (match.pickbans.Contains(r));
                                match.BanMap(r);
                                do { r = (byte)Random.Range(0, 7); } while (match.pickbans.Contains(r));
                                match.BanMap(r);
                                do { r = (byte)Random.Range(0, 7); } while (match.pickbans.Contains(r));
                                match.BanMap(r);
                                do { r = (byte)Random.Range(0, 7); } while (match.pickbans.Contains(r));
                                match.BanMap(r);
                                do { r = (byte)Random.Range(0, 7); } while (match.pickbans.Contains(r));
                                match.BanMap(r);
                                do { r = (byte)Random.Range(0, 7); } while (match.pickbans.Contains(r));
                                match.PickMap(r);
                            }
                            if (match.type == MatchType.BO3)
                            {
                                byte r;
                                do { r = (byte)Random.Range(0, 7); } while (match.pickbans.Contains(r));
                                match.BanMap(r);
                                do { r = (byte)Random.Range(0, 7); } while (match.pickbans.Contains(r));
                                match.BanMap(r);
                                do { r = (byte)Random.Range(0, 7); } while (match.pickbans.Contains(r));
                                match.PickMap(r);
                                do { r = (byte)Random.Range(0, 7); } while (match.pickbans.Contains(r));
                                match.PickMap(r);
                                do { r = (byte)Random.Range(0, 7); } while (match.pickbans.Contains(r));
                                match.BanMap(r);
                                do { r = (byte)Random.Range(0, 7); } while (match.pickbans.Contains(r));
                                match.BanMap(r);
                                do { r = (byte)Random.Range(0, 7); } while (match.pickbans.Contains(r));
                                match.PickMap(r);
                            }
                            if (match.type == MatchType.BO5)
                            {
                                byte r;
                                do { r = (byte)Random.Range(0, 7); } while (match.pickbans.Contains(r));
                                match.BanMap(r);
                                do { r = (byte)Random.Range(0, 7); } while (match.pickbans.Contains(r));
                                match.BanMap(r);
                                do { r = (byte)Random.Range(0, 7); } while (match.pickbans.Contains(r));
                                match.PickMap(r);
                                do { r = (byte)Random.Range(0, 7); } while (match.pickbans.Contains(r));
                                match.PickMap(r);
                                do { r = (byte)Random.Range(0, 7); } while (match.pickbans.Contains(r));
                                match.PickMap(r);
                                do { r = (byte)Random.Range(0, 7); } while (match.pickbans.Contains(r));
                                match.PickMap(r);
                                do { r = (byte)Random.Range(0, 7); } while (match.pickbans.Contains(r));
                                match.PickMap(r);
                            }
                            match.SimulateMatch(true);
                        }
                        match.matchGoing = true;
                        nextDayBlock.SetActive(false);
                        yield return new WaitWhile(() => match.matchGoing);
                        if (skipResults == 0 || (skipResults == 1 && ts[i].isMajor) ||
                            match.team2.teamName == myTeam.teamName ||
                            match.team1.teamName == myTeam.teamName) { MenuResults(0); blockdayr = true; }
                        yield return new WaitWhile(() => blockdayr);
                        for (int w = 0; w < Bets.manager.bets.Count; w++)
                        {
                            if (Bets.manager.bets[w].team1 == match.team1.teamName && Bets.manager.bets[w].team2 == match.team2.teamName)
                            {
                                if (match.round1 > match.round2)
                                {
                                    myTeam.BetWin((int)(Bets.manager.bets[w].money1 * Bets.manager.bets[w].coef1));
                                }
                                else
                                {
                                    myTeam.BetWin((int)(Bets.manager.bets[w].money2 * Bets.manager.bets[w].coef2));
                                }
                                Bets.manager.bets.RemoveAt(w);
                            }
                        }
                        Destroy(go);
                        //yield return null;
                    }
                    mapResults.AddRange(tempResults);
                    if (ts[i].day + 6 != day && k == myTeam.currentTournament) { HidePopUp(); }
                    continue;
                }
            }
        }
        //
        nextDayBlock.SetActive(true);
        myTeam.WorsenForm();
        byte mapv = 0;
        for (byte i = 0; i < myTeam.players.Count; i++) myTeam.players[i].daysInTeam++;
        if (myBootcamp != null)
        {
            if (day % 30 == payPsychoDay)
            {
                table = new string[]
                {
                    "Not enough money to keep bootcamp full",
                    "Недостаточно денег, чтобы держать буткемп полным",
                    "Nicht genug Geld, um das Bootcamp voll zu halten",
                    "Dinheiro insuficiente para manter o bootcamp cheio",
                    "Pas assez d'argent pour garder le bootcamp au maximum"
                };
                if (!myTeam.ReduceMoney(myBootcamp.psychologist.pricePerMonth))
                {
                    myBootcamp.psychologist = new Psychologist(string.Empty, 0, 0);
                    ShowPopUp(table[(int)TranslateObject.language]);
                }
            }
            if (day % 30 == payCoachDay)
            {
                table = new string[]
                {
                    "Not enough money to keep bootcamp full",
                    "Недостаточно денег, чтобы держать буткемп полным",
                    "Nicht genug Geld, um das Bootcamp voll zu halten",
                    "Dinheiro insuficiente para manter o bootcamp cheio",
                    "Pas assez d'argent pour garder le bootcamp au maximum"
                };
                //if (myTeam.money > myBootcamp.assistantCoach.pricePerMonth)
                //    myTeam.money -= myBootcamp.assistantCoach.pricePerMonth;
                if (!myTeam.ReduceMoney(myBootcamp.assistantCoach.pricePerMonth))
                {
                    myBootcamp.assistantCoach = new AssistantCoach(string.Empty, 0, 0);
                    ShowPopUp(table[(int)TranslateObject.language]);
                }
            }
            if (day % 30 == payBootDay)
            {
                //if (myTeam.money > myBootcamp.pricePerMonth)
                //{
                //    myTeam.money -= myBootcamp.pricePerMonth;
                //}
                if (!myTeam.ReduceMoney(myBootcamp.pricePerMonth))
                {
                    table = new string[]
                    {
                        "Not enough money to keep bootcamp",
                        "Недостаточно денег на содержание буткемпа",
                        "Nicht genug Geld, um Bootcamp zu halten",
                        "Dinheiro insuficiente para manter o bootcamp",
                        "Pas assez d'argent pour garder le bootcamp"
                    };
                    ShowPopUp(table[(int)TranslateObject.language]);
                    myBootcamp.assistantCoach = new AssistantCoach(string.Empty, 0, 0);
                    myBootcamp.psychologist = new Psychologist(string.Empty, 0, 0);
                    myBootcamp = null;
                }
            }
        }
        bool mbp = true;
        if (myBootcamp != null)
        {
            mapv = (byte)((myBootcamp.mapPoints * 5) - 7 + myBootcamp.assistantCoach.mapAdd);
            byte used = myTeam.GetMapPoints();
            for (byte i = 0; i < 7; i++) used += myTeam.GetMap((Map)i);
            mbp = used <= mapv;
            //Debug.Log("mapv: " + mapv.ToString() + ", used: " + used.ToString());
            if (mbp) myTeam.AddMapPoints((byte)(mapv - used));
            myTeam.AddEnergy(myBootcamp);
            myTeam.AddForm(myBootcamp);
            myTeam.AddChemistry(myBootcamp);
            myTeam.AddPsychology(myBootcamp);
        }
        for (byte i = 0; i < 5; i++)
        {
            if (myTeam.UseMap((Map)Random.Range(0, 7)) && mbp) myTeam.AddMapPoints(1);
        }
        //PlayerPrefs.Save();
        if (k != myTeam.currentTournament && !bbul)
        {
            table = new string[]
            {
                "Your team successfully qualified for ",
                "Ваша команда успешно квалифицировалась на ",
                "Ihr Team hat sich erfolgreich qualifiziert für ",
                "Sua equipe se qualificou com sucesso para ",
                "Votre équipe s'est qualifiée pour "
            };
            ShowPopUp(table[(int)TranslateObject.language] +
            Events.events.GetTournaments()[myTeam.currentTournament].title);
        }
        definedTeams.GroupTeams();
        definedTeams.PointDecay();
        for (short i = 0; i < definedTeams.teams.Count; i++)
        {
            Team tet = definedTeams.GetTeamPlacement(i);
            if (tet.teamName == "Free") continue;
            tet.CalculateMedia();
            if (myTeam.teamName != tet.teamName)
            {
                tet.BuyJersey();
                tet.BuyPoster();
                tet.BuySticker();
                tet.SellJerseyF();
                tet.SellPosterF();
                tet.SellStickerF();
            }
            else
            {
                if (autoBuyMerch)
                {
                    tet.BuyJersey();
                    tet.BuyPoster();
                    tet.BuySticker();
                    int st = tet.GetMoney();
                    //tet.taskSponsor.soldJerseys += myTeam.SellJerseyF();
                    //tet.taskSponsor.soldPosters += myTeam.SellPosterF();
                    //tet.taskSponsor.soldStickers += myTeam.SellStickerF();
                    tet.SellJerseyF();
                    tet.SellPosterF();
                    tet.SellStickerF();
                    tet.ReduceMoney((int)((tet.GetMoney() - st) * 0.15));
                }
                else
                {
                    tet.SellJerseyF();
                    tet.SellPosterF();
                    tet.SellStickerF();
                    //tet.taskSponsor.soldJerseys += myTeam.SellJerseyF();
                    //tet.taskSponsor.soldPosters += myTeam.SellPosterF();
                    //tet.taskSponsor.soldStickers += myTeam.SellStickerF();
                }
            }
            tet.academy.AddDay(tet);
        }
        for (byte i = 0; i < myTeam.players.Count; i++)
        {
            myTeam.players[i].daysPayed--;
            if (myTeam.players[i].daysPayed < 1)
            {
                if (myTeam.ReduceMoney(myTeam.players[i].salary))
                {
                    myTeam.players[i].daysPayed += 30;
                }
                else
                {
                    table = new string[]
                    {
                        "No money to pay salaries, earn money before the end of the month or players will leave",
                        "Нет денег на зарплату, нужно заработать до конца месяца, иначе игроки уйдут",
                        "Kein Geld, um Gehälter zu zahlen, Sie müssen vor Ende des Monats Geld verdienen, oder die Spieler werden gehen",
                        "Sem dinheiro para pagar salários, você precisa ganhar dinheiro antes do final do mês ou os jogadores irão embora",
                        "Pas assez d'argent pour payer les salaires des joueurs, gagner de l'argent avant la fin du mois ou les joueurs partiront"
                    };
                    ShowPopUp(table[(int)TranslateObject.language]);
                }
            }
        }
        if (GrandSlamCheck())
        {
            myTeam.AddMoney(3000000);
            //myTeam.money += 3000000;
            playGames.UnlockAchievement(3);
            table = new string[]
            {
                " are awarded with golden ingots",
                " награждаются золотыми слитками",
                " werden mit goldenen Barren ausgezeichnet",
                " são premiados com lingotes de ouro",
                " est récompensé avec un lingots d'or"
            };
            ShowPopUp("GrandSlam: " + myTeam.teamName + table[(int)TranslateObject.language]);
            yield return new WaitForSeconds(3.0f);
            eventRecords = new Queue<EventRecord>(8);
        }
        int s = stage;
        stagexp += (myTeam.GetMoney() - h) + 1000;
        saveState.stagexp = stagexp;
        if (stagexp >= 8000000) { stage = 3; saveState.stage = stage; }
        else if (stagexp >= 2000000) { stage = 2; saveState.stage = stage; }
        else if (stagexp >= 500000) { stage = 1; saveState.stage = stage; }
        if (stagexp >= 1000000) playGames.UnlockAchievement(0);
        if (definedTeams.GetTeamPlacement(myTeam.teamName) == 1) playGames.UnlockAchievement(4);
        if (Random.Range(0, 100) < 3)
        {
            if (Random.Range(0, 100) < 60)
            {
                int t = Random.Range(1, 19);
                int pl = definedTeams.GetTeamPlacement(myTeam.teamName);
                int mont = Mathf.Clamp((1 - pl) * 3500 + t * 17500 + Random.Range(1, 26) * 1000, 5000, 9999999);
                tempspon = new SponsorUnit(
                    Sponsor.names[Random.Range(0, Sponsor.names.Length)],
                    mont,
                    (ushort)(t * 30)
                    );
                popupQuest = AddTempSponsor;
                ShowPopUpAsk($"{ tempspon }", false);
            }
            else
            {
                int t = Random.Range(1, 19);
                int pl = definedTeams.GetTeamPlacement(myTeam.teamName);
                int randompc = Random.Range(1, 11);
                int mont = Mathf.Clamp((definedTeams.teams.Count - pl), 10, 999) * t * 160 * randompc;
                tempinv = new InvestorUnit(
                    Sponsor.firstnames[Random.Range(0, Sponsor.firstnames.Length)],
                    Sponsor.lastnames[Random.Range(0, Sponsor.lastnames.Length)],
                    mont,
                    (ushort)(t * 30),
                    (byte)randompc
                    );
                popupQuest = AddTempInvestor;
                ShowPopUpAsk($"{ tempinv }", false);
            }
        }
        for (int i = 0; i < investors.Count; i++)
            if (investors[i].PassDay())
                investors.RemoveAt(i--);
        for (int i = 0; i < sponsors.Count; i++)
            if (sponsors[i].PassDay())
                sponsors.RemoveAt(i--);
        if ((day - 1) % 30 == 0)
        {
            myTeam.leaguePlayers--;
            if (myTeam.leaguePlayers < 0) myTeam.leaguePlayers = 0;
            for (byte i = 0; i < 15; i++) weaponOffsets[i] = (byte)Random.Range(0, 16);
            saveState.weaponOffsets = weaponOffsets;
            for (short i = 0; i < definedTeams.teams.Count; i++)
            {
                Team tet = definedTeams.GetTeamPlacement(i);
                if (myTeam.teamName == tet.teamName) 
                {
                    //if (tet.taskSponsor.IsCompleted()) { sponsorKarma += 5; stagexp += 25000 * (stage + 1); }
                    //else sponsorKarma -= 5;
                    saveState.sponsorKarma = sponsorKarma;
                    continue;
                }
                if (tet.wantsChanges > 30) { tet.Disappointment(); tet.wantsChanges = 0; }
                //tet.taskSponsor = Sponsor.SignSponsor((byte)((80 - i) * 0.05f), 50);
                //tet.money += tet.taskSponsor.money;
            }
            //SwitchMenu("sponsor");
            nextDayBlock.SetActive(false);
        }
        if ((day - 1) % 360 == 0)
        {
            for (int i = 1; i < definedPlayers.players.Count; i++)
                definedPlayers.players[i].age++;
            /*int tc = ((day - 1) / 360) * 57 + 67;
            List<Tournament> ots = new List<Tournament>(57);
            for (int i = ((day - 1) / 360) * 57 + 10; i < tc; i++)
                ots.Add(MatchSave.LoadTournament(i));
            Events.events.AddTournaments(ots);*/
        }
        //if (s != stage) { SwitchMenu("teamchoice"); nextDayBlock.SetActive(false); }
        influence += 16;
        QuickSave();
        definedTeams.UpdateStats();
        //PlayerPrefs.Save();
        /*table = new string[]
        {
            "Simulating day...",
            "Симуляция дня...",
            "Tag simulieren...",
            "Simulando dia..."
        };*/
        //if (popupText.text == table[(int)TranslateObject.language]) HidePopUp();
        nextDay = false;
        nextDayBlock.SetActive(false);
        if (!(day % 360 == 1)) MenuCalendar();
        else MenuTop20();
        //yield return null;
    }
    private List<InvestorUnit> investors;
    private List<SponsorUnit> sponsors;
    private InvestorUnit tempinv;
    private SponsorUnit tempspon;
    public int GetInvestorShares() { if (investors == null) return 0; return investors.Sum(x => x.percent); }
    private byte AddTempInvestor(bool b)
    {
        if (!b) return 255;
        investors.Add(tempinv);
        ShowPopUp($"Now { tempinv.name } earns { tempinv.percent }% of your gains");
        return 0;
    }
    private byte AddTempSponsor(bool b)
    {
        if (!b) return 255;
        sponsors.Add(tempspon);
        ShowPopUp($"Now { tempspon.name } have sponsorship with you");
        return 0;
    }
    private bool autoBuyMerch = false;
    public void AutoBuyMerch(GameObject obj)
    {
        autoBuyMerch = autoBuyMerch ? false : true;
        obj.SetActive(autoBuyMerch);
    }

    public void AutoBuyMerch(bool b)
    {
        autoBuyMerch = b;
        //MenuMerch();
    }

    public void ExitGame()
    {
        /*string[] table = new string[]
        {
            "Saving the game",
            "Сохранение игры",
            "Das Spiel speichern",
            "Salvando o jogo",
            "Sauvegarde de la partie"
        };*/
        StartCoroutine(ExitEnum());
    }

    private IEnumerator ExitEnum()
    {
        //saveWindow.gameObject.SetActive(true);
        //PopUpDesc(saveWindow);
        //yield return new WaitWhile(() => transition);
        SaveGame();
        yield return new WaitWhile(() => saving);
        //PopDownDesc();
        //yield return new WaitWhile(() => transition);
        //saveWindow.gameObject.SetActive(false);
        definedTeams.StopAllCoroutines();
        StopAllCoroutines();
        Application.Quit();
    }

    public void AppQuit() { Application.Quit(); }

    public void SwitchMap(Map map)
    {
        for (byte i = 0; i < 7; i++)
        {
            mapBackground[i].SetActive(false);
            mapRadars[i].SetActive(false);
        }
        mapBackground[(int)map].SetActive(true);
        mapRadars[(int)map].SetActive(true);
    }

    private byte nupage = 0, ngpage = 0;

    public string GenerateNewsTitle(string winner, string loser, 
        string tournament, string bestPlayer, byte hasWon = 0)
    {
        string[] strs1 = new string[] 
        { "edge out", "move past", "demolish", "smash", 
            "stun", "beat", "take down", "defeat", "eliminate" };
        int r = Random.Range(0, 100);
        string text;
        if (r < 50)
        {
            text = winner + " " + strs1[Random.Range(0, strs1.Length)] + " " + loser;
            if (r < 15) text += " in " + tournament;
        }
        else if (r < 63 && hasWon == 0) text = bestPlayer + "'s performance isn't enought to beat " + winner;
        else if (r < 55 && hasWon == 1) text = bestPlayer + " leads " + winner + " to win";
        else if (r < 75 && hasWon == 0) text = loser + " lose despite " + bestPlayer + " efforts";
        else if (r < 60 && hasWon == 1) text = bestPlayer + " smashes " + loser;
        else if (r < 65 && hasWon == 1) text = bestPlayer + " and co. outclasses " + loser;
        else if (r < 70 && hasWon == 1) text = bestPlayer + " shows a masterclass against " + loser;
        else if (r < 75 && hasWon == 1) text = winner + " with power of " + bestPlayer + " beat " + loser;
        else if (r < 80) text = bestPlayer + ": \"We are feeling confident\"";
        else if (r < 85) text = winner + ": \"We want to show everyone that we are the best\"";
        else if (r < 90) text = bestPlayer + ": \"I am hungry for trophies\"";
        else if (r < 95) text = loser + ": \"We were playing too passively\"";
        else text = loser + ": \"We weren't playing our game\"";
        return text;
    }

    public Sprite LogoSprite(string name)
    {
        for (byte i = 0; i < tournamentIDs.Length; i++)
            if (tournamentIDs[i].name == name) return tournamentIDs[i].sprite;
        return null;
    }

    public int LogoSize(string eventName)
    {
        switch (eventName)
        {
            case "iem":
                return 128;
            case "dh":
                return 72;
            case "esl":
            case "blast":
            case "hype":
            case "star":
            default:
                return 56;
        } 
    }

    public void CreateOrRecruitPlayer(int i)
    {
        if (i < myTeam.academy.GetPlayersCount())
        {
            string[] table;
            switch (myTeam.academy.TakePlayer(myTeam, (byte)i))
            {
                case 0:
                    table = new string[]
                    {
                        "Your player isn't ready yet",
                        "Ваш игрок еще не готов",
                        "Dein Player ist noch nicht bereit",
                        "Seu player ainda não está pronto",
                        "Votre joueur n'est pas encore pret",
                    };
                    ShowPopUp(table[(int)TranslateObject.language]);
                    break;
                case 11:
                    table = new string[]
                    {
                        "Your squad is full",
                        "Ваша команда заполнена",
                        "Dein Team ist voll",
                        "Seu time está cheio",
                        "Votre équipe est complète"
                    };
                    ShowPopUp(table[(int)TranslateObject.language]);
                    break;
                case 1:
                    table = new string[]
                    {
                        "The player has been placed on the bench of main roster",
                        "Игрок был помещен на скамейку запасных основного состава",
                        "Der Spieler wurde auf die Bank des Hauptkaders gesetzt",
                        "O jogador foi colocado no banco da escalação principal",
                        "Le joueur a été placé sur le banc de l'équipe principale"
                    };
                    ShowPopUp(table[(int)TranslateObject.language]);
                    AddPlayerToList(myTeam.players[myTeam.players.Count - 1]);
                    break;
            }
        }
        else ShowPopUpAc();
    }
    /*private int academyRating;
    public byte academyRole { get; private set; }
    public string academyNickname { get; private set; }*/
    /*public void RecruitPlayerAcademy()
    {
        if (academyNickname.Length < 3) 
        {
            string[] table = new string[]
            {
                "Nickname is too short",
                "Никнейм слишком короткий",
                "Spitzname ist zu kurz",
                "Apelido é muito curto",
                "Pseudo trop court"
            };
            ShowPopUp(table[(int)TranslateObject.language]); return;
        }
        if (academyRating < 40)
        {
            string[] table = new string[]
            {
                "Rating should be between 39 and 100",
                "Рейтинг должен быть от 39 до 100",
                "Die Bewertung sollte von 39 bis 100 sein",
                "A classificação deve estar entre 39 e 100",
                "La note doit etre entre 39 et 100"
            };
            ShowPopUp(table[(int)TranslateObject.language]); return;
        }
        HidePopUpAc();
        myTeam.academy.CreatePlayer(
            academyNickname,
            (Role)academyRole,
            (Activity)Random.Range(0, 3),
            myTeam.players[Random.Range(0, myTeam.players.Count)].nationality,
            (byte)academyRating);
        MenuAcademy();
    }*/

    private System.Func<bool, byte> popupQuest = null;
    public void ShowPopUp(string text)
    {
        popupText.text = text;
        StartCoroutine(PopUpEnum());
    }

    public void HidePopUp()
    {
        StartCoroutine(PopDownEnum());
    }

    public void ShowPopUpAsk(string text, bool showAdd)
    {
        showAddAgree = showAdd;
        popupAskText.text = text;
        StartCoroutine(PopUpAskEnum());
    }

    private bool showAddAgree = true;
    public void AgreePopUpAsk()
    {
        if (showAddAgree) AdManager.ShowAdd();
        popupQuest(true);
        HidePopUpAsk();
    }

    public void DisagreePopUpAsk()
    {
        popupQuest(false);
        HidePopUpAsk();
    }

    private void HidePopUpAsk()
    {
        StartCoroutine(PopDownAskEnum());
    }

    public void ShowPopUpAc()
    {
        StartCoroutine(PopUpAcEnum());
    }

    public void HidePopUpAc()
    {
        StartCoroutine(PopDownAcEnum());
    }

    private IEnumerator PopUpEnum()
    {
        while (transition) yield return new WaitForSeconds(0.125f);
        float f = 0.03125f;
        if (rateSetting == 1) f = 0.0625f;
        else if (rateSetting == 2) f = 0.125f;
        transition = true;
        while (popupWindow.localScale.x < 1.0f)
        {
            popupWindow.localScale += new Vector3(f, f, f);
            yield return new WaitForSeconds(frameTime);
        }
        transition = false;
    }

    private IEnumerator PopDownEnum()
    {
        while (transition) yield return new WaitForSeconds(0.125f);
        float f = 0.03125f;
        if (rateSetting == 1) f = 0.0625f;
        else if (rateSetting == 2) f = 0.125f;
        transition = true;
        while (popupWindow.localScale.x > 0.0f)
        {
            popupWindow.localScale -= new Vector3(f, f, f);
            yield return new WaitForSeconds(frameTime);
        }
        transition = false;
    }

    private IEnumerator PopUpAskEnum()
    {
        while (transition) yield return new WaitForSeconds(0.125f);
        float f = 0.03125f;
        if (rateSetting == 1) f = 0.0625f;
        else if (rateSetting == 2) f = 0.125f;
        transition = true;
        while (popupWindowAsk.localScale.x < 1.0f)
        {
            popupWindowAsk.localScale += new Vector3(f, f, f);
            yield return new WaitForSeconds(frameTime);
        }
        transition = false;
    }

    private IEnumerator PopDownAskEnum()
    {
        while (transition) yield return new WaitForSeconds(0.125f);
        float f = 0.03125f;
        if (rateSetting == 1) f = 0.0625f;
        else if (rateSetting == 2) f = 0.125f;
        transition = true;
        while (popupWindowAsk.localScale.x > 0.0f)
        {
            popupWindowAsk.localScale -= new Vector3(f, f, f);
            yield return new WaitForSeconds(frameTime);
        }
        transition = false;
    }

    private IEnumerator PopUpAcEnum()
    {
        while (transition) yield return new WaitForSeconds(0.125f);
        float f = 0.03125f;
        if (rateSetting == 1) f = 0.0625f;
        else if (rateSetting == 2) f = 0.125f;
        transition = true;
        while (popupWindowAc.localScale.x < 1.0f)
        {
            popupWindowAc.localScale += new Vector3(f, f, f);
            yield return new WaitForSeconds(frameTime);
        }
        transition = false;
    }

    private IEnumerator PopDownAcEnum()
    {
        while (transition) yield return new WaitForSeconds(0.125f);
        float f = 0.03125f;
        if (rateSetting == 1) f = 0.0625f;
        else if (rateSetting == 2) f = 0.125f;
        transition = true;
        while (popupWindowAc.localScale.x > 0.0f)
        {
            popupWindowAc.localScale -= new Vector3(f, f, f);
            yield return new WaitForSeconds(frameTime);
        }
        transition = false;
    }

    
    private bool transition = false;

    private IEnumerator LoadOverlay(GameObject current)
    {
        if (!transition && current != null)
        {
            transition = true;
            byte f = (byte)(rateSetting == 0 ? 1 : (rateSetting == 1 ? 2 : 4));
            byte _i = (byte)(rateSetting == 0 ? 32 : (rateSetting == 1 ? 16 : 8));

            RectTransform[] rectTransforms = current.GetComponentsInChildren<RectTransform>();
            Vector2[] vector2s = new Vector2[rectTransforms.Length - 1];
            for (short i = 0; i < vector2s.Length; i++)
                vector2s[i] = rectTransforms[i + 1].anchoredPosition;
            byte t = 0;
            while (t < _i)
            {
                for (short i = 1; i < rectTransforms.Length; i++)
                    if (rectTransforms[i] != null) rectTransforms[i].anchoredPosition 
                        += new Vector2((i & 1) == 1 ? -72 * f : 72 * f, (i & 1) == 1 ? 48 * f : -48 * f);
                t++;
                yield return new WaitForSeconds(frameTime);
            }
            for (short i = 0; i < vector2s.Length; i++)
                if (rectTransforms[i + 1] != null) 
                    rectTransforms[i + 1].anchoredPosition = vector2s[i];
            transition = false;
        }
    }


    private byte payCoachDay = 100, payPsychoDay = 100, payBootDay = 100;
    public void ChooseCoach(object o) { ChooseCoach((int)o); }
    public void ChoosePsychologist(object o) { ChoosePsychologist((int)o); }
    public void ChooseCoach(int i)
    {
        ccc = (byte)i;
        popupQuest = ChooseCoach;
        string[] table = new string[] { "Are you sure?", "Вы уверенны?", "Bist du sicher?", "Tem certeza?", "Etes-vous sure?" };
        ShowPopUpAsk(table[(byte)TranslateObject.language], false);

    }
    byte ccc = 0;
    public byte ChooseCoach(bool b)
    {
        if (!b) return 101;
        string[] table = new string[]
        { "Not enough money", "Не хватает денег", "Nicht genug Geld", "Dinheiro insuficiente", "Pas assez d'argent" };
        switch (ccc)
        {
            case 0:
                if (myTeam.ReduceMoney(24300))
                {
                    myBootcamp.assistantCoach = new AssistantCoach("Andrey Gorodensky", 9, 18);
                    payCoachDay = (byte)(day % 30);
                    MenuBootcamp();
                    byte used = myTeam.GetMapPoints();
                    for (byte i = 0; i < 7; i++) used += myTeam.GetMap((Map)i);
                    int e = myBootcamp.assistantCoach.mapAdd + (myBootcamp.mapPoints * 5 - 7) - used;
                    if (e > 0) myTeam.AddMapPoints((byte)e);
                }
                else { ShowPopUp(table[(int)TranslateObject.language]); }
                return 0;
            case 1:
                if (myTeam.ReduceMoney(28350))
                {
                    myBootcamp.assistantCoach = new AssistantCoach("Danny Sørensen", 9, 21);
                    payCoachDay = (byte)(day % 30);
                    MenuBootcamp();
                    byte used = myTeam.GetMapPoints();
                    for (byte i = 0; i < 7; i++) used += myTeam.GetMap((Map)i);
                    int e = myBootcamp.assistantCoach.mapAdd + (myBootcamp.mapPoints * 5 - 7) - used;
                    if (e > 0) myTeam.AddMapPoints((byte)e);
                }
                else { ShowPopUp(table[(int)TranslateObject.language]); }
                return 0;
            case 2:
                if (myTeam.ReduceMoney(19950))
                {
                    myBootcamp.assistantCoach = new AssistantCoach("Konstantin Pikiner", 7, 19);
                    payCoachDay = (byte)(day % 30);
                    MenuBootcamp();
                    byte used = myTeam.GetMapPoints();
                    for (byte i = 0; i < 7; i++) used += myTeam.GetMap((Map)i);
                    int e = myBootcamp.assistantCoach.mapAdd + (myBootcamp.mapPoints * 5 - 7) - used;
                    if (e > 0) myTeam.AddMapPoints((byte)e);
                }
                else { ShowPopUp(table[(int)TranslateObject.language]); }
                return 0;
            case 3:
                if (myTeam.ReduceMoney(16800))
                {
                    myBootcamp.assistantCoach = new AssistantCoach("Björn Pers", 8, 14);
                    payCoachDay = (byte)(day % 30);
                    MenuBootcamp();
                    byte used = myTeam.GetMapPoints();
                    for (byte i = 0; i < 7; i++) used += myTeam.GetMap((Map)i);
                    int e = myBootcamp.assistantCoach.mapAdd + (myBootcamp.mapPoints * 5 - 7) - used;
                    if (e > 0) myTeam.AddMapPoints((byte)e);
                }
                else { ShowPopUp(table[(int)TranslateObject.language]); }
                return 0;
            case 4:
                if (myTeam.ReduceMoney(9000))
                {
                    myBootcamp.assistantCoach = new AssistantCoach("Rémy Quoniam", 5, 12);
                    payCoachDay = (byte)(day % 30);
                    MenuBootcamp();
                    byte used = myTeam.GetMapPoints();
                    for (byte i = 0; i < 7; i++) used += myTeam.GetMap((Map)i);
                    int e = myBootcamp.assistantCoach.mapAdd + (myBootcamp.mapPoints * 5 - 7) - used;
                    if (e > 0) myTeam.AddMapPoints((byte)e);
                }
                else { ShowPopUp(table[(int)TranslateObject.language]); }
                return 0;
            case 5:
                if (myTeam.ReduceMoney(9600))
                {
                    myBootcamp.assistantCoach = new AssistantCoach("Damien Marcel", 4, 16);
                    payCoachDay = (byte)(day % 30);
                    MenuBootcamp();
                    byte used = myTeam.GetMapPoints();
                    for (byte i = 0; i < 7; i++) used += myTeam.GetMap((Map)i);
                    int e = myBootcamp.assistantCoach.mapAdd + (myBootcamp.mapPoints * 5 - 7) - used;
                    if (e > 0) myTeam.AddMapPoints((byte)e);
                }
                else { ShowPopUp(table[(int)TranslateObject.language]); }
                return 0;
        }
        return 202;
    }
    public void ChoosePsychologist(int i)
    {
        cpc = (byte)i;
        popupQuest = ChoosePsychologist;
        string[] table = new string[] { "Are you sure?", "Вы уверенны?", "Bist du sicher?", "Tem certeza?", "Etes-vous sure?" };
        ShowPopUpAsk(table[(byte)TranslateObject.language], false);
    }
    byte cpc = 0;
    public byte ChoosePsychologist(bool b)
    {
        if (!b) return 101;
        string[] table = new string[]
        { "Not enough money", "Не хватает денег", "Nicht genug Geld", "Dinheiro insuficiente", "Pas assez d'argent" };
        switch (cpc) 
        {
            case 0:
                if (myTeam.ReduceMoney(14000))
                {
                    myBootcamp.psychologist = new Psychologist("Max Payne", 7, 10);
                    MenuBootcamp();
                    payPsychoDay = (byte)(day % 30);
                }
                else { ShowPopUp(table[(int)TranslateObject.language]); }
                return 0;
            case 1:
                if (myTeam.ReduceMoney(10800))
                {
                    myBootcamp.psychologist = new Psychologist("Peng Luion", 6, 9);
                    MenuBootcamp();
                    payPsychoDay = (byte)(day % 30);
                }
                else { ShowPopUp(table[(int)TranslateObject.language]); }
                return 0;
            case 2: 
                if (myTeam.ReduceMoney(4800))
                {
                    myBootcamp.psychologist = new Psychologist("James Lionier", 3, 8);
                    MenuBootcamp();
                    payPsychoDay = (byte)(day % 30);
                }
                else { ShowPopUp(table[(int)TranslateObject.language]); }
                return 0;
            case 3:
                if (myTeam.ReduceMoney(8400))
                {
                    myBootcamp.psychologist = new Psychologist("Karol Witkowski", 7, 6);
                    MenuBootcamp();
                    payPsychoDay = (byte)(day % 30);
                }
                else { ShowPopUp(table[(int)TranslateObject.language]); }
                return 0;
            case 4:
                if (myTeam.ReduceMoney(4000))
                {
                    myBootcamp.psychologist = new Psychologist("Fabien Sapiel", 4, 5);
                    MenuBootcamp();
                    payPsychoDay = (byte)(day % 30);
                }
                else { ShowPopUp(table[(int)TranslateObject.language]); }
                return 0;
            case 5:
                if (myTeam.ReduceMoney(1200))
                {
                    myBootcamp.psychologist = new Psychologist("Andrew Pagolini", 1, 6);
                    MenuBootcamp();
                    payPsychoDay = (byte)(day % 30);
                }
                else { ShowPopUp(table[(int)TranslateObject.language]); }
                return 0;
        }
        return 202;
    }
    Team[] meats;

    private int academyRating;
    private byte academyRole = 2;
    [HideInInspector] public string academyNickname = string.Empty;
    [HideInInspector] public GameObject[] menuelements; // -256
    private int menuelhelp = -256;
    [SerializeField] private GameObject fspawner, fbool, finput, farray, oldpreview, adremover;
    [SerializeField] private List<Sprite> fsprite;
    bool blockdayr = false;
    public void MenuReset()
    {
        MenuReset(false);
    }
    public void MenuReset(bool b)
    {
        blockdayr = b;
        adremover.SetActive(false);
        welcgo.SetActive(false);
        matchUI.SetActive(false);
        oldpreview.SetActive(false);
        if (evBracketMatches != null)
        {
            for (int i = 0; i < evBracketMatches.Length; i++) Destroy(evBracketMatches[i]);
            evBracketMatches = null;
        }
        menuelhelp = 0;
        if (menuelements != null)
        {
            for (int i = 0; i < menuelements.Length; i++)
            {
                if (menuelements[i] != null) Destroy(menuelements[i]);
            }
        }
        menuelements = null;
        FIGMAMenu.menu.ShowMenu();
    }

    private byte ptom = 127;
    public void PlayerToMove(object o)
    { if (ptom != (byte)(int)o) ptom = (byte)(int)o; else { MenuPlayer(myTeam.players[ptom].nickname); ptom = 127; return; } MenuTeam(); }
    public void PlayerToMoveUp(object o)
    {
        if (ptom == 0 || ptom == 127) return;
        Player temp;
        temp = myTeam.players[ptom];
        myTeam.players.RemoveAt(ptom);
        myTeam.players.Insert(--ptom, GetPlayers().GetPlayer(temp.nickname));
        MenuTeam();
    }

    public void MenuMerch()
    {
        MenuReset();
        menuelements = new GameObject[13];
        FIGMAStructure figma;
        //Sprite flag = (Sprite)Resources.Load("FlagsNew/belgium", typeof(Sprite));
        menuelements[0] = Instantiate(farray, fspawner.transform, false);
        RectTransform rt = (RectTransform)menuelements[0].GetComponent(typeof(RectTransform));
        menuelhelp -= Mathf.RoundToInt(rt.rect.height * 0.5f);
        int m = menuelhelp;
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height);
        figma = (FIGMAStructure)menuelements[0].GetComponentInChildren(typeof(FIGMAStructure));
        //new FIGMAParameter("color", "#2A3C44", 7);
        object[] objects = FIGMAStructure.MakeParams(
            new FIGMAParameter("text", string.Format("{0:## ### ###}", myTeam.GetMoney()) + "$", 0),
            new FIGMAParameter("text", "Money", 1)
        );
        FIGMAStructure.ElementType[] et = new FIGMAStructure.ElementType[]
        {
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle
        };
        figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 2 }, et));
        System.Action<object> action = UpgradeFactory;
        menuelements[1] = Instantiate(farray, fspawner.transform, false);
        rt = (RectTransform)menuelements[1].GetComponent(typeof(RectTransform));
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height);
        figma = (FIGMAStructure)menuelements[1].GetComponentInChildren(typeof(FIGMAStructure));
        //new FIGMAParameter("color", "#2A3C44", 7);
        objects = FIGMAStructure.MakeParams(
            new FIGMAParameter("text", myTeam.GetFactoryLevel().ToString() + "/10", 0),
            new FIGMAParameter("text", "Factory Level", 1),
            new FIGMAParameter("action", new object[] { action, null }, 2),
            new FIGMAParameter("color", "#2A3C44", 3),
            new FIGMAParameter("text", "Upgrade", 4),
            new FIGMAParameter("x", (float)0.0f, 4),
            new FIGMAParameter("y", (float)0.0f, 4)
        );
        et = new FIGMAStructure.ElementType[]
        {
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
            FIGMAStructure.ElementType.Action, FIGMAStructure.ElementType.Background, FIGMAStructure.ElementType.TextValue
        };
        figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 2, 3 }, et));
        menuelements[2] = Instantiate(farray, fspawner.transform, false);
        rt = (RectTransform)menuelements[2].GetComponent(typeof(RectTransform));
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height);
        figma = (FIGMAStructure)menuelements[2].GetComponentInChildren(typeof(FIGMAStructure));
        //new FIGMAParameter("color", "#2A3C44", 7);
        objects = FIGMAStructure.MakeParams(
            new FIGMAParameter("text", myTeam.GetStickers().ToString() + "/" + (myTeam.GetFactoryLevel() * 6250).ToString(), 0),
            new FIGMAParameter("text", "Stickers", 1),
            new FIGMAParameter("color", "#2A3C44", 2),
            new FIGMAParameter("text", myTeam.GetPosters().ToString() + "/" + (myTeam.GetFactoryLevel() * 1250).ToString(), 3),
            new FIGMAParameter("text", "Posters", 4),
            new FIGMAParameter("text", myTeam.GetJerseys().ToString() + "/" + (myTeam.GetFactoryLevel() * 250).ToString(), 5),
            new FIGMAParameter("text", "Jerseys", 6)
        );
        et = new FIGMAStructure.ElementType[]
        {
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
            FIGMAStructure.ElementType.Background, FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle
        };
        figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 2, 3, 2 }, et));
        menuelements[3] = Instantiate(farray, fspawner.transform, false);
        rt = (RectTransform)menuelements[3].GetComponent(typeof(RectTransform));
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height);
        figma = (FIGMAStructure)menuelements[3].GetComponentInChildren(typeof(FIGMAStructure));
        //new FIGMAParameter("color", "#2A3C44", 7);
        objects = FIGMAStructure.MakeParams(
            new FIGMAParameter("text", "0.0625$/1$", 0),
            new FIGMAParameter("text", "Buy/Sell", 1),
            new FIGMAParameter("color", "#2A3C44", 2),
            new FIGMAParameter("text", "9$/30$", 3),
            new FIGMAParameter("text", "Buy/Sell", 4),
            new FIGMAParameter("text", "30$/100$", 5),
            new FIGMAParameter("text", "Buy/Sell", 6)
        );
        et = new FIGMAStructure.ElementType[]
        {
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
            FIGMAStructure.ElementType.Background, FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle
        };
        figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 2, 3, 2 }, et));
        menuelements[4] = Instantiate(finput, fspawner.transform, false);
        rt = (RectTransform)menuelements[4].GetComponent(typeof(RectTransform));
        menuelhelp += 32;
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height + 32);
        FIGMAInput figmai = (FIGMAInput)menuelements[4].GetComponentInChildren(typeof(FIGMAInput));
        figmai.SetAction(new System.Action<string>(SetStickers));
        figmai.SetText("Stickers");
        figmai.SetMode(InputField.ContentType.IntegerNumber);
        menuelements[5] = Instantiate(farray, fspawner.transform, false);
        rt = (RectTransform)menuelements[5].GetComponent(typeof(RectTransform));
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height);
        figma = (FIGMAStructure)menuelements[5].GetComponentInChildren(typeof(FIGMAStructure));
        //new FIGMAParameter("color", "#2A3C44", 7);
        action = BuyStickers;
        objects = FIGMAStructure.MakeParams(
            new FIGMAParameter("action", new object[] { action, null }, 0),
            new FIGMAParameter("text", "Buy", 1),
            new FIGMAParameter("x", (float)0.0f, 1),
            new FIGMAParameter("y", (float)0.0f, 1)
        );
        et = new FIGMAStructure.ElementType[]
        {
            FIGMAStructure.ElementType.Action, FIGMAStructure.ElementType.TextValue,
        };
        figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 2 }, et));
        menuelements[6] = Instantiate(finput, fspawner.transform, false);
        rt = (RectTransform)menuelements[6].GetComponent(typeof(RectTransform));
        menuelhelp += 32;
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height + 32);
        figmai = (FIGMAInput)menuelements[6].GetComponentInChildren(typeof(FIGMAInput));
        figmai.SetAction(new System.Action<string>(SetPosters));
        figmai.SetText("Posters");
        figmai.SetMode(InputField.ContentType.IntegerNumber);
        menuelements[7] = Instantiate(farray, fspawner.transform, false);
        rt = (RectTransform)menuelements[7].GetComponent(typeof(RectTransform));
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height);
        figma = (FIGMAStructure)menuelements[7].GetComponentInChildren(typeof(FIGMAStructure));
        //new FIGMAParameter("color", "#2A3C44", 7);
        action = BuyPosters;
        objects = FIGMAStructure.MakeParams(
            new FIGMAParameter("action", new object[] { action, null }, 0),
            new FIGMAParameter("text", "Buy", 1),
            new FIGMAParameter("x", (float)0.0f, 1),
            new FIGMAParameter("y", (float)0.0f, 1)
        );
        et = new FIGMAStructure.ElementType[]
        {
            FIGMAStructure.ElementType.Action, FIGMAStructure.ElementType.TextValue,
        };
        figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 2 }, et));
        menuelements[8] = Instantiate(finput, fspawner.transform, false);
        rt = (RectTransform)menuelements[8].GetComponent(typeof(RectTransform));
        menuelhelp += 32;
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height + 32);
        figmai = (FIGMAInput)menuelements[8].GetComponentInChildren(typeof(FIGMAInput));
        figmai.SetAction(new System.Action<string>(SetJerseys));
        figmai.SetText("Jerseys");
        figmai.SetMode(InputField.ContentType.IntegerNumber);
        menuelements[9] = Instantiate(farray, fspawner.transform, false);
        rt = (RectTransform)menuelements[9].GetComponent(typeof(RectTransform));
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height);
        figma = (FIGMAStructure)menuelements[9].GetComponentInChildren(typeof(FIGMAStructure));
        //new FIGMAParameter("color", "#2A3C44", 7);
        action = BuyJerseys;
        objects = FIGMAStructure.MakeParams(
            new FIGMAParameter("action", new object[] { action, null }, 0),
            new FIGMAParameter("text", "Buy", 1),
            new FIGMAParameter("x", (float)0.0f, 1),
            new FIGMAParameter("y", (float)0.0f, 1)
        );
        et = new FIGMAStructure.ElementType[]
        {
            FIGMAStructure.ElementType.Action, FIGMAStructure.ElementType.TextValue,
        };
        figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 2 }, et));
        menuelements[10] = Instantiate(fbool, fspawner.transform, false);
        rt = (RectTransform)menuelements[10].GetComponent(typeof(RectTransform));
        menuelhelp += 32;
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height + 32);
        FIGMASwitch figmas = (FIGMASwitch)menuelements[10].GetComponentInChildren(typeof(FIGMASwitch));
        figmas.SetAction(new System.Action<bool>(AutoBuyMerch));
        figmas.SetText("Auto-Buy (15% loss)");
        figmas.SetBool(autoBuyMerch);
        rt = (RectTransform)fspawner.GetComponent(typeof(RectTransform));
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, m - menuelhelp);
    }

    public void MenuContract(object o)
    {
        MenuReset();
        menuelements = new GameObject[2];
        FIGMAStructure figma;
        FIGMAInput figmai;
        RectTransform rt;
        menuelhelp = -64;
        const int m = -64;
        object[] objects;
        FIGMAStructure.ElementType[] et;
        System.Action<object> action = MenuPlayer;
        string text = (string)o;
        if (text == ".")
        {
            menuelements[0] = Instantiate(finput, fspawner.transform, false);
            rt = (RectTransform)menuelements[0].GetComponent(typeof(RectTransform));
            //menuelhelp += 32;
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
            menuelhelp -= Mathf.RoundToInt(rt.rect.height + 32);
            figmai = (FIGMAInput)menuelements[0].GetComponentInChildren(typeof(FIGMAInput));
            figmai.SetAction(MenuContract);
            figmai.SetText("Salary");
            //figmai.SetString(text);
            menuelements[1] = Instantiate(farray, fspawner.transform, false);
            rt = (RectTransform)menuelements[1].GetComponent(typeof(RectTransform));
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
            menuelhelp -= Mathf.RoundToInt(rt.rect.height);
            figma = (FIGMAStructure)menuelements[1].GetComponentInChildren(typeof(FIGMAStructure));
            objects = FIGMAStructure.MakeParams(
                new FIGMAParameter("action", new object[] { null, (string)myTeam.players[myTeam.players.Count - 1].nickname }, 7),
                new FIGMAParameter("text", "Player doesn't agree", 8),
                new FIGMAParameter("x", (float)0.0f, 8),
                new FIGMAParameter("y", (float)0.0f, 8)
                );
            et = new FIGMAStructure.ElementType[]
            {
                FIGMAStructure.ElementType.Action, FIGMAStructure.ElementType.TextValue,
            };
            figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 2 }, et));
            rt = (RectTransform)fspawner.GetComponent(typeof(RectTransform));
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, m - menuelhelp);
            return;
        }
        if (System.Int32.Parse(text) < myTeam.players[myTeam.players.Count - 1].GetValue() * 0.02f)
        {
            menuelements[0] = Instantiate(finput, fspawner.transform, false);
            rt = (RectTransform)menuelements[0].GetComponent(typeof(RectTransform));
            //menuelhelp += 32;
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
            menuelhelp -= Mathf.RoundToInt(rt.rect.height + 32);
            figmai = (FIGMAInput)menuelements[0].GetComponentInChildren(typeof(FIGMAInput));
            figmai.SetAction(MenuContract);
            figmai.SetText("Salary");
            figmai.SetString(text);
            menuelements[1] = Instantiate(farray, fspawner.transform, false);
            rt = (RectTransform)menuelements[1].GetComponent(typeof(RectTransform));
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
            menuelhelp -= Mathf.RoundToInt(rt.rect.height);
            figma = (FIGMAStructure)menuelements[1].GetComponentInChildren(typeof(FIGMAStructure));
            objects = FIGMAStructure.MakeParams(
                new FIGMAParameter("action", new object[] { null, (string)myTeam.players[myTeam.players.Count - 1].nickname }, 7),
                new FIGMAParameter("text", "Player doesn't agree", 8),
                new FIGMAParameter("x", (float)0.0f, 8),
                new FIGMAParameter("y", (float)0.0f, 8)
                );
            et = new FIGMAStructure.ElementType[]
            {
                FIGMAStructure.ElementType.Action, FIGMAStructure.ElementType.TextValue,
            };
            figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 2 }, et));
            rt = (RectTransform)fspawner.GetComponent(typeof(RectTransform));
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, m - menuelhelp);
            return;
        }
        menuelements[0] = Instantiate(finput, fspawner.transform, false);
        rt = (RectTransform)menuelements[0].GetComponent(typeof(RectTransform));
        //menuelhelp += 32;
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height + 32);
        figmai = (FIGMAInput)menuelements[0].GetComponentInChildren(typeof(FIGMAInput));
        figmai.SetAction(MenuContract);
        figmai.SetText("Salary");
        figmai.SetString(text);
        menuelements[1] = Instantiate(farray, fspawner.transform, false);
        rt = (RectTransform)menuelements[1].GetComponent(typeof(RectTransform));
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height);
        figma = (FIGMAStructure)menuelements[1].GetComponentInChildren(typeof(FIGMAStructure));
        objects = FIGMAStructure.MakeParams(
            new FIGMAParameter("action", new object[] { action, (string)myTeam.players[myTeam.players.Count - 1].nickname }, 7),
            new FIGMAParameter("text", "Continue", 8),
            new FIGMAParameter("x", (float)0.0f, 8),
            new FIGMAParameter("y", (float)0.0f, 8)
            );
        et = new FIGMAStructure.ElementType[]
        {
                FIGMAStructure.ElementType.Action, FIGMAStructure.ElementType.TextValue,
        };
        figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 2 }, et));
        rt = (RectTransform)fspawner.GetComponent(typeof(RectTransform));
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, m - menuelhelp);
        myTeam.players[myTeam.players.Count - 1].salary = System.Int32.Parse(text);
        myTeam.players[myTeam.players.Count - 1].daysPayed = 30;
        //contractButton.SetActive(true);
        //contractApproved.SetActive(true);
    }

    public void MenuCreateTeam(string text) { MenuCreateTeam((object)text); }
    public void MenuCreateTeam(object o)
    {
        MenuReset();
        menuelements = new GameObject[10];
        FIGMAStructure figma;
        FIGMAInput figmai;
        RectTransform rt;
        menuelhelp = -64;
        const int m = -64;
        object[] objects;
        FIGMAStructure.ElementType[] et;
        System.Action<object> action = CreateTeam;
        string text = (string)o;
        if (text.Length < 2 || text == ".")
        {
            menuelements[0] = Instantiate(finput, fspawner.transform, false);
            rt = (RectTransform)menuelements[0].GetComponent(typeof(RectTransform));
            //menuelhelp += 32;
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
            menuelhelp -= Mathf.RoundToInt(rt.rect.height + 32);
            figmai = (FIGMAInput)menuelements[0].GetComponentInChildren(typeof(FIGMAInput));
            figmai.SetAction(MenuCreateTeam);
            figmai.SetText("Teamname");
            figmai.SetString(text);
            menuelements[1] = Instantiate(farray, fspawner.transform, false);
            rt = (RectTransform)menuelements[1].GetComponent(typeof(RectTransform));
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
            menuelhelp -= Mathf.RoundToInt(rt.rect.height);
            figma = (FIGMAStructure)menuelements[1].GetComponentInChildren(typeof(FIGMAStructure));
            objects = FIGMAStructure.MakeParams(
                new FIGMAParameter("action", new object[] { null, (string)text }, 7),
                new FIGMAParameter("text", "Teamname should be at least 2 characters long", 8),
                new FIGMAParameter("x", (float)0.0f, 8),
                new FIGMAParameter("y", (float)0.0f, 8)
                );
            et = new FIGMAStructure.ElementType[]
            {
                FIGMAStructure.ElementType.Action, FIGMAStructure.ElementType.TextValue,
            };
            figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 2 }, et));
            rt = (RectTransform)fspawner.GetComponent(typeof(RectTransform));
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, m - menuelhelp);
            return;
        }
        menuelements[0] = Instantiate(finput, fspawner.transform, false);
        rt = (RectTransform)menuelements[0].GetComponent(typeof(RectTransform));
        //menuelhelp += 32;
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height + 32);
        figmai = (FIGMAInput)menuelements[0].GetComponentInChildren(typeof(FIGMAInput));
        figmai.SetAction(MenuCreateTeam);
        figmai.SetText("Teamname");
        figmai.SetString(text);
        menuelements[1] = Instantiate(farray, fspawner.transform, false);
        rt = (RectTransform)menuelements[1].GetComponent(typeof(RectTransform));
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height);
        figma = (FIGMAStructure)menuelements[1].GetComponentInChildren(typeof(FIGMAStructure));
        objects = FIGMAStructure.MakeParams(
            new FIGMAParameter("action", new object[] { action, (string)text }, 7),
            new FIGMAParameter("text", "Continue", 8),
            new FIGMAParameter("x", (float)0.0f, 8),
            new FIGMAParameter("y", (float)0.0f, 8)
            );
        et = new FIGMAStructure.ElementType[]
        {
            FIGMAStructure.ElementType.Action, FIGMAStructure.ElementType.TextValue,
        };
        figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 2 }, et));
        rt = (RectTransform)fspawner.GetComponent(typeof(RectTransform));
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, m - menuelhelp);
        myTeam.players[myTeam.players.Count - 1].salary = System.Int32.Parse(text);
        myTeam.players[myTeam.players.Count - 1].daysPayed = 30;
        //contractButton.SetActive(true);
        //contractApproved.SetActive(true);
    }

    public void MenuLeague()
    {
        MenuReset();
        menuelements = new GameObject[10];
        FIGMAStructure figma;
        RectTransform rt;
        menuelhelp = -96;
        const int m = -96;
        object[] objects;
        FIGMAStructure.ElementType[] et;
        System.Action<object> action = LeagueContract;
        for (int i = 0; i < League.players.Count; i++)
        {
            menuelements[i] = Instantiate(farray, fspawner.transform, false);
            rt = (RectTransform)menuelements[i].GetComponent(typeof(RectTransform));
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
            menuelhelp -= Mathf.RoundToInt(rt.rect.height);
            figma = (FIGMAStructure)menuelements[i].GetComponentInChildren(typeof(FIGMAStructure));
            objects = FIGMAStructure.MakeParams(
                new FIGMAParameter("text", League.players[i].nickname + ", " + League.players[i].age, 0),
                new FIGMAParameter("text", "Player", 1),
                new FIGMAParameter("sprite", nations[(int)League.players[i].nationality].sprite, 2),
                new FIGMAParameter("sprite", roles[(int)League.players[i].role].sprite, 3),
                new FIGMAParameter("color", "#2A3C44", 4),
                new FIGMAParameter("text", League.players[i].strength.ToString(), 5),
                new FIGMAParameter("text", "Strength", 6),
                new FIGMAParameter("action", new object[] { action, (int)i }, 7),
                new FIGMAParameter("text", "Hire", 8),
                new FIGMAParameter("x", (float)0.0f, 8),
                new FIGMAParameter("y", (float)0.0f, 8)
                );
            et = new FIGMAStructure.ElementType[]
            {
                FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle, FIGMAStructure.ElementType.Flag,
                FIGMAStructure.ElementType.Flag, FIGMAStructure.ElementType.Background, FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
                FIGMAStructure.ElementType.Action, FIGMAStructure.ElementType.TextValue, 
            };
            figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 3, 4, 2 }, et));
        }
        rt = (RectTransform)fspawner.GetComponent(typeof(RectTransform));
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, m - menuelhelp);
    }

    public void MenuTeam()
    {
        MenuReset();
        menuelements = new GameObject[13];
        FIGMAStructure figma;
        //Sprite flag = (Sprite)Resources.Load("FlagsNew/belgium", typeof(Sprite));
        menuelements[0] = Instantiate(farray, fspawner.transform, false);
        RectTransform rt = (RectTransform)menuelements[0].GetComponent(typeof(RectTransform));
        menuelhelp -= Mathf.RoundToInt(rt.rect.height * 0.5f);
        int m = menuelhelp;
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height);
        figma = (FIGMAStructure)menuelements[0].GetComponentInChildren(typeof(FIGMAStructure));
        //new FIGMAParameter("color", "#2A3C44", 7);
        object[] objects;
        FIGMAStructure.ElementType[] et;
        Sprite sprite = Modding.modding.GetLogo(myTeam.teamName);
        if (sprite == null)
        {
            objects = FIGMAStructure.MakeParams(
            new FIGMAParameter("text", myTeam.teamName + ", #" + definedTeams.GetTeamPlacement(myTeam.teamName).ToString(), 0),
            new FIGMAParameter("text", "Team", 1),
            new FIGMAParameter("text", "Leave Team", 3),
            new FIGMAParameter("text", "Explore other options", 4),
            new FIGMAParameter("action", new object[] { new System.Action<object>(MenuChooseTA), null }, 5),
            new FIGMAParameter("color", "#2A3C44", 7),
            new FIGMAParameter("text", string.Format("{0:## ### ###}", myTeam.GetMoney()) + "$", 8),
            new FIGMAParameter("text", "Money", 9)
            );
            et = new FIGMAStructure.ElementType[]
            {
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle, FIGMAStructure.ElementType.Action, FIGMAStructure.ElementType.Background,
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle
            };
            figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 2, 4, 2 }, et));
        }
        else
        {
            objects = FIGMAStructure.MakeParams(
            new FIGMAParameter("text", myTeam.teamName + ", #" + definedTeams.GetTeamPlacement(myTeam.teamName).ToString(), 0),
            new FIGMAParameter("text", "Team", 1),
            new FIGMAParameter("sprite", sprite, 2),
            new FIGMAParameter("text", "Leave Team", 3),
            new FIGMAParameter("text", "Explore other options", 4),
            new FIGMAParameter("action", new object[] { new System.Action<object>(MenuChooseTA), null }, 5),
            new FIGMAParameter("color", "#2A3C44", 7),
            new FIGMAParameter("text", string.Format("{0:## ### ###}", myTeam.GetMoney()) + "$", 8),
            new FIGMAParameter("text", "Money", 9)
            );
            et = new FIGMAStructure.ElementType[]
            {
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle, FIGMAStructure.ElementType.Flag,
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle, FIGMAStructure.ElementType.Action, FIGMAStructure.ElementType.Background,
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle
            };
            figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 3, 4, 2 }, et));
        }
        
        System.Action<object> action = SellPlayer;
        for (int i = 0; i < myTeam.players.Count; i++)
        {
            menuelements[i + 1] = Instantiate(farray, fspawner.transform, false);
            rt = (RectTransform)menuelements[i + 1].GetComponent(typeof(RectTransform));
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
            menuelhelp -= Mathf.RoundToInt(rt.rect.height);
            figma = (FIGMAStructure)menuelements[i + 1].GetComponentInChildren(typeof(FIGMAStructure));
            //action = SellPlayer;
            System.Action<object> action2 = KickPlayer;
            System.Action<object> action3 = PlayerToMove;
            objects = FIGMAStructure.MakeParams(
                new FIGMAParameter("text", myTeam.players[i].nickname + ", " + myTeam.players[i].age, 0),
                new FIGMAParameter("text", "Player", 1),
                new FIGMAParameter("action", new object[] { action3, (int)i }, 2),
                new FIGMAParameter("color", ptom == i ? "#3DD598" : "#30444E", 3),
                new FIGMAParameter("sprite", roles[(int)myTeam.players[i].role].sprite, 4),
                new FIGMAParameter("color", "#2A3C44", 5),
                new FIGMAParameter("text", myTeam.players[i].strength.ToString(), 6),
                new FIGMAParameter("x", (float)0.0f, 6),
                new FIGMAParameter("y", (float)0.0f, 6),
                new FIGMAParameter("sprite", fsprite[0], 7),
                new FIGMAParameter("action", new object[] { action, (int)i }, 8),
                new FIGMAParameter("sprite", fsprite[1], 9),
                new FIGMAParameter("color", "#2A3C44", 10),
                new FIGMAParameter("action", new object[] { action2, (int)i }, 11)
                );
            et = new FIGMAStructure.ElementType[]
            {
                FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle, FIGMAStructure.ElementType.Action, FIGMAStructure.ElementType.Background,
                FIGMAStructure.ElementType.Flag, FIGMAStructure.ElementType.Background, FIGMAStructure.ElementType.TextValue,
                FIGMAStructure.ElementType.Icon, FIGMAStructure.ElementType.Action,
                FIGMAStructure.ElementType.Icon, FIGMAStructure.ElementType.Background, FIGMAStructure.ElementType.Action,
            };
            figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 4, 3, 2, 3 }, et));
        }
        action = PlayerToMoveUp;
        menuelements[12] = Instantiate(farray, fspawner.transform, false);
        rt = (RectTransform)menuelements[12].GetComponent(typeof(RectTransform));
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height);
        figma = (FIGMAStructure)menuelements[12].GetComponentInChildren(typeof(FIGMAStructure));
        objects = FIGMAStructure.MakeParams(
            new FIGMAParameter("text", "Move higher", 0),
            new FIGMAParameter("x", (float)0.0f, 0),
            new FIGMAParameter("y", (float)0.0f, 0),
            new FIGMAParameter("action", new object[] { action, (int)0 }, 1)
            );
        et = new FIGMAStructure.ElementType[]
        {
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Action
        };
        figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 2 }, et));
        rt = (RectTransform)fspawner.GetComponent(typeof(RectTransform));
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, m - menuelhelp);
    }

    public void MenuAnyTeam(object o)
    {
        MenuReset();
        Team myTeam = definedTeams.GetTeam((string)o);
        menuelements = new GameObject[12];
        FIGMAStructure figma;
        //Sprite flag = (Sprite)Resources.Load("FlagsNew/belgium", typeof(Sprite));
        menuelements[0] = Instantiate(farray, fspawner.transform, false);
        RectTransform rt = (RectTransform)menuelements[0].GetComponent(typeof(RectTransform));
        menuelhelp -= Mathf.RoundToInt(rt.rect.height * 0.5f);
        int m = menuelhelp;
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height);
        figma = (FIGMAStructure)menuelements[0].GetComponentInChildren(typeof(FIGMAStructure));
        //new FIGMAParameter("color", "#2A3C44", 7);
        object[] objects;
        FIGMAStructure.ElementType[] et;
        Sprite sprite = Modding.modding.GetLogo(myTeam.teamName);
        if (sprite == null)
        {
            objects = FIGMAStructure.MakeParams(
                new FIGMAParameter("text", myTeam.teamName + ", #" + definedTeams.GetTeamPlacement(myTeam.teamName).ToString(), 0),
                new FIGMAParameter("text", "Team", 1)
            );
            et = new FIGMAStructure.ElementType[]
            {
                FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
            };
            figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 2 }, et));
        }
        else
        {
            objects = FIGMAStructure.MakeParams(
                new FIGMAParameter("text", myTeam.teamName + ", #" + definedTeams.GetTeamPlacement(myTeam.teamName).ToString(), 0),
                new FIGMAParameter("text", "Team", 1),
                new FIGMAParameter("sprite", sprite, 2)
            );
            et = new FIGMAStructure.ElementType[]
            {
                FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle, FIGMAStructure.ElementType.Flag
            };
            figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 3 }, et));
        }
        System.Action<object> action = MenuPlayer;
        for (int i = 0; i < myTeam.players.Count; i++)
        {
            menuelements[i + 1] = Instantiate(farray, fspawner.transform, false);
            rt = (RectTransform)menuelements[i + 1].GetComponent(typeof(RectTransform));
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
            menuelhelp -= Mathf.RoundToInt(rt.rect.height);
            figma = (FIGMAStructure)menuelements[i + 1].GetComponentInChildren(typeof(FIGMAStructure));
            objects = FIGMAStructure.MakeParams(
                new FIGMAParameter("text", myTeam.players[i].nickname + ", " + myTeam.players[i].age, 0),
                new FIGMAParameter("text", "Player", 1),
                new FIGMAParameter("action", new object[] { action, myTeam.players[i].nickname }, 2),
                new FIGMAParameter("color", ptom == i ? "#3DD598" : "#30444E", 3),
                new FIGMAParameter("sprite", roles[(int)myTeam.players[i].role].sprite, 4),
                new FIGMAParameter("color", "#2A3C44", 5),
                new FIGMAParameter("text", myTeam.players[i].FullStat(30).rating.ToString("0.00"), 6),
                new FIGMAParameter("text", "Rating", 7)
                );
            et = new FIGMAStructure.ElementType[]
            {
                FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle, FIGMAStructure.ElementType.Action, FIGMAStructure.ElementType.Background,
                FIGMAStructure.ElementType.Flag, FIGMAStructure.ElementType.Background, FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle
            };
            figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 4, 4 }, et));
        }
        rt = (RectTransform)fspawner.GetComponent(typeof(RectTransform));
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, m - menuelhelp);
    }

    public void MenuPlayer(object _player)
    {
        MenuReset();
        string player = (string)_player;
        menuelements = new GameObject[7];
        FIGMAStructure figma;
        describePlayer = definedPlayers.GetPlayer(player);
        nickSel = describePlayer.nickname;
        tSel = definedTeams.GetTeam(describePlayer.teamName);
        Player p = describePlayer;
        Stat stat = p.FullStat(30);
        //Sprite flag = (Sprite)Resources.Load("FlagsNew/belgium", typeof(Sprite));
        menuelements[0] = Instantiate(farray, fspawner.transform, false);
        RectTransform rt = (RectTransform)menuelements[0].GetComponent(typeof(RectTransform));
        menuelhelp -= Mathf.RoundToInt(rt.rect.height * 0.5f);
        int m = menuelhelp;
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height);
        figma = (FIGMAStructure)menuelements[0].GetComponentInChildren(typeof(FIGMAStructure));
        System.Action<object> action = MenuAnyTeam;
        object[] objects;
        FIGMAStructure.ElementType[] et;
        Sprite sprite = Modding.modding.GetLogo(player);
        if (sprite == null)
        {
            objects = FIGMAStructure.MakeParams(
                new FIGMAParameter("sprite", nations[(int)p.nationality].sprite, 0),
                new FIGMAParameter("text", player + ", " + p.age.ToString(), 1),
                new FIGMAParameter("text", "Nickname", 2),
                new FIGMAParameter("action", new object[] { action, p.teamName }, 4),
                new FIGMAParameter("text", p.teamName + ", #" + definedTeams.GetTeamPlacement(p.teamName).ToString(), 5),
                new FIGMAParameter("color", "#2A3C44", 6),
                new FIGMAParameter("text", "Team", 7),
                new FIGMAParameter("sprite", roles[(int)p.role].sprite, 8)
            );
            et = new FIGMAStructure.ElementType[]
            {
                FIGMAStructure.ElementType.Flag, FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle, FIGMAStructure.ElementType.Action,
                FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Background, FIGMAStructure.ElementType.Subtitle, FIGMAStructure.ElementType.Flag
            };
            figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 3, 5 }, et));
        }
        else
        {
            objects = FIGMAStructure.MakeParams(
                new FIGMAParameter("sprite", nations[(int)p.nationality].sprite, 0),
                new FIGMAParameter("text", player + ", " + p.age.ToString(), 1),
                new FIGMAParameter("text", "Nickname", 2),
                new FIGMAParameter("sprite", sprite, 3),
                new FIGMAParameter("action", new object[] { action, p.teamName }, 4),
                new FIGMAParameter("text", p.teamName + ", #" + definedTeams.GetTeamPlacement(p.teamName).ToString(), 5),
                new FIGMAParameter("color", "#2A3C44", 6),
                new FIGMAParameter("text", "Team", 7),
                new FIGMAParameter("sprite", roles[(int)p.role].sprite, 8)
            );
            et = new FIGMAStructure.ElementType[]
            {
                FIGMAStructure.ElementType.Icon, FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle, FIGMAStructure.ElementType.Flag, FIGMAStructure.ElementType.Action,
                FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Background, FIGMAStructure.ElementType.Subtitle, FIGMAStructure.ElementType.Flag
            };
            figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 4, 5 }, et));
        }
        //System.Action<object> action;
        List<FIGMAParameter> os = new List<FIGMAParameter>(105);
        List<FIGMAStructure.ElementType> ets = new List<FIGMAStructure.ElementType>(105);
        List<byte> byter = new List<byte>(10);
        List<int> shiso = new List<int>(10);
        os.Add(new FIGMAParameter("sprite", fsprite[5], 0));
        os.Add(new FIGMAParameter("text", p.mvp.ToString(), 1));
        os.Add(new FIGMAParameter("text", "MVPs", 2));
        ets.Add(FIGMAStructure.ElementType.Flag);
        ets.Add(FIGMAStructure.ElementType.TextValue);
        ets.Add(FIGMAStructure.ElementType.Subtitle);
        byter.Add(3);
        shiso.Add(192);
        var tt = p.GetTop20Appearances();
        if (tt == null) tt = new List<BestPlayer>(0);
        byte pa = 3;
        for (int i = 0; i < tt.Count; i++)
        {
            if ((i & 1) == 0)
            {
                os.Add(new FIGMAParameter("text", "#" + tt[i].place.ToString(), (byte)(pa)));
                os.Add(new FIGMAParameter("text", "'" + tt[i].year.ToString(), (byte)(pa + 1)));
                os.Add(new FIGMAParameter("color", "#2A3C44", (byte)(pa + 2)));
                ets.Add(FIGMAStructure.ElementType.TextValue);
                ets.Add(FIGMAStructure.ElementType.Subtitle);
                ets.Add(FIGMAStructure.ElementType.Background);
                byter.Add(3);
                shiso.Add(192);
                pa += 3;
            }
            else
            {
                os.Add(new FIGMAParameter("text", "#" + tt[i].place.ToString(), (byte)(pa)));
                os.Add(new FIGMAParameter("text", "'" + tt[i].year.ToString(), (byte)(pa + 1)));
                ets.Add(FIGMAStructure.ElementType.TextValue);
                ets.Add(FIGMAStructure.ElementType.Subtitle);
                byter.Add(2);
                shiso.Add(192);
                pa += 2;
            }
        }
        menuelements[1] = Instantiate(farray, fspawner.transform, false);
        rt = (RectTransform)menuelements[1].GetComponent(typeof(RectTransform));
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height);
        figma = (FIGMAStructure)menuelements[1].GetComponentInChildren(typeof(FIGMAStructure));
        objects = FIGMAStructure.MakeParamsA(os.ToArray());
        figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, byter.ToArray(), ets.ToArray()), shiso.ToArray());
        menuelements[2] = Instantiate(farray, fspawner.transform, false);
        rt = (RectTransform)menuelements[2].GetComponent(typeof(RectTransform));
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height);
        figma = (FIGMAStructure)menuelements[2].GetComponentInChildren(typeof(FIGMAStructure));
        objects = FIGMAStructure.MakeParams(
            new FIGMAParameter("text", (stat.kills / (double)stat.rounds).ToString("0.00"), 0),
            new FIGMAParameter("text", "KPR", 1),
            new FIGMAParameter("color", "#2A3C44", 2),
            new FIGMAParameter("text", (stat.deaths / (double)stat.rounds).ToString("0.00"), 3),
            new FIGMAParameter("text", "DPR", 4),
            new FIGMAParameter("text", (stat.impact).ToString("0.00"), 5),
            new FIGMAParameter("text", "Impact", 6),
            new FIGMAParameter("color", "#2A3C44", 7),
            new FIGMAParameter("text", (stat.damage / (double)stat.rounds) < 100 ? (stat.damage / (double)stat.rounds).ToString("00.0") : (stat.damage / (double)stat.rounds).ToString("000.0"), 8),
            new FIGMAParameter("text", "ADR", 9),
            new FIGMAParameter("text", Mathf.Approximately(stat.kast, 1) ? "100.0%" : stat.kast.ToString("00.0%"), 10),
            new FIGMAParameter("text", "KAST", 11),
            new FIGMAParameter("color", "#2A3C44", 12),
            new FIGMAParameter("text", (stat.kills / (double)stat.deaths).ToString("0.00"), 13),
            new FIGMAParameter("text", "KDR", 14),
            new FIGMAParameter("text", (stat.rating).ToString("0.00"), 15),
            new FIGMAParameter("text", "Rating", 16),
            new FIGMAParameter("color", "#2A3C44", 17)
            );
        et = new FIGMAStructure.ElementType[]
        {
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle, FIGMAStructure.ElementType.Background,
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle, FIGMAStructure.ElementType.Background,
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle, FIGMAStructure.ElementType.Background,
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle, FIGMAStructure.ElementType.Background
        };
        figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 3, 2, 3, 2, 3, 2, 3 }, et));
        menuelements[3] = Instantiate(farray, fspawner.transform, false);
        rt = (RectTransform)menuelements[3].GetComponent(typeof(RectTransform));
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height);
        figma = (FIGMAStructure)menuelements[3].GetComponentInChildren(typeof(FIGMAStructure));
        objects = FIGMAStructure.MakeParams(
            new FIGMAParameter("text", p.awp.ToString(), 0),
            new FIGMAParameter("text", "AWP", 1),
            new FIGMAParameter("text", p.clutching.ToString(), 2),
            new FIGMAParameter("text", "Clutch", 3),
            new FIGMAParameter("color", "#2A3C44", 4),
            new FIGMAParameter("text", p.ct.ToString(), 5),
            new FIGMAParameter("text", "CT", 6),
            new FIGMAParameter("text", p.entring.ToString(), 7),
            new FIGMAParameter("text", "Entry", 8),
            new FIGMAParameter("color", "#2A3C44", 9),
            new FIGMAParameter("text", p.killing.ToString(), 10),
            new FIGMAParameter("text", "Killing", 11),
            new FIGMAParameter("text", p.rifle.ToString(), 12),
            new FIGMAParameter("text", "Rifle", 13),
            new FIGMAParameter("color", "#2A3C44", 14),
            new FIGMAParameter("text", p.t.ToString(), 15),
            new FIGMAParameter("text", "T", 16),
            new FIGMAParameter("text", p.strength.ToString(), 17),
            new FIGMAParameter("text", "Overall", 18),
            new FIGMAParameter("color", "#2A3C44", 19)
            );
        et = new FIGMAStructure.ElementType[]
        {
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle, FIGMAStructure.ElementType.Background,
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle, FIGMAStructure.ElementType.Background,
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle, FIGMAStructure.ElementType.Background,
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle, FIGMAStructure.ElementType.Background,
        };
        figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 2, 3, 2, 3, 2, 3, 2, 3 }, et));
        menuelements[4] = Instantiate(farray, fspawner.transform, false);
        rt = (RectTransform)menuelements[4].GetComponent(typeof(RectTransform));
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height);
        figma = (FIGMAStructure)menuelements[4].GetComponentInChildren(typeof(FIGMAStructure));
        objects = FIGMAStructure.MakeParams(
            new FIGMAParameter("text", stat.kills.ToString(), 0),
            new FIGMAParameter("text", "Kills", 1),
            new FIGMAParameter("color", "#2A3C44", 2),
            new FIGMAParameter("text", stat.deaths.ToString(), 3),
            new FIGMAParameter("text", "Deaths", 4),
            new FIGMAParameter("text", stat.x1.ToString(), 5),
            new FIGMAParameter("text", "1 kills", 6),
            new FIGMAParameter("color", "#2A3C44", 7),
            new FIGMAParameter("text", stat.x2.ToString(), 8),
            new FIGMAParameter("text", "2 kills", 9),
            new FIGMAParameter("text", stat.x3.ToString(), 10),
            new FIGMAParameter("text", "3 kills", 11),
            new FIGMAParameter("color", "#2A3C44", 12),
            new FIGMAParameter("text", stat.x4.ToString(), 13),
            new FIGMAParameter("text", "4 kills", 14),
            new FIGMAParameter("text", stat.x5.ToString(), 15),
            new FIGMAParameter("text", "5 kills", 16),
            new FIGMAParameter("color", "#2A3C44", 17),
            new FIGMAParameter("text", stat.rounds.ToString(), 18),
            new FIGMAParameter("text", "Rounds", 19)
            );
        et = new FIGMAStructure.ElementType[]
        {
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle, FIGMAStructure.ElementType.Background,
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle, FIGMAStructure.ElementType.Background,
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle, FIGMAStructure.ElementType.Background,
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle, FIGMAStructure.ElementType.Background,
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
        };
        figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 3, 2, 3, 2, 3, 2, 3, 2 }, et));
        if (myTeam.teamName != p.teamName)
        {
            menuelements[5] = Instantiate(finput, fspawner.transform, false);
            rt = (RectTransform)menuelements[5].GetComponent(typeof(RectTransform));
            menuelhelp += 32;
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
            menuelhelp -= Mathf.RoundToInt(rt.rect.height + 32);
            FIGMAInput figmai = (FIGMAInput)menuelements[5].GetComponentInChildren(typeof(FIGMAInput));
            figmai.SetAction(BuyPlayerPrice);
            figmai.SetMode(InputField.ContentType.IntegerNumber);
            figmai.SetText("Price");
            menuelements[6] = Instantiate(farray, fspawner.transform, false);
            rt = (RectTransform)menuelements[6].GetComponent(typeof(RectTransform));
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
            menuelhelp -= Mathf.RoundToInt(rt.rect.height);
            figma = (FIGMAStructure)menuelements[6].GetComponentInChildren(typeof(FIGMAStructure));
            action = BuyPlayer;
            objects = FIGMAStructure.MakeParams(
                new FIGMAParameter("text", "Make an offer", 0),
                new FIGMAParameter("x", (float)0.0f, 0),
                new FIGMAParameter("y", (float)0.0f, 0),
                new FIGMAParameter("action", new object[] { action, null }, 1)
                );
            et = new FIGMAStructure.ElementType[]
            {
                FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Action
            };
            figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 2 }, et));
        }
        rt = (RectTransform)fspawner.GetComponent(typeof(RectTransform));
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, m - menuelhelp);
    }

    public void MenuEditor()
    {
        MenuReset();
        menuelements = new GameObject[10];
        FIGMAStructure figma;
        FIGMAInput figmai;
        RectTransform rt;
        menuelhelp = -64;
        const int m = -64;
        object[] objects;
        FIGMAStructure.ElementType[] et;
        System.Action<object> action;
        //string text = (string)o;
        /*edTeam[0].SetTextWithoutNotify(editorTeam.teamName);
        edTeam[1].SetTextWithoutNotify(editorTeam.GetMoney().ToString());
        //edTeam[2].SetTextWithoutNotify(editorTeam.points.ToString());
        edTeam[3].SetTextWithoutNotify(editorTeam.followers.ToString());
        edPlayer[0].SetTextWithoutNotify(editorPlayer.nickname);
        edPlayer[1].SetTextWithoutNotify(editorPlayer.strength.ToString());
        edPlayer[2].SetTextWithoutNotify(editorPlayer.age.ToString());
        edPlayerD[0].SetValueWithoutNotify((int)editorPlayer.role);
        ednation = (byte)editorPlayer.nationality;
        edImage.sprite = nations[ednation].sprite;*/
        menuelements[0] = Instantiate(finput, fspawner.transform, false);
        rt = (RectTransform)menuelements[0].GetComponent(typeof(RectTransform));
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height);
        figmai = (FIGMAInput)menuelements[0].GetComponentInChildren(typeof(FIGMAInput));
        figmai.SetAction(EditorTeam);
        figmai.SetText("Team");
        if (editorTeam != null)
        {
            figmai.SetString(editorTeam.teamName);
            //figmai.SetString(text);
            menuelements[1] = Instantiate(finput, fspawner.transform, false);
            rt = (RectTransform)menuelements[1].GetComponent(typeof(RectTransform));
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
            menuelhelp -= Mathf.RoundToInt(rt.rect.height);
            figmai = (FIGMAInput)menuelements[1].GetComponentInChildren(typeof(FIGMAInput));
            figmai.SetAction(EditTeamTeamname);
            figmai.SetText("Teamname");
            figmai.SetString(editorTeam.teamName);
            menuelements[2] = Instantiate(finput, fspawner.transform, false);
            rt = (RectTransform)menuelements[2].GetComponent(typeof(RectTransform));
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
            menuelhelp -= Mathf.RoundToInt(rt.rect.height);
            figmai = (FIGMAInput)menuelements[2].GetComponentInChildren(typeof(FIGMAInput));
            figmai.SetAction(EditTeamMoney);
            figmai.SetMode(InputField.ContentType.IntegerNumber);
            figmai.SetText("Money");
            figmai.SetString(editorTeam.GetMoney().ToString());
            menuelements[3] = Instantiate(finput, fspawner.transform, false);
            rt = (RectTransform)menuelements[3].GetComponent(typeof(RectTransform));
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
            menuelhelp -= Mathf.RoundToInt(rt.rect.height);
            figmai = (FIGMAInput)menuelements[3].GetComponentInChildren(typeof(FIGMAInput));
            figmai.SetAction(EditTeamFollowers);
            figmai.SetMode(InputField.ContentType.IntegerNumber);
            figmai.SetText("Followers");
            figmai.SetString(editorTeam.followers.ToString());
        }
        menuelements[4] = Instantiate(finput, fspawner.transform, false);
        rt = (RectTransform)menuelements[4].GetComponent(typeof(RectTransform));
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height);
        figmai = (FIGMAInput)menuelements[4].GetComponentInChildren(typeof(FIGMAInput));
        figmai.SetAction(EditorPlayer);
        figmai.SetText("Player");
        if (editorPlayer != null)
        {
            figmai.SetString(editorPlayer.nickname);
            //figmai.SetString(text);
            menuelements[5] = Instantiate(finput, fspawner.transform, false);
            rt = (RectTransform)menuelements[5].GetComponent(typeof(RectTransform));
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
            menuelhelp -= Mathf.RoundToInt(rt.rect.height);
            figmai = (FIGMAInput)menuelements[5].GetComponentInChildren(typeof(FIGMAInput));
            figmai.SetAction(EditPlayerNickname);
            figmai.SetText("Nickname");
            figmai.SetString(editorPlayer.nickname);
            menuelements[6] = Instantiate(finput, fspawner.transform, false);
            rt = (RectTransform)menuelements[6].GetComponent(typeof(RectTransform));
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
            menuelhelp -= Mathf.RoundToInt(rt.rect.height);
            figmai = (FIGMAInput)menuelements[6].GetComponentInChildren(typeof(FIGMAInput));
            figmai.SetAction(EditPlayerStrength);
            figmai.SetMode(InputField.ContentType.IntegerNumber);
            figmai.SetText("Strength");
            figmai.SetString(editorPlayer.strength.ToString());
            menuelements[7] = Instantiate(finput, fspawner.transform, false);
            rt = (RectTransform)menuelements[7].GetComponent(typeof(RectTransform));
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
            menuelhelp -= Mathf.RoundToInt(rt.rect.height + 32);
            figmai = (FIGMAInput)menuelements[7].GetComponentInChildren(typeof(FIGMAInput));
            figmai.SetAction(EditPlayerAge);
            figmai.SetMode(InputField.ContentType.IntegerNumber);
            figmai.SetText("Age");
            figmai.SetString(editorPlayer.age.ToString());
            //
            menuelements[8] = Instantiate(farray, fspawner.transform, false);
            rt = (RectTransform)menuelements[8].GetComponent(typeof(RectTransform));
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
            menuelhelp -= Mathf.RoundToInt(rt.rect.height - 32);
            figma = (FIGMAStructure)menuelements[8].GetComponentInChildren(typeof(FIGMAStructure));
            action = EditPlayerNationality;
            objects = FIGMAStructure.MakeParams(
                new FIGMAParameter("sprite", fsprite[2], 0),
                new FIGMAParameter("action", new object[] { action, (int)-1 }, 1),
                new FIGMAParameter("sprite", nations[(int)editorPlayer.nationality].sprite, 2),
                new FIGMAParameter("sprite", fsprite[3], 3),
                new FIGMAParameter("action", new object[] { action, (int)1 }, 4)
            );
            et = new FIGMAStructure.ElementType[]
            {
                FIGMAStructure.ElementType.Icon, FIGMAStructure.ElementType.Action,
                FIGMAStructure.ElementType.Icon,
                FIGMAStructure.ElementType.Icon, FIGMAStructure.ElementType.Action
            };
            figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 2, 1, 2 }, et));
            menuelements[9] = Instantiate(farray, fspawner.transform, false);
            rt = (RectTransform)menuelements[9].GetComponent(typeof(RectTransform));
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
            menuelhelp -= Mathf.RoundToInt(rt.rect.height);
            figma = (FIGMAStructure)menuelements[9].GetComponentInChildren(typeof(FIGMAStructure));
            action = EditPlayerRole;
            objects = FIGMAStructure.MakeParams(
                new FIGMAParameter("sprite", fsprite[2], 0),
                new FIGMAParameter("action", new object[] { action, (int)-1 }, 1),
                new FIGMAParameter("sprite", roles[(int)editorPlayer.role].sprite, 2),
                new FIGMAParameter("sprite", fsprite[3], 3),
                new FIGMAParameter("action", new object[] { action, (int)1 }, 4)
            );
            et = new FIGMAStructure.ElementType[]
            {
                FIGMAStructure.ElementType.Icon, FIGMAStructure.ElementType.Action,
                FIGMAStructure.ElementType.Icon,
                FIGMAStructure.ElementType.Icon, FIGMAStructure.ElementType.Action
            };
            figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 2, 1, 2 }, et));
        }
        rt = (RectTransform)fspawner.GetComponent(typeof(RectTransform));
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, m - menuelhelp);
    }

    public class ShowMatch
    {
        public Team team1, team2;

        public void Init()
        { 
            team1 = new Team();
            team1.currentTournament = -1;
            team1.teamName = string.Empty;
            team1.followers = 0;
            team1.players = new List<Player>(5);
            for (int i = 0; i < 5; i++)
            { team1.players.Add(new Player()); team1.players[i].strength = 70; team1.players[i].role = 0; team1.players[i].stats = new List<int>(); }
            team1.academy = new Academy();
            team2 = new Team();
            team2.currentTournament = -1;
            team2.teamName = string.Empty;
            team2.followers = 0;
            team2.players = new List<Player>(5);
            for (int i = 0; i < 5; i++)
            { team2.players.Add(new Player()); team2.players[i].strength = 70; team2.players[i].role = 0; team2.players[i].stats = new List<int>(); }
            team2.academy = new Academy();
        }
        public void Nickname(object _nickname)
        {
            string nickname = (string)_nickname;
            int t = nickname[0] - 48;
            if (t < 5)
            {
                team1.players[t].nickname = nickname.Substring(1, nickname.Length - 1);
                team1.players[t].GeneratePlayer();
            }
            else
            {
                team2.players[t % 5].nickname = nickname.Substring(1, nickname.Length - 1);
                team2.players[t % 5].GeneratePlayer();
            }
            Manager.mainInstance.MenuShowMatch();
        }

        public void Strength(object _strength)
        {
            string strength = (string)_strength;
            if (strength.Length < 3) return;
            int t = strength[0] - 48;
            if (t < 5)
            {
                team1.players[t].strength = (byte)Mathf.Clamp(((strength[1] - 48) * 10 + strength[2] - 48), 40, 99);
                team1.players[t].GeneratePlayer();
            }
            else
            {
                team2.players[t % 5].strength = (byte)Mathf.Clamp(((strength[1] - 48) * 10 + strength[2] - 48), 40, 99);
                team2.players[t % 5].GeneratePlayer();
            }
            Manager.mainInstance.MenuShowMatch();
        }
        public void Role(object _role)
        {
            string role = (string)_role;
            if (role.Length < 2) return;
            int t = role[0] - 48;
            if (t < 5)
            {
                team1.players[t].role = (Manager.Role)(role[1] - 48);
                team1.players[t].GeneratePlayer();
            }
            else
            {
                team2.players[t % 5].role = (Manager.Role)(role[1] - 48);
                team2.players[t % 5].GeneratePlayer();
            }
            Manager.mainInstance.MenuShowMatch();
        }
        public void Team1(string team) { team1.teamName = team; Manager.mainInstance.MenuShowMatch(); }
        public void Team2(string team) { team2.teamName = team; Manager.mainInstance.MenuShowMatch(); }
    }
    public ShowMatch showMatch;

    bool showMatchG = false;
    public void StartShowMatch(object o)
    {
        showMatchG = true;
        GameObject go = Instantiate(emptyPrefab);
        match = go.AddComponent<Match>();
        match.type = MatchType.BO1;
        match.team1 = showMatch.team1;
        match.team2 = showMatch.team2;
        match.StartPick(0, -1);
        StartPick();
        match.matchGoing = true;
        StartCoroutine(SSM());
    }

    public IEnumerator SSM()
    {
        yield return new WaitWhile(() => match.matchGoing);
        showMatchG = false;
        MenuResults(0);
        blockdayr = true;
    }

    public void MenuShowMatch()
    {
        MenuReset();
        menuelements = new GameObject[33];
        FIGMAStructure figma;
        FIGMAInput figmai;
        RectTransform rt;
        menuelhelp = -64;
        const int m = -64;
        object[] objects;
        FIGMAStructure.ElementType[] et;
        System.Action<object> action;
        menuelements[0] = Instantiate(finput, fspawner.transform, false);
        rt = (RectTransform)menuelements[0].GetComponent(typeof(RectTransform));
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height);
        figmai = (FIGMAInput)menuelements[0].GetComponentInChildren(typeof(FIGMAInput));
        figmai.SetAction(showMatch.Team1);
        figmai.SetText("Team 1");
        if (showMatch.team1 != null) figmai.SetString(showMatch.team1.teamName);
        for (int i = 0; i < 5; i++)
        {
            menuelements[i * 3 + 1] = Instantiate(finput, fspawner.transform, false);
            rt = (RectTransform)menuelements[i * 3 + 1].GetComponent(typeof(RectTransform));
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
            menuelhelp -= Mathf.RoundToInt(rt.rect.height);
            figmai = (FIGMAInput)menuelements[i * 3 + 1].GetComponentInChildren(typeof(FIGMAInput));
            figmai.SetAction(showMatch.Nickname);
            //figmai.SetMode(InputField.ContentType.IntegerNumber);
            figmai.SetText("Nickname");
            figmai.SetPrefix(i.ToString());
            figmai.SetString(showMatch.team1.players[i].nickname);
            menuelements[i * 3 + 2] = Instantiate(finput, fspawner.transform, false);
            rt = (RectTransform)menuelements[i * 3 + 2].GetComponent(typeof(RectTransform));
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
            menuelhelp -= Mathf.RoundToInt(rt.rect.height + 32);
            figmai = (FIGMAInput)menuelements[i * 3 + 2].GetComponentInChildren(typeof(FIGMAInput));
            figmai.SetAction(showMatch.Strength);
            figmai.SetMode(InputField.ContentType.IntegerNumber);
            figmai.SetText("Strength");
            figmai.SetPrefix(i.ToString());
            figmai.SetString(showMatch.team1.players[i].strength.ToString());
            menuelements[i * 3 + 3] = Instantiate(farray, fspawner.transform, false);
            rt = (RectTransform)menuelements[i * 3 + 3].GetComponent(typeof(RectTransform));
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
            menuelhelp -= Mathf.RoundToInt(rt.rect.height);
            figma = (FIGMAStructure)menuelements[i * 3 + 3].GetComponentInChildren(typeof(FIGMAStructure));
            action = showMatch.Role;
            objects = FIGMAStructure.MakeParams(
                new FIGMAParameter("action", new object[] { action, i.ToString() + "0" }, 0),
                new FIGMAParameter("sprite", roles[0].sprite, 1),
                new FIGMAParameter("action", new object[] { action, i.ToString() + "1" }, 2),
                new FIGMAParameter("sprite", roles[1].sprite, 3),
                new FIGMAParameter("action", new object[] { action, i.ToString() + "2" }, 4),
                new FIGMAParameter("sprite", roles[2].sprite, 5),
                new FIGMAParameter("action", new object[] { action, i.ToString() + "3" }, 6),
                new FIGMAParameter("sprite", roles[3].sprite, 7),
                new FIGMAParameter("action", new object[] { action, i.ToString() + "4" }, 8),
                new FIGMAParameter("sprite", roles[4].sprite, 9),
                new FIGMAParameter("sprite", roles[(int)showMatch.team1.players[i].role].sprite, 10),
                new FIGMAParameter("text", "Role", 11)
            );
            et = new FIGMAStructure.ElementType[]
            {
                FIGMAStructure.ElementType.Action, FIGMAStructure.ElementType.Icon,
                FIGMAStructure.ElementType.Action, FIGMAStructure.ElementType.Icon,
                FIGMAStructure.ElementType.Action, FIGMAStructure.ElementType.Icon,
                FIGMAStructure.ElementType.Action, FIGMAStructure.ElementType.Icon,
                FIGMAStructure.ElementType.Action, FIGMAStructure.ElementType.Icon,
                FIGMAStructure.ElementType.Icon, FIGMAStructure.ElementType.Subtitle,
            };
            figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[6] { 2, 2, 2, 2, 2, 2 }, et));
        }
        menuelements[16] = Instantiate(finput, fspawner.transform, false);
        rt = (RectTransform)menuelements[16].GetComponent(typeof(RectTransform));
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height);
        figmai = (FIGMAInput)menuelements[16].GetComponentInChildren(typeof(FIGMAInput));
        figmai.SetAction(showMatch.Team2);
        figmai.SetText("Team 2");
        if (showMatch.team2 != null) figmai.SetString(showMatch.team2.teamName);
        for (int i = 0; i < 5; i++)
        {
            menuelements[i * 3 + 17] = Instantiate(finput, fspawner.transform, false);
            rt = (RectTransform)menuelements[i * 3 + 17].GetComponent(typeof(RectTransform));
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
            menuelhelp -= Mathf.RoundToInt(rt.rect.height);
            figmai = (FIGMAInput)menuelements[i * 3 + 17].GetComponentInChildren(typeof(FIGMAInput));
            figmai.SetAction(showMatch.Nickname);
            //figmai.SetMode(InputField.ContentType.IntegerNumber);
            figmai.SetText("Nickname");
            figmai.SetPrefix((i + 5).ToString());
            figmai.SetString(showMatch.team2.players[i].nickname);
            menuelements[i * 3 + 18] = Instantiate(finput, fspawner.transform, false);
            rt = (RectTransform)menuelements[i * 3 + 18].GetComponent(typeof(RectTransform));
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
            menuelhelp -= Mathf.RoundToInt(rt.rect.height + 32);
            figmai = (FIGMAInput)menuelements[i * 3 + 18].GetComponentInChildren(typeof(FIGMAInput));
            figmai.SetAction(showMatch.Strength);
            figmai.SetMode(InputField.ContentType.IntegerNumber);
            figmai.SetText("Strength");
            figmai.SetPrefix((i + 5).ToString());
            figmai.SetString(showMatch.team2.players[i].strength.ToString());
            menuelements[i * 3 + 19] = Instantiate(farray, fspawner.transform, false);
            rt = (RectTransform)menuelements[i * 3 + 19].GetComponent(typeof(RectTransform));
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
            menuelhelp -= Mathf.RoundToInt(rt.rect.height);
            figma = (FIGMAStructure)menuelements[i * 3 + 19].GetComponentInChildren(typeof(FIGMAStructure));
            action = showMatch.Role;
            objects = FIGMAStructure.MakeParams(
                new FIGMAParameter("action", new object[] { action, (i + 5).ToString() + "0" }, 0),
                new FIGMAParameter("sprite", roles[0].sprite, 1),
                new FIGMAParameter("action", new object[] { action, (i + 5).ToString() + "1" }, 2),
                new FIGMAParameter("sprite", roles[1].sprite, 3),
                new FIGMAParameter("action", new object[] { action, (i + 5).ToString() + "2" }, 4),
                new FIGMAParameter("sprite", roles[2].sprite, 5),
                new FIGMAParameter("action", new object[] { action, (i + 5).ToString() + "3" }, 6),
                new FIGMAParameter("sprite", roles[3].sprite, 7),
                new FIGMAParameter("action", new object[] { action, (i + 5).ToString() + "4" }, 8),
                new FIGMAParameter("sprite", roles[4].sprite, 9),
                new FIGMAParameter("sprite", roles[(int)showMatch.team2.players[i].role].sprite, 10),
                new FIGMAParameter("text", "Role", 11)
            );
            et = new FIGMAStructure.ElementType[]
            {
                FIGMAStructure.ElementType.Action, FIGMAStructure.ElementType.Icon,
                FIGMAStructure.ElementType.Action, FIGMAStructure.ElementType.Icon,
                FIGMAStructure.ElementType.Action, FIGMAStructure.ElementType.Icon,
                FIGMAStructure.ElementType.Action, FIGMAStructure.ElementType.Icon,
                FIGMAStructure.ElementType.Action, FIGMAStructure.ElementType.Icon,
                FIGMAStructure.ElementType.Icon, FIGMAStructure.ElementType.Subtitle,
            };
            figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[6] { 2, 2, 2, 2, 2, 2 }, et));
        }
        menuelements[32] = Instantiate(farray, fspawner.transform, false);
        rt = (RectTransform)menuelements[32].GetComponent(typeof(RectTransform));
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height);
        figma = (FIGMAStructure)menuelements[32].GetComponentInChildren(typeof(FIGMAStructure));
        action = StartShowMatch;
        objects = FIGMAStructure.MakeParams(
            new FIGMAParameter("action", new object[] { action, 0 }, 0),
            new FIGMAParameter("text", "Continue", 11),
            new FIGMAParameter("x", (float)0.0f, 11),
            new FIGMAParameter("y", (float)0.0f, 11)
        );
        et = new FIGMAStructure.ElementType[]
        {
            FIGMAStructure.ElementType.Action, FIGMAStructure.ElementType.TextValue,
        };
        figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[1] { 2 }, et));
        rt = (RectTransform)fspawner.GetComponent(typeof(RectTransform));
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, m - menuelhelp);
    }

    public void MenuHelper()
    {
        MenuReset();
        if (showAds != 202) adremover.SetActive(true);
        menuelements = new GameObject[2];
        FIGMAStructure figma;
        //Sprite flag = (Sprite)Resources.Load("FlagsNew/belgium", typeof(Sprite));
        menuelements[0] = Instantiate(farray, fspawner.transform, false);
        RectTransform rt = (RectTransform)menuelements[0].GetComponent(typeof(RectTransform));
        menuelhelp -= Mathf.RoundToInt(rt.rect.height * 0.5f);
        int m = menuelhelp;
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height);
        figma = (FIGMAStructure)menuelements[0].GetComponentInChildren(typeof(FIGMAStructure));
        //new FIGMAParameter("color", "#2A3C44", 7);
        object[] objects;
        FIGMAStructure.ElementType[] et;
        System.Action<object> action = HyperLinkURL;
        objects = FIGMAStructure.MakeParams(
            new FIGMAParameter("text", "Need help?", 0),
            new FIGMAParameter("text", "Check our discord", 1),
            new FIGMAParameter("sprite", fsprite[4], 2),
            new FIGMAParameter("action", new object[] { action, "http://discord.gg/PTyPvBBmBc" }, 4)
        );
        et = new FIGMAStructure.ElementType[]
        {
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle, 
            FIGMAStructure.ElementType.Icon, FIGMAStructure.ElementType.Action
        };
        figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 2, 2 }, et));
        menuelements[1] = Instantiate(finput, fspawner.transform, false);
        rt = (RectTransform)menuelements[1].GetComponent(typeof(RectTransform));
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height);
        FIGMAInput figmai = (FIGMAInput)menuelements[1].GetComponentInChildren(typeof(FIGMAInput));
        figmai.SetAction(CanadianGamer);
        figmai.SetText("Secret Code");
        rt = (RectTransform)fspawner.GetComponent(typeof(RectTransform));
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, m - menuelhelp);
    }

    public struct TransferSetting
    {
        public Sprite role, flag; //todo
        public string level, age;
    }
    private TransferSetting tsetting = new TransferSetting();

    public void MenuTransfer()
    {
        MenuReset();
        menuelements = new GameObject[6];
        FIGMAStructure figma;
        menuelements[0] = Instantiate(farray, fspawner.transform, false);
        RectTransform rt = (RectTransform)menuelements[0].GetComponent(typeof(RectTransform));
        menuelhelp -= Mathf.RoundToInt(rt.rect.height * 0.5f);
        int m = menuelhelp;
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height);
        figma = (FIGMAStructure)menuelements[0].GetComponentInChildren(typeof(FIGMAStructure));
        System.Action<object> action = TransferSettings;
        object[] objects = FIGMAStructure.MakeParams(
            new FIGMAParameter("sprite", fsprite[2], 0),
            new FIGMAParameter("action", new object[] { action, (int)0 }, 1),
            new FIGMAParameter("sprite", tsetting.role, 2),
            new FIGMAParameter("sprite", fsprite[3], 3),
            new FIGMAParameter("action", new object[] { action, (int)1 }, 4)
        );
        FIGMAStructure.ElementType[] et = new FIGMAStructure.ElementType[]
        {
            FIGMAStructure.ElementType.Icon, FIGMAStructure.ElementType.Action,
            FIGMAStructure.ElementType.Icon,
            FIGMAStructure.ElementType.Icon, FIGMAStructure.ElementType.Action
        };
        figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 2, 1, 2 }, et));
        menuelements[1] = Instantiate(farray, fspawner.transform, false);
        rt = (RectTransform)menuelements[1].GetComponent(typeof(RectTransform));
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height);
        figma = (FIGMAStructure)menuelements[1].GetComponentInChildren(typeof(FIGMAStructure));
        //action = MenuPreviewPlacements;
        objects = FIGMAStructure.MakeParams(
            new FIGMAParameter("sprite", fsprite[2], 0),
            new FIGMAParameter("action", new object[] { action, (int)2 }, 1),
            new FIGMAParameter("text", tsetting.age, 2),
            new FIGMAParameter("text", "Age", 3),
            new FIGMAParameter("sprite", fsprite[3], 4),
            new FIGMAParameter("action", new object[] { action, (int)3 }, 5)
        );
        et = new FIGMAStructure.ElementType[]
        {
            FIGMAStructure.ElementType.Icon, FIGMAStructure.ElementType.Action,
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
            FIGMAStructure.ElementType.Icon, FIGMAStructure.ElementType.Action
        };
        figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 2, 2, 2 }, et));
        menuelements[2] = Instantiate(farray, fspawner.transform, false);
        rt = (RectTransform)menuelements[2].GetComponent(typeof(RectTransform));
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height);
        figma = (FIGMAStructure)menuelements[2].GetComponentInChildren(typeof(FIGMAStructure));
        //action = MenuPreviewPlacements;
        objects = FIGMAStructure.MakeParams(
            new FIGMAParameter("sprite", fsprite[2], 0),
            new FIGMAParameter("action", new object[] { action, (int)4 }, 1),
            new FIGMAParameter("text", tsetting.level, 2),
            new FIGMAParameter("text", "Level", 3),
            new FIGMAParameter("sprite", fsprite[3], 4),
            new FIGMAParameter("action", new object[] { action, (int)5 }, 5)
        );
        et = new FIGMAStructure.ElementType[]
        {
            FIGMAStructure.ElementType.Icon, FIGMAStructure.ElementType.Action,
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
            FIGMAStructure.ElementType.Icon, FIGMAStructure.ElementType.Action
        };
        figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 2, 2, 2 }, et));
        menuelements[3] = Instantiate(farray, fspawner.transform, false);
        rt = (RectTransform)menuelements[3].GetComponent(typeof(RectTransform));
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height - 32);
        figma = (FIGMAStructure)menuelements[3].GetComponentInChildren(typeof(FIGMAStructure));
        //action = MenuPreviewPlacements;
        objects = FIGMAStructure.MakeParams(
            new FIGMAParameter("sprite", fsprite[2], 0),
            new FIGMAParameter("action", new object[] { action, (int)6 }, 1),
            new FIGMAParameter("sprite", tsetting.flag, 2),
            new FIGMAParameter("sprite", fsprite[3], 3),
            new FIGMAParameter("action", new object[] { action, (int)7 }, 4)
        );
        et = new FIGMAStructure.ElementType[]
        {
            FIGMAStructure.ElementType.Icon, FIGMAStructure.ElementType.Action,
            FIGMAStructure.ElementType.Icon,
            FIGMAStructure.ElementType.Icon, FIGMAStructure.ElementType.Action
        };
        figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 2, 1, 2 }, et));
        menuelements[4] = Instantiate(finput, fspawner.transform, false);
        rt = (RectTransform)menuelements[4].GetComponent(typeof(RectTransform));
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height + 32);
        FIGMAInput figmai = (FIGMAInput)menuelements[4].GetComponentInChildren(typeof(FIGMAInput));
        figmai.SetAction(new System.Action<string>(TransferLookForField));
        figmai.SetText("Nickname");
        figmai.SetString(/*string.Empty == searchByTransfer ? ".." : */searchByTransfer);
        menuelements[5] = Instantiate(farray, fspawner.transform, false);
        rt = (RectTransform)menuelements[5].GetComponent(typeof(RectTransform));
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height);
        figma = (FIGMAStructure)menuelements[5].GetComponentInChildren(typeof(FIGMAStructure));
        action = TransferSettings;
        objects = FIGMAStructure.MakeParams(
            new FIGMAParameter("text", "Search", 3),
            new FIGMAParameter("x", (float)0.0f, 3),
            new FIGMAParameter("y", (float)0.0f, 3),
            new FIGMAParameter("action", new object[] { action, (int)8 }, 4)
        );
        et = new FIGMAStructure.ElementType[]
        {
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Action,
        };
        figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 2 }, et));
        rt = (RectTransform)fspawner.GetComponent(typeof(RectTransform));
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, m - menuelhelp);
    }

    public void MenuTransferList(object o)
    {
        MenuReset();
        menuelhelp -= 96;
        int m = menuelhelp;
        RectTransform rt;
        object[] objects;
        FIGMAStructure.ElementType[] et;
        FIGMAStructure figma;
        System.Action<object> action;
        menuelements = new GameObject[_ps.Count > 64 ? 64 : _ps.Count];
        for (int i = 0; i < menuelements.Length; i++)
        {
            menuelements[i] = Instantiate(farray, fspawner.transform, false);
            rt = (RectTransform)menuelements[i].GetComponent(typeof(RectTransform));
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
            menuelhelp -= Mathf.RoundToInt(rt.rect.height);
            figma = (FIGMAStructure)menuelements[i].GetComponentInChildren(typeof(FIGMAStructure));
            action = MenuPlayer;
            Player p = definedPlayers.GetPlayer(_ps[i].nickname);
            Stat stat = p.FullStat(30);
            int a = (stat.kills - stat.deaths);
            objects = FIGMAStructure.MakeParams(
                new FIGMAParameter("text", p.nickname + ", " + p.age.ToString(), 0),
                new FIGMAParameter("text", "Player", 1),
                new FIGMAParameter("action", new object[] { action, (string)p.nickname }, 2),
                new FIGMAParameter("color", "#2A3C44", 3),
                new FIGMAParameter("text", a > 0 ? "+" + a.ToString() : a.ToString(), 4),
                new FIGMAParameter("text", "K-D Diff", 5),
                new FIGMAParameter("text", ((double)stat.kills / stat.deaths).ToString("0.00"), 6),
                new FIGMAParameter("text", "K/D", 7),
                new FIGMAParameter("color", "#2A3C44", 8),
                new FIGMAParameter("text", stat.GetRating().ToString("0.00"), 9),
                new FIGMAParameter("text", "Rating", 10)
                );
            et = new FIGMAStructure.ElementType[]
            {
                FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle, FIGMAStructure.ElementType.Action,
                FIGMAStructure.ElementType.Background, FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
                FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
                FIGMAStructure.ElementType.Background, FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
            };
            figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 3, 3, 2, 3 }, et));
        }
        rt = (RectTransform)fspawner.GetComponent(typeof(RectTransform));
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, m - menuelhelp);
    }

    public void MenuPreviewEvent() { MenuPreviewEvent(null); }
    public void MenuPreviewEvent(object o)
    {
        MenuReset();
        if (o == null) eventViewer = (int)myTeam.currentTournament;
        else eventViewer = (int)o;
        if (eventViewer == -1)
        {
            ShowPopUp("You aren't registered for any tournament"); MenuEvents(); return;
        }
        menuelements = new GameObject[4]; // playoff groups playerstats placements
        FIGMAStructure figma;
        menuelements[0] = Instantiate(farray, fspawner.transform, false);
        RectTransform rt = (RectTransform)menuelements[0].GetComponent(typeof(RectTransform));
        menuelhelp -= Mathf.RoundToInt(rt.rect.height * 0.5f);
        int m = menuelhelp;
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height);
        figma = (FIGMAStructure)menuelements[0].GetComponentInChildren(typeof(FIGMAStructure));
        System.Action<object> action = MenuPreviewStats;
        object[] objects = FIGMAStructure.MakeParams(
            new FIGMAParameter("text", "Stats", 0),
            new FIGMAParameter("x", (float)0.0f, 0),
            new FIGMAParameter("y", (float)0.0f, 0),
            new FIGMAParameter("action", new object[] { action, null }, 1)
        );
        FIGMAStructure.ElementType[] et = new FIGMAStructure.ElementType[]
        {
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Action
        };
        figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 2 }, et));
        menuelements[1] = Instantiate(farray, fspawner.transform, false);
        rt = (RectTransform)menuelements[1].GetComponent(typeof(RectTransform));
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height);
        figma = (FIGMAStructure)menuelements[1].GetComponentInChildren(typeof(FIGMAStructure));
        action = MenuPreviewPlacements;
        objects = FIGMAStructure.MakeParams(
            new FIGMAParameter("text", "Placements", 0),
            new FIGMAParameter("x", (float)0.0f, 0),
            new FIGMAParameter("y", (float)0.0f, 0),
            new FIGMAParameter("action", new object[] { action, null }, 1)
        );
        et = new FIGMAStructure.ElementType[]
        {
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Action
        };
        figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 2 }, et));
        Tournament _t = Events.events.GetTournaments()[eventViewer];
        TournamentStructure structure = Modding.modding.modSave.structure[_t.btype];
        if (structure.groupCount > 0)
        {
            menuelements[2] = Instantiate(farray, fspawner.transform, false);
            rt = (RectTransform)menuelements[2].GetComponent(typeof(RectTransform));
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
            menuelhelp -= Mathf.RoundToInt(rt.rect.height);
            figma = (FIGMAStructure)menuelements[2].GetComponentInChildren(typeof(FIGMAStructure));
            action = MenuPreviewGroupA;
            objects = FIGMAStructure.MakeParams(
                new FIGMAParameter("text", "Groups", 0),
                new FIGMAParameter("x", (float)0.0f, 0),
                new FIGMAParameter("y", (float)0.0f, 0),
                new FIGMAParameter("action", new object[] { action, 0 }, 1)
            );
            et = new FIGMAStructure.ElementType[]
            {
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Action
            };
            figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 2 }, et));
        }
        if (structure.matchSettings.Length - (structure.groupCount * structure.groupMatchesLength) > 0)
        {
            menuelements[3] = Instantiate(farray, fspawner.transform, false);
            rt = (RectTransform)menuelements[3].GetComponent(typeof(RectTransform));
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
            menuelhelp -= Mathf.RoundToInt(rt.rect.height);
            figma = (FIGMAStructure)menuelements[3].GetComponentInChildren(typeof(FIGMAStructure));
            action = MenuPreviewPlayoff;
            objects = FIGMAStructure.MakeParams(
                new FIGMAParameter("text", "Playoff", 0),
                new FIGMAParameter("x", (float)0.0f, 0),
                new FIGMAParameter("y", (float)0.0f, 0),
                new FIGMAParameter("action", new object[] { action, null }, 1)
            );
            et = new FIGMAStructure.ElementType[]
            {
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Action
            };
            figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 2 }, et));
        }
        rt = (RectTransform)fspawner.GetComponent(typeof(RectTransform));
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, m - menuelhelp);
    }

    public void MenuPreviewStats(object o)
    {
        MenuReset();
        int eV = eventViewer;
        Tournament _t = Events.events.GetTournaments()[eV];
        TournamentStructure structure = Modding.modding.modSave.structure[_t.btype];
        List<Stat> stats = _t.GetStats();
        menuelements = new GameObject[stats.Count + 1];
        FIGMAStructure figma;
        menuelements[0] = Instantiate(farray, fspawner.transform, false);
        RectTransform rt = (RectTransform)menuelements[0].GetComponent(typeof(RectTransform));
        menuelhelp -= Mathf.RoundToInt(rt.rect.height * 0.5f);
        int m = menuelhelp;
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height);
        figma = (FIGMAStructure)menuelements[0].GetComponentInChildren(typeof(FIGMAStructure));
        System.Action<object> action = MenuPreviewEvent;
        object[] objects = FIGMAStructure.MakeParams(
            new FIGMAParameter("text", _t.title, 0),
            new FIGMAParameter("text", _t.type, 1),
            new FIGMAParameter("action", new object[] { action, eV }, 2),
            new FIGMAParameter("sprite", _t.logo, 3)
        );
        FIGMAStructure.ElementType[] et = new FIGMAStructure.ElementType[]
        {
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
            FIGMAStructure.ElementType.Action, FIGMAStructure.ElementType.Flag
        };
        figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 4 }, et));
        int furllys = Manager.day - _t.day + 1;
        int obrig = stats.Count > 48 ? 48 : stats.Count;
        for (int i = 0; i < obrig; i++)
        {
            menuelements[i + 1] = Instantiate(farray, fspawner.transform, false);
            rt = (RectTransform)menuelements[i + 1].GetComponent(typeof(RectTransform));
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
            menuelhelp -= Mathf.RoundToInt(rt.rect.height);
            figma = (FIGMAStructure)menuelements[i + 1].GetComponentInChildren(typeof(FIGMAStructure));
            action = MenuPlayer;
            Player p = definedPlayers.GetPlayer(stats[i].nickname);
            int a = (stats[i].kills - stats[i].deaths);
            objects = FIGMAStructure.MakeParams(
                new FIGMAParameter("text", p.nickname + ", " + p.age.ToString(), 0),
                new FIGMAParameter("text", "Player", 1),
                new FIGMAParameter("action", new object[] { action, (string)p.nickname }, 2),
                new FIGMAParameter("color", "#2A3C44", 3),
                new FIGMAParameter("text", a > 0 ? "+" + a.ToString() : a.ToString(), 4),
                new FIGMAParameter("text", "K-D Diff", 5),
                new FIGMAParameter("text", ((double)stats[i].kills / stats[i].deaths).ToString("0.00"), 6),
                new FIGMAParameter("text", "K/D", 7),
                new FIGMAParameter("color", "#2A3C44", 8),
                new FIGMAParameter("text", stats[i].GetRating().ToString("0.00"), 9),
                new FIGMAParameter("text", "Rating", 10)
                );
            et = new FIGMAStructure.ElementType[]
            {
                FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle, FIGMAStructure.ElementType.Action,
                FIGMAStructure.ElementType.Background, FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
                FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
                FIGMAStructure.ElementType.Background, FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
            };
            figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 3, 3, 2, 3 }, et));
        }
        rt = (RectTransform)fspawner.GetComponent(typeof(RectTransform));
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, m - menuelhelp);
    }

    public void MenuPreviewPlacements(object o)
    {
        MenuReset();
        int eV = eventViewer;
        Tournament _t = Events.events.GetTournaments()[eV];
        TournamentStructure structure = Modding.modding.modSave.structure[_t.btype];
        //List<Stat> stats = _t.GetGroup((groupCounter - 1) % structure.groupCount); //????
        Team[] tms = _t.GetTeams();
        menuelements = new GameObject[tms.Length + 1];
        FIGMAStructure figma;
        menuelements[0] = Instantiate(farray, fspawner.transform, false);
        RectTransform rt = (RectTransform)menuelements[0].GetComponent(typeof(RectTransform));
        menuelhelp -= Mathf.RoundToInt(rt.rect.height * 0.5f);
        int m = menuelhelp;
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height);
        figma = (FIGMAStructure)menuelements[0].GetComponentInChildren(typeof(FIGMAStructure));
        System.Action<object> action = MenuPreviewEvent;
        object[] objects = FIGMAStructure.MakeParams(
            new FIGMAParameter("text", _t.title, 0),
            new FIGMAParameter("text", _t.type, 1),
            new FIGMAParameter("action", new object[] { action, eV }, 2),
            new FIGMAParameter("sprite", _t.logo, 3)
        );
        FIGMAStructure.ElementType[] et = new FIGMAStructure.ElementType[]
        {
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
            FIGMAStructure.ElementType.Action, FIGMAStructure.ElementType.Flag
        };
        figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 4 }, et));
        action = MenuAnyTeam;
        for (int i = 0; i < tms.Length; i++)
        {
            if (tms[i] == null) continue;
            menuelements[i + 1] = Instantiate(farray, fspawner.transform, false);
            rt = (RectTransform)menuelements[i + 1].GetComponent(typeof(RectTransform));
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
            menuelhelp -= Mathf.RoundToInt(rt.rect.height);
            figma = (FIGMAStructure)menuelements[i + 1].GetComponentInChildren(typeof(FIGMAStructure));
            Sprite sprite = Modding.modding.GetLogo(tms[i].teamName);
            if (sprite == null)
            {
                objects = FIGMAStructure.MakeParams(
                    new FIGMAParameter("text", tms[i].teamName, 0),
                    new FIGMAParameter("text", "Team", 1),
                    new FIGMAParameter("action", new object[] { action, (string)tms[i].teamName }, 2),
                    new FIGMAParameter("color", "#2A3C44", 3),
                    new FIGMAParameter("text", "#" + (i + 1).ToString(), 4),
                    new FIGMAParameter("text", "Placement", 5),
                    new FIGMAParameter("text", string.Format("{0:## ### ###}", (int)(structure.prizePoolDistribution[i] * _t.prizePool + 0.5f)), 6),
                    new FIGMAParameter("text", "Prize", 7)
                );
                et = new FIGMAStructure.ElementType[]
                {
                    FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle, FIGMAStructure.ElementType.Action,
                    FIGMAStructure.ElementType.Background, FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
                    FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
                };
                figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 3, 3, 2 }, et));
            }
            else
            {
                objects = FIGMAStructure.MakeParams(
                    new FIGMAParameter("text", tms[i].teamName, 0),
                    new FIGMAParameter("text", "Team", 1),
                    new FIGMAParameter("action", new object[] { action, (string)tms[i].teamName }, 2),
                    new FIGMAParameter("sprite", sprite, 3),
                    new FIGMAParameter("color", "#2A3C44", 4),
                    new FIGMAParameter("text", "#" + (i + 1).ToString(), 5),
                    new FIGMAParameter("text", "Placement", 6),
                    new FIGMAParameter("text", string.Format("{0:## ### ###}", (int)(structure.prizePoolDistribution[i] * _t.prizePool + 0.5f)), 7),
                    new FIGMAParameter("text", "Prize", 8)
                );
                et = new FIGMAStructure.ElementType[]
                {
                    FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle, FIGMAStructure.ElementType.Action, FIGMAStructure.ElementType.Flag,
                    FIGMAStructure.ElementType.Background, FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
                    FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
                };
                figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 4, 3, 2 }, et));
            }
        }
        rt = (RectTransform)fspawner.GetComponent(typeof(RectTransform));
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, m - menuelhelp);
    }

    public void MenuPreviewGroupA(object o) { MenuPreviewGroup(0); }
    public void MenuPreviewGroup(string text) { groupCounter = int.Parse(text); MenuPreviewGroup(0); }
    public void MenuPreviewGroup(int o)
    {
        MenuReset();
        int eV = eventViewer;
        Tournament _t = Events.events.GetTournaments()[eV];
        TournamentStructure structure = Modding.modding.modSave.structure[_t.btype];
        //List<Stat> stats = _t.GetGroup((groupCounter - 1) % structure.groupCount); //????
        if (o > 0) groupCounter = (int)o;
        var tms = _t.GetGroup(groupCounter % structure.groupCount);
        menuelements = new GameObject[tms.Count + 2];
        FIGMAStructure figma;
        menuelements[0] = Instantiate(farray, fspawner.transform, false);
        RectTransform rt = (RectTransform)menuelements[0].GetComponent(typeof(RectTransform));
        menuelhelp -= Mathf.RoundToInt(rt.rect.height * 0.5f);
        int m = menuelhelp;
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height - 32);
        figma = (FIGMAStructure)menuelements[0].GetComponentInChildren(typeof(FIGMAStructure));
        System.Action<object> action = MenuPreviewEvent;
        object[] objects = FIGMAStructure.MakeParams(
            new FIGMAParameter("text", _t.title, 0),
            new FIGMAParameter("text", _t.type, 1),
            new FIGMAParameter("action", new object[] { action, eV }, 2),
            new FIGMAParameter("sprite", _t.logo, 3)
        );
        FIGMAStructure.ElementType[] et = new FIGMAStructure.ElementType[]
        {
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
            FIGMAStructure.ElementType.Action, FIGMAStructure.ElementType.Flag
        };
        figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 4 }, et));
        menuelements[1] = Instantiate(finput, fspawner.transform, false);
        rt = (RectTransform)menuelements[1].GetComponent(typeof(RectTransform));
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height + 32);
        FIGMAInput figmai = (FIGMAInput)menuelements[1].GetComponentInChildren(typeof(FIGMAInput));
        //action = MenuPreviewGroup;
        figmai.SetAction(new System.Action<string>(MenuPreviewGroup));
        figmai.SetMode(InputField.ContentType.IntegerNumber);
        figmai.SetText("Group");
        figmai.SetString(((int)groupCounter).ToString());
        action = MenuAnyTeam;
        for (int i = 0; i < tms.Count; i++)
        {
            menuelements[i + 2] = Instantiate(farray, fspawner.transform, false);
            rt = (RectTransform)menuelements[i + 2].GetComponent(typeof(RectTransform));
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
            menuelhelp -= Mathf.RoundToInt(rt.rect.height);
            figma = (FIGMAStructure)menuelements[i + 2].GetComponentInChildren(typeof(FIGMAStructure));
            Sprite sprite = Modding.modding.GetLogo(tms[i].teamname);
            if (sprite == null)
            {
                objects = FIGMAStructure.MakeParams(
                    new FIGMAParameter("text", tms[i].teamname.ToString(), 0),
                    new FIGMAParameter("text", "Team", 1),
                    new FIGMAParameter("action", new object[] { action, (string)tms[i].teamname }, 2),
                    new FIGMAParameter("color", "#2A3C44", 3),
                    new FIGMAParameter("text", tms[i].points.ToString(), 4),
                    new FIGMAParameter("text", "Points", 5)
                );
                et = new FIGMAStructure.ElementType[]
                {
                    FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle, FIGMAStructure.ElementType.Action,
                    FIGMAStructure.ElementType.Background, FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle
                };
                figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 3, 3 }, et));
            }
            else
            {
                objects = FIGMAStructure.MakeParams(
                    new FIGMAParameter("text", tms[i].teamname.ToString(), 0),
                    new FIGMAParameter("text", "Team", 1),
                    new FIGMAParameter("action", new object[] { action, (string)tms[i].teamname }, 2),
                    new FIGMAParameter("sprite", sprite, 3),
                    new FIGMAParameter("color", "#2A3C44", 4),
                    new FIGMAParameter("text", tms[i].points.ToString(), 5),
                    new FIGMAParameter("text", "Points", 6)
                );
                et = new FIGMAStructure.ElementType[]
                {
                    FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle, FIGMAStructure.ElementType.Action, FIGMAStructure.ElementType.Flag,
                    FIGMAStructure.ElementType.Background, FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle
                };
                figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 4, 3 }, et));
            }
        }
        rt = (RectTransform)fspawner.GetComponent(typeof(RectTransform));
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, m - menuelhelp);
    }

    public void MenuPreviewPlayoff(object o)
    {
        MenuReset();
        oldpreview.SetActive(true);
        menuelements = new GameObject[1];
        int eV = eventViewer;
        Tournament _t = Events.events.GetTournaments()[eV];
        TournamentStructure structure = Modding.modding.modSave.structure[_t.btype];
        evBracketMatches = new GameObject[structure.matchSettings.Length -
            (structure.groupCount * structure.groupMatchesLength)];
        FIGMAStructure figma;
        menuelements[0] = Instantiate(farray, fspawner.transform, false);
        RectTransform rt = (RectTransform)menuelements[0].GetComponent(typeof(RectTransform));
        menuelhelp -= Mathf.RoundToInt(rt.rect.height * 0.5f);
        int m = menuelhelp;
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height);
        figma = (FIGMAStructure)menuelements[0].GetComponentInChildren(typeof(FIGMAStructure));
        System.Action<object> action = MenuPreviewEvent;
        object[] objects = FIGMAStructure.MakeParams(
            new FIGMAParameter("text", _t.title, 0),
            new FIGMAParameter("text", _t.type, 1),
            new FIGMAParameter("action", new object[] { action, eV }, 2),
            new FIGMAParameter("sprite", _t.logo, 3)
        );
        FIGMAStructure.ElementType[] et = new FIGMAStructure.ElementType[]
        {
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
            FIGMAStructure.ElementType.Action, FIGMAStructure.ElementType.Flag
        };
        figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 4 }, et));
        for (int i = structure.groupCount * structure.groupMatchesLength, po = 0; i < structure.matchSettings.Length; i++, po++)
        {
            evBracketMatches[po] = Instantiate(matchGrid, evprBracket, false);
            evBracketMatches[po].GetComponent<RectTransform>().anchoredPosition =
                new Vector2(structure.matchSettings[i].x, structure.matchSettings[i].y);
            MatchGrid _matchGrid = (MatchGrid)evBracketMatches[po].GetComponent(typeof(MatchGrid));
            _matchGrid.team1.text = _t.matches[i].team1 == null ? "TBA" : _t.matches[i].team1.teamName;
            _matchGrid.team2.text = _t.matches[i].team2 == null ? "TBA" : _t.matches[i].team2.teamName;
            if (_t.matches[i].winner != 0)
            {
                switch (_t.matches[i].type)
                {
                    case MatchType.BO1:
                        if (_t.matches[i].winner <= 5)
                        {
                            _matchGrid.points1.text = "1";
                            _matchGrid.points2.text = "0";
                        }
                        else
                        {
                            _matchGrid.points1.text = "0";
                            _matchGrid.points2.text = "1";
                        }
                        break;
                    case MatchType.BO3:
                        if (_t.matches[i].winner <= 5)
                        {
                            _matchGrid.points1.text = "2";
                            _matchGrid.points2.text = _t.matches[i].winner == 2 ? "0" : "1";
                        }
                        else
                        {
                            _matchGrid.points1.text = _t.matches[i].winner == 7 ? "0" : "1";
                            _matchGrid.points2.text = "2";
                        }
                        break;
                    case MatchType.BO5:
                        if (_t.matches[i].winner <= 5)
                        {
                            _matchGrid.points1.text = "3";
                            _matchGrid.points2.text = (_t.matches[i].winner - 3).ToString();
                        }
                        else
                        {
                            _matchGrid.points1.text = (_t.matches[i].winner - 8).ToString();
                            _matchGrid.points2.text = "3";
                        }
                        break;
                }
            }
        }
        rt = (RectTransform)fspawner.GetComponent(typeof(RectTransform));
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, m - menuelhelp);
    }
    
    private void SetEventPlayback(string text)
    { eventPast = int.Parse(text); if (eventPast < 0) eventPast = 0; MenuEvents(); }
    public void MenuEvents()
    {
        MenuReset();
        menuelements = new GameObject[11];
        FIGMAStructure figma;
        //Sprite flag = (Sprite)Resources.Load("FlagsNew/belgium", typeof(Sprite));
        menuelements[0] = Instantiate(finput, fspawner.transform, false);
        RectTransform rt = (RectTransform)menuelements[0].GetComponent(typeof(RectTransform));
        menuelhelp -= Mathf.RoundToInt(rt.rect.height * 0.5f);
        int m = menuelhelp;
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height + 32);
        FIGMAInput figmai = (FIGMAInput)menuelements[0].GetComponentInChildren(typeof(FIGMAInput));
        figmai.SetAction(SetEventPlayback);
        figmai.SetMode(InputField.ContentType.IntegerNumber);
        figmai.SetText("Days back");
        if (eventPast > 0) figmai.SetString(eventPast.ToString());
        //remake
        object[] objects;
        FIGMAStructure.ElementType[] et;
        //figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 2, 3 }, et));
        System.Action<object> action = EventRegistration;
        short s = 0;
        for (; s < Events.events.GetTournaments().Count; s++)
            if (Events.events.GetTournaments()[s].day > day - 10 - eventPast) break;
        for (byte i = 0; i < 10; i++)
        {
            Tournament tt = Events.events.GetTournaments()[s + i];
            menuelements[i + 1] = Instantiate(farray, fspawner.transform, false);
            rt = (RectTransform)menuelements[i + 1].GetComponent(typeof(RectTransform));
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
            menuelhelp -= Mathf.RoundToInt(rt.rect.height);
            figma = (FIGMAStructure)menuelements[i + 1].GetComponentInChildren(typeof(FIGMAStructure));
            objects = FIGMAStructure.MakeParams(
                new FIGMAParameter("sprite", tt.logo, 0),
                new FIGMAParameter("text", tt.title, 1),
                new FIGMAParameter("text", tt.type, 2),
                new FIGMAParameter("action", new object[] { action, (int)i }, 3),
                new FIGMAParameter("text", GetDateText(tt.day - day), 4),
                new FIGMAParameter("text", "Winner: " + (tt.EventState() == 2 ? tt.GetTeams()[0].teamName : "TBA"), 5),
                new FIGMAParameter("color", "#2A3C44", 6)
                );
            et = new FIGMAStructure.ElementType[]
            {
                FIGMAStructure.ElementType.Flag, FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle, FIGMAStructure.ElementType.Action,
                FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle, FIGMAStructure.ElementType.Background
            };
            figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 4, 3 }, et));
        }
        rt = (RectTransform)fspawner.GetComponent(typeof(RectTransform));
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, m - menuelhelp);
    }
    [SerializeField] private Sprite[] fmaps;
    private void AddMapT(object o) { myTeam.AddMap(o); MenuBootcamp(); }
    public void MenuBootcamp()
    {
        if (myBootcamp == null) { MenuBootcampChoice(null); return; }
        MenuReset();
        menuelements = new GameObject[14];
        FIGMAStructure figma;
        //Sprite flag = (Sprite)Resources.Load("FlagsNew/belgium", typeof(Sprite));
        menuelements[0] = Instantiate(farray, fspawner.transform, false);
        RectTransform rt = (RectTransform)menuelements[0].GetComponent(typeof(RectTransform));
        menuelhelp -= Mathf.RoundToInt(rt.rect.height * 0.5f);
        int m = menuelhelp;
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height);
        figma = (FIGMAStructure)menuelements[0].GetComponentInChildren(typeof(FIGMAStructure));
        //new FIGMAParameter("color", "#2A3C44", 7);
        object[] objects = FIGMAStructure.MakeParams(
            new FIGMAParameter("text", myTeam.GetMapPoints().ToString(), 0),
            new FIGMAParameter("text", "Map Points", 1),
            new FIGMAParameter("color", "#2A3C44", 2),
            new FIGMAParameter("text", myTeam.GetForm().ToString(), 3),
            new FIGMAParameter("text", "Form", 4),
            new FIGMAParameter("text", myTeam.GetChemistry().ToString(), 5),
            new FIGMAParameter("text", "Chemistry", 6),
            new FIGMAParameter("color", "#2A3C44", 7),
            new FIGMAParameter("text", myTeam.GetPsychology().ToString(), 8),
            new FIGMAParameter("text", "Psychology", 9)
            );
        FIGMAStructure.ElementType[] et = new FIGMAStructure.ElementType[]
        {
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
            FIGMAStructure.ElementType.Background, FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
            FIGMAStructure.ElementType.Background, FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle
        };
        figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 2, 3, 2, 3 }, et));
        System.Action<object> action = AddMapT;
        List<FIGMAParameter> fp = new List<FIGMAParameter>(28);
        List<FIGMAStructure.ElementType> ets = new List<FIGMAStructure.ElementType>(28);
        for (int i = 0; i < 7; i++)
        {
            fp.Add(new FIGMAParameter("sprite", fmaps[i], (byte)(i * 4)));
            fp.Add(new FIGMAParameter("text", myTeam.GetMap((Map)i).ToString() + "/10", (byte)(i * 4 + 1)));
            fp.Add(new FIGMAParameter("text", ((Map)i).ToString(), (byte)(i * 4 + 2)));
            fp.Add(new FIGMAParameter("action", new object[] { action, (Map)i }, (byte)(i * 4 + 3)));
            ets.AddRange(new FIGMAStructure.ElementType[]
            { FIGMAStructure.ElementType.Background, FIGMAStructure.ElementType.TextValue,
                FIGMAStructure.ElementType.Subtitle, FIGMAStructure.ElementType.Action });
        }
        menuelements[1] = Instantiate(farray, fspawner.transform, false);
        rt = (RectTransform)menuelements[1].GetComponent(typeof(RectTransform));
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height);
        figma = (FIGMAStructure)menuelements[1].GetComponentInChildren(typeof(FIGMAStructure));
        objects = FIGMAStructure.MakeParamsA(fp.ToArray());
        figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[7] { 4, 4, 4, 4, 4, 4, 4 }, ets.ToArray()));
        menuelements[2] = Instantiate(farray, fspawner.transform, false);
        rt = (RectTransform)menuelements[2].GetComponent(typeof(RectTransform));
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height);
        figma = (FIGMAStructure)menuelements[2].GetComponentInChildren(typeof(FIGMAStructure));
        action = MenuAnalyst;
        System.Action<object> action2 = MenuPsychologist;
        objects = FIGMAStructure.MakeParams(
            new FIGMAParameter("text", myBootcamp.assistantCoach.name == string.Empty ? "Choose an analyst" : myBootcamp.assistantCoach.name, 0),
            new FIGMAParameter("text", "Analyst", 1),
            new FIGMAParameter("action", new object[] { action, 0 }, 2),
            new FIGMAParameter("color", "#2A3C44", 3),
            new FIGMAParameter("text", myBootcamp.psychologist.name == string.Empty ? "Choose a psychologist" : myBootcamp.psychologist.name, 4),
            new FIGMAParameter("text", "Psychologist", 5),
            new FIGMAParameter("action", new object[] { action2, 0 }, 6)
            );
        et = new FIGMAStructure.ElementType[]
        {
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle, FIGMAStructure.ElementType.Action,
            FIGMAStructure.ElementType.Background, FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle, FIGMAStructure.ElementType.Action
        };
        figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 3, 4 }, et));
        menuelements[3] = Instantiate(farray, fspawner.transform, false);
        rt = (RectTransform)menuelements[3].GetComponent(typeof(RectTransform));
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height);
        figma = (FIGMAStructure)menuelements[3].GetComponentInChildren(typeof(FIGMAStructure));
        action = MenuBootcampChoice;
        objects = FIGMAStructure.MakeParams(
            new FIGMAParameter("text", "Move bootcamp", 0),
            new FIGMAParameter("x", (float)0.0f, 0),
            new FIGMAParameter("y", (float)0.0f, 0),
            new FIGMAParameter("action", new object[] { action, null }, 2)
            );
        et = new FIGMAStructure.ElementType[]
        {
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Action
        };
        figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 2 }, et));
        rt = (RectTransform)fspawner.GetComponent(typeof(RectTransform));
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, m - menuelhelp);
    }

    public void MenuPsychologist(object o)
    {
        MenuReset();
        menuelements = new GameObject[6];
        menuelhelp -= 96;
        int m = menuelhelp;
        string[] table = new string[6]
        { "Max Payne", "Peng Luion", "James Lionier", "Karol Witkowski", "Fabien Sapiel", "Andrew Pagolini" };
        byte[] btable = new byte[6] { 17, 15, 11, 13, 9, 7 };
        ushort[] utable = new ushort[6] { 14000, 10800, 4800, 8400, 4000, 1200 };
        RectTransform rt;
        object[] objects;
        FIGMAStructure.ElementType[] et;
        FIGMAStructure figma;
        System.Action<object> action;
        for (int i = 0; i < 6; i++)
        {
            menuelements[i] = Instantiate(farray, fspawner.transform, false);
            rt = (RectTransform)menuelements[i].GetComponent(typeof(RectTransform));
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
            menuelhelp -= Mathf.RoundToInt(rt.rect.height);
            figma = (FIGMAStructure)menuelements[i].GetComponentInChildren(typeof(FIGMAStructure));
            action = ChoosePsychologist;
            objects = FIGMAStructure.MakeParams(
                new FIGMAParameter("text", table[i], 0),
                new FIGMAParameter("text", "Psychologist", 1),
                new FIGMAParameter("color", "#2A3C44", 2),
                new FIGMAParameter("text", utable[i].ToString() + "$", 3),
                new FIGMAParameter("text", btable[i].ToString() + "/17", 4),
                new FIGMAParameter("text", "Hire", 5),
                new FIGMAParameter("x", (float)0.0f, 5),
                new FIGMAParameter("y", (float)0.0f, 5),
                new FIGMAParameter("action", new object[] { action, (int)i }, 6)
                );
            et = new FIGMAStructure.ElementType[]
            {
                FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
                FIGMAStructure.ElementType.Background, FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
                FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Action
            };
            figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 2, 3, 2 }, et));
        }
        rt = (RectTransform)fspawner.GetComponent(typeof(RectTransform));
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, m - menuelhelp);
    }

    public void MenuAnalyst(object o)
    {
        MenuReset();
        menuelements = new GameObject[6];
        menuelhelp -= 96;
        int m = menuelhelp;
        string[] table = new string[6]
        { "Andrey Gorodensky", "Danny Sørensen", "Konstantin Pikiner", "Björn Pers", "Rémy Quoniam", "Damien Marcel" };
        byte[] btable = new byte[6] { 27, 30, 26, 22, 17, 20 };
        ushort[] utable = new ushort[6] { 24300, 28350, 19950, 16800, 9000, 9600 };
        RectTransform rt;
        object[] objects;
        FIGMAStructure.ElementType[] et;
        FIGMAStructure figma;
        System.Action<object> action;
        for (int i = 0; i < 6; i++)
        {
            menuelements[i] = Instantiate(farray, fspawner.transform, false);
            rt = (RectTransform)menuelements[i].GetComponent(typeof(RectTransform));
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
            menuelhelp -= Mathf.RoundToInt(rt.rect.height);
            figma = (FIGMAStructure)menuelements[i].GetComponentInChildren(typeof(FIGMAStructure));
            action = ChooseCoach;
            objects = FIGMAStructure.MakeParams(
                new FIGMAParameter("text", table[i], 0),
                new FIGMAParameter("text", "Analyst", 1),
                new FIGMAParameter("color", "#2A3C44", 2),
                new FIGMAParameter("text", utable[i].ToString() + "$", 3),
                new FIGMAParameter("text", btable[i].ToString() + "/30", 4),
                new FIGMAParameter("text", "Hire", 5),
                new FIGMAParameter("x", (float)0.0f, 5),
                new FIGMAParameter("y", (float)0.0f, 5),
                new FIGMAParameter("action", new object[] { action, (int)i }, 6)
                );
            et = new FIGMAStructure.ElementType[]
            {
                FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
                FIGMAStructure.ElementType.Background, FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
                FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Action
            };
            figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 2, 3, 2 }, et));
        }
        rt = (RectTransform)fspawner.GetComponent(typeof(RectTransform));
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, m - menuelhelp);
    }

    public void MenuBootcampChoice(object o)
    {
        MenuReset();
        menuelements = new GameObject[definedBootcamps.bootcamps.Count];
        menuelhelp -= 96;
        int m = menuelhelp;
        RectTransform rt;
        object[] objects;
        FIGMAStructure.ElementType[] et;
        FIGMAStructure figma;
        System.Action<object> action;
        for (int i = 0; i < definedBootcamps.bootcamps.Count; i++)
        {
            menuelements[i] = Instantiate(farray, fspawner.transform, false);
            rt = (RectTransform)menuelements[i].GetComponent(typeof(RectTransform));
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
            menuelhelp -= Mathf.RoundToInt(rt.rect.height);
            figma = (FIGMAStructure)menuelements[i].GetComponentInChildren(typeof(FIGMAStructure));
            action = ChooseBootcamp;
            objects = FIGMAStructure.MakeParams(
                new FIGMAParameter("text", definedBootcamps.bootcamps[i].city, 0),
                new FIGMAParameter("text", "City", 1),
                new FIGMAParameter("color", "#2A3C44", 2),
                new FIGMAParameter("text", definedBootcamps.bootcamps[i].pricePerMonth.ToString() + "$", 3),
                new FIGMAParameter("text", (definedBootcamps.bootcamps[i].energyAdd +
                    definedBootcamps.bootcamps[i].chemistryAdd +
                    definedBootcamps.bootcamps[i].mapPoints +
                    definedBootcamps.bootcamps[i].formAdd).ToString() + "/80", 4),
                new FIGMAParameter("text", "Rent", 5),
                new FIGMAParameter("x", (float)0.0f, 5),
                new FIGMAParameter("y", (float)0.0f, 5),
                new FIGMAParameter("action", new object[] { action, (int)i }, 6)
                );
            et = new FIGMAStructure.ElementType[]
            {
                FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
                FIGMAStructure.ElementType.Background, FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
                FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Action
            };
            figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 2, 3, 2 }, et));
        }
        rt = (RectTransform)fspawner.GetComponent(typeof(RectTransform));
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, m - menuelhelp);
    }

    public void MenuAcademy()
    {
        MenuReset();
        menuelements = new GameObject[myTeam.academy.GetPlayersCount() + 1];
        FIGMAStructure figma;
        //Sprite flag = (Sprite)Resources.Load("FlagsNew/belgium", typeof(Sprite));
        menuelements[0] = Instantiate(farray, fspawner.transform, false);
        RectTransform rt = (RectTransform)menuelements[0].GetComponent(typeof(RectTransform));
        menuelhelp -= Mathf.RoundToInt(rt.rect.height * 0.5f);
        int m = menuelhelp;
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height);
        figma = (FIGMAStructure)menuelements[0].GetComponentInChildren(typeof(FIGMAStructure));
        //new FIGMAParameter("color", "#2A3C44", 7);
        System.Action<object> action = UpgradeAcademy;
        object[] objects = FIGMAStructure.MakeParams(
            new FIGMAParameter("text", myTeam.teamName + ", #" + definedTeams.GetTeamPlacement(myTeam.teamName).ToString(), 0),
            new FIGMAParameter("text", "Team", 1),
            new FIGMAParameter("color", "#2A3C44", 2),
            new FIGMAParameter("text", myTeam.academy.GetAcademyLevel().ToString(), 3),
            new FIGMAParameter("text", "Academy Level", 4),
            new FIGMAParameter("text", "Upgrade", 5),
            new FIGMAParameter("x", (float)0.0f, 5),
            new FIGMAParameter("y", (float)0.0f, 5),
            new FIGMAParameter("action", new object[] { action, 0 }, 6)
            );
        FIGMAStructure.ElementType[] et = new FIGMAStructure.ElementType[]
        {
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
            FIGMAStructure.ElementType.Background, FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Action,
        };
        figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 2, 3, 2 }, et));
        for (int i = 0; i < myTeam.academy.GetPlayersCount(); i++)
        {
            menuelements[i + 1] = Instantiate(farray, fspawner.transform, false);
            rt = (RectTransform)menuelements[i + 1].GetComponent(typeof(RectTransform));
            //m = menuelhelp;
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
            menuelhelp -= Mathf.RoundToInt(rt.rect.height);
            figma = (FIGMAStructure)menuelements[i + 1].GetComponentInChildren(typeof(FIGMAStructure));
            objects = FIGMAStructure.MakeParams(
                new FIGMAParameter("sprite", nations[myTeam.academy.GetPlayerNationality(i)].sprite, 0),
                new FIGMAParameter("text", myTeam.academy.GetPlayerNickname(i) + ", " + myTeam.academy.GetPlayerAge(i).ToString(), 1),
                new FIGMAParameter("text", "Nickname", 2),
                new FIGMAParameter("text", myTeam.academy.GetPlayerStrength(i).ToString(), 3),
                new FIGMAParameter("color", "#2A3C44", 4),
                new FIGMAParameter("text", "Current", 5),
                new FIGMAParameter("sprite", roles[myTeam.academy.GetPlayerRole(i)].sprite, 6),
                new FIGMAParameter("text", myTeam.academy.GetPlayerPotential(i).ToString(), 7),
                new FIGMAParameter("text", "Target", 8)
                );
            et = new FIGMAStructure.ElementType[]
            {
                FIGMAStructure.ElementType.Flag, FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
                FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Background, FIGMAStructure.ElementType.Subtitle, FIGMAStructure.ElementType.Flag,
                FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
            };
            figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 3, 4, 2 }, et));
        }
        rt = (RectTransform)fspawner.GetComponent(typeof(RectTransform));
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, m - menuelhelp);
    }

    public void MenuChooseTA(object o)
    {
        if (showAds == 202) MenuChoosePro();
        else MenuChooseNotPro();
    }
    public void MenuChoosePro()
    {
        MenuReset();
        FIGMAMenu.menu.HideMenu();
        int a = Mathf.Clamp(definedTeams.teams.Count, 0, 64);
        menuelements = new GameObject[a + 1];
        menuelhelp -= 96;
        int m = menuelhelp;
        RectTransform rt;
        object[] objects;
        FIGMAStructure.ElementType[] et;
        FIGMAStructure figma;
        System.Action<object> action;
        menuelements[0] = Instantiate(farray, fspawner.transform, false);
        rt = (RectTransform)menuelements[0].GetComponent(typeof(RectTransform));
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height);
        figma = (FIGMAStructure)menuelements[0].GetComponentInChildren(typeof(FIGMAStructure));
        action = MenuCreateTeam;
        objects = FIGMAStructure.MakeParams(
            new FIGMAParameter("text", "Create Team", 5),
            new FIGMAParameter("x", (float)0.0f, 5),
            new FIGMAParameter("y", (float)0.0f, 5),
            new FIGMAParameter("action", new object[] { action, "." }, 6)
        );
        et = new FIGMAStructure.ElementType[]
        {
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Action
        };
        figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 2 }, et));
        definedTeams.GroupTeams();
        action = TeamOffer;
        for (int i = 0; i < a; i++)
        {
            menuelements[i + 1] = Instantiate(farray, fspawner.transform, false);
            rt = (RectTransform)menuelements[i + 1].GetComponent(typeof(RectTransform));
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
            menuelhelp -= Mathf.RoundToInt(rt.rect.height);
            figma = (FIGMAStructure)menuelements[i + 1].GetComponentInChildren(typeof(FIGMAStructure));
            Team t = definedTeams.GetTeamPlacement(i);
            Sprite sprite = Modding.modding.GetLogo(t.teamName);
            if (sprite == null)
            {
                objects = FIGMAStructure.MakeParams(
                    new FIGMAParameter("text", t.teamName + ", #" + definedTeams.GetTeamPlacement(t.teamName).ToString(), 0),
                    new FIGMAParameter("text", "Team", 1),
                    new FIGMAParameter("color", "#2A3C44", 12),
                    new FIGMAParameter("text", ((int)(t.GetFORMPoints() * Teams.form + t.GetLANPoints() * Teams.lan + t.GetACHPoints() * Teams.ach)).ToString() + "/1000", 13),
                    new FIGMAParameter("text", "Points", 14),
                    new FIGMAParameter("text", "Choose", 15),
                    new FIGMAParameter("x", (float)0.0f, 15),
                    new FIGMAParameter("y", (float)0.0f, 15),
                    new FIGMAParameter("action", new object[] { action, i }, 16)
                );
                et = new FIGMAStructure.ElementType[]
                {
                    FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
                    FIGMAStructure.ElementType.Background, FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
                    FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Action
                };
                figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 2, 3, 2 }, et));
            }
            else
            {
                objects = FIGMAStructure.MakeParams(
                    new FIGMAParameter("text", t.teamName + ", #" + definedTeams.GetTeamPlacement(t.teamName).ToString(), 0),
                    new FIGMAParameter("text", "Team", 1),
                    new FIGMAParameter("sprite", sprite, 2),
                    new FIGMAParameter("color", "#2A3C44", 12),
                    new FIGMAParameter("text", ((int)(t.GetFORMPoints() * Teams.form + t.GetLANPoints() * Teams.lan + t.GetACHPoints() * Teams.ach)).ToString() + "/1000", 13),
                    new FIGMAParameter("text", "Points", 14),
                    new FIGMAParameter("text", "Choose", 15),
                    new FIGMAParameter("x", (float)0.0f, 15),
                    new FIGMAParameter("y", (float)0.0f, 15),
                    new FIGMAParameter("action", new object[] { action, i }, 16)
                );
                et = new FIGMAStructure.ElementType[]
                {
                    FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle, FIGMAStructure.ElementType.Flag,
                    FIGMAStructure.ElementType.Background, FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
                    FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Action
                };
                figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 3, 3, 2 }, et));
            }
        }
        rt = (RectTransform)fspawner.GetComponent(typeof(RectTransform));
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, m - menuelhelp);
    }

    public void MenuChooseNotPro()
    {
        MenuReset();
        FIGMAMenu.menu.HideMenu();
        menuelements = new GameObject[6];
        menuelhelp -= 96;
        int m = menuelhelp;
        RectTransform rt;
        object[] objects;
        FIGMAStructure.ElementType[] et;
        FIGMAStructure figma;
        System.Action<object> action;
        int ao = Mathf.Clamp(definedTeams.teams.Count - 1 - Mathf.RoundToInt(stagexp * 0.00004f), 8, definedTeams.teams.Count - 1); // 250k
        //int ai = ao - 10;
        for (int i = 0; i < 5; i++)
        {
            menuelements[i] = Instantiate(farray, fspawner.transform, false);
            rt = (RectTransform)menuelements[i].GetComponent(typeof(RectTransform));
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
            menuelhelp -= Mathf.RoundToInt(rt.rect.height);
            figma = (FIGMAStructure)menuelements[i].GetComponentInChildren(typeof(FIGMAStructure));
            action = TeamOffer;
            Team t = definedTeams.GetTeamPlacement(ao);
            Sprite sprite = Modding.modding.GetLogo(t.teamName);
            if (sprite == null)
            {
                objects = FIGMAStructure.MakeParams(
                    new FIGMAParameter("text", t.teamName + ", #" + definedTeams.GetTeamPlacement(t.teamName).ToString(), 0),
                    new FIGMAParameter("text", "Team", 1),
                    new FIGMAParameter("color", "#2A3C44", 12),
                    new FIGMAParameter("text", ((int)(t.GetFORMPoints() * Teams.form + t.GetLANPoints() * Teams.lan + t.GetACHPoints() * Teams.ach)).ToString() + "/1000", 13),
                    new FIGMAParameter("text", "Points", 14),
                    new FIGMAParameter("text", "Choose", 15),
                    new FIGMAParameter("x", (float)0.0f, 15),
                    new FIGMAParameter("y", (float)0.0f, 15),
                    new FIGMAParameter("action", new object[] { action, ao }, 16)
                );
                et = new FIGMAStructure.ElementType[]
                {
                    FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
                    FIGMAStructure.ElementType.Background, FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
                    FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Action
                };
                figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 2, 3, 2 }, et));
            }
            else
            {
                objects = FIGMAStructure.MakeParams(
                    new FIGMAParameter("text", t.teamName + ", #" + definedTeams.GetTeamPlacement(t.teamName).ToString(), 0),
                    new FIGMAParameter("text", "Team", 1),
                    new FIGMAParameter("sprite", sprite, 2),
                    new FIGMAParameter("color", "#2A3C44", 12),
                    new FIGMAParameter("text", ((int)(t.GetFORMPoints() * Teams.form + t.GetLANPoints() * Teams.lan + t.GetACHPoints() * Teams.ach)).ToString() + "/1000", 13),
                    new FIGMAParameter("text", "Points", 14),
                    new FIGMAParameter("text", "Choose", 15),
                    new FIGMAParameter("x", (float)0.0f, 15),
                    new FIGMAParameter("y", (float)0.0f, 15),
                    new FIGMAParameter("action", new object[] { action, ao }, 16)
                );
                et = new FIGMAStructure.ElementType[]
                {
                    FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle, FIGMAStructure.ElementType.Flag,
                    FIGMAStructure.ElementType.Background, FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
                    FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Action
                };
                figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 3, 3, 2 }, et));
            }
            ao -= Random.Range(1, 3);
        }
        menuelements[5] = Instantiate(farray, fspawner.transform, false);
        rt = (RectTransform)menuelements[5].GetComponent(typeof(RectTransform));
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height);
        figma = (FIGMAStructure)menuelements[5].GetComponentInChildren(typeof(FIGMAStructure));
        action = MenuCreateTeam;
        objects = FIGMAStructure.MakeParams(
            new FIGMAParameter("text", "Create Team", 5),
            new FIGMAParameter("x", (float)0.0f, 5),
            new FIGMAParameter("y", (float)0.0f, 5),
            new FIGMAParameter("action", new object[] { action, "." }, 6)
        );
        et = new FIGMAStructure.ElementType[]
        {
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Action
        };
        figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 2 }, et));
        rt = (RectTransform)fspawner.GetComponent(typeof(RectTransform));
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, m - menuelhelp);
    }

    public void MenuChooseSave()
    {
        MenuReset();
        FIGMAMenu.menu.HideMenu();
        menuelements = new GameObject[5];
        menuelhelp -= 96;
        int m = menuelhelp;
        RectTransform rt;
        object[] objects;
        FIGMAStructure.ElementType[] et;
        FIGMAStructure figma;
        System.Action<object> action = LoadGame;
        System.Action<object> action2 = DeleteGame;
        //int ai = ao - 10;
        for (int i = 0; i < 5; i++)
        {
            menuelements[i] = Instantiate(farray, fspawner.transform, false);
            rt = (RectTransform)menuelements[i].GetComponent(typeof(RectTransform));
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
            menuelhelp -= Mathf.RoundToInt(rt.rect.height);
            figma = (FIGMAStructure)menuelements[i].GetComponentInChildren(typeof(FIGMAStructure));
            SaveManager.save = $"save{i + 1}";
            Save saveState = new Save();
            if (SaveManager.SaveExists("save")) saveState = SaveManager.Load<Save>("save");
            objects = FIGMAStructure.MakeParams(
                new FIGMAParameter("text", saveState.myteam != null ? saveState.myteam : "Empty Slot", 0),
                new FIGMAParameter("text", $"Slot {i + 1}", 1),
                new FIGMAParameter("color", "#2A3C44", 2),
                new FIGMAParameter("text", "Choose", 5),
                new FIGMAParameter("x", (float)0.0f, 5),
                new FIGMAParameter("y", (float)0.0f, 5),
                new FIGMAParameter("action", new object[] { action, i + 1 }, 6),
                new FIGMAParameter("text", "Delete", 7),
                new FIGMAParameter("x", (float)0.0f, 7),
                new FIGMAParameter("y", (float)0.0f, 7),
                new FIGMAParameter("action", new object[] { action2, i + 1 }, 8)
            );
            et = new FIGMAStructure.ElementType[]
            {
                FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
                FIGMAStructure.ElementType.Background, FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Action,
                FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Action
            };
            figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 2, 3, 2 }, et));
        }
        rt = (RectTransform)fspawner.GetComponent(typeof(RectTransform));
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, m - menuelhelp);
    }

    public void MenuChooseDifficulty()
    {
        MenuReset();
        FIGMAMenu.menu.HideMenu();
        menuelements = new GameObject[6];
        menuelhelp -= 96;
        int m = menuelhelp;
        RectTransform rt;
        object[] objects;
        FIGMAStructure.ElementType[] et;
        FIGMAStructure figma;
        System.Action<object> action = SetDifficulty;
        //int ai = ao - 10;
        string[] table = new string[] { "Easy", "Medium", "Hard", "Demon" };
        int[] ttable = new int[] { 140, 200, 280, 444 };
        for (int i = 0; i < 4; i++)
        {
            menuelements[i] = Instantiate(farray, fspawner.transform, false);
            rt = (RectTransform)menuelements[i].GetComponent(typeof(RectTransform));
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
            menuelhelp -= Mathf.RoundToInt(rt.rect.height);
            figma = (FIGMAStructure)menuelements[i].GetComponentInChildren(typeof(FIGMAStructure));
            objects = FIGMAStructure.MakeParams(
                new FIGMAParameter("text", table[i], 0),
                //new FIGMAParameter("text", "Difficulty", 1),
                new FIGMAParameter("text", ttable[i].ToString() + " points", 1),
                new FIGMAParameter("color", "#2A3C44", 2),
                new FIGMAParameter("text", "Choose", 5),
                new FIGMAParameter("x", (float)0.0f, 5),
                new FIGMAParameter("y", (float)0.0f, 5),
                new FIGMAParameter("action", new object[] { action, ttable[i] }, 6)
            );
            et = new FIGMAStructure.ElementType[]
            {
                FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
                FIGMAStructure.ElementType.Background, FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Action
            };
            figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 2, 3 }, et));
        }
        menuelements[4] = Instantiate(finput, fspawner.transform, false);
        rt = (RectTransform)menuelements[4].GetComponent(typeof(RectTransform));
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp + 32);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height);
        FIGMAInput figmai = (FIGMAInput)menuelements[4].GetComponentInChildren(typeof(FIGMAInput));
        figmai.SetAction(SetCustomDifficulty);
        figmai.SetMode(InputField.ContentType.IntegerNumber);
        figmai.SetText("Custom Difficulty");
        if (customdiff != 0)
        {
            figmai.SetString(customdiff.ToString());
            menuelements[5] = Instantiate(farray, fspawner.transform, false);
            rt = (RectTransform)menuelements[5].GetComponent(typeof(RectTransform));
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
            menuelhelp -= Mathf.RoundToInt(rt.rect.height);
            figma = (FIGMAStructure)menuelements[5].GetComponentInChildren(typeof(FIGMAStructure));
            objects = FIGMAStructure.MakeParams(
                new FIGMAParameter("text", "Continue with custom", 5),
                new FIGMAParameter("x", (float)0.0f, 5),
                new FIGMAParameter("y", (float)0.0f, 5),
                new FIGMAParameter("action", new object[] { action, customdiff }, 6)
            );
            et = new FIGMAStructure.ElementType[]
            {
            FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Action
            };
            figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 2 }, et));
        }
        rt = (RectTransform)fspawner.GetComponent(typeof(RectTransform));
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, m - menuelhelp);
    }
    public void SetCustomDifficulty(string text)
    {
        customdiff = Mathf.Clamp(int.Parse(text), 1, 999);
        MenuChooseDifficulty();
    }
    public int customdiff = 0;
    public void MenuStatTeam()
    {
        MenuReset();
        menuelements = new GameObject[definedTeams.teams.Count > 64 ? 64 : definedTeams.teams.Count];
        menuelhelp -= 96;
        int m = menuelhelp;
        RectTransform rt;
        object[] objects;
        FIGMAStructure.ElementType[] et;
        FIGMAStructure figma;
        System.Action<object> action = MenuAnyTeam;
        for (int i = 0; i < menuelements.Length; i++)
        {
            menuelements[i] = Instantiate(farray, fspawner.transform, false);
            rt = (RectTransform)menuelements[i].GetComponent(typeof(RectTransform));
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
            menuelhelp -= Mathf.RoundToInt(rt.rect.height);
            figma = (FIGMAStructure)menuelements[i].GetComponentInChildren(typeof(FIGMAStructure));
            Team t = definedTeams.GetTeamPlacement(i);
            Sprite sprite = Modding.modding.GetLogo(t.teamName);
            if (sprite == null)
            {
                objects = FIGMAStructure.MakeParams(
                    new FIGMAParameter("text", t.teamName + ", #" + definedTeams.GetTeamPlacement(t.teamName).ToString(), 0),
                    new FIGMAParameter("text", "Team", 1),
                    new FIGMAParameter("action", new object[] { action, (string)t.teamName }, 2),
                    new FIGMAParameter("color", "#2A3C44", 3),
                    new FIGMAParameter("text", ((int)(t.GetFORMPoints() * Teams.form)).ToString() + "/200", 4),
                    new FIGMAParameter("text", "Form", 5),
                    new FIGMAParameter("text", ((int)(t.GetLANPoints() * Teams.lan)).ToString() + "/300", 6),
                    new FIGMAParameter("text", "LAN", 7),
                    new FIGMAParameter("color", "#2A3C44", 8),
                    new FIGMAParameter("text", ((int)(t.GetACHPoints() * Teams.ach)).ToString() + "/500", 9),
                    new FIGMAParameter("text", "Achievements", 10)
                );
                et = new FIGMAStructure.ElementType[]
                {
                    FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle, FIGMAStructure.ElementType.Action,
                    FIGMAStructure.ElementType.Background, FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
                    FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
                    FIGMAStructure.ElementType.Background, FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
                };
                figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 3, 3, 2, 3 }, et), new int[] { 600, 420, 420, 480 });
            }
            else
            {
                objects = FIGMAStructure.MakeParams(
                    new FIGMAParameter("text", t.teamName + ", #" + definedTeams.GetTeamPlacement(t.teamName).ToString(), 0),
                    new FIGMAParameter("text", "Team", 1),
                    new FIGMAParameter("action", new object[] { action, (string)t.teamName }, 2),
                    new FIGMAParameter("sprite", sprite, 3),
                    new FIGMAParameter("color", "#2A3C44", 4),
                    new FIGMAParameter("text", ((int)(t.GetFORMPoints() * Teams.form)).ToString() + "/200", 5),
                    new FIGMAParameter("text", "Form", 6),
                    new FIGMAParameter("text", ((int)(t.GetLANPoints() * Teams.lan)).ToString() + "/300", 7),
                    new FIGMAParameter("text", "LAN", 8),
                    new FIGMAParameter("color", "#2A3C44", 9),
                    new FIGMAParameter("text", ((int)(t.GetACHPoints() * Teams.ach)).ToString() + "/500", 10),
                    new FIGMAParameter("text", "Achievements", 11)
                );
                et = new FIGMAStructure.ElementType[]
                {
                    FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle, FIGMAStructure.ElementType.Action, FIGMAStructure.ElementType.Flag,
                    FIGMAStructure.ElementType.Background, FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
                    FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
                    FIGMAStructure.ElementType.Background, FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
                };
                figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 4, 3, 2, 3 }, et), new int[] { 600, 420, 420, 480 });
            }
        }
        rt = (RectTransform)fspawner.GetComponent(typeof(RectTransform));
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, m - menuelhelp);
    }

    public void MenuStatPlayer()
    {
        MenuReset();
        menuelhelp -= 96;
        int m = menuelhelp;
        RectTransform rt;
        object[] objects;
        FIGMAStructure.ElementType[] et;
        FIGMAStructure figma;
        System.Action<object> action = MenuPlayer;
        List<Player> pla = definedPlayers.players.OrderByDescending(x => x.FullStat(30).rating).ToList();
        menuelements = new GameObject[pla.Count > 64 ? 64 : pla.Count];
        for (int i = 0; i < menuelements.Length; i++)
        {
            menuelements[i] = Instantiate(farray, fspawner.transform, false);
            rt = (RectTransform)menuelements[i].GetComponent(typeof(RectTransform));
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
            menuelhelp -= Mathf.RoundToInt(rt.rect.height);
            figma = (FIGMAStructure)menuelements[i].GetComponentInChildren(typeof(FIGMAStructure));
            Player p = definedPlayers.GetPlayer(pla[i].nickname);
            Stat stat = p.FullStat(30);
            int a = (stat.kills - stat.deaths);
            Sprite sprite = Modding.modding.GetLogo(p.nickname);
            if (sprite == null)
            {
                objects = FIGMAStructure.MakeParams(
                    new FIGMAParameter("text", p.nickname + ", " + p.age.ToString(), 0),
                    new FIGMAParameter("text", "Player", 1),
                    new FIGMAParameter("action", new object[] { action, (string)p.nickname }, 2),
                    new FIGMAParameter("color", "#2A3C44", 3),
                    new FIGMAParameter("text", a > 0 ? "+" + a.ToString() : a.ToString(), 4),
                    new FIGMAParameter("text", "K-D Diff", 5),
                    new FIGMAParameter("text", ((double)stat.kills / stat.deaths).ToString("0.00"), 6),
                    new FIGMAParameter("text", "K/D", 7),
                    new FIGMAParameter("color", "#2A3C44", 8),
                    new FIGMAParameter("text", stat.GetRating().ToString("0.00"), 9),
                    new FIGMAParameter("text", "Rating", 10)
                );
                et = new FIGMAStructure.ElementType[]
                {
                    FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle, FIGMAStructure.ElementType.Action,
                    FIGMAStructure.ElementType.Background, FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
                    FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
                    FIGMAStructure.ElementType.Background, FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
                };
                figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 3, 3, 2, 3 }, et), new int[] { 600, 440, 440, 440 });
            }
            else
            {
                objects = FIGMAStructure.MakeParams(
                    new FIGMAParameter("text", p.nickname + ", " + p.age.ToString(), 0),
                    new FIGMAParameter("text", "Player", 1),
                    new FIGMAParameter("action", new object[] { action, (string)p.nickname }, 2),
                    new FIGMAParameter("sprite", sprite, 3),
                    new FIGMAParameter("color", "#2A3C44", 4),
                    new FIGMAParameter("text", a > 0 ? "+" + a.ToString() : a.ToString(), 5),
                    new FIGMAParameter("text", "K-D Diff", 6),
                    new FIGMAParameter("text", ((double)stat.kills / stat.deaths).ToString("0.00"), 7),
                    new FIGMAParameter("text", "K/D", 17),
                    new FIGMAParameter("color", "#2A3C44", 18),
                    new FIGMAParameter("text", stat.GetRating().ToString("0.00"), 19),
                    new FIGMAParameter("text", "Rating", 20)
                );
                et = new FIGMAStructure.ElementType[]
                {
                    FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle, FIGMAStructure.ElementType.Action, FIGMAStructure.ElementType.Flag,
                    FIGMAStructure.ElementType.Background, FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
                    FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
                    FIGMAStructure.ElementType.Background, FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
                };
                figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 4, 3, 2, 3 }, et), new int[] { 600, 440, 440, 440 });
            }
        }
        rt = (RectTransform)fspawner.GetComponent(typeof(RectTransform));
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, m - menuelhelp);
    }

    public void MenuTop20()
    {
        MenuReset();
        menuelhelp -= 96;
        int m = menuelhelp;
        RectTransform rt;
        object[] objects;
        FIGMAStructure.ElementType[] et;
        FIGMAStructure figma;
        System.Action<object> action = MenuPlayer;
        List<Player> pla = definedPlayers.players.OrderByDescending(x => x.yearPerformance).ToList();
        menuelements = new GameObject[20];
        for (int i = 0; i < 20; i++)
        {
            menuelements[i] = Instantiate(farray, fspawner.transform, false);
            rt = (RectTransform)menuelements[i].GetComponent(typeof(RectTransform));
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
            menuelhelp -= Mathf.RoundToInt(rt.rect.height);
            figma = (FIGMAStructure)menuelements[i].GetComponentInChildren(typeof(FIGMAStructure));
            Player p = definedPlayers.GetPlayer(pla[i].nickname);
            p.ReportTop20((byte)(i + 1), (byte)(((day - 1) / 360) + 22));
            Stat stat = p.FullStat(360);
            int a = (stat.kills - stat.deaths);
            Sprite sprite = Modding.modding.GetLogo(p.nickname);
            if (sprite == null)
            {
                objects = FIGMAStructure.MakeParams(
                    new FIGMAParameter("text", p.nickname + ", #" + (i + 1).ToString(), 0),
                    new FIGMAParameter("text", "Player", 1),
                    new FIGMAParameter("color", "#2A3C44", 3),
                    new FIGMAParameter("text", a > 0 ? "+" + a.ToString() : a.ToString(), 4),
                    new FIGMAParameter("text", "K-D Diff", 5),
                    new FIGMAParameter("text", ((double)stat.kills / stat.deaths).ToString("0.00"), 6),
                    new FIGMAParameter("text", "K/D", 7),
                    new FIGMAParameter("color", "#2A3C44", 8),
                    new FIGMAParameter("text", stat.GetRating().ToString("0.00"), 9),
                    new FIGMAParameter("text", "Rating", 10)
                );
                et = new FIGMAStructure.ElementType[]
                {
                    FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
                    FIGMAStructure.ElementType.Background, FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
                    FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
                    FIGMAStructure.ElementType.Background, FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
                };
                figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 2, 3, 2, 3 }, et), new int[] { 600, 440, 440, 440 });
            }
            else
            {
                objects = FIGMAStructure.MakeParams(
                    new FIGMAParameter("text", p.nickname + ", #" + (i + 1).ToString(), 0),
                    new FIGMAParameter("text", "Player", 1),
                    new FIGMAParameter("sprite", sprite, 3),
                    new FIGMAParameter("color", "#2A3C44", 4),
                    new FIGMAParameter("text", a > 0 ? "+" + a.ToString() : a.ToString(), 5),
                    new FIGMAParameter("text", "K-D Diff", 6),
                    new FIGMAParameter("text", ((double)stat.kills / stat.deaths).ToString("0.00"), 7),
                    new FIGMAParameter("text", "K/D", 17),
                    new FIGMAParameter("color", "#2A3C44", 18),
                    new FIGMAParameter("text", stat.GetRating().ToString("0.00"), 19),
                    new FIGMAParameter("text", "Rating", 20)
                );
                et = new FIGMAStructure.ElementType[]
                {
                    FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle, FIGMAStructure.ElementType.Flag,
                    FIGMAStructure.ElementType.Background, FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
                    FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
                    FIGMAStructure.ElementType.Background, FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
                };
                figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 3, 3, 2, 3 }, et), new int[] { 600, 440, 440, 440 });
            }
        }
        for (int i = 0; i < pla.Count; i++) { pla[i].yearPerformance = 0; }
        rt = (RectTransform)fspawner.GetComponent(typeof(RectTransform));
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, m - menuelhelp);
    }

    public void MenuPartners()
    {
        MenuReset();
        menuelhelp -= 96;
        int m = menuelhelp;
        RectTransform rt;
        object[] objects;
        FIGMAStructure.ElementType[] et;
        FIGMAStructure figma;
        //System.Action<object> action;
        menuelements = new GameObject[investors.Count + sponsors.Count];
        for (int i = 0; i < sponsors.Count; i++)
        {
            menuelements[i] = Instantiate(farray, fspawner.transform, false);
            rt = (RectTransform)menuelements[i].GetComponent(typeof(RectTransform));
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
            menuelhelp -= Mathf.RoundToInt(rt.rect.height);
            figma = (FIGMAStructure)menuelements[i].GetComponentInChildren(typeof(FIGMAStructure));
            objects = FIGMAStructure.MakeParams(
                new FIGMAParameter("text", sponsors[i].name, 0),
                new FIGMAParameter("text", "Sponsor", 1),
                new FIGMAParameter("color", "#2A3C44", 3),
                new FIGMAParameter("text", sponsors[i].day, 4),
                new FIGMAParameter("text", "Days left", 5),
                new FIGMAParameter("text", "$" + sponsors[i].money, 6),
                new FIGMAParameter("text", "Declared gain", 7)
                );
            et = new FIGMAStructure.ElementType[]
            {
                FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
                FIGMAStructure.ElementType.Background, FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
                FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
            };
            figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 2, 3, 2 }, et));
        }
        for (int i = 0; i < investors.Count; i++)
        {
            menuelements[i + sponsors.Count] = Instantiate(farray, fspawner.transform, false);
            rt = (RectTransform)menuelements[i + sponsors.Count].GetComponent(typeof(RectTransform));
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
            menuelhelp -= Mathf.RoundToInt(rt.rect.height);
            figma = (FIGMAStructure)menuelements[i + sponsors.Count].GetComponentInChildren(typeof(FIGMAStructure));
            objects = FIGMAStructure.MakeParams(
                new FIGMAParameter("text", investors[i].name, 0),
                new FIGMAParameter("text", "Sponsor", 1),
                new FIGMAParameter("color", "#2A3C44", 3),
                new FIGMAParameter("text", investors[i].day, 4),
                new FIGMAParameter("text", "Days left", 5),
                new FIGMAParameter("text", $"${investors[i].money}, {investors[i].percent}%", 6),
                new FIGMAParameter("text", "Declared gain", 7)
                );
            et = new FIGMAStructure.ElementType[]
            {
                FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
                FIGMAStructure.ElementType.Background, FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
                FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
            };
            figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 2, 3, 2 }, et));
        }
        rt = (RectTransform)fspawner.GetComponent(typeof(RectTransform));
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, m - menuelhelp);
    }
    [SerializeField] private GameObject welcgo, mtchgo;
    public void MenuWelcome()
    {
        MenuReset();
        FIGMAMenu.menu.HideMenu();
        welcgo.SetActive(true);
    }

    public void MenuPickban()
    {
        MenuReset();
        FIGMAMenu.menu.HideMenu();
        menuelhelp -= 96;
        int m = menuelhelp;
        RectTransform rt;
        object[] objects;
        FIGMAStructure.ElementType[] et;
        FIGMAStructure figma;
        System.Action<object> action = !showMatchG ? ChooseMap : ChooseMapA;
        menuelements = new GameObject[3];
        List<FIGMAParameter> fp = new List<FIGMAParameter>(35);
        byte[] bytes = new byte[7] { 4, 4, 4, 4, 4, 4, 4 };
        List<FIGMAStructure.ElementType> ets = new List<FIGMAStructure.ElementType>(28);
        menuelements[0] = Instantiate(farray, fspawner.transform, false);
        rt = (RectTransform)menuelements[0].GetComponent(typeof(RectTransform));
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height);
        figma = (FIGMAStructure)menuelements[0].GetComponentInChildren(typeof(FIGMAStructure));
        Sprite sprite1 = Modding.modding.GetLogo(match.team1.teamName);
        Sprite sprite2 = Modding.modding.GetLogo(match.team2.teamName);
        List<FIGMAParameter> lol = new List<FIGMAParameter>(4);
        List<FIGMAStructure.ElementType> elol = new List<FIGMAStructure.ElementType>(4);
        List<byte> blol = new List<byte>(2);
        if (sprite1 == null)
        {
            lol.Add(new FIGMAParameter("text", match.team1.teamName, 0));
            lol.Add(new FIGMAParameter("text", "Team 1", 1));
            elol.Add(FIGMAStructure.ElementType.TextValue);
            elol.Add(FIGMAStructure.ElementType.Subtitle);
            blol.Add(2);
        }
        else
        {
            lol.Add(new FIGMAParameter("text", match.team1.teamName, 0));
            lol.Add(new FIGMAParameter("text", "Team 1", 1));
            lol.Add(new FIGMAParameter("sprite", sprite1, 2));
            elol.Add(FIGMAStructure.ElementType.TextValue);
            elol.Add(FIGMAStructure.ElementType.Subtitle);
            elol.Add(FIGMAStructure.ElementType.Flag);
            blol.Add(3);
        }
        if (sprite2 == null)
        {
            lol.Add(new FIGMAParameter("text", match.team2.teamName, 3));
            lol.Add(new FIGMAParameter("text", "Team 2", 4));
            elol.Add(FIGMAStructure.ElementType.TextValue);
            elol.Add(FIGMAStructure.ElementType.Subtitle);
            blol.Add(2);
        }
        else
        {
            lol.Add(new FIGMAParameter("text", match.team2.teamName, 3));
            lol.Add(new FIGMAParameter("text", "Team 2", 4));
            lol.Add(new FIGMAParameter("sprite", sprite2, 5));
            elol.Add(FIGMAStructure.ElementType.TextValue);
            elol.Add(FIGMAStructure.ElementType.Subtitle);
            elol.Add(FIGMAStructure.ElementType.Flag);
            blol.Add(3);
        }
        objects = FIGMAStructure.MakeParamsA(lol.ToArray());
        et = elol.ToArray();
        figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, blol.ToArray(), et));
        menuelements[1] = Instantiate(farray, fspawner.transform, false);
        rt = (RectTransform)menuelements[1].GetComponent(typeof(RectTransform));
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height);
        figma = (FIGMAStructure)menuelements[1].GetComponentInChildren(typeof(FIGMAStructure));
        Player p1 = match.team1.players[0];
        Player p2 = match.team2.players[0];
        for (int i = 1; i < 5; i++)
        {
            if (p1.GetRating() < match.team1.players[i].FullStat().rating) p1 = match.team1.players[i];
            if (p2.GetRating() < match.team2.players[i].FullStat().rating) p2 = match.team2.players[i];
        }
        sprite1 = Modding.modding.GetLogo(p1.nickname);
        sprite2 = Modding.modding.GetLogo(p2.nickname);
        lol = new List<FIGMAParameter>(7);
        elol = new List<FIGMAStructure.ElementType>(7);
        blol = new List<byte>(3);
        if (sprite1 == null)
        {
            lol.Add(new FIGMAParameter("text", p1.nickname + ", " + p1.GetRating().ToString("0.00"), 0));
            lol.Add(new FIGMAParameter("text", "Highlighted Player", 1));
            lol.Add(new FIGMAParameter("color", "#2A3C44", 3));
            lol.Add(new FIGMAParameter("text", "BO" + ((int)match.type * 2 + 1).ToString(), 4));
            lol.Add(new FIGMAParameter("text", "Match Format", 5));
            elol.Add(FIGMAStructure.ElementType.TextValue);
            elol.Add(FIGMAStructure.ElementType.Subtitle);
            elol.Add(FIGMAStructure.ElementType.Background);
            elol.Add(FIGMAStructure.ElementType.TextValue);
            elol.Add(FIGMAStructure.ElementType.Subtitle);
            blol.Add(2);
            blol.Add(3);
        }
        else
        {
            lol.Add(new FIGMAParameter("text", p1.nickname + ", " + p1.GetRating().ToString("0.00"), 0));
            lol.Add(new FIGMAParameter("text", "Highlighted Player", 1));
            lol.Add(new FIGMAParameter("sprite", sprite1, 2));
            lol.Add(new FIGMAParameter("color", "#2A3C44", 3));
            lol.Add(new FIGMAParameter("text", "BO" + ((int)match.type * 2 + 1).ToString(), 4));
            lol.Add(new FIGMAParameter("text", "Match Format", 5));
            elol.Add(FIGMAStructure.ElementType.TextValue);
            elol.Add(FIGMAStructure.ElementType.Subtitle);
            elol.Add(FIGMAStructure.ElementType.Flag);
            elol.Add(FIGMAStructure.ElementType.Background);
            elol.Add(FIGMAStructure.ElementType.TextValue);
            elol.Add(FIGMAStructure.ElementType.Subtitle);
            blol.Add(3);
            blol.Add(3);
        }
        if (sprite2 == null)
        {
            lol.Add(new FIGMAParameter("text", p2.nickname + ", " + p2.GetRating().ToString("0.00"), 6));
            lol.Add(new FIGMAParameter("text", "Highlighted Player", 7));
            elol.Add(FIGMAStructure.ElementType.TextValue);
            elol.Add(FIGMAStructure.ElementType.Subtitle);
            blol.Add(2);
        }
        else
        {
            lol.Add(new FIGMAParameter("text", p2.nickname + ", " + p2.GetRating().ToString("0.00"), 6));
            lol.Add(new FIGMAParameter("text", "Highlighted Player", 7));
            lol.Add(new FIGMAParameter("sprite", sprite2, 8));
            elol.Add(FIGMAStructure.ElementType.TextValue);
            elol.Add(FIGMAStructure.ElementType.Subtitle);
            elol.Add(FIGMAStructure.ElementType.Flag);
            blol.Add(3);
        }
        objects = FIGMAStructure.MakeParamsA(lol.ToArray());
        et = elol.ToArray();
        figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, blol.ToArray(), et));
        for (int i = 0; i < 7; i++)
        {
            fp.Add(new FIGMAParameter("sprite", fmaps[i], (byte)(i * 4)));
            if (match.type == MatchType.BO1)
            {
                fp.Add(new FIGMAParameter("color", match.pickbans.Contains((byte)i) ? (match.pickbans.IndexOf((byte)i) == 6 ? "#40DF9F" : "#DF405E") : "#30444E", (byte)(i * 4)));
            }
            else if (match.type == MatchType.BO3)
            {
                fp.Add(new FIGMAParameter("color", match.pickbans.Contains((byte)i) ? (
                    match.pickbans.IndexOf((byte)i) == 6 || match.pickbans.IndexOf((byte)i) == 3 ||
                    match.pickbans.IndexOf((byte)i) == 2 ? "#40DF9F" : "#DF405E") : "#30444E", (byte)(i * 4)));
            }
            else if (match.type == MatchType.BO5)
            {
                fp.Add(new FIGMAParameter("color", match.pickbans.Contains((byte)i) ? (
                    match.pickbans.IndexOf((byte)i) == 0 || match.pickbans.IndexOf((byte)i) == 1
                    ? "#DF405E" : "#40DF9F") : "#30444E", (byte)(i * 4)));
            }
            if (myTeam.teamName == match.team1.teamName || myTeam.teamName == match.team2.teamName)
                fp.Add(new FIGMAParameter("text", myTeam.GetMap((Map)i).ToString() + "/10", (byte)(i * 4 + 1)));
            else fp.Add(new FIGMAParameter("text", string.Empty, (byte)(i * 4 + 1)));
            fp.Add(new FIGMAParameter("text", ((Map)i).ToString(), (byte)(i * 4 + 2)));
            fp.Add(new FIGMAParameter("action", new object[] { action, i }, (byte)(i * 4 + 3)));
            ets.AddRange(new FIGMAStructure.ElementType[]
            { FIGMAStructure.ElementType.Background, FIGMAStructure.ElementType.TextValue,
                FIGMAStructure.ElementType.Subtitle, FIGMAStructure.ElementType.Action });
        }
        menuelements[2] = Instantiate(farray, fspawner.transform, false);
        rt = (RectTransform)menuelements[2].GetComponent(typeof(RectTransform));
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height);
        figma = (FIGMAStructure)menuelements[2].GetComponentInChildren(typeof(FIGMAStructure));
        objects = FIGMAStructure.MakeParamsA(fp.ToArray());
        figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, bytes, ets.ToArray()));
        rt = (RectTransform)fspawner.GetComponent(typeof(RectTransform));
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, m - menuelhelp);
    }

    public void MenuMatch()
    {
        MenuReset();
        FIGMAMenu.menu.HideMenu();
        matchUI.SetActive(true);
    }

    public void MenuResults(object o)
    {
        MenuReset(true);
        FIGMAMenu.menu.HideMenu();
        matchUI.SetActive(false);
        pickbansUI.SetActive(false);
        menuelhelp -= 96;
        int m = menuelhelp;
        RectTransform rt;
        object[] objects;
        FIGMAStructure.ElementType[] et;
        FIGMAStructure figma;
        System.Action<object> action = MenuResults;
        menuelements = new GameObject[13];
        int duhast;
        if (o == null) duhast = 0;
        else duhast = (int)o;
        byte[] mres = tempResults[tempResults.Count - 1].result;
        List<string> str = new List<string>(5);
        List<FIGMAParameter> ob = new List<FIGMAParameter>();
        List<FIGMAStructure.ElementType> ets = new List<FIGMAStructure.ElementType>();
        List<byte> bytes = new List<byte>();
        ob.Add(new FIGMAParameter("text", "All", 0));
        ob.Add(new FIGMAParameter("x", (float)0.0f, 0));
        ob.Add(new FIGMAParameter("y", (float)0.0f, 0));
        ob.Add(new FIGMAParameter("action", new object[] { action, 0 }, 1));
        ets.Add(FIGMAStructure.ElementType.TextValue);
        ets.Add(FIGMAStructure.ElementType.Action);
        bytes.Add(2);
        for (byte i = 0; i < mres[15]; i++)
        {
            switch ((Map)mres[10 + i])
            {
                case Map.Mirage:
                    str.Add("Mirage");
                    break;
                case Map.Dust:
                    str.Add("Dust2");
                    break;
                case Map.Inferno:
                    str.Add("Inferno");
                    break;
                case Map.Nuke:
                    str.Add("Nuke");
                    break;
                case Map.Overpass:
                    str.Add("Overpass");
                    break;
                case Map.Vertigo:
                    str.Add("Vertigo");
                    break;
                case Map.Ancient:
                    str.Add("Ancient");
                    break;
                default:
                    str.Add("BUG");
                    break;
            }
            ob.Add(new FIGMAParameter("text", mres[i].ToString() + "-" + mres[i + 5].ToString(), (byte)(i * 3 + 2)));
            ob.Add(new FIGMAParameter("action", new object[] { action, i + 1 }, (byte)(i * 3 + 3)));
            ob.Add(new FIGMAParameter("text", str[i], (byte)(i * 3 + 4)));
            ets.Add(FIGMAStructure.ElementType.TextValue);
            ets.Add(FIGMAStructure.ElementType.Action);
            ets.Add(FIGMAStructure.ElementType.Subtitle);
            bytes.Add(3);
        }
        menuelements[0] = Instantiate(farray, fspawner.transform, false);
        rt = (RectTransform)menuelements[0].GetComponent(typeof(RectTransform));
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height);
        figma = (FIGMAStructure)menuelements[0].GetComponentInChildren(typeof(FIGMAStructure));
        objects = FIGMAStructure.MakeParamsA(ob.ToArray());
        figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, bytes.ToArray(), ets.ToArray()));
        ob = new List<FIGMAParameter>(4);
        ets = new List<FIGMAStructure.ElementType>(4);
        //bytes = new List<byte>(2);
        ob.Add(new FIGMAParameter("text", match.team1.teamName, 0));
        ob.Add(new FIGMAParameter("text", "Team 1", 1));
        ob.Add(new FIGMAParameter("text", match.team2.teamName, 2));
        ob.Add(new FIGMAParameter("text", "Team 2", 3));
        ets.Add(FIGMAStructure.ElementType.TextValue);
        ets.Add(FIGMAStructure.ElementType.Subtitle);
        ets.Add(FIGMAStructure.ElementType.TextValue);
        ets.Add(FIGMAStructure.ElementType.Subtitle);
        menuelements[1] = Instantiate(farray, fspawner.transform, false);
        rt = (RectTransform)menuelements[1].GetComponent(typeof(RectTransform));
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height);
        figma = (FIGMAStructure)menuelements[1].GetComponentInChildren(typeof(FIGMAStructure));
        objects = FIGMAStructure.MakeParamsA(ob.ToArray());
        figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 2, 2 }, ets.ToArray()));
        Stat[] p1 = new Stat[5];
        Stat[] p2 = new Stat[5];
        for (byte i = 0; i < 5; i++)
        {
            p1[i] = new Stat().SetStats(tempResults[tempResults.Count - 1].stats[i].nickname);
            byte j = 0;
            if (duhast == 0)
            {
                for (j = 0; j * 10 < tempResults[tempResults.Count - 1].stats.Count; j++)
                {
                    p1[i].AddStats(tempResults[tempResults.Count - 1].stats[j * 10 + i]);
                }
                p1[i].kast /= j;
            }
            else
            {
                j = (byte)(duhast - 1);
                p1[i].AddStats(tempResults[tempResults.Count - 1].stats[j * 10 + i]);
            }
            p2[i] = new Stat().SetStats(tempResults[tempResults.Count - 1].stats[i + 5].nickname);
            if (duhast == 0)
            {
                for (j = 0; j * 10 < tempResults[tempResults.Count - 1].stats.Count; j++)
                {
                    p2[i].AddStats(tempResults[tempResults.Count - 1].stats[j * 10 + i + 5]);
                }
                p2[i].kast /= j;
            }
            else
            {
                p2[i].AddStats(tempResults[tempResults.Count - 1].stats[j * 10 + i + 5]);
            }
            p1[i].CalculateRating();
            p2[i].CalculateRating();
        }
        p1 = p1.OrderByDescending(x => x.rating).ToArray();
        p2 = p2.OrderByDescending(x => x.rating).ToArray();
        for (int i = 0; i < 10; i++)
        {
            menuelements[i + 2] = Instantiate(farray, fspawner.transform, false);
            rt = (RectTransform)menuelements[i + 2].GetComponent(typeof(RectTransform));
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
            menuelhelp -= Mathf.RoundToInt(rt.rect.height);
            figma = (FIGMAStructure)menuelements[i + 2].GetComponentInChildren(typeof(FIGMAStructure));
            action = MenuPlayer;
            Stat stat = i < 5 ? p1[i] : p2[i - 5];
            if (i == 4) menuelhelp -= 48;
            double adr = (double)stat.damage / stat.rounds;
            objects = FIGMAStructure.MakeParams(
                new FIGMAParameter("text", stat.nickname, 0),
                new FIGMAParameter("text", "Player", 1),
                new FIGMAParameter("color", "#2A3C44", 2),
                new FIGMAParameter("text", stat.kills.ToString() + "-" + stat.deaths.ToString(), 3),
                new FIGMAParameter("text", "K-D", 4),
                new FIGMAParameter("text", adr.ToString(adr < 100 ? "00.0" : "000.0"), 5),
                new FIGMAParameter("text", "ADR", 6),
                new FIGMAParameter("color", "#2A3C44", 7),
                new FIGMAParameter("text", (stat.kast).ToString(!Mathf.Approximately(stat.kast, 1) ? "00.0%" : "000.0%"), 8),
                new FIGMAParameter("text", "KAST%", 9),
                new FIGMAParameter("text", stat.GetRating().ToString("0.00"), 10),
                new FIGMAParameter("text", "Rating", 11)
            );
            et = new FIGMAStructure.ElementType[]
            {
                FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
                FIGMAStructure.ElementType.Background, FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
                FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
                FIGMAStructure.ElementType.Background, FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
                FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle
            };
            figma.AddBar(
                FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 2, 3, 2, 3, 2 }, et),
                new int[5] { 740, 295, 295, 295, 295 }
            );
        }
        menuelements[12] = Instantiate(farray, fspawner.transform, false);
        rt = (RectTransform)menuelements[12].GetComponent(typeof(RectTransform));
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height);
        figma = (FIGMAStructure)menuelements[12].GetComponentInChildren(typeof(FIGMAStructure));
        action = MenuPreviewEvent;
        objects = FIGMAStructure.MakeParams(
            new FIGMAParameter("text", "Continue", 0),
            new FIGMAParameter("x", (float)0.0f, 0),
            new FIGMAParameter("y", (float)0.0f, 0),
            new FIGMAParameter("action", new object[2] { action, null }, 1)
        );
        et = new FIGMAStructure.ElementType[2] { FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Action };
        figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[1] { 2 }, et));
        rt = (RectTransform)fspawner.GetComponent(typeof(RectTransform));
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, m - menuelhelp);
    }

    public void MenuCalendar()
    {
        MenuReset();
        menuelhelp -= 96;
        int m = menuelhelp;
        RectTransform rt;
        object[] objects;
        FIGMAStructure.ElementType[] et;
        FIGMAStructure figma;
        System.Action<object> action;
        List<Sprite> sprites = new List<Sprite>(3);
        List<int> days = new List<int>(3);
        List<string> titles = new List<string>(3);
        //List<string> colors = new List<string>(30);
        List<Tournament> lt = Events.events.GetTournaments();
        int d = (day - 1) / 30; // month from 0
        int a = d * 30, b = d * 30 + 30;
        for (int j = 0; j < lt.Count; j++)
        {
            if (lt[j].day >= a && lt[j].day < b)
            { sprites.Add(lt[j].logo); days.Add(lt[j].day % 30); titles.Add(lt[j].title.Split(' ')[0]); }
        }
        //List<Player> pla = definedPlayers.players.OrderByDescending(x => x.FullStat(30).rating).ToList();
        menuelements = new GameObject[6];
        action = MenuPlayer;
        List<FIGMAParameter> os = new List<FIGMAParameter>(105);
        List<FIGMAStructure.ElementType> ets = new List<FIGMAStructure.ElementType>(105);
        byte[] bytes = new byte[] { 3, 3, 3, 3, 3, 3, 3 };
        for (int i = 0; i < 35; i++)
        {
            os.Add(new FIGMAParameter("text", (i - 1).ToString("00"), (byte)(i * 3)));
            ets.Add(FIGMAStructure.ElementType.Subtitle);
            os.Add(new FIGMAParameter("color", (i & 1) == 0 ? "#1F2E35" : "#2A3C44", (byte)(i * 3 + 1)));
            ets.Add(FIGMAStructure.ElementType.Background);
            if (!days.Contains(i - 1))
            {
                os.Add(new FIGMAParameter("text", string.Empty, (byte)(i * 3 + 2)));
                ets.Add(FIGMAStructure.ElementType.TextValue);
            }
            else
            {
                os.Add(new FIGMAParameter("sprite", sprites[days.IndexOf(i - 1)], (byte)(i * 3 + 2)));
                ets.Add(FIGMAStructure.ElementType.Flag);
                os[os.Count - 3] = new FIGMAParameter("text", titles[days.IndexOf(i - 1)], os[os.Count - 3].id);
                //ets[ets.Count - 3] = FIGMAStructure.ElementType.TextValue;
            }
        }
        os[0] = new FIGMAParameter("text", GetDateText(), 0);
        os[3] = new FIGMAParameter("text", string.Empty, 3);
        os[96] = new FIGMAParameter("text", string.Empty, 96);
        os[99] = new FIGMAParameter("text", string.Empty, 99);
        os[102] = new FIGMAParameter("text", string.Empty, 102);
        menuelements[0] = Instantiate(farray, fspawner.transform, false);
        rt = (RectTransform)menuelements[0].GetComponent(typeof(RectTransform));
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
        menuelhelp -= Mathf.RoundToInt(rt.rect.height);
        figma = (FIGMAStructure)menuelements[0].GetComponentInChildren(typeof(FIGMAStructure));
        action = NextDay;
        objects = FIGMAStructure.MakeParams(
            new FIGMAParameter("text", "Skip Day", 0),
            new FIGMAParameter("x", (float)0.0f, 0),
            new FIGMAParameter("y", (float)0.0f, 0),
            new FIGMAParameter("action", new object[2] { action, null }, 1)
        );
        et = new FIGMAStructure.ElementType[2] { FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Action };
        figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[1] { 2 }, et));
        for (int i = 0; i < 5; i++)
        {
            menuelements[i + 1] = Instantiate(farray, fspawner.transform, false);
            rt = (RectTransform)menuelements[i + 1].GetComponent(typeof(RectTransform));
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
            menuelhelp -= Mathf.RoundToInt(rt.rect.height);
            figma = (FIGMAStructure)menuelements[i + 1].GetComponentInChildren(typeof(FIGMAStructure));
            objects = FIGMAStructure.MakeParams(os[i * 21], os[i * 21 + 1], os[i * 21 + 2], os[i * 21 + 3], os[i * 21 + 4],
                os[i * 21 + 5], os[i * 21 + 6], os[i * 21 + 7], os[i * 21 + 8], os[i * 21 + 9], os[i * 21 + 10],
                os[i * 21 + 11], os[i * 21 + 12], os[i * 21 + 13], os[i * 21 + 14], os[i * 21 + 15], os[i * 21 + 16],
                os[i * 21 + 17], os[i * 21 + 18], os[i * 21 + 19], os[i * 21 + 20]
            );
            et = new FIGMAStructure.ElementType[21];
            System.Array.Copy(ets.ToArray(), i * 21, et, 0, 21);
            figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, bytes, et));
        }
        rt = (RectTransform)fspawner.GetComponent(typeof(RectTransform));
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, m - menuelhelp);
    }

    public void MenuGrandSlam()
    {
        //if (eventRecords == null) return;
        MenuReset();
        menuelhelp -= 96;
        int m = menuelhelp;
        RectTransform rt;
        object[] objects;
        FIGMAStructure.ElementType[] et;
        FIGMAStructure figma;
        //System.Action<object> action;
        menuelements = new GameObject[eventRecords.Count];
        byte i = 0;
        foreach (var j in eventRecords)
        {
            menuelements[i] = Instantiate(farray, fspawner.transform, false);
            rt = (RectTransform)menuelements[i].GetComponent(typeof(RectTransform));
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, menuelhelp);
            menuelhelp -= Mathf.RoundToInt(rt.rect.height);
            figma = (FIGMAStructure)menuelements[i].GetComponentInChildren(typeof(FIGMAStructure));
            Sprite sprite = Modding.modding.GetLogo(j.team);
            if (sprite == null)
            {
                objects = FIGMAStructure.MakeParams(
                    new FIGMAParameter("text", j.title, 0),
                    new FIGMAParameter("text", "Title", 1),
                    //new FIGMAParameter("sprite", sprite, 2),
                    new FIGMAParameter("color", "#2A3C44", 3),
                    new FIGMAParameter("text", j.team, 4),
                    new FIGMAParameter("text", "Winner", 5)
                );
                et = new FIGMAStructure.ElementType[]
                {
                    FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle,
                    FIGMAStructure.ElementType.Background, FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle
                };
                figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 2, 3 }, et));
            }
            else
            {
                objects = FIGMAStructure.MakeParams(
                    new FIGMAParameter("text", j.title, 0),
                    new FIGMAParameter("text", "Title", 1),
                    new FIGMAParameter("sprite", sprite, 2),
                    new FIGMAParameter("color", "#2A3C44", 3),
                    new FIGMAParameter("text", j.team, 4),
                    new FIGMAParameter("text", "Winner", 5)
                );
                et = new FIGMAStructure.ElementType[]
                {
                    FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle, FIGMAStructure.ElementType.Flag,
                    FIGMAStructure.ElementType.Background, FIGMAStructure.ElementType.TextValue, FIGMAStructure.ElementType.Subtitle
                };
                figma.AddBar(FIGMAStructure.MakeFlagsA(FIGMAStructure.PredefinedType.Custom, objects, new byte[] { 3, 3 }, et));
            }
            ++i;
        }
        rt = (RectTransform)fspawner.GetComponent(typeof(RectTransform));
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, m - menuelhelp);
    }

    public void LeaveTheGame() { SaveGame(); Application.Quit(); }

    public string currentMenu { get; private set; }
    public GameObject currentMenuObject { get; private set; }
    private bool blockSwitch = false;

    [SerializeField] private RectTransform tsContent;
    [SerializeField] private GameObject topteamPrefab, topteamproPrefab;
    private GameObject[] topteams;

    [SerializeField] byte banCounter = 0, mapCounterTraining = 0;
    public byte MapCounterTraining() { return mapCounterTraining; }
    public void StartPick() 
    { 
        banCounter = 0;
        //setup match
        ResetKillFeed();
        MenuPickban();
    }
    public void ChooseMap(object o) { ChooseMap((int)o); }
    public void ChooseMapA(object o) { ChooseMap((int)o, false); }
    public void ChooseMap(int i)
    {
        if (match.pickbans.Contains((byte)i)) return;
        if (banCounter == 6) return;
        if (match.type == MatchType.BO1) { Ban(i); BanCPU(); }
        if (match.type == MatchType.BO3)
        {
            if (banCounter == 2 || banCounter == 3) { Pick(i); PickCPU(); return; }
            Ban(i); BanCPU();
        }
        if (match.type == MatchType.BO5)
        {
            if (banCounter == 0 || banCounter == 1) { Ban(i); BanCPU(); return; }
            Pick(i); PickCPU();
        }
        //MenuPickban();
    }
    public void ChooseMap(int i, bool set)
    {
        if (match.pickbans.Contains((byte)i)) return;
        if (banCounter == 6) return;
        if (match.type == MatchType.BO1) { Ban(i); if (set) BanCPU(); }
        if (match.type == MatchType.BO3)
        {
            if (banCounter == 2 || banCounter == 3) { Pick(i); if (set) PickCPU(); return; }
            Ban(i); if (set) BanCPU();
        }
        if (match.type == MatchType.BO5)
        {
            if (banCounter == 0 || banCounter == 1) { Ban(i); if (set) BanCPU(); return; }
            Pick(i); if (set) PickCPU();
        }
        //MenuPickban();
    }
    private void Pick(int i)
    {
        //if (match.pickbans.Contains((byte)i)) return;
        match.PickMap(i);
        //pbPicks[i].SetActive(true);
        banCounter++;
        if (banCounter == 6)
        {
            //SwitchMenu("match");
            for (byte j = 0; j < 7; j++)
            {
                byte b = 0;
                for (byte l = 0; l < 6; l++)
                {
                    if (match.pickbans[l] == j/*map*/) b = 1;
                }
                if (b == 0) { match.PickMap(j); break; }
            }
            popupQuest = match.SimulateMatch;
            blockSwitch = true;
            string[] table = new string[]
            {
                "Watch whole match or skip?",
                "Смотреть весь матч или пропустить?",
                "Ganzes Spiel ansehen oder überspringen?",
                "Assistir à partida inteira ou pular?",
                "Regarder le match en entier ou sauter?"
            };
            ShowPopUpAsk(table[(byte)TranslateObject.language], Random.Range(0, 100) < 37 ? true : false);
        }
        MenuPickban();
    }
    private void Ban(int i)
    {
        //if (match.pickbans.Contains((byte)i)) return;
        match.BanMap(i);
        //pbBans[i].SetActive(true);
        banCounter++;
        if (banCounter == 6) 
        { 
            //SwitchMenu("match"); 
            for (byte j = 0; j < 7; j++)
            {
                byte b = 0;
                for (byte l = 0; l < 6; l++)
                {
                    if (match.pickbans[l] == j/*map*/) b = 1;
                }
                if (b == 0) { match.PickMap(j); break; }
            }
            popupQuest = match.SimulateMatch;
            blockSwitch = true;
            string[] table = new string[]
            {
                "Watch whole match or skip?",
                "Смотреть весь матч или пропустить?",
                "Ganzes Spiel ansehen oder überspringen?",
                "Assistir à partida inteira ou pular?",
                "Regarder le match en entier ou sauter?"
            };
            ShowPopUpAsk(table[(byte)TranslateObject.language], Random.Range(0, 100) < 30 ? true : false);
        }
        MenuPickban();
    }
    private void PickCPU()
    {
        int h = 11;
        //List<byte> bubble = new List<byte>(new byte[] { 0, 1, 2, 3, 4, 5, 6 }); 0 - 6
        List<byte> bestAvailable = new List<byte>();
        for (byte l = 0; l < 7; l++)
        {
            if (myTeam.GetMap((Map)l) < h && !match.pickbans.Contains((byte)l))
                h = myTeam.GetMap((Map)l);
        }
        for (byte l = 0; l < 7; l++)
        {
            if (myTeam.GetMap((Map)l) == h && !match.pickbans.Contains((byte)l))
                bestAvailable.Add(l);
        }
        byte i = bestAvailable[Random.Range(0, bestAvailable.Count)];
        match.PickMap(i);
        //pbPicks[i].SetActive(true);
        banCounter++;
        if (banCounter == 6)
        {
            //SwitchMenu("match");
            for (byte j = 0; j < 7; j++)
            {
                byte b = 0;
                for (byte l = 0; l < 6; l++)
                {
                    if (match.pickbans[l] == j/*map*/) b = 1;
                }
                if (b == 0) { match.PickMap(j); break; }
            }
            popupQuest = match.SimulateMatch;
            blockSwitch = true;
            string[] table = new string[]
            {
                "Watch whole match or skip?",
                "Смотреть весь матч или пропустить?",
                "Ganzes Spiel ansehen oder überspringen?",
                "Assistir à partida inteira ou pular?",
                "Regarder le match en entier ou sauter?"
            };
            ShowPopUpAsk(table[(byte)TranslateObject.language], Random.Range(0, 100) < 30 ? true : false);
        }
        MenuPickban();
    }
    private void BanCPU()
    {
        int h = -1;
        //List<byte> bubble = new List<byte>(new byte[] { 0, 1, 2, 3, 4, 5, 6 }); 0 - 6
        List<byte> bestAvailable = new List<byte>();
        for (byte l = 0; l < 7; l++)
        {
            if (myTeam.GetMap((Map)l) > h && !match.pickbans.Contains((byte)l))
            h = myTeam.GetMap((Map)l);
        }
        for (byte l = 0; l < 7; l++)
        {
            if (myTeam.GetMap((Map)l) == h && !match.pickbans.Contains((byte)l))
                bestAvailable.Add(l);
        }
        byte i = bestAvailable[Random.Range(0, bestAvailable.Count)];
        match.BanMap(i);
        //pbBans[i].SetActive(true);
        banCounter++;
        if (banCounter == 6)
        {
            //SwitchMenu("match");
            for (byte j = 0; j < 7; j++)
            {
                byte b = 0;
                for (byte l = 0; l < 6; l++)
                {
                    if (match.pickbans[l] == j/*map*/) b = 1;
                }
                if (b == 0) { match.PickMap(j); break; }
            }
            popupQuest = match.SimulateMatch;
            blockSwitch = true;
            string[] table = new string[]
            {
                "Watch whole match or skip?",
                "Смотреть весь матч или пропустить?",
                "Ganzes Spiel ansehen oder überspringen?",
                "Assistir à partida inteira ou pular?",
                "Regarder le match en entier ou sauter?"
            };
            ShowPopUpAsk(table[(byte)TranslateObject.language], Random.Range(0, 100) < 30 ? true : false);
        }
        MenuPickban();
    }

    public void TransferLookForField(string text) { searchByTransfer = text; }

    // 0 1 2 3 4 5, 0 16 20 24 28 32, 0 55 60 65 70 75 80 85 90, 0 70 80 90 100 110 120 130 140
    [SerializeField]
    private byte transferRole = 0, transferAge = 0, transferLevel = 0, transferRating = 0, tPage = 0,
        groupPage = 0;
    bool recall = false;
    public string searchByTransfer = string.Empty;
    List<Player> _ps;
    [SerializeField] Sprite euSprite;
    public void TransferSettings(object o) { TransferSettings((int)o); }
    public void TransferSettings(int i)
    {
        switch (i)
        {
            case 0:
                if (transferRole == 0) transferRole = 5;
                else --transferRole;
                if (transferRole == 0) tsetting.role = roles[5].sprite;
                else tsetting.role = roles[transferRole - 1].sprite;
                break;
            case 1:
                if (transferRole == 5) transferRole = 0;
                else ++transferRole;
                if (transferRole == 0) tsetting.role = roles[5].sprite;
                else tsetting.role = roles[transferRole - 1].sprite;
                break;
            case 2:
                if (transferAge == 0) transferAge = 32;
                else 
                { 
                    transferAge = (byte)((transferAge - 12) * 0.25f); transferAge--;
                    transferAge = (byte)(transferAge == 0 ? 0 : (transferAge * 4 + 12));
                }
                if (transferAge == 0) 
                {
                    string[] table = new string[] { "ANY", "ЛЮБОЙ", "ALLE", "ALGUM", "TOUT" };
                    tsetting.age = table[(int)TranslateObject.language];
                }
                else if (transferAge == 16) tsetting.age = "19-";
                else if (transferAge == 32) tsetting.age = "32+";
                else tsetting.age = transferAge.ToString() + "-" + (transferAge + 3).ToString();
                break;
            case 3:
                if (transferAge == 32) transferAge = 0;
                else
                {
                    if (transferAge != 0)
                        transferAge = (byte)((transferAge - 12) * 0.25f); 
                    transferAge++;
                    transferAge = (byte)(transferAge == 0 ? 0 : (transferAge * 4 + 12));
                }
                if (transferAge == 0)
                {
                    string[] table = new string[] { "ANY", "ЛЮБОЙ", "ALLE", "ALGUM", "TOUT" };
                    tsetting.age = table[(int)TranslateObject.language];
                }
                else if (transferAge == 16) tsetting.age = "19-";
                else if (transferAge == 32) tsetting.age = "32+";
                else tsetting.age = transferAge.ToString() + "-" + (transferAge + 3).ToString();
                break;
            case 4:
                if (transferLevel == 0) transferLevel = 90;
                else
                {
                    transferLevel = (byte)(((transferLevel - 50) * 0.2f) + 0.5f); transferLevel--;
                    transferLevel = (byte)(transferLevel == 0 ? 0 : (transferLevel * 5 + 50));
                }
                if (transferLevel == 0)
                {
                    string[] table = new string[] { "ANY", "ЛЮБОЙ", "ALLE", "ALGUM", "TOUT" };
                    tsetting.level = table[(int)TranslateObject.language];
                }
                else if (transferLevel == 55) tsetting.level = "59-";
                else if (transferLevel == 90) tsetting.level = "90+";
                else tsetting.level = transferLevel.ToString() + "-" + (transferLevel + 4).ToString();
                break;
            case 5:
                if (transferLevel == 90) transferLevel = 0;
                else
                {
                    if (transferLevel != 0)
                        transferLevel = (byte)(((transferLevel - 50) * 0.2f) + 0.5f); 
                    transferLevel++;
                    transferLevel = (byte)(transferLevel == 0 ? 0 : (transferLevel * 5 + 50));
                }
                if (transferLevel == 0)
                {
                    string[] table = new string[] { "ANY", "ЛЮБОЙ", "ALLE", "ALGUM", "TOUT" };
                    tsetting.level = table[(int)TranslateObject.language];
                }
                else if (transferLevel == 55) tsetting.level = "59-";
                else if (transferLevel == 90) tsetting.level = "90+";
                else tsetting.level = transferLevel.ToString() + "-" + (transferLevel + 4).ToString();
                break;
            case 6: //0 70 80 90 100 110 120 130 140
                if (transferRating == 0) transferRating = (byte)(nations.Length);
                else --transferRating;
                if (transferRating == 0)
                {
                    tsetting.flag = euSprite; break;
                }
                else tsetting.flag = nations[transferRating - 1].sprite;
                break;
            case 7:
                if (transferRating == nations.Length) transferRating = 0;
                else ++transferRating;
                if (transferRating == 0)
                {
                    tsetting.flag = euSprite; break;
                }
                else tsetting.flag = nations[transferRating - 1].sprite;
                break;
            case 8:
                List<Player> ps = definedPlayers.players.OrderByDescending(x => x.RecalculateValue()).ToList();
                List<Player> sortedPlayers = new List<Player>();
                for (short j = 0; j < ps.Count; j++)
                {
                    byte req = 0;
                    if ((byte)ps[j].role == transferRole - 1 || transferRole == 0) req++;

                    if ((ps[j].age >= transferAge && ps[j].age <= transferAge + 3) || transferAge == 0) req++;
                    else if (transferAge == 32 && ps[j].age >= 32) req++;
                    else if (transferAge == 16 && ps[j].age <= 19) req++;
                    
                    if ((ps[j].strength >= transferLevel && ps[j].strength <= transferLevel + 4) || 
                        transferLevel == 0) req++;
                    else if (transferLevel == 90 && ps[j].strength >= 90) req++;
                    else if (transferLevel == 55 && ps[j].strength <= 59) req++;

                    if (transferRating == 0) req++;
                    else if (nations[transferRating - 1].nation == ps[j].nationality) req++;

                    string find = searchByTransfer.ToLower();
                    find.Trim(new char[] { '/', '\\', '.', '<', ',', '>', '?', '!', '@', '#', '$', '%', ':', ';' });
                    string pf = ps[j].nickname.ToLower();
                    if (find.Length > pf.Length) find = find.Substring(0, pf.Length);
                    else pf = pf.Substring(0, find.Length);

                    if ((req == 4 && find.Length < 1) || (find.Length > 0 && find != string.Empty && find == pf)) sortedPlayers.Add(ps[j]);
                }
                if (!recall) tPage = 0;
                recall = false;
                byte sh = 6;
                //if (Mathf.CeilToInt(sortedPlayers.Count + 6 - ((tPage + 1) * 6)) <= 6) 
                //    sh = (byte)(sortedPlayers.Count + 6 - ((tPage + 1) * 6));
                /*for (byte j = 0; j < 6; j++) transfBars[j].SetActive(false);
                for (byte j = 0; j < sh; j++)
                {
                    if (j + tPage * 6 == sortedPlayers.Count - 1 && sortedPlayers[j + tPage * 6].GetValue() < 0)
                        break;
                    transfRatings[j].text = sortedPlayers[j + tPage * 6].GetRating().ToString("0.00");
                    transfLevels[j].text = sortedPlayers[j + tPage * 6].strength.ToString();
                    if (sortedPlayers[j + tPage * 6].GetValue() > 999999) transfValues[j].text = 
                        string.Format("{0:## ### ###}", sortedPlayers[j + tPage * 6].GetValue()) + " $";
                    else transfValues[j].text =
                        string.Format("{0:### ###}", sortedPlayers[j + tPage * 6].GetValue()) + " $";
                    if (sortedPlayers[j + tPage * 6].GetValue() == 0) transfValues[j].text = "0 $";
                    transfNicknames[j].text = sortedPlayers[j + tPage * 6].nickname;
                    transfBars[j].SetActive(true);
                    if ((int)sortedPlayers[j + tPage * 6].role == -1) 
                    { transfRoles[j].sprite = roles[5].sprite; continue; }
                    for (byte l = 0; l < 5; l++) 
                        if (sortedPlayers[j + tPage * 6].role == roles[l].role) 
                            transfRoles[j].sprite = roles[l].sprite;
                }*/
                _ps = sortedPlayers;
                MenuTransferList(null);
                break;
            case 9:
                if (tPage != 0) tPage--;
                recall = true;
                TransferSettings(8);
                break;
            case 10:
                //if (transfBars[5].activeInHierarchy) tPage++;
                recall = true;
                TransferSettings(8);
                break;
            case 11:
                if (mapCounterTraining != 0) mapCounterTraining--;
                break;
            case 12:
                if (mapCounterTraining != 6) mapCounterTraining++;
                break;
        }
        if (i < 8) MenuTransfer();
    }
    public void TeamOffer(object o)
    {
        toomu = (int)o;
        popupQuest = TeamOffer;
        string[] table = new string[]
        { "Are you sure?", "Вы уверенны?", "Bist du sicher?", "Tem certeza?", "Etes-vous sure?" };
        ShowPopUpAsk(table[(byte)TranslateObject.language], false);
    }
    int toomu;
    private byte TeamOffer(bool b)
    {
        if (!b) { return 202; }
        int i = toomu;
        myTeam = definedTeams.GetTeamPlacement(i);
        saveState.myteam = myTeam.teamName;
        string[] table = new string[]
        {
            "Management warmly welcomes you and waits for new trophies",
            "Руководство тепло приветствует Вас и ждет новых трофеев",
            "Das Management heißt Sie herzlich willkommen und wartet auf neue Trophäen",
            "A gerência lhe dá as boas-vindas calorosamente e espera por novos troféus",
            "L'administration vous acceuille chaleuresement et attend de nouveaux trophées"
        };
        ShowPopUp(table[(byte)TranslateObject.language]);
        MenuTeam();
        FIGMAMenu.menu.ShowMenu();
        //if (day == 1) StartCoroutine(Tutor());
        return 0;
    }
    [SerializeField] private GameObject[] disableWhenProChoosing;
    [SerializeField] private GameObject enableWhenProChoosing;
    public void TeamOfferPro(int i)
    {
        myTeam = definedTeams.GetTeamPlacement(i);
        saveState.myteam = myTeam.teamName;
        for (byte j = 0; j < 4; j++) disableWhenProChoosing[j].SetActive(true);
        enableWhenProChoosing.SetActive(false);
        string[] table = new string[]
        {
            "Management warmly welcomes you and waits for new trophies",
            "Руководство тепло приветствует Вас и ждет новых трофеев",
            "Das Management heißt Sie herzlich willkommen und wartet auf neue Trophäen",
            "A gerência lhe dá as boas-vindas calorosamente e espera por novos troféus",
            "L'administration vous acceuille chaleuresement et attend de nouveaux trophées"
        };
        ShowPopUp(table[(byte)TranslateObject.language]);
        MenuTeam();
        //if (day == 1) StartCoroutine(Tutor());
    }

    public void KillFeed(string player1, Weapon weapon, string player2)
    {
        if (player1 == null) player1 = string.Empty;
        if (player2 == null) player2 = string.Empty;
        for (byte i = 0; i < kills.Length; i++)
        {
            if (!kills[i].activeInHierarchy)
            {
                byte l;
                for (l = 0; l < 5; l++)
                {
                    if (match.team1.players[l].nickname == player2)
                    { players[l].color = new Color(0.5f, 0.5f, 0.5f, 1.0f); break; }
                    if (match.team2.players[l].nickname == player2)
                    { players[l + 5].color = new Color(0.5f, 0.5f, 0.5f, 1.0f); l += 5; break; }
                }
                player1s[i].text = player1;
                if (player1.Length > 6) player1s[i].fontSize = 32 - ((player1.Length - 6) * 2);
                else player1s[i].fontSize = 32;
                for (byte j = 0; j < weapons.Length; j++)
                {
                    if (weapon == weapons[j].weapon)
                    {
                        if (weapon == Weapon.DefaultPistol)
                        {
                            if ((l < 5 && match.sider == 1) || (l > 4 && match.sider == 0))
                            { sprites[i].sprite = weapons[2].sprite; break; }
                            else { sprites[i].sprite = weapons[3].sprite; break; }
                        }
                        else sprites[i].sprite = weapons[j].sprite; break;
                    }
                }
                player2s[i].text = player2;
                if (player2.Length > 6) player2s[i].fontSize = 32 - ((player2.Length - 6) * 2);
                else player2s[i].fontSize = 32;
                kills[i].SetActive(true);
                RefreshInterface();
                break;
            }
        }
    }
    //t = 0, ct = 1; mid = 254, a = 1, b = 2; entry = 1, default = 0;
    public void AppearFrag(byte currentMap, byte deadSide, byte site, byte entring = 0)
    {
        byte i = 0;
        for (; i < 9; i++) if (killsRadar[i] == null) break;
        RectTransform[] rectTransforms = new RectTransform[1];
        if (deadSide == 0)
        {
            if (site == 1)
            {
                if (Random.Range(0, 100) < 4)
                {
                    rectTransforms = (entring == 0 ? positionsANY[currentMap].bsite : 
                        positionsTT[currentMap].bsite);
                }
                else if (Random.Range(0, 100) < 29)
                {
                    rectTransforms = (entring == 0 ? positionsANY[currentMap].mid :
                            positionsTT[currentMap].mid);
                }
                else
                {
                    rectTransforms = (entring == 0 ? positionsANY[currentMap].asite :
                        positionsTT[currentMap].asite);
                }
            }
            else if (site == 2)
            {
                if (Random.Range(0, 100) < 4)
                {
                    rectTransforms = (entring == 0 ? positionsANY[currentMap].asite :
                        positionsTT[currentMap].asite);
                }
                else if (Random.Range(0, 100) < 29)
                {
                    rectTransforms = (entring == 0 ? positionsANY[currentMap].mid :
                            positionsTT[currentMap].mid);
                }
                else
                {
                    rectTransforms = (entring == 0 ? positionsANY[currentMap].bsite :
                        positionsTT[currentMap].bsite);
                }
            }
        }
        if (deadSide == 1)
        {
            if (site == 1)
            {
                if (Random.Range(0, 100) < 4)
                {
                    rectTransforms = (entring == 0 ? positionsANY[currentMap].bsite :
                        positionsCT[currentMap].bsite);
                }
                else if (Random.Range(0, 100) < 29)
                {
                    rectTransforms = (entring == 0 ? positionsANY[currentMap].mid :
                            positionsCT[currentMap].mid);
                }
                else
                {
                    rectTransforms = (entring == 0 ? positionsANY[currentMap].asite :
                        positionsCT[currentMap].asite);
                }
            }
            else if (site == 2)
            {
                if (Random.Range(0, 100) < 4)
                {
                    rectTransforms = (entring == 0 ? positionsANY[currentMap].asite :
                        positionsCT[currentMap].asite);
                }
                else if (Random.Range(0, 100) < 29)
                {
                    rectTransforms = (entring == 0 ? positionsANY[currentMap].mid :
                            positionsCT[currentMap].mid);
                }
                else
                {
                    rectTransforms = (entring == 0 ? positionsANY[currentMap].bsite :
                        positionsCT[currentMap].bsite);
                }
            }
        }
        int seed;
        do
        {
            seed = Random.Range(0, rectTransforms.Length);
        } while (rectTransforms[seed].childCount == 1);
        killsRadar[i] = Instantiate(deadSide == 0 ? ttDown : ctDown,
            rectTransforms[seed], false);
        killsRadar[i].GetComponent<RectTransform>().anchoredPosition =
            new Vector2(Random.Range(-2.5f, 2.5f), Random.Range(-2.5f, 2.5f));
    }
    //player
    //0 money, 1 kills, 2 deaths, 3 assists, 4 entries, 5 x1, 6 x2, 7 x3, 8 x4, 9 x5, 10 1v1, 11 1v2
    //12 1v3, 13 1v4, 14 1v5, 15 kast, 16 weapon, 
    //roundStat: 17 kills, 18 deaths, 19 probable clutch(values 1-5),
    //20 henade, 21 smoke, 22 flash, 23 molotov, 24 kevlar, 25 helmet
    //team
    //0 lossbonus, 1 rounds-won, 2 side
    [SerializeField] private Text followersRI;
    public void RefreshInterface()
    {
        for (byte i = 0; i < 10; i++)
        {
            if (match.vest[i] == 1) mtKevlar[i].SetActive(true);
            else mtKevlar[i].SetActive(false);
            if (match.helmet[i] == 1) mtHelmet[i].SetActive(true);
            else mtHelmet[i].SetActive(false);
        }
        for (byte i = 0; i < players.Length; i++)
        {
            nicknames[i].text = i < 5 ? match.team1.players[i].nickname : match.team2.players[i % 5].nickname;
            kd[i].text = match.kills[i].ToString() + "/" + match.deaths[i].ToString();
            balance[i].text = match.money[i].ToString() + " $";
            for (byte j = 0; j < weapons.Length; j++)
            {
                if (match.weapons[i] == (short)weapons[j].weapon)
                { 
                    if (weapons[j].weapon == Weapon.DefaultPistol)
                    {
                        if (i < 5) boughtDevice[i].sprite = weapons[j + match.sider].sprite;
                        else boughtDevice[i].sprite = weapons[j + (match.sider ^ 1)].sprite;
                        break;
                    }
                    boughtDevice[i].sprite = weapons[j].sprite; break; 
                }
            }

        }
        followersRI.text = ((int)
            ((match.team1.followers + match.team2.followers + match.followerBoost) * matchImportance)).ToString();
        if (match.round1 < 10) rounds1.text = "0" + match.round1.ToString();
        else rounds1.text = match.round1.ToString();
        if (match.round2 < 10) rounds2.text = "0" + match.round2.ToString();
        else rounds2.text = match.round2.ToString();
        teamname1.text = match.team1.teamName;
        teamname2.text = match.team2.teamName;
        if (match.round1 + match.round2 < 30) roundNumber.text =
                "Round " + (match.round1 + match.round2 + 1).ToString() + "/30";
        else roundNumber.text = "Overtimes";
        byte a1 = 0, a2 = 0;
        for (byte j = 0; j < 5; j++)
        {
            if (match.alive[j] == 1)
            {
                a1++; players[j].color = new Color(1f, 1f, 1f, 1.0f);
            }
            else players[j].color = new Color(0.5f, 0.5f, 0.5f, 1.0f);
            if (match.alive[j + 5] == 1)
            {
                a2++; players[j + 5].color = new Color(1f, 1f, 1f, 1.0f);
            }
            else players[j + 5].color = new Color(0.5f, 0.5f, 0.5f, 1.0f);
        }
        alive1.text = a1.ToString();
        alive2.text = a2.ToString();
    }

    public Color RatingColor(float f)
    {
        int r = Mathf.RoundToInt(f * 100.0f);
        Color color;
        if (r > 124)
            color = new Color(0.0f, 1.0f, 1.0f);
        else if (r > 108)
            color = new Color(0.0f, 1.0f, (r - 108) * 0.0625f);
        else if (r > 92)
            color = new Color(1.0f - (r - 92) * 0.0625f, 1.0f, 0.0f);
        else if (r > 76)
            color = new Color(1.0f, (r - 76) * 0.0625f, 0.0f);
        else color = new Color(1.0f, 0.0f, 0.0f);
        return color;
    }

    public Color RatingColor(float f, float[] comps, float mp = 0.0625f)
    {
        int r = Mathf.RoundToInt(f * 100.0f);
        //if (comps.Length != 4) return; performance lose
        Color color;
        if (r > comps[0])
            color = new Color(0.0f, 1.0f, 1.0f);
        else if (r > comps[1])
            color = new Color(0.0f, 1.0f, (r - comps[1]) * mp);
        else if (r > comps[2])
            color = new Color(1.0f - (r - comps[2]) * mp, 1.0f, 0.0f);
        else if (r > comps[3])
            color = new Color(1.0f, (r - comps[3]) * mp, 0.0f);
        else color = new Color(1.0f, 0.0f, 0.0f);
        return color;
    }

    public void ResetKillFeed()
    {
        for (byte i = 0; i < 9; i++)
        { 
            kills[i].SetActive(false);
            if (killsRadar[i] != null) Destroy(killsRadar[i]);
        }
    }
    [SerializeField] RectTransform betpop;
    [SerializeField] GameObject betGrid;
    [SerializeField] Text[] betTeam;
    GameObject[] betgrido;
    /*
    public void BetMenu()
    {
        if (betgrido != null)
        {
            for (int i = 0; i < betgrido.Length; i++)
                Destroy(betgrido[i]);
        }
        List<MatchSetting> ms = null;
        List<Tournament> ts = Events.events.GetTournaments();
        for (int i = 0; i < ts.Count; i++)
        {
            if (ts[i].day <= day)
            {
                if (ts[i].AreThereAnyMatches(day + 1))
                {
                    ms = ts[i].GetMatchesIfThereAreAny(day + 1);
                    break;
                }
            }
        }
        if (ms == null) return;
        betgrido = new GameObject[ms.Count];
        byte t = (byte)(betgrido.Length > 16 ? 16 : betgrido.Length);
        for (byte i = 0; i < t; i++)
        {
            var betg = GetBets(ms[i]);
            betgrido[i] = Instantiate(betGrid, betsUI.transform, false);
            betgrido[i].GetComponent<RectTransform>().anchoredPosition = new Vector2(-660 + (i & 3) * 440, 675 - (int)(i * 0.25) * 160);
            betgrido[i].GetComponent<MatchGrid>().SetValues(ms[i].team1.teamName, ms[i].team2.teamName,
                betg[0].ToString("0.00"), betg[1].ToString("0.00"));
        }
    }*/
    
    public void EventRegistration(object o) { EventRegistration((int)o); }
    public void EventRegistration(int i)
    {
        short s = 0;
        for (; s < Events.events.GetTournaments().Count; s++)
            if (Events.events.GetTournaments()[s].day > day - 10 - eventPast) break;
        if (myTeam.currentTournament != -1)
        {
            if (Events.events.GetTournaments()[myTeam.currentTournament].EventState() != 2)
            {
                if (Events.events.GetTournaments()[s + i].EventState() == 2)
                {
                    SetEventViewer(s + i);
                    MenuPreviewEvent(s + i);
                    return;
                }
                else
                {
                    string[] table = new string[]
                    {
                        "You are already registered for a tournament",
                        "Вы уже зарегистрированы на турнир",
                        "Sie sind bereits für Turnier angemeldet",
                        "Você já está registrado para um torneio",
                        "Vous etes déjà enregisté pour un tournoi"
                    };
                    ShowPopUp(table[(byte)TranslateObject.language]); return;
                }
            }
        }
        if (Events.events.GetTournaments()[s + i].EventState() == 1)
        {
            string[] table = new string[]
            {
                "This tournament has already started",
                "Этот турнир уже стартовал",
                "Dieses Turnier hat bereits begonnen",
                "Este torneio já começou",
                "Ce tournoi a déjà commencé"
            };
            ShowPopUp(table[(byte)TranslateObject.language]); return;
        }
        else if (Events.events.GetTournaments()[s + i].EventState() == 2)
        {
            MenuPreviewEvent(s + i);
            return;
        }
        else if (myTeam.players.Count < 5)
        {
            string[] table = new string[]
            {
                "Not enough players",
                "Недостаточно игроков",
                "Nicht genug Spieler",
                "Jogadores insuficientes",
                "Pas assez de joueurs"
            };
            ShowPopUp(table[(byte)TranslateObject.language]); return;
        }
        if (Events.events.GetTournaments()[s + i].open == 0)
        {
            string[] table = new string[]
            { 
                "You can only get invited on this tournament",
                "Вы можете попасть на этот турнир только получив приглашение",
                "Sie können nur zu diesem Turnier eingeladen werden",
                "Você só pode ser convidado para este torneio",
                "Vous devez etre invité pour participer a ce tournoi"
            };
            ShowPopUp(table[(byte)TranslateObject.language]); return;
        }
        if (myTeam.currentTournament == s + i)
        {
            string[] table = new string[]
            {
                "You are already registered for this tournament",
                "Вы уже зарегистрированы на этот турнир",
                "Sie sind bereits für dieses Turnier angemeldet",
                "Você já está registrado para este torneio",
                "Vous etes déjà enregisté pour ce tournoi"
            };
            ShowPopUp(table[(byte)TranslateObject.language]); return;
        }
        if (Events.events.GetTournaments()[s + i].day - 3 > day)
        {
            string[] table = new string[]
            {
                "Registration will open on ",
                "Регистрация откроется ",
                "Die Registrierung öffnet am ",
                "O registro abrirá em ",
                "Les inscriptions seront ouvertes le "
            };
            ShowPopUp(table[(byte)TranslateObject.language] + 
                GetDateText((Events.events.GetTournaments()[s + i].day - 3) - day)); return;
        }
        if (Events.events.GetTournaments()[s + i].day >= day)
        {
            myTeam.currentTournament = s + i;
            Tournament _t = Events.events.GetTournaments()[s + i];
            string[] table = new string[]
            {
                "Successfully registered for ",
                "Вы успешно зарегистрировались на ",
                "Erfolgreich registriert für ",
                "Registrado com sucesso para ",
                "Enregisté pour"
            };
            ShowPopUp(table[(byte)TranslateObject.language] + _t.title);
            _t.GenerateTournament(true, myTeam, _t.btype, _t.tier);
        }
        else 
        {
            string[] table = new string[]
            {
                "This tournament is already finished",
                "Этот турнир уже завершен",
                "Dieses Turnier ist bereits beendet",
                "Este torneio já terminou",
                "Ce tournoi est déjà terminé"
            };
            ShowPopUp(table[(byte)TranslateObject.language]);
        }
    }

    private byte f1 = 5;

    Team tSel;
    string nickSel;
    int cptbi;
    public void ChoosePlayerToBuy(int i)
    {
        cptbi = i;
        tSel = definedTeams.GetTeam(_ps[i + tPage * 6].teamName);
        nickSel = _ps[i + tPage * 6].nickname;
        describePlayer = _ps[i + tPage * 6];
        //DescribePlayerUpdate();
    }

    public static float buyPlayerMP = 1.0f;
    public void BuyPlayer(object o) { BuyPlayer(); }
    public void BuyPlayer()
    {
        if (describePlayer.teamName == "Free") 
        {
            if (myTeam.players.Count == 11)
            {
                string[] table = new string[]
                {
                    "Your squad is full",
                    "Ваш состав полон",
                    "Ihr Team ist voll",
                    "Seu time está cheio",
                    "Votre équipe est complète"
                };
                ShowPopUp(table[(byte)TranslateObject.language]); return;
            }
            myTeam.players.Add(describePlayer);
            myTeam.players[myTeam.players.Count - 1].daysInTeam = 0;
            myTeam.players[myTeam.players.Count - 1].teamName = myTeam.teamName;
            myTeam.players[myTeam.players.Count - 1].RecalculateValue();
            MenuContract(".");
            //PopDownDesc();
            return;
        }
        if (describePlayer.GetValue() * buyPlayerMP > describedPlayerPrice)
        {
            string[] table = new string[]
            {
                "Management of that team doesn't agree with that price",
                "Менеджмент этой команды не согласен с такой ценой",
                "Das Management dieses Teams ist mit diesem Preis nicht einverstanden",
                "A gestão dessa equipe não concorda com esse preço",
                "Le Manager de cette équipe n'est pas d'accord avec ce prix"
            };
            ShowPopUp(table[(byte)TranslateObject.language]); return;
        }
        tSel.SellPlayerCPU(myTeam, nickSel);
        return;
    }
    byte sellP = 0;

    public void KickPlayer(object o) { KickPlayer((int)o); }
    public void KickPlayer(int i)
    {
        if (sellP == 0)
        {
            ++sellP;
            string[] table = new string[]
            {
                "If you really want to kick the player, click kick again",
                "Если вы действительно хотите выгнать игрока, нажмите \"Кикнуть\" еще раз",
                "Wenn du den Spieler wirklich kicken willst, klicke noch einmal kicken",
                "Se você realmente quer chutar o jogador, clique em chutar novamente",
                "Si vous voulez vraiment botter le joueur, cliquez à nouveau sur botter"
            };
            ShowPopUp(table[(byte)TranslateObject.language]); return;
        }
        if (myTeam.currentTournament != -1)
        {
            if (!(Events.events.GetTournaments()[myTeam.currentTournament].EventState() == 2 || myTeam.players.Count > 5))
            {
                string[] table = new string[]
                {
                    "You can't kick players during tournament",
                    "Вы не можете кикать игроков во время турнира",
                    "Sie können während des Turniers keine Spieler verkaufen",
                    "Você não pode chutar jogadores durante o torneio",
                    "Vous ne pouvez pas virer les joueurs pendant le tournoi"
                };
                ShowPopUp(table[(byte)TranslateObject.language]);
                sellP = 0; return;
            }
        }
        sellP = 0;
        myTeam.players[i].teamName = "Free";
        myTeam.players.RemoveAt(i);
    }

    private void SellPlayer(object o) { SellPlayer((int)o); }
    public void SellPlayer(int i) 
    {
        if (sellP == 0)
        { 
            ++sellP;
            string[] table = new string[]
            {
                "If you really want to sell the player, click sell again",
                "Если вы действительно хотите продать игрока, нажмите продать еще раз",
                "Wenn Sie den Player wirklich verkaufen möchten, klicken Sie erneut auf Verkaufen",
                "Se você realmente deseja vender o jogador, clique em vender novamente",
                "Si vous voulez vraiment vendre ce joueur, appuyé une deuxieme fois sur vendre"
            };
            ShowPopUp(table[(byte)TranslateObject.language]); return;
        }
        if (myTeam.currentTournament != -1)
        {
            if (!(Events.events.GetTournaments()[myTeam.currentTournament].EventState() == 2 || myTeam.players.Count > 5))
            {
                string[] table = new string[]
                {
                    "You can't sell players during tournament",
                    "Вы не можете продавать игроков во время турнира",
                    "Sie können während des Turniers keine Spieler verkaufen",
                    "Você não pode vender jogadores durante o torneio",
                    "Vous ne pouvez pas vendre de joueurs pendant un tournoi"
                };
                ShowPopUp(table[(byte)TranslateObject.language]);
                sellP = 0; return;
            }
        }
        sellP = 0; myTeam.Sell(i);
    }

    byte lSel = 10;
    public void LeagueContract(object o)
    {
        int i = (int)o;
        lSel = (byte)i;
        popupQuest = LeagueGetPlayer;
        string[] table = new string[]
        { "Your roster is full", "Ваш состав уже полон", "Ihr Team ist voll", "Seu time está cheio", "Votre équipe est complète" };
        if (myTeam.players.Count == 11) { ShowPopUp(table[(byte)TranslateObject.language]); return; }
        table = new string[]
        {
            "You have reached the limit of league players for this month",
            "Вы достигли лимита игроков лиги на этот месяц",
            "Du hast das Limit der Ligaspieler für diesen Monat erreicht",
            "Você atingiu o limite de jogadores da liga neste mês",
            "Vous avez atteint la limite de joueurs libres ce mois-ci"
        };
        if (myTeam.leaguePlayers > 5) { ShowPopUp(table[(byte)TranslateObject.language]); return; }
        table = new string[]
        { "Are you sure?", "Вы уверенны?", "Bist du sicher?", "Tem certeza?", "Etes-vous sure?" };
        ShowPopUpAsk(table[(byte)TranslateObject.language], false);
    }

    private byte LeagueGetPlayer(bool b)
    {
        if (!b) return 202;
        string[] table = new string[]
        { "Your roster is full", "Ваш состав уже полон", "Ihr Team ist voll", "Seu time está cheio", "Votre équipe est complète" };
        if (myTeam.players.Count == 11) { ShowPopUp(table[(byte)TranslateObject.language]); return 11; }
        League.players[lSel].daysInTeam = 0;
        myTeam.players.Add(League.players[lSel]);
        AddPlayerToList(League.players[lSel]);
        League.players[lSel].teamName = myTeam.teamName;
        League.players[lSel].RecalculateValue();
        League.players.Remove(League.players[lSel]);
        League.players.Add(PlayerNicknames.GeneratePlayerLeague());
        myTeam.leaguePlayers++;
        playGames.UnlockAchievement(6);
        return 1;
    }

    byte uAcad = 0;
    public void UpgradeAcademyInspector() { UpgradeAcademy(true); }
    public void UpgradeAcademy(object o) { UpgradeAcademy(true); }
    private byte UpgradeAcademy(bool b)
    {
        if (!b) { uAcad = 0; return 202; }
        if (uAcad == 0)
        {
            popupQuest = UpgradeAcademy;
            string[] table = new string[] 
            {
                "Upgrade academy for: ",
                "Улучшить академию за: ",
                "Verbessern Sie die Akademie für: ",
                "Atualizar academia para: ",
                "Améliorer l'académie pour"
            };
            int p = 1024 * myTeam.academy.GetAcademyLevel() * myTeam.academy.GetAcademyLevel();
            string str;
            if (p > 999999) str = string.Format("{0:## ### ###}", p);
            else str = string.Format("{0:### ###}", p);
            ShowPopUpAsk(table[(byte)TranslateObject.language] + str + " $", false);
            uAcad = 1;
            return 101;
        }
        byte t = myTeam.academy.UpgradeAcademy(this);
        uAcad = 0;
        if (t == 0) 
        {
            string[] table = new string[]
            {
                "Not enough money, needed: ",
                "Слишком мало денег, нужно: ",
                "Nicht genug Geld, benötigt: ",
                "Dinheiro insuficiente, necessário: ",
                "Pas assez d'argent, vous avez besoin: "
            };
            int p = 1024 * myTeam.academy.GetAcademyLevel() * myTeam.academy.GetAcademyLevel();
            string str;
            if (p > 999999) str = string.Format("{0:## ### ###}", p);
            else str = string.Format("{0:### ###}", p);
            ShowPopUp(table[(byte)TranslateObject.language] + str + " $");
            return 0;
        }
        if (t == 25)
        {
            string[] table = new string[]
            {
                "Your academy is already at max level",
                "Ваша академия уже на максимальном уровне",
                "Ihre Akademie ist bereits auf maximalem Niveau",
                "Sua academia já está no nível máximo",
                "Votre académie est déjà amélioré au maximum"
            };
            ShowPopUp(table[(byte)TranslateObject.language]);
            return 25;
        }
        MenuAcademy();
        return 1;
    }

    private int sj = 0, sp = 0, ss = 0;
    public void SetJerseys(string text)
    {
        string[] table = new string[]
        {
            "Only digits are allowed",
            "Разрешены только цифры",
            "Nur Zahlen sind erlaubt",
            "Somente dígitos são permitidos",
            "Seuls les chiffres sont autorisés"
        };
        if (!System.Int32.TryParse(text, out sj)) { ShowPopUp(table[(byte)TranslateObject.language]); return; }
    }

    public void BuyJerseys(object o) { BuyJerseys(); }
    public void BuyJerseys()
    {
        byte i = myTeam.BuyJersey(sj);
        if (i == 0)
        {
            string[] table = new string[]
            {
                "Not enough money",
                "Не хватает денег",
                "Nicht genug Geld",
                "Dinheiro insuficiente",
                "Pas assez d'argent"
            };
            ShowPopUp(table[(byte)TranslateObject.language]); return;
        }
        if (i == 222)
        {
            string[] table = new string[]
            {
                "Not enough space",
                "Не хватает места",
                "Nicht genügend Platz",
                "Espaço insuficiente",
                "Pas assez d'espace"
            };
            ShowPopUp(table[(byte)TranslateObject.language]); return;
        }
        string[] _table = new string[] 
        {
            "Purchase has been completed successfully",
            "Покупка была завершена успешно",
            "Kauf wurde erfolgreich abgeschlossen",
            "A compra foi concluída com sucesso",
            "L'achat a été validé"
        };
        ShowPopUp(_table[(byte)TranslateObject.language]);
        MenuMerch();
    }

    public void SetPosters(string text)
    {
        string[] table = new string[]
        {
            "Only digits are allowed",
            "Разрешены только цифры",
            "Nur Zahlen sind erlaubt",
            "Somente dígitos são permitidos",
            "Seuls les chiffres sont autorisés"
        };
        if (!System.Int32.TryParse(text, out sp)) { ShowPopUp(table[(byte)TranslateObject.language]); return; }
    }

    public void BuyPosters(object o) { BuyPosters(); }
    public void BuyPosters()
    {
        byte i = myTeam.BuyPoster(sp);
        if (i == 0)
        {
            string[] table = new string[]
            {
                "Not enough money",
                "Не хватает денег",
                "Nicht genug Geld",
                "Dinheiro insuficiente",
                "Pas assez d'argent"
            };
            ShowPopUp(table[(byte)TranslateObject.language]); return;
        }
        if (i == 222)
        {
            string[] table = new string[]
            {
                "Not enough space",
                "Не хватает места",
                "Nicht genügend Platz",
                "Espaço insuficiente",
                "Pas assez d'espace"
            };
            ShowPopUp(table[(byte)TranslateObject.language]); return;
        }
        string[] _table = new string[]
        {
            "Purchase has been completed successfully",
            "Покупка была завершена успешно",
            "Kauf wurde erfolgreich abgeschlossen",
            "A compra foi concluída com sucesso",
            "L'achat a été validé"
        };
        ShowPopUp(_table[(byte)TranslateObject.language]);
        MenuMerch();
    }

    public void SetStickers(string text)
    {
        string[] table = new string[]
        {
            "Only digits are allowed",
            "Разрешены только цифры",
            "Nur Zahlen sind erlaubt",
            "Somente dígitos são permitidos",
            "Seuls les chiffres sont autorisés"
        };
        if (!System.Int32.TryParse(text, out ss)) { ShowPopUp(table[(byte)TranslateObject.language]); return; }
    }
    public void BuyStickers(object o) { BuyStickers(); }
    public void BuyStickers()
    {
        byte i = myTeam.BuySticker(ss);
        if (i == 0)
        {
            string[] table = new string[]
            {
                "Not enough money",
                "Не хватает денег",
                "Nicht genug Geld",
                "Dinheiro insuficiente",
                "Pas assez d'argent"
            };
            ShowPopUp(table[(byte)TranslateObject.language]); return;
        }
        if (i == 222)
        {
            string[] table = new string[]
            {
                "Not enough space",
                "Не хватает места",
                "Nicht genügend Platz",
                "Espaço insuficiente",
                "Pas assez d'espace"
            };
            ShowPopUp(table[(byte)TranslateObject.language]); return;
        }
        string[] _table = new string[]
        {
            "Purchase has been completed successfully",
            "Покупка была завершена успешно",
            "Kauf wurde erfolgreich abgeschlossen",
            "A compra foi concluída com sucesso",
            "L'achat a été validé"
        };
        ShowPopUp(_table[(byte)TranslateObject.language]);
        MenuMerch();
    }

    byte sco = 3;
    /*public void SponsorChoose(int i)
    {
        sco = (byte)i;
        popupQuest = SponsorChooseAlready;
        changeQuest = "menu";
        dontSwitchMenuDemand = true;
        string[] table = new string[]
        { "Are you sure?", "Вы уверенны?", "Bist du sicher?", "Tem certeza?", "Etes-vous sure?" };
        ShowPopUpAsk(table[(byte)TranslateObject.language], false);
    }

    private byte SponsorChooseAlready(bool b)
    {
        if (!b) return 202;
        myTeam.taskSponsor = ts[sco];
        myTeam.money += myTeam.taskSponsor.money;
        return 1;
    }*/

    public void UpgradeFactory(object o) { UpgradeFactory(); }

    public void UpgradeFactory()
    {
        popupQuest = UpgradeThisFactoryAlready;
        string[] table = new string[]
        {
            "Upgrade factory for: ",
            "Улучшить завод за: ",
            "Verbessern Sie die Fabrik für: ",
            "Atualizar fábrica para: ",
            "Améliorer l'usine pour"
        };
        int p = 32768 * myTeam.GetFactoryLevel() * myTeam.GetFactoryLevel();
        string str;
        if (p > 999999) str = string.Format("{0:# ### ###}", p);
        else str = string.Format("{0:### ###}", p);
        ShowPopUpAsk(table[(byte)TranslateObject.language] + str + " $", false);
    }

    private byte UpgradeThisFactoryAlready(bool b)
    {
        if (!b) return 199;
        int p = myTeam.GetFactoryLevel() * myTeam.GetFactoryLevel() * 32768;
        byte t = myTeam.UpgradeFactory();
        if (t == 0)
        {
            string[] table = new string[]
            {
                "Not enough money, needed: ",
                "Слишком мало денег, нужно: ",
                "Nicht genug Geld, benötigt: ",
                "Dinheiro insuficiente, necessário:",
                "Pas assez d'argent, vous avez besoin"
            };
            ShowPopUp(table[(byte)TranslateObject.language] + p.ToString() + " $"); return 0;
        }
        if (t == 10)
        {
            string[] table = new string[] 
            {
                "Your factory is already upgraded to highest level",
                "Ваш завод уже модернизирован до высочайшего уровня",
                "Ihre Fabrik wurde bereits auf das höchste Niveau aufgerüstet",
                "Sua fábrica já foi atualizada para o nível mais alto",
                "Votre usine est déjà améliorer au maximum"
            };
            ShowPopUp(table[(byte)TranslateObject.language]); return 10;
        }
        MenuMerch();
        return 1;
    }
    [SerializeField] private GameObject contractButton, contractApproved;
    public void CheckContract(string text)
    {
        if (text == ".")
        {
            contractButton.SetActive(false);
            contractApproved.SetActive(false);
            return;
        }
        if (System.Int32.Parse(text) < myTeam.players[myTeam.players.Count - 1].GetValue() * 0.02f)
        {
            string[] table = new string[]
            {
                "Player doesn't agree",
                "Игрок не согласен",
                "Spieler stimmt nicht zu",
                "Jogador não concorda",
                "Le joueur n'est pas d'acord"
            };
            ShowPopUp(table[(byte)TranslateObject.language]);
            contractButton.SetActive(false);
            contractApproved.SetActive(false);
            return;
        }
        myTeam.players[myTeam.players.Count - 1].salary = System.Int32.Parse(text);
        myTeam.players[myTeam.players.Count - 1].daysPayed = 30;
        contractButton.SetActive(true);
        contractApproved.SetActive(true);
    }

    public void CreateTeam(object o)
    {
        if (myTeam != null && stage == 0) return;
        myTeam = new Team();
        myTeam.currentTournament = -1;
        myTeam.teamName = (string)o;
        myTeam.followers = 0;
        myTeam.AddMoney(100000);
        //myTeam.money = 100000;
        myTeam.players = new List<Player>();
        myTeam.academy = new Academy();
        definedTeams.AddTeam(myTeam);
        definedTeams.GroupTeams();
        MenuTeam();
        //if (day == 1) StartCoroutine(Tutor());
    }

    public void AddPlayerToList(Player p) { definedPlayers.AddPlayer(p); }
}

[System.Serializable]
public struct EventRecord
{
    public string team, title;
    public EventRecord(EventRecord r) { this = r; }
    public EventRecord(string te, string ti) { team = te; title = ti; }
}

[System.Serializable]
public class NationID
{
    public Sprite sprite;
    public Manager.Nation nation;
    public string name;
}

[System.Serializable]
public class RoleID
{
    public Sprite sprite;
    public Manager.Role role;
}

[System.Serializable]
public class TournamentID
{
    public Sprite sprite;
    public string name;
}

[System.Serializable]
public class WeaponID //element 2 = glock, element 3 = usp
{
    public Sprite sprite;
    public Manager.Weapon weapon;
}

[System.Serializable]
public class Positions
{
    public RectTransform[] asite, bsite, mid;
}

[System.Serializable]
public struct AcademyInfo
{
    public short[] progression, needed;
    public byte[] targetRating, ready;
    public byte academyLevel;
    public bool anyPlayer;
    public List<Player> players;
}

[System.Serializable]
public class Academy
{
    private List<Player> players = new List<Player>(6); 
    private short[] progression = new short[6];
    private short[] needed = new short[6];
    private byte[] targetRating = new byte[6];
    private byte[] ready = new byte[6];
    [SerializeField] private byte academyLevel = 1; //25th lvl max
    public byte GetAcademyLevel() { return academyLevel; }
    public byte GetPlayersCount() { return (byte)players.Count; }
    public byte GetPlayerStrength(int i) { return players[i].strength; }
    public byte GetPlayerRole(int i) { return (byte)players[i].role; }
    public string GetPlayerNickname(int i) { return players[i].nickname; }
    public byte GetPlayerPotential(int i) { return targetRating[i]; }
    public byte GetPlayerNationality(int i) { return (byte)players[i].nationality; }
    public byte GetPlayerAge(int i) { return players[i].age; }

    public AcademyInfo Save()
    {
        AcademyInfo academyInfo = new AcademyInfo();
        if (players.Count == 0)
        {
            academyInfo.anyPlayer = false;
            academyInfo.progression = new short[] { 0 };
            academyInfo.needed = new short[] { 0 };
            academyInfo.targetRating = new byte[] { 0 };
            academyInfo.ready = new byte[] { 0 };
            academyInfo.players = new List<Player>(new Player[] { new Player() });
        }
        else
        {
            academyInfo.anyPlayer = true;
            academyInfo.progression = progression;
            academyInfo.needed = needed;
            academyInfo.targetRating = targetRating;
            academyInfo.ready = ready;
            academyInfo.players = players;
        }
        academyInfo.academyLevel = academyLevel;
        return academyInfo;
    }

    public void Load(AcademyInfo academyInfo)
    {
        //AcademyInfo academyInfo = SaveManager.Load<AcademyInfo>("academyInfo");
        if (academyInfo.anyPlayer == true)
        {
            progression = academyInfo.progression;
            needed = academyInfo.needed;
            targetRating = academyInfo.targetRating;
            ready = academyInfo.ready;
            players = academyInfo.players;
        }
        else
        {
            progression = new short[6];
            needed = new short[6];
            targetRating = new byte[6];
            ready = new byte[6];
            players = new List<Player>(6);
        }
        academyLevel = academyInfo.academyLevel;
    }

    public void SetLevel(byte level) { academyLevel = level; }

    public void AddDay(Team team)
    {
        if (team.players.Count == 0) return; 
        if (players.Count < 5)
        {
            for (byte i = 0; i < 5; i++) //role
            {
                for (byte j = 0; j < players.Count; j++)
                {
                    if ((int)players[j].role == i)
                    {
                        ++i; j = 0;
                    }
                }
                string _nicknames = PlayerNicknames.nickname[Random.Range(0, PlayerNicknames.nickname.Length)];
                CreatePlayer(
                    _nicknames,
                    (Manager.Role)i,
                    (Manager.Activity)Random.Range(0, 3),
                    team.players[Random.Range(0, team.players.Count)].nationality,
                    (byte)Random.Range(50 + academyLevel, 56 + academyLevel));
            }
        }
        for (byte i = 0; i < players.Count; i++)
        {
            ++progression[i];
            players[i].strength = (byte)
                (Mathf.RoundToInt(progression[i] / needed[i] * (targetRating[i] - 39)) + 39);
            if (players[i].strength > targetRating[i]) players[i].strength = targetRating[i];
            if (progression[i] >= needed[i]) ready[i] = 1;
        }
        for (byte i = 0; i < players.Count; i++)
        {
            if (ready[i] == 1) TakePlayer(team, i);
        }
    }

    public byte CreatePlayer(string nickname, Manager.Role role, Manager.Activity activity, 
        Manager.Nation nationality, byte rating)
    {
        if (players.Count == 6) return 6;
        players.Add(new Player());
        players[players.Count - 1].
            GeneratePlayer(nickname, role, activity, nationality, 39, true, false, true);
        needed[players.Count - 1] = (short)((rating - 39) * (30 - academyLevel));
        targetRating[players.Count - 1] = rating; ready[players.Count - 1] = 0;
        return 1;
    }

    public byte UpgradeAcademy(Manager manager)
    {
        if (academyLevel == 25) return 25;
        int total = 1024 * academyLevel * academyLevel;
        if (!manager.GetMyTeam().ReduceMoney(total)) return 0;
        ++academyLevel;
        for (byte i = 0; i < players.Count; i++)
        {
            needed[i] = (short)((targetRating[i] - 39) * (30 - academyLevel));
            if (needed[i] <= progression[i]) ready[i] = 1;
        }
        return 1;
    }

    private void Shift(byte indexer)
    {
        for (byte i = indexer; i < players.Count; i++)
        {
            progression[i] = progression[i + 1];
            needed[i] = needed[i + 1];
            ready[i] = ready[i + 1];
            targetRating[i] = targetRating[i + 1];
        }
        for (byte i = (byte)players.Count; i < 6; i++)
        {
            progression[i] = 0;
            needed[i] = 0;
            ready[i] = 0;
            targetRating[i] = 0;
        }
    }

    public byte TakePlayer(Team team, byte indexer)
    {
        if (team.players.Count > 10) return 11;
        if (ready[indexer] == 0) return 0;
        players[indexer].teamName = team.teamName;
        players[indexer].GeneratePlayer();
        players[indexer].RecalculateValue();
        team.players.Add(players[indexer]);
        players.RemoveAt(indexer);
        Shift(indexer);
        return 1;
    }
}

[System.Serializable]
public struct PointsEvent
{
    private byte tier;
    private short points;
    private int day;
    private float relevance, lanevance;

    public PointsEvent(byte tier, short points, int day)
    {
        this.tier = tier;
        this.points = points;
        this.day = day;
        relevance = Mathf.Clamp((day + 128 - Manager.day) * 0.0078125f, 0.0f, 1.0f);
        lanevance = Mathf.Clamp((3 - tier) * (day + 64 - Manager.day) * 0.015625f * 0.334f, 0, 1);
    }

    private void UpdateRelevance() { relevance = Mathf.Clamp((day + 128 - Manager.day) * 0.0078125f, 0.0f, 1.0f); }
    private void UpdateLanevance() { lanevance = Mathf.Clamp((3 - tier) * (day + 64 - Manager.day) * 0.015625f * 0.334f, 0, 1); }
    public int GetACHPoints() { UpdateRelevance(); return (int)(points * relevance); }
    public int GetLANPoints() { UpdateLanevance(); return (int)(points * lanevance); }
    public bool IsRelevant() { UpdateRelevance(); return !Mathf.Approximately(relevance, 0.0f); }
}

[System.Serializable] // 567 bytes avg
public class Team
{
    public string teamName; // 10 avg bytes
    private int money = 0; // 4 bytes
    public short majorPoints = 0; // 2 bytes
    [HideInInspector] public byte wantsChanges = 0;
    public int currentTournament = -1; // index of tournament which you are taking part in
    public int followers = 0, leaguePlayers = 0, playedToday = 0;
    public Academy academy;
    [HideInInspector] public byte place1st = 0, place2nd = 0, place3rd = 0;
    [HideInInspector] public bool entry, sniper, support, igl, lurk; // 5 bytes
    public byte[] mapStat; // 3 bytes
    public byte forSort;
    private byte[] maps = new byte[] { 0, 0, 0, 0, 0, 0, 0 };
    public List<Player> players; // 108 bytes * 5 = 540 bytes on average
    //public TaskSponsor taskSponsor;
    private byte energy = 100, mapPoints = 0, factoryLevel = 1;
    //media
    private int jersey = 0, sticker = 0, poster = 0; //jersey = 250, poster = 1250, sticker = 6250
    private List<PointsEvent> pointsEvents = new List<PointsEvent>();
    public void ReportTournament(PointsEvent t)
    {
        if (pointsEvents == null) pointsEvents = new List<PointsEvent>();
        for (byte i = 0; i < pointsEvents.Count; i++)
        {
            if (!pointsEvents[i].IsRelevant())
            {
                pointsEvents.RemoveAt(i--);
            }
        }
        pointsEvents.Add(t);
    }

    public int GetLANPoints() { return pointsEvents.Sum(x => x.GetLANPoints()); }
    public int GetACHPoints() { return pointsEvents.Sum(x => x.GetACHPoints()); }
    public int GetFORMPoints() { return players.Sum(x => x.WinForm()); }
    public void AddMoney(int m)
    {
        if (Manager.mainInstance != null)
            if (Manager.mainInstance.GetMyTeam() != null)
                if (Manager.mainInstance.GetMyTeam().teamName == teamName)
                    m -= (int)(m * 0.01 * Mathf.Clamp(Manager.mainInstance.GetInvestorShares(), 0, 100) + 0.5);
        money += m;
    }
    public void BetWin(int m) { money += m; }
    public int GetMoney() { return money; }
    public bool ReduceMoney(int m)
    {
        if (money < m) return false;
        money -= m;
        return true;
    }
    public byte UpgradeFactory()
    {
        int p = factoryLevel * factoryLevel * 32768;
        if (p > money) return 0;
        if (factoryLevel == 10) return 10;
        ReduceMoney(p); factoryLevel++;
        return 1;
    }
    public byte GetFactoryLevel() { return factoryLevel; }
    public void SetFactoryLevel(byte value) { factoryLevel = value; }
    public int GetJerseys() { return jersey; }
    public void SetJerseys(int value) { jersey = value; }
    public int GetPosters() { return poster; }
    public void SetPosters(int value) { poster = value; }
    public int GetStickers() { return sticker; }
    public void SetStickers(int value) { sticker = value; }
    public byte BuyJersey()
    {
        if (((250 * factoryLevel) - jersey) * 30 > money) return 0;
        ReduceMoney(((250 * factoryLevel) - jersey) * 30);
        jersey = 250 * factoryLevel; return 1;
    }
    public byte BuyJersey(int count) 
    {
        if (jersey == (factoryLevel * 250)) return 222;
        if (count + jersey > (factoryLevel * 250))
            count = (factoryLevel * 250) - jersey;
        if (count * 30 > money) return 0;
        ReduceMoney(count * 30);
        jersey += count; return 1;
    }
    public byte BuyPoster()
    {
        if (((1250 * factoryLevel) - poster) * 9 > money) return 0;
        ReduceMoney(((1250 * factoryLevel) - poster) * 9);
        money -= ((1250 * factoryLevel) - poster) * 9;
        poster = 1250 * factoryLevel; return 1;
    }
    public byte BuyPoster(int count)
    {
        if (poster == (factoryLevel * 1250)) return 222;
        if (count + poster > (factoryLevel * 1250))
            count = (factoryLevel * 1250) - poster;
        if (count * 9 > money) return 0;
        ReduceMoney(count * 9);
        poster += count; return 1;
    }
    public byte BuySticker()
    {
        if (Mathf.CeilToInt(((6250 * factoryLevel) - sticker) * 0.0625f) > money) return 0;
        ReduceMoney(Mathf.CeilToInt(((6250 * factoryLevel) - sticker) * 0.0625f));
        sticker = 6250 * factoryLevel; return 1;
    }
    public byte BuySticker(int count)
    {
        if (sticker == (factoryLevel * 6250)) return 222;
        if (count + sticker > (factoryLevel * 6250))
            count = (factoryLevel * 6250) - sticker;
        if (Mathf.CeilToInt(count * 0.0625f) > money) return 0;
        ReduceMoney(Mathf.CeilToInt(count * 0.0625f));
        sticker += count; return 1;
    }
    public void CalculateMedia() { for (byte i = 0; i < players.Count; i++) players[i].CalculateMedia(); }
    public int SellJersey()
    {
        short media = (short)(players[0].GetMedia() + players[1].GetMedia() + players[2].GetMedia()
            + players[3].GetMedia() + players[4].GetMedia());
        short t = (short)Mathf.CeilToInt((media & 511) * factoryLevel * 0.25f);
        if (t <= jersey)
        {
            AddMoney(t * 100);
            jersey -= t;
        }
        else
        {
            AddMoney(jersey * 100);
            jersey = 0;
        }
        return t;
    }
    public int SellPoster()
    {
        short media = (short)(players[0].GetMedia() + players[1].GetMedia() + players[2].GetMedia()
            + players[3].GetMedia() + players[4].GetMedia());
        short t = (short)((media & 511) * factoryLevel);
        if (t <= poster)
        {
            AddMoney(t * 30);
            poster -= t;
        }
        else
        {
            AddMoney(poster * 30);
            poster = 0;
        }
        return t;
    }
    public int SellSticker() 
    {
        short media = (short)(players[0].GetMedia() + players[1].GetMedia() + players[2].GetMedia()
            + players[3].GetMedia() + players[4].GetMedia());
        short t = (short)((media & 511) * factoryLevel * 4);
        if (t <= sticker)
        {
            AddMoney(t);
            sticker -= t;
        }
        else
        {
            AddMoney(sticker);
            sticker = 0;
        }
        return t;
    }
    public int SellJerseyF()
    {
        short media = (short)(followers * 0.001f);
        if (media > 511) media = 511;
        short t = (short)Mathf.CeilToInt((media & 511) * factoryLevel * 0.25f);
        if (t <= jersey)
        {
            AddMoney(t * 100);
            jersey -= t;
        }
        else
        {
            AddMoney(jersey * 100);
            jersey = 0;
        }
        return t;
    }
    public int SellPosterF()
    {
        short media = (short)(followers * 0.001f);
        if (media > 511) media = 511;
        short t = (short)((media & 511) * factoryLevel);
        if (t <= poster)
        {
            AddMoney(t * 30);
            poster -= t;
        }
        else
        {
            AddMoney(poster * 30);
            poster = 0;
        }
        return t;
    }
    public int SellStickerF()
    {
        short media = (short)(followers * 0.001f);
        if (media > 511) media = 511;
        short t = (short)((media & 511) * factoryLevel * 4);
        if (t <= sticker)
        {
            AddMoney(t);
            sticker -= t;
        }
        else
        {
            AddMoney(sticker);
            sticker = 0;
        }
        return t;
    }
    public byte GetMapPoints() { return mapPoints; }
    public void SetMapPoints(byte value) { mapPoints = value; }
    public void SetMap(int indexer, int value) { maps[indexer] = (byte)value; }
    public byte GetMap(Manager.Map map) { return maps[(byte)map]; }
    public byte[] GetMaps() { return maps; }
    public void AddMap(object o) { AddMap((Manager.Map)o); }
    public bool AddMap(Manager.Map map) 
    { if (maps[(byte)map] >= 10 || mapPoints == 0) return false; ++maps[(byte)map]; mapPoints--; return true; }
    public bool UseMap(Manager.Map map)
    { if (maps[(byte)map] > 0) --maps[(byte)map]; else return false; return true; }
    public void AddMapPoints(byte v) { mapPoints += v; if (mapPoints > 70) mapPoints = 70; }
    public bool UseMapPoints(byte v) { if (v > mapPoints) return false; mapPoints -= v; return true; }
    public byte GetEnergy() { return energy; }
    public void SetEnergy(byte value) { energy = value; }
    public void AddEnergy(Bootcamp bootcamp) { energy += bootcamp.energyAdd; if (energy > 100) energy = 100; }
    public bool UseEnergy(byte v) { if (v > energy) return false; energy -= v; return true; }
    private byte form = 50, chemistry = 50, psychology = 50;
    public void WorsenForm() { form -= 10; if (form > 101) form = 0; }
    public void WorsenChemistry() { chemistry -= (byte)(chemistry * 0.3125); if (chemistry > 101) chemistry = 0; }
    public void WorsenPsychology() { psychology -= (byte)(psychology * 0.3125); if (psychology > 101) psychology = 0; }
    public void AddForm(Bootcamp bootcamp)
    { form += (byte)(bootcamp.formAdd + bootcamp.assistantCoach.formAdd); if (form > 100) form = 100; }
    public void AddChemistry(Bootcamp bootcamp)
    { chemistry += (byte)(bootcamp.chemistryAdd + bootcamp.psychologist.chemistryAdd); if (chemistry > 100) chemistry = 100; }
    public void AddPsychology(Bootcamp bootcamp)
    { psychology += bootcamp.psychologist.psychologyAdd; if (psychology > 100) psychology = 100; }
    public void SetForm(byte v) { form = v; }
    public void SetChemistry(byte v) { chemistry = v; }
    public void SetPsychology(byte v) { psychology = v; }
    public byte GetForm() { return form; }
    public byte GetChemistry() { return chemistry; }
    public byte GetPsychology() { return psychology; }

    public void HasRoles()
    {
        entry = false; igl = false; support = false; sniper = false; lurk = false;
        for (byte i = 0; i < (players.Count > 4 ? 5 : players.Count); i++)
        {
            switch (players[i].role)
            {
                case Manager.Role.Entry:
                    entry = true;
                    break;
                case Manager.Role.Sniper:
                    sniper = true;
                    break;
                case Manager.Role.Support:
                    support = true;
                    break;
                case Manager.Role.Lurk:
                    lurk = true;
                    break;
                case Manager.Role.IGL:
                    igl = true;
                    break;
            }
        }
    }

    public void Sell(int indexer)
    {
        if (indexer >= players.Count) return;
        for (short i = 1; i < Manager.mainInstance.GetTeams().teams.Count; i++)
        {
            if (Manager.mainInstance.GetTeams().teams[i].money > players[indexer].RecalculateValue()
                && Manager.mainInstance.GetTeams().teams[i].players.Count < 11 &&
                Manager.mainInstance.GetTeams().teams[i].teamName != Manager.mainInstance.GetMyTeam().teamName)
            {
                Team t = Manager.mainInstance.GetTeams().teams[i];
                if ((t.players.OrderBy(x => x.RecalculateValue()).ToArray()[1].strength * 3 + 15 >
                    players[indexer].strength * 3 + t.wantsChanges)) continue;
                t.money -= players[indexer].GetValue();
                money += players[indexer].GetValue();
                string[] _table = new string[]
                { "Sold for: ", "Продано за: ", "Verkauft für: ", "Vendido por: ", "Vendu pour: " };
                Manager.mainInstance.ShowPopUp(_table[(int)TranslateObject.language] +
                    players[indexer].GetValue().ToString() + "$");
                players[indexer].teamName = t.teamName;
                players[indexer].RecalculateValue();
                t.players.Add(players[indexer]);
                players[indexer].daysInTeam = 90;
                players.Remove(players[indexer]);
                HasRoles();
                t.HasRoles();
                Manager.mainInstance.MenuTeam();
                return;
            }
        }
        string[] table = new string[]
        {
            "No one agreed to buy your player",
            "Никто не согласился покупать вашего игрока",
            "Niemand hat zugestimmt, Ihren Spieler zu kaufen",
            "Ninguém concordou em comprar o seu jogador",
            "Personne ne veut acheter votre joueur"
        };
        Manager.mainInstance.ShowPopUp(table[(int)TranslateObject.language]);
    }

    public void SellPlayerCPU(Team team, string nickname)
    {
        byte i = 0;
        for (; i < players.Count; i++)
        {
            if (players[i].nickname == nickname) break;
        }
        if (i == players.Count) 
        { Manager.mainInstance.ShowPopUp("Most likely a bug, no such a player"); return; }
        if (team.players.Count == 11)
        {
            string[] table = new string[]
            { "Your squad is full", "Ваш состав полон", "Dein Team ist voll", "Seu time está cheio", "Votre équipe est complète" };
            Manager.mainInstance.ShowPopUp(table[(byte)TranslateObject.language]); return;
        }
        if (players[i].RecalculateValue() > team.money) 
        {
            string[] table = new string[]
            { "Not enough money", "Не хватает денег", "Nicht genug Geld", "Dinheiro insuficiente", "Pas assez d'argent" };
            Manager.mainInstance.ShowPopUp(table[(byte)TranslateObject.language]); return;
        }
        int pp = (int)(Manager.describedPlayerPrice);
        Manager.buyPlayerMP = Random.Range(0.875f, 1.25f);
        Manager.mainInstance.saveState.buyPlayerMP = Manager.buyPlayerMP;
        double sr = (pp * 0.00001);
        Manager.influence -= (int)((sr * sr) + 0.5);
        money += pp;
        team.money -= pp;
        team.players.Add(players[i]);
        players[i].teamName = team.teamName;
        players[i].RecalculateValue();
        Player p = League.GetPlayerCPU(players[i].role);
        Manager.mainInstance.GetPlayers().AddPlayer(p);
        p.daysInTeam = 0;
        players.Add(p);
        players[i].daysInTeam = 90;
        players.Remove(players[i]);
        players[players.Count - 1].teamName = teamName;
        HasRoles();
        team.HasRoles();
        Manager.mainInstance.MenuContract(".");
        //Manager.mainInstance.PopDownDesc();
        Manager.mainInstance.playGames.UnlockAchievement(5);
    }

    public void Disappointment()
    {
        int indexer = 0;
        float lowest = 4.0f;
        for (byte i = 0; i < players.Count; i++)
        {
            if (players[i].FullStat().rating < lowest) { indexer = i; lowest = players[i].GetRating(); }
        }
        for (short i = 1; i < Manager.mainInstance.GetTeams().teams.Count; i++)
        {
            if (Manager.mainInstance.GetTeams().teams[i].money > players[indexer].RecalculateValue() &&
                Manager.mainInstance.GetTeams().teams[i].teamName != teamName &&
                Manager.mainInstance.GetTeams().teams[i].players.Count < 11 &&
                Manager.mainInstance.GetTeams().teams[i].teamName != Manager.mainInstance.GetMyTeam().teamName)
            {
                Team t = Manager.mainInstance.GetTeams().teams[i];
                if ((t.players.OrderBy(x => x.RecalculateValue()).ToArray()[1].strength * 3 + 33 >
                    players[indexer].strength * 3 + t.wantsChanges)) continue;
                int itp = 0;
                for (byte j = 0; j < t.players.Count; j++)
                {
                    if (t.players[j].role == players[indexer].role) { itp = j; break; }
                }
                t.wantsChanges = 0;
                wantsChanges = 0;
                money -= t.players[itp].RecalculateValue();
                t.money += t.players[itp].GetValue();
                double sr = (t.players[itp].GetValue() * 0.00001);
                Manager.influence -= (int)((sr * sr) + 0.5);
                money += players[indexer].RecalculateValue();
                t.money -= players[indexer].GetValue();
                sr = (players[indexer].GetValue() * 0.00001);
                Manager.influence -= (int)((sr * sr) + 0.5);
                players[indexer].teamName = t.teamName;
                players[indexer].RecalculateValue();
                players[indexer].daysInTeam = 90;
                t.players[itp].teamName = teamName;
                t.players[itp].RecalculateValue();
                t.players.Add(players[indexer]);
                //Manager.mainInstance.NewsGeneralUpdate(teamName + ": -" + players[indexer].nickname
                //    + ", +" + t.players[itp].nickname);
                players.Add(t.players[itp]);
                t.players[itp].daysInTeam = 90;
                players.Remove(players[indexer]);
                t.players.Remove(t.players[itp]);
                HasRoles();
                t.HasRoles();
                return;
            }
        }
        Player p = League.GetPlayerCPU(players[indexer].role, teamName);
        Manager.mainInstance.GetPlayers().AddPlayer(p);
        players.Add(p);
        p.daysInTeam = 90;
        //Manager.mainInstance.NewsGeneralUpdate(teamName + ": +" + p.nickname);
        HasRoles();
    }

    public void Check5MenMatch()
    {
        for (int i = players.Count; i < 5; i++)
        {
            int indexer = 0;
            if (!igl) indexer = 1;
            if (!sniper) indexer = 2;
            if (!support) indexer = 3;
            if (!lurk) indexer = 4;
            Player p = League.GetPlayerCPU(players[indexer].role, teamName);
            Manager.mainInstance.GetPlayers().AddPlayer(p);
            players.Add(p);
            p.daysInTeam = 90;
            //Manager.mainInstance.NewsGeneralUpdate(teamName + ": +" + p.nickname);
            HasRoles();
        }
    }
}


public class League
{
    public static List<Player> players;
    public static void GeneratePlayers()
    {
        players = new List<Player>(10);
        for (byte i = 0; i < 10; i++)
        {
            players.Add(PlayerNicknames.GeneratePlayerLeague());
        }
    }

    public static Player GetPlayerCPU(Manager.Role role)
    {
        return PlayerNicknames.GeneratePlayerLeague(role);
    }

    public static Player GetPlayerCPU(Manager.Role role, string teamname)
    {
        return PlayerNicknames.GeneratePlayerLeague(role, teamname);
    }
}

public struct SponsorUnit
{
    public string name;
    public int money;
    public ushort day;
    public bool PassDay() { if (--day == 0) return true; return false; }
    public SponsorUnit(string name, int money, ushort day)
    {
        this.name = name;
        this.money = money;
        this.day = day;
    }

    public override string ToString()
    {
        return $"{ name } offer you ${ money } for sponsorship for { day } days";
    }
}

public struct InvestorUnit
{
    public string name;
    public int money;
    public ushort day;
    public byte percent;
    public bool PassDay() { if (--day == 0) return true; return false; }
    public InvestorUnit(string first, string last, int money, ushort day, byte percent)
    {
        this.name = first + last;
        this.money = money;
        this.day = day;
        this.percent = percent;
    }

    public override string ToString()
    {
        return $"{ name } offers you ${ money } for { percent }% of your shares for { day } days";
    }
}

public class Sponsor
{
    public static readonly string[] names = new string[] 
    { 
        "Panthera", "Hyper", "Gaudi", "Bluebull", "Renoult", "FuelS", "Nisson", "Silverseries", "GazPRO",
        "Abibas", "OMAN", "TRITCH", "APOC", "ASOS", "Bonga", "PEPSO", "EXTM", "ScamBet", "ThrowBet", "Myway",
        "LukasOIL", "AKERBIO", "MIXAM", "EPSOS", "RVIDIA", "newzoo", "socios", "Logictech", "Monste", "ATT",
        "Chainblock", "BWM", "Kaiser", "Kingstons", "Mircosoft", "Secrets Lab", "DESPAWN", "SCUFM", "Previous",
        "DriftKings", "SunPay", "Totino", "MrDonald", "CoronaSelt"
    };

    public static readonly string[] firstnames = new string[]
    {
        "James", "Robert", "Adolf", "Max", "John", "David", "William", "Richard", "Joseph", "Daniel", "Matthew",
        "Anthony", "Steven", "Paul", "Mark", "Donald", "Thomas", "Charles", "Ryan", "Jacob", "Bill", "George",
        "Mary", "Patricia", "Jennifer", "Linda", "Sarah", "Nancy", "Lisa", "Betty", "Margaret", "Sandra",
        "Ashley", "Donna", "Amanda", "Melissa", "Rebecca", "Emily", "Laura", "Nicole", "Maria", "Victoria"
    };
    public static readonly string[] lastnames = new string[]
    {
        "Smith", "Johnson", "William", "Bronw", "Jones", "Davis", "Martin", "Anderson", "Bell", "Brooks", "Butler",
        "Coleman", "Cook", "Edwards", "Evans", "Flores", "Foster", "Garcia", "Gomez", "Gray", "Green", "Harris",
        "Hill", "Hughes", "Jackson", "James", "Kelly", "Lee", "Lewis", "Murphy", "Nelson", "Ortiz", "Simmons",
        "Patterson", "Perry", "Phillips", "Reyes", "Richardson", "Rivera", "Robinson", "Rogers", "Sanders",
    };
}

[System.Serializable]
public struct BestPlayer
{
    public byte place, year;
    public BestPlayer(byte place, byte year) { this.place = place; this.year = year; }
}

[System.Serializable] // 108 bytes avg without maps
public class Player
{
    public string nickname; // 7 bytes avg
    public string teamName; // 8 bytes avg
    private float rating = 0.0f, kpr = 0.0f, dpr = 1.0f, kast = 0.0f, impact = 0.0f, adr = 0.0f;
    private short progression = 0;
    public ushort yearPerformance = 0;
    public short daysInTeam = 90;
    private Queue<byte> formwin = new Queue<byte>(30);
    private List<BestPlayer> appearanceInTop = new List<BestPlayer>();
    public void ReportTop20(byte place, byte year)
    { 
        if (appearanceInTop == null) appearanceInTop = new List<BestPlayer>(); 
        if (Manager.day % 360 == 1) appearanceInTop.Add(new BestPlayer(place, year));
    }
    public List<BestPlayer> GetTop20Appearances() { return appearanceInTop; }
    public short GetProgression() { return progression; }
    public void SetProgression(short value) { progression = value; }
    public float GetRating() { return rating; }
    public float GetKPR() { return kpr; }
    public float GetDPR() { return dpr; }
    public float GetKAST() { return kast; }
    public float GetImpact() { return impact; }
    public float GetADR() { return adr; }
    public int GetValue() { return value; }
    public void Progress(Stat stat)
    {
        progression += (short)Mathf.RoundToInt((stat.rating - 1.0f) * 20 - (age - 26));
        if (strength == 99) { if (progression > 10000) progression = 9999; return; }
        if (strength == 40) { if (progression < -1000) progression = -999; return; }
        float t = Mathf.Pow((strength - 38) * 0.03125f, 2.0f);
        if (progression > 750 * t) { strength++; GeneratePlayer(); return; }
        float p = Mathf.Pow((120 - strength) * 0.02f, 2.0f);
        if (progression < -750 * p) { strength--; GeneratePlayer(); return; }
    }
    public int WinForm() { return formwin.Sum(x => x); }
    public void Win(byte tier)
    {
        progression += (short)(130 - ((age - 16) * 7));
        if (strength == 99) { if (progression > 10000) progression = 9999; return; }
        if (progression > 749) 
        { progression = 0; ++strength; ++awp; ++t; ++ct; ++entring; ++clutching; ++killing; ++rifle; }
        formwin.Enqueue((byte)(tier - 4));
        if (formwin.Count > 30) formwin.Dequeue();
    }
    public void Lose()
    {
        progression -= (short)(-10 + ((age - 16) * 7));
        if (strength == 40) { if (progression < -1000) progression = -999; return; }
        if (progression < -749)
        { progression = 0; --strength; --awp; --t; --ct; --entring; --clutching; --killing; --rifle; }
        formwin.Enqueue(0);
        if (formwin.Count > 30) formwin.Dequeue();
    }
    public List<int> stats; // 40 bytes for 1 map
    public List<Stat> playerStats;
    public Stat OneMatchStat(int indexer)
    {
        byte j = 0;
        Stat[] _stats = MatchSave.LoadStats(indexer, nickname);
        Stat stat = new Stat().SetStats(nickname);
        for (; _stats[j] != null;) { stat.AddStats(_stats[j]); j++; }
        stat.kast /= (float)j;
        stat.CalculateRating();
        return stat;
    }
    public Stat FullStat(int period = 90) // 30 d, 90 d, 180 d, 360 d
    {
        if (stats.Count == 0) { Stat _stat = new Stat(); _stat.SetStats(nickname); return _stat; }
        //int endDay = Manager.day;
        int startDay = Manager.day - period;
        Stat stat = new Stat();
        stat.SetStats(nickname);
        //stat.SetStats(nickname);
        ushort j = 0;
        /*ushort i = stats.Count - period > 0 ? (ushort)(stats.Count - period) : (ushort)0;
        //hope it works
        for (; i < stats.Count; i++)
        {
            int d = MatchSave.GetDay(stats[i]);
            if (d >= startDay /*!(d >= startDay) && d <= endDay) break;
        }
        for (; i < stats.Count; i++)
        {
            Stat[] _stats = MatchSave.LoadStats(stats[i], nickname);
            for (ushort l = 0; _stats[l] != null; l++) { stat.AddStats(_stats[l]); j++; }
        }*/
        ushort i = playerStats.Count - (period * 4) > 0 ? (ushort)(playerStats.Count - (period * 4)) : (ushort)0;
        //hope it works
        for (; i < playerStats.Count; i++)
        {
            if (playerStats[i].day >= startDay /*!(d >= startDay) && d <= endDay*/) break;
        }
        for (; i < playerStats.Count; i++)
        {
            stat.AddStats(playerStats[i]); j++;
        }
        if (j == 0) { Stat _stat = new Stat(); _stat.SetStats(nickname); return _stat; }
        stat.kast /= (float)j;
        stat.CalculateRating();
        rating = stat.rating;
        float r = 1.0f / (float)stat.rounds;
        kpr = (float)stat.kills * r;
        dpr = (float)stat.deaths * r;
        adr = (float)stat.damage / stat.rounds;
        kast = stat.kast;
        impact = stat.impact;
        return stat;
    }
    public bool calculating = false;
    public IEnumerator FullStatEnum(int period = 90) // 30 d, 90 d, 180 d, 360 d
    {
        calculating = true;
        if (stats.Count != 0)
        {
            int startDay = Manager.day - period;
            Stat stat = new Stat();
            stat.SetStats(nickname);
            //stat.SetStats(nickname);
            ushort j = 0;
            ushort i = playerStats.Count - (period * 4) > 0 ? (ushort)(playerStats.Count - (period * 4)) : (ushort)0;
            //hope it works
            for (; i < playerStats.Count; i++)
            {
                if (playerStats[i].day >= startDay /*!(d >= startDay) && d <= endDay*/) break;
            }
            for (; i < playerStats.Count; i++)
            {
                yield return new WaitForSeconds(0.03125f);
                stat.AddStats(playerStats[i]); j++;
            }
            /*ushort i = stats.Count - period > 0 ? (ushort)(stats.Count - period) : (ushort)0;
            //hope it works
            for (; i < stats.Count; i++)
            {
                int d = MatchSave.GetDay(stats[i]);
                if (d >= startDay /*!(d >= startDay) && d <= endDay) break;
            }
            for (; i < stats.Count; i++)
            {
                
                Stat[] _stats = MatchSave.LoadStats(stats[i], nickname);
                for (ushort l = 0; _stats[l] != null; l++) { stat.AddStats(_stats[l]); j++; }
            }*/
            if (j != 0)
            {
                stat.kast /= (float)j;
                stat.CalculateRating();
                rating = stat.rating;
                float r = 1.0f / (float)stat.rounds;
                kpr = (float)stat.kills * r;
                dpr = (float)stat.deaths * r;
                adr = (float)stat.damage / stat.rounds;
                kast = stat.kast;
                impact = stat.impact;
            }
        }
        calculating = false;
    }
    [SerializeField] private int value; // 4 bytes
    [HideInInspector] public int[] mapStat; // 48 bytes?
    [HideInInspector] public byte[] maps; // 1-5 5 the best 1 the worst // 7 bytes
    private byte media = 0; // 1 byte
    public byte GetMedia() { return (byte)(media & 127); }
    public byte CalculateMedia()
    {
        int p = Manager.mainInstance.GetTeams().GetTeamPlacement(teamName);
        media = (byte)((p < 50 ? 50 - p : 0) + (strength - 47));
        return media;
    }
    public byte evp = 0, mvp = 0;
    public byte strength, age; // 2 bytes
    //just for testing i disabled [HideInInspector]
    public byte awp, rifle, entring, clutching, ct, t, killing; // 7 bytes
    public Manager.Nation nationality, language; // 8 bytes
    public Manager.Activity activity; // 4 bytes
    public Manager.Role role; // 4 bytes
    [HideInInspector] public int salary = 0, daysPayed = 0;

    public Player GeneratePlayer(string nickname, Manager.Role role, Manager.Activity activity, 
        Manager.Nation nationality, byte strength, bool free = false, bool hadStats = false, bool genAge = false)
    {
        if (!hadStats) stats = new List<int>();
        if (genAge) age = (byte)Random.Range(16, 21);
        daysInTeam = 90;
        this.nickname = nickname;
        if (role == Manager.Role.O_NOT_SET) this.role = (Manager.Role)Random.Range(0, 5);
        else this.role = role;
        if (activity == Manager.Activity.O_NOT_SET) this.activity = (Manager.Activity)Random.Range(0, 3);
        else this.activity = activity;
        this.nationality = nationality;
        language = nationality;
        switch (language)
        {
            case Manager.Nation.Bosnian:
                language = Manager.Nation.Serbian;
                break;
            case Manager.Nation.Ukrainian:
                language = Manager.Nation.Russian;
                break;
            case Manager.Nation.American:
                language = Manager.Nation.Brit;
                break;
            case Manager.Nation.Canadian:
                language = Manager.Nation.Brit;
                break;
            case Manager.Nation.Australian:
                language = Manager.Nation.Brit;
                break;
            case Manager.Nation.Kazakh:
                language = Manager.Nation.Russian;
                break;
            case Manager.Nation.Belarusian:
                language = Manager.Nation.Russian;
                break;
            case Manager.Nation.Montenegrish:
                language = Manager.Nation.Serbian;
                break;
            case Manager.Nation.African:
                language = Manager.Nation.Brit;
                break;
            case Manager.Nation.Portugese:
                language = Manager.Nation.Brazilian;
                break;
            case Manager.Nation.Indonesian:
                language = Manager.Nation.Brit;
                break;
            case Manager.Nation.Uzbek:
                language = Manager.Nation.Russian;
                break;
            case Manager.Nation.Irish:
                language = Manager.Nation.Brit;
                break;
            case Manager.Nation.Luxembourgish:
                language = Manager.Nation.German;
                break;
            case Manager.Nation.Switzerlandish:
                language = Manager.Nation.German;
                break;
        }
        this.strength = strength;
        maps = new byte[8];
        for (byte i = 0; i < maps.Length; i++) maps[i] = 1;
        switch (role)
        {
            case Manager.Role.Entry:
                maps[(byte)Manager.Map.Vertigo] += 2;
                maps[(byte)Manager.Map.Mirage]++;
                maps[(byte)Manager.Map.Inferno]++;
                if (activity == Manager.Activity.Agressive)
                {
                    awp = (byte)(strength - Random.Range(1, 12));
                    entring = (byte)(strength + Random.Range(5, 15));
                    clutching = (byte)(strength + Random.Range(-11, 6));
                    rifle = (byte)(strength + Random.Range(-3, 12));
                    killing = (byte)(strength + Random.Range(-7, 8));
                    byte d = (byte)Random.Range(-17, 8);
                    ct = (byte)(strength + d);
                    t = (byte)(strength - d);
                }
                else if (activity == Manager.Activity.Neutral)
                {
                    awp = (byte)(strength - Random.Range(1, 12));
                    entring = (byte)(strength + Random.Range(3, 10));
                    clutching = (byte)(strength + Random.Range(-9, 8));
                    rifle = (byte)(strength + Random.Range(-3, 10));
                    killing = (byte)(strength + Random.Range(-5, 6));
                    byte d = (byte)(Random.Range(-10, 11));
                    ct = (byte)(strength + d);
                    t = (byte)(strength - d);
                }
                else
                {
                    awp = (byte)(strength - Random.Range(1, 12));
                    entring = (byte)(strength + Random.Range(1, 8));
                    clutching = (byte)(strength + Random.Range(-8, 9));
                    rifle = (byte)(strength + Random.Range(-3, 7));
                    killing = (byte)(strength + Random.Range(-3, 4));
                    byte d = (byte)(Random.Range(-7, 18));
                    ct = (byte)(strength + d);
                    t = (byte)(strength - d);
                }
                break;
            case Manager.Role.Sniper:
                maps[(byte)Manager.Map.Dust] += 2;
                maps[(byte)Manager.Map.Nuke]++;
                maps[(byte)Manager.Map.Overpass]++;
                if (activity == Manager.Activity.Agressive)
                {
                    awp = (byte)(strength + Random.Range(5, 15));
                    entring = (byte)(strength + Random.Range(0, 11));
                    clutching = (byte)(strength + Random.Range(-10, 11));
                    rifle = (byte)(strength + Random.Range(-7, 8));
                    killing = (byte)(strength + Random.Range(-7, 8));
                    byte d = (byte)Random.Range(-13, 13);
                    ct = (byte)(strength + d);
                    t = (byte)(strength - d);
                }
                else if (activity == Manager.Activity.Neutral)
                {
                    awp = (byte)(strength + Random.Range(5, 15));
                    entring = (byte)(strength + Random.Range(-5, 6));
                    clutching = (byte)(strength + Random.Range(-10, 11));
                    rifle = (byte)(strength + Random.Range(-9, 6));
                    killing = (byte)(strength + Random.Range(-5, 6));
                    byte d = (byte)(Random.Range(-8, 18));
                    ct = (byte)(strength + d);
                    t = (byte)(strength - d);
                }
                else
                {
                    awp = (byte)(strength + Random.Range(5, 15));
                    entring = (byte)(strength + Random.Range(-10, 1));
                    clutching = (byte)(strength + Random.Range(-10, 11));
                    rifle = (byte)(strength + Random.Range(-11, 4));
                    killing = (byte)(strength + Random.Range(-3, 4));
                    byte d = (byte)(Random.Range(-3, 23));
                    ct = (byte)(strength + d);
                    t = (byte)(strength - d);
                }
                break;
            case Manager.Role.Support:
                maps[(byte)Manager.Map.Ancient]++;
                maps[(byte)Manager.Map.Overpass] += 2;
                maps[(byte)Manager.Map.Inferno]++;
                if (activity == Manager.Activity.Agressive)
                {
                    awp = (byte)(strength - Random.Range(1, 12));
                    entring = (byte)(strength + Random.Range(-5, 15));
                    clutching = (byte)(strength + Random.Range(-15, 6));
                    rifle = (byte)(strength + Random.Range(-11, 12));
                    killing = (byte)(strength + Random.Range(-9, 6));
                    byte d = (byte)Random.Range(-17, 8);
                    ct = (byte)(strength + d);
                    t = (byte)(strength - d);
                }
                else if (activity == Manager.Activity.Neutral)
                {
                    awp = (byte)(strength - Random.Range(1, 12));
                    entring = (byte)(strength + Random.Range(-8, 12));
                    clutching = (byte)(strength + Random.Range(-10, 11));
                    rifle = (byte)(strength + Random.Range(-9, 10));
                    killing = (byte)(strength + Random.Range(-9, 6));
                    byte d = (byte)(Random.Range(-10, 11));
                    ct = (byte)(strength + d);
                    t = (byte)(strength - d);
                }
                else
                {
                    awp = (byte)(strength - Random.Range(1, 12));
                    entring = (byte)(strength + Random.Range(-10, 9));
                    clutching = (byte)(strength + Random.Range(-5, 16));
                    rifle = (byte)(strength + Random.Range(-7, 8));
                    killing = (byte)(strength + Random.Range(-9, 6));
                    byte d = (byte)(Random.Range(-7, 18));
                    ct = (byte)(strength + d);
                    t = (byte)(strength - d);
                }
                break;
            case Manager.Role.Lurk:
                maps[(byte)Manager.Map.Dust]++;
                maps[(byte)Manager.Map.Nuke] += 2;
                maps[(byte)Manager.Map.Vertigo]++;
                killing = (byte)(strength + Random.Range(-7, 10));
                if (activity == Manager.Activity.Agressive)
                {
                    awp = (byte)(strength - Random.Range(0, 12));
                    entring = (byte)(strength + Random.Range(-9, 8));
                    clutching = (byte)(strength + Random.Range(-9, 9));
                    rifle = (byte)(strength + Random.Range(-9, 14));
                    byte d = (byte)Random.Range(-17, 8);
                    ct = (byte)(strength + d);
                    t = (byte)(strength - d);
                }
                else if (activity == Manager.Activity.Neutral)
                {
                    awp = (byte)(strength - Random.Range(0, 12));
                    entring = (byte)(strength + Random.Range(-13, 4));
                    clutching = (byte)(strength + Random.Range(-4, 14));
                    rifle = (byte)(strength + Random.Range(-7, 12));
                    byte d = (byte)(Random.Range(-10, 11));
                    ct = (byte)(strength + d);
                    t = (byte)(strength - d);
                }
                else
                {
                    awp = (byte)(strength - Random.Range(0, 12));
                    entring = (byte)(strength + Random.Range(-17, 0));
                    clutching = (byte)(strength + Random.Range(2, 20));
                    rifle = (byte)(strength + Random.Range(-4, 9));
                    byte d = (byte)(Random.Range(-7, 18));
                    ct = (byte)(strength + d);
                    t = (byte)(strength - d);
                }
                break;
            case Manager.Role.IGL:
                maps[(byte)Manager.Map.Ancient]++;
                maps[(byte)Manager.Map.Mirage] += 2;
                maps[(byte)Manager.Map.Inferno]++;
                awp = (byte)(strength + Random.Range(-7, 8));
                entring = (byte)(strength + Random.Range(-9, 10));
                clutching = (byte)(strength + Random.Range(-10, 11));
                rifle = (byte)(strength + Random.Range(-7, 8));
                killing = (byte)(strength + Random.Range(-5, 6));
                byte diff = (byte)Random.Range(-10, 11);
                ct = (byte)(strength + diff);
                t = (byte)(strength - diff);
                break;
        }
        if (Manager.Activity.Agressive == activity)
        {
            maps[(byte)Manager.Map.Dust] += 2;
            maps[(byte)Manager.Map.Nuke] += 1;
            maps[(byte)Manager.Map.Vertigo] += 2;
            maps[(byte)Manager.Map.Overpass] += 1;
            //maps[(byte)Manager.Map.Ancient] += 0;
            maps[(byte)Manager.Map.Mirage] += 1;
            maps[(byte)Manager.Map.Inferno] += 2;
        }
        else if (Manager.Activity.Neutral == activity)
        {
            maps[(byte)Manager.Map.Dust] += 2;
            //maps[(byte)Manager.Map.Nuke] += 0;
            maps[(byte)Manager.Map.Vertigo] += 1;
            maps[(byte)Manager.Map.Overpass] += 2;
            maps[(byte)Manager.Map.Ancient] += 1;
            maps[(byte)Manager.Map.Mirage] += 2;
            maps[(byte)Manager.Map.Inferno] += 1;
        }
        else
        {
            maps[(byte)Manager.Map.Dust] += 1;
            maps[(byte)Manager.Map.Nuke] += 2;
            //maps[(byte)Manager.Map.Vertigo] += 0;
            maps[(byte)Manager.Map.Overpass] += 2;
            maps[(byte)Manager.Map.Ancient] += 2;
            maps[(byte)Manager.Map.Mirage] += 1;
            maps[(byte)Manager.Map.Inferno] += 1;
        }
        if (!free) RecalculateValue();
        return this;
    }

    private static int roleCount = 0;
    public Player GeneratePlayer()
    {
        if (role == Manager.Role.O_NOT_SET) role = (Manager.Role)(roleCount % 5);
        if (activity == Manager.Activity.O_NOT_SET) activity = (Manager.Activity)Random.Range(0, 3);
        daysInTeam = 90;
        ++roleCount; language = nationality;
        switch (language)
        {
            case Manager.Nation.Bosnian:
                language = Manager.Nation.Serbian;
                break;
            case Manager.Nation.Ukrainian:
                language = Manager.Nation.Russian;
                break;
            case Manager.Nation.American:
                language = Manager.Nation.Brit;
                break;
            case Manager.Nation.Canadian:
                language = Manager.Nation.Brit;
                break;
            case Manager.Nation.Australian:
                language = Manager.Nation.Brit;
                break;
            case Manager.Nation.Kazakh:
                language = Manager.Nation.Russian;
                break;
            case Manager.Nation.Belarusian:
                language = Manager.Nation.Russian;
                break;
            case Manager.Nation.Montenegrish:
                language = Manager.Nation.Serbian;
                break;
            case Manager.Nation.African:
                language = Manager.Nation.Brit;
                break;
            case Manager.Nation.Portugese:
                language = Manager.Nation.Brazilian;
                break;
            case Manager.Nation.Indonesian:
                language = Manager.Nation.Brit;
                break;
            case Manager.Nation.Uzbek:
                language = Manager.Nation.Russian;
                break;
            case Manager.Nation.Irish:
                language = Manager.Nation.Brit;
                break;
            case Manager.Nation.Luxembourgish:
                language = Manager.Nation.German;
                break;
            case Manager.Nation.Switzerlandish:
                language = Manager.Nation.German;
                break;
        }
        maps = new byte[8];
        for (byte i = 0; i < maps.Length; i++) maps[i] = 1;
        switch (role)
        {
            case Manager.Role.Entry:
                maps[(byte)Manager.Map.Vertigo] += 2;
                maps[(byte)Manager.Map.Mirage]++;
                maps[(byte)Manager.Map.Inferno]++;
                if (activity == Manager.Activity.Agressive)
                {
                    awp = (byte)(strength - Random.Range(1, 12));
                    entring = (byte)(strength + Random.Range(5, 15));
                    clutching = (byte)(strength + Random.Range(-11, 6));
                    rifle = (byte)(strength + Random.Range(-3, 12));
                    killing = (byte)(strength + Random.Range(-7, 8));
                    byte d = (byte)Random.Range(-17, 8);
                    ct = (byte)(strength + d);
                    t = (byte)(strength - d);
                }
                else if (activity == Manager.Activity.Neutral)
                {
                    awp = (byte)(strength - Random.Range(1, 12));
                    entring = (byte)(strength + Random.Range(3, 10));
                    clutching = (byte)(strength + Random.Range(-9, 8));
                    rifle = (byte)(strength + Random.Range(-3, 10));
                    killing = (byte)(strength + Random.Range(-5, 6));
                    byte d = (byte)(Random.Range(-10, 11));
                    ct = (byte)(strength + d);
                    t = (byte)(strength - d);
                }
                else
                {
                    awp = (byte)(strength - Random.Range(1, 12));
                    entring = (byte)(strength + Random.Range(1, 8));
                    clutching = (byte)(strength + Random.Range(-8, 9));
                    rifle = (byte)(strength + Random.Range(-3, 7));
                    killing = (byte)(strength + Random.Range(-3, 4));
                    byte d = (byte)(Random.Range(-7, 18));
                    ct = (byte)(strength + d);
                    t = (byte)(strength - d);
                }
                break;
            case Manager.Role.Sniper:
                maps[(byte)Manager.Map.Dust] += 2;
                maps[(byte)Manager.Map.Nuke]++;
                maps[(byte)Manager.Map.Overpass]++;
                if (activity == Manager.Activity.Agressive)
                {
                    awp = (byte)(strength + Random.Range(5, 15));
                    entring = (byte)(strength + Random.Range(0, 11));
                    clutching = (byte)(strength + Random.Range(-10, 11));
                    rifle = (byte)(strength + Random.Range(-7, 8));
                    killing = (byte)(strength + Random.Range(-7, 8));
                    byte d = (byte)Random.Range(-13, 13);
                    ct = (byte)(strength + d);
                    t = (byte)(strength - d);
                }
                else if (activity == Manager.Activity.Neutral)
                {
                    awp = (byte)(strength + Random.Range(5, 15));
                    entring = (byte)(strength + Random.Range(-5, 6));
                    clutching = (byte)(strength + Random.Range(-10, 11));
                    rifle = (byte)(strength + Random.Range(-9, 6));
                    killing = (byte)(strength + Random.Range(-5, 6));
                    byte d = (byte)(Random.Range(-8, 18));
                    ct = (byte)(strength + d);
                    t = (byte)(strength - d);
                }
                else
                {
                    awp = (byte)(strength + Random.Range(5, 15));
                    entring = (byte)(strength + Random.Range(-10, 1));
                    clutching = (byte)(strength + Random.Range(-10, 11));
                    rifle = (byte)(strength + Random.Range(-11, 4));
                    killing = (byte)(strength + Random.Range(-3, 4));
                    byte d = (byte)(Random.Range(-3, 23));
                    ct = (byte)(strength + d);
                    t = (byte)(strength - d);
                }
                break;
            case Manager.Role.Support:
                maps[(byte)Manager.Map.Ancient]++;
                maps[(byte)Manager.Map.Overpass] += 2;
                maps[(byte)Manager.Map.Inferno]++;
                if (activity == Manager.Activity.Agressive)
                {
                    awp = (byte)(strength - Random.Range(1, 12));
                    entring = (byte)(strength + Random.Range(-5, 15));
                    clutching = (byte)(strength + Random.Range(-15, 6));
                    rifle = (byte)(strength + Random.Range(-11, 12));
                    killing = (byte)(strength + Random.Range(-9, 6));
                    byte d = (byte)Random.Range(-17, 8);
                    ct = (byte)(strength + d);
                    t = (byte)(strength - d);
                }
                else if (activity == Manager.Activity.Neutral)
                {
                    awp = (byte)(strength - Random.Range(1, 12));
                    entring = (byte)(strength + Random.Range(-8, 12));
                    clutching = (byte)(strength + Random.Range(-10, 11));
                    rifle = (byte)(strength + Random.Range(-9, 10));
                    killing = (byte)(strength + Random.Range(-9, 6));
                    byte d = (byte)(Random.Range(-10, 11));
                    ct = (byte)(strength + d);
                    t = (byte)(strength - d);
                }
                else
                {
                    awp = (byte)(strength - Random.Range(1, 12));
                    entring = (byte)(strength + Random.Range(-10, 9));
                    clutching = (byte)(strength + Random.Range(-5, 16));
                    rifle = (byte)(strength + Random.Range(-7, 8));
                    killing = (byte)(strength + Random.Range(-9, 6));
                    byte d = (byte)(Random.Range(-7, 18));
                    ct = (byte)(strength + d);
                    t = (byte)(strength - d);
                }
                break;
            case Manager.Role.Lurk:
                maps[(byte)Manager.Map.Dust]++;
                maps[(byte)Manager.Map.Nuke] += 2;
                maps[(byte)Manager.Map.Vertigo]++;
                killing = (byte)(strength + Random.Range(-7, 10));
                if (activity == Manager.Activity.Agressive)
                {
                    awp = (byte)(strength - Random.Range(0, 12));
                    entring = (byte)(strength + Random.Range(-9, 8));
                    clutching = (byte)(strength + Random.Range(-9, 9));
                    rifle = (byte)(strength + Random.Range(-9, 14));
                    byte d = (byte)Random.Range(-17, 8);
                    ct = (byte)(strength + d);
                    t = (byte)(strength - d);
                }
                else if (activity == Manager.Activity.Neutral)
                {
                    awp = (byte)(strength - Random.Range(0, 12));
                    entring = (byte)(strength + Random.Range(-13, 4));
                    clutching = (byte)(strength + Random.Range(-4, 14));
                    rifle = (byte)(strength + Random.Range(-7, 12));
                    byte d = (byte)(Random.Range(-10, 11));
                    ct = (byte)(strength + d);
                    t = (byte)(strength - d);
                }
                else
                {
                    awp = (byte)(strength - Random.Range(0, 12));
                    entring = (byte)(strength + Random.Range(-17, 0));
                    clutching = (byte)(strength + Random.Range(2, 20));
                    rifle = (byte)(strength + Random.Range(-4, 9));
                    byte d = (byte)(Random.Range(-7, 18));
                    ct = (byte)(strength + d);
                    t = (byte)(strength - d);
                }
                break;
            case Manager.Role.IGL:
                maps[(byte)Manager.Map.Ancient]++;
                maps[(byte)Manager.Map.Mirage] += 2;
                maps[(byte)Manager.Map.Inferno]++;
                awp = (byte)(strength + Random.Range(-7, 8));
                entring = (byte)(strength + Random.Range(-9, 10));
                clutching = (byte)(strength + Random.Range(-10, 11));
                rifle = (byte)(strength + Random.Range(-7, 8));
                killing = (byte)(strength + Random.Range(-5, 6));
                byte diff = (byte)Random.Range(-10, 11);
                ct = (byte)(strength + diff);
                t = (byte)(strength - diff);
                break;
        }
        if (Manager.Activity.Agressive == activity)
        {
            maps[(byte)Manager.Map.Dust] += 2;
            maps[(byte)Manager.Map.Nuke] += 1;
            maps[(byte)Manager.Map.Vertigo] += 2;
            maps[(byte)Manager.Map.Overpass] += 1;
            //maps[(byte)Manager.Map.Ancient] += 0;
            maps[(byte)Manager.Map.Mirage] += 1;
            maps[(byte)Manager.Map.Inferno] += 2;
        }
        else if (Manager.Activity.Neutral == activity)
        {
            maps[(byte)Manager.Map.Dust] += 2;
            //maps[(byte)Manager.Map.Nuke] += 0;
            maps[(byte)Manager.Map.Vertigo] += 1;
            maps[(byte)Manager.Map.Overpass] += 2;
            maps[(byte)Manager.Map.Ancient] += 1;
            maps[(byte)Manager.Map.Mirage] += 2;
            maps[(byte)Manager.Map.Inferno] += 1;
        }
        else
        {
            maps[(byte)Manager.Map.Dust] += 1;
            maps[(byte)Manager.Map.Nuke] += 2;
            //maps[(byte)Manager.Map.Vertigo] += 0;
            maps[(byte)Manager.Map.Overpass] += 2;
            maps[(byte)Manager.Map.Ancient] += 2;
            maps[(byte)Manager.Map.Mirage] += 1;
            maps[(byte)Manager.Map.Inferno] += 1;
        }
        RecalculateValue();
        return this;
    }

    public int RecalculateValue()
    {
        if (teamName == "Free") return 0;
        if (strength > 86) value = (Mathf.Abs(36 - age) * Manager.influence * 
                Mathf.RoundToInt((strength - 86) * 0.375f + 1.0f));
        else if (strength > 70) value = (int)(Mathf.Abs(36 - age) * Manager.influence *
                Mathf.Pow((strength - 54) * 0.03125f, 1.5f));
        else value = (int)(Mathf.Abs(36 - age) * Manager.influence *
                ((strength - 6) * 0.015625f * 0.3535533f));
        if (role == Manager.Role.IGL) value = (int)(value * 0.84375f);
        else if (role == Manager.Role.Support) value = (int)(value * 0.75f);
        //else if (role == Manager.Role.Lurk) value = (int)(value * 1f);
        else if (role == Manager.Role.Sniper) value = (int)(value * 1.265625f);
        else if (role == Manager.Role.Entry) value = (int)(value * 1.171875f);
        short t = (short)Manager.mainInstance.GetTeams().GetTeamPlacement(teamName);
        if (t < 16) value = (int)(value * (Mathf.Abs(t - 15) * 0.125f + 1.0f));
        else value = (int)(value * (Mathf.Abs(t - 272) * 0.00390625f));
        float f = (float)daysInTeam / 90;
        return (daysInTeam > 90 ? value : (int)(value * (f * f)));
    }
}

[System.Serializable]
public class Stat
{
    public string nickname;
    public float kast, rating, impact;
    public int day, kills, deaths, assists, entries, rounds, x1, x2, x3, x4, x5, v1, v2, v3, v4, v5, damage;

    public float GetRating() { return rating; }

    public Stat SetStats(string _nickname)
    {
        nickname = _nickname;
        rating = 0.0f;
        impact = 0.0f;
        kills = 0;
        deaths = 0;
        entries = 0;
        x1 = 0; x2 = 0; x3 = 0; x4 = 0; x5 = 0;
        v1 = 0;//v2v3v4v5
        kast = 0;
        rounds = 0;
        return this;
    }

    public void SetStats(string _nickname, int[] info, short _rounds, short _day)
    {
        day = _day;
        nickname = _nickname;
        kills = info[1];
        deaths = info[2];
        assists = info[3];
        entries = info[4];
        x1 = info[5]; x2 = info[6]; x3 = info[7]; x4 = info[8]; x5 = info[9];
        v1 = info[10]; v2 = info[11]; v3 = info[12]; v4 = info[13]; v5 = info[14];
        kast = ((float)info[15]) / ((float)_rounds);
        damage = info[20];
        rounds = _rounds;
    }

    public void AddStats(Stat a)
    {
        this.kills += a.kills;
        this.deaths += a.deaths;
        this.assists += a.assists;
        this.damage += a.damage;
        //this.impact += a.impact;
        //this.rating += a.rating;
        this.kast += a.kast;
        this.entries += a.entries;
        this.x1 += a.x1;
        this.x2 += a.x2;
        this.x3 += a.x3;
        this.x4 += a.x4;
        this.x5 += a.x5;
        this.rounds += a.rounds;
    }

    public void CalculateRating()
    {
        impact = (float)((double)((x2 + x3 + x4 + x5) * 5.25 + entries * 3.5 + (v1 + v2 + v3 + v4 + v5) * 2.5) / rounds);
        float adr = (float)damage / rounds;
        float kpr = (float)kills / rounds;
        float kr = (kpr > 0.2f ? kpr - 0.2f : 0.0f) * 2.128f;
        rating = (kr * 1.05f + ((float)(rounds - deaths) / rounds * 3.0f) * 0.7f +
            impact * 1.25f +
            (kast - 0.336f > 0.0f ? (kast - 0.336f) * 3.0f : 0.0f) +
            ((adr - 30.0f) > 0.0f ? (adr - 30.0f) * 0.0222f : 0.0f)) * 0.2f;
    }
}

[System.Serializable] // 1557 bytes at least
public class Match : MonoBehaviour
{
    public Team team1, team2; // about 1130 bytes
    public Manager.MatchType type; // 4 bytes
    public byte[] maps; // 7 bytes
    public List<byte> pickbans; // 7 bytes
    public byte currentMap = 250; // 1 byte
    public Manager manager; // ref
    public int day; // 4 bytes
    public List<Stat> playerStats; // about 400bytes on average
    public List<byte> maproundst1, maproundst2; // rounds won + 2 bytes || 6 bytes || 10 bytes
    public byte winner; // 1 byte
    private int tournament;
    private bool skip = false;
    public int returnValue;

    /*public Match(MatchSetting matchSetting)
    {
        this.team1 = matchSetting.team1;
        this.team2 = matchSetting.team2;
        this.type = matchSetting.type;
    }*/

    public void StartPick(int d, int t)
    {
        matchGoing = true;
        tournament = t;
        day = d;
        currentMap = 250;
        manager = Manager.mainInstance;
        manager.ResetKillFeed();
        maps = new byte[] { 2, 2, 2, 2, 2, 2, 2 };
        pickbans = new List<byte>();
        money = new int[10];
        kills = new int[10];
        deaths = new int[10];
        weapons = new int[10];
        helmet = new int[10];
        vest = new int[10];
        alive = new int[10];
        //type = (Manager.MatchType)i;
    }

    public void BanMap(int i)
    {
        maps[i] = 0;
        pickbans.Add((byte)i);
    }

    public void PickMap(int i)
    {
        maps[i] = 1;
        pickbans.Add((byte)i);
    }

    public byte SimulateMatch(bool skip = false)
    {
        this.skip = skip;
        team1.HasRoles();
        team2.HasRoles();
        if (!skip) Manager.mainInstance.MenuMatch();
        StartCoroutine(FastMatch());
        return 0;
    }

    private byte sitePlant = 1, counterAWP1 = 0, counterAWP2 = 0; // 1 byte
    private short mapAdvantage = 0, power = 0;
    public int followerBoost = 0, playerTeamBuff = 200, opponentTeamBuff = 200;
    [SerializeField] int[] a_playerStats;
    public bool matchGoing = false;
    public byte round1 = 0, round2 = 0, sider = 0;
    public int[] money, kills, deaths, weapons, helmet, vest, alive;
    //sider = 0 => team1 : t && team2 : ct, sider = 1 => team1 : ct && team2 : t
    private IEnumerator MatchPlayback(int[] move)
    {
        for (int i = 0; i < move.Length; i++)
        {
            bool stop = false;
            switch (move[i])
            {
                case 17011: //start
                    matchGoing = true;
                    for (int j = 0; j < 10; j++) alive[j] = 1;
                    sitePlant = (byte)Random.Range(1, 3);
                    manager.RefreshInterface();
                    continue;
                    //break;
                case 17012: //end
                    manager.RefreshInterface();
                    matchGoing = false;
                    stop = true;
                    break;
                case 17201: //team1 wins the round
                    round1++;
                    for (int j = 0; j < 10; j++) alive[j] = 1;
                    sitePlant = (byte)Random.Range(1, 3);
                    manager.ResetKillFeed();
                    manager.RefreshInterface();
                    yield return new WaitForSeconds(Random.Range(0.25f, 0.75f));
                    continue;
                case 17202: //team2 wins the round
                    round2++;
                    for (int j = 0; j < 10; j++) alive[j] = 1;
                    sitePlant = (byte)Random.Range(1, 3);
                    manager.ResetKillFeed();
                    manager.RefreshInterface();
                    yield return new WaitForSeconds(Random.Range(0.25f, 0.75f));
                    continue;
            }
            if (stop) break;
            if (move[i] >= 17100 && move[i] <= 17104) // map 
            {
                for (int j = 0; j < 10; j++)
                {
                    kills[j] = 0;
                    deaths[j] = 0;
                    money[j] = 800;
                    weapons[j] = 0;
                    helmet[j] = 0;
                    vest[j] = 0;
                }
                round1 = 0; round2 = 0;
                manager.ResetKillFeed();
                manager.RefreshInterface();
                yield return new WaitForSeconds(Random.Range(0.25f, 0.75f));
                if (type == Manager.MatchType.BO1) { currentMap = pickbans[6]; Manager.mainInstance.SwitchMap((Manager.Map)pickbans[6]); continue; }
                else if (type == Manager.MatchType.BO3)
                {
                    switch (move[i])
                    {
                        case 17100: //1
                            currentMap = pickbans[2];
                            Manager.mainInstance.SwitchMap((Manager.Map)pickbans[2]);
                            continue;
                        //break;
                        case 17101: //2
                            currentMap = pickbans[3];
                            Manager.mainInstance.SwitchMap((Manager.Map)pickbans[3]);
                            continue;
                        case 17102: //3
                            currentMap = pickbans[6];
                            Manager.mainInstance.SwitchMap((Manager.Map)pickbans[6]);
                            continue;
                    }
                }
                else if (type == Manager.MatchType.BO5)
                {
                    switch (move[i])
                    {
                        case 17100: //1
                            currentMap = pickbans[2];
                            Manager.mainInstance.SwitchMap((Manager.Map)pickbans[2]);
                            continue;
                        case 17101: //2
                            currentMap = pickbans[3];
                            Manager.mainInstance.SwitchMap((Manager.Map)pickbans[3]);
                            continue;
                        case 17102: //3
                            currentMap = pickbans[4];
                            Manager.mainInstance.SwitchMap((Manager.Map)pickbans[4]);
                            continue;
                        case 17103: //4
                            currentMap = pickbans[5];
                            Manager.mainInstance.SwitchMap((Manager.Map)pickbans[5]);
                            continue;
                        case 17104: //5
                            currentMap = pickbans[6];
                            Manager.mainInstance.SwitchMap((Manager.Map)pickbans[6]);
                            continue;
                    }
                }
            }
            string str = move[i].ToString();
            if (str.Length == 7)
            {
                switch (str[0])
                {
                    case '1': //kill
                        byte kill = (byte)(str[1] - 48);
                        byte death = (byte)(str[2] - 48);
                        int weapon = System.Int32.Parse(str.Substring(3, 4));
                        bool all = false;
                        byte x = 0;
                        for (int f = 0; f < 10; f++)
                        {
                            if (alive[f] == 1) x++;
                        }
                        if (x == 10) all = true;
                        if (kill > death)
                        {
                            manager.KillFeed(team2.players[kill - 5].nickname,
                                (Manager.Weapon)weapon, team1.players[death].nickname);
                            manager.AppearFrag(currentMap, sider, sitePlant, (byte)(all ? 1 : 0));
                            deaths[death]++;
                            weapons[death] = 0;
                            alive[death] = 0;
                            kills[kill]++;
                            money[kill] += 300;
                        }
                        else
                        {
                            manager.KillFeed(team1.players[kill].nickname,
                                (Manager.Weapon)weapon, team2.players[death - 5].nickname);
                            manager.AppearFrag(currentMap, (byte)(sider ^ 1), sitePlant, (byte)(all ? 1 : 0));
                            deaths[death]++;
                            weapons[death] = 0;
                            alive[death] = 0;
                            kills[kill]++;
                            money[kill] += 300;
                        }
                        manager.RefreshInterface();
                        yield return new WaitForSeconds(Random.Range(0.25f, 1.5f));
                        continue;
                    case '2': //buy 20 + index + weapon
                        byte indexer = (byte)(str[2] - 48);
                        //byte team = (byte)(str[1] - 48);
                        weapons[indexer] = System.Int32.Parse(str.Substring(3, 4));
                        manager.RefreshInterface();
                        continue;
                    case '3': //3 + index + money
                        indexer = (byte)(str[1] - 48);
                        money[indexer] = System.Int32.Parse(str.Substring(2, 5));
                        manager.RefreshInterface();
                        continue;
                }
            }
            move[i] -= 10000000;
            if ((move[i] & 1) == 1) vest[0] = 1; else vest[0] = 0;
            if ((move[i] & 2) == 2) vest[1] = 1; else vest[1] = 0;
            if ((move[i] & 4) == 4) vest[2] = 1; else vest[2] = 0;
            if ((move[i] & 8) == 8) vest[3] = 1; else vest[3] = 0;
            if ((move[i] & 16) == 16) vest[4] = 1; else vest[4] = 0;
            if ((move[i] & 32) == 32) vest[5] = 1; else vest[5] = 0;
            if ((move[i] & 64) == 64) vest[6] = 1; else vest[6] = 0;
            if ((move[i] & 128) == 128) vest[7] = 1; else vest[7] = 0;
            if ((move[i] & 256) == 256) vest[8] = 1; else vest[8] = 0;
            if ((move[i] & 512) == 512) vest[9] = 1; else vest[9] = 0;
            if ((move[i] & 1024) == 1024) helmet[0] = 1; else helmet[0] = 0;
            if ((move[i] & 2048) == 2048) helmet[1] = 1; else helmet[1] = 0;
            if ((move[i] & 4096) == 4096) helmet[2] = 1; else helmet[2] = 0;
            if ((move[i] & 8192) == 8192) helmet[3] = 1; else helmet[3] = 0;
            if ((move[i] & 16384) == 16384) helmet[4] = 1; else helmet[4] = 0;
            if ((move[i] & 32768) == 32768) helmet[5] = 1; else helmet[5] = 0;
            if ((move[i] & 65536) == 65536) helmet[6] = 1; else helmet[6] = 0;
            if ((move[i] & 131072) == 131072) helmet[7] = 1; else helmet[7] = 0;
            if ((move[i] & 262144) == 262144) helmet[8] = 1; else helmet[8] = 0;
            if ((move[i] & 524288) == 524288) helmet[9] = 1; else helmet[9] = 0;
            manager.RefreshInterface();
        }
    }
    public byte playerTeam = 0;
    private IEnumerator FastMatch()
    {
        matchGoing = true;
        playerStats = new List<Stat>();
        NativeArray<int> playersStat = new NativeArray<int>(1400, Allocator.TempJob);
        NativeArray<int> teamStat = new NativeArray<int>(60, Allocator.TempJob);
        int[] seedi = new int[] { Random.Range(0, int.MaxValue) };
        NativeArray<int> seed = new NativeArray<int>(seedi, Allocator.TempJob);
        NativeArray<int> sitePlant = new NativeArray<int>(1, Allocator.TempJob); // mapAdvantage
        NativeArray<int> power = new NativeArray<int>(1, Allocator.TempJob);
        NativeArray<int> mapAdvantage = new NativeArray<int>(1, Allocator.TempJob);
        seedi[0] = playerTeam;
        NativeArray<int> pTeam = new NativeArray<int>(seedi, Allocator.TempJob);
        NativeArray<byte> awinner = new NativeArray<byte>(1, Allocator.TempJob);
        bool[] vsii = new bool[] { Random.Range(0, 100) < 50 };
        NativeArray<bool> chances = new NativeArray<bool>(vsii, Allocator.TempJob);
        seedi[0] = 0;
        NativeArray<int> followerBoost = new NativeArray<int>(seedi, Allocator.TempJob);
        int aol = Manager.mainInstance.GetMyTeam().GetChemistry() + Manager.mainInstance.GetMyTeam().GetForm() +
            Manager.mainInstance.GetMyTeam().GetPsychology();
        int[] biden = new int[]
        {
            aol + Manager.mainInstance.GetMyTeam().GetMap((Manager.Map)0) * 10,
            aol + Manager.mainInstance.GetMyTeam().GetMap((Manager.Map)1) * 10,
            aol + Manager.mainInstance.GetMyTeam().GetMap((Manager.Map)2) * 10,
            aol + Manager.mainInstance.GetMyTeam().GetMap((Manager.Map)3) * 10,
            aol + Manager.mainInstance.GetMyTeam().GetMap((Manager.Map)4) * 10,
            aol + Manager.mainInstance.GetMyTeam().GetMap((Manager.Map)5) * 10,
            aol + Manager.mainInstance.GetMyTeam().GetMap((Manager.Map)6) * 10,
            Manager.mainInstance.AdaptiveDifficulty(),
            (int)Time.timeAsDouble
        };
        NativeArray<int> difficulty = new NativeArray<int>(biden, Allocator.TempJob);
        byte[] indi = new byte[10];
        for (byte i = 0; i < 5; i++) { indi[i] = team1.players[i].killing; indi[i + 5] = team2.players[i].killing; }
        NativeArray<byte> killing = new NativeArray<byte>(indi, Allocator.TempJob);
        for (byte i = 0; i < 5; i++) { indi[i] = team1.players[i].ct; indi[i + 5] = team2.players[i].ct; }
        NativeArray<byte> ct = new NativeArray<byte>(indi, Allocator.TempJob);
        for (byte i = 0; i < 5; i++) { indi[i] = team1.players[i].t; indi[i + 5] = team2.players[i].t; }
        NativeArray<byte> t = new NativeArray<byte>(indi, Allocator.TempJob);
        NativeArray<byte> weaponOffsets = new NativeArray<byte>(Manager.weaponOffsets.ToArray(), Allocator.TempJob);
        indi = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        NativeArray<byte> maproundst = new NativeArray<byte>(indi, Allocator.TempJob);
        for (byte i = 0; i < 5; i++) { indi[i] = (byte)team1.players[i].activity; indi[i + 5] = (byte)team2.players[i].activity; }
        NativeArray<byte> activity = new NativeArray<byte>(indi, Allocator.TempJob);
        for (byte i = 0; i < 5; i++) { indi[i] = (byte)team1.players[i].language; indi[i + 5] = (byte)team2.players[i].language; }
        NativeArray<byte> languages = new NativeArray<byte>(indi, Allocator.TempJob);
        for (byte i = 0; i < 5; i++) { indi[i] = team1.players[i].awp; indi[i + 5] = team2.players[i].awp; }
        NativeArray<byte> awp = new NativeArray<byte>(indi, Allocator.TempJob);
        for (byte i = 0; i < 5; i++) { indi[i] = (byte)team1.players[i].role; indi[i + 5] = (byte)team2.players[i].role; }
        NativeArray<byte> roles = new NativeArray<byte>(indi, Allocator.TempJob);
        for (byte i = 0; i < 5; i++) { indi[i] = team1.players[i].rifle; indi[i + 5] = team2.players[i].rifle; }
        NativeArray<byte> rifle = new NativeArray<byte>(indi, Allocator.TempJob);
        for (byte i = 0; i < 5; i++) { indi[i] = team1.players[i].clutching; indi[i + 5] = team2.players[i].clutching; }
        NativeArray<byte> clutching = new NativeArray<byte>(indi, Allocator.TempJob);
        for (byte i = 0; i < 5; i++) { indi[i] = team1.players[i].entring; indi[i + 5] = team2.players[i].entring; }
        NativeArray<byte> entring = new NativeArray<byte>(indi, Allocator.TempJob);
        pickbans.Add((byte)type);
        NativeArray<byte> picks = new NativeArray<byte>(pickbans.ToArray(), Allocator.TempJob);
        NativeArray<byte> arrrands = new NativeArray<byte>(Manager.arrOfRands, Allocator.TempJob);
        bool[] bob = new bool[]
        {
            team1.entry,
            team1.igl,
            team1.sniper,
            team1.support,
            team1.lurk,
            team2.entry,
            team2.igl,
            team2.sniper,
            team2.support,
            team2.lurk,
        };
        NativeArray<bool> panini = new NativeArray<bool>(bob, Allocator.TempJob);
        NativeArray<bool> dafsfas = new NativeArray<bool>(new bool[] { !skip }, Allocator.TempJob);
        NativeArray<int> kone = new NativeArray<int>(121800, Allocator.TempJob);
        MatchJob matchJob = new MatchJob
        {
            seed = seed,
            playersStat = playersStat,
            sitePlant = sitePlant,
            teamStat = teamStat,
            chances = chances,
            followerBoost = followerBoost,
            difficulty = difficulty,
            killing = killing,
            t = t,
            ct = ct,
            weaponOffsets = weaponOffsets,
            maproundst = maproundst,
            activity = activity,
            languages = languages,
            awp = awp,
            roles = roles,
            rifle = rifle,
            clutching = clutching,
            entring = entring,
            winner = awinner,
            pickbans = picks,
            power = power,
            playerTeam = pTeam,
            mapAdvantage = mapAdvantage,
            hasRole = panini,
            saveDemo = dafsfas,
            logs = kone,
            arrrands = arrrands
        };
        JobHandle jobHandle = matchJob.Schedule(Manager.trackHandle);
        jobHandle.Complete();
        int[] logs = kone.ToArray();
        arrrands.Dispose();
        kone.Dispose();
        dafsfas.Dispose();
        seed.Dispose();
        a_playerStats = new int[1400];
        playersStat.CopyTo(a_playerStats);
        playersStat.Dispose();
        byte n = 0;
        for (; n < 5; n++)
        {
            if (maproundst[n] == 0 && maproundst[n + 5] == 0) break;
            for (byte i = 0; i < 10; i++) playerStats.Add(new Stat());
            for (byte i = 0; i < 5; i++)
            {
                int[] vs1 = new int[28];
                System.Array.Copy(a_playerStats, n * 280 + i * 28, vs1, 0, 28);
                int[] vs2 = new int[28];
                System.Array.Copy(a_playerStats, n * 280 + (i + 5) * 28, vs2, 0, 28);
                playerStats[i + n * 10].SetStats(team1.players[i].nickname, vs1,
                    (short)(maproundst[n] + maproundst[n + 5]), (short)day);
                playerStats[i + n * 10].CalculateRating();
                if (!(day == 0 || tournament == -1))
                {
                    team1.players[i].Progress(playerStats[i + n * 10]);
                    team1.players[i].playerStats.Add(playerStats[i + n * 10]);
                }
                playerStats[i + 5 + n * 10].SetStats(team2.players[i].nickname, vs2,
                    (short)(maproundst[n] + maproundst[n + 5]), (short)day);
                playerStats[i + 5 + n * 10].CalculateRating();
                if (!(day == 0 || tournament == -1))
                {
                    team2.players[i].playerStats.Add(playerStats[i + 5 + n * 10]);
                    team2.players[i].Progress(playerStats[i + 5 + n * 10]);
                }
            }
            if (n == 4) break;
        }
        sitePlant.Dispose();
        int[] a_teamStats = new int[60];
        teamStat.CopyTo(a_teamStats);
        teamStat.Dispose();
        chances.Dispose();
        followerBoost.Dispose();
        difficulty.Dispose();
        killing.Dispose();
        ct.Dispose();
        t.Dispose();
        entring.Dispose();
        weaponOffsets.Dispose();
        activity.Dispose();
        languages.Dispose();
        clutching.Dispose();
        awp.Dispose();
        rifle.Dispose();
        roles.Dispose();
        byte[] a_maproundst = new byte[10];
        maproundst.CopyTo(a_maproundst);
        maproundst.Dispose();
        byte a_winner = awinner[0];
        awinner.Dispose();
        picks.Dispose();
        power.Dispose();
        pTeam.Dispose();
        mapAdvantage.Dispose();
        panini.Dispose();
        Manager.trackHandle = jobHandle;
        int r = MatchSave.Save(this, a_maproundst);
        returnValue = r;
        yield return new WaitWhile(() => MatchSave.isLoading);
        team1.playedToday = 1;
        team2.playedToday = 1;
        byte ui1 = 0, ui2 = 0;
        for (byte i = 0; i < 5; i++)
        {
            if (a_maproundst[i] > a_maproundst[i + 5]) ui1++;
            else if (a_maproundst[i] < a_maproundst[i + 5]) ui2++;
            else break;
        }
        //PlayerPrefs.Save();
        if (!(day == 0 || tournament == -1))
        {
            for (byte i = 0; i < 5; i++) { team1.players[i].stats.Add(r); team2.players[i].stats.Add(r); }
            if (ui1 < ui2)
            {
                winner = (byte)(n + 5);
                //team1.taskSponsor.thrownMatches++;
                //team1.taskSponsor.playedMatches++;
                //team2.taskSponsor.playedMatches++;
                //team2.taskSponsor.wonMatches++;
                int g = Random.Range(1000, 5001);
                team2.followers += g;
                team1.followers -= g;
                if (team1.followers < 1000) team1.followers = 1000;
                for (byte i = 0; i < 5; i++) { team1.players[i].Lose(); team2.players[i].Win(Events.events.GetTournaments()[tournament].tier); }
            }
            else
            {
                winner = (byte)(n);
                //team2.taskSponsor.thrownMatches++;
                //team2.taskSponsor.playedMatches++;
                //team1.taskSponsor.playedMatches++;
                //team1.taskSponsor.wonMatches++;
                int g = Random.Range(1000, 5001);
                team2.followers -= g;
                if (team2.followers < 1000) team2.followers = 1000;
                team1.followers += g;
                for (byte i = 0; i < 5; i++) { team2.players[i].Lose(); team1.players[i].Win(Events.events.GetTournaments()[tournament].tier); }
            }
            if (playerTeam == 1)
            {
                if (ui2 > ui1)
                {
                    team1.WorsenChemistry(); team1.WorsenPsychology();
                    Manager.mainInstance.AdaptiveDifficultyChange(0);
                }
                else Manager.mainInstance.AdaptiveDifficultyChange(1);
            }
            else if (playerTeam == 2)
            {
                if (ui1 > ui2)
                {
                    team2.WorsenChemistry(); team2.WorsenPsychology();
                    Manager.mainInstance.AdaptiveDifficultyChange(0);
                }
                else Manager.mainInstance.AdaptiveDifficultyChange(1);
            }
            Events.events.GetTournaments()[tournament].SendResult(day, winner, a_maproundst, pickbans, team1, team2, type);
        }
        if (!skip) StartCoroutine(MatchPlayback(logs));
        else matchGoing = false;
        yield return null;
    }
}

// don't know if anyone besides me can understand what's going on here 
[BurstCompile(FloatPrecision = FloatPrecision.High, FloatMode = FloatMode.Default, OptimizeFor = OptimizeFor.Performance)]
public struct MatchJob : IJob
{
    public NativeArray<int> playersStat; // 28 per player * 10 * 5 maps = 1400 alloc (map * 280 + player * 28 + stat)
    public NativeArray<int> teamStat; // 3 per team * 2 * 5 maps = 30 alloc
    [ReadOnly] public NativeArray<byte> languages; // 5-9 team2 0-4 team1
    [ReadOnly] public NativeArray<byte> roles; //
    [ReadOnly] public NativeArray<byte> activity, weaponOffsets; //
    public NativeArray<byte> winner;
    [ReadOnly] public NativeArray<int> seed;
    [ReadOnly] public NativeArray<bool> hasRole; // 0 - 4, 5 - 9
    public NativeArray<bool> chances, saveDemo; // 5 for maps
    public NativeArray<byte> maproundst; // 0 - 4, 5 - 9 = team1, team2
    [ReadOnly] public NativeArray<byte> pickbans; // 0 - 6, 7 format
    [ReadOnly] public NativeArray<int> difficulty; // 0 - 6 map + strength, 7 difficulty
    [ReadOnly] public NativeArray<byte> t, ct, killing, entring, awp, rifle, clutching;
    public NativeArray<int> logs;
    public NativeArray<int> followerBoost, mapAdvantage, power, sitePlant, playerTeam;
    public NativeArray<byte> arrrands;
    public void Execute()
    {
        NativeArray<byte> counterAWP = new NativeArray<byte>(2, Allocator.Temp);
        NativeArray<int> playerTeamBuff = new NativeArray<int>(1, Allocator.Temp),
                       opponentTeamBuff = new NativeArray<int>(1, Allocator.Temp),
                       iter = new NativeArray<int>(1, Allocator.Temp);
        NativeArray<uint> counterTo = new NativeArray<uint>(1, Allocator.Temp);
        counterTo[0] = 0;
        counterAWP[0] = 0; counterAWP[1] = 0;
        iter[0] = 0;
        logs[iter[0]++] = 17011;
        //matchGoing = true;
        for (byte i = 0; i < 5; i++)
        {
            for (byte j = 0; j < 5; j++)
            {
                if (j != i)
                {
                    if (languages[i] == languages[j]) counterAWP[1]++;
                    if (languages[i + 5] == languages[j + 5]) counterAWP[0]++;
                }
            }
        }
        NativeArray<short> arrByte = new NativeArray<short>(6, Allocator.Temp);
        NativeArray<bool> arrBool = new NativeArray<bool>(1, Allocator.Temp);
        arrBool[0] = false; // demand ROUND END
        arrByte[0] = -1; // r = -1;
        arrByte[1] = 0; arrByte[2] = 0; // ui1, ui2
        arrByte[3] = 0; // n 
        arrByte[4] = 250; // current map
        //playerStats = new List<Stat>();
        //maproundst1 = new List<byte>((int)type * 2 + 1); maproundst2 = new List<byte>((int)type * 2 + 1);
        NativeArray<int> vs = new NativeArray<int>(27, Allocator.Temp);
        NativeArray<byte> it = new NativeArray<byte>(2, Allocator.Temp);
        NativeArray<int> randit = new NativeArray<int>(2, Allocator.Temp);
        for (; arrByte[3] < 5; arrByte[3]++)
        {
            if (saveDemo[0]) logs[iter[0]++] = 17100 + arrByte[3];
            power[0] = 0;
            if (pickbans[7] == 0) arrByte[4] = pickbans[6];
            if (pickbans[7] == 1)
            {
                if (arrByte[4] == 250) { arrByte[4] = pickbans[2]; mapAdvantage[0] = 25; }
                else if (arrByte[4] == pickbans[2]) { arrByte[4] = pickbans[3]; mapAdvantage[0] = -25; }
                else if (arrByte[4] == pickbans[3]) { arrByte[4] = pickbans[6]; mapAdvantage[0] = 0; }
            }
            if (pickbans[7] == 2)
            {
                if (arrByte[4] == 250) { arrByte[4] = pickbans[2]; mapAdvantage[0] = 50; }
                else if (arrByte[4] == pickbans[2]) { arrByte[4] = pickbans[3]; mapAdvantage[0] = -25; }
                else if (arrByte[4] == pickbans[3]) { arrByte[4] = pickbans[4]; mapAdvantage[0] = 25; }
                else if (arrByte[4] == pickbans[4]) { arrByte[4] = pickbans[5]; mapAdvantage[0] = -25; }
                else if (arrByte[4] == pickbans[5]) { arrByte[4] = pickbans[6]; mapAdvantage[0] = 0; }
            }
            // 17, mapCounter (1-5), 0, which map (0-6)
            //logs[iter[0]++] = 17000 + arrByte[4] + (arrByte[3] + 1) * 100;
            playerTeamBuff[0] = difficulty[arrByte[4]];
            opponentTeamBuff[0] = difficulty[7];
            //player
            //0 money, 1 kills, 2 deaths, 3 assists, 4 entries, 5 x1, 6 x2, 7 x3, 8 x4, 9 x5, 10 1v1, 11 1v2
            //12 1v3, 13 1v4, 14 1v5, 15 kast, 16 weapon, 
            //roundStat: 17 kills, 18 deaths, 19 probable clutch(values 1-5), 20 total damage, 
            //21 henade, 22 smoke, 23 flash, 24 molotov, 25 kevlar, 26 helmet, 27 round damage
            //team
            //0 lossbonus, 1 rounds-won, 2 side
            teamStat[0] = 1;
            teamStat[3] = 1; // 0 + 3
            teamStat[1] = 0;
            teamStat[4] = 0;
            //bool k = random.NextInt(0, 100) < 50 ? true : false;
            //t = 0, ct = 1
            if (chances[0]) { teamStat[2] = 0; teamStat[5] = 1; }
            if (!chances[0]) { teamStat[2] = 1; teamStat[5] = 0; }
            if (chances[0]) chances[0] = false;
            else chances[0] = true;
            Unity.Mathematics.Random random = new Unity.Mathematics.Random((uint)seed[0] + (uint)(arrByte[3] * 200));
            random.NextInt();
            for (byte i = 0; i < 5; i++)
            {
                //team1.players[i].mapStat = new int[28];
                playersStat[arrByte[3] * 280 + (i) * 28 + 0] = 800;
                //team2.players[i].mapStat = new int[28];
                playersStat[arrByte[3] * 280 + (i + 5) * 28 + 0] = 800;
                for (byte j = 1; j < 28; j++)
                { playersStat[arrByte[3] * 280 + (i) * 28 + j] = 0; playersStat[arrByte[3] * 280 + (i + 5) * 28 + j] = 0; }
            }
            for (byte i = 1; i <= 15; i++)
            {
                SimulateBuy(i, arrByte[3], random);
                if (saveDemo[0])
                {
                    // 20 player 0-9 weapon 0000-5000
                    logs[iter[0]++] = 2000000 + playersStat[arrByte[3] * 280 + 16];
                    logs[iter[0]++] = 2010000 + playersStat[arrByte[3] * 280 + 1 * 28 + 16];
                    logs[iter[0]++] = 2020000 + playersStat[arrByte[3] * 280 + 2 * 28 + 16];
                    logs[iter[0]++] = 2030000 + playersStat[arrByte[3] * 280 + 3 * 28 + 16];
                    logs[iter[0]++] = 2040000 + playersStat[arrByte[3] * 280 + 4 * 28 + 16];
                    logs[iter[0]++] = 2050000 + playersStat[arrByte[3] * 280 + 5 * 28 + 16];
                    logs[iter[0]++] = 2060000 + playersStat[arrByte[3] * 280 + 6 * 28 + 16];
                    logs[iter[0]++] = 2070000 + playersStat[arrByte[3] * 280 + 7 * 28 + 16];
                    logs[iter[0]++] = 2080000 + playersStat[arrByte[3] * 280 + 8 * 28 + 16];
                    logs[iter[0]++] = 2090000 + playersStat[arrByte[3] * 280 + 9 * 28 + 16];
                    // 3 player 0-9 money 00000-16000
                    logs[iter[0]++] = 3000000 + playersStat[arrByte[3] * 280];
                    logs[iter[0]++] = 3100000 + playersStat[arrByte[3] * 280 + 1 * 28];
                    logs[iter[0]++] = 3200000 + playersStat[arrByte[3] * 280 + 2 * 28];
                    logs[iter[0]++] = 3300000 + playersStat[arrByte[3] * 280 + 3 * 28];
                    logs[iter[0]++] = 3400000 + playersStat[arrByte[3] * 280 + 4 * 28];
                    logs[iter[0]++] = 3500000 + playersStat[arrByte[3] * 280 + 5 * 28];
                    logs[iter[0]++] = 3600000 + playersStat[arrByte[3] * 280 + 6 * 28];
                    logs[iter[0]++] = 3700000 + playersStat[arrByte[3] * 280 + 7 * 28];
                    logs[iter[0]++] = 3800000 + playersStat[arrByte[3] * 280 + 8 * 28];
                    logs[iter[0]++] = 3900000 + playersStat[arrByte[3] * 280 + 9 * 28];
                    // vest and helmet
                    logs[iter[0]] += 1 * playersStat[arrByte[3] * 280 + 25];
                    logs[iter[0]] += 2 * playersStat[arrByte[3] * 280 + 1 * 28 + 25];
                    logs[iter[0]] += 4 * playersStat[arrByte[3] * 280 + 2 * 28 + 25];
                    logs[iter[0]] += 8 * playersStat[arrByte[3] * 280 + 3 * 28 + 25];
                    logs[iter[0]] += 16 * playersStat[arrByte[3] * 280 + 4 * 28 + 25];
                    logs[iter[0]] += 32 * playersStat[arrByte[3] * 280 + 5 * 28 + 25];
                    logs[iter[0]] += 64 * playersStat[arrByte[3] * 280 + 6 * 28 + 25];
                    logs[iter[0]] += 128 * playersStat[arrByte[3] * 280 + 7 * 28 + 25];
                    logs[iter[0]] += 256 * playersStat[arrByte[3] * 280 + 8 * 28 + 25];
                    logs[iter[0]] += 512 * playersStat[arrByte[3] * 280 + 9 * 28 + 25];
                    logs[iter[0]] += 1024 * playersStat[arrByte[3] * 280 + 26];
                    logs[iter[0]] += 2048 * playersStat[arrByte[3] * 280 + 1 * 28 + 26];
                    logs[iter[0]] += 4096 * playersStat[arrByte[3] * 280 + 2 * 28 + 26];
                    logs[iter[0]] += 8192 * playersStat[arrByte[3] * 280 + 3 * 28 + 26];
                    logs[iter[0]] += 16384 * playersStat[arrByte[3] * 280 + 4 * 28 + 26];
                    logs[iter[0]] += 32768 * playersStat[arrByte[3] * 280 + 5 * 28 + 26];
                    logs[iter[0]] += 65536 * playersStat[arrByte[3] * 280 + 6 * 28 + 26];
                    logs[iter[0]] += 131072 * playersStat[arrByte[3] * 280 + 7 * 28 + 26];
                    logs[iter[0]] += 262144 * playersStat[arrByte[3] * 280 + 8 * 28 + 26];
                    logs[iter[0]] += 524288 * playersStat[arrByte[3] * 280 + 9 * 28 + 26];
                    logs[iter[0]++] += 10000000;
                }
                sitePlant[0] = (byte)random.NextInt(1, 3);
                followerBoost[0] += random.NextInt(1000, 6001);
                SimulateRound(arrBool[0], ref vs, ref random, opponentTeamBuff[0], playerTeamBuff[0], (byte)arrByte[3],
                    ref randit, ref it, ref counterAWP, ref iter, i, ref counterTo);
                //yield return new WaitUntil(() => !roundGoing);
                //xz
                for (byte j = 0; j < 5; j++)
                {
                    //kills
                    playersStat[arrByte[3] * 280 + (j) * 28 + 1] += playersStat[arrByte[3] * 280 + (j) * 28 + 17];
                    playersStat[arrByte[3] * 280 + (j + 5) * 28 + 1] += playersStat[arrByte[3] * 280 + (j + 5) * 28 + 17];
                    //deaths
                    playersStat[arrByte[3] * 280 + (j) * 28 + 2] += playersStat[arrByte[3] * 280 + (j) * 28 + 18];
                    playersStat[arrByte[3] * 280 + (j + 5) * 28 + 2] += playersStat[arrByte[3] * 280 + (j + 5) * 28 + 18];
                    //assists
                    if (playersStat[arrByte[3] * 280 + (j) * 28 + 27] > 40) playersStat[arrByte[3] * 280 + (j) * 28 + 3] += 1;
                    if (playersStat[arrByte[3] * 280 + (j + 5) * 28 + 27] > 40) playersStat[arrByte[3] * 280 + (j + 5) * 28 + 3] += 1;
                    //multikills
                    if (playersStat[arrByte[3] * 280 + (j) * 28 + 17] != 0)
                        playersStat[arrByte[3] * 280 + (j) * 28 + 4 + playersStat[arrByte[3] * 280 + (j) * 28 + 17]]++;
                    if (playersStat[arrByte[3] * 280 + (j + 5) * 28 + 17] != 0)
                        playersStat[arrByte[3] * 280 + (j + 5) * 28 + 4 + playersStat[arrByte[3] * 280 + (j + 5) * 28 + 17]]++;
                    //clutches + entries precounted
                    //kast
                    if (playersStat[arrByte[3] * 280 + (j) * 28 + 17] > 0 ||
                        playersStat[arrByte[3] * 280 + (j) * 28 + 18] == 0 ||
                        playersStat[arrByte[3] * 280 + (j) * 28 + 27] > 40 ||
                        random.NextInt(0, 100) < 10 /*traded*/) playersStat[arrByte[3] * 280 + (j) * 28 + 15]++;
                    if (playersStat[arrByte[3] * 280 + (j + 5) * 28 + 17] > 0 ||
                        playersStat[arrByte[3] * 280 + (j + 5) * 28 + 18] == 0 ||
                        playersStat[arrByte[3] * 280 + (j + 5) * 28 + 27] > 40 ||
                        random.NextInt(0, 100) < 10 /*traded*/) playersStat[arrByte[3] * 280 + (j + 5) * 28 + 15]++;
                    //damage
                    playersStat[arrByte[3] * 280 + (j + 5) * 28 + 20] += playersStat[arrByte[3] * 280 + (j + 5) * 28 + 27];
                    playersStat[arrByte[3] * 280 + (j) * 28 + 20] += playersStat[arrByte[3] * 280 + (j) * 28 + 27];
                    //round reset right?
                    playersStat[arrByte[3] * 280 + (j + 5) * 28 + 17] = 0;
                    playersStat[arrByte[3] * 280 + (j + 5) * 28 + 18] = 0;
                    playersStat[arrByte[3] * 280 + (j + 5) * 28 + 19] = 0;
                    playersStat[arrByte[3] * 280 + (j + 5) * 28 + 27] = 0;
                    playersStat[arrByte[3] * 280 + (j) * 28 + 17] = 0;
                    playersStat[arrByte[3] * 280 + (j) * 28 + 18] = 0;
                    playersStat[arrByte[3] * 280 + (j) * 28 + 19] = 0;
                    playersStat[arrByte[3] * 280 + (j) * 28 + 27] = 0;
                }
            }
            //2nd half
            teamStat[0] = 1;
            teamStat[3] = 1;
            if (chances[0]) { teamStat[2] = 0; teamStat[5] = 1; }
            if (!chances[0]) { teamStat[2] = 1; teamStat[5] = 0; }
            if (chances[0]) chances[0] = false;
            else chances[0] = true;
            //Unity.Mathematics.Random random = new Unity.Mathematics.Random((uint)seed[0]);
            for (byte i = 0; i < 5; i++)
            {
                //team1.players[i].mapStat = new int[28];
                playersStat[arrByte[3] * 280 + (i) * 28 + 0] = 800;
                //team2.players[i].mapStat = new int[28];
                playersStat[arrByte[3] * 280 + (i + 5) * 28 + 0] = 800;
                for (byte j = 16; j < 20; j++)
                { playersStat[arrByte[3] * 280 + (i) * 28 + j] = 0; playersStat[arrByte[3] * 280 + (i + 5) * 28 + j] = 0; }
                for (byte j = 21; j < 28; j++)
                { playersStat[arrByte[3] * 280 + (i) * 28 + j] = 0; playersStat[arrByte[3] * 280 + (i + 5) * 28 + j] = 0; }
            }
            for (byte i = 1; i <= 15; i++)
            {
                SimulateBuy(i, arrByte[3], random);
                if (saveDemo[0])
                {
                    // 20 player 0-9 weapon 0000-5000
                    logs[iter[0]++] = 2000000 + playersStat[arrByte[3] * 280 + 16];
                    logs[iter[0]++] = 2010000 + playersStat[arrByte[3] * 280 + 1 * 28 + 16];
                    logs[iter[0]++] = 2020000 + playersStat[arrByte[3] * 280 + 2 * 28 + 16];
                    logs[iter[0]++] = 2030000 + playersStat[arrByte[3] * 280 + 3 * 28 + 16];
                    logs[iter[0]++] = 2040000 + playersStat[arrByte[3] * 280 + 4 * 28 + 16];
                    logs[iter[0]++] = 2050000 + playersStat[arrByte[3] * 280 + 5 * 28 + 16];
                    logs[iter[0]++] = 2060000 + playersStat[arrByte[3] * 280 + 6 * 28 + 16];
                    logs[iter[0]++] = 2070000 + playersStat[arrByte[3] * 280 + 7 * 28 + 16];
                    logs[iter[0]++] = 2080000 + playersStat[arrByte[3] * 280 + 8 * 28 + 16];
                    logs[iter[0]++] = 2090000 + playersStat[arrByte[3] * 280 + 9 * 28 + 16];
                    // 3 player 0-9 money 00000-16000
                    logs[iter[0]++] = 3000000 + playersStat[arrByte[3] * 280];
                    logs[iter[0]++] = 3100000 + playersStat[arrByte[3] * 280 + 1 * 28];
                    logs[iter[0]++] = 3200000 + playersStat[arrByte[3] * 280 + 2 * 28];
                    logs[iter[0]++] = 3300000 + playersStat[arrByte[3] * 280 + 3 * 28];
                    logs[iter[0]++] = 3400000 + playersStat[arrByte[3] * 280 + 4 * 28];
                    logs[iter[0]++] = 3500000 + playersStat[arrByte[3] * 280 + 5 * 28];
                    logs[iter[0]++] = 3600000 + playersStat[arrByte[3] * 280 + 6 * 28];
                    logs[iter[0]++] = 3700000 + playersStat[arrByte[3] * 280 + 7 * 28];
                    logs[iter[0]++] = 3800000 + playersStat[arrByte[3] * 280 + 8 * 28];
                    logs[iter[0]++] = 3900000 + playersStat[arrByte[3] * 280 + 9 * 28];
                    // vest and helmet
                    logs[iter[0]] += 1 * playersStat[arrByte[3] * 280 + 25];
                    logs[iter[0]] += 2 * playersStat[arrByte[3] * 280 + 1 * 28 + 25];
                    logs[iter[0]] += 4 * playersStat[arrByte[3] * 280 + 2 * 28 + 25];
                    logs[iter[0]] += 8 * playersStat[arrByte[3] * 280 + 3 * 28 + 25];
                    logs[iter[0]] += 16 * playersStat[arrByte[3] * 280 + 4 * 28 + 25];
                    logs[iter[0]] += 32 * playersStat[arrByte[3] * 280 + 5 * 28 + 25];
                    logs[iter[0]] += 64 * playersStat[arrByte[3] * 280 + 6 * 28 + 25];
                    logs[iter[0]] += 128 * playersStat[arrByte[3] * 280 + 7 * 28 + 25];
                    logs[iter[0]] += 256 * playersStat[arrByte[3] * 280 + 8 * 28 + 25];
                    logs[iter[0]] += 512 * playersStat[arrByte[3] * 280 + 9 * 28 + 25];
                    logs[iter[0]] += 1024 * playersStat[arrByte[3] * 280 + 26];
                    logs[iter[0]] += 2048 * playersStat[arrByte[3] * 280 + 1 * 28 + 26];
                    logs[iter[0]] += 4096 * playersStat[arrByte[3] * 280 + 2 * 28 + 26];
                    logs[iter[0]] += 8192 * playersStat[arrByte[3] * 280 + 3 * 28 + 26];
                    logs[iter[0]] += 16384 * playersStat[arrByte[3] * 280 + 4 * 28 + 26];
                    logs[iter[0]] += 32768 * playersStat[arrByte[3] * 280 + 5 * 28 + 26];
                    logs[iter[0]] += 65536 * playersStat[arrByte[3] * 280 + 6 * 28 + 26];
                    logs[iter[0]] += 131072 * playersStat[arrByte[3] * 280 + 7 * 28 + 26];
                    logs[iter[0]] += 262144 * playersStat[arrByte[3] * 280 + 8 * 28 + 26];
                    logs[iter[0]] += 524288 * playersStat[arrByte[3] * 280 + 9 * 28 + 26];
                    logs[iter[0]++] += 10000000;
                }
                sitePlant[0] = (byte)random.NextInt(1, 3);
                followerBoost[0] += random.NextInt(1000, 6001);
                SimulateRound(arrBool[0], ref vs, ref random, opponentTeamBuff[0], playerTeamBuff[0], (byte)arrByte[3],
                    ref randit, ref it, ref counterAWP, ref iter, i + 15, ref counterTo);
                //
                //xz
                for (byte j = 0; j < 5; j++)
                {
                    //kills
                    playersStat[arrByte[3] * 280 + (j) * 28 +1] += playersStat[arrByte[3] * 280 + (j) * 28 +17];
                    playersStat[arrByte[3] * 280 + (j + 5) * 28 + 1] += playersStat[arrByte[3] * 280 + (j + 5) * 28 + 17];
                    //deaths
                    playersStat[arrByte[3] * 280 + (j) * 28 +2] += playersStat[arrByte[3] * 280 + (j) * 28 +18];
                    playersStat[arrByte[3] * 280 + (j + 5) * 28 + 2] += playersStat[arrByte[3] * 280 + (j + 5) * 28 + 18];
                    //assists
                    if (playersStat[arrByte[3] * 280 + (j) * 28 +27] > 40) playersStat[arrByte[3] * 280 + (j) * 28 +3] += 1;
                    if (playersStat[arrByte[3] * 280 + (j + 5) * 28 + 27] > 40) playersStat[arrByte[3] * 280 + (j + 5) * 28 + 3] += 1;
                    //multikills
                    if (playersStat[arrByte[3] * 280 + (j) * 28 +17] != 0)
                        playersStat[arrByte[3] * 280 + (j) * 28 +4 + playersStat[arrByte[3] * 280 + (j) * 28 +17]]++;
                    if (playersStat[arrByte[3] * 280 + (j + 5) * 28 + 17] != 0)
                        playersStat[arrByte[3] * 280 + (j + 5) * 28 + 4 + playersStat[arrByte[3] * 280 + (j + 5) * 28 + 17]]++;
                    //clutches + entries precounted
                    //kast
                    if (playersStat[arrByte[3] * 280 + (j) * 28 +17] > 0 ||
                        playersStat[arrByte[3] * 280 + (j) * 28 +18] == 0 ||
                        playersStat[arrByte[3] * 280 + (j) * 28 +27] > 40 ||
                        random.NextInt(0, 100) < 10 /*traded*/) playersStat[arrByte[3] * 280 + (j) * 28 +15]++;
                    if (playersStat[arrByte[3] * 280 + (j + 5) * 28 + 17] > 0 ||
                        playersStat[arrByte[3] * 280 + (j + 5) * 28 + 18] == 0 ||
                        playersStat[arrByte[3] * 280 + (j + 5) * 28 + 27] > 40 ||
                        random.NextInt(0, 100) < 10 /*traded*/) playersStat[arrByte[3] * 280 + (j + 5) * 28 + 15]++;
                    //damage
                    playersStat[arrByte[3] * 280 + (j + 5) * 28 + 20] += playersStat[arrByte[3] * 280 + (j + 5) * 28 + 27];
                    playersStat[arrByte[3] * 280 + (j) * 28 +20] += playersStat[arrByte[3] * 280 + (j) * 28 +27];
                    //round reset right?
                    playersStat[arrByte[3] * 280 + (j + 5) * 28 + 17] = 0;
                    playersStat[arrByte[3] * 280 + (j + 5) * 28 + 18] = 0;
                    playersStat[arrByte[3] * 280 + (j + 5) * 28 + 19] = 0;
                    playersStat[arrByte[3] * 280 + (j + 5) * 28 + 27] = 0;
                    playersStat[arrByte[3] * 280 + (j) * 28 +17] = 0;
                    playersStat[arrByte[3] * 280 + (j) * 28 +18] = 0;
                    playersStat[arrByte[3] * 280 + (j) * 28 +19] = 0;
                    playersStat[arrByte[3] * 280 + (j) * 28 +27] = 0;
                }
                if (teamStat[1] == 16 || teamStat[4] == 16) break;
            }
            if (!(teamStat[1] == 16 || teamStat[4] == 16))
            {
                for (byte p = 0; p < 32; p++)
                {
                    NativeArray<bool> br = new NativeArray<bool>(1, Allocator.Temp);
                    br[0] = false;
                    teamStat[0] = 1;
                    teamStat[3] = 1;
                    //Unity.Mathematics.Random random = new Unity.Mathematics.Random((uint)seed[0]);
                    for (byte i = 0; i < 5; i++)
                    {
                        //team1.players[i].mapStat = new int[28];
                        playersStat[arrByte[3] * 280 + (i) * 28 + 0] = 16000;
                        //team2.players[i].mapStat = new int[28];
                        playersStat[arrByte[3] * 280 + (i + 5) * 28 + 0] = 16000;
                        for (byte j = 16; j < 20; j++)
                        { playersStat[arrByte[3] * 280 + (i) * 28 + j] = 0; playersStat[arrByte[3] * 280 + (i + 5) * 28 + j] = 0; }
                        for (byte j = 21; j < 28; j++)
                        { playersStat[arrByte[3] * 280 + (i) * 28 + j] = 0; playersStat[arrByte[3] * 280 + (i + 5) * 28 + j] = 0; }
                    }
                    for (byte i = 1; i <= 3; i++)
                    {
                        SimulateBuy(i, arrByte[3], random);
                        if (saveDemo[0])
                        {
                            // 20 player 0-9 weapon 0000-5000
                            logs[iter[0]++] = 2000000 + playersStat[arrByte[3] * 280 + 16];
                            logs[iter[0]++] = 2010000 + playersStat[arrByte[3] * 280 + 1 * 28 + 16];
                            logs[iter[0]++] = 2020000 + playersStat[arrByte[3] * 280 + 2 * 28 + 16];
                            logs[iter[0]++] = 2030000 + playersStat[arrByte[3] * 280 + 3 * 28 + 16];
                            logs[iter[0]++] = 2040000 + playersStat[arrByte[3] * 280 + 4 * 28 + 16];
                            logs[iter[0]++] = 2050000 + playersStat[arrByte[3] * 280 + 5 * 28 + 16];
                            logs[iter[0]++] = 2060000 + playersStat[arrByte[3] * 280 + 6 * 28 + 16];
                            logs[iter[0]++] = 2070000 + playersStat[arrByte[3] * 280 + 7 * 28 + 16];
                            logs[iter[0]++] = 2080000 + playersStat[arrByte[3] * 280 + 8 * 28 + 16];
                            logs[iter[0]++] = 2090000 + playersStat[arrByte[3] * 280 + 9 * 28 + 16];
                            // 3 player 0-9 money 00000-16000
                            logs[iter[0]++] = 3000000 + playersStat[arrByte[3] * 280];
                            logs[iter[0]++] = 3100000 + playersStat[arrByte[3] * 280 + 1 * 28];
                            logs[iter[0]++] = 3200000 + playersStat[arrByte[3] * 280 + 2 * 28];
                            logs[iter[0]++] = 3300000 + playersStat[arrByte[3] * 280 + 3 * 28];
                            logs[iter[0]++] = 3400000 + playersStat[arrByte[3] * 280 + 4 * 28];
                            logs[iter[0]++] = 3500000 + playersStat[arrByte[3] * 280 + 5 * 28];
                            logs[iter[0]++] = 3600000 + playersStat[arrByte[3] * 280 + 6 * 28];
                            logs[iter[0]++] = 3700000 + playersStat[arrByte[3] * 280 + 7 * 28];
                            logs[iter[0]++] = 3800000 + playersStat[arrByte[3] * 280 + 8 * 28];
                            logs[iter[0]++] = 3900000 + playersStat[arrByte[3] * 280 + 9 * 28];
                            // vest and helmet
                            logs[iter[0]] += 1 * playersStat[arrByte[3] * 280 + 25];
                            logs[iter[0]] += 2 * playersStat[arrByte[3] * 280 + 1 * 28 + 25];
                            logs[iter[0]] += 4 * playersStat[arrByte[3] * 280 + 2 * 28 + 25];
                            logs[iter[0]] += 8 * playersStat[arrByte[3] * 280 + 3 * 28 + 25];
                            logs[iter[0]] += 16 * playersStat[arrByte[3] * 280 + 4 * 28 + 25];
                            logs[iter[0]] += 32 * playersStat[arrByte[3] * 280 + 5 * 28 + 25];
                            logs[iter[0]] += 64 * playersStat[arrByte[3] * 280 + 6 * 28 + 25];
                            logs[iter[0]] += 128 * playersStat[arrByte[3] * 280 + 7 * 28 + 25];
                            logs[iter[0]] += 256 * playersStat[arrByte[3] * 280 + 8 * 28 + 25];
                            logs[iter[0]] += 512 * playersStat[arrByte[3] * 280 + 9 * 28 + 25];
                            logs[iter[0]] += 1024 * playersStat[arrByte[3] * 280 + 26];
                            logs[iter[0]] += 2048 * playersStat[arrByte[3] * 280 + 1 * 28 + 26];
                            logs[iter[0]] += 4096 * playersStat[arrByte[3] * 280 + 2 * 28 + 26];
                            logs[iter[0]] += 8192 * playersStat[arrByte[3] * 280 + 3 * 28 + 26];
                            logs[iter[0]] += 16384 * playersStat[arrByte[3] * 280 + 4 * 28 + 26];
                            logs[iter[0]] += 32768 * playersStat[arrByte[3] * 280 + 5 * 28 + 26];
                            logs[iter[0]] += 65536 * playersStat[arrByte[3] * 280 + 6 * 28 + 26];
                            logs[iter[0]] += 131072 * playersStat[arrByte[3] * 280 + 7 * 28 + 26];
                            logs[iter[0]] += 262144 * playersStat[arrByte[3] * 280 + 8 * 28 + 26];
                            logs[iter[0]] += 524288 * playersStat[arrByte[3] * 280 + 9 * 28 + 26];
                            logs[iter[0]++] += 10000000;
                        }
                        sitePlant[0] = (byte)random.NextInt(1, 3);
                        followerBoost[0] += random.NextInt(1000, 6001);
                        SimulateRound(arrBool[0], ref vs, ref random, opponentTeamBuff[0], playerTeamBuff[0], (byte)arrByte[3],
                            ref randit, ref it, ref counterAWP, ref iter, i + p * 6 + 30, ref counterTo);
                        //xz
                        for (byte j = 0; j < 5; j++)
                        {
                            //kills
                            playersStat[arrByte[3] * 280 + (j) * 28 +1] += playersStat[arrByte[3] * 280 + (j) * 28 +17];
                            playersStat[arrByte[3] * 280 + (j + 5) * 28 + 1] += playersStat[arrByte[3] * 280 + (j + 5) * 28 + 17];
                            //deaths
                            playersStat[arrByte[3] * 280 + (j) * 28 +2] += playersStat[arrByte[3] * 280 + (j) * 28 +18];
                            playersStat[arrByte[3] * 280 + (j + 5) * 28 + 2] += playersStat[arrByte[3] * 280 + (j + 5) * 28 + 18];
                            //assists
                            if (playersStat[arrByte[3] * 280 + (j) * 28 +27] > 40) playersStat[arrByte[3] * 280 + (j) * 28 +3] += 1;
                            if (playersStat[arrByte[3] * 280 + (j + 5) * 28 + 27] > 40) playersStat[arrByte[3] * 280 + (j + 5) * 28 + 3] += 1;
                            //multikills
                            if (playersStat[arrByte[3] * 280 + (j) * 28 +17] != 0)
                                playersStat[arrByte[3] * 280 + (j) * 28 +4 + playersStat[arrByte[3] * 280 + (j) * 28 +17]]++;
                            if (playersStat[arrByte[3] * 280 + (j + 5) * 28 + 17] != 0)
                                playersStat[arrByte[3] * 280 + (j + 5) * 28 + 4 + playersStat[arrByte[3] * 280 + (j + 5) * 28 + 17]]++;
                            //clutches + entries precounted
                            //kast
                            if (playersStat[arrByte[3] * 280 + (j) * 28 +17] > 0 ||
                                playersStat[arrByte[3] * 280 + (j) * 28 +18] == 0 ||
                                playersStat[arrByte[3] * 280 + (j) * 28 +27] > 40 ||
                                random.NextInt(0, 100) < 10 /*traded*/) playersStat[arrByte[3] * 280 + (j) * 28 +15]++;
                            if (playersStat[arrByte[3] * 280 + (j + 5) * 28 + 17] > 0 ||
                                playersStat[arrByte[3] * 280 + (j + 5) * 28 + 18] == 0 ||
                                playersStat[arrByte[3] * 280 + (j + 5) * 28 + 27] > 40 ||
                                random.NextInt(0, 100) < 10 /*traded*/) playersStat[arrByte[3] * 280 + (j + 5) * 28 + 15]++;
                            //damage
                            playersStat[arrByte[3] * 280 + (j + 5) * 28 + 20] += playersStat[arrByte[3] * 280 + (j + 5) * 28 + 27];
                            playersStat[arrByte[3] * 280 + (j) * 28 +20] += playersStat[arrByte[3] * 280 + (j) * 28 +27];
                            //round reset right?
                            playersStat[arrByte[3] * 280 + (j + 5) * 28 + 17] = 0;
                            playersStat[arrByte[3] * 280 + (j + 5) * 28 + 18] = 0;
                            playersStat[arrByte[3] * 280 + (j + 5) * 28 + 19] = 0;
                            playersStat[arrByte[3] * 280 + (j + 5) * 28 + 27] = 0;
                            playersStat[arrByte[3] * 280 + (j) * 28 +17] = 0;
                            playersStat[arrByte[3] * 280 + (j) * 28 +18] = 0;
                            playersStat[arrByte[3] * 280 + (j) * 28 +19] = 0;
                            playersStat[arrByte[3] * 280 + (j) * 28 +27] = 0;
                        }
                    }
                    teamStat[0] = 1;
                    teamStat[3] = 1;
                    if (chances[0]) { teamStat[2] = 0; teamStat[5] = 1; }
                    if (!chances[0]) { teamStat[2] = 1; teamStat[5] = 0; }
                    if (chances[0]) chances[0] = false;
                    else chances[0] = true;
                    //Unity.Mathematics.Random random = new Unity.Mathematics.Random((uint)seed[0]);
                    for (byte i = 0; i < 5; i++)
                    {
                        //team1.players[i].mapStat = new int[28];
                        playersStat[arrByte[3] * 280 + (i) * 28 + 0] = 16000;
                        //team2.players[i].mapStat = new int[28];
                        playersStat[arrByte[3] * 280 + (i + 5) * 28 + 0] = 16000;
                        for (byte j = 16; j < 20; j++)
                        { playersStat[arrByte[3] * 280 + (i) * 28 + j] = 0; playersStat[arrByte[3] * 280 + (i + 5) * 28 + j] = 0; }
                        for (byte j = 21; j < 28; j++)
                        { playersStat[arrByte[3] * 280 + (i) * 28 + j] = 0; playersStat[arrByte[3] * 280 + (i + 5) * 28 + j] = 0; }
                    }
                    //if (!skip) manager.RefreshInterface();
                    for (byte i = 1; i <= 3; i++)
                    {
                        SimulateBuy(i, arrByte[3], random);
                        if (saveDemo[0])
                        {
                            // 20 player 0-9 weapon 0000-5000
                            logs[iter[0]++] = 2000000 + playersStat[arrByte[3] * 280 + 16];
                            logs[iter[0]++] = 2010000 + playersStat[arrByte[3] * 280 + 1 * 28 + 16];
                            logs[iter[0]++] = 2020000 + playersStat[arrByte[3] * 280 + 2 * 28 + 16];
                            logs[iter[0]++] = 2030000 + playersStat[arrByte[3] * 280 + 3 * 28 + 16];
                            logs[iter[0]++] = 2040000 + playersStat[arrByte[3] * 280 + 4 * 28 + 16];
                            logs[iter[0]++] = 2050000 + playersStat[arrByte[3] * 280 + 5 * 28 + 16];
                            logs[iter[0]++] = 2060000 + playersStat[arrByte[3] * 280 + 6 * 28 + 16];
                            logs[iter[0]++] = 2070000 + playersStat[arrByte[3] * 280 + 7 * 28 + 16];
                            logs[iter[0]++] = 2080000 + playersStat[arrByte[3] * 280 + 8 * 28 + 16];
                            logs[iter[0]++] = 2090000 + playersStat[arrByte[3] * 280 + 9 * 28 + 16];
                            // 3 player 0-9 money 00000-16000
                            logs[iter[0]++] = 3000000 + playersStat[arrByte[3] * 280];
                            logs[iter[0]++] = 3100000 + playersStat[arrByte[3] * 280 + 1 * 28];
                            logs[iter[0]++] = 3200000 + playersStat[arrByte[3] * 280 + 2 * 28];
                            logs[iter[0]++] = 3300000 + playersStat[arrByte[3] * 280 + 3 * 28];
                            logs[iter[0]++] = 3400000 + playersStat[arrByte[3] * 280 + 4 * 28];
                            logs[iter[0]++] = 3500000 + playersStat[arrByte[3] * 280 + 5 * 28];
                            logs[iter[0]++] = 3600000 + playersStat[arrByte[3] * 280 + 6 * 28];
                            logs[iter[0]++] = 3700000 + playersStat[arrByte[3] * 280 + 7 * 28];
                            logs[iter[0]++] = 3800000 + playersStat[arrByte[3] * 280 + 8 * 28];
                            logs[iter[0]++] = 3900000 + playersStat[arrByte[3] * 280 + 9 * 28];
                            // vest and helmet
                            logs[iter[0]] += 1 * playersStat[arrByte[3] * 280 + 25];
                            logs[iter[0]] += 2 * playersStat[arrByte[3] * 280 + 1 * 28 + 25];
                            logs[iter[0]] += 4 * playersStat[arrByte[3] * 280 + 2 * 28 + 25];
                            logs[iter[0]] += 8 * playersStat[arrByte[3] * 280 + 3 * 28 + 25];
                            logs[iter[0]] += 16 * playersStat[arrByte[3] * 280 + 4 * 28 + 25];
                            logs[iter[0]] += 32 * playersStat[arrByte[3] * 280 + 5 * 28 + 25];
                            logs[iter[0]] += 64 * playersStat[arrByte[3] * 280 + 6 * 28 + 25];
                            logs[iter[0]] += 128 * playersStat[arrByte[3] * 280 + 7 * 28 + 25];
                            logs[iter[0]] += 256 * playersStat[arrByte[3] * 280 + 8 * 28 + 25];
                            logs[iter[0]] += 512 * playersStat[arrByte[3] * 280 + 9 * 28 + 25];
                            logs[iter[0]] += 1024 * playersStat[arrByte[3] * 280 + 26];
                            logs[iter[0]] += 2048 * playersStat[arrByte[3] * 280 + 1 * 28 + 26];
                            logs[iter[0]] += 4096 * playersStat[arrByte[3] * 280 + 2 * 28 + 26];
                            logs[iter[0]] += 8192 * playersStat[arrByte[3] * 280 + 3 * 28 + 26];
                            logs[iter[0]] += 16384 * playersStat[arrByte[3] * 280 + 4 * 28 + 26];
                            logs[iter[0]] += 32768 * playersStat[arrByte[3] * 280 + 5 * 28 + 26];
                            logs[iter[0]] += 65536 * playersStat[arrByte[3] * 280 + 6 * 28 + 26];
                            logs[iter[0]] += 131072 * playersStat[arrByte[3] * 280 + 7 * 28 + 26];
                            logs[iter[0]] += 262144 * playersStat[arrByte[3] * 280 + 8 * 28 + 26];
                            logs[iter[0]] += 524288 * playersStat[arrByte[3] * 280 + 9 * 28 + 26];
                            logs[iter[0]++] += 10000000;
                        }
                        sitePlant[0] = (byte)random.NextInt(1, 3);
                        followerBoost[0] += random.NextInt(1000, 6001);
                        SimulateRound(arrBool[0], ref vs, ref random, opponentTeamBuff[0], playerTeamBuff[0], (byte)arrByte[3],
                            ref randit, ref it, ref counterAWP, ref iter, i + p * 6 + 33, ref counterTo);
                        //xz
                        for (byte j = 0; j < 5; j++)
                        {
                            //kills
                            playersStat[arrByte[3] * 280 + (j) * 28 + 1] += playersStat[arrByte[3] * 280 + (j) * 28 + 17];
                            playersStat[arrByte[3] * 280 + (j + 5) * 28 + 1] += playersStat[arrByte[3] * 280 + (j + 5) * 28 + 17];
                            //deaths
                            playersStat[arrByte[3] * 280 + (j) * 28 + 2] += playersStat[arrByte[3] * 280 + (j) * 28 + 18];
                            playersStat[arrByte[3] * 280 + (j + 5) * 28 + 2] += playersStat[arrByte[3] * 280 + (j + 5) * 28 + 18];
                            //assists
                            if (playersStat[arrByte[3] * 280 + (j) * 28 + 27] > 40) playersStat[arrByte[3] * 280 + (j) * 28 + 3] += 1;
                            if (playersStat[arrByte[3] * 280 + (j + 5) * 28 + 27] > 40) playersStat[arrByte[3] * 280 + (j + 5) * 28 + 3] += 1;
                            //multikills
                            if (playersStat[arrByte[3] * 280 + (j) * 28 + 17] != 0)
                                playersStat[arrByte[3] * 280 + (j) * 28 + 4 + playersStat[arrByte[3] * 280 + (j) * 28 + 17]]++;
                            if (playersStat[arrByte[3] * 280 + (j + 5) * 28 + 17] != 0)
                                playersStat[arrByte[3] * 280 + (j + 5) * 28 + 4 + playersStat[arrByte[3] * 280 + (j + 5) * 28 + 17]]++;
                            //clutches + entries precounted
                            //kast
                            if (playersStat[arrByte[3] * 280 + (j) * 28 + 17] > 0 ||
                                playersStat[arrByte[3] * 280 + (j) * 28 + 18] == 0 ||
                                playersStat[arrByte[3] * 280 + (j) * 28 + 27] > 40 ||
                                random.NextInt(0, 100) < 10 /*traded*/) playersStat[arrByte[3] * 280 + (j) * 28 + 15]++;
                            if (playersStat[arrByte[3] * 280 + (j + 5) * 28 + 17] > 0 ||
                                playersStat[arrByte[3] * 280 + (j + 5) * 28 + 18] == 0 ||
                                playersStat[arrByte[3] * 280 + (j + 5) * 28 + 27] > 40 ||
                                random.NextInt(0, 100) < 10 /*traded*/) playersStat[arrByte[3] * 280 + (j + 5) * 28 + 15]++;
                            //damage
                            playersStat[arrByte[3] * 280 + (j + 5) * 28 + 20] += playersStat[arrByte[3] * 280 + (j + 5) * 28 + 27];
                            playersStat[arrByte[3] * 280 + (j) * 28 + 20] += playersStat[arrByte[3] * 280 + (j) * 28 + 27];
                            //round reset right?
                            playersStat[arrByte[3] * 280 + (j + 5) * 28 + 17] = 0;
                            playersStat[arrByte[3] * 280 + (j + 5) * 28 + 18] = 0;
                            playersStat[arrByte[3] * 280 + (j + 5) * 28 + 19] = 0;
                            playersStat[arrByte[3] * 280 + (j + 5) * 28 + 27] = 0;
                            playersStat[arrByte[3] * 280 + (j) * 28 + 17] = 0;
                            playersStat[arrByte[3] * 280 + (j) * 28 + 18] = 0;
                            playersStat[arrByte[3] * 280 + (j) * 28 + 19] = 0;
                            playersStat[arrByte[3] * 280 + (j) * 28 + 27] = 0;
                        }
                        if (teamStat[1] == (19 + p * 3) || teamStat[4] == (19 + p * 3))
                        { br[0] = true; break; }
                    }
                    if (br[0]) { br.Dispose(); break; }
                    br.Dispose();
                }
            }
            followerBoost[0] -= random.NextInt(10000, 50001);
            /*
            for (byte i = 0; i < 10; i++) playerStats.Add(new Stat());
            for (byte i = 0; i < 5; i++)
            {
                playerStats[i + n * 10].SetStats(team1.players[i].nickname, team1.players[i].mapStat,
                    (short)(team1.mapStat[1] + team2.mapStat[1]), (short)day);
                playerStats[i + n * 10].CalculateRating();
                playerStats[i + 5 + n * 10].SetStats(team2.players[i].nickname, team2.players[i].mapStat,
                    (short)(team1.mapStat[1] + team2.mapStat[1]), (short)day);
                playerStats[i + 5 + n * 10].CalculateRating();
            }
            maproundst1.Add(team1.mapStat[1]); maproundst2.Add(team2.mapStat[1]);*/
            maproundst[arrByte[3]] = (byte)teamStat[1]; maproundst[arrByte[3] + 5] = (byte)teamStat[4];
            if (maproundst[arrByte[3]] > maproundst[arrByte[3] + 5]) arrByte[1]++;
            else arrByte[2]++;
            NativeArray<bool> breaking = new NativeArray<bool>(1, Allocator.Temp);
            breaking[0] = true;
            if (arrByte[1] == (int)pickbans[7] + 1) winner[0] = 1;
            else if (arrByte[2] == (int)pickbans[7] + 1) winner[0] = 2;
            else breaking[0] = false;
            if (breaking[0]) { breaking.Dispose(); break; }
            breaking.Dispose();
        }
        logs[iter[0]++] = 17012;
        counterTo.Dispose();
        iter.Dispose();
        randit.Dispose();
        it.Dispose();
        vs.Dispose();
        counterAWP.Dispose();
        playerTeamBuff.Dispose();
        opponentTeamBuff.Dispose();
        arrBool.Dispose();
        arrByte.Dispose();
    }

    //byte entryf = 1;
    //public byte playerTeam = 0;

    private void SimulateBuy(int i, short arrByte, Unity.Mathematics.Random random)
    {
        NativeArray<int> helpvar = new NativeArray<int>(4, Allocator.Temp);
        for (int n = 0; n < 2; n++)
        {
            helpvar[0] = 0;
            for (int x = 0; x < 5; x++)
            {
                helpvar[0] += playersStat[arrByte * 280 + (x + (5 * n)) * 28 + 0] + playersStat[arrByte * 280 + (x + (5 * n)) * 28 + 16];
                helpvar[0] += playersStat[arrByte * 280 + (x + (5 * n)) * 28 + 25] * 650 + playersStat[arrByte * 280 + (x + (5 * n)) * 28 + 26] * 350;
            }
            if (helpvar[0] <= 4000)
            {
                for (int x = 0; x < 5; x++)
                {
                    playersStat[arrByte * 280 + (x + (5 * n)) * 28 + 0] = 150;
                    playersStat[arrByte * 280 + (x + (5 * n)) * 28 + 25] = 1;
                }
            }
            else if (helpvar[0] <= 10000)
            {
                if (random.NextInt(100) < 50)
                {
                    helpvar[1] = (helpvar[0] - 3500) / 5;
                    for (int x = 0; x < 5; x++)
                    {
                        playersStat[arrByte * 280 + (x + (5 * n)) * 28 + 0] = helpvar[1];
                        playersStat[arrByte * 280 + (x + (5 * n)) * 28 + 16] = 700;
                    }
                }
                //else
                //{
                //}
            }
            else if (helpvar[0] <= 13000)
            {
                helpvar[2] = random.NextInt(100);
                if (helpvar[2] < 40)
                {
                    helpvar[1] = (helpvar[0] - 6750) / 5;
                    for (int x = 0; x < 5; x++)
                    {
                        playersStat[arrByte * 280 + (x + (5 * n)) * 28 + 0] = helpvar[1];
                        playersStat[arrByte * 280 + (x + (5 * n)) * 28 + 16] = 700;
                        playersStat[arrByte * 280 + (x + (5 * n)) * 28 + 25] = 1;
                    }
                }
                else if (helpvar[2] < 75)
                {
                    helpvar[3] = -1;
                    helpvar[2] = -1;
                    for (int x = 0; x < 5; x++)
                    {
                        if (roles[(x + (5 * n))] == 2 && helpvar[3] == -1) { helpvar[3] = (x + (5 * n)); continue; }
                        if (roles[(x + (5 * n))] == 2 && helpvar[2] == -1) helpvar[2] = (x + (5 * n));
                    }
                    //todo
                    helpvar[1] = (helpvar[0] - (teamStat[2 + n * 3] == 0 ? 1750 : 1900) *
                        ((helpvar[2] == -1 ? 1 : 0) + (helpvar[3] == -1 ? 1 : 0) + 3) + 1700 *
                        ((helpvar[2] == -1 ? 0 : 1) + (helpvar[3] == -1 ? 0 : 1))) / 5;
                    for (int x = 0; x < 5; x++)
                    {
                        playersStat[arrByte * 280 + (x + (5 * n)) * 28 + 0] = helpvar[1];
                        if ((x + (5 * n)) == helpvar[3] || (x + (5 * n)) == helpvar[2]) { playersStat[arrByte * 280 + (x + (5 * n)) * 28 + 16] = 1700; continue; }
                        playersStat[arrByte * 280 + (x + (5 * n)) * 28 + 16] = (teamStat[2 + n * 3] == 0 ? 1100 : 1250);
                        playersStat[arrByte * 280 + (x + (5 * n)) * 28 + 25] = 1;
                    }
                }
            }
            else if (helpvar[0] <= 15000)
            {
                helpvar[2] = random.NextInt(100);
                if (helpvar[2] < 40)
                {
                    helpvar[1] = (helpvar[0] - 6750) / 5;
                    for (int x = 0; x < 5; x++)
                    {
                        playersStat[arrByte * 280 + (x + (5 * n)) * 28 + 0] = helpvar[1];
                        playersStat[arrByte * 280 + (x + (5 * n)) * 28 + 16] = 700;
                        playersStat[arrByte * 280 + (x + (5 * n)) * 28 + 25] = 1;
                    }
                }
                else
                {
                    helpvar[3] = -1;
                    helpvar[2] = -1;
                    for (int x = 0; x < 5; x++)
                    {
                        if (roles[(x + (5 * n))] == 2 && helpvar[3] == -1) { helpvar[3] = (x + (5 * n)); continue; }
                        if (roles[(x + (5 * n))] == 2 && helpvar[2] == -1) helpvar[2] = (x + (5 * n));
                    }
                    //todo
                    helpvar[1] = (helpvar[0] - (teamStat[2 + n * 3] == 0 ? 2450 : 2250) *
                        ((helpvar[2] == -1 ? 1 : 0) + (helpvar[3] == -1 ? 1 : 0) + 3) + 2450 *
                        ((helpvar[2] == -1 ? 0 : 1) + (helpvar[3] == -1 ? 0 : 1))) / 5;
                    for (int x = 0; x < 5; x++)
                    {
                        playersStat[arrByte * 280 + (x + (5 * n)) * 28 + 0] = helpvar[1];
                        playersStat[arrByte * 280 + (x + (5 * n)) * 28 + 25] = 1;
                        if (x == helpvar[3] || x == helpvar[2]) { playersStat[arrByte * 280 + (x + (5 * n)) * 28 + 16] = 1700; continue; }
                        playersStat[arrByte * 280 + (x + (5 * n)) * 28 + 16] = (teamStat[2 + n * 3] == 0 ? 1800 : 1250);
                        playersStat[arrByte * 280 + (x + (5 * n)) * 28 + 26] = teamStat[2 + n * 3];// == 0 ? 0 : 1;
                    }
                }
            }
            else if (helpvar[0] <= 17500)
            {
                helpvar[2] = random.NextInt(100);
                if (helpvar[2] < 60)
                {
                    helpvar[1] = (helpvar[0] - (teamStat[2 + n * 3] == 0 ? 10500 : 11250)) / 5;
                    for (int x = 0; x < 5; x++)
                    {
                        playersStat[arrByte * 280 + (x + (5 * n)) * 28 + 0] = helpvar[1];
                        playersStat[arrByte * 280 + (x + (5 * n)) * 28 + 16] = teamStat[2 + n * 3] == 0 ? 1100 : 1250;
                        playersStat[arrByte * 280 + (x + (5 * n)) * 28 + 25] = 1;
                        playersStat[arrByte * 280 + (x + (5 * n)) * 28 + 26] = 1;
                    }
                }
                else
                {
                    helpvar[3] = -1;
                    helpvar[2] = -1;
                    for (int x = 0; x < 5; x++)
                    {
                        if (roles[(x + (5 * n))] == 2 && helpvar[3] == -1) { helpvar[3] = (x + (5 * n)); continue; }
                        if (roles[(x + (5 * n))] == 2 && helpvar[2] == -1) helpvar[2] = (x + (5 * n));
                    }
                    //todo
                    helpvar[1] = (helpvar[0] - (teamStat[2 + n * 3] == 0 ? 2800 : 2700) *
                        ((helpvar[2] == -1 ? 1 : 0) + (helpvar[3] == -1 ? 1 : 0) + 3) + 2700 *
                        ((helpvar[2] == -1 ? 0 : 1) + (helpvar[3] == -1 ? 0 : 1))) / 5;
                    for (int x = 0; x < 5; x++)
                    {
                        playersStat[arrByte * 280 + (x + (5 * n)) * 28 + 0] = helpvar[1];
                        playersStat[arrByte * 280 + (x + (5 * n)) * 28 + 25] = 1;
                        if ((x + (5 * n)) == helpvar[3] || (x + (5 * n)) == helpvar[2])
                        { playersStat[arrByte * 280 + (x + (5 * n)) * 28 + 16] = 1700; playersStat[arrByte * 280 + (x + (5 * n)) * 28 + 26] = 1; continue; }
                        playersStat[arrByte * 280 + (x + (5 * n)) * 28 + 26] = teamStat[2 + n * 3] ^ 1;
                        playersStat[arrByte * 280 + (x + (5 * n)) * 28 + 16] = (teamStat[2 + n * 3] == 0 ? 1800 : 2050);
                    }
                }
            }
            else if (helpvar[0] <= 20000)
            {
                helpvar[2] = random.NextInt(100);
                if (helpvar[2] < 35)
                {
                    helpvar[1] = (helpvar[0] - (teamStat[2 + n * 3] == 0 ? 10500 : 11250)) / 5;
                    for (int x = 0; x < 5; x++)
                    {
                        playersStat[arrByte * 280 + (x + (5 * n)) * 28 + 0] = helpvar[1];
                        playersStat[arrByte * 280 + (x + (5 * n)) * 28 + 16] = teamStat[2 + n * 3] == 0 ? 1100 : 1250;
                        playersStat[arrByte * 280 + (x + (5 * n)) * 28 + 25] = 1;
                        playersStat[arrByte * 280 + (x + (5 * n)) * 28 + 26] = 1;
                    }
                }
                else
                {
                    helpvar[3] = -1;
                    helpvar[2] = -1;
                    for (int x = 0; x < 5; x++)
                    {
                        if (roles[(x + (5 * n))] == 2 && helpvar[3] == -1) { helpvar[3] = (x + (5 * n)); continue; }
                        if (roles[(x + (5 * n))] == 2 && helpvar[2] == -1) helpvar[2] = (x + (5 * n));
                    }
                    //todo
                    helpvar[1] = (helpvar[0] - (teamStat[2 + n * 3] == 0 ? 3350 : 3050) *
                        ((helpvar[2] == -1 ? 1 : 0) + (helpvar[3] == -1 ? 1 : 0) + 3) + 2700 *
                        ((helpvar[2] == -1 ? 0 : 1) + (helpvar[3] == -1 ? 0 : 1))) / 5;
                    for (int x = 0; x < 5; x++)
                    {
                        playersStat[arrByte * 280 + (x + (5 * n)) * 28 + 0] = helpvar[1];
                        playersStat[arrByte * 280 + (x + (5 * n)) * 28 + 25] = 1;
                        playersStat[arrByte * 280 + (x + (5 * n)) * 28 + 26] = 1;
                        if ((x + (5 * n)) == helpvar[3] || (x + (5 * n)) == helpvar[2])
                        { playersStat[arrByte * 280 + (x + (5 * n)) * 28 + 16] = 1700; continue; }
                        playersStat[arrByte * 280 + (x + (5 * n)) * 28 + 16] = (teamStat[2 + n * 3] == 0 ? 2700 : 2050);
                    }
                }
            }
            else if (helpvar[0] <= 22500)
            {
                //todo
                helpvar[1] = (helpvar[0] - (teamStat[2 + n * 3] == 0 ? 3700 : 3900) * 5) / 5;
                for (int x = 0; x < 5; x++)
                {
                    playersStat[arrByte * 280 + (x + (5 * n)) * 28 + 0] = helpvar[1];
                    playersStat[arrByte * 280 + (x + (5 * n)) * 28 + 25] = 1;
                    playersStat[arrByte * 280 + (x + (5 * n)) * 28 + 26] = 1;
                    playersStat[arrByte * 280 + (x + (5 * n)) * 28 + 16] = (teamStat[2 + n * 3] == 0 ? 2700 : 2900);
                }
            }
            else if (helpvar[0] < 28750)
            {
                //todo
                helpvar[1] = (helpvar[0] - (teamStat[2 + n * 3] == 0 ? 3700 : 3900) * 5) / 5;
                for (int x = 0; x < 5; x++)
                {
                    playersStat[arrByte * 280 + (x + (5 * n)) * 28 + 0] = helpvar[1];
                    playersStat[arrByte * 280 + (x + (5 * n)) * 28 + 25] = 1;
                    playersStat[arrByte * 280 + (x + (5 * n)) * 28 + 26] = 1;
                    playersStat[arrByte * 280 + (x + (5 * n)) * 28 + 16] = (teamStat[2 + n * 3] == 0 ? (random.NextInt(100) < 90 ? 2700 : 3000) : (random.NextInt(100) < 70 ? 2900 : 3300));
                }
            }
            else
            {
                helpvar[3] = -1;
                helpvar[2] = -1;
                for (int x = 0; x < 5; x++)
                {
                    if (roles[(x + (5 * n))] == 2 && helpvar[3] == -1) { helpvar[3] = (x + (5 * n)); continue; }
                    if (roles[(x + (5 * n))] == 2 && helpvar[2] == -1) helpvar[2] = (x + (5 * n));
                }
                //todo
                helpvar[1] = (helpvar[0] - (teamStat[2 + n * 3] == 0 ? 3700 : 3900) *
                    ((helpvar[2] == -1 ? 1 : 0) + (helpvar[3] == -1 ? 1 : 0) + 3) + 5750 *
                    ((helpvar[2] == -1 ? 0 : 1) + (helpvar[3] == -1 ? 0 : 1))) / 5;
                for (int x = 0; x < 5; x++)
                {
                    playersStat[arrByte * 280 + (x + (5 * n)) * 28 + 0] = helpvar[1];
                    playersStat[arrByte * 280 + (x + (5 * n)) * 28 + 25] = 1;
                    playersStat[arrByte * 280 + (x + (5 * n)) * 28 + 26] = 1;
                    if ((x + (5 * n)) == helpvar[3] || (x + (5 * n)) == helpvar[2])
                    { playersStat[arrByte * 280 + (x + (5 * n)) * 28 + 16] = 4750; continue; }
                    playersStat[arrByte * 280 + (x + (5 * n)) * 28 + 16] = (teamStat[2 + n * 3] == 0 ? (random.NextInt(100) < 90 ? 2700 : 3000) : (random.NextInt(100) < 70 ? 2900 : 3300));
                }
            }
        }
        helpvar.Dispose();
    }

    private void SimulateRound(bool arrBool, ref NativeArray<int> vs, ref Unity.Mathematics.Random random,
        int opponentTeamBuff, int playerTeamBuff, byte arrByte, ref NativeArray<int> randit,
        ref NativeArray<byte> it, ref NativeArray<byte> counterAWP, ref NativeArray<int> iter, int round,
        ref NativeArray<uint> counterTo)
    {
        // <---
        ///
        //
        arrBool = false;
        for (int b = 0; b < 27; b++) vs[b] = 0;
        vs[0] = (byte)random.NextInt(25, 76);
        vs[1] = 1; //byte vs[1] = 1;
        vs[2] = 1; //FOR ENTRY
        vs[3] = 5; vs[4] = 5; // team1 team2 alive
        if (teamStat[2] == 0) for (byte s = 0; s < 5; s++)
            { vs[0] += 5 * (activity[s] - 1); }
        if (teamStat[5] == 0)
        {
            vs[1] = 2; for (byte s = 0; s < 5; s++)
            { vs[0] += 5 * (activity[s + 5] - 1); }
        }
        //float timer = 115.0f;
        //byte vs[3] = 5, vs[4] = 5;
        //byte[] hp1 = new byte[5];
        //byte[] hp2 = new byte[5];
        for (byte s = 0; s < 5; s++) { vs[s + 5] = 100; vs[s + 10] = 100; }
        NativeArray<bool> aliveBool = new NativeArray<bool>(10, Allocator.Temp);
        for (int x = 0; x < 10; x++) aliveBool[x] = true;
        //NativeArray<byte> it = new NativeArray<byte>(2, Allocator.Temp);
        if (vs[1] == 1)
        {
            it[0] = 0;
            it[1] = (byte)random.NextInt((int)0, (int)5);
            //FirstFragT(ref it[0], ref it[1], 1, ref hp1, ref hp2, ref vs[3], ref vs[4]);
            vs[26] = (byte)random.NextInt(0, 100);
            //team1 is t
            if (vs[2] == 1)
            {
                if (vs[26] < 45 && hasRole[0])
                { for (it[0] = 0; it[0] < 5;) { if (roles[it[0]] == 0) break; else ++it[0]; } }
                else if (vs[26] < 65 && hasRole[3])
                { for (it[0] = 0; it[0] < 5;) { if (roles[it[0]] == 3) break; else ++it[0]; } }
                else if (vs[26] < 85 && hasRole[2])
                { for (it[0] = 0; it[0] < 5;) { if (roles[it[0]] == 2) break; else ++it[0]; } }
                else if (vs[26] < 98 && hasRole[1])
                { for (it[0] = 0; it[0] < 5;) { if (roles[it[0]] == 1) break; else ++it[0]; } }
                else it[0] = (byte)random.NextInt(0, 5);
            }
            //
            vs[15] = GetWeaponPoints(it[0], 1, arrByte, random.NextInt(4096 + difficulty[8]));
            vs[16] = GetWeaponPoints(it[1], 2, arrByte, random.NextInt(4096 + difficulty[8]));
            if (playerTeam[0] == 0) { vs[17] = 0; vs[18] = 0; }
            else if (playerTeam[0] == 1)
            {
                vs[17] = playerTeamBuff; vs[18] = opponentTeamBuff;
            }
            else if (playerTeam[0] == 2)
            {
                vs[18] = playerTeamBuff; vs[17] = opponentTeamBuff;
            }
            vs[19] = t[it[0]] + ((playersStat[(arrByte * 280) + ((it[0]) * 28) + 16] >= 4750 ||
                playersStat[(arrByte * 280) + ((it[0]) * 28) + 16] == 1700) ?
                awp[it[0]] : rifle[it[0]]) + entring[it[0]] + vs[15] + vs[17] +
                (playersStat[(arrByte * 280) + ((it[0]) * 28) + 25] * 10) + (playersStat[(arrByte * 280) + ((it[0]) * 28) + 26] * 15);
            vs[20] = ct[it[1] + 5] + ((playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 16] >= 4750 ||
                playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 16] == 1700) ?
                awp[it[1] + 5] : rifle[it[1] + 5]) +
                (vs[2] == 1 ? entring[it[1] + 5] : ct[it[1] + 5]) + vs[16] + vs[18] +
                (playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 25] * 10) + (playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 26] * 15);
            randit[0] = random.NextInt(0, (int)((killing[it[0]] + killing[it[1] + 5]) * 1.6875));
            randit[1] = randit[0] - (int)(killing[it[0]] * 1.6875);
            vs[22] = (short)(-randit[1] + vs[19] + mapAdvantage[0]);
            vs[23] = (short)(randit[1] + vs[20] - mapAdvantage[0]);
            if (mapAdvantage[0] > 0 && random.NextInt(0, 100) < 85) mapAdvantage[0] -= 1;
            if (mapAdvantage[0] < 0 && random.NextInt(0, 100) < 85) mapAdvantage[0] += 1;
            if (vs[22] > vs[23])
            {
                /*logs[iter[0]++] = (arrByte + 1) * 1000000000L + round * 1000000 + it[0] * 100000 + (it[1] + 5) * 10000 +
                    playersStat[(arrByte * 280) + ((it[0]) * 28) + 16];*/
                if (saveDemo[0]) logs[iter[0]++] = 1000000 + it[0] * 100000 + (it[1] + 5) * 10000 + playersStat[(arrByte * 280) + ((it[0]) * 28) + 16];
                playersStat[(arrByte * 280) + ((it[0]) * 28) + 27] += vs[it[1] + 10];
                vs[it[1] + 10] = 0; vs[4]--;
                aliveBool[it[1] + 5] = false;
                playersStat[(arrByte * 280) + ((it[0]) * 28) + 17]++;
                playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 18]++;
                if (random.NextInt(0, 100) < 60 && vs[it[0] + 5] == 100)
                {
                    vs[24] = random.NextInt(1, 100);
                    playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 27] += vs[24];
                    vs[it[0] + 5] -= vs[24];
                }
                playersStat[(arrByte * 280) + ((it[0]) * 28) + 0] += 300;
                if (playersStat[(arrByte * 280) + ((it[0]) * 28) + 0] > 16000) playersStat[(arrByte * 280) + ((it[0]) * 28) + 0] = 16000;
                playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 16] = 0;
                playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 25] = 0;
                playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 26] = 0;
                playersStat[(arrByte * 280) + ((it[0]) * 28) + 4]++; // entry
            }
            else if (vs[22] < vs[23])
            {
                /*logs[iter[0]++] = (arrByte + 1) * 1000000000L + round * 1000000 + (it[1] + 5) * 100000 + it[0] * 10000 +
                    playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 16];*/
                if (saveDemo[0]) logs[iter[0]++] = 1000000 + (it[1] + 5) * 100000 + it[0] * 10000 + playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 16];
                playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 4]++; // entry
                if (random.NextInt(0, 100) < 60 && vs[it[1] + 10] == 100)
                {
                    vs[24] = random.NextInt(1, 100); // = 1 - 99
                    playersStat[(arrByte * 280) + ((it[0]) * 28) + 27] += vs[24];
                    vs[it[1] + 10] -= vs[24];
                }
                playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 27] += vs[it[0] + 5];
                playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 17]++;
                playersStat[(arrByte * 280) + ((it[0]) * 28) + 18]++;
                playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 0] += 300;
                playersStat[(arrByte * 280) + ((it[0]) * 28) + 25] = 0;
                playersStat[(arrByte * 280) + ((it[0]) * 28) + 26] = 0;
                if (playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 0] > 16000) playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 0] = 16000;
                playersStat[(arrByte * 280) + ((it[0]) * 28) + 16] = 0;
                vs[it[0] + 5] = 0; vs[3]--;
                aliveBool[it[0]] = false;
            }
            else
            {
                /*logs[iter[0]++] = (arrByte + 1) * 1000000000L + round * 1000000 + it[0] * 100000 +
                    (it[1] + 5) * 10000 + playersStat[(arrByte * 280) + ((it[0]) * 28) + 16]; // t1 k t2 d
                logs[iter[0]++] = (arrByte + 1) * 1000000000L + round * 1000000 + (it[1] + 5) * 100000 +
                    it[0] * 10000 + playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 16]; // t2 k t1 d*/
                if (saveDemo[0]) logs[iter[0]++] = 1000000 + it[0] * 100000 + (it[1] + 5) * 10000 + playersStat[(arrByte * 280) + ((it[0]) * 28) + 16];
                if (saveDemo[0]) logs[iter[0]++] = 1000000 + (it[1] + 5) * 100000 + it[0] * 10000 + playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 16];
                playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 4]++; // entry
                playersStat[(arrByte * 280) + ((it[0]) * 28) + 17]++;
                playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 18]++;
                playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 17]++;
                playersStat[(arrByte * 280) + ((it[0]) * 28) + 18]++;
                playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 0] += 300;
                if (playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 0] > 16000) playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 0] = 16000;
                playersStat[(arrByte * 280) + ((it[0]) * 28) + 16] = 0;
                playersStat[(arrByte * 280) + ((it[0]) * 28) + 0] += 300;
                if (playersStat[(arrByte * 280) + ((it[0]) * 28) + 0] > 16000) playersStat[(arrByte * 280) + ((it[0]) * 28) + 0] = 16000;
                playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 16] = 0;
                playersStat[(arrByte * 280) + ((it[0]) * 28) + 27] += vs[it[0] + 5];
                playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 27] += vs[it[1] + 10];
                playersStat[(arrByte * 280) + ((it[0]) * 28) + 25] = 0;
                playersStat[(arrByte * 280) + ((it[0]) * 28) + 26] = 0;
                playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 25] = 0;
                playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 26] = 0;
                vs[it[0] + 5] = 0; vs[3]--;
                vs[it[1] + 10] = 0; vs[4]--;
                aliveBool[it[1] + 5] = false;
                aliveBool[it[0]] = false;
            }
            while (vs[3] > 0 && vs[4] > 0 && !arrBool)
            {
                ///
                ///DEFAULTFRAG
                ///
                //byte rol = (byte)random.NextInt(0, 100);
                it[0] = arrrands[(int)((counterTo[0]++) % (uint)arrrands.Length)];
                it[1] = arrrands[(int)((counterTo[0]++) % (uint)arrrands.Length)];
                for (int z = 0; !(aliveBool[it[0]]) && z < 10; z++)
                {
                    it[0] = arrrands[(int)((counterTo[0]++) % (uint)arrrands.Length)];
                }
                if (!(aliveBool[it[0]]))
                {
                    for (byte z = 0; !(aliveBool[it[0]]) && z < 5; z++)
                    {
                        if (aliveBool[z]) { it[0] = z; break; }
                    }
                }
                for (int z = 0; !(aliveBool[it[1] + 5]) && z < 10; z++)
                {
                    it[1] = arrrands[(int)((counterTo[0]++) % (uint)arrrands.Length)];
                }
                if (!(aliveBool[it[1] + 5]))
                {
                    for (byte z = 0; !(aliveBool[it[1] + 5]) && z < 5; z++)
                    {
                        if (aliveBool[z]) { it[1] = z; break; }
                    }
                }
                if (vs[3] == 1 && vs[4] > 0)
                {
                    if (vs[4] > playersStat[(arrByte * 280) + ((it[0]) * 28) + 19]) playersStat[(arrByte * 280) + ((it[0]) * 28) + 19] = vs[4];
                }
                if (vs[3] > 0 && vs[4] == 1)
                {
                    if (vs[3] > playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 19]) playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 19] = vs[3];
                }
                //weapon
                vs[15] = GetWeaponPoints(it[0], 1, arrByte, random.NextInt(2345 + difficulty[8]));
                vs[16] = GetWeaponPoints(it[1], 2, arrByte, random.NextInt(2345 + difficulty[8]));
                if (playerTeam[0] == 0) { vs[17] = 0; vs[18] = 0; }
                else if (playerTeam[0] == 1)
                {
                    vs[17] = playerTeamBuff; vs[18] = opponentTeamBuff;
                }
                else if (playerTeam[0] == 2)
                {
                    vs[18] = playerTeamBuff; vs[17] = opponentTeamBuff;
                }
                //
                vs[19] = (vs[1] == 1 ? t[it[0]] : ct[it[0]]) +
                    (playersStat[(arrByte * 280) + ((it[0]) * 28) + 16] == 4750 ?
                    awp[it[0]] : rifle[it[0]]) +
                    (vs[3] == 1 ? clutching[it[0]] : killing[it[0]]) + vs[15] + vs[17] +
                    playersStat[(arrByte * 280) + ((it[0]) * 28) + 25] * 10 + playersStat[(arrByte * 280) + ((it[0]) * 28) + 26] * 15;
                vs[20] = (vs[1] == 1 ? ct[it[1] + 5] : t[it[1] + 5]) +
                    (playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 16] == 4750 ?
                    awp[it[1] + 5] : rifle[it[1] + 5]) +
                    (vs[4] == 1 ? clutching[it[1] + 5] : killing[it[1] + 5]) + vs[16] + vs[18] +
                    playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 25] * 10 + playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 26] * 15;
                //randit[0] = random.NextInt(0, (int)(killing[it[0]] * 21.5 * (vs[3] * 0.025 + 0.875)));
                //randit[1] = random.NextInt(0, (int)(killing[it[1] + 5] * 21.5 * (vs[4] * 0.025 + 0.875)));
                randit[0] = random.NextInt(0,
                    (int)((killing[it[0]] * (vs[3] * 0.025 + 0.875) +
                    killing[it[1] + 5] * (vs[4] * 0.025 + 0.875))
                    * 1.6875));
                randit[1] = randit[0] - (int)(killing[it[0]] * (vs[3] * 0.025 + 0.875) * 1.6875);
                vs[22] = (short)(-randit[1] + vs[19] + mapAdvantage[0] - (playersStat[(arrByte * 280) + ((it[0]) * 28) + 16] < 4600 ? 0 : counterAWP[0]));
                vs[23] = (short)(randit[1] + vs[20] - mapAdvantage[0] - (playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 16] < 4600 ? 0 : counterAWP[1]));
                if (mapAdvantage[0] > 0 && random.NextInt(0, 100) < 85) mapAdvantage[0] -= 1;
                if (mapAdvantage[0] < 0 && random.NextInt(0, 100) < 85) mapAdvantage[0] += 1;
                if (vs[22] == vs[23] && (vs[3] == 1 || vs[4] == 1)) { if (random.NextInt(0, 100) < 50) vs[22]++; else vs[23]++; }
                if (vs[22] > vs[23])
                {
                    //if (playerTeam[0] == 2 && random.NextInt(0, 100) < buff[7]) continue;
                    if (saveDemo[0]) logs[iter[0]++] = 1000000 + it[0] * 100000 + (it[1] + 5) * 10000 + playersStat[(arrByte * 280) + ((it[0]) * 28) + 16];
                    //logs[iter[0]++] = (arrByte + 1) * 1000000000L + round * 1000000 + it[0] * 100000 +
                    //(it[1] + 5) * 10000 + playersStat[(arrByte * 280) + ((it[0]) * 28) + 16]; // t1 k t2 d
                    playersStat[(arrByte * 280) + ((it[0]) * 28) + 27] += vs[it[1] + 10];
                    playersStat[(arrByte * 280) + ((it[0]) * 28) + 17]++;
                    playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 18]++;
                    if (random.NextInt(0, 100) < 60 && vs[it[0] + 5] == 100)
                    {
                        vs[24] = (byte)random.NextInt(1, 100);
                        playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 27] += vs[24];
                        vs[it[0] + 5] -= vs[24];
                    }
                    playersStat[(arrByte * 280) + ((it[0]) * 28) + 0] += 300;
                    if (playersStat[(arrByte * 280) + ((it[0]) * 28) + 0] > 16000) playersStat[(arrByte * 280) + ((it[0]) * 28) + 0] = 16000;
                    playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 16] = 0;
                    playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 25] = 0;
                    playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 26] = 0;
                    vs[it[1] + 10] = 0; vs[4]--;
                    aliveBool[it[1] + 5] = false;
                }
                else if (vs[22] < vs[23])
                {
                    //if (playerTeam[0] == 1 && random.NextInt(0, 100) < buff[7]) continue;
                    if (saveDemo[0]) logs[iter[0]++] = 1000000 + (it[1] + 5) * 100000 + it[0] * 10000 + playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 16];
                    //logs[iter[0]++] = (arrByte + 1) * 1000000000L + round * 1000000 + (it[1] + 5) * 100000 +
                    //    it[0] * 10000 + playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 16]; // t2 k t1 d
                    playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 17]++;
                    playersStat[(arrByte * 280) + ((it[0]) * 28) + 18]++;
                    playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 0] += 300;
                    if (playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 0] > 16000) playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 0] = 16000;
                    playersStat[(arrByte * 280) + ((it[0]) * 28) + 16] = 0;
                    playersStat[(arrByte * 280) + ((it[0]) * 28) + 25] = 0;
                    playersStat[(arrByte * 280) + ((it[0]) * 28) + 26] = 0;
                    if (random.NextInt(0, 100) < 60 && vs[it[1] + 10] == 100)
                    {
                        vs[24] = (byte)random.NextInt(1, 100);
                        playersStat[(arrByte * 280) + ((it[0]) * 28) + 27] += vs[24];
                        vs[it[1] + 10] -= vs[24];
                    }
                    playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 27] += vs[it[0] + 5];
                    vs[it[0] + 5] = 0; vs[3]--;
                    aliveBool[it[0]] = false;
                }
                else
                {
                    //logs[iter[0]++] = (arrByte + 1) * 1000000000L + round * 1000000 + it[0] * 100000 +
                    //(it[1] + 5) * 10000 + playersStat[(arrByte * 280) + ((it[0]) * 28) + 16]; // t1 k t2 d
                    //logs[iter[0]++] = (arrByte + 1) * 1000000000L + round * 1000000 + (it[1] + 5) * 100000 +
                    //    it[0] * 10000 + playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 16]; // t2 k t1 d
                    if (saveDemo[0]) logs[iter[0]++] = 1000000 + it[0] * 100000 + (it[1] + 5) * 10000 + playersStat[(arrByte * 280) + ((it[0]) * 28) + 16];
                    if (saveDemo[0]) logs[iter[0]++] = 1000000 + (it[1] + 5) * 100000 + it[0] * 10000 + playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 16];
                    playersStat[(arrByte * 280) + ((it[0]) * 28) + 17]++;
                    playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 18]++;
                    playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 17]++;
                    playersStat[(arrByte * 280) + ((it[0]) * 28) + 18]++;
                    playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 0] += 300;
                    if (playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 0] > 16000) playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 0] = 16000;
                    playersStat[(arrByte * 280) + ((it[0]) * 28) + 16] = 0;
                    playersStat[(arrByte * 280) + ((it[0]) * 28) + 0] += 300;
                    if (playersStat[(arrByte * 280) + ((it[0]) * 28) + 0] > 16000) playersStat[(arrByte * 280) + ((it[0]) * 28) + 0] = 16000;
                    playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 16] = 0;
                    playersStat[(arrByte * 280) + ((it[0]) * 28) + 27] += vs[it[0] + 5];
                    playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 27] += vs[it[1] + 10];
                    playersStat[(arrByte * 280) + ((it[0]) * 28) + 25] = 0;
                    playersStat[(arrByte * 280) + ((it[0]) * 28) + 26] = 0;
                    playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 25] = 0;
                    playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 26] = 0;
                    vs[it[0] + 5] = 0; vs[3]--;
                    vs[it[1] + 10] = 0; vs[4]--;
                    aliveBool[it[1] + 5] = false;
                    aliveBool[it[0]] = false;
                }
                if (vs[3] > 0 && vs[4] == 0)
                {
                    teamStat[1]++;
                    if (saveDemo[0]) logs[iter[0]++] = 17201;
                    //logs[iter[0]++] = 18000 + 1;
                    //power[0] -= (short)random.NextInt(0, 4);
                    for (byte n = 0; n < 5; n++)
                    {
                        playersStat[arrByte * 280 + (n) * 28 + 0] += 3250;
                        if (playersStat[arrByte * 280 + (n) * 28 + 0] > 16000) playersStat[arrByte * 280 + (n) * 28 + 0] = 16000;
                        playersStat[arrByte * 280 + (n + 5) * 28 + 0] += (short)(1400 + (500 * teamStat[3]));
                        if (playersStat[arrByte * 280 + (n + 5) * 28 + 0] > 16000) playersStat[arrByte * 280 + (n + 5) * 28 + 0] = 16000;
                    }
                    if (teamStat[0] > 0) teamStat[0]--;
                    if (teamStat[3] < 4) teamStat[3]++;
                    if (vs[3] == 1 && vs[4] == 0)
                    {
                        playersStat[(arrByte * 280) + ((it[0]) * 28) + 9 + playersStat[(arrByte * 280) + ((it[0]) * 28) + 19]]++;
                    }
                }
                else if (vs[3] == 0 && vs[4] > 0)
                {
                    teamStat[4]++;
                    if (saveDemo[0]) logs[iter[0]++] = 17202;
                    //logs[iter[0]++] = 18000 + 2;
                    //power[0] += (short)random.NextInt(0, 4);
                    for (byte n = 0; n < 5; n++)
                    {
                        playersStat[arrByte * 280 + (n) * 28 + 0] += (short)(1400 + (500 * teamStat[0]));
                        if (playersStat[arrByte * 280 + (n) * 28 + 0] > 16000) playersStat[arrByte * 280 + (n) * 28 + 0] = 16000;
                        playersStat[arrByte * 280 + (n + 5) * 28 + 0] += 3250;
                        if (playersStat[arrByte * 280 + (n + 5) * 28 + 0] > 16000) playersStat[arrByte * 280 + (n + 5) * 28 + 0] = 16000;
                    }
                    if (teamStat[3] > 0) teamStat[3]--;
                    if (teamStat[0] < 4) teamStat[0]++;
                    if (vs[3] == 0 && vs[4] == 1)
                    {
                        playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 9 + playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 19]]++;
                    }
                }
                else
                {
                    arrBool = false;
                    if (vs[1] == 1)
                    {
                        if (vs[3] - vs[4] > 0 && random.NextInt(0, 100) < (vs[3] - vs[4]) * (sitePlant[0] * 3 + 3))
                        {
                            arrBool = true;
                        }
                    }
                    else if (vs[1] == 2)
                    {
                        if (vs[4] - vs[3] > 0 && random.NextInt(0, 100) < (vs[4] - vs[3]) * (sitePlant[0] * 3 + 3))
                        {
                            arrBool = true;
                        }
                    }
                    if (arrBool)
                    {
                        if (vs[3] > vs[4])
                        {
                            teamStat[1]++;
                            if (saveDemo[0]) logs[iter[0]++] = 17201;
                            //logs[iter[0]++] = 18000 + 1;
                            //power[0] -= (short)random.NextInt(0, 4);
                            for (byte n = 0; n < 5; n++)
                            {
                                playersStat[arrByte * 280 + (n) * 28 + 0] += 3250;
                                if (playersStat[arrByte * 280 + (n) * 28 + 0] > 16000) playersStat[arrByte * 280 + (n) * 28 + 0] = 16000;
                                playersStat[arrByte * 280 + (n + 5) * 28 + 0] += (short)(1400 + (500 * teamStat[3]));
                                if (playersStat[arrByte * 280 + (n + 5) * 28 + 0] > 16000) playersStat[arrByte * 280 + (n + 5) * 28 + 0] = 16000;
                            }
                            if (teamStat[0] > 0) teamStat[0]--;
                            if (teamStat[3] < 4) teamStat[3]++;
                            if (vs[3] == 1 && vs[4] == 0)
                            {
                                playersStat[(arrByte * 280) + ((it[0]) * 28) + 9 + playersStat[(arrByte * 280) + ((it[0]) * 28) + 19]]++;
                            }
                        }
                        else if (vs[4] > vs[3])
                        {
                            teamStat[4]++;
                            if (saveDemo[0]) logs[iter[0]++] = 17202;
                            //logs[iter[0]++] = 18000 + 2;
                            //power[0] += (short)random.NextInt(0, 4);
                            for (byte n = 0; n < 5; n++)
                            {
                                playersStat[arrByte * 280 + (n) * 28 + 0] += (short)(1400 + (500 * teamStat[0]));
                                if (playersStat[arrByte * 280 + (n) * 28 + 0] > 16000) playersStat[arrByte * 280 + (n) * 28 + 0] = 16000;
                                playersStat[arrByte * 280 + (n + 5) * 28 + 0] += 3250;
                                if (playersStat[arrByte * 280 + (n + 5) * 28 + 0] > 16000) playersStat[arrByte * 280 + (n + 5) * 28 + 0] = 16000;
                            }
                            if (teamStat[3] > 0) teamStat[3]--;
                            if (teamStat[0] < 4) teamStat[0]++;
                            if (vs[3] == 0 && vs[4] == 1)
                            {
                                playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 9 + playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 19]]++;
                            }
                        }
                        //else Debug.LogWarning("GG");
                    }
                }
                //if (!skip) yield return new WaitForSeconds(random.NextInt(0.25f, 2.0f));
            }
            //if (vs[3] == 5 && vs[4] == 5)
        }
        if (vs[1] == 2)
        {
            it[0] = (byte)random.NextInt((int)0, (int)5);
            it[1] = 0;
            //FirstFragT(ref it[0], ref it[1], 2, ref hp1, ref hp2, ref vs[3], ref vs[4]);
            vs[26] = (byte)random.NextInt(0, 100);
            //team2 is t
            if (vs[2] == 1)
            {
                if (vs[26] < 45 && hasRole[5])
                { for (it[1] = 0; it[1] < 5;) { if (roles[it[1] + 5] == 0) break; else ++it[1]; } }
                else if (vs[26] < 65 && hasRole[8])
                { for (it[1] = 0; it[1] < 5;) { if (roles[it[1] + 5] == 3) break; else ++it[1]; } }
                else if (vs[26] < 85 && hasRole[7])
                { for (it[1] = 0; it[1] < 5;) { if (roles[it[1] + 5] == 2) break; else ++it[1]; } }
                else if (vs[26] < 98 && hasRole[6])
                { for (it[1] = 0; it[1] < 5;) { if (roles[it[1] + 5] == 1) break; else ++it[1]; } }
                else it[1] = (byte)random.NextInt(0, 5);
            }
            //
            vs[15] = GetWeaponPoints(it[0], 1, arrByte, random.NextInt(7777 + difficulty[8]));
            vs[16] = GetWeaponPoints(it[1], 2, arrByte, random.NextInt(7777 + difficulty[8]));
            if (playerTeam[0] == 0) { vs[17] = 0; vs[18] = 0; }
            else if (playerTeam[0] == 1)
            {
                vs[17] = playerTeamBuff; vs[18] = opponentTeamBuff;
            }
            else if (playerTeam[0] == 2)
            {
                vs[18] = playerTeamBuff; vs[17] = opponentTeamBuff;
            }
            vs[19] = ct[it[0]] + (playersStat[(arrByte * 280) + ((it[0]) * 28) + 16] >= 4750 ||
                playersStat[(arrByte * 280) + ((it[0]) * 28) + 16] == 1700 ?
                awp[it[0]] : rifle[it[0]]) +
                (vs[2] == 1 ? entring[it[0]] : ct[it[0]]) + vs[15] + vs[17] +
                playersStat[(arrByte * 280) + ((it[0]) * 28) + 25] * 10 + playersStat[(arrByte * 280) + ((it[0]) * 28) + 26] * 15;
            vs[20] = t[it[1] + 5] + (playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 16] >= 4750 ||
                playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 16] == 1700 ?
                awp[it[1] + 5] : rifle[it[1] + 5]) + entring[it[1] + 5] + vs[16] + vs[18] +
                playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 25] * 10 + playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 26] * 15;
            randit[0] = random.NextInt(0, (int)((killing[it[0]] + killing[it[1] + 5]) * 1.6875));
            randit[1] = randit[0] - (int)(killing[it[0]] * 1.6875);
            vs[22] = (short)(-randit[1] + vs[19] + mapAdvantage[0]);
            vs[23] = (short)(randit[1] + vs[20] - mapAdvantage[0]);
            if (mapAdvantage[0] > 0 && random.NextInt(0, 100) < 85) mapAdvantage[0] -= 1;
            if (mapAdvantage[0] < 0 && random.NextInt(0, 100) < 85) mapAdvantage[0] += 1;
            if (vs[22] > vs[23])
            {
                //logs[iter[0]++] = (arrByte + 1) * 1000000000L + round * 1000000 + it[0] * 100000 +
                //    (it[1] + 5) * 10000 + playersStat[(arrByte * 280) + ((it[0]) * 28) + 16]; // t1 k t2 d
                if (saveDemo[0]) logs[iter[0]++] = 1000000 + it[0] * 100000 + (it[1] + 5) * 10000 + playersStat[(arrByte * 280) + ((it[0]) * 28) + 16];
                playersStat[(arrByte * 280) + ((it[0]) * 28) + 27] += vs[it[1] + 10];
                vs[it[1] + 10] = 0; vs[4]--;
                playersStat[(arrByte * 280) + ((it[0]) * 28) + 17]++;
                playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 18]++;
                if (random.NextInt(0, 100) < 60 && vs[it[0] + 5] == 100)
                {
                    vs[24] = random.NextInt(1, 100); // = 1 - 100
                    playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 27] += vs[24];
                    vs[it[0] + 5] -= vs[24];
                }
                playersStat[(arrByte * 280) + ((it[0]) * 28) + 0] += 300;
                if (playersStat[(arrByte * 280) + ((it[0]) * 28) + 0] > 16000) playersStat[(arrByte * 280) + ((it[0]) * 28) + 0] = 16000;
                playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 16] = 0;
                playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 25] = 0;
                playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 26] = 0;
                playersStat[(arrByte * 280) + ((it[0]) * 28) + 4]++; // entry
                aliveBool[it[1] + 5] = false;
            }
            else if (vs[22] < vs[23])
            {
                //logs[iter[0]++] = (arrByte + 1) * 1000000000L + round * 1000000 + (it[1] + 5) * 100000 +
                //    it[0] * 10000 + playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 16]; // t2 k t1 d
                if (saveDemo[0]) logs[iter[0]++] = 1000000 + (it[1] + 5) * 100000 + it[0] * 10000 + playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 16];
                playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 4]++; // entry
                playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 17]++;
                playersStat[(arrByte * 280) + ((it[0]) * 28) + 18]++;
                playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 0] += 300;
                if (playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 0] > 16000) playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 0] = 16000;
                playersStat[(arrByte * 280) + ((it[0]) * 28) + 16] = 0;
                playersStat[(arrByte * 280) + ((it[0]) * 28) + 25] = 0;
                playersStat[(arrByte * 280) + ((it[0]) * 28) + 26] = 0;
                if (random.NextInt(0, 100) < 60 && vs[it[1] + 10] == 100)
                {
                    vs[24] = random.NextInt(1, 100); // = 1 - 100
                    playersStat[(arrByte * 280) + ((it[0]) * 28) + 27] += vs[24];
                    vs[it[1] + 10] -= vs[24];
                }
                playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 27] += vs[it[0] + 5];
                vs[it[0] + 5] = 0; vs[3]--;
                aliveBool[it[0]] = false;
            }
            else
            {
                //logs[iter[0]++] = (arrByte + 1) * 1000000000L + round * 1000000 + it[0] * 100000 +
                //    (it[1] + 5) * 10000 + playersStat[(arrByte * 280) + ((it[0]) * 28) + 16]; // t1 k t2 d
                //logs[iter[0]++] = (arrByte + 1) * 1000000000L + round * 1000000 + (it[1] + 5) * 100000 +
                //    it[0] * 10000 + playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 16]; // t2 k t1 d
                if (saveDemo[0]) logs[iter[0]++] = 1000000 + it[0] * 100000 + (it[1] + 5) * 10000 + playersStat[(arrByte * 280) + ((it[0]) * 28) + 16];
                if (saveDemo[0]) logs[iter[0]++] = 1000000 + (it[1] + 5) * 100000 + it[0] * 10000 + playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 16];
                playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 4]++; // entry
                playersStat[(arrByte * 280) + ((it[0]) * 28) + 17]++;
                playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 18]++;
                playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 17]++;
                playersStat[(arrByte * 280) + ((it[0]) * 28) + 18]++;
                playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 0] += 300;
                if (playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 0] > 16000) playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 0] = 16000;
                playersStat[(arrByte * 280) + ((it[0]) * 28) + 16] = 0;
                playersStat[(arrByte * 280) + ((it[0]) * 28) + 0] += 300;
                if (playersStat[(arrByte * 280) + ((it[0]) * 28) + 0] > 16000) playersStat[(arrByte * 280) + ((it[0]) * 28) + 0] = 16000;
                playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 16] = 0;
                playersStat[(arrByte * 280) + ((it[0]) * 28) + 27] += vs[it[0] + 5];
                playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 27] += vs[it[1] + 10];
                playersStat[(arrByte * 280) + ((it[0]) * 28) + 25] = 0;
                playersStat[(arrByte * 280) + ((it[0]) * 28) + 26] = 0;
                playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 25] = 0;
                playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 26] = 0;
                vs[it[0] + 5] = 0; vs[3]--;
                vs[it[1] + 10] = 0; vs[4]--;
                aliveBool[it[1] + 5] = false;
                aliveBool[it[0]] = false;
            }
            while (vs[3] > 0 && vs[4] > 0 && !arrBool)
            {
                ///
                ///DEFAULTFRAG
                ///
                //byte rol = (byte)random.NextInt(0, 100);
                it[0] = arrrands[(int)((counterTo[0]++) % (uint)arrrands.Length)];
                it[1] = arrrands[(int)((counterTo[0]++) % (uint)arrrands.Length)];
                for (int z = 0; !(aliveBool[it[0]]) && z < 10; z++)
                {
                    it[0] = arrrands[(int)((counterTo[0]++) % (uint)arrrands.Length)];
                }
                if (!(aliveBool[it[0]]))
                {
                    for (byte z = 0; !(aliveBool[it[0]]) && z < 5; z++)
                    {
                        if (aliveBool[z]) { it[0] = z; break; }
                    }
                }
                for (int z = 0; !(aliveBool[it[1] + 5]) && z < 10; z++)
                {
                    it[1] = arrrands[(int)((counterTo[0]++) % (uint)arrrands.Length)];
                }
                if (!(aliveBool[it[1] + 5]))
                {
                    for (byte z = 0; !(aliveBool[it[1] + 5]) && z < 5; z++)
                    {
                        if (aliveBool[z]) { it[1] = z; break; }
                    }
                }
                if (vs[3] == 1 && vs[4] > 0)
                {
                    if (vs[4] > playersStat[(arrByte * 280) + ((it[0]) * 28) + 19]) playersStat[(arrByte * 280) + ((it[0]) * 28) + 19] = vs[4];
                }
                if (vs[3] > 0 && vs[4] == 1)
                {
                    if (vs[3] > playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 19]) playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 19] = vs[3];
                }
                //weapon
                vs[15] = GetWeaponPoints(it[0], 1, arrByte, random.NextInt(5005 + difficulty[8]));
                vs[16] = GetWeaponPoints(it[1], 2, arrByte, random.NextInt(5005 + difficulty[8]));
                if (playerTeam[0] == 0) { vs[17] = 0; vs[18] = 0; }
                else if (playerTeam[0] == 1)
                {
                    vs[17] = playerTeamBuff; vs[18] = opponentTeamBuff;
                }
                else if (playerTeam[0] == 2)
                {
                    vs[18] = playerTeamBuff; vs[17] = opponentTeamBuff;
                }
                //
                vs[19] = (vs[1] == 1 ? t[it[0]] : ct[it[0]]) +
                    (playersStat[(arrByte * 280) + ((it[0]) * 28) + 16] == 4750 ?
                    awp[it[0]] : rifle[it[0]]) +
                    (vs[3] == 1 ? clutching[it[0]] : killing[it[0]]) + vs[17] + vs[15] +
                    playersStat[(arrByte * 280) + ((it[0]) * 28) + 25] * 10 + playersStat[(arrByte * 280) + ((it[0]) * 28) + 26] * 15;
                vs[20] = (vs[1] == 1 ? ct[it[1] + 5] : t[it[1] + 5]) +
                    (playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 16] == 4750 ?
                    awp[it[1] + 5] : rifle[it[1] + 5]) +
                    (vs[4] == 1 ? clutching[it[1] + 5] : killing[it[1] + 5]) + vs[18] + vs[16] +
                    playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 25] * 10 + playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 26] * 15;
                randit[0] = random.NextInt(0,
                    (int)((killing[it[0]] * (vs[3] * 0.025 + 0.875) +
                    killing[it[1] + 5] * (vs[4] * 0.025 + 0.875))
                    * 1.6875));
                randit[1] = randit[0] - (int)(killing[it[0]] * (vs[3] * 0.025 + 0.875) * 1.6875);
                vs[22] = (short)(-randit[1] + vs[19] + mapAdvantage[0] - (playersStat[(arrByte * 280) + ((it[0]) * 28) + 16] < 4600 ? 0 : counterAWP[0]));
                vs[23] = (short)(randit[1] + vs[20] - mapAdvantage[0] - (playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 16] < 4600 ? 0 : counterAWP[1]));
                if (mapAdvantage[0] > 0 && random.NextInt(0, 100) < 85) mapAdvantage[0] -= 1;
                if (mapAdvantage[0] < 0 && random.NextInt(0, 100) < 85) mapAdvantage[0] += 1;
                if (vs[22] == vs[23] && (vs[3] == 1 || vs[4] == 1)) { if (random.NextInt(0, 100) < 50) vs[22]++; else vs[23]++; }
                if (vs[22] > vs[23])
                {
                    //if (playerTeam[0] == 2 && random.NextInt(0, 100) < buff[7]) continue;
                    if (saveDemo[0]) logs[iter[0]++] = 1000000 + it[0] * 100000 + (it[1] + 5) * 10000 + playersStat[(arrByte * 280) + ((it[0]) * 28) + 16];
                    //logs[iter[0]++] = (arrByte + 1) * 1000000000L + round * 1000000 + it[0] * 100000 +
                    //    (it[1] + 5) * 10000 + playersStat[(arrByte * 280) + ((it[0]) * 28) + 16]; // t1 k t2 d
                    playersStat[(arrByte * 280) + ((it[0]) * 28) + 27] += vs[it[1] + 10];
                    playersStat[(arrByte * 280) + ((it[0]) * 28) + 17]++;
                    playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 18]++;
                    if (random.NextInt(0, 100) < 60 && vs[it[0] + 5] == 100)
                    {
                        vs[24] = (byte)random.NextInt(1, 100);
                        playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 27] += vs[24];
                        vs[it[0] + 5] -= vs[24];
                    }
                    playersStat[(arrByte * 280) + ((it[0]) * 28) + 0] += 300;
                    if (playersStat[(arrByte * 280) + ((it[0]) * 28) + 0] > 16000) playersStat[(arrByte * 280) + ((it[0]) * 28) + 0] = 16000;
                    playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 16] = 0;
                    playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 25] = 0;
                    playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 26] = 0;
                    vs[it[1] + 10] = 0; vs[4]--;
                    aliveBool[it[1] + 5] = false;
                }
                else if (vs[22] < vs[23])
                {
                    //logs[iter[0]++] = (arrByte + 1) * 1000000000L + round * 1000000 + (it[1] + 5) * 100000 +
                    //    it[0] * 10000 + playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 16]; // t2 k t1 d
                    //if (playerTeam[0] == 1 && random.NextInt(0, 100) < buff[7]) continue;
                    if (saveDemo[0]) logs[iter[0]++] = 1000000 + (it[1] + 5) * 100000 + it[0] * 10000 + playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 16];
                    playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 17]++;
                    playersStat[(arrByte * 280) + ((it[0]) * 28) + 18]++;
                    playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 0] += 300;
                    if (playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 0] > 16000) playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 0] = 16000;
                    playersStat[(arrByte * 280) + ((it[0]) * 28) + 16] = 0;
                    playersStat[(arrByte * 280) + ((it[0]) * 28) + 25] = 0;
                    playersStat[(arrByte * 280) + ((it[0]) * 28) + 26] = 0;
                    if (random.NextInt(0, 100) < 60 && vs[it[1] + 10] == 100)
                    {
                        vs[24] = (byte)random.NextInt(1, 100);
                        playersStat[(arrByte * 280) + ((it[0]) * 28) + 27] += vs[24];
                        vs[it[1] + 10] -= vs[24];
                    }
                    playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 27] += vs[it[0] + 5];
                    vs[it[0] + 5] = 0; vs[3]--;
                    aliveBool[it[0]] = false;
                }
                else
                {
                    //logs[iter[0]++] = (arrByte + 1) * 1000000000L + round * 1000000 + it[0] * 100000 +
                    //    (it[1] + 5) * 10000 + playersStat[(arrByte * 280) + ((it[0]) * 28) + 16]; // t1 k t2 d
                    //logs[iter[0]++] = (arrByte + 1) * 1000000000L + round * 1000000 + (it[1] + 5) * 100000 +
                    //    it[0] * 10000 + playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 16]; // t2 k t1 d
                    if (saveDemo[0]) logs[iter[0]++] = 1000000 + it[0] * 100000 + (it[1] + 5) * 10000 + playersStat[(arrByte * 280) + ((it[0]) * 28) + 16];
                    if (saveDemo[0]) logs[iter[0]++] = 1000000 + (it[1] + 5) * 100000 + it[0] * 10000 + playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 16];
                    playersStat[(arrByte * 280) + ((it[0]) * 28) + 17]++;
                    playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 18]++;
                    playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 17]++;
                    playersStat[(arrByte * 280) + ((it[0]) * 28) + 18]++;
                    playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 0] += 300;
                    if (playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 0] > 16000) playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 0] = 16000;
                    playersStat[(arrByte * 280) + ((it[0]) * 28) + 16] = 0;
                    playersStat[(arrByte * 280) + ((it[0]) * 28) + 0] += 300;
                    if (playersStat[(arrByte * 280) + ((it[0]) * 28) + 0] > 16000) playersStat[(arrByte * 280) + ((it[0]) * 28) + 0] = 16000;
                    playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 16] = 0;
                    playersStat[(arrByte * 280) + ((it[0]) * 28) + 27] += vs[it[0] + 5];
                    playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 27] += vs[it[1] + 10];
                    playersStat[(arrByte * 280) + ((it[0]) * 28) + 25] = 0;
                    playersStat[(arrByte * 280) + ((it[0]) * 28) + 26] = 0;
                    playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 25] = 0;
                    playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 26] = 0;
                    vs[it[0] + 5] = 0; vs[3]--;
                    vs[it[1] + 10] = 0; vs[4]--;
                    aliveBool[it[1] + 5] = false;
                    aliveBool[it[0]] = false;
                }
                if (vs[3] > 0 && vs[4] == 0)
                {
                    teamStat[1]++;
                    if (saveDemo[0]) logs[iter[0]++] = 17201;
                    //logs[iter[0]++] = 18000 + 1;
                    //power[0] -= (short)random.NextInt(0, 4);
                    for (byte n = 0; n < 5; n++)
                    {
                        playersStat[arrByte * 280 + (n) * 28 + 0] += 3250;
                        if (playersStat[arrByte * 280 + (n) * 28 + 0] > 16000) playersStat[arrByte * 280 + (n) * 28 + 0] = 16000;
                        playersStat[arrByte * 280 + (n + 5) * 28 + 0] += (short)(1400 + (500 * teamStat[3]));
                        if (playersStat[arrByte * 280 + (n + 5) * 28 + 0] > 16000) playersStat[arrByte * 280 + (n + 5) * 28 + 0] = 16000;
                    }
                    if (teamStat[0] > 0) teamStat[0]--;
                    if (teamStat[3] < 4) teamStat[3]++;
                    if (vs[3] == 1 && vs[4] == 0)
                    {
                        playersStat[(arrByte * 280) + ((it[0]) * 28) + 9 + playersStat[(arrByte * 280) + ((it[0]) * 28) + 19]]++;
                    }
                }
                else if (vs[3] == 0 && vs[4] > 0)
                {
                    teamStat[4]++;
                    if (saveDemo[0]) logs[iter[0]++] = 17202;
                    //logs[iter[0]++] = 18000 + 2;
                    //power[0] += (short)random.NextInt(0, 4);
                    for (byte n = 0; n < 5; n++)
                    {
                        playersStat[arrByte * 280 + (n) * 28 + 0] += (short)(1400 + (500 * teamStat[0]));
                        if (playersStat[arrByte * 280 + (n) * 28 + 0] > 16000) playersStat[arrByte * 280 + (n) * 28 + 0] = 16000;
                        playersStat[arrByte * 280 + (n + 5) * 28 + 0] += 3250;
                        if (playersStat[arrByte * 280 + (n + 5) * 28 + 0] > 16000) playersStat[arrByte * 280 + (n + 5) * 28 + 0] = 16000;
                    }
                    if (teamStat[3] > 0) teamStat[3]--;
                    if (teamStat[0] < 4) teamStat[0]++;
                    if (vs[3] == 0 && vs[4] == 1)
                    {
                        playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 9 + playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 19]]++;
                    }
                }
                else
                {
                    arrBool = false;
                    if (vs[1] == 1)
                    {
                        if (vs[3] - vs[4] > 0 && random.NextInt(0, 100) < (vs[3] - vs[4]) * (sitePlant[0] * 3 + 3))
                        {
                            arrBool = true;
                        }
                    }
                    else if (vs[1] == 2)
                    {
                        if (vs[4] - vs[3] > 0 && random.NextInt(0, 100) < (vs[4] - vs[3]) * (sitePlant[0] * 3 + 3))
                        {
                            arrBool = true;
                        }
                    }
                    if (arrBool)
                    {
                        if (vs[3] > vs[4])
                        {
                            teamStat[1]++;
                            if (saveDemo[0]) logs[iter[0]++] = 17201;
                            //logs[iter[0]++] = 18000 + 1;
                            //power[0] -= (short)random.NextInt(0, 4);
                            for (byte n = 0; n < 5; n++)
                            {
                                playersStat[arrByte * 280 + (n) * 28 + 0] += 3250;
                                if (playersStat[arrByte * 280 + (n) * 28 + 0] > 16000) playersStat[arrByte * 280 + (n) * 28 + 0] = 16000;
                                playersStat[arrByte * 280 + (n + 5) * 28 + 0] += (short)(1400 + (500 * teamStat[3]));
                                if (playersStat[arrByte * 280 + (n + 5) * 28 + 0] > 16000) playersStat[arrByte * 280 + (n + 5) * 28 + 0] = 16000;
                            }
                            if (teamStat[0] > 0) teamStat[0]--;
                            if (teamStat[3] < 4) teamStat[3]++;
                            if (vs[3] == 1 && vs[4] == 0)
                            {
                                playersStat[(arrByte * 280) + ((it[0]) * 28) + 9 + playersStat[(arrByte * 280) + ((it[0]) * 28) + 19]]++;
                            }
                        }
                        else if (vs[4] > vs[3])
                        {
                            teamStat[4]++;
                            if (saveDemo[0]) logs[iter[0]++] = 17202;
                            //logs[iter[0]++] = 18000 + 2;
                            //power[0] += (short)random.NextInt(0, 4);
                            for (byte n = 0; n < 5; n++)
                            {
                                playersStat[arrByte * 280 + (n) * 28 + 0] += (short)(1400 + (500 * teamStat[0]));
                                if (playersStat[arrByte * 280 + (n) * 28 + 0] > 16000) playersStat[arrByte * 280 + (n) * 28 + 0] = 16000;
                                playersStat[arrByte * 280 + (n + 5) * 28 + 0] += 3250;
                                if (playersStat[arrByte * 280 + (n + 5) * 28 + 0] > 16000) playersStat[arrByte * 280 + (n + 5) * 28 + 0] = 16000;
                            }
                            if (teamStat[3] > 0) teamStat[3]--;
                            if (teamStat[0] < 4) teamStat[0]++;
                            if (vs[3] == 0 && vs[4] == 1)
                            {
                                playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 9 + playersStat[(arrByte * 280) + ((it[1] + 5) * 28) + 19]]++;
                            }
                        }
                        //else Debug.LogWarning("GG");
                    }
                }
                //if (!skip) yield return new WaitForSeconds(random.NextInt(0.25f, 2.0f));
            }
        }
        aliveBool.Dispose();
        //
        ///
        //
    }
    /*
    private int GetWeaponPoints(int i)
    {
        Unity.Mathematics.Random rand = new Unity.Mathematics.Random((uint)(seed[0] + 10001));
        switch ((int)i)
        {
            case 0: return rand.NextInt(0, 12) + weaponOffsets[0];
            case 700: return rand.NextInt(0, 100) + weaponOffsets[1];
            case 1250: return rand.NextInt(23, 51) + weaponOffsets[2];
            case 1100: return rand.NextInt(23, 51) + weaponOffsets[3];
            case 1700: return rand.NextInt(25, 81) + weaponOffsets[4];
            case 1800: return rand.NextInt(40, 66) + weaponOffsets[5];
            case 3300: return rand.NextInt(55, 86) + weaponOffsets[6];
            case 3000: return rand.NextInt(55, 86) + weaponOffsets[7];
            case 2700: return rand.NextInt(55, 86) + weaponOffsets[8];
            case 3100: return rand.NextInt(55, 86) + weaponOffsets[9];
            case 2900: return rand.NextInt(55, 86) + weaponOffsets[10];
            case 4750: return rand.NextInt(70, 106) + weaponOffsets[11];
            case 5000: return rand.NextInt(50, 119) + weaponOffsets[12];
            case 2000: return rand.NextInt(30, 74) + weaponOffsets[13];
            case 2050: return rand.NextInt(40, 66) + weaponOffsets[14]; //famas
        }
        return 0;
    }*/

    private int GetWeaponPoints(byte i, byte team, short map, int rand)
    {
        /*DefaultPistol = 0, Deagle = 700, MP9 = 1250, MAC10 = 1100, SSG = 1700, GALIL = 1800, AUG = 3300, 
        SG = 3000, AK47 = 2700, M4A4 = 3100, M4A1S = 2900, AWP = 4750, AUTOSNIPER = 5000, XM = 2000,
        Famas = 2050, Molotov = 600, HEGrenade = 300*/
        switch (playersStat[map * 280 + (team == 1 ? i : i + 5) * 28 + 16])
        {
            case 0: return rand % 12 + 0 + weaponOffsets[0];
            case 700: return rand % 100 + 0 + weaponOffsets[1];
            case 1250: return rand % 28 + 23 + weaponOffsets[2];
            case 1100: return rand % 28 + 23 + weaponOffsets[3];
            case 1700: return rand % 56 + 25 + weaponOffsets[4];
            case 1800: return rand % 26 + 40 + weaponOffsets[5];
            case 3300: return rand % 31 + 55 + weaponOffsets[6];
            case 3000: return rand % 31 + 55 + weaponOffsets[7];
            case 2700: return rand % 31 + 55 + weaponOffsets[8];
            case 3100: return rand % 31 + 55 + weaponOffsets[9];
            case 2900: return rand % 31 + 55 + weaponOffsets[10];
            case 4750: return rand % 36 + 70 + weaponOffsets[11];
            case 5000: return rand % 69 + 50 + weaponOffsets[12];
            case 2000: return rand % 44 + 30 + weaponOffsets[13];
            case 2050: return rand % 26 + 40 + weaponOffsets[14]; //famas
        }
        return 0;
    }
}

[System.Serializable]
public struct MapResult
{
    public byte[] result;
    public List<Stat> stats;
    public int day;
}

[System.Serializable]
public struct PlayerInfo
{
    public string nickname, teamName;
    public byte nationality, language, age, activity, role, awp, rifle, ct, t, clutching, entring, 
        killing, mvp, evp, strength, chemistry, morale, form;
    public short progression, daysInTeam;
    public int salary, daysPayed;
    public int[] stats;
    public List<Stat> playerStats;
}

[System.Serializable]
public struct TeamInfo
{
    public string teamName, taskname;
    public int money, currentTournament, jersey, sticker, poster, taskmoney, followers, leaguePlayers,
        tasksellJersey, tasksoldJersey, tasksellPoster, tasksoldPoster, tasksellSticker, tasksoldSticker;
    public short points, majorPoints;
    public byte place3rd, place2nd, place1st, energy, mapPoints, factory,
        playMatches, winMatches, throwMatches, playedMatches, wonMatches, thrownMatches,
        form, chemistry, morale;
    public bool entry, sniper, igl, lurk, support;
    public byte[] maps;
}

public class MatchSave
{
    public static bool isLoading = false;
    public static int GetInt(string key, int defaultValue)
    {
        return PlayerPrefs.GetInt(key + "save" + Manager.save.ToString(), defaultValue);
    }

    public static int GetInt(string key)
    {
        return PlayerPrefs.GetInt(key + "save" + Manager.save.ToString());
    }

    public static string GetString(string key, string defaultValue)
    {
        return PlayerPrefs.GetString(key + "save" + Manager.save.ToString(), defaultValue);
    }

    public static string GetString(string key)
    {
        return PlayerPrefs.GetString(key + "save" + Manager.save.ToString());
    }

    public static void SetInt(string key, int value)
    {
        PlayerPrefs.SetInt(key + "save" + Manager.save.ToString(), value);
    }

    public static void SetString(string key, string value)
    {
        PlayerPrefs.SetString(key + "save" + Manager.save.ToString(), value);
    }

    public static bool HasKey(string key)
    {
        return PlayerPrefs.HasKey(key + "save" + Manager.save.ToString());
    }

    public static void DeleteKey(string key)
    {
        PlayerPrefs.DeleteKey(key + "save" + Manager.save.ToString());
    }

    public static Bootcamp SaveBootcamp(Bootcamp bootcamp)
    {
        if (bootcamp == null) return null;
        return Manager.mainInstance.GetBootcamps().bootcamps[bootcamp.id];
    }

    public static Bootcamp LoadBootcamp()
    {
        if (SaveManager.SaveExists("bootcamp"))
        {
            Bootcamp bootcamp = SaveManager.Load<Bootcamp>("bootcamp");
            Manager.mainInstance.GetBootcamps().bootcamps[bootcamp.id] = bootcamp;
            return Manager.mainInstance.GetBootcamps().bootcamps[bootcamp.id];
        }
        else return null;
    }

    public static int Save(Match match)
    {
        isLoading = true;
        int indexer = SaveManager.Load<int>("index");
        //SetInt("day" + indexer.ToString(), match.day);
        //SaveManager.Save<int>(match.day, "day" + indexer.ToString());
        MapResult mapResult = new MapResult();
        mapResult.day = match.day;
        mapResult.result = new byte[16];
        if (match.type == Manager.MatchType.BO1)
        {
            mapResult.result[10] = match.pickbans[6];
            mapResult.result[0] = match.maproundst1[0];
            mapResult.result[5] = match.maproundst2[0];
            mapResult.result[15] = 1;
        }
        if (match.type == Manager.MatchType.BO3)
        {
            mapResult.result[10] = match.pickbans[2];
            mapResult.result[0] = match.maproundst1[0];
            mapResult.result[5] = match.maproundst2[0];
            mapResult.result[11] = match.pickbans[3];
            mapResult.result[1] = match.maproundst1[1];
            mapResult.result[6] = match.maproundst2[1];
            mapResult.result[15] = 2;
            if (match.playerStats.Count > 20) 
            {
                mapResult.result[12] = match.pickbans[6];
                mapResult.result[2] = match.maproundst1[2];
                mapResult.result[7] = match.maproundst2[2];
                mapResult.result[15] = 3;
            }
        }
        if (match.type == Manager.MatchType.BO5)
        {
            mapResult.result[10] = match.pickbans[2];
            mapResult.result[0] = match.maproundst1[0];
            mapResult.result[5] = match.maproundst2[0];
            mapResult.result[11] = match.pickbans[3];
            mapResult.result[1] = match.maproundst1[1];
            mapResult.result[6] = match.maproundst2[1];
            mapResult.result[12] = match.pickbans[4];
            mapResult.result[2] = match.maproundst1[2];
            mapResult.result[7] = match.maproundst2[2];
            mapResult.result[15] = 3;
            if (match.playerStats.Count > 30) 
            {
                mapResult.result[13] = match.pickbans[5];
                mapResult.result[3] = match.maproundst1[3];
                mapResult.result[8] = match.maproundst2[3];
                mapResult.result[15] = 4;
            }
            if (match.playerStats.Count > 40) 
            {
                mapResult.result[14] = match.pickbans[6];
                mapResult.result[4] = match.maproundst1[4];
                mapResult.result[9] = match.maproundst2[4];
                mapResult.result[15] = 5;
            }
        }
        //SaveManager.Save<MapResult>(mapResult, "matchres" + indexer.ToString());
        mapResult.stats = match.playerStats;
        Manager.mainInstance.tempResults.Add(mapResult);
        /*for (byte i = 0; i < match.playerStats.Count; i++)
        {
            SaveManager.Save<Stat>(match.playerStats[i], i.ToString() + match.playerStats[i].nickname + indexer.ToString());
        }*/
        SaveManager.Save<int>(indexer + 1, "index");
        //PlayerPrefs.Save();
        isLoading = false;
        return indexer;
    }

    public static int Save(Match match, byte[] maproundst)
    {
        isLoading = true;
        int indexer = SaveManager.Load<int>("index");
        //SetInt("day" + indexer.ToString(), match.day);
        //SaveManager.Save<int>(match.day, "day" + indexer.ToString());
        MapResult mapResult = new MapResult();
        mapResult.day = match.day;
        mapResult.result = new byte[16];
        //SetInt("0rounds" + indexer.ToString(), match.playerStats[0].rounds);
        //if (match.playerStats.Count > 10) SetInt("1rounds" + indexer.ToString(), match.playerStats[10].rounds);
        //if (match.playerStats.Count > 20) SetInt("2rounds" + indexer.ToString(), match.playerStats[20].rounds);
        //if (match.playerStats.Count > 30) SetInt("3rounds" + indexer.ToString(), match.playerStats[30].rounds);
        //if (match.playerStats.Count > 40) SetInt("4rounds" + indexer.ToString(), match.playerStats[40].rounds);
        if (match.type == Manager.MatchType.BO1)
        {
            mapResult.result[10] = match.pickbans[6];
            mapResult.result[0] = maproundst[0];
            mapResult.result[5] = maproundst[5];
            mapResult.result[15] = 1;
        }
        if (match.type == Manager.MatchType.BO3)
        {
            mapResult.result[10] = match.pickbans[2];
            mapResult.result[0] = maproundst[0];
            mapResult.result[5] = maproundst[5];
            mapResult.result[11] = match.pickbans[3];
            mapResult.result[1] = maproundst[1];
            mapResult.result[6] = maproundst[6];
            mapResult.result[15] = 2;
            if (match.playerStats.Count > 20)
            {
                mapResult.result[12] = match.pickbans[6];
                mapResult.result[2] = maproundst[2];
                mapResult.result[7] = maproundst[7];
                mapResult.result[15] = 3;
            }
        }
        if (match.type == Manager.MatchType.BO5)
        {
            mapResult.result[10] = match.pickbans[2];
            mapResult.result[0] = maproundst[0];
            mapResult.result[5] = maproundst[5];
            mapResult.result[11] = match.pickbans[3];
            mapResult.result[1] = maproundst[1];
            mapResult.result[6] = maproundst[6];
            mapResult.result[12] = match.pickbans[4];
            mapResult.result[2] = maproundst[2];
            mapResult.result[7] = maproundst[7];
            mapResult.result[15] = 3;
            if (match.playerStats.Count > 30)
            {
                mapResult.result[13] = match.pickbans[5];
                mapResult.result[3] = maproundst[3];
                mapResult.result[8] = maproundst[8];
                mapResult.result[15] = 4;
            }
            if (match.playerStats.Count > 40)
            {
                mapResult.result[14] = match.pickbans[6];
                mapResult.result[4] = maproundst[4];
                mapResult.result[9] = maproundst[9];
                mapResult.result[15] = 5;
            }
        }
        mapResult.stats = match.playerStats;
        //SaveManager.Save<MapResult>(mapResult, "matchres" + indexer.ToString());
        /*for (byte i = 0; i < match.playerStats.Count; i++)
        {
            SaveManager.Save<Stat>(match.playerStats[i], i.ToString() + match.playerStats[i].nickname + indexer.ToString());
        }*/
        Manager.mainInstance.tempResults.Add(mapResult);
        SaveManager.Save<int>(indexer + 1, "index");
        //PlayerPrefs.Save();
        isLoading = false;
        return indexer;
    }

    public static Stat[] LoadStats(int indexer, string _nickname)
    {
        MapResult result = Manager.mainInstance.mapResults[indexer];
        byte j = 0;
        for (; j < 10; j++)
        {
            if (result.stats[j].nickname == _nickname) break;
        }
        Stat[] stats = new Stat[6];
        int t = result.stats.Count;
        for (byte i = 0; i * 10 + j < t; i++) stats[i] = result.stats[i * 10 + j];
        return stats;
    }

    //public static int GetDay(int indexer)
    //{
    //    return SaveManager.Load<int>("day" + indexer.ToString());
    //}

    public static byte[] LoadMapResults(int indexer)
    {
        return Manager.mainInstance.mapResults[indexer].result;
    }

    public static TournamentInfo SaveTournament(Tournament tournament, int indexer)
    {
        //todo
        TournamentInfo tournamentInfo = new TournamentInfo();
        tournamentInfo.title = tournament.title;
        tournamentInfo.btype = tournament.btype;
        tournamentInfo.type = tournament.type;
        tournamentInfo.tier = tournament.tier;
        tournamentInfo.indexerStart = tournament.indexerStart;
        tournamentInfo.state = tournament.EventState();
        tournamentInfo.prizePool = tournament.prizePool;
        tournamentInfo.open = tournament.open;
        tournamentInfo.day = tournament.day;
        tournamentInfo.isCalled = tournament.isCalled;
        tournamentInfo.unlockEvent = tournament.unlockEvent;
        tournamentInfo.logo = tournament.logo.name;
        tournamentInfo.majorPoints = tournament.majorPoints;
        tournamentInfo.isMajor = tournament.isMajor;
        Team[] te = tournament.GetTeams();
        if (te != null)
        {
            tournamentInfo.placements = new string[te.Length];
            for (byte i = 0; i < te.Length; i++)
            {
                if (te[i] == null) { tournamentInfo.placements[i] = "null"; continue; }
                tournamentInfo.placements[i] = te[i].teamName;
            }
        }
        if (tournament.matches != null)
        {
            MatchSettingInfo[] matchSettingInfo = new MatchSettingInfo[tournament.matches.Length];
            for (int i = 0; i < tournament.matches.Length; i++)
            {
                matchSettingInfo[i] = new MatchSettingInfo();
                matchSettingInfo[i].winner = tournament.matches[i].winner;
                matchSettingInfo[i].type = (byte)tournament.matches[i].type;
                matchSettingInfo[i].day = tournament.matches[i].day;
                if (tournament.matches[i].team2 != null)
                    matchSettingInfo[i].team2 = tournament.matches[i].team2.teamName;
                else matchSettingInfo[i].team2 = string.Empty;
                if (tournament.matches[i].team1 != null)
                    matchSettingInfo[i].team1 = tournament.matches[i].team1.teamName;
                else matchSettingInfo[i].team1 = string.Empty;
                matchSettingInfo[i].title = tournament.matches[i].title;
                matchSettingInfo[i].onLose = tournament.matches[i].onLose;
                matchSettingInfo[i].onWin = tournament.matches[i].onWin;
            }
            tournamentInfo.matchSettingInfo = matchSettingInfo;
        }
        else
        {
            tournamentInfo.matchSettingInfo = new MatchSettingInfo[1];
            tournamentInfo.matchSettingInfo[0] = new MatchSettingInfo();
            tournamentInfo.matchSettingInfo[0].team1 = string.Empty;
            tournamentInfo.matchSettingInfo[0].team2 = string.Empty;
            tournamentInfo.matchSettingInfo[0].type = 0;
            tournamentInfo.matchSettingInfo[0].winner = 0;
            tournamentInfo.matchSettingInfo[0].day = 0;
            tournamentInfo.matchSettingInfo[0].x = 0;
            tournamentInfo.matchSettingInfo[0].y = 0;
            tournamentInfo.matchSettingInfo[0].onWin = string.Empty;
            tournamentInfo.matchSettingInfo[0].title = string.Empty;
            tournamentInfo.matchSettingInfo[0].onLose = string.Empty;
        }
        return tournamentInfo;
    }

    public static void SaveTournaments()
    {
        List<Tournament> t = Events.events.GetTournaments();
        List<TournamentInfo> result = new List<TournamentInfo>(120);
        for (int i = 0; i < t.Count; i++) result.Add(SaveTournament(t[i], i));
        Manager.mainInstance.tourInfo = result;
        //SaveManager.Save(result, "tournamentslist");
    }

    public static void LoadTournaments()
    {
        List<TournamentInfo> result = Manager.mainInstance.tourInfo; //SaveManager.Load<List<TournamentInfo>>("tournamentslist");
        List<Tournament> t = new List<Tournament>(120);
        for (int i = 0; i < result.Count; i++) t.Add(LoadTournament(result, i));
        Events.events.SetTournaments(t);
    }

    public static Tournament LoadTournament(List<TournamentInfo> tInfo, int indexer)
    {
        TournamentInfo tournamentInfo = tInfo[indexer];
        Tournament tournament = new Tournament(tournamentInfo.state);
        tournament.title = tournamentInfo.title;
        tournament.btype = tournamentInfo.btype;
        tournament.type = tournamentInfo.type;
        tournament.tier = tournamentInfo.tier;
        tournament.indexerStart = tournamentInfo.indexerStart;
        //tournamentInfo.state = tournament.EventState();
        tournament.prizePool = tournamentInfo.prizePool;
        tournament.open = tournamentInfo.open;
        tournament.day = tournamentInfo.day;
        tournament.isCalled = tournamentInfo.isCalled;
        tournament.unlockEvent = tournamentInfo.unlockEvent;
        tournament.majorPoints = tournamentInfo.majorPoints;
        tournament.isMajor = tournamentInfo.isMajor;
        tournament.logo = Manager.mainInstance.LogoSprite(tournamentInfo.logo);
        if (tournamentInfo.placements != null)
        {
            Team[] te = new Team[tournamentInfo.placements.Length];
            for (byte i = 0; i < te.Length; i++)
                te[i] = Manager.mainInstance.GetTeams().GetTeam(tournamentInfo.placements[i]);
            tournament.SetTeams(te);
        }
        if (tournamentInfo.matchSettingInfo.Length == 1)
        {
            tournament.matches = null;
        }
        else
        {
            MatchSetting[] matchSettings = new MatchSetting[tournamentInfo.matchSettingInfo.Length];
            for (int i = 0; i < matchSettings.Length; i++)
            {
                matchSettings[i] = new MatchSetting();
                matchSettings[i].winner = tournamentInfo.matchSettingInfo[i].winner;
                matchSettings[i].type = (Manager.MatchType)tournamentInfo.matchSettingInfo[i].type;
                matchSettings[i].day = tournamentInfo.matchSettingInfo[i].day;
                if (tournamentInfo.matchSettingInfo[i].team2 != string.Empty)
                    matchSettings[i].team2 = Manager.mainInstance.GetTeams().GetTeam(tournamentInfo.matchSettingInfo[i].team2);
                else matchSettings[i].team2 = null;
                if (tournamentInfo.matchSettingInfo[i].team1 != string.Empty)
                    matchSettings[i].team1 = Manager.mainInstance.GetTeams().GetTeam(tournamentInfo.matchSettingInfo[i].team1);
                else matchSettings[i].team1 = null;
                matchSettings[i].title = tournamentInfo.matchSettingInfo[i].title;
                matchSettings[i].onLose = tournamentInfo.matchSettingInfo[i].onLose;
                matchSettings[i].onWin = tournamentInfo.matchSettingInfo[i].onWin;
                matchSettings[i].x = tournamentInfo.matchSettingInfo[i].x;
                matchSettings[i].y = tournamentInfo.matchSettingInfo[i].y;
            }
            tournament.matches = matchSettings;
        }
        return tournament;
    }
}

[System.Serializable]
public struct TournamentInfo
{
    public string title, type, logo;
    public int day, prizePool, indexerStart;
    public MatchSettingInfo[] matchSettingInfo;
    public string[] placements;
    public byte state;
    public byte tier, btype, open, majorPoints;
    public int unlockEvent;
    public bool isCalled, isMajor;
}

[System.Serializable]
public struct TeamGroupInfo
{
    public string[] teams;
    public string[] matches;
    public byte[] points;
}

[System.Serializable]
public struct TeamGroup
{
    public Team[] teams;
    public MatchSetting[] matches;
    public byte[] points;
}

[System.Serializable]
public struct MatchSettingInfo
{
    public string title, team1, team2;
    public string onLose, onWin;
    public byte type, winner;
    public int day;
    public int x, y;
}

[System.Serializable]
public struct MatchSetting
{
    public Team team1, team2;
    public Manager.MatchType type;
    public string onLose, onWin, title;
    public int day, x, y;
    public byte winner;
    //public int[] index;

    public static bool operator ==(MatchSetting m1, MatchSetting m2)
    {
        if (m1.day == m2.day && m1.team1 == m2.team1 && m1.team2 == m2.team2) return true; else return false;
    }
    public static bool operator !=(MatchSetting m1, MatchSetting m2)
    {
        if (m1.day == m2.day && m1.team1 == m2.team1 && m1.team2 == m2.team2) return false; else return true;
    }

    public override bool Equals(object obj)
    {
        if (day == ((MatchSetting)obj).day && team1 == ((MatchSetting)obj).team1 &&
            team2 == ((MatchSetting)obj).team2) return true; else return false;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public override string ToString()
    {
        return team1.teamName + " vs " + team2.teamName + ", BO" + ((int)type * 2 + 1).ToString();
    }

    public MatchSetting(Team team1, Team team2, Manager.MatchType type, int day)
    {
        this.team1 = team1;
        this.team2 = team2;
        this.type = type;
        this.day = day;
        this.onLose = string.Empty;
        this.onWin = string.Empty;
        this.title = string.Empty;
        x = 0;
        y = 0;
        winner = 0;
        //index = new int[] { -1 };
    }

    public MatchSetting(Team team1, Team team2, Manager.MatchType type, int day, string onLose, string onWin, string title)
    {
        this.team1 = team1;
        this.team2 = team2;
        this.type = type;
        this.day = day;
        this.onLose = onLose;
        this.onWin = onWin;
        this.title = title;
        x = 0;
        y = 0;
        winner = 0;
        //index = new int[] { -1 };
    }
}

[System.Serializable]
public class Tournament
{
    public string title, type;
    public int day, prizePool, indexerStart;
    public Sprite logo;
    private Team[] placements;
    public Team[] GetTeams() { return placements; }
    public void SetTeams(Team[] teams) { placements = teams; }
    public MatchSetting[] matches;
    private byte state = 0;
    public byte tier, btype, open, majorPoints;
    public int unlockEvent;
    public byte EventState() { return state; }
    public bool isCalled = false, isMajor = false;
    //tier
    //0 major, 1 tier1, 2 tier2, 3 online
    //mode
    //0 default 4x4 then singleelim with 8 teams
    //1 qualifier (only groups) 8x4
    public void GenerateTournament(List<Team> teamFill, byte type, byte tier)
    {
        indexerStart = SaveManager.Load<int>("index");
        btype = type;
        this.tier = tier;
        Teams teams = Manager.mainInstance.GetTeams();
        List<byte> indexers = new List<byte>(32);
        int _i = Events.events.GetTournamentIndex(this);
        TournamentStructure structure = Modding.modding.modSave.structure[btype];
        matches = new MatchSetting[structure.matchSettings.Length];
        placements = new Team[structure.teamPool];
        List<Team> tfill = new List<Team>(structure.teamPool);
        for (int i = 0; i < structure.teamPool; i++)
        {
            tfill.Add(teams.GetTeam(teamFill[i].teamName)); 
        }
        List<Team> teamPool = new List<Team>(tfill.Count);
        tfill = tfill.OrderBy(x => x.forSort = (byte)Random.Range(0, 255)).ToList();
        for (int i = 0; i < structure.teamPool; i++)
            teamPool.Add(teams.GetTeam(tfill[i].teamName));
        for (int i = 0; i < matches.Length; i++)
        {
            //(Team team1, Team team2, Manager.MatchType type, int day, string onLose, string onWin, string title)
            int p1 = -1, p2 = -1;
            if (structure.matchSettings[i].team1.Substring(0, 4) == "pool")
                p1 = System.Int32.Parse(structure.matchSettings[i].team1.Substring(4, 3));
            if (structure.matchSettings[i].team2.Substring(0, 4) == "pool")
                p2 = System.Int32.Parse(structure.matchSettings[i].team2.Substring(4, 3));
            matches[i] = new MatchSetting(
                p1 == -1 ? null : teams.GetTeam(teamPool[p1].teamName),
                p2 == -1 ? null : teams.GetTeam(teamPool[p2].teamName),
                (Manager.MatchType)structure.matchSettings[i].type,
                day + structure.matchSettings[i].day,
                structure.matchSettings[i].onLose,
                structure.matchSettings[i].onWin,
                structure.matchSettings[i].title
                );
        }
        for (int i = 0; i < matches.Length; i++)
        {
            if (matches[i].team1 == null || matches[i].team2 == null) continue;
            teams.GetTeam(matches[i].team1.teamName).currentTournament = _i;
            teams.GetTeam(matches[i].team2.teamName).currentTournament = _i;
        }
        isCalled = true;
    }
    public void GenerateTournament(bool isPlaying, Team regTeam, byte type, byte tier, bool decision = true)
    {
        indexerStart = SaveManager.Load<int>("index");
        btype = type;
        this.tier = tier;
        Teams teams = Manager.mainInstance.GetTeams();
        List<byte> indexers = new List<byte>(32);
        int _i = Events.events.GetTournamentIndex(this);
        TournamentStructure structure = Modding.modding.modSave.structure[btype];
        matches = new MatchSetting[structure.matchSettings.Length];
        placements = new Team[structure.teamPool];
        int[] range = structure.range;
        Team[] teamPool = new Team[range.Length];
        if (tier == 2) for (int i = 0; i < range.Length; i++)
            { range[i] *= 2; if (range[i] >= teams.teams.Count - 1) range[i] = teams.teams.Count - 1; }
        if (tier == 3) for (int i = 0; i < range.Length; i++) range[i] = teams.teams.Count - 1;
        int max = range.Max();
        if (max > teams.teams.Count - 1) max = teams.teams.Count - 1;
        if (!isMajor)
        {
            for (int i = 0; i < range.Length; i++)
            {
                byte j;
                byte tr = 0;
                if (range[i] > teams.teams.Count - 1) range[i] = teams.teams.Count - 1;
                do
                {
                    do
                    {
                        if (tr++ < 10) j = (byte)Random.Range(0, range[i]);
                        else j = (byte)Random.Range(0, max);
                    } while (teams.GetTeamPlacement(j).teamName == "Free");
                } while (indexers.Contains(j) || teams.GetTeamPlacement(j).players.Count < 5);
                teamPool[i] = teams.GetTeamPlacement(j);
                indexers.Add(j);
            }
        }
        else
        {
            string[] t_t = new string[16];
            t_t = teams.GetMajorTeams();
            for (int i = 0; i < range.Length; i++)
                teamPool[i] = teams.GetTeam(t_t[i]);
        }
        for (int i = 0; i < matches.Length; i++)
        {
            //(Team team1, Team team2, Manager.MatchType type, int day, string onLose, string onWin, string title)
            int p1 = -1, p2 = -1;
            if (structure.matchSettings[i].team1.Substring(0, 4) == "pool")
                p1 = System.Int32.Parse(structure.matchSettings[i].team1.Substring(4, 3));
            if (structure.matchSettings[i].team2.Substring(0, 4) == "pool")
                p2 = System.Int32.Parse(structure.matchSettings[i].team2.Substring(4, 3));
            matches[i] = new MatchSetting(
                p1 == -1 ? null : teams.GetTeam(teamPool[p1].teamName),
                p2 == -1 ? null : teams.GetTeam(teamPool[p2].teamName),
                (Manager.MatchType)structure.matchSettings[i].type,
                day + structure.matchSettings[i].day,
                structure.matchSettings[i].onLose,
                structure.matchSettings[i].onWin,
                structure.matchSettings[i].title
                );
        }
        if (isPlaying)
        {
            bool a = false;
            for (int i = 0; i < matches.Length; i++)
            {
                if (matches[i].team1 != null) if (matches[i].team1.teamName == regTeam.teamName) { a = true; break; }
                if (matches[i].team2 != null) if (matches[i].team2.teamName == regTeam.teamName) { a = true; break; }
            }
            if (a)
            {
                for (int i = 0; i < matches.Length; i++)
                {
                    if (matches[i].team1 == null || matches[i].team2 == null) continue;
                    teams.GetTeam(matches[i].team1.teamName).currentTournament = _i;
                    teams.GetTeam(matches[i].team2.teamName).currentTournament = _i;
                }
            }
            else
            {
                int random = Random.Range(0, indexers.Count);
                byte randomTeamOut = indexers[random];
                string teamOut = teams.GetTeamPlacement(randomTeamOut).teamName;
                indexers[random] = (byte)teams.GetTeamPlacement(regTeam.teamName);
                for (int i = 0; i < matches.Length; i++)
                {
                    if (matches[i].team1 == null || matches[i].team2 == null) continue;
                    if (matches[i].team1.teamName == teamOut) matches[i].team1 = regTeam;
                    if (matches[i].team2.teamName == teamOut) matches[i].team2 = regTeam;
                    teams.GetTeam(matches[i].team1.teamName).currentTournament = _i;
                    teams.GetTeam(matches[i].team2.teamName).currentTournament = _i;
                }
            }
        }
        else
        {
            if (!decision)
            {
                if (IsMyTeamEverPlaying(Manager.mainInstance.GetMyTeam().teamName))
                {
                    for (int i = 0; i < teams.teams.Count; i++)
                    {
                        if (IsMyTeamEverPlaying(teams.GetTeamPlacement(i).teamName)) continue;
                        if (teams.GetTeamPlacement(i).teamName == "Free") continue;
                        if (teams.GetTeamPlacement(i).teamName == Manager.mainInstance.GetMyTeam().teamName) continue;
                        if (teams.GetTeamPlacement(i).players.Count < 5) continue;
                        ReplaceTeam(Manager.mainInstance.GetMyTeam(), teams.GetTeamPlacement(i));
                    }
                }
            }
            for (int i = 0; i < matches.Length; i++)
            {
                if (matches[i].team1 == null || matches[i].team2 == null) continue;
                teams.GetTeam(matches[i].team1.teamName).currentTournament = _i;
                teams.GetTeam(matches[i].team2.teamName).currentTournament = _i;
            }
        }
        isCalled = true;
    }

    public bool IsMyTeamEverPlaying(string team)
    {
        for (int i = 0; i < matches.Length; i++)
        {
            if (matches[i].team1 != null) if (matches[i].team1.teamName == team) return true;
            if (matches[i].team2 != null) if (matches[i].team2.teamName == team) return true;
        }
        return false;
    }

    public void ReplaceTeam(Team which, Team to)
    {
        for (int i = 0; i < matches.Length; i++)
        {
            if (matches[i].team1 != null) if (matches[i].team1.teamName == which.teamName) matches[i].team1 = to;
            if (matches[i].team2 != null) if (matches[i].team2.teamName == which.teamName) matches[i].team2 = to;
        }
    }

    /// <summary>
    /// Call only after all placements are known!
    /// </summary>
    /// <returns></returns>
    private List<Stat> GetMVP()
    {
        List<Stat> stats = new List<Stat>();
        List<string> strs = new List<string>(8);
        int p = 0;
        for (byte l = 0; l < placements[0].players.Count; l++)
        {
            stats.Add(placements[0].players[l].FullStat(Manager.day - day));
            stats[p].rating *= 1.25f;
            p++;
        }
        for (byte l = 0; l < placements[1].players.Count; l++)
        {
            stats.Add(placements[1].players[l].FullStat(Manager.day - day));
            stats[p].rating *= 1.1875f;
            p++;
        }
        for (byte l = 0; l < placements[2].players.Count; l++)
        {
            stats.Add(placements[2].players[l].FullStat(Manager.day - day));
            stats[p].rating *= 1.0625f;
            p++;
        }
        for (byte l = 0; l < placements[3].players.Count; l++)
        {
            stats.Add(placements[3].players[l].FullStat(Manager.day - day));
            stats[p].rating *= 1.0625f;
            p++;
        }
        return stats.OrderByDescending(x => x.rating).ToList();
    }

    public List<Stat> GetStats()
    {
        List<Stat> stats = new List<Stat>(40);
        TournamentStructure structure = Modding.modding.modSave.structure[btype];
        List<string> queue = new List<string>(40);
        if (structure.groupCount == 0)
        {
            for (int i = 0; i < matches.Length; i++)
            {
                if (matches[i].team1 == null || matches[i].team2 == null) continue;
                Team t1 = Manager.mainInstance.GetTeams().GetTeam(matches[i].team1.teamName);
                Team t2 = Manager.mainInstance.GetTeams().GetTeam(matches[i].team2.teamName);
                if (!queue.Contains(t1.teamName))
                {
                    queue.Add(t1.teamName);
                    for (byte l = 0; l < t1.players.Count; l++)
                    {
                        stats.Add(Manager.mainInstance.GetPlayers().GetPlayerStats
                        (t1.players[l].nickname, Manager.day - day));
                    }
                }
                if (!queue.Contains(t2.teamName))
                {
                    queue.Add(t2.teamName);
                    for (byte l = 0; l < t2.players.Count; l++)
                    {
                        stats.Add(Manager.mainInstance.GetPlayers().GetPlayerStats
                        (t2.players[l].nickname, Manager.day - day));
                    }
                }
            }
        }
        else
        {
            if (structure.groupCount * structure.groupMatchesLength < matches.Length)
            {
                if (matches[structure.groupCount * structure.groupMatchesLength].team1 != null &&
                    matches[structure.groupCount * structure.groupMatchesLength].team2 != null)
                {
                    for (int i = structure.groupCount * structure.groupMatchesLength; i < matches.Length; i++)
                    {
                        if (matches[i].team1 == null || matches[i].team2 == null) continue;
                        Team t1 = Manager.mainInstance.GetTeams().GetTeam(matches[i].team1.teamName);
                        Team t2 = Manager.mainInstance.GetTeams().GetTeam(matches[i].team2.teamName);
                        if (!queue.Contains(t1.teamName))
                        {
                            queue.Add(t1.teamName);
                            for (byte l = 0; l < t1.players.Count; l++)
                            {
                                stats.Add(Manager.mainInstance.GetPlayers().GetPlayerStats
                                (t1.players[l].nickname, Manager.day - day));
                            }
                        }
                        if (!queue.Contains(t2.teamName))
                        {
                            queue.Add(t2.teamName);
                            for (byte l = 0; l < t2.players.Count; l++)
                            {
                                stats.Add(Manager.mainInstance.GetPlayers().GetPlayerStats
                                (t2.players[l].nickname, Manager.day - day));
                            }
                        }
                    }
                    return stats.OrderByDescending(x => x.GetRating()).ToList();
                }
            }
            for (int i = 0; i < matches.Length; i++)
            {
                if (matches[i].team1 == null || matches[i].team2 == null) continue;
                Team t1 = Manager.mainInstance.GetTeams().GetTeam(matches[i].team1.teamName);
                Team t2 = Manager.mainInstance.GetTeams().GetTeam(matches[i].team2.teamName);
                if (!queue.Contains(t1.teamName))
                {
                    queue.Add(t1.teamName);
                    for (byte l = 0; l < t1.players.Count; l++)
                    {
                        stats.Add(Manager.mainInstance.GetPlayers().GetPlayerStats
                        (t1.players[l].nickname, Manager.day - day));
                    }
                }
                if (!queue.Contains(t2.teamName))
                {
                    queue.Add(t2.teamName);
                    for (byte l = 0; l < t2.players.Count; l++)
                    {
                        stats.Add(Manager.mainInstance.GetPlayers().GetPlayerStats
                        (t2.players[l].nickname, Manager.day - day));
                    }
                }
            }
        }
        return stats.OrderByDescending(x => x.rating).ToList();
    }

    /*
    public byte GetGroup(string teamName)
    {
        for (byte i = 0; i < groups.Length; i++)
        {
            for (byte j = 0; j < groups[i].teams.Length; j++)
            {
                if (teamName == groups[i].teams[j].teamName) return i;
            }
        }
        return 228;
    }*/

    public struct GroupResult
    {
        public string teamname;
        public int points;
    }

    public List<GroupResult> GetGroup(int group)
    {
        //if (state > 2) return null; ???????
        TournamentStructure structure = Modding.modding.modSave.structure[btype];
        if (structure.groupCount == 0) return null;
        group %= (structure.groupCount);
        List<GroupResult> result = new List<GroupResult>(4);
        for (int i = group * structure.groupMatchesLength; i < (group + 1) * structure.groupMatchesLength; i++)
        {
            GroupResult groupResult = new GroupResult
            {
                points = 0,
                teamname = matches[i].team1.teamName
            };
            if (!result.Contains(groupResult)) result.Add(groupResult);
            groupResult = new GroupResult
            {
                points = 0,
                teamname = matches[i].team2.teamName
            };
            if (!result.Contains(groupResult)) result.Add(groupResult);
        }
        for (int i = group * structure.groupMatchesLength; i < (group + 1) * structure.groupMatchesLength; i++)
        {
            if (matches[i].winner == 0) continue;
            if (matches[i].winner <= 5)
            {
                for (int j = 0; j < result.Count; j++)
                {
                    if (matches[i].team1.teamName == result[j].teamname)
                    {
                        result[j] = new GroupResult { teamname = result[j].teamname, points = result[j].points + 3 };
                        break;
                    }
                }
                continue;
            }
            else
            {
                for (int j = 0; j < result.Count; j++)
                {
                    if (matches[i].team2.teamName == result[j].teamname)
                    {
                        result[j] = new GroupResult { teamname = result[j].teamname, points = result[j].points + 3 };
                        break;
                    }
                }
                continue;
            }
        }
        return result.OrderByDescending(x => x.points).ToList();
    }

    public void SendResult(int tday, byte winner, byte[] maproundst, List<byte> pickbans,
        Team team1, Team team2, Manager.MatchType matchType)
    {
        state = 1;
        int matchIndex = 0;
        TournamentStructure structure = Modding.modding.modSave.structure[btype];
        for (int i = 0; i < matches.Length; i++)
        {
            if (tday == matches[i].day && matches[i].team1 == team1 && matches[i].team2 == team2)
            {
                matches[i].winner = winner;
                matchIndex = i;
                Team twinner, tloser;
                if (winner <= 5)
                {
                    twinner = Manager.mainInstance.GetTeams().GetTeam(team1.teamName);
                    tloser = Manager.mainInstance.GetTeams().GetTeam(team2.teamName);
                }
                else
                {
                    twinner = Manager.mainInstance.GetTeams().GetTeam(team2.teamName);
                    tloser = Manager.mainInstance.GetTeams().GetTeam(team1.teamName);
                }
                if (i >= structure.groupCount * structure.groupMatchesLength)
                {
                    string str = structure.matchSettings[i].onWin;
                    if (str.Length > 3)
                    {
                        if (str.Substring(0, 3) == "out")
                        {
                            int num = System.Int32.Parse(str.Substring(3, str.Length - 3));
                            placements[num] = twinner;
                        }
                    }
                    if (str.Length > 5)
                    {
                        if (str.Substring(str.Length - 5, 4) == "team")
                        {
                            string round = str.Substring(0, str.Length - 5);
                            int team = System.Int32.Parse(str.Substring(str.Length - 1, 1));
                            int f = 0;
                            for (; f < structure.matchSettings.Length; f++)
                            { if (structure.matchSettings[f].title == round) break; }
                            if (team == 1) matches[f].team1 = twinner;
                            if (team == 2) matches[f].team2 = twinner;
                        }
                    }
                    str = structure.matchSettings[i].onLose;
                    if (str.Length > 3)
                    {
                        if (str.Substring(0, 3) == "out")
                        {
                            int num = System.Int32.Parse(str.Substring(3, str.Length - 3));
                            placements[num] = tloser;
                        }
                    }
                    if (str.Length > 5)
                    {
                        if (str.Substring(str.Length - 5, 4) == "team")
                        {
                            string round = str.Substring(0, str.Length - 5);
                            int team = System.Int32.Parse(str.Substring(str.Length - 1, 1));
                            int f = 0;
                            for (; f < structure.matchSettings.Length; f++)
                            { if (structure.matchSettings[f].title == round) break; }
                            if (team == 1) matches[f].team1 = tloser;
                            if (team == 2) matches[f].team2 = tloser;
                        }
                    }
                }
                if (structure.groupCount > 0)
                {
                    if (i + 1 == structure.groupCount * structure.groupMatchesLength)
                    {
                        if (structure.afterGroup.Length > 1)
                        {
                            for (int j = 0; j < structure.groupCount; j++)
                            {
                                List<GroupResult> groupResults = GetGroup(j);
                                for (int k = 0; k < structure.groupLength; k++)
                                {
                                    string str = structure.afterGroup[j * structure.groupLength + k];
                                    if (str.Length > 3)
                                    {
                                        if (str.Substring(0, 3) == "out")
                                        {
                                            int num = System.Int32.Parse(str.Substring(3, str.Length - 3));
                                            placements[num] = Manager.mainInstance.GetTeams().GetTeam(groupResults[k].teamname);
                                        }
                                    }
                                    if (str.Length > 5)
                                    {
                                        if (str.Substring(str.Length - 5, 4) == "team")
                                        {
                                            string round = str.Substring(0, str.Length - 5);
                                            int team = System.Int32.Parse(str.Substring(str.Length - 1, 1));
                                            int f = 0;
                                            for (; f < structure.matchSettings.Length; f++)
                                            { if (structure.matchSettings[f].title == round) break; }
                                            if (team == 1) matches[f].team1 =
                                                    Manager.mainInstance.GetTeams().GetTeam(groupResults[k].teamname);
                                            if (team == 2) matches[f].team2 =
                                                    Manager.mainInstance.GetTeams().GetTeam(groupResults[k].teamname);
                                        }
                                    }
                                }
                            }
                        }
                        else if (structure.afterGroup.Length == 1)
                        {
                            if (structure.afterGroup[0] == "fill")
                            {
                                for (int j = 0; j < structure.groupCount; j++)
                                {
                                    List<GroupResult> groupResults = GetGroup(j);
                                    for (int k = 0; k < structure.groupLength; k++)
                                    {
                                        placements[(structure.groupCount * k) + j] =
                                            Manager.mainInstance.GetTeams().GetTeam(groupResults[k].teamname);
                                    }
                                }
                                Tournament tour = Events.events.GetTournaments()[unlockEvent];
                                tour.GenerateTournament(placements.ToList(), tour.btype, tour.tier);
                            }
                        }
                    }
                }
            }
        }
        if (matchIndex == structure.matchSettings.Length - 1) //redo for final and quals
        {
            /*if (structure.afterEnd == "fill")
            {
                for (int j = 0; j < structure.groupCount; j++)
                {
                    List<GroupResult> groupResults = GetGroup(j);
                    for (int k = 0; k < structure.groupLength; k++)
                    {
                        Debug.Log((structure.groupCount * k + j).ToString() + "/" + placements.Length + " " + groupResults.Count.ToString());
                        placements[(structure.groupCount * k) + j] =
                            Manager.mainInstance.GetTeams().GetTeam(groupResults[k].teamname);
                    }
                }
                Tournament tour = Events.events.GetTournaments()[unlockEvent];
                tour.GenerateTournament(placements.ToList(), tour.btype, tour.tier);
            }*/
            state = 2;
            int pr = 768;
            if (tier == 3) pr = 101;
            else if (tier == 2) pr = 228;
            else if (tier == 1) pr = 464;
            for (int i = 0; i < placements.Length; i++)
            {
                placements[i].AddMoney((int)(prizePool * structure.prizePoolDistribution[i]));
                placements[i].followers += (int)(pr * structure.followersDistribution[i]);
                //placements[i].points += (short)(pr * structure.pointsDistribution[i]);
                placements[i].ReportTournament(new PointsEvent(tier, (short)(pr * structure.pointsDistribution[i]), Manager.day));
            }
            if (majorPoints != 0)
            {
                for (int i = 0; i < placements.Length; i++)
                    placements[i].majorPoints += (short)(majorPoints * structure.majorPointsDistribution[i]);
            }
            if (structure.stage == "final")
            {
                if (isMajor)
                {
                    Manager.mainInstance.GetTeams().ResetMajorPoints();
                    for (int i = 0; i < placements.Length; i++) placements[i].wantsChanges += (byte)i;
                }
                else
                {
                    for (int i = 4; i < placements.Length; i++) placements[i].wantsChanges += (byte)(i - 3);
                }
                if (tier < 2)
                {
                    Manager.mainInstance.GrandSlam(new EventRecord(placements[0].teamName, title));
                    Manager.mainInstance.playGames.UnlockAchievement(2);
                    if (isMajor) Manager.mainInstance.playGames.UnlockAchievement(1);
                }
                placements[0].place1st++;
                placements[1].place2nd++;
                placements[2].place3rd++;
                placements[3].place3rd++;
                List<Stat> _stats = GetMVP();
                for (int i = 0; i < 4; i++)
                {
                    for (byte l = 0; l < placements[i].players.Count; l++)
                    {
                        if (_stats[0].nickname == placements[i].players[l].nickname)
                        { 
                            placements[i].players[l].mvp++;
                            placements[i].players[l].yearPerformance += (ushort)((4 - tier) * 0.25 * _stats[0].rating);
                        }
                        for (byte j = 1; j < 10; j++)
                        {
                            if (_stats[j].nickname == placements[i].players[l].nickname)
                            {
                                placements[i].players[l].evp++;
                                placements[i].players[l].yearPerformance += (ushort)((4 - tier) * 0.125 * _stats[j].rating);
                                break;
                            }
                        }
                    }
                }
                if (placements[0].teamName == Manager.mainInstance.GetMyTeam().teamName ||
                    placements[1].teamName == Manager.mainInstance.GetMyTeam().teamName)
                {
                    string[] table = new string[]
                    {
                        " wins ",
                        " побеждают в ",
                        " gewinnen ",
                        " vitórias ",
                        " victoires "
                    };
                    string[] table1 = new string[]
                    {
                        " earns MVP",
                        " получает MVP",
                        " verdient MVP",
                        " ganha MVP",
                        " MVP gagnés"
                    };
                    Manager.mainInstance.ShowPopUp
                        (placements[0].teamName + table[(byte)TranslateObject.language] + title + ", " +
                        _stats[0].nickname + table1[(byte)TranslateObject.language]);
                }
                
            }
        }
    }

    public bool AreThereAnyMatches()
    {
        if (matches != null)
            for (int i = 0; i < matches.Length; i++)
                if (matches[i].day == Manager.day)
                    return true;
        return false;
    }

    public bool AreThereAnyMatches(int _day)
    {
        if (matches != null)
            for (int i = 0; i < matches.Length; i++)
                if (matches[i].day == _day)
                    return true;
        return false;
    }

    //for performance reasons use only after AreThereAnyMatches()
    public List<MatchSetting> GetMatchesIfThereAreAny()
    {
        List<MatchSetting> ms = new List<MatchSetting>();
        if (matches != null)
            for (int i = 0; i < matches.Length; i++)
                if (matches[i].day == Manager.day) ms.Add(matches[i]);
        return ms;
    }
    public List<MatchSetting> GetMatchesIfThereAreAny(int _day)
    {
        List<MatchSetting> ms = new List<MatchSetting>();
        if (matches != null)
            for (int i = 0; i < matches.Length; i++)
                if (matches[i].day == _day) ms.Add(matches[i]);
        return ms;
    }

    public MatchSetting AreThereAnyMatches(Team team, int day = 0)
    {
        if (matches != null)
            for (int i = 0; i < matches.Length; i++)
                if (matches[i].team1 != null && matches[i].team2 != null)
                    if (matches[i].day == (day == 0 ? Manager.day : day) &&
                        (team.teamName == matches[i].team1.teamName ||
                        team.teamName == matches[i].team2.teamName)) return matches[i];
        return new MatchSetting();
    }

    public Tournament(byte state = 0)
    {
        this.state = state;
    }

    public Tournament(string title, string type, byte tier, int prizePool, int day,
        byte btype, Sprite logo, bool isMajor, byte majorPoints, byte open = 1, int unlockEvent = -1)
    {
        this.state = 0;
        this.tier = tier;
        this.title = title;
        this.type = type;
        this.unlockEvent = unlockEvent;
        this.btype = btype;
        this.day = day;
        this.logo = logo;
        this.open = open;
        this.prizePool = prizePool;
        this.majorPoints = majorPoints;
        this.isMajor = isMajor;
    }
}

[System.Serializable]
public class Bootcamp
{
    public int pricePerMonth;
    public byte formAdd, chemistryAdd, mapPoints, energyAdd, id; //map points * 5
    public string city;
    public AssistantCoach assistantCoach;
    public Psychologist psychologist;
    public Scout scout;
}

[System.Serializable]
public struct AssistantCoach
{
    public int pricePerMonth; // form * map * 150
    public byte formAdd, mapAdd; // 1-9 5-21
    public string name;
    public AssistantCoach(string name, byte f, byte m)
    {
        pricePerMonth = f * m * 150;
        formAdd = f;
        mapAdd = m;
        this.name = name;
    }
}

[System.Serializable]
public struct Psychologist
{
    public int pricePerMonth; // chemistry * psychology * 200
    public byte chemistryAdd, psychologyAdd; // 2-7 3-10
    public string name;
    public Psychologist(string name, byte c, byte p)
    {
        pricePerMonth = c * p * 200;
        chemistryAdd = c;
        psychologyAdd = p;
        this.name = name;
    }
}

[System.Serializable]
public struct Scout
{
    public int pricePerMonth; // (90 - startStrength) * 250
    public byte startStrength; // 90-50
    public string name;
    public Scout(string name, byte s)
    {
        pricePerMonth = (90 - s) * 250;
        startStrength = s;
        this.name = name;
    }
}