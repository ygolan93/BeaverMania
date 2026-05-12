using Beavermania.Core.GameFlow;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIMenu : MonoBehaviour
{
    [SerializeField] public GameObject PauseMenu;
    [SerializeField] public GameObject Question;
    [SerializeField] public Behaviour Player;
    [SerializeField] Slider volumeSlider;
    [SerializeField] AudioSource Music;

    PauseController pauseController;
    bool loggedMissingPlayer;
    bool loggedMissingMusic;

    PauseController PauseController
    {
        get
        {
            if (pauseController == null)
                pauseController = GetComponent<PauseController>();

            if (pauseController == null)
                pauseController = gameObject.AddComponent<PauseController>();

            return pauseController;
        }
    }

    // Start is called before the first frame update
    private void Start()
    {
        if (Player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                Player = playerObject.GetComponent<Behaviour>();

            if (Player == null)
                LogMissingReference(nameof(Player), ref loggedMissingPlayer);
        }

        if (Player != null && Player.seekMusic == true && Music == null)
        {
            GameObject musicObject = GameObject.FindGameObjectWithTag("Music");
            if (musicObject != null)
                Music = musicObject.GetComponent<AudioSource>();

            if (Music == null)
                LogMissingReference(nameof(Music), ref loggedMissingMusic);
        }

        PauseController.Bind(PauseMenu, Question, Player);
    }

    public void Pause()
    {
        PauseController.Pause();
    }

    public void ChangeBolean()
    {
        PauseController.ChangeBolean();
    }

    public void RestartCheckpointFromMenu()
    {
        if (Player == null)
            return;

        Player.RestartCheckpoint();
        PauseController.HideQuestion();
    }
    public void QuitGame()
    {
        Application.Quit();
    }

    public void MainMenu()
    {
        SceneManager.LoadScene("Menu");
    }
    public void Volume()
    {
        //AudioListener.volume = volumeSlider.value;
        if (Player != null && Player.seekMusic == true && Music != null)
        {
            Music.volume = volumeSlider.value;
        }
    }

    void LogMissingReference(string referenceName, ref bool logged)
    {
        if (logged)
            return;

        logged = true;
#if DEVELOPMENT_BUILD
        Debug.LogWarning($"{nameof(UIMenu)} could not resolve {referenceName} fallback.", this);
#endif
    }
}
