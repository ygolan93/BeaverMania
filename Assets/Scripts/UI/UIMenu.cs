using Beavermania.Core.GameFlow;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIMenu : MonoBehaviour
{
    public GameObject PauseMenu;
    public GameObject Question;
    public Behaviour Player;
    [SerializeField] Slider volumeSlider;
    [SerializeField] AudioSource Music;

    PauseController pauseController;

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
        Player = GameObject.FindGameObjectWithTag("Player").GetComponent<Behaviour>();
        if (Player.seekMusic == true)
        {
            Music = GameObject.FindGameObjectWithTag("Music").GetComponent<AudioSource>();
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
        if (Player.seekMusic == true)
        {
            Music.volume = volumeSlider.value;
        }
    }
}
