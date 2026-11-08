using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Thrust")]
    [SerializeField] private float thrustAcceleration = 25f;
    [SerializeField] private float maxSpeed = 30f;
    [Tooltip("How quickly the ship bleeds off speed when you stop thrusting. 0 = drifts forever.")]
    [SerializeField] private float linearDamping = 0.8f;

    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = 3f;
    [Tooltip("How far the nose can pitch up or down, in degrees.")]
    [SerializeField] private float maxPitchAngle = 85f;
    [SerializeField] private bool invertY = false;

    [Header("Banking")]
    [Tooltip("Roll applied while strafing. Purely cosmetic,it does not steer the ship.")]
    [SerializeField] private float bankAngle = 25f;
    [SerializeField] private float bankSmoothing = 6f;

    [Header("Model")]
    [Tooltip("Correction for models whose nose does not point along local +Z. " +
             "The capsule placeholder lies along its local +Y, so it needs -90 on X. " +
             "Set this to zero once the ship mesh faces forward on its own.")]
    [SerializeField] private Vector3 modelRotationOffset = new Vector3(-90f, 0f, 0f);

    [Header("Camera")]
    [Tooltip("Adds a ThirdPersonCamera to the main camera at runtime if it does not already have one.")]
    [SerializeField] private bool autoSetUpCamera = true;

    [Header("Death")]
    [Tooltip("Seconds to wait for the death animation before ending the run anyway, in case its Animation Event never fires.")]
    [SerializeField] private float deathFallbackDelay = 3f;

    private Rigidbody rb;
    private AnimationController animationController;
    private PlayerShooting shooting;
    private bool isDead;
    private string deathCause;
    private float yaw;
    private float pitch;
    private float roll;
    private Vector3 thrustInput;

    public Quaternion AimRotation => Quaternion.Euler(pitch, yaw, 0f);
    public Vector3 AimForward => AimRotation * Vector3.forward;
    public float SpeedFraction => maxSpeed > 0f ? rb.linearVelocity.magnitude / maxSpeed : 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animationController = GetComponent<AnimationController>();
        shooting = GetComponent<PlayerShooting>();

        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.linearDamping = linearDamping;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        Vector3 startAngles = (transform.rotation * Quaternion.Inverse(Quaternion.Euler(modelRotationOffset))).eulerAngles;
        yaw = startAngles.y;
        pitch = Mathf.Clamp(Mathf.DeltaAngle(0f, startAngles.x), -maxPitchAngle, maxPitchAngle);
    }

    private void Start()
    {
        LockCursor();

        if (autoSetUpCamera)
        {
            SetUpCamera();
        }

        if (animationController != null)
        {
            animationController.DeathAnimationFinished += EndRunAfterDeath;
        }
    }

    private void OnDestroy()
    {
        if (animationController != null)
        {
            animationController.DeathAnimationFinished -= EndRunAfterDeath;
        }
    }

    private void Update()
    {
        if (isDead)
        {
            return;
        }

        HandleCursorLock();
        HandleLook();
        ReadThrustInput();
        UpdateBank();
        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        if (isDead)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        MovePlayer();
    }

    private void UpdateAnimation()
    {
        if (animationController != null)
        {
            animationController.SetThrusting(thrustInput.sqrMagnitude > 0f);
        }
    }

    private void MovePlayer()
    {
        rb.MoveRotation(Quaternion.Euler(pitch, yaw, roll) * Quaternion.Euler(modelRotationOffset));

        if (thrustInput.sqrMagnitude > 0f)
        {
            rb.AddForce(AimRotation * thrustInput * thrustAcceleration, ForceMode.Acceleration);
        }

        rb.linearVelocity = Vector3.ClampMagnitude(rb.linearVelocity, maxSpeed);
    }

    private void HandleLook()
    {
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }

        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;

        float pitchDelta = Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch += invertY ? pitchDelta : -pitchDelta;
        pitch = Mathf.Clamp(pitch, -maxPitchAngle, maxPitchAngle);
    }

    private void ReadThrustInput()
    {
        thrustInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));

        if (thrustInput.sqrMagnitude > 1f)
        {
            thrustInput.Normalize();
        }
    }

    private void UpdateBank()
    {
        float targetRoll = -thrustInput.x * bankAngle;
        roll = Mathf.Lerp(roll, targetRoll, 1f - Mathf.Exp(-bankSmoothing * Time.deltaTime));
    }

    private void HandleCursorLock()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (Cursor.lockState == CursorLockMode.None && Input.GetMouseButtonDown(0))
        {
            LockCursor();
        }
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void SetUpCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("PlayerController: no camera tagged MainCamera, so the chase camera was not set up.", this);
            return;
        }

        ThirdPersonCamera chaseCamera = mainCamera.GetComponent<ThirdPersonCamera>();
        if (chaseCamera == null)
        {
            chaseCamera = mainCamera.gameObject.AddComponent<ThirdPersonCamera>();
        }

        chaseCamera.SetTarget(this);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isDead)
        {
            return;
        }

        GameObject other = collision.gameObject;

        if (other.CompareTag(GameTags.Asteroid))
        {
            HandleObstacleHit(other, "Hit by an asteroid");
        }
        else if (other.CompareTag(GameTags.Enemy))
        {
            HandleObstacleHit(other, "Destroyed by an enemy drone");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(GameTags.Scrap) && other.TryGetComponent(out ScrapPickup scrap))
        {
            scrap.Collect();
        }
        else if (other.TryGetComponent(out PowerUpPickup powerUp))
        {
            powerUp.Collect();
        }
    }

    private void HandleObstacleHit(GameObject obstacle, string cause)
    {
        if (PowerUpManager.TryAbsorbHit())
        {
            // Destroy whatever hit the shield so it can't immediately hit the unprotected ship.
            Debug.Log($"Shield saved the player: {cause}.");

            if (obstacle.TryGetComponent(out Health obstacleHealth))
            {
                obstacleHealth.Kill();
            }

            return;
        }

        Die(cause);
    }

    private void Die(string cause)
    {
        isDead = true;
        deathCause = cause;
        thrustInput = Vector3.zero;
        Debug.Log($"Game lost: {cause}.");
        AudioManager.PlaySound(SoundNames.GameOver);

        if (shooting != null)
        {
            shooting.enabled = false;
        }

        if (animationController == null)
        {
            EndRunAfterDeath();
            return;
        }

        // The run normally ends when the death clip's Animation Event fires; this is the safety net.
        animationController.PlayDeath();
        Invoke(nameof(EndRunAfterDeath), deathFallbackDelay);
    }

    private void EndRunAfterDeath()
    {
        GameManager.EndRun($"You lost! {deathCause}.");
    }

    private void OnValidate()
    {
        if (rb != null)
        {
            rb.linearDamping = linearDamping;
        }
    }
}
