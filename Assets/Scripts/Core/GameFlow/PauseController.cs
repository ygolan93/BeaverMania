using UnityEngine;

public class PauseController : MonoBehaviour
{
    public bool ActivePause = false;

    GameObject pauseMenu;
    GameObject question;
    Behaviour player;

    public void Bind(GameObject pauseMenu, GameObject question, Behaviour player)
    {
        this.pauseMenu = pauseMenu;
        this.question = question;
        this.player = player;

        ActivePause = false;
        ApplyPauseState();
    }

    public void Pause()
    {
        ApplyPauseState();
    }

    public void HideQuestion()
    {
        SetQuestionVisible(false);
    }

    public void ChangeBolean()
    {
        ActivePause = !ActivePause;
        ApplyPauseState();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            ChangeBolean();
    }

    void ApplyPauseState()
    {
        if (ActivePause)
        {
            SetMenuVisible(true);
            Time.timeScale = 0;

            if (player != null)
                player.ShowCursor();

            return;
        }

        SetMenuVisible(false);
        Time.timeScale = 1f;
        SetQuestionVisible(false);

        if (player != null && player.isAtTrader == false)
            player.HideCursor();
    }

    void SetMenuVisible(bool visible)
    {
        if (pauseMenu != null)
            pauseMenu.SetActive(visible);
    }

    void SetQuestionVisible(bool visible)
    {
        if (question != null)
            question.SetActive(visible);
    }
}
