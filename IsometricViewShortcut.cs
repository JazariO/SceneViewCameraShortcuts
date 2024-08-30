using Unity.EditorCoroutines.Editor;
using System.Collections;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class IsometricViewShortcut
{
    private static Vector3 storedPosition;
    private static Quaternion storedRotation;
    private static float storedFOV;
    private static bool isPerspectiveStored = false;

    private static readonly float lerpDuration = 0.5f; // Duration of the camera animation in seconds
    private static readonly float isometricFOV = 0.0125f;  // Field of view for isometric view

    static IsometricViewShortcut()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        Event e = Event.current;

        if (e.type == EventType.KeyDown)
        {
            switch (e.keyCode)
            {
                case KeyCode.Keypad7:
                    SetIsometricView(sceneView, Quaternion.Euler(90, 0, 0), Vector3.zero); // Top view
                    e.Use();
                    break;
                case KeyCode.Keypad1:
                    SetIsometricView(sceneView, Quaternion.Euler(0, 0, 0), Vector3.zero); // Front view
                    e.Use();
                    break;
                case KeyCode.Keypad3:
                    SetIsometricView(sceneView, Quaternion.Euler(0, 90, 0), Vector3.zero); // Side view
                    e.Use();
                    break;
                case KeyCode.Keypad5:
                    TogglePerspective(sceneView);
                    e.Use();
                    break;
                case KeyCode.Keypad9:
                    LookAtOriginFrom6thOctant(sceneView);
                    e.Use();
                    break;
                case KeyCode.Keypad8:
                    ChangeFOV(sceneView, 90f);
                    e.Use();
                    break;
                case KeyCode.Keypad2:
                    ChangeFOV(sceneView, 15f);
                    e.Use();
                    break;
            }
        }
    }

    private static void ChangeFOV(SceneView sceneView, float targetFOV)
    {
        if (!sceneView.orthographic)
        {
            sceneView.StartCoroutine(LerpCamera(sceneView, sceneView.rotation, sceneView.pivot, false, targetFOV));
        }
    }



    private static void SetIsometricView(SceneView sceneView, Quaternion targetRotation, Vector3 targetPivot)
    {
        if (!isPerspectiveStored && !sceneView.orthographic)
        {
            // Store perspective view settings
            storedPosition = sceneView.pivot;
            storedRotation = sceneView.rotation;
            storedFOV = sceneView.camera.fieldOfView;
            isPerspectiveStored = true;
        }

        sceneView.in2DMode = false; // Disable 2D mode if active.
        sceneView.StartCoroutine(LerpCamera(sceneView, targetRotation, targetPivot, true, isometricFOV));
    }

    private static void TogglePerspective(SceneView sceneView)
    {
        if (sceneView.orthographic && isPerspectiveStored)
        {
            // Animate the transition back to the stored perspective view and FOV
            sceneView.StartCoroutine(LerpCamera(sceneView, storedRotation, storedPosition, false, storedFOV));
        } else if (!sceneView.orthographic)
        {
            // Store the current perspective view settings before switching to isometric
            storedPosition = sceneView.pivot;
            storedRotation = sceneView.rotation;
            storedFOV = sceneView.camera.fieldOfView;
            isPerspectiveStored = true;

            // Switch to isometric view with the same pivot and FOV
            sceneView.StartCoroutine(LerpCamera(sceneView, Quaternion.Euler(30, 45, 0), storedPosition, true, isometricFOV));
        }
    }

    private static IEnumerator LerpCamera(SceneView sceneView, Quaternion targetRotation, Vector3 targetPivot, bool toIsometric, float targetFOV)
    {
        float timeElapsed = 0f;
        Vector3 initialPivot = sceneView.pivot;
        Quaternion initialRotation = sceneView.rotation;

        float initialFOV = toIsometric ? sceneView.cameraSettings.fieldOfView : isometricFOV;
        if (!toIsometric)
        {
            sceneView.orthographic = false;
        }    

        while (timeElapsed < lerpDuration)
        {
            float t = timeElapsed / lerpDuration;
            sceneView.rotation = Quaternion.Slerp(initialRotation, targetRotation, t);
            sceneView.pivot = Vector3.Lerp(initialPivot, targetPivot, t);
            sceneView.cameraSettings.fieldOfView = Mathf.Lerp(initialFOV, targetFOV, t);
            timeElapsed += Time.deltaTime;
            sceneView.Repaint();
            yield return null;
        }

        // Ensure the final values are set
        sceneView.rotation = targetRotation;
        sceneView.pivot = targetPivot;
        sceneView.cameraSettings.fieldOfView = targetFOV;
        sceneView.Repaint();

        // Set the orthographic mode after the lerp to avoid mid-animation switch
        sceneView.orthographic = toIsometric;
        if (!toIsometric)
        {
            // Clear stored perspective settings
            isPerspectiveStored = false;
        }
    }


    private static void LookAtOriginFrom6thOctant(SceneView sceneView)
    {
        // Position 10 units away from the origin in the 6th octant
        Vector3 targetPosition = new Vector3(-10, 10, -10);
        Quaternion targetRotation = Quaternion.LookRotation(Vector3.zero - targetPosition); // Look at the origin

        // Directly set the perspective mode if it's currently orthographic
        if (sceneView.orthographic)
        {
            sceneView.orthographic = false;
        }

        sceneView.StartCoroutine(LerpCamera(sceneView, targetRotation, Vector3.zero, false, storedFOV));
    }
}
