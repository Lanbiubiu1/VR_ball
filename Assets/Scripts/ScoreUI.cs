using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ScoreUI : MonoBehaviour
{
    public static ScoreUI Instance;

    public TMP_Text hitText;   // drag your TMP object here in Inspector


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
}
