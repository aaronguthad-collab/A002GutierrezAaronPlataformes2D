using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteAnimator : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float framesPerSecond = 10f;

    private SpriteRenderer spriteRenderer;
    private float offset;

    public Sprite[] Frames
    {
        get => frames;
        set => frames = value;
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        offset = Random.value;
    }

    private void Update()
    {
        if (frames == null || frames.Length == 0) return;
        int index = (int)((Time.time + offset) * framesPerSecond) % frames.Length;
        spriteRenderer.sprite = frames[index];
    }
}
