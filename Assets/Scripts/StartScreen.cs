using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

public class StartScreen : MonoBehaviour
{
    private InputDevice leftHand;
    private InputDevice rightHand;

    private bool lastState = false;

    void Start()
    {
        GetDevices();
    }

    void GetDevices()
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

    void Update()
    {
        // Reacquire if lost
        if (!leftHand.isValid || !rightHand.isValid)
        {
            GetDevices();
            return;
        }

        // 🔥 Check ANY button on EITHER controller
        bool pressed =
            GetPress(leftHand) ||
            GetPress(rightHand);

        // Trigger once on press-down (not hold)
        if (pressed && !lastState)
        {
            StartGame();
        }

        lastState = pressed;
    }

    bool GetPress(InputDevice device)
    {
        // Check a list of common buttons
        return
            (device.TryGetFeatureValue(CommonUsages.primaryButton, out bool primary) && primary) ||     // A / X
            (device.TryGetFeatureValue(CommonUsages.secondaryButton, out bool secondary) && secondary) || // B / Y
            (device.TryGetFeatureValue(CommonUsages.triggerButton, out bool trigger) && trigger) ||
            (device.TryGetFeatureValue(CommonUsages.gripButton, out bool grip) && grip) ||
            (device.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out bool stickClick) && stickClick);
    }

    public void StartGame()
    {
        SceneManager.LoadScene("Main");
    }
}
