using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Hierarchy")]
    [Tooltip("Parent object that contains Level1, Level2, ... as children")]
    public Transform ghostManagerRoot;

    [Header("Timer")]
    [Tooltip("Time (in seconds) for each level. Index 0 = Level 1, etc.")]
    public List<float> levelTimesSeconds = new List<float>();

    private int hitCount = 0;
    private int totalGhosts = 0;
    private int currentLevel = 0;   // 1-based index (1 = Level1)
    private int levelsCount = 0;

    private float currentTimeLeft = 0f;
    private bool timerRunning = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (ghostManagerRoot == null)
        {
            Debug.LogError("GameManager: ghostManagerRoot is not assigned.");
            return;
        }

        // how many Level groups under GhostManager (Level1, Level2, ...)
        levelsCount = ghostManagerRoot.childCount;

        SetAllGhostDisable();
    }

    private void Start()
    {
        GoToNextLevel();
    }

    private void Update()
    {
        if (!timerRunning) return;

        currentTimeLeft -= Time.deltaTime;
        if (currentTimeLeft <= 0f)
        {
            currentTimeLeft = 0f;
            timerRunning = false;   // no more timer updates
        }

        if (TimerUI.Instance != null)
        {
            TimerUI.Instance.UpdateTimerText(currentTimeLeft);
        }
    }

    private float GetTimeForLevel(int levelIndex1Based)
    {
        int idx = levelIndex1Based - 1;

        if (levelTimesSeconds == null || levelTimesSeconds.Count == 0)
        {
            return 0f; // no timer config
        }

        if (idx >= 0 && idx < levelTimesSeconds.Count)
        {
            return Mathf.Max(0f, levelTimesSeconds[idx]);
        }

        // If there are more levels than times, reuse the last time
        return Mathf.Max(0f, levelTimesSeconds[levelTimesSeconds.Count - 1]);
    }

    private void GoToNextLevel()
    {
        if (currentLevel >= levelsCount)
        {
            Debug.Log("All levels cleared!");
            timerRunning = false;
            return;
        }

        currentLevel++;         // move to Level1, Level2, ...
        hitCount = 0;

        // Set ghosts / level UI
        SetGhostActiveByLevel();

        // Reset timer for this level
        currentTimeLeft = GetTimeForLevel(currentLevel);
        timerRunning = currentTimeLeft > 0f;

        if (TimerUI.Instance != null)
        {
            TimerUI.Instance.UpdateTimerText(currentTimeLeft);
        }
    }

    public void AddHit()
    {
        hitCount++;
        ScoreUI.Instance.UpdateText(hitCount, totalGhosts);

        if (hitCount >= totalGhosts)
        {
            GoToNextLevel();
        }
    }

    /// <summary>
    /// Disable all ghosts in all level groups.
    /// </summary>
    private void SetAllGhostDisable()
    {
        for (int i = 0; i < ghostManagerRoot.childCount; i++)
        {
            Transform level = ghostManagerRoot.GetChild(i);
            for (int j = 0; j < level.childCount; j++)
            {
                level.GetChild(j).gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Enable all ghosts for the current level, compute totalGhosts,
    /// and refresh the score UI.
    /// </summary>
    private void SetGhostActiveByLevel()
    {
        // safety
        if (currentLevel <= 0 || currentLevel > ghostManagerRoot.childCount)
        {
            Debug.LogWarning("GameManager: currentLevel out of range.");
            return;
        }

        // first, disable everything (so previous level ghosts are off)
        SetAllGhostDisable();

        // Level1 is index 0, Level2 is index 1, etc.
        Transform level = ghostManagerRoot.GetChild(currentLevel - 1);

        totalGhosts = level.childCount;

        for (int i = 0; i < level.childCount; i++)
        {
            level.GetChild(i).gameObject.SetActive(true);
        }

        // update UI with new level’s totals
        ScoreUI.Instance.UpdateText(hitCount, totalGhosts);
        ScoreUI.Instance.UpdateLevelText(currentLevel);
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
