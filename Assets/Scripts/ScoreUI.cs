using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ScoreUI : MonoBehaviour
{
    public static ScoreUI Instance;

    public TMP_Text hitText;
    public TMP_Text levelText;


    private void Awake()
    {
        // simple singleton
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void UpdateText(int hit, int total)
    {
        if (hitText != null)
        {
            hitText.text = $"Ghosts hit: {hit} / {total}";
        }
    }
    public void UpdateLevelText(int level)
    {
        if (levelText != null)
        {
            levelText.text = $"Current level: {level}";
        }
    }
}
