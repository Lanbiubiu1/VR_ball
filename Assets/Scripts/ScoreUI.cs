using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ScoreUI : MonoBehaviour
{
    public static ScoreUI Instance;

    public TMP_Text hitText;   // drag your TMP object here in Inspector

    private int hitCount = 0;

    private void Awake()
    {
        // simple singleton
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        UpdateText();
    }

    public void AddHit()
    {
        hitCount++;
        UpdateText();
    }

    private void UpdateText()
    {
        if (hitText != null)
        {
            hitText.text = $"Ghosts hit: {hitCount}";
        }
    }
}
