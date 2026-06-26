using UnityEngine;
using TMPro;

public class MatchManager : MonoBehaviour
{
    public float matchTime = 240f;

    public TMP_Text timerText;

    void Update()
    {
        if (matchTime > 0)
        {
            matchTime -= Time.deltaTime;

            int minutes = Mathf.FloorToInt(matchTime / 60);
            int seconds = Mathf.FloorToInt(matchTime % 60);

            timerText.text =
                minutes.ToString("00") +
                ":" +
                seconds.ToString("00");
        }
        else
        {
            EndMatch();
        }
    }

    void EndMatch()
    {
        Debug.Log("Match Ended");

        Time.timeScale = 0f;
    }
}