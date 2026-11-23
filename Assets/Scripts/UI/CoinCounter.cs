using TMPro;
using UnityEngine;

public class CoinCounter : MonoBehaviour
{
    [SerializeField] private PlayerWallet wallet;
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private Transform punchTarget;

    private float punch;

    private void Awake()
    {
        if (wallet == null) wallet = FindAnyObjectByType<PlayerWallet>();
        if (punchTarget == null) punchTarget = transform;
    }

    private void OnEnable()
    {
        if (wallet != null) wallet.CoinsChanged += OnCoinsChanged;
    }

    private void OnDisable()
    {
        if (wallet != null) wallet.CoinsChanged -= OnCoinsChanged;
    }

    private void OnCoinsChanged(int coins)
    {
        if (label != null) label.text = $"x {coins}";
        if (coins > 0) punch = 1f;
    }

    private void Update()
    {
        punch = Mathf.MoveTowards(punch, 0f, Time.deltaTime * 5f);
        punchTarget.localScale = Vector3.one * (1f + punch * 0.3f);
    }
}
