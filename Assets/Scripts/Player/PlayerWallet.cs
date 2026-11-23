using System;
using UnityEngine;

public class PlayerWallet : MonoBehaviour
{
    public int Coins { get; private set; }

    public event Action<int> CoinsChanged;

    private void Start()
    {
        CoinsChanged?.Invoke(Coins);
    }

    public void AddCoins(int amount)
    {
        Coins += amount;
        CoinsChanged?.Invoke(Coins);
    }
}
