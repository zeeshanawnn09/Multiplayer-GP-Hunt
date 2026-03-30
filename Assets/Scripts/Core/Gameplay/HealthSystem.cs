using UnityEngine;
using Photon.Pun;

public class HealthSystem : MonoBehaviourPun
{
    [SerializeField]
    private int startingHealth = 100;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => startingHealth;

    private bool _isDead;
    private bool _hasDisplayedHealth;

    private void Start()
    {
        CurrentHealth = startingHealth;
        UpdateLocalHealthUI();
    }

    private void Update()
    {
        if (!_hasDisplayedHealth)
        {
            UpdateLocalHealthUI();
        }
    }

    public void ApplyDamage(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        // Each client should only modify health for its own player instance.
        if (!photonView.IsMine || _isDead)
        {
            return;
        }

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        UpdateLocalHealthUI();

        if (CurrentHealth == 0)
        {
            _isDead = true;
            Debug.Log($"{photonView.Owner?.NickName} has died.");
        }
    }

    private void UpdateLocalHealthUI()
    {
        // Only the local player should see and update their own health.
        if (!photonView.IsMine)
        {
            return;
        }

        if (TestConnectionText.TestUI != null)
        {
            TestConnectionText.TestUI.GetComponent<TestConnectionText>().DisplayHealth(CurrentHealth, MaxHealth);
            _hasDisplayedHealth = true;
        }
    }
}
