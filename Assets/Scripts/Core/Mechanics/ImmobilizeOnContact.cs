using UnityEngine;
using System.Collections.Generic;

public class ImmobilizeOnContact : MonoBehaviour
{
    [SerializeField]
    private float immobilizeDuration = 10f;

    private readonly HashSet<int> _playersInsideTrap = new HashSet<int>();
    private readonly HashSet<int> _detectedThisFrame = new HashSet<int>();
    private Collider _trapCollider;

    private void Awake()
    {
        _trapCollider = GetComponent<Collider>();
        if (_trapCollider == null)
        {
            Debug.LogWarning("[ImmobilizeOnContact] Missing Collider on trap object.");
        }
    }

    private void Update()
    {
        if (_trapCollider == null)
        {
            return;
        }

        _detectedThisFrame.Clear();

        // CharacterController setups can miss trigger/collision callbacks. Overlap ensures detection still works.
        Bounds bounds = _trapCollider.bounds;
        Collider[] overlaps = Physics.OverlapBox(bounds.center, bounds.extents, _trapCollider.transform.rotation, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        for (int i = 0; i < overlaps.Length; i++)
        {
            TryImmobilize(overlaps[i], registerPresenceOnly: false);
        }

        _playersInsideTrap.RemoveWhere(id => !_detectedThisFrame.Contains(id));
    }

    private void OnTriggerEnter(Collider other)
    {
        TryImmobilize(other, registerPresenceOnly: false);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null)
        {
            return;
        }

        TryImmobilize(collision.collider, registerPresenceOnly: false);
    }

    private void TryImmobilize(Collider other, bool registerPresenceOnly)
    {
        if (other == null)
        {
            return;
        }

        if (!TryGetPlayerControls(other, out PlayerControls playerControls))
        {
            return;
        }

        int playerId = GetPlayerIdentifier(playerControls);
        _detectedThisFrame.Add(playerId);

        if (_playersInsideTrap.Contains(playerId))
        {
            return;
        }

        _playersInsideTrap.Add(playerId);
        if (registerPresenceOnly)
        {
            return;
        }

        if (!playerControls.IsPriest)
        {
            return;
        }

        playerControls.Immobilize(immobilizeDuration);
    }

    private bool TryGetPlayerControls(Collider other, out PlayerControls playerControls)
    {
        playerControls = other.GetComponentInParent<PlayerControls>();
        if (playerControls == null && other.attachedRigidbody != null)
        {
            playerControls = other.attachedRigidbody.GetComponentInParent<PlayerControls>();
        }

        return playerControls != null;
    }

    private static int GetPlayerIdentifier(PlayerControls playerControls)
    {
        return playerControls.photonView != null ? playerControls.photonView.ViewID : playerControls.GetInstanceID();
    }
}