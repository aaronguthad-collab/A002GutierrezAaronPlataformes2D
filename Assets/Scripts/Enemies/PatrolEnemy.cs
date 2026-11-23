using UnityEngine;

public class PatrolEnemy : Enemy
{
    [Header("Patrulla")]
    [SerializeField] private float speed = 2f;
    [SerializeField] private float patrolDistance = 3f;
    [SerializeField] private int direction = 1;

    private float startX;

    protected override void Start()
    {
        base.Start();
        startX = transform.position.x;
        FaceDirection(direction);
    }

    private void Update()
    {
        float x = transform.position.x;
        if (x > startX + patrolDistance) direction = -1;
        else if (x < startX - patrolDistance) direction = 1;

        transform.Translate(speed * direction * Time.deltaTime * Vector2.right);
        FaceDirection(direction);
    }

    private void OnDrawGizmosSelected()
    {
        float x = Application.isPlaying ? startX : transform.position.x;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(x - patrolDistance, transform.position.y), new Vector3(x + patrolDistance, transform.position.y));
    }
}
