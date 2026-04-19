using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class SoulAI : MonoBehaviourPunCallbacks, IPunObservable
{
    [SerializeField]
    private float moveSpeed = 2.5f;

    [SerializeField]
    private float wanderRadius = 4f;

    [SerializeField]
    private float wanderRetargetIntervalSeconds = 2.5f;

    [SerializeField]
    private float lampSearchIntervalSeconds = 0.35f;

    [SerializeField]
    private float lampExtinguishDistance = 1.1f;

    [SerializeField]
    private int maxHealth = 3;

    [SerializeField]
    private bool debugLogs = true;

    private AISoulSystem owningSpawnSystem;
    private SoulPickup soulPickup;
    private PhotonView soulPhotonView;
    private LitLampBehavior currentLampTarget;
    private Vector3 wanderCenter;
    private Vector3 wanderTarget;
    private Vector3 networkPosition;
    private Quaternion networkRotation = Quaternion.identity;
    private float nextLampSearchTime;
    private float nextWanderRetargetTime;
    private int currentHealth;
    private bool isDroppedSoul;
    private bool isDestroyed;

    private void Awake()
    {
        soulPhotonView = GetComponent<PhotonView>();
        soulPickup = GetComponent<SoulPickup>();
        currentHealth = maxHealth;

        EnsureObservableRegistration();

        wanderCenter = transform.position;
        wanderTarget = transform.position;
        networkPosition = transform.position;
        networkRotation = transform.rotation;
    }

    private void EnsureObservableRegistration()
    {
        if (soulPhotonView == null)
        {
            return;
        }

        if (soulPhotonView.ObservedComponents == null)
        {
            soulPhotonView.ObservedComponents = new List<Component>();
        }

        if (!soulPhotonView.ObservedComponents.Contains(this))
        {
            soulPhotonView.ObservedComponents.Add(this);
        }

        soulPhotonView.observableSearch = PhotonView.ObservableSearch.Manual;
    }

    public void InitializeSpawned(AISoulSystem spawnSystem, SoulPickup pickup)
    {
        owningSpawnSystem = spawnSystem;
        soulPickup = pickup;
        isDroppedSoul = false;
        currentHealth = maxHealth;
        wanderCenter = transform.position;
        wanderTarget = transform.position;
    }

    public void ActivateDroppedSoul(AISoulSystem spawnSystem, SoulPickup pickup)
    {
        owningSpawnSystem = spawnSystem;
        soulPickup = pickup;
        isDroppedSoul = true;
        currentHealth = maxHealth;
        wanderCenter = transform.position;
        wanderTarget = transform.position;
        nextLampSearchTime = 0f;
        nextWanderRetargetTime = 0f;

        if (debugLogs)
        {
            Debug.Log($"[SoulAI] Dropped soul activated on '{name}' at {transform.position}");
        }
    }

    private void Update()
    {
        if (isDestroyed || soulPhotonView == null)
        {
            return;
        }

        if (!soulPhotonView.IsMine)
        {
            transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * 12f);
            transform.rotation = Quaternion.Lerp(transform.rotation, networkRotation, Time.deltaTime * 12f);
            return;
        }

        if (!isDroppedSoul)
        {
            return;
        }

        UpdateDroppedSoulMovement();
    }

    private void UpdateDroppedSoulMovement()
    {
        if (currentHealth <= 0)
        {
            return;
        }

        LitLampBehavior targetLamp = GetNearestLitLamp();
        if (targetLamp != null)
        {
            currentLampTarget = targetLamp;
            if (debugLogs)
            {
                Debug.Log($"[SoulAI] '{name}' targeting lamp '{targetLamp.name}' at {targetLamp.GetLightWorldPosition()}");
            }
            MoveTowardsLamp(targetLamp);
            return;
        }

        currentLampTarget = null;
        if (debugLogs)
        {
            Debug.Log($"[SoulAI] '{name}' found no lit lamps. Wandering.");
        }
        WanderAround();
    }

    private LitLampBehavior GetNearestLitLamp()
    {
        if (Time.time < nextLampSearchTime)
        {
            return currentLampTarget != null && currentLampTarget.IsLit ? currentLampTarget : null;
        }

        nextLampSearchTime = Time.time + lampSearchIntervalSeconds;

        LitLampBehavior[] lamps = FindObjectsByType<LitLampBehavior>(FindObjectsSortMode.None);
        LitLampBehavior nearestLitLamp = null;
        float nearestSqrDistance = float.MaxValue;

        for (int i = 0; i < lamps.Length; i++)
        {
            LitLampBehavior lamp = lamps[i];
            if (lamp == null || !lamp.IsLit)
            {
                continue;
            }

            float sqrDistance = (lamp.GetLightWorldPosition() - transform.position).sqrMagnitude;
            if (sqrDistance < nearestSqrDistance)
            {
                nearestSqrDistance = sqrDistance;
                nearestLitLamp = lamp;
            }
        }

        return nearestLitLamp;
    }

    private void MoveTowardsLamp(LitLampBehavior lamp)
    {
        if (lamp == null)
        {
            return;
        }

        Vector3 targetPosition = lamp.GetLightWorldPosition();
        if (debugLogs)
        {
            Debug.Log($"[SoulAI] '{name}' moving toward {lamp.name} light position {targetPosition}. Current position {transform.position}");
        }
        Vector3 nextPosition = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        transform.position = nextPosition;

        Vector3 lookDirection = targetPosition - transform.position;
        if (lookDirection.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDirection.normalized), Time.deltaTime * 8f);
        }

        if (Vector3.Distance(transform.position, targetPosition) <= lampExtinguishDistance)
        {
            if (debugLogs)
            {
                Debug.Log($"[SoulAI] '{name}' reached extinguish distance for lamp '{lamp.name}'. Trying to turn it off.");
            }

            if (lamp.TryExtinguishFromSoul())
            {
                if (debugLogs)
                {
                    Debug.Log($"[SoulAI] '{name}' successfully extinguished lamp '{lamp.name}' and will be destroyed.");
                }

                DestroySoul();
            }
            else if (debugLogs)
            {
                Debug.Log($"[SoulAI] '{name}' reached lamp '{lamp.name}' but TryExtinguishFromSoul returned false.");
            }
        }

        if (Time.time >= nextWanderRetargetTime)
        {
            wanderCenter = transform.position;
            wanderTarget = transform.position;
            nextWanderRetargetTime = Time.time + wanderRetargetIntervalSeconds;
        }
    }

    private void WanderAround()
    {
        if (Time.time >= nextWanderRetargetTime || Vector3.Distance(transform.position, wanderTarget) <= 0.3f)
        {
            Vector2 randomOffset = Random.insideUnitCircle * wanderRadius;
            wanderTarget = new Vector3(wanderCenter.x + randomOffset.x, transform.position.y, wanderCenter.z + randomOffset.y);
            nextWanderRetargetTime = Time.time + wanderRetargetIntervalSeconds;
        }

        Vector3 nextPosition = Vector3.MoveTowards(transform.position, wanderTarget, moveSpeed * 0.6f * Time.deltaTime);
        transform.position = nextPosition;

        Vector3 lookDirection = wanderTarget - transform.position;
        if (lookDirection.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDirection.normalized), Time.deltaTime * 6f);
        }
    }

    [PunRPC]
    public void RPC_ApplyDamage(int amount)
    {
        if (!PhotonNetwork.IsMasterClient || isDestroyed || amount <= 0)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - amount);

        if (debugLogs)
        {
            Debug.Log($"[SoulAI] '{name}' took {amount} damage. Remaining health: {currentHealth}/{maxHealth}");
        }

        if (currentHealth <= 0)
        {
            if (debugLogs)
            {
                Debug.Log($"[SoulAI] '{name}' destroyed by priest damage.");
            }

            DestroySoul();
        }
    }

    private void DestroySoul()
    {
        if (isDestroyed)
        {
            return;
        }

        isDestroyed = true;

        if (owningSpawnSystem != null && soulPickup != null)
        {
            owningSpawnSystem.NotifySoulDestroyed(soulPickup);
            return;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            stream.SendNext(currentHealth);
            stream.SendNext(isDroppedSoul);
        }
        else
        {
            networkPosition = (Vector3)stream.ReceiveNext();
            networkRotation = (Quaternion)stream.ReceiveNext();
            currentHealth = Mathf.Max(0, (int)stream.ReceiveNext());
            isDroppedSoul = (bool)stream.ReceiveNext();
        }
    }
}