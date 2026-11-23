using System;
using System.Collections;
using Platformer2DSystem;
using UnityEngine;

[RequireComponent(typeof(Actor))]
public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 6;
    [SerializeField] private float invulnerableTime = 1.2f;
    [SerializeField] private float knockbackSpeed = 22f;
    [SerializeField] private float knockbackUpSpeed = 18f;
    [SerializeField] private float restartDelay = 1.5f;
    [SerializeField] private SpriteRenderer graphics;

    private Actor actor;
    private float invulnerableUntil;

    public int MaxHealth => maxHealth;
    public int CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }
    public bool IsInvulnerable => IsInvincible || Time.time < invulnerableUntil;

    public bool IsInvincible { get; set; }

    public event Action<int, int> HealthChanged;
    public event Action Died;

    private void Awake()
    {
        actor = GetComponent<Actor>();
        if (graphics == null) graphics = GetComponentInChildren<SpriteRenderer>();
        CurrentHealth = maxHealth;
    }

    private void Start()
    {
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public void TakeDamage(int amount, Vector2 sourcePosition)
    {
        if (IsDead || IsInvulnerable || amount <= 0) return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        HealthChanged?.Invoke(CurrentHealth, maxHealth);

        float side = transform.position.x >= sourcePosition.x ? 1f : -1f;
        actor.velocity = new Vector2(side * knockbackSpeed, knockbackUpSpeed);
        if (TryGetComponent(out Platformer2DSystem.Example.PlayerController controller)) controller.LockControl(0.3f);

        if (CurrentHealth == 0)
        {
            Die();
            return;
        }

        AudioManager.Instance.PlaySfx(AudioManager.Instance.Hurt, 1f, 0.05f);
        invulnerableUntil = Time.time + invulnerableTime;
        StartCoroutine(BlinkRoutine());
    }

    public void Heal(int amount)
    {
        if (IsDead) return;
        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public void Kill()
    {
        if (IsDead || IsInvincible) return;
        CurrentHealth = 0;
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
        Die();
    }

    private void Die()
    {
        IsDead = true;
        Died?.Invoke();
        AudioManager.Instance.PlaySfx(AudioManager.Instance.PlayerDeath);

        var controller = GetComponent<Platformer2DSystem.Example.PlayerController>();
        if (controller != null) controller.enabled = false;
        actor.velocity = new Vector2(0f, knockbackUpSpeed * 1.5f);
        if (graphics != null) graphics.color = new Color(1f, 0.4f, 0.4f);

        StartCoroutine(RestartRoutine());
    }

    private IEnumerator RestartRoutine()
    {
        yield return new WaitForSeconds(restartDelay);
        SceneLoader.ReloadCurrent();
    }

    private IEnumerator BlinkRoutine()
    {
        if (graphics == null) yield break;
        while (Time.time < invulnerableUntil && !IsDead)
        {
            graphics.enabled = !graphics.enabled;
            yield return new WaitForSeconds(0.08f);
        }
        graphics.enabled = true;
    }
}
