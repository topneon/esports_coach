using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Players : MonoBehaviour
{
    public List<Player> players;
    //public void CreateNewPlayersArray(int x) { players = new Player[x]; }
    /*
    public Player LoadPlayer(int indexer)
    {
        PlayerInfo playerInfo = SaveManager.Load<PlayerInfo>("player" + indexer.ToString());
        if (indexer >= players.Count) players.Add(new Player());
        players[indexer].nickname = playerInfo.nickname;
        players[indexer].teamName = playerInfo.teamName;
        players[indexer].salary = playerInfo.salary;
        players[indexer].daysInTeam = playerInfo.daysInTeam;
        players[indexer].daysPayed = playerInfo.daysPayed;
        players[indexer].nationality = (Manager.Nation)playerInfo.nationality;
        players[indexer].language = (Manager.Nation)playerInfo.language;
        players[indexer].age = playerInfo.age;
        players[indexer].activity = (Manager.Activity)playerInfo.activity;
        players[indexer].role = (Manager.Role)playerInfo.role;
        players[indexer].awp = playerInfo.awp;
        players[indexer].rifle = playerInfo.rifle;
        players[indexer].ct = playerInfo.ct;
        players[indexer].t = playerInfo.t;
        players[indexer].clutching = playerInfo.clutching;
        players[indexer].entring = playerInfo.entring;
        players[indexer].killing = playerInfo.killing;
        players[indexer].mvp = playerInfo.mvp;
        players[indexer].evp = playerInfo.evp;
        players[indexer].strength = playerInfo.strength;
        players[indexer].playerStats = playerInfo.playerStats;
        //int t = GetInt(player.nickname + "gamesPlayed", 0);
        players[indexer].stats = new List<int>();
        players[indexer].SetProgression(playerInfo.progression);
        if (playerInfo.stats != null)
        {
            players[indexer].stats = playerInfo.stats.ToList();
        }
        players[indexer].FullStat(14);
        return players[indexer];
    }

    public List<Player> GetRolePlayers(Manager.Role role)
    {
        List<Player> ps = new List<Player>((int)(players.Count * 0.1875f));
        for (short j = 0; j < players.Count; j++)
            if (players[j].role == role) ps.Add(players[j]);
        return ps;
    }*/

    public Stat GetPlayerStats(string nickname, int period)
    {
        for (short j = 0; j < players.Count; j++)
            if (players[j].nickname == nickname)
                return players[j].FullStat(period);
        return null;
    }

    public Player GetPlayer(string nickname)
    {
        for (short j = 0; j < players.Count; j++)
            if (players[j].nickname == nickname)
                return players[j];
        return null;
    }

    public void AddPlayer(Player player)
    {
        player.playerStats = new List<Stat>();
        players.Add(player);
    }
}
