using System.Collections;
using UnityEngine;

public class BossEnemy : Enemy
{
    private enum State { Walk, Telegraph, Charge, Stunned }

    [Header("Moviment")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float chargeSpeed = 11f;
    [SerializeField] private float speedIncreasePerHit = 0.25f;
    [SerializeField] private float arenaMinX = -10f;
    [SerializeField] private float arenaMaxX = 10f;
    [SerializeField] private float chargeInterval = 3.5f;
    [SerializeField] private float telegraphTime = 0.7f;
    [SerializeField] private float stunTime = 0.8f;
    [SerializeField] private Transform player;

    private State state = State.Walk;
    private int direction = -1;
    private float stateTimer;
    private float speedMultiplier = 1f;

    public void SetArena(float minX, float maxX)
    {
        arenaMinX = minX;
        arenaMaxX = maxX;
    }

    protected override void Start()
    {
        base.Start();
        if (player == null)
        {
            var health = FindAnyObjectByType<PlayerHealth>();
            if (health != null) player = health.transform;
        }
        stateTimer = chargeInterval;
    }

    private void Update()
    {
        stateTimer -= Time.deltaTime;

        switch (state)
        {
            case State.Walk:
                Move(walkSpeed * speedMultiplier);
                if (stateTimer <= 0f && player != null)
                {
                    state = State.Telegraph;
                    stateTimer = telegraphTime;
                    direction = player.position.x >= transform.position.x ? 1 : -1;
                    FaceDirection(direction);
                }
                break;

            case State.Telegraph:
                if (graphics != null)
                    graphics.transform.localPosition = new Vector3(Mathf.Sin(Time.time * 60f) * 0.06f, graphics.transform.localPosition.y, 0f);
                if (stateTimer <= 0f)
                {
                    if (graphics != null) graphics.transform.localPosition = new Vector3(0f, graphics.transform.localPosition.y, 0f);
                    state = State.Charge;
                }
                break;

            case State.Charge:
                if (!Move(chargeSpeed * speedMultiplier))
                {
                    state = State.Stunned;
                    stateTimer = stunTime;
                }
                break;

            case State.Stunned:
                if (stateTimer <= 0f)
                {
                    state = State.Walk;
                    stateTimer = chargeInterval / speedMultiplier;
                }
                break;
        }
    }

    private bool Move(float speed)
    {
        float x = transform.position.x + direction * speed * Time.deltaTime;
        bool hitWall = false;
        if (x > arenaMaxX) { x = arenaMaxX; direction = -1; hitWall = true; }
        else if (x < arenaMinX) { x = arenaMinX; direction = 1; hitWall = true; }

        transform.position = new Vector3(x, transform.position.y, transform.position.z);
        FaceDirection(direction);
        return !hitWall;
    }

    protected override void OnHit()
    {
        AudioManager.Instance.PlaySfx(AudioManager.Instance.BossHit);
        speedMultiplier += speedIncreasePerHit;
        state = State.Stunned;
        stateTimer = stunTime;
        StartCoroutine(FlashRoutine(hitInvulnerableTime));
    }

    protected override void OnDefeated()
    {
        AudioManager.Instance.PlaySfx(AudioManager.Instance.BossDefeated);
        StartCoroutine(DefeatRoutine());
    }

    private IEnumerator DefeatRoutine()
    {
        foreach (var col in GetComponentsInChildren<Collider2D>()) col.enabled = false;
        enabled = false;

        Vector3 start = transform.position;
        for (float t = 0f; t < 1.5f; t += Time.deltaTime)
        {
            transform.position = start + (Vector3)(Random.insideUnitCircle * 0.15f);
            if (graphics != null) graphics.color = new Color(1f, 0.5f, 0.5f, 1f - t / 1.5f);
            yield return null;
        }
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(arenaMinX, transform.position.y), new Vector3(arenaMaxX, transform.position.y));
    }
}
