using UnityEngine;

public class PlayerRespawnSystem : MonoBehaviour
{
    [SerializeField]
    private Transform respawnPlatform;

    [SerializeField]
    private string respawnPlatformTag = "GhostRespawnPlatform";

    [SerializeField]
    private float respawnSurfacePadding = 0.1f;

    private void Awake()
    {
        ResolveRespawnPlatform();
    }

    public bool TryGetRespawnPosition(out Vector3 respawnPosition)
    {
        ResolveRespawnPlatform();

        if (respawnPlatform == null)
        {
            respawnPosition = transform.position;
            return false;
        }

        if (TryGetPlatformSurface(respawnPlatform, out Vector3 surfaceCenter, out float surfaceTopY))
        {
            respawnPosition = new Vector3(surfaceCenter.x, surfaceTopY + respawnSurfacePadding, surfaceCenter.z);
            return true;
        }

        respawnPosition = respawnPlatform.position + Vector3.up * respawnSurfacePadding;
        return true;
    }

    private void ResolveRespawnPlatform()
    {
        if (respawnPlatform != null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(respawnPlatformTag))
        {
            return;
        }

        GameObject platform = GameObject.FindWithTag(respawnPlatformTag);
        if (platform != null)
        {
            respawnPlatform = platform.transform;
        }
    }

    private bool TryGetPlatformSurface(Transform platformTransform, out Vector3 surfaceCenter, out float surfaceTopY)
    {
        surfaceCenter = platformTransform.position;
        surfaceTopY = platformTransform.position.y;

        Collider platformCollider = platformTransform.GetComponent<Collider>();
        if (platformCollider != null)
        {
            Bounds bounds = platformCollider.bounds;
            surfaceCenter = bounds.center;
            surfaceTopY = bounds.max.y;
            return true;
        }

        Renderer platformRenderer = platformTransform.GetComponentInChildren<Renderer>(true);
        if (platformRenderer != null)
        {
            Bounds bounds = platformRenderer.bounds;
            surfaceCenter = bounds.center;
            surfaceTopY = bounds.max.y;
            return true;
        }

        return false;
    }
}
