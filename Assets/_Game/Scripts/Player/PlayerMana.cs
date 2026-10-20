using System;
using UnityEngine;

/// <summary>Mana for the arrow skill. Slowly regenerates over time.</summary>
public class PlayerMana : MonoBehaviour
{
    public float Current { get; private set; }
    public float Max { get; private set; }
    public float Normalized => Max > 0f ? Current / Max : 0f;
    public bool IsFull => Current >= Max;

    public event Action Changed;

    float regenPerSecond;

    public void Setup(float max, float regen)
    {
        Max = max;
        Current = max;
        regenPerSecond = regen;
        Changed?.Invoke();
    }

    void Update()
    {
        if (Current >= Max || regenPerSecond <= 0f) return;
        Current = Mathf.Min(Max, Current + regenPerSecond * Time.deltaTime);
        Changed?.Invoke();
    }

    public bool TrySpend(float amount)
    {
        if (Current < amount) return false;
        Current -= amount;
        Changed?.Invoke();
        return true;
    }

    public void Restore(float amount)
    {
        Current = Mathf.Min(Max, Current + amount);
        Changed?.Invoke();
    }

    /// <summary>Changes max mana. Any increase is also added to current mana.</summary>
    public void SetMax(float value)
    {
        float increase = Mathf.Max(0f, value - Max);
        Max = value;
        Current = Mathf.Clamp(Current + increase, 0f, Max);
        Changed?.Invoke();
    }
}
