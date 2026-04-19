using System;
using UnityEngine;

public class PooledProjectile : MonoBehaviour
{
    [SerializeField]
    private float deactivateDistanceThreshold = 0.02f;

    private Vector3 _targetPosition;
    private float _moveSpeed;
    private Action<PooledProjectile> _releaseCallback;
    private bool _isActive;

    public void Launch(Vector3 startPosition, Vector3 targetPosition, float moveSpeed, Action<PooledProjectile> releaseCallback)
    {
        transform.position = startPosition;
        _targetPosition = targetPosition;
        _moveSpeed = Mathf.Max(0.01f, moveSpeed);
        _releaseCallback = releaseCallback;
        _isActive = true;
        gameObject.SetActive(true);
    }

    private void Update()
    {
        if (!_isActive)
        {
            return;
        }

        transform.position = Vector3.MoveTowards(transform.position, _targetPosition, _moveSpeed * Time.deltaTime);

        if ((transform.position - _targetPosition).sqrMagnitude <= deactivateDistanceThreshold * deactivateDistanceThreshold)
        {
            ReleaseToPool();
        }
    }

    public void ReleaseToPool()
    {
        if (!_isActive)
        {
            return;
        }

        _isActive = false;
        _releaseCallback?.Invoke(this);
    }
}
