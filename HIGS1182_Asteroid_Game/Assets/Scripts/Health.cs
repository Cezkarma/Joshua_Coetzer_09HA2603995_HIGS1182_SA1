using System.Collections;
using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private int maxHealth = 1;

    [Header("Hit Feedback")]
    [SerializeField] private Color hitFlashColour = Color.white;
    [SerializeField] private float hitFlashDuration = 0.1f;

    [Header("Death Feedback")]
    [SerializeField] private Color deathBurstColour = new Color(1f, 0.6f, 0.2f);
    [SerializeField] private float deathBurstSize = 4f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public bool IsDead => CurrentHealth <= 0;

    private Renderer[] renderers;
    private MaterialPropertyBlock flashBlock;
    private Coroutine flashRoutine;

    private void Awake()
    {
        CurrentHealth = maxHealth;
        renderers = GetComponentsInChildren<Renderer>();

        flashBlock = new MaterialPropertyBlock();
        flashBlock.SetColor(BaseColorId, hitFlashColour);
    }

    public void ApplyDamage(int amount)
    {
        if (IsDead || amount <= 0)
        {
            return;
        }

        CurrentHealth = Mathf.Max(CurrentHealth - amount, 0);
        Debug.Log($"{name} took {amount} damage ({CurrentHealth}/{maxHealth} health left).");

        if (IsDead)
        {
            Die();
        }
        else
        {
            Flash();
        }
    }

    public void Kill()
    {
        ApplyDamage(CurrentHealth);
    }

    private void Die()
    {
        Debug.Log($"{name} destroyed.");

        Effects.SpawnBurst(transform.position, deathBurstSize, deathBurstColour);
        AudioManager.PlaySound(SoundNames.Explosion);
        Destroy(gameObject);
    }

    private void Flash()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        SetFlash(true);
        yield return new WaitForSeconds(hitFlashDuration);
        SetFlash(false);

        flashRoutine = null;
    }

    private void SetFlash(bool on)
    {
        foreach (Renderer meshRenderer in renderers)
        {
            meshRenderer.SetPropertyBlock(on ? flashBlock : null);
        }
    }
}
