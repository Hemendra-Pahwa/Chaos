using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
    [Header("Logo")]
    public RectTransform logoTransform;
    public TMP_Text logoText;

    [Header("Buttons")]
    public Button playButton;
    public Button settingsButton;
    public Button quitButton;

    [Header("Settings Panel")]
    public GameObject settingsPanel;

    private void Start()
    {
        StartCoroutine(LogoEntrance());
        StartCoroutine(FloatLogo());
        StartCoroutine(FlickerLogo());

        playButton.onClick.AddListener(PlayGame);
        settingsButton.onClick.AddListener(OpenSettings);
        quitButton.onClick.AddListener(QuitGame);
    }

    IEnumerator LogoEntrance()
    {
        float t = 0f;
        logoTransform.localScale = Vector3.one * 0.7f;
        Color c = logoText.color;
        c.a = 0f;
        logoText.color = c;

        while (t < 0.7f)
        {
            t += Time.deltaTime;
            float progress = t / 0.7f;
            logoTransform.localScale = Vector3.Lerp(
                Vector3.one * 0.7f, Vector3.one, progress);
            c.a = Mathf.Lerp(0f, 1f, progress);
            logoText.color = c;
            yield return null;
        }
        logoTransform.localScale = Vector3.one;
    }

    IEnumerator FloatLogo()
    {
        yield return new WaitForSeconds(0.8f);
        Vector3 startPos = logoTransform.anchoredPosition;
        float t = 0f;

        while (true)
        {
            t += Time.deltaTime;
            float offsetY = Mathf.Sin(t * Mathf.PI / 4f) * 7f;
            logoTransform.anchoredPosition = startPos + new Vector3(0, offsetY, 0);
            yield return null;
        }
    }

    IEnumerator FlickerLogo()
    {
        yield return new WaitForSeconds(1f);

        while (true)
        {
            yield return new WaitForSeconds(Random.Range(2f, 4f));
            logoText.color = new Color(1f, 0.19f, 0.19f, 0.8f);
            yield return new WaitForSeconds(0.05f);
            logoText.color = new Color(1f, 0.19f, 0.19f, 1f);
            yield return new WaitForSeconds(0.05f);
            logoText.color = new Color(1f, 0.19f, 0.19f, 0.85f);
            yield return new WaitForSeconds(0.08f);
            logoText.color = new Color(1f, 0.19f, 0.19f, 1f);
        }
    }

    public void PlayGame()
    {
        SceneManager.LoadScene("MainScene");
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(!settingsPanel.activeSelf);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}