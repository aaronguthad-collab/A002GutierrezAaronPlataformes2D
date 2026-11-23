using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour
{
    private const float FadeDuration = 0.35f;

    private static SceneLoader instance;

    private CanvasGroup fadeGroup;
    private bool isLoading;

    private static SceneLoader Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject(nameof(SceneLoader));
                instance = go.AddComponent<SceneLoader>();
                DontDestroyOnLoad(go);
                instance.BuildFadeCanvas();
            }
            return instance;
        }
    }

    public static void Load(string sceneName)
    {
        Instance.StartLoad(sceneName);
    }

    public static void ReloadCurrent()
    {
        Load(SceneManager.GetActiveScene().name);
    }

    public static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void StartLoad(string sceneName)
    {
        if (isLoading) return;
        StartCoroutine(LoadRoutine(sceneName));
    }

    private IEnumerator LoadRoutine(string sceneName)
    {
        isLoading = true;
        Time.timeScale = 1f;
        yield return Fade(0f, 1f);
        yield return SceneManager.LoadSceneAsync(sceneName);
        yield return Fade(1f, 0f);
        isLoading = false;
    }

    private IEnumerator Fade(float from, float to)
    {
        fadeGroup.blocksRaycasts = true;
        for (float t = 0f; t < FadeDuration; t += Time.unscaledDeltaTime)
        {
            fadeGroup.alpha = Mathf.Lerp(from, to, t / FadeDuration);
            yield return null;
        }
        fadeGroup.alpha = to;
        fadeGroup.blocksRaycasts = to > 0.5f;
    }

    private void BuildFadeCanvas()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        fadeGroup = gameObject.AddComponent<CanvasGroup>();
        fadeGroup.alpha = 0f;
        fadeGroup.blocksRaycasts = false;

        var image = new GameObject("FadeImage", typeof(RectTransform), typeof(Image));
        image.transform.SetParent(transform, false);
        var rect = (RectTransform)image.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        image.GetComponent<Image>().color = Color.black;
    }
}
