using System;
using System.Collections.Generic;
using EasyButtons;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Events;

namespace Zodiac.Core {
public class HealthController : MonoBehaviour
{
    [SerializeField] private float maxHealth;
    [SerializeField] private float health;

    [Serializable] public class HealthUpdateEvent : UnityEvent {}

    [SerializeField] private HealthUpdateEvent
        onDamaged = new(),
        onKilled  = new(),
        onHealed  = new(),
        onRevived = new(),
        onChanged = new();

    public bool IsAlive => health > 0;

    public float Value => health;

    void Start()
    {
        health = maxHealth;
    }

    public void Damage(float damage)
    {
        health -= damage;
        onDamaged.Invoke();
        ClampHealth();
        if (!IsAlive) onKilled.Invoke();
        onChanged.Invoke();
    }

    public void Kill()
    {
        health = 0;
        onDamaged.Invoke();
        onKilled.Invoke();
        onChanged.Invoke();
    }

    public void Heal(float hp)
    {
        bool revived = !IsAlive;
        health += hp;
        ClampHealth();
        onHealed.Invoke();
        if (revived) onRevived.Invoke();
        onChanged.Invoke();
    }

    [Button("Set Health")]
    public void SetTo(float hp)
    {
        hp = Mathf.Clamp(hp, 0f, maxHealth);
        if (hp == 0)
        {
            Kill();
            return;
        }
        bool wasDead = !IsAlive;
        float delta = health - hp;
        if (delta == 0) return;
        health = hp;
        if (delta > 0)
        {
            onHealed.Invoke();
            if (wasDead) onRevived.Invoke();
        }
        else
        {
            onDamaged.Invoke();
        }
        onChanged.Invoke();
    }

    private void ClampHealth()
    {
        health = Mathf.Clamp(health, 0f, maxHealth);
    }
}
}