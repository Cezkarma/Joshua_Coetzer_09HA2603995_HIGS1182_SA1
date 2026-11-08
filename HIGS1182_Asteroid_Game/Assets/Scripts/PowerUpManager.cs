using System;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public enum PowerUpType
{
    Shield
}

public class PowerUpManager : MonoBehaviour
{
    [Tooltip("The pickup the player flies through to gain an Energy Shield. Needs a trigger collider on its root.")]
    [SerializeField] private GameObject shieldPickupPrefab;

    [Header("Energy Shield")]
    [Tooltip("Seconds the shield lasts once collected, unless it absorbs a hit first.")]
    [SerializeField] private float shieldDuration = 5f;
    [Tooltip("The bubble around the player. Purely cosmetic: it is only shown and hidden, and must not have a collider.")]
    [SerializeField] private GameObject shieldVisual;

    [Header("UI")]
    [Tooltip("Shows whether the shield is up and how long it has left.")]
    [SerializeField] private TMP_Text statusText;

    [Header("Placement")]
    [Tooltip("Who to scatter pickups around. Leave empty to use this object's position.")]
    [SerializeField] private Transform player;
    [Tooltip("Pickups are placed between these distances from the player, so none start on top of them.")]
    [SerializeField] private float minDistance = 40f;
    [SerializeField] private float maxDistance = 150f;

    [Header("Feedback")]
    [SerializeField] private Color shieldColour = new Color(0.4f, 0.85f, 1f);

    private static PowerUpManager instance;

    private float shieldTimeLeft;

    private bool IsShieldActive => shieldTimeLeft > 0f;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning("PowerUpManager: more than one in the scene, so this one is ignored.", this);
            enabled = false;
            return;
        }

        instance = this;

        if (shieldPickupPrefab == null)
        {
            Debug.LogWarning("PowerUpManager: assign a shield pickup prefab, otherwise no power-ups will spawn.", this);
        }

        SetShieldVisual(false);
        RefreshStatusText();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void Update()
    {
        if (!IsShieldActive)
        {
            return;
        }

        shieldTimeLeft -= Time.deltaTime;

        if (shieldTimeLeft <= 0f)
        {
            DeactivateShield("Energy shield wore off");
        }
        else
        {
            RefreshStatusText();
        }
    }

    // Returns true if an active shield took the hit, which uses it up.
    public static bool TryAbsorbHit()
    {
        return instance != null && instance.AbsorbHit();
    }

    public void SpawnPowerUps(int count)
    {
        if (shieldPickupPrefab == null)
        {
            return;
        }

        Vector3 centre = player != null ? player.position : transform.position;

        for (int i = 0; i < count; i++)
        {
            Vector3 position = centre + Random.onUnitSphere * Random.Range(minDistance, maxDistance);

            PowerUpPickup pickup = Instantiate(shieldPickupPrefab, position, Random.rotation).AddComponent<PowerUpPickup>();
            pickup.Type = PowerUpType.Shield;
            pickup.Collected += HandlePowerUp;
        }

        Debug.Log($"Spawned {count} power-up(s).");
    }

    // Single entry point for collected power-ups; new types get their own case.
    private void HandlePowerUp(PowerUpType type)
    {
        switch (type)
        {
            case PowerUpType.Shield:
                ActivateShield();
                break;
        }
    }

    private void ActivateShield()
    {
        bool wasActive = IsShieldActive;

        shieldTimeLeft = shieldDuration;
        SetShieldVisual(true);
        RefreshStatusText();

        Debug.Log(wasActive ? "Power-up collected: Energy shield recharged." : $"Power-up collected: Energy shield active for {shieldDuration}s.");
        HUDManager.ShowMessage(wasActive ? "Shield recharged" : "Energy Shield activated!", shieldColour);
    }

    private bool AbsorbHit()
    {
        if (!IsShieldActive)
        {
            return false;
        }

        DeactivateShield("Shield absorbed the hit!");
        return true;
    }

    private void DeactivateShield(string reason)
    {
        shieldTimeLeft = 0f;
        SetShieldVisual(false);
        RefreshStatusText();

        Debug.Log($"Energy shield down: {reason}");
        HUDManager.ShowMessage(reason, shieldColour);
    }

    private void SetShieldVisual(bool visible)
    {
        if (shieldVisual != null)
        {
            shieldVisual.SetActive(visible);
        }
    }

    private void RefreshStatusText()
    {
        if (statusText == null)
        {
            return;
        }

        statusText.text = IsShieldActive ? $"Shield: {shieldTimeLeft:0.0}s" : "Shield: Offline";
    }
}

public class PowerUpPickup : MonoBehaviour
{
    public PowerUpType Type;
    public Action<PowerUpType> Collected;

    public void Collect()
    {
        Collected?.Invoke(Type);
        Destroy(gameObject);
    }
}
