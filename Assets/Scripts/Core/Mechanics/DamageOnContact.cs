using UnityEngine;
using System.Collections.Generic;

public class DamageOnContact : MonoBehaviour
{
    [SerializeField]
    private int damageAmount = 10;

    [SerializeField]
    private float damageInterval = 1f;

    private readonly Dictionary<int, float> _nextDamageTimeByPlayer = new Dictionary<int, float>();

    private void OnTriggerEnter(Collider other)
    {
        TryApplyDamage(other.gameObject, true);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryApplyDamage(collision.gameObject, true);
    }

    private void OnTriggerStay(Collider other)
    {
        TryApplyDamage(other.gameObject, false);
    }

    private void OnCollisionStay(Collision collision)
    {
        TryApplyDamage(collision.gameObject, false);
    }

    private void OnTriggerExit(Collider other)
    {
        RemovePlayerCooldown(other.gameObject);
    }

    private void OnCollisionExit(Collision collision)
    {
        RemovePlayerCooldown(collision.gameObject);
    }

    private void TryApplyDamage(GameObject otherObject, bool forceFirstTick)
    {
        if (!otherObject.CompareTag("Player"))
        {
            return;
        }

        HealthSystem healthSystem = otherObject.GetComponentInParent<HealthSystem>();
        if (healthSystem == null)
        {
            return;
        }

        int playerId = healthSystem.gameObject.GetInstanceID();
        float now = Time.time;

        if (forceFirstTick || ShouldDamagePlayer(playerId, now))
        {
            healthSystem.ApplyDamage(damageAmount);
            _nextDamageTimeByPlayer[playerId] = now + Mathf.Max(0.05f, damageInterval);
        }
    }

    private bool ShouldDamagePlayer(int playerId, float currentTime)
    {
        if (!_nextDamageTimeByPlayer.TryGetValue(playerId, out float nextTime))
        {
            return true;
        }

        return currentTime >= nextTime;
    }

    private void RemovePlayerCooldown(GameObject otherObject)
    {
        if (!otherObject.CompareTag("Player"))
        {
            return;
        }

        HealthSystem healthSystem = otherObject.GetComponentInParent<HealthSystem>();
        if (healthSystem != null)
        {
            _nextDamageTimeByPlayer.Remove(healthSystem.gameObject.GetInstanceID());
        }
    }
}