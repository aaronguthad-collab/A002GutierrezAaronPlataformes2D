using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button menuButton;

    private bool paused;

    private void Start()
    {
        panel.SetActive(false);
        resumeButton.onClick.AddListener(() => SetPaused(false));
        restartButton.onClick.AddListener(() => { SetPaused(false); SceneLoader.ReloadCurrent(); });
        menuButton.onClick.AddListener(() => { SetPaused(false); SceneLoader.Load(SceneNames.MainMenu); });
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) SetPaused(!paused);
    }

    private void SetPaused(bool value)
    {
        paused = value;
        panel.SetActive(value);
        Time.timeScale = value ? 0f : 1f;
        if (value) EventSystem.current?.SetSelectedGameObject(resumeButton.gameObject);
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}
