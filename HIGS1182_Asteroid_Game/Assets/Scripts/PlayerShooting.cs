using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [Tooltip("The laser prefab fired from the ship. Needs a Projectile component.")]
    [SerializeField] private Projectile laserPrefab;
    [Tooltip("Where shots start from. Left empty, the child named ShootingPoint is used.")]
    [SerializeField] private Transform shootingPoint;
    [Tooltip("How far a shot reaches, in world units.")]
    [SerializeField] private float range = 400f;
    [Tooltip("How far down the camera's line of sight aiming starts, so the ship's own hull is never read as the target.")]
    [SerializeField] private float shipClearance = 15f;
    [Tooltip("When nothing is under the crosshair, lasers aim at the point this far down the camera's line of sight. " +
             "Lower values make the beam rise to meet the crosshair sooner; higher values keep it lower for longer.")]
    [SerializeField] private float convergenceDistance = 60f;

    [Header("Damage")]
    [Tooltip("Layers a shot can hit. Set this to Asteroid and Enemy.")]
    [SerializeField] private LayerMask targetLayers = ~0;
    [SerializeField] private int damage = 1;

    private Camera cam;
    private AnimationController animationController;

    private void Awake()
    {
        animationController = GetComponent<AnimationController>();

        if (shootingPoint == null)
        {
            shootingPoint = transform.Find("ShootingPoint");
        }
    }

    private void Start()
    {
        cam = Camera.main;

        if (cam == null || shootingPoint == null || laserPrefab == null)
        {
            Debug.LogWarning("PlayerShooting: needs a camera tagged MainCamera, a ShootingPoint and a laser prefab, so shooting is disabled.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && Cursor.lockState == CursorLockMode.Locked)
        {
            HandleShooting();
        }
    }

    private void HandleShooting()
    {
        Vector3 origin = shootingPoint.position;
        Vector3 direction = (GetAimPoint() - origin).normalized;

        Debug.DrawRay(origin, direction * range, Color.red, 0.2f);

        FireLaser(origin, direction);

        if (animationController != null)
        {
            animationController.PlayShoot();
        }
    }

    private void FireLaser(Vector3 origin, Vector3 direction)
    {
        Projectile laser = Instantiate(laserPrefab, origin, Quaternion.LookRotation(direction));
        laser.Launch(direction, range, damage, targetLayers);
        AudioManager.PlaySound(SoundNames.Laser);

        Debug.Log("Laser fired.");
    }

    private Vector3 GetAimPoint()
    {
        Ray aim = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        aim.origin = aim.GetPoint(shipClearance);

        // On a miss, aim at a nearer point so the beam lines up with the crosshair sooner.
        return Physics.Raycast(aim, out RaycastHit hit, range, targetLayers, QueryTriggerInteraction.Ignore) ? hit.point : aim.GetPoint(convergenceDistance);
    }
}
