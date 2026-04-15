using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StatusEffectManager
{
    public StatusEffectManager(CombatUnit unit) { _owningUnit = unit; }


    private int comboStatusEffectMultiplier = 5;
    private List<StatusEffectSO> _activeStatusEffects = new();
    private CombatUnit _owningUnit;

    public bool IsStunned { get; private set; }


    public void AddEffect(StatusEffectSO newEffect)
    {
        if (newEffect == null)
            return;

        if (_activeStatusEffects.Count > 0)
        {
            var activeFireEffect = _activeStatusEffects.Find(e => e.Name == "Fire");
            var activeLightningEffect = _activeStatusEffects.Find(e => e.Name == "Lightning");

            if (activeFireEffect != null && newEffect.Name == "Lightning")
            {
                _owningUnit.ReceiveDamage((activeFireEffect.DotAmount + newEffect.DotAmount) * comboStatusEffectMultiplier);
                RemoveEffect(activeFireEffect);
                return;
            }
            else if (activeLightningEffect != null && newEffect.Name == "Fire")
            {
                _owningUnit.ReceiveDamage((activeLightningEffect.DotAmount + newEffect.DotAmount) * comboStatusEffectMultiplier);
                RemoveEffect(activeLightningEffect);
                return;
            }
        }

        if (newEffect.Name == "Fire")
            _owningUnit.ReceiveDamage(newEffect.DotAmount);

        if (_activeStatusEffects.Contains(newEffect))
        {
            _activeStatusEffects.Find(e => e.Name == newEffect.Name).TurnAffectedCount = newEffect.TurnAffectedCount;
            return;
        }

        var clone = GameObject.Instantiate(newEffect);

        ParticleSystem effect = GameObject.Instantiate(newEffect.ParticleEffect, _owningUnit.transform.position, Quaternion.identity);
        effect.name = newEffect.Name;
        effect.transform.SetParent(_owningUnit.transform);
        effect.Play();

        effect.GetComponent<ParticleSystemRenderer>().sortingOrder = 11;

        _activeStatusEffects.Add(clone);

        if (clone.IsStun)
        {
            IsStunned = true;

            _owningUnit.GetComponent<SpriteRenderer>().color = Color.black;

            if (_owningUnit is PlayerUnit)
            {
                TurnManager.I.RemainingMoveSteps = 1;
                TurnManager.I.NotifyPlayerMovedStep(1);
                TurnManager.I.ExecuteSkipAction();
            }
        }
    }

    public void UpdateEffects()
    {
        if (IsStunned)
        {
            IsStunned = false;
            _owningUnit.GetComponent<SpriteRenderer>().color = Color.white;
        }

        for (int i = _activeStatusEffects.Count - 1; i >= 0; i--)
        {
            if (_activeStatusEffects[i].TurnAffectedCount == 0)
            {
                RemoveEffect(_activeStatusEffects[i]);
                continue;
            }

            if (_activeStatusEffects[i].DotAmount > 0)
            {
                _owningUnit.ReceiveDamage(_activeStatusEffects[i].DotAmount);
            }

            _activeStatusEffects[i].TurnAffectedCount--;
        }
    }

    public void RemoveEffect(StatusEffectSO effect)
    {
        _activeStatusEffects.Remove(effect);

        effect.ParticleEffect.Stop();

        ParticleSystem pfx = _owningUnit.GetComponentsInChildren<ParticleSystem>()
    .FirstOrDefault(e => e.name == effect.Name);
        if (pfx != null) GameObject.Destroy(pfx.gameObject);

        GameObject.Destroy(effect);
    }
}
