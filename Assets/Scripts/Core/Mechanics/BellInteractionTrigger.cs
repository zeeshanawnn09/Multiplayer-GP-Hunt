using UnityEngine;

public class BellInteractionTrigger : MonoBehaviour
{
    private RitualSystem _ritualSystem;

    public void Initialize(RitualSystem ritualSystem)
    {
        _ritualSystem = ritualSystem;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_ritualSystem == null || !other.CompareTag("Player"))
        {
            return;
        }

        PlayerControls playerControls = other.GetComponentInParent<PlayerControls>();
        if (playerControls == null || playerControls.gameObject != PlayerControls.localPlayerInstance || !playerControls.IsPriest)
        {
            return;
        }

        _ritualSystem.NotifyBellTriggerEntered(playerControls);
    }

    private void OnTriggerStay(Collider other)
    {
        if (_ritualSystem == null || !other.CompareTag("Player"))
        {
            return;
        }

        PlayerControls playerControls = other.GetComponentInParent<PlayerControls>();
        if (playerControls == null || playerControls.gameObject != PlayerControls.localPlayerInstance || !playerControls.IsPriest)
        {
            return;
        }

        _ritualSystem.NotifyBellTriggerEntered(playerControls);
    }

    private void OnTriggerExit(Collider other)
    {
        if (_ritualSystem == null || !other.CompareTag("Player"))
        {
            return;
        }

        PlayerControls playerControls = other.GetComponentInParent<PlayerControls>();
        if (playerControls == null || playerControls.gameObject != PlayerControls.localPlayerInstance)
        {
            return;
        }

        _ritualSystem.NotifyBellTriggerExited(playerControls);
    }
}