using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TimerUI : MonoBehaviour
{
    public static TimerUI Instance;

    public TMP_Text timerText;

    private void Awake()
    {
        // simple singleton
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void UpdateTimerText(float time)
    {
        if (timerText == null) return;

        // Clamp to non-negative and convert to whole seconds
        int totalSeconds = Mathf.Max(0, Mathf.CeilToInt(time));
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        timerText.text = $"{minutes:00}:{seconds:00}";
    }
}
