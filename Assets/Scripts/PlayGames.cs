using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using GooglePlayGames;
using GooglePlayGames.BasicApi;

public class PlayGames : MonoBehaviour
{
    [SerializeField] private List<string> leaderboards;
    public static PlayGamesPlatform platform;

    void Start()
    {
        if (platform == null)
        {
            PlayGamesClientConfiguration config = new PlayGamesClientConfiguration.Builder().Build();
            PlayGamesPlatform.InitializeInstance(config);
            //PlayGamesPlatform.DebugLogEnabled = false;
            platform = PlayGamesPlatform.Activate();
        }
        PlayGamesPlatform.Instance.Authenticate(success =>
        {
            if (success)
            {
                Debug.Log("Logged in successfully");
            }
            else
            {
                Debug.Log("Login Failed");
            }
        });
    }

    public void AddScoreToLeaderboard(int playerScore, int leaderboard)
    {
        if (Social.Active.localUser.authenticated)
        {
            Social.ReportScore(playerScore, leaderboards[leaderboard], success => { });
        }
    }

    public static bool IsAuthenticated() { return Social.Active.localUser.authenticated; }

    public void ShowLeaderboard()
    {
        if (Social.Active.localUser.authenticated)
        {
            platform.ShowLeaderboardUI();
        }
        else Start();
    }

    public void ShowAchievements()
    {
        if (Social.Active.localUser.authenticated)
        {
            platform.ShowAchievementsUI();
        }
        else Start();
    }

    public void UnlockAchievement(int achievment)
    {
        if (Social.Active.localUser.authenticated)
        {
            string ach = string.Empty;
            switch (achievment)
            {
                case 0:
                    ach = GPGSIds.achievement_millionaire; //1mil
                    break;
                case 1:
                    ach = GPGSIds.achievement_major_champion; //major
                    break;
                case 2:
                    ach = GPGSIds.achievement_fun_experience; //t1 lan
                    break;
                case 3:
                    ach = GPGSIds.achievement_sweet_ingots; //grand slam
                    break;
                case 4:
                    ach = GPGSIds.achievement_worlds_best; //top 1
                    break;
                case 5:
                    ach = GPGSIds.achievement_successful_purchase; //player 1m+
                    break;
                case 6:
                    ach = GPGSIds.achievement_give_him_a_try; //league player
                    break;
                case 7:
                    ach = GPGSIds.achievement_build_an_empire; //bootcamp
                    break;
            }
            Social.ReportProgress(ach, 100f, success => { });
        }
    }
}