using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Teams : MonoBehaviour
{
    [SerializeField] private Players playersInfo;
    public List<Team> teams;
    private List<Team> sortedTeams;
    //public static Teams mainTeams;
    /*private IEnumerator Start()
    {
        yield return new WaitForSeconds(0.125f);
        GroupTeams();
        GeneratePlayers();
    }*/
    public static double ach = 1, form = 1, lan = 1;
    public int GetTeamPlacement(string teamName, bool sort = false)
    {
        if (sort)
        {
            ach = 500.0 / teams.Max(x => x.GetACHPoints());
            form = 200.0 / teams.Max(x => x.GetFORMPoints());
            lan = 300.0 / teams.Max(x => x.GetLANPoints());
            sortedTeams = teams.OrderByDescending(x => x.GetACHPoints() * ach + x.GetFORMPoints() * form + x.GetLANPoints() * lan).ToList();
        }
        for (short i = 0; i < sortedTeams.Count; i++) 
            if (sortedTeams[i].teamName == teamName) return i + 1;
        return 404;
    }

    public Team GetTeamPlacement(int indexer, bool sort = false)
    {
        if (sort)
        {
            ach = 500.0 / teams.Max(x => x.GetACHPoints());
            form = 200.0 / teams.Max(x => x.GetFORMPoints());
            lan = 300.0 / teams.Max(x => x.GetLANPoints());
            sortedTeams = teams.OrderByDescending(x => x.GetACHPoints() * ach + x.GetFORMPoints() * form + x.GetLANPoints() * lan).ToList();
        }
        return sortedTeams[indexer];
    }
    /*
    public int GetTeamPlacementPoints(int indexer, bool sort = false)
    {
        if (sort) sortedTeams = teams.OrderByDescending(x => x.points).ToList();
        return sortedTeams[indexer].points;
    }
    public string GetTeamPlacementTeamname(int indexer, bool sort = false)
    {
        if (sort) sortedTeams = teams.OrderByDescending(x => x.points).ToList();
        return sortedTeams[indexer].teamName;
    }*/

    public void UpdateStats()
    {
        if (updatingStats)
        {
            StopCoroutine(latestStat);
            StopCoroutine(updateStats);
        }
        updateStats = UpdateStatsEnum();
        StartCoroutine(updateStats);
    }
    public bool updatingStats = false;
    private IEnumerator latestStat, updateStats;
    private IEnumerator UpdateStatsEnum()
    {
        updatingStats = true;
        for (int i = 0; i < teams.Count; i++)
            if (teams[i].players.Count > 4)
            {
                if (teams[i].playedToday == 1)
                {
                    for (byte j = 0; j < 5; j++)
                    {
                        latestStat = teams[i].players[j].FullStatEnum(14);
                        StartCoroutine(latestStat);
                        teams[i].players[j].calculating = true;
                        yield return new WaitWhile(() => teams[i].players[j].calculating);
                    }
                    teams[i].playedToday = 0;
                }
            }
        updatingStats = false;
    }

    public Team GetTeam(string teamName)
    {
        for (short i = 0; i < sortedTeams.Count; i++)
            if (sortedTeams[i].teamName == teamName) return sortedTeams[i];
        return null;
    }

    public static Team[] randomTeams;

    public void AbsoluteStart()
    {
        ach = 500.0 / teams.Max(x => x.GetACHPoints());
        lan = 300.0 / teams.Max(x => x.GetLANPoints());
        if (ach.ToString() == "Infinity") ach = 1;
        form = 1;
        if (lan.ToString() == "Infinity") lan = 1;
        sortedTeams = teams.OrderByDescending(x => x.GetACHPoints() * ach + x.GetLANPoints() * lan).ToList();
        for (int i = 0; i < sortedTeams.Count; i++)
        {
            for (int j = 0; j < teams.Count; j++)
            {
                if (sortedTeams[i].teamName == teams[j].teamName) sortedTeams[i] = teams[j];
            }
        }
    }

    [ContextMenu("Sort Teams")]
    public void GroupTeams()
    {
        ach = 500.0 / teams.Max(x => x.GetACHPoints());
        form = 200.0 / teams.Max(x => x.GetFORMPoints());
        lan = 300.0 / teams.Max(x => x.GetLANPoints());
        if (ach.ToString() == "Infinity") ach = 1;
        if (form.ToString() == "Infinity") form = 1;
        if (lan.ToString() == "Infinity") lan = 1;
        sortedTeams = teams.OrderByDescending(x => x.GetACHPoints() * ach + x.GetFORMPoints() * form + x.GetLANPoints() * lan).ToList();
        for (int i = 0; i < sortedTeams.Count; i++)
        {
            for (int j = 0; j < teams.Count; j++)
            {
                if (sortedTeams[i].teamName == teams[j].teamName) sortedTeams[i] = teams[j];
            }
        }
    }

    public void ResetMajorPoints()
    {
        for (short i = 0; i < sortedTeams.Count; i++) sortedTeams[i].majorPoints = 0;
    }

    public string[] GetMajorTeams()
    {
        Team[] t = sortedTeams.OrderByDescending(x => x.majorPoints).ToArray();
        string[] res = new string[16];
        for (byte i = 0; i < 16; i++) res[i] = t[i].teamName;
        return res;
    }

    public void PointDecay()
    {
        GroupTeams();
    }

    [ContextMenu("Attach Players to Teams")]
    public void GeneratePlayers()
    {
        List<Player> playersList = playersInfo.players.ToList();
        for (ushort s = 0; s < teams.Count; s++)
        {
            teams[s].players = new List<Player>();
            for (ushort j = 0; j < playersList.Count; j++)
            {
                if (teams[s].teamName == playersList[j].teamName)
                { 
                    playersList[j].GeneratePlayer();
                    teams[s].players.Add(playersList[j]);
                    if (playersList[j].role == Manager.Role.Entry) teams[s].entry = true;
                    if (playersList[j].role == Manager.Role.IGL) teams[s].igl = true;
                    if (playersList[j].role == Manager.Role.Lurk) teams[s].lurk = true;
                    if (playersList[j].role == Manager.Role.Sniper) teams[s].sniper = true;
                    if (playersList[j].role == Manager.Role.Support) teams[s].support = true;
                    playersList.RemoveAt(j);
                    j--;
                }
            }
        }
        //for (ushort s = 1; s < teams.Length; s++) if (teams[s].players.Count < 5) Debug.Log(teams[s].teamName);
        for (short i = 0; i < playersList.Count; i++) playersList[i].GeneratePlayer();
    }
    /*
    [ContextMenu("Reset Players in Teams")]
    public void ResetPlayersInTeams()
    {
        for (ushort i = 0; i < teams.Count; i++)
        {
            teams[i].players = new List<Player>();
        }
    }*/

    public void AddTeam(Team team)
    {
        teams.Add(team);
    }
}
