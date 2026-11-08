using UnityEngine;

public static class SaveManager
{
    private const string HighScoreKey = "HighScore";
    private const string VolumeKeySuffix = "Volume";

    public static float LoadVolume(string channel, float defaultVolume)
    {
        return PlayerPrefs.GetFloat(channel + VolumeKeySuffix, defaultVolume);
    }

    public static void SaveVolume(string channel, float volume)
    {
        PlayerPrefs.SetFloat(channel + VolumeKeySuffix, volume);
        PlayerPrefs.Save();
    }

    public static int LoadHighScore()
    {
        return PlayerPrefs.GetInt(HighScoreKey, 0);
    }

    public static void SaveHighScore(int highScore)
    {
        PlayerPrefs.SetInt(HighScoreKey, highScore);
        PlayerPrefs.Save();
        Debug.Log($"New High Score Saved: {highScore}");
    }

    public static bool TrySubmitScore(int score)
    {
        int highScore = LoadHighScore();

        if (score <= highScore)
        {
            Debug.Log($"Score {score} did not beat the high score of {highScore}.");
            return false;
        }

        SaveHighScore(score);
        return true;
    }
}
