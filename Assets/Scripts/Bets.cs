using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bets : MonoBehaviour
{
    public static Bets manager;
    public List<Bet> bets;
    private void Awake()
    {
        manager = this;
    }

    private void Start()
    {
        bets = new List<Bet>(1);
    }
}

public struct Bet
{
    public string team1, team2;
    public int money1, money2;
    public float coef1, coef2;

    public Bet(string team1, string team2, int money1, int money2, float coef1, float coef2)
    {
        this.team1 = team1;
        this.team2 = team2;
        this.money1 = money1;
        this.money2 = money2;
        this.coef1 = coef1;
        this.coef2 = coef2;
    }
}
