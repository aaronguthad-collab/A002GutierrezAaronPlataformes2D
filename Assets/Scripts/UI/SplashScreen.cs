using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class SplashScreen : MonoBehaviour
{
    [SerializeField] private CanvasGroup logoGroup;
    [SerializeField] private RectTransform logo;
    [SerializeField] private float fadeIn = 1f;
    [SerializeField] private float hold = 2f;
    [SerializeField] private float fadeOut = 0.8f;

    private bool skipping;

    private IEnumerator Start()
    {
        logoGroup.alpha = 0f;
        for (float t = 0f; t < fadeIn && !skipping; t += Time.deltaTime)
        {
            float k = t / fadeIn;
            logoGroup.alpha = k;
            logo.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, 1f - (1f - k) * (1f - k));
            yield return null;
        }
        logoGroup.alpha = 1f;
        logo.localScale = Vector3.one;

        for (float t = 0f; t < hold && !skipping; t += Time.deltaTime) yield return null;
        for (float t = 0f; t < fadeOut; t += Time.deltaTime)
        {
            logoGroup.alpha = 1f - t / fadeOut;
            yield return null;
        }

        SceneLoader.Load(SceneNames.MainMenu);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) skipping = true;
    }
}
