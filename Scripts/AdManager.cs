using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Advertisements;

public class AdManager : MonoBehaviour, IUnityAdsListener
{
    string gameId = "4269681";
    static readonly string adType = "Rewarded_Android";
    bool testMode = false;

    // Initialize the Ads listener and service:
    void Start()
    {
        //Debug.Log(PlayerNicknames.nickname.Length.ToString());
        Advertisement.AddListener(this);
        Advertisement.Initialize(gameId, testMode);
    }

    // Implement IUnityAdsListener interface methods:
    public void OnUnityAdsDidFinish(string placementId, ShowResult showResult)
    {
        if (showResult == ShowResult.Finished)
        {
            // Reward the user for watching the ad to completion.
        }
        else if (showResult == ShowResult.Skipped)
        {
            // Do not reward the user for skipping the ad.
        }
        else if (showResult == ShowResult.Failed)
        {
            Debug.LogWarning("The ad did not finish due to an error.");
        }
    }

    public static void ShowAdd()
    {
        if (Advertisement.IsReady(adType) && Manager.mainInstance.showAds != 202) Advertisement.Show(adType);
    }

    public void OnUnityAdsReady(string placementId)
    {
        // If the ready Placement is rewarded, show the ad:
    }

    public void OnUnityAdsDidError(string message)
    {
        // Log the error.
    }

    public void OnUnityAdsDidStart(string placementId)
    {
        // Optional actions to take when the end-users triggers an ad.
    }
}
