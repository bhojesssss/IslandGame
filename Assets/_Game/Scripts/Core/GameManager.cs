using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Owns the overall game state: playing, paused, reading a dialog, game over and victory.
/// </summary>
public class GameManager : MonoBehaviour
{
    public enum State { Playing, Paused, Dialog, GameOver, Victory }

    public static GameManager Instance { get; private set; }

    [Tooltip("How many crystals are needed to activate the portal.")]
    [SerializeField] int requiredCrystals = 3;
    [SerializeField] string mainMenuScene = "MainMenu";

    public State Current { get; private set; } = State.Playing;
    public int RequiredCrystals => requiredCrystals;
    public float PlayTime { get; private set; }
    public int EnemiesDefeated { get; set; }

    int unblockedFrame = -1;

    /// <summary>
    /// True while a menu or dialog is open, or the game has ended. Gameplay input is ignored then.
    /// Also true on the frame a dialog closes so the same key press is not reused.
    /// </summary>
    public static bool InputBlocked =>
        Instance != null && (Instance.Current != State.Playing || Time.frameCount <= Instance.unblockedFrame);

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (Current == State.Playing) PlayTime += Time.deltaTime;

        if (!GameInput.PausePressed) return;
        switch (Current)
        {
            case State.Playing: Pause(); break;
            case State.Paused: Resume(); break;
            case State.Dialog: GameUI.Instance?.CloseDialogs(); break;
        }
    }

    public void Pause()
    {
        if (Current != State.Playing) return;
        Current = State.Paused;
        Time.timeScale = 0f;
        GameUI.Instance?.ShowPause(true);
    }

    public void Resume()
    {
        if (Current != State.Paused) return;
        GameUI.Instance?.ShowPause(false);
        BackToPlaying();
    }

    /// <summary>Freezes gameplay while a dialog (clue note, upgrade menu) is open.</summary>
    public void OpenDialog()
    {
        if (Current != State.Playing) return;
        Current = State.Dialog;
        Time.timeScale = 0f;
    }

    public void CloseDialog()
    {
        if (Current == State.Dialog) BackToPlaying();
    }

    void BackToPlaying()
    {
        Current = State.Playing;
        Time.timeScale = 1f;
        unblockedFrame = Time.frameCount;
    }

    public void GameOver()
    {
        if (Current is State.GameOver or State.Victory) return;
        Current = State.GameOver;
        GameUI.Instance?.ShowGameOver();
    }

    public void Victory()
    {
        if (Current is State.GameOver or State.Victory) return;
        Current = State.Victory;
        Time.timeScale = 0f;
        GameUI.Instance?.ShowVictory();
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuScene);
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
