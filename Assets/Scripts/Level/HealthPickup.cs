using UnityEngine;

public class HealthPickup : MonoBehaviour
{
    [SerializeField] private int healAmount = 2;

    private bool collected;

    public void Collect(PlayerHealth player)
    {
        if (collected || player == null || player.CurrentHealth >= player.MaxHealth) return;
        collected = true;
        player.Heal(healAmount);
        AudioManager.Instance.PlaySfx(AudioManager.Instance.Coin, 1f);
        Destroy(gameObject);
    }

    private void Update()
    {
        transform.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * 5f) * 0.08f);
    }
}
