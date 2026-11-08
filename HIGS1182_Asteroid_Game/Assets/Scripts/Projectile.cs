using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    [Tooltip("How fast the laser travels, in world units per second.")]
    [SerializeField] private float speed = 250f;
    [Tooltip("Thickness of the sweep that checks for hits each physics step. " +
             "Roughly match the beam's visual width.")]
    [SerializeField] private float hitRadius = 0.3f;

    [Header("Feedback")]
    [SerializeField] private Color hitColour = new Color(1f, 0.3f, 0.2f);
    [SerializeField] private float hitBurstSize = 1.5f;

    private Rigidbody rb;
    private LayerMask targetLayers;
    private int damage;
    private bool hasHit;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.isKinematic = true;
        rb.useGravity = false;
    }

    public void Launch(Vector3 direction, float range, int laserDamage, LayerMask layers)
    {
        transform.rotation = Quaternion.LookRotation(direction);
        damage = laserDamage;
        targetLayers = layers;

        // Despawn once the full range is flown so missed shots don't pile up.
        float lifetime = range / speed;
        Destroy(gameObject, lifetime);
    }

    private void FixedUpdate()
    {
        float step = speed * Time.fixedDeltaTime;

        // Sweep the distance about to be covered, so a fast laser can't skip past a target between steps.
        if (Physics.SphereCast(rb.position, hitRadius, transform.forward, out RaycastHit hit, step, targetLayers, QueryTriggerInteraction.Ignore))
        {
            rb.MovePosition(hit.point);
            HitTarget(hit.collider, hit.point);
            return;
        }

        rb.MovePosition(rb.position + transform.forward * step);
    }

    // Backup for targets that move into the laser rather than being caught by the sweep.
    private void OnTriggerEnter(Collider other)
    {
        if (IsOnTargetLayer(other.gameObject))
        {
            HitTarget(other, transform.position);
        }
    }

    private void HitTarget(Collider targetCollider, Vector3 hitPoint)
    {
        if (hasHit)
        {
            return;
        }

        GameObject target = targetCollider.attachedRigidbody != null ? targetCollider.attachedRigidbody.gameObject : targetCollider.gameObject;

        if (!target.CompareTag(GameTags.Asteroid) && !target.CompareTag(GameTags.Enemy))
        {
            return;
        }

        hasHit = true;

        Health health = target.GetComponent<Health>();
        if (health == null)
        {
            Debug.LogWarning($"Projectile: {target.name} is tagged {target.tag} but has no Health component.", target);
        }
        else
        {
            Debug.Log($"Laser hit {target.tag}: {target.name}.");
            health.ApplyDamage(damage);
            HUDManager.ShowMessage(health.IsDead ? $"{target.tag} destroyed" : $"{target.tag} hit ({health.CurrentHealth}/{health.MaxHealth})", hitColour);
        }

        Effects.SpawnBurst(hitPoint, hitBurstSize, hitColour);
        Destroy(gameObject);
    }

    private bool IsOnTargetLayer(GameObject other)
    {
        return (targetLayers.value & (1 << other.layer)) != 0;
    }
}
