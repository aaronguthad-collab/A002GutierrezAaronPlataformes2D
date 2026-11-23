using UnityEngine;

public class FlyingEnemy : Enemy
{
    [Header("Vol")]
    [SerializeField] private float speed = 2.5f;
    [SerializeField] private float patrolDistance = 4f;
    [SerializeField] private float waveAmplitude = 1f;
    [SerializeField] private float waveFrequency = 1.5f;

    private Vector3 origin;
    private float phase;

    protected override void Start()
    {
        base.Start();
        origin = transform.position;
        phase = Random.value * Mathf.PI * 2f;
    }

    private void Update()
    {
        float time = Time.time * speed / Mathf.Max(0.01f, patrolDistance) + phase;
        float x = origin.x + Mathf.Sin(time) * patrolDistance;
        float y = origin.y + Mathf.Sin(Time.time * waveFrequency * Mathf.PI * 2f + phase) * waveAmplitude;
        FaceDirection(Mathf.Cos(time));
        transform.position = new Vector3(x, y, origin.z);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 o = Application.isPlaying ? origin : transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(o, new Vector3(patrolDistance * 2f, waveAmplitude * 2f, 0f));
    }
}
