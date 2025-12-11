using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class VRButtonController : MonoBehaviour
{
    private InputDevice leftHand;
    private InputDevice rightHand;

    private bool lastPrimaryPressed = false;   // A / X
    private bool lastSecondaryPressed = false; // B / Y

    private void Start()
    {
        GetDevices();
    }

    private void GetDevices()
    {
        var devices = new List<InputDevice>();

        // Left controller
        InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, devices);
        if (devices.Count > 0)
            leftHand = devices[0];

        // Right controller
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, devices);
        if (devices.Count > 0)
            rightHand = devices[0];
    }

    private void Update()
    {
        // Reacquire if lost
        if (!leftHand.isValid || !rightHand.isValid)
        {
            GetDevices();
            return;
        }

        bool primaryPressed =
            GetPrimaryPress(leftHand) ||
            GetPrimaryPress(rightHand);   // A (right) or X (left)

        bool secondaryPressed =
            GetSecondaryPress(leftHand) ||
            GetSecondaryPress(rightHand); // B (right) or Y (left)

        var gm = GameManager.Instance;
        var pm = PowerUpManager.Instance;

        // 1) If game over: buttons control replay / quit
        if (gm != null && gm.IsGameOver)
        {
            if (primaryPressed && !lastPrimaryPressed)
            {
                gm.Restart();
            }

            if (secondaryPressed && !lastSecondaryPressed)
            {
                gm.QuitGame();
            }

            lastPrimaryPressed = primaryPressed;
            lastSecondaryPressed = secondaryPressed;
            return;
        }

        // 2) Else if power-up screen is active: buttons pick power-up
        if (pm != null && pm.isPowerUpActive)
        {
            // A / X → speed (if not max)
            if (primaryPressed && !lastPrimaryPressed)
            {
                if (pm.currentSpeedLevel < pm.MAX_LEVEL)
                {
                    pm.selectSpeed(); // false = speed
                }
            }

            // B / Y → size (if not max)
            if (secondaryPressed && !lastSecondaryPressed)
            {
                if (pm.currenrSizeLevel < pm.MAX_LEVEL)
                {
                    pm.selectSize(); // true = size
                }
            }

            lastPrimaryPressed = primaryPressed;
            lastSecondaryPressed = secondaryPressed;
            return;
        }

        // 3) Otherwise, nothing special; just update last states
        lastPrimaryPressed = primaryPressed;
        lastSecondaryPressed = secondaryPressed;
    }

    private bool GetPrimaryPress(InputDevice device)
    {
        return device.TryGetFeatureValue(CommonUsages.primaryButton, out bool v) && v;
    }

    private bool GetSecondaryPress(InputDevice device)
    {
        return device.TryGetFeatureValue(CommonUsages.secondaryButton, out bool v) && v;
    }
}
