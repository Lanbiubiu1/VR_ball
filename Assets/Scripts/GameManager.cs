using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Hierarchy")]
    [Tooltip("Parent object that contains Level1, Level2, ... as children")]
    public Transform ghostManagerRoot;

    private int hitCount = 0;
    private int totalGhosts = 0;
    private int currentLevel = 0;   // 1-based index (1 = Level1)
    private int levelsCount = 0;

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

    private void GoToNextLevel()
    {
        if (currentLevel >= levelsCount)
        {
            Debug.Log("All levels cleared!");
            return;
        }

        currentLevel++;         // move to Level1, Level2, ...
        hitCount = 0;

        SetGhostActiveByLevel(); // also sets totalGhosts + updates UI
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
}
