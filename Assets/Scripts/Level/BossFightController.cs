using System.Collections;
using TMPro;
using UnityEngine;

public class BossFightController : MonoBehaviour
{
    [SerializeField] private BossEnemy boss;
    [SerializeField] private float creditsDelay = 10f;

    [Header("UI")]
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private TextMeshProUGUI victoryText;
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private GameObject bossHealthPanel;

    private bool finished;

    private void Start()
    {
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (boss != null) boss.Defeated += OnBossDefeated;
    }

    private void OnDestroy()
    {
        if (boss != null) boss.Defeated -= OnBossDefeated;
    }

    private void OnBossDefeated(Enemy _)
    {
        if (finished) return;
        finished = true;
        StartCoroutine(VictoryRoutine());
    }

    private IEnumerator VictoryRoutine()
    {
        AudioManager.Instance.StopMusic();
        AudioManager.Instance.PlaySfx(AudioManager.Instance.Victory);

        var player = FindAnyObjectByType<PlayerHealth>();
        if (player != null) player.IsInvincible = true;
        foreach (var enemy in FindObjectsByType<Enemy>()) enemy.enabled = false;

        if (bossHealthPanel != null) bossHealthPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(true);
        if (victoryText != null) victoryText.text = "YOU WON!";

        for (float remaining = creditsDelay; remaining > 0f; remaining -= Time.deltaTime)
        {
            if (countdownText != null) countdownText.text = $"Credits in {Mathf.CeilToInt(remaining)}...";
            if (victoryText != null) victoryText.transform.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * 4f) * 0.06f);
            yield return null;
        }

        SceneLoader.Load(SceneNames.Credits);
    }
}
