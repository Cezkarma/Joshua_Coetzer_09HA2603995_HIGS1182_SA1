using System;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class CollectibleManager : MonoBehaviour
{
    [SerializeField] private GameObject scrapPrefab;

    [Header("UI")]
    [Tooltip("Shows the score and how much scrap is left in this wave.")]
    [SerializeField] private TMP_Text scoreText;
    [Tooltip("Optional. Shows the high score to beat, updating live once it is beaten.")]
    [SerializeField] private TMP_Text highScoreText;

    [Header("Placement")]
    [Tooltip("Who to scatter the scrap around. Leave empty to use this object's position.")]
    [SerializeField] private Transform player;
    [Tooltip("Scrap is placed between these distances from the player, so none of it starts on top of them.")]
    [SerializeField] private float minDistance = 40f;
    [SerializeField] private float maxDistance = 150f;

    [Header("Feedback")]
    [Tooltip("Colour of the pickup burst and the '+1 Scrap' message.")]
    [SerializeField] private Color pickupColour = new Color(0.3f, 1f, 0.5f);
    [SerializeField] private float pickupBurstSize = 3f;

    public event Action AllScrapCollected;

    private int score;
    private int remaining;
    private int savedHighScore;

    private void Awake()
    {
        savedHighScore = SaveManager.LoadHighScore();

        GameManager.ReportScore(score);
        RefreshScoreText();

        if (scrapPrefab == null)
        {
            Debug.LogWarning("CollectibleManager: assign a scrap prefab, otherwise nothing will spawn.", this);
        }
    }

    public void SpawnScrap(int count)
    {
        if (scrapPrefab == null)
        {
            return;
        }

        Vector3 centre = player != null ? player.position : transform.position;

        for (int i = 0; i < count; i++)
        {
            Vector3 position = centre + Random.onUnitSphere * Random.Range(minDistance, maxDistance);

            Instantiate(scrapPrefab, position, Random.rotation)
                .AddComponent<ScrapPickup>()
                .Collected += OnScrapCollected;
        }

        remaining += count;
        RefreshScoreText();
        Debug.Log($"Generated {count} pieces of scrap.");
    }

    private void OnScrapCollected(Vector3 position)
    {
        score++;
        remaining--;
        GameManager.ReportScore(score);
        RefreshScoreText();
        Debug.Log($"Scrap collected. Score {score}, {remaining} piece(s) left this wave.");

        Effects.SpawnBurst(position, pickupBurstSize, pickupColour);
        HUDManager.ShowMessage("+1 Scrap", pickupColour);
        AudioManager.PlaySound(SoundNames.Pickup);

        if (remaining <= 0)
        {
            Debug.Log("Wave cleared: all scrap collected.");
            AllScrapCollected?.Invoke();
        }
    }

    private void RefreshScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Scrap: {score}  ({remaining} left)";
        }

        // Saving still happens once at game over; this only previews the new record.
        if (highScoreText != null)
        {
            highScoreText.text = score > savedHighScore ? $"High Score: {score} (New!)" : $"High Score: {savedHighScore}";
        }
    }
}

public class ScrapPickup : MonoBehaviour
{
    public Action<Vector3> Collected;

    public void Collect()
    {
        Collected?.Invoke(transform.position);
        Destroy(gameObject);
    }
}
