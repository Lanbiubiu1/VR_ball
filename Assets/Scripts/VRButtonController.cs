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
        // Need a valid GameManager and game must be over (win OR lose)
        if (GameManager.Instance == null || !GameManager.Instance.IsGameOver)
        {
            lastPrimaryPressed = false;
            lastSecondaryPressed = false;
            return;
        }

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

        // A / X pressed this frame → Replay
        if (primaryPressed && !lastPrimaryPressed)
        {
            GameManager.Instance.Restart();
        }

        // B / Y pressed this frame → Quit
        if (secondaryPressed && !lastSecondaryPressed)
        {
            GameManager.Instance.QuitGame();
        }

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
