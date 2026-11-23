using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class LevelTitle : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private string title = "Level";
    [SerializeField] private float holdTime = 2f;
    [SerializeField] private float fadeTime = 0.6f;

    private IEnumerator Start()
    {
        var group = GetComponent<CanvasGroup>();
        if (label != null) label.text = title;
        group.alpha = 1f;
        yield return new WaitForSeconds(holdTime);
        for (float t = 0f; t < fadeTime; t += Time.deltaTime)
        {
            group.alpha = 1f - t / fadeTime;
            yield return null;
        }
        gameObject.SetActive(false);
    }
}
