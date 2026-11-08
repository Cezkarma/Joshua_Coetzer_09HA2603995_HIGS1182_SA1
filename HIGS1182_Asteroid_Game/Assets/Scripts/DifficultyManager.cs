using TMPro;
using UnityEngine;

public class DifficultyManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CollectibleManager collectibleManager;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private AsteroidSpawner asteroidSpawner;
    [Tooltip("Optional. Spawns power-up pickups at the start of each wave.")]
    [SerializeField] private PowerUpManager powerUpManager;
    [Tooltip("Optional. Shows the current wave for the whole run.")]
    [SerializeField] private TMP_Text waveText;

    [Header("Wave 1")]
    [SerializeField] private int startingScrap = 10;
    [SerializeField] private int startingEnemies = 2;
    [Tooltip("Power-up pickups placed at the start of every wave.")]
    [SerializeField] private int powerUpsPerWave = 1;

    [Header("Added Each Wave")]
    [SerializeField] private int extraScrapPerWave = 2;
    [SerializeField] private int extraEnemiesPerWave = 1;
    [Tooltip("Added to the enemy speed multiplier each wave. 0.15 = 15% faster per wave.")]
    [SerializeField] private float enemySpeedIncrease = 0.15f;
    [Tooltip("Asteroid spawn delay is multiplied by this each wave. 0.85 = 15% more asteroids per wave.")]
    [SerializeField] private float asteroidIntervalMultiplier = 0.85f;

    [Header("Limits")]
    [SerializeField] private float maxEnemySpeedMultiplier = 2.5f;
    [SerializeField] private float minAsteroidIntervalScale = 0.3f;

    [Header("Feedback")]
    [SerializeField] private Color warningColour = new Color(1f, 0.8f, 0.2f);

    private int wave = 1;
    private float enemySpeedMultiplier = 1f;
    private float asteroidIntervalScale = 1f;

    private void Start()
    {
        GameManager.StartRun();

        if (collectibleManager == null || enemySpawner == null || asteroidSpawner == null)
        {
            Debug.LogWarning("DifficultyManager: assign the collectible manager, enemy spawner and asteroid spawner, otherwise waves will not work.", this);
            enabled = false;
            return;
        }

        collectibleManager.AllScrapCollected += OnWaveCleared;
        StartWave(startingScrap, startingEnemies);
    }

    private void OnDestroy()
    {
        if (collectibleManager != null)
        {
            collectibleManager.AllScrapCollected -= OnWaveCleared;
        }
    }

    private void OnWaveCleared()
    {
        wave++;
        IncreaseDifficulty();
        StartWave(startingScrap + extraScrapPerWave * (wave - 1), extraEnemiesPerWave);

        HUDManager.ShowMessage("Asteroid Density Increasing", warningColour);
        HUDManager.ShowMessage("Enemy drones are faster", warningColour);
    }

    private void IncreaseDifficulty()
    {
        enemySpeedMultiplier = Mathf.Min(enemySpeedMultiplier + enemySpeedIncrease, maxEnemySpeedMultiplier);
        asteroidIntervalScale = Mathf.Max(asteroidIntervalScale * asteroidIntervalMultiplier, minAsteroidIntervalScale);

        enemySpawner.SetSpeedMultiplier(enemySpeedMultiplier);
        asteroidSpawner.SetIntervalScale(asteroidIntervalScale);

        Debug.Log($"Difficulty increased for wave {wave}: enemy speed x{enemySpeedMultiplier:0.00}, asteroid spawn delay x{asteroidIntervalScale:0.00}.");
    }

    private void StartWave(int scrapCount, int enemyCount)
    {
        GameManager.ReportWave(wave);

        collectibleManager.SpawnScrap(scrapCount);
        enemySpawner.SpawnEnemies(enemyCount);

        if (powerUpManager != null)
        {
            powerUpManager.SpawnPowerUps(powerUpsPerWave);
        }

        if (waveText != null)
        {
            waveText.text = $"Wave {wave}";
        }

        HUDManager.ShowAnnouncement($"Wave {wave}");
        Debug.Log($"Wave {wave} started with {scrapCount} scrap and {enemyCount} new enemy drone(s).");
    }
}
