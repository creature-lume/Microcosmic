using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] Animator creditsAnimator;
    [SerializeField] AudioSource menu1;
    [SerializeField] AudioSource menu2;
    [SerializeField] float transitionSpeed;

    private void Awake()
    {
        Application.runInBackground = true;
    }

    private void Start()
    {
        creditsAnimator.SetTrigger("StartLeft");
        menu1.Play();
        menu2.Play();
        menu2.volume = 0.0f;
        Cursor.visible = true;
    }

    public void OnQuit()
    {
        Application.Quit();
        Debug.Log("Application.Quit()");
    }

    public void OnLume()
    {
        Application.OpenURL("https://creature-lume.itch.io/");
    }

    public void OnBlue()
    {
        Application.OpenURL("https://shambolic-blue.itch.io/");
    }

    public void OnSpiralNotebookPlanet()
    {
        //SceneManager.LoadScene("SpiralNotebookPlanet");
        SceneManager.LoadScene("Level1");
    }

    public void ToMenu()
    {
        StartCoroutine(ChangeMusic(true));
    }

    public void ToLevel()
    {
        StartCoroutine(ChangeMusic(false));
    }

    public IEnumerator ChangeMusic(bool toMenu)
    {
        if (toMenu)
        {
            yield return new WaitForSeconds(transitionSpeed);
            menu2.volume -= 0.01f;
            menu1.volume += 0.01f;
            if (menu1.volume != 1f) StartCoroutine(ChangeMusic(toMenu));
        }
        else
        {
            yield return new WaitForSeconds(transitionSpeed);
            menu1.volume -= 0.01f;
            menu2.volume += 0.01f;
            if (menu2.volume != 1f) StartCoroutine(ChangeMusic(toMenu));
        }
    }
}
