using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private PlayerHealth player;
    [SerializeField] private Slider slider;
    [SerializeField] private Image fill;
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private Gradient colorByHealth;
    [SerializeField] private float smoothSpeed = 8f;

    private float target = 1f;

    private void Awake()
    {
        if (player == null) player = FindAnyObjectByType<PlayerHealth>();
    }

    private void OnEnable()
    {
        if (player != null) player.HealthChanged += OnHealthChanged;
    }

    private void OnDisable()
    {
        if (player != null) player.HealthChanged -= OnHealthChanged;
    }

    private void OnHealthChanged(int current, int max)
    {
        target = max > 0 ? (float)current / max : 0f;
        if (label != null) label.text = $"{current} / {max}";
    }

    private void Update()
    {
        if (slider == null) return;
        slider.value = Mathf.MoveTowards(slider.value, target, smoothSpeed * Time.deltaTime);
        if (fill != null && colorByHealth != null) fill.color = colorByHealth.Evaluate(slider.value);
    }
}
