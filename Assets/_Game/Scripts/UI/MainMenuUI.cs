using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>Main menu buttons: play, how to play, quit.</summary>
public class MainMenuUI : MonoBehaviour
{
    [SerializeField] string gameScene = "Island";
    [SerializeField] GameObject howToPlayPanel;
    [SerializeField] GameObject firstSelected;
    [SerializeField] GameObject howToPlayFirstSelected;

    void Start()
    {
        Time.timeScale = 1f;
        if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
        Select(firstSelected);
    }

    public void OnPlayClicked() => SceneManager.LoadScene(gameScene);

    public void OnHowToPlayClicked()
    {
        howToPlayPanel.SetActive(true);
        Select(howToPlayFirstSelected);
    }

    public void OnCloseHowToPlayClicked()
    {
        howToPlayPanel.SetActive(false);
        Select(firstSelected);
    }

    public void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    static void Select(GameObject target)
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(target);
    }
}
