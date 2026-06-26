using System.Collections;
using UnityEngine;
using TMPro;
using StarterAssets;

public class EventManager : MonoBehaviour
{
    public TMP_Text eventText;
    public Light sun;
    public FirstPersonController player;

    public Material nightSky;

    public enum MatchEvent
    {
        ReverseControls,
        NightTime,
        SuperSpeed,
        JumpsDisabled
    }

    public MatchEvent currentEvent;

    private string[] eventNames =
    {
        "REVERSE CONTROLS",
        "NIGHT TIME",
        "SUPER SPEED",
        "JUMPS DISABLED"
    };

    void Start()
    {
        StartCoroutine(SpinEvent());
    }

    IEnumerator SpinEvent()
    {
        eventText.gameObject.SetActive(true);

        // Spin animation
        for (int i = 0; i < 30; i++)
        {
            int randomIndex = Random.Range(0, eventNames.Length);

            eventText.text = eventNames[randomIndex];

            yield return new WaitForSeconds(0.08f);
        }

        // Final event
        currentEvent = (MatchEvent)Random.Range(0, eventNames.Length);

        eventText.text = eventNames[(int)currentEvent];

        ApplyEvent();
    }

    void ApplyEvent()
    {
        switch (currentEvent)
        {
            case MatchEvent.ReverseControls:

                player.reverseControls = true;

                Debug.Log("EVENT: Reverse Controls");
                break;

        case MatchEvent.NightTime:

            if (sun != null)
            {
                sun.enabled = false;
            }

            RenderSettings.skybox = nightSky;
            RenderSettings.ambientIntensity = 0f;
            RenderSettings.reflectionIntensity = 0f;

            Debug.Log("EVENT: Night Time");

            break;

            case MatchEvent.SuperSpeed:

                player.MoveSpeed *= 2f;
                player.SprintSpeed *= 2f;

                Debug.Log("EVENT: Super Speed");
                break;

            case MatchEvent.JumpsDisabled:

                player.jumpsDisabled = true;

                Debug.Log("EVENT: Jumps Disabled");
                break;
        }
    }
}