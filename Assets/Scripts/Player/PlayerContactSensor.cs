using System.Collections.Generic;
using Platformer2DSystem;
using UnityEngine;

[RequireComponent(typeof(Actor))]
public class PlayerContactSensor : MonoBehaviour
{
    [SerializeField] private float stompBounceMultiplier = 0.75f;
    [SerializeField] private float stompTolerance = 0.35f;

    private readonly List<Collider2D> overlaps = new(16);
    private Actor actor;
    private Jumper jumper;
    private PlayerHealth health;
    private PlayerWallet wallet;
    private ContactFilter2D filter;
    private float previousBottom = float.NegativeInfinity;

    private void Awake()
    {
        actor = GetComponent<Actor>();
        jumper = GetComponent<Jumper>();
        health = GetComponent<PlayerHealth>();
        wallet = GetComponent<PlayerWallet>();
        filter = new ContactFilter2D { useTriggers = true, useLayerMask = false };
    }

    private void Update()
    {
        if (health != null && health.IsDead) return;

        Bounds bounds = actor.CollisionBox.bounds;
        if (float.IsNegativeInfinity(previousBottom)) previousBottom = bounds.min.y;
        Physics2D.OverlapBox(bounds.center, bounds.size, 0f, filter, overlaps);

        bool stompedThisFrame = false;
        foreach (Collider2D other in overlaps)
        {
            if (other == null || other == actor.CollisionBox) continue;

            if (other.TryGetComponent(out Coin coin))
            {
                if (coin.Collect()) wallet.AddCoins(coin.Value);
                continue;
            }

            if (other.TryGetComponent(out HealthPickup pickup))
            {
                pickup.Collect(health);
                continue;
            }

            if (other.TryGetComponent(out KillZone _))
            {
                health.Kill();
                return;
            }

            if (other.TryGetComponent(out Portal portal))
            {
                portal.Enter();
                continue;
            }

            Enemy enemy = other.GetComponentInParent<Enemy>();
            if (enemy != null && enemy.IsAlive)
            {
                if (IsStomp(bounds, other.bounds) || stompedThisFrame)
                {
                    stompedThisFrame = true;
                    enemy.Stomp(this);
                    float bounce = jumper != null ? jumper.MaxJumpVelocity * stompBounceMultiplier : 40f;
                    actor.velocity.y = bounce;
                }
                else
                {
                    enemy.TouchPlayer(health);
                }
            }
        }

        previousBottom = bounds.min.y;
    }

    private bool IsStomp(Bounds player, Bounds enemy)
    {
        bool falling = actor.velocity.y <= 0.01f;
        bool cameFromAbove = Mathf.Max(previousBottom, player.min.y) >= enemy.max.y - stompTolerance;
        return falling && cameFromAbove;
    }
}
