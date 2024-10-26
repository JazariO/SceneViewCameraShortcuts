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

    private static readonly float lerpDuration = 0.25f; // Duration of the camera animation in seconds
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
                    SetIsometricView(sceneView, Quaternion.Euler(90, 0, 0)); // Top view
                    e.Use();
                    break;
                case KeyCode.Keypad1:
                    SetIsometricView(sceneView, Quaternion.Euler(0, 0, 0)); // Front view
                    e.Use();
                    break;
                case KeyCode.Keypad3:
                    SetIsometricView(sceneView, Quaternion.Euler(0, 90, 0)); // Side view
                    e.Use();
                    break;
                case KeyCode.Keypad5:
                case KeyCode.Alpha5: // Support both keypad and alphanumeric 5 for toggling
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
            EditorCoroutineUtility.StartCoroutine(LerpCamera(sceneView, sceneView.rotation, sceneView.pivot, false, targetFOV), sceneView);
        }
    }

    private static void SetIsometricView(SceneView sceneView, Quaternion targetRotation)
    {
        Vector3 targetPivot = GetSelectedObjectPosition(sceneView);

        if (!isPerspectiveStored && !sceneView.orthographic)
        {
            // Store perspective view settings (Position A)
            storedPosition = sceneView.pivot;
            storedRotation = sceneView.rotation;
            storedFOV = sceneView.camera.fieldOfView;
            isPerspectiveStored = true;
        }

        sceneView.in2DMode = false; // Disable 2D mode if active.
        EditorCoroutineUtility.StartCoroutine(LerpCamera(sceneView, targetRotation, targetPivot, true, isometricFOV), sceneView);
    }

    private static void TogglePerspective(SceneView sceneView)
    {
        Vector3 targetPivot = GetSelectedObjectPosition(sceneView);

        if (sceneView.orthographic && isPerspectiveStored)
        {
            // Return to stored perspective view settings (Position A)
            EditorCoroutineUtility.StartCoroutine(LerpCamera(sceneView, storedRotation, storedPosition, false, storedFOV), sceneView);
        } else if (!sceneView.orthographic)
        {
            // Store the current perspective view settings (Position A) before switching to isometric
            storedPosition = sceneView.pivot;
            storedRotation = sceneView.rotation;
            storedFOV = sceneView.camera.fieldOfView;
            isPerspectiveStored = true;

            // Switch to isometric view with the selected object pivot (or scene center if none)
            EditorCoroutineUtility.StartCoroutine(LerpCamera(sceneView, Quaternion.Euler(30, 45, 0), targetPivot, true, isometricFOV), sceneView);
        }
    }

    private static IEnumerator LerpCamera(SceneView sceneView, Quaternion targetRotation, Vector3 targetPivot, bool toIsometric, float targetFOV)
    {
        float timeElapsed = 0f;
        Vector3 initialPivot = sceneView.pivot;
        Quaternion initialRotation = sceneView.rotation;
        float initialFOV = sceneView.cameraSettings.fieldOfView;

        if (toIsometric)
        {
            initialFOV = isometricFOV;
        }

        if (!toIsometric)
        {
            sceneView.orthographic = false;
        }

        float startTime = (float)EditorApplication.timeSinceStartup;

        while (timeElapsed < lerpDuration)
        {
            float currentTime = (float)EditorApplication.timeSinceStartup;
            timeElapsed = currentTime - startTime;
            float t = timeElapsed / lerpDuration;
            sceneView.rotation = Quaternion.Slerp(initialRotation, targetRotation, t);
            sceneView.pivot = Vector3.Lerp(initialPivot, targetPivot, t);
            sceneView.cameraSettings.fieldOfView = Mathf.Lerp(initialFOV, targetFOV, t);
            sceneView.Repaint();
            yield return null;
        }

        sceneView.rotation = targetRotation;
        sceneView.pivot = targetPivot;
        sceneView.cameraSettings.fieldOfView = targetFOV;
        sceneView.Repaint();

        sceneView.orthographic = toIsometric;
        if (!toIsometric)
        {
            isPerspectiveStored = false;
        }
    }

    private static Vector3 GetSelectedObjectPosition(SceneView sceneView)
    {
        if (Selection.activeTransform != null)
        {
            return Selection.activeTransform.position;
        }
        return sceneView.pivot; // Default to current pivot if nothing is selected
    }

    private static void LookAtOriginFrom6thOctant(SceneView sceneView)
    {
        Vector3 targetPosition = new Vector3(-10, 10, -10);
        Quaternion targetRotation = Quaternion.LookRotation(Vector3.zero - targetPosition);

        if (sceneView.orthographic)
        {
            sceneView.orthographic = false;
        }

        EditorCoroutineUtility.StartCoroutine(LerpCamera(sceneView, targetRotation, Vector3.zero, false, storedFOV), sceneView);
    }
}
