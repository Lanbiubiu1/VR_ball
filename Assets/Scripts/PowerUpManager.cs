using Oculus.Interaction.Locomotion;
using System;
using TMPro;
using UnityEngine;
using NaughtyAttributes;

public class PowerUpManager : MonoBehaviour
{
    public static PowerUpManager Instance;

    [Header("Levels")]
    public int currenrSizeLevel = 0;
    public int currentSpeedLevel = 0;
    public int MAX_LEVEL = 3;

    [Header("State")]
    public bool isPowerUpActive = false;

    [Header("UI")]
    public GameObject powerUpRoot;
    public TMP_Text speedlText;
    public TMP_Text sizeText;
    public GameObject speedButtom;
    public GameObject sizeButtom;

    [Header("Ball Reference")]
    [Tooltip("Ball GameObject (script will grab Transform + SphereCollider from this)")]
    public GameObject ballObject;

    [Header("Movement Reference")]
    [Tooltip("FirstPersonLocomotor on PlayerController")]
    public FirstPersonLocomotor locomotor;

    // cached runtime refs
    private Transform ballTransform;
    private SphereCollider ballCollider;

    // cached originals
    private Vector3 _baseBallScale;
    private float _baseBallRadius;
    private float _baseMoveSpeed;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (powerUpRoot != null)
            powerUpRoot.SetActive(false);
    }

    private void Start()
    {
        // ----- BALL SETUP -----
        if (ballObject == null)
        {
            // optional fallback: try by tag
            ballObject = GameObject.FindWithTag("Ball");
        }

        if (ballObject != null)
        {
            ballTransform = ballObject.transform;
            ballCollider = ballObject.GetComponent<SphereCollider>();

            if (ballCollider == null)
            {
                Debug.LogError("PowerUpManager: Ball object has no SphereCollider!");
            }
        }
        else
        {
            Debug.LogError("PowerUpManager: ballObject is not assigned and no object with tag 'Ball' was found.");
        }

        // cache base values
        if (ballTransform != null)
            _baseBallScale = ballTransform.localScale;

        if (ballCollider != null)
            _baseBallRadius = ballCollider.radius;

        if (locomotor != null)
            _baseMoveSpeed = locomotor.SpeedFactor;

        // initialize to level 0
        updateSize();
        updateSpeed();
        UpdateAllTexts();
    }

    // Called by GameManager after clearing a level (except last)
    public void ShowPowerUp()
    {
        // if both upgrades are maxed, skip UI and go directly to next level
        if (currenrSizeLevel >= MAX_LEVEL && currentSpeedLevel >= MAX_LEVEL)
        {
            isPowerUpActive = false;
            if (powerUpRoot != null) powerUpRoot.SetActive(false);
            GameManager.Instance.GoToNextLevel();
            return;
        }

        isPowerUpActive = true;

        if (powerUpRoot != null)
            powerUpRoot.SetActive(true);

        // enable/disable buttons depending on levels
        if (speedButtom != null)
            speedButtom.SetActive(currentSpeedLevel < MAX_LEVEL);

        if (sizeButtom != null)
            sizeButtom.SetActive(currenrSizeLevel < MAX_LEVEL);

        UpdateAllTexts();
    }

    [Button("Select Size Upgrade")]
    public void selectSize()
    {
        selectPowerUp(true);
    }

    [Button("Select Speed Upgrade")]
    public void selectSpeed()
    {
        selectPowerUp(false);
    }

    /// <summary>
    /// Called from UI buttons or VRButtonController.
    /// isSize = true  -> choose size upgrade
    /// isSize = false -> choose speed upgrade
    /// </summary>
    private void selectPowerUp(bool isSize)
    {
        if (isSize)
        {
            if (currenrSizeLevel >= MAX_LEVEL) return;

            currenrSizeLevel++;
            updateSize();

            if (currenrSizeLevel >= MAX_LEVEL && sizeButtom != null)
            {
                sizeButtom.SetActive(false);
            }
        }
        else
        {
            if (currentSpeedLevel >= MAX_LEVEL) return;

            currentSpeedLevel++;
            updateSpeed();

            if (currentSpeedLevel >= MAX_LEVEL && speedButtom != null)
            {
                speedButtom.SetActive(false);
            }
        }

        updateText(isSize);

        isPowerUpActive = false;
        if (powerUpRoot != null)
            powerUpRoot.SetActive(false);

        // continue to next level after picking
        GameManager.Instance.GoToNextLevel();
    }

    // ----- APPLY SIZE UPGRADE -----
    // +10% per level based on original size
    private void updateSize()
    {
        if (ballTransform == null && ballCollider == null) return;

        float factor = 1f + 0.1f * currenrSizeLevel;

        if (ballTransform != null)
        {
            ballTransform.localScale = _baseBallScale * factor;
        }

        if (ballCollider != null)
        {
            ballCollider.radius = _baseBallRadius * factor;
        }
    }

    // ----- APPLY SPEED UPGRADE -----
    // +10% per level based on original move speed
    private void updateSpeed()
    {
        if (locomotor == null) return;

        float factor = 1f + 0.1f * currentSpeedLevel;
        locomotor.SpeedFactor = _baseMoveSpeed * factor;
    }

    private void UpdateAllTexts()
    {
        updateText(true);   // size
        updateText(false);  // speed
    }

    private void updateText(bool isSize)
    {
        int level = isSize ? currenrSizeLevel : currentSpeedLevel;
        TMP_Text target = isSize ? sizeText : speedlText;

        if (target == null) return;

        int percent = level * 10;
        target.text = $"current: +{percent}%({level}/{MAX_LEVEL})";
    }

    /// <summary>
    /// Reset all upgrades back to base values (used by GameManager.Restart).
    /// </summary>
    public void ResetAllPowerUps()
    {
        currenrSizeLevel = 0;
        currentSpeedLevel = 0;
        isPowerUpActive = false;

        if (powerUpRoot != null)
            powerUpRoot.SetActive(false);

        if (speedButtom != null) speedButtom.SetActive(true);
        if (sizeButtom != null) sizeButtom.SetActive(true);

        updateSize();
        updateSpeed();
        UpdateAllTexts();
    }
}
