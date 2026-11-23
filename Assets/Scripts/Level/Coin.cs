using System.Collections;
using UnityEngine;

public class Coin : MonoBehaviour
{
    [SerializeField] private int value = 1;
    [SerializeField] private float bobAmplitude = 0.12f;
    [SerializeField] private float bobSpeed = 3f;

    private Vector3 origin;
    private bool collected;
    private float phase;

    public int Value => value;

    private void Start()
    {
        origin = transform.position;
        phase = origin.x * 0.7f;
    }

    private void Update()
    {
        if (collected) return;
        transform.position = origin + Vector3.up * (Mathf.Sin(Time.time * bobSpeed + phase) * bobAmplitude);
    }

    public bool Collect()
    {
        if (collected) return false;
        collected = true;
        foreach (var col in GetComponentsInChildren<Collider2D>()) col.enabled = false;
        AudioManager.Instance.PlaySfx(AudioManager.Instance.Coin, 0.8f, 0.04f);
        StartCoroutine(CollectRoutine());
        return true;
    }

    private IEnumerator CollectRoutine()
    {
        var sprite = GetComponentInChildren<SpriteRenderer>();
        Vector3 start = transform.position;
        for (float t = 0f; t < 0.3f; t += Time.deltaTime)
        {
            float k = t / 0.3f;
            transform.position = start + Vector3.up * (k * 1.2f);
            if (sprite != null) sprite.color = new Color(1f, 1f, 1f, 1f - k);
            yield return null;
        }
        Destroy(gameObject);
    }
}
