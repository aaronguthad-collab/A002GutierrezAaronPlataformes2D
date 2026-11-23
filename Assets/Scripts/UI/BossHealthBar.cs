using UnityEngine;
using UnityEngine.UI;

public class BossHealthBar : MonoBehaviour
{
    [SerializeField] private Enemy boss;
    [SerializeField] private Image[] hearts;
    [SerializeField] private Color fullColor = new(0.9f, 0.15f, 0.2f);
    [SerializeField] private Color emptyColor = new(0.2f, 0.2f, 0.2f, 0.6f);

    private void OnEnable()
    {
        if (boss != null) boss.LivesChanged += OnLivesChanged;
    }

    private void OnDisable()
    {
        if (boss != null) boss.LivesChanged -= OnLivesChanged;
    }

    private void OnLivesChanged(int lives, int max)
    {
        for (int i = 0; i < hearts.Length; i++)
        {
            hearts[i].color = i < lives ? fullColor : emptyColor;
        }
    }
}
