using Beavermania.Display;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Beavermania.UI.Menus
{
    public class MainMenu : MonoBehaviour
    {
        //double Timer=57;
        //Behavior Ottis;
        //Start is called before the first frame update
        //private void Update()
        //{
        //    Timer -= Time.deltaTime;
        //    if (Timer <= 0)
        //    {
        //        PlayGame();
        //    }
        //}
        void Awake()
        {
            PlayerCursorRules.ApplyUnlockedVisible();
        }

        void OnEnable()
        {
            PlayerCursorRules.ApplyUnlockedVisible();
        }

        void LateUpdate()
        {
            PlayerCursorRules.ApplyUnlockedVisible();
        }

        public void PlayGame()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex +1);
        }
        public void QuitGame()
        {
            Debug.Log("Quit");
            Application.Quit();
        }
    }
}
