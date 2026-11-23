using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CreditsMenu : MonoBehaviour
{
    [SerializeField] private Button backButton;
    [SerializeField] private RectTransform scrollContent;
    [SerializeField] private float scrollSpeed = 25f;
    [SerializeField] private float scrollLimit = 120f;

    private float startY;

    private void Start()
    {
        backButton.onClick.AddListener(() => SceneLoader.Load(SceneNames.MainMenu));
        EventSystem.current?.SetSelectedGameObject(backButton.gameObject);
        if (scrollContent != null) startY = scrollContent.anchoredPosition.y;
    }

    private void Update()
    {
        if (scrollContent != null)
        {
            var pos = scrollContent.anchoredPosition;
            pos.y = Mathf.Min(pos.y + scrollSpeed * Time.deltaTime, startY + scrollLimit);
            scrollContent.anchoredPosition = pos;
        }

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            SceneLoader.Load(SceneNames.MainMenu);
    }
}
