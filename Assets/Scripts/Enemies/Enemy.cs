using System;
using System.Collections;
using UnityEngine;

public abstract class Enemy : MonoBehaviour
{
    [Header("Combat")]
    [SerializeField] protected int maxLives = 1;
    [SerializeField] protected int contactDamage = 1;
    [SerializeField] protected float hitInvulnerableTime = 0.25f;

    [Header("Visual")]
    [SerializeField] protected SpriteRenderer graphics;

    private float invulnerableUntil;

    public int Lives { get; private set; }
    public int MaxLives => maxLives;
    public bool IsAlive => Lives > 0;
    public bool IsInvulnerable => Time.time < invulnerableUntil;

    public event Action<int, int> LivesChanged;
    public event Action<Enemy> Defeated;

    protected virtual void Awake()
    {
        Lives = maxLives;
        if (graphics == null) graphics = GetComponentInChildren<SpriteRenderer>();
    }

    protected virtual void Start()
    {
        LivesChanged?.Invoke(Lives, maxLives);
    }

    public void TouchPlayer(PlayerHealth player)
    {
        if (!IsAlive || player == null) return;
        player.TakeDamage(contactDamage, transform.position);
    }

    public void Stomp(PlayerContactSensor player)
    {
        if (!IsAlive || IsInvulnerable) return;

        Lives--;
        invulnerableUntil = Time.time + hitInvulnerableTime;
        LivesChanged?.Invoke(Lives, maxLives);

        if (Lives <= 0)
        {
            OnDefeated();
            Defeated?.Invoke(this);
        }
        else
        {
            OnHit();
        }
    }

    protected virtual void OnHit()
    {
        AudioManager.Instance.PlaySfx(AudioManager.Instance.Stomp);
        StartCoroutine(FlashRoutine(hitInvulnerableTime));
    }

    protected virtual void OnDefeated()
    {
        AudioManager.Instance.PlaySfx(AudioManager.Instance.Stomp, 1f, 0.1f);
        StartCoroutine(SquashAndDestroyRoutine());
    }

    protected IEnumerator FlashRoutine(float duration)
    {
        if (graphics == null) yield break;
        Color original = Color.white;
        for (float t = 0f; t < duration; t += 0.1f)
        {
            graphics.color = graphics.color == original ? new Color(1f, 0.35f, 0.35f) : original;
            yield return new WaitForSeconds(0.1f);
        }
        graphics.color = original;
    }

    private IEnumerator SquashAndDestroyRoutine()
    {
        foreach (var col in GetComponentsInChildren<Collider2D>()) col.enabled = false;
        enabled = false;

        Transform visual = graphics != null ? graphics.transform : transform;
        Vector3 start = visual.localScale;
        Vector3 squashed = new(start.x * 1.3f, start.y * 0.2f, start.z);
        for (float t = 0f; t < 0.25f; t += Time.deltaTime)
        {
            visual.localScale = Vector3.Lerp(start, squashed, t / 0.25f);
            if (graphics != null) graphics.color = new Color(1f, 1f, 1f, 1f - t / 0.25f);
            yield return null;
        }
        Destroy(gameObject);
    }

    protected void FaceDirection(float direction)
    {
        if (graphics == null || Mathf.Approximately(direction, 0f)) return;
        graphics.flipX = direction > 0f;
    }
}
