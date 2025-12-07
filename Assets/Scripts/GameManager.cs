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

    [Header("Player")]
    [Tooltip("Root transform of the player / XR rig")]
    public Transform playerRoot;

    [Header("Screens")]
    [Tooltip("Win screen panel (under Canvas)")]
    public GameObject winScreen;
    [Tooltip("Lose screen panel (under Canvas)")]
    public GameObject loseScreen;

    private int hitCount = 0;
    private int totalGhosts = 0;
    private int currentLevel = 0;   // 1-based index (1 = Level1)
    private int levelsCount = 0;

    private float currentTimeLeft = 0f;
    private bool timerRunning = false;

    private bool gameOver = false;
    private bool loseRoutineStarted = false;

    private Vector3 initialPlayerPos;
    private Quaternion initialPlayerRot;
    private bool hasInitialPlayerPos = false;

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

        // record initial player position/rotation
        if (playerRoot != null)
        {
            initialPlayerPos = playerRoot.position;
            initialPlayerRot = playerRoot.rotation;
            hasInitialPlayerPos = true;
        }

        // ensure screens start hidden (and their children)
        SetScreenActive(winScreen, false);
        SetScreenActive(loseScreen, false);
    }

    private void Start()
    {
        GoToNextLevel();
    }

    private void Update()
    {
        if (gameOver || !timerRunning) return;

        currentTimeLeft -= Time.deltaTime;
        if (currentTimeLeft <= 0f)
        {
            currentTimeLeft = 0f;
            timerRunning = false;   // timer stops at 0

            if (!loseRoutineStarted)
            {
                loseRoutineStarted = true;
                StartCoroutine(LoseAfterDelay(5f)); // wait 5 seconds, then lose
            }
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
        if (gameOver) return;

        if (currentLevel >= levelsCount)
        {
            // already at or beyond last level – should have won before
            return;
        }

        currentLevel++;         // move to Level1, Level2, ...
        hitCount = 0;

        // reset player position to initial scene pos
        ResetPlayerToInitial();

        // enable ghosts / level UI
        SetGhostActiveByLevel();

        // reset timer for this level
        currentTimeLeft = GetTimeForLevel(currentLevel);
        timerRunning = currentTimeLeft > 0f;

        if (TimerUI.Instance != null)
        {
            TimerUI.Instance.UpdateTimerText(currentTimeLeft);
        }

        // reset lose flag when entering a new level
        loseRoutineStarted = false;
    }

    public void AddHit()
    {
        if (gameOver) return;

        hitCount++;
        ScoreUI.Instance.UpdateText(hitCount, totalGhosts);

        if (hitCount >= totalGhosts)
        {
            // cleared this level
            if (currentLevel >= levelsCount)
            {
                // cleared last level -> win immediately
                WinGame();
            }
            else
            {
                GoToNextLevel();
            }
        }
    }

    private IEnumerator LoseAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (!gameOver)  // if we didn't already win in those 5s
        {
            LoseGame();
        }
    }

    private void WinGame()
    {
        if (gameOver) return;
        gameOver = true;
        timerRunning = false;

        // snap player back to original position/orientation
        ResetPlayerToInitial();

        // show win screen + children
        SetScreenActive(winScreen, true);
    }

    private void LoseGame()
    {
        if (gameOver) return;
        gameOver = true;
        timerRunning = false;

        // snap player back to original position/orientation
        ResetPlayerToInitial();

        // show lose screen + children
        SetScreenActive(loseScreen, true);
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

    // Central helper for resetting player pose
    private void ResetPlayerToInitial()
    {
        if (!hasInitialPlayerPos || playerRoot == null) return;

        playerRoot.position = initialPlayerPos;
        playerRoot.rotation = initialPlayerRot;
    }

    // Enable/disable a screen and all its children safely
    private void SetScreenActive(GameObject screen, bool active)
    {
        if (screen == null) return;

        screen.SetActive(active);

        foreach (Transform child in screen.transform)
        {
            child.gameObject.SetActive(active);
        }
    }

    // Replay button: soft reset (no scene reload)
    public void Restart()
    {
        // reset state
        gameOver = false;
        timerRunning = false;
        loseRoutineStarted = false;

        hitCount = 0;
        currentLevel = 0;
        totalGhosts = 0;

        // disable both screens + their children
        SetScreenActive(winScreen, false);
        SetScreenActive(loseScreen, false);

        // reset ghosts
        SetAllGhostDisable();

        // reset player transform
        ResetPlayerToInitial();

        // restart from level 1
        GoToNextLevel();
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
