using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button quitButton;

    private void Start()
    {
        playButton.onClick.AddListener(() => SceneLoader.Load(SceneNames.Level1));
        creditsButton.onClick.AddListener(() => SceneLoader.Load(SceneNames.Credits));
        quitButton.onClick.AddListener(SceneLoader.Quit);
        EventSystem.current?.SetSelectedGameObject(playButton.gameObject);
    }
}
