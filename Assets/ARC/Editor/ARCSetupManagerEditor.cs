using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ARC.ARCSetupManager))]
public class ARCSetupManagerEditor : Editor
{
    SerializedProperty minimumCountProperty;
    SerializedProperty scalingFactorProperty;
    SerializedProperty scanningTimeProperty;
    SerializedProperty autocreateSessionProperty;
    SerializedProperty autocreateDelayTimeProperty;
    SerializedProperty planeManagerProperty;
    SerializedProperty raycastManagerProperty;
    SerializedProperty trackedImageManagerProperty;
    SerializedProperty scanningPrefabProperty;
    SerializedProperty markerPendingPrefabProperty;
    SerializedProperty markerDonePrefabProperty;
    SerializedProperty staticScenePrefabProperty;
    SerializedProperty captureEventsProperty;
    SerializedProperty networkEventsProperty;
    SerializedProperty scanEventsProperty;
    SerializedProperty sessionEventsProperty;

    void OnEnable()
    {
        minimumCountProperty = serializedObject.FindProperty("_minimumCount");
        scalingFactorProperty = serializedObject.FindProperty("_scalingFactor");
        scanningTimeProperty = serializedObject.FindProperty("_scanningTime");
        autocreateSessionProperty = serializedObject.FindProperty("_autocreateSession");
        autocreateDelayTimeProperty = serializedObject.FindProperty("_autocreateDelayTime");

        planeManagerProperty = serializedObject.FindProperty("_planeManager");
        raycastManagerProperty = serializedObject.FindProperty("_raycastManager");
        trackedImageManagerProperty = serializedObject.FindProperty("_trackedImageManager");

        scanningPrefabProperty = serializedObject.FindProperty("_scanningPrefab");
        markerPendingPrefabProperty = serializedObject.FindProperty("_markerPendingPrefab");
        markerDonePrefabProperty = serializedObject.FindProperty("_markerDonePrefab");

        staticScenePrefabProperty = serializedObject.FindProperty("_staticScenePrefab");

        captureEventsProperty = serializedObject.FindProperty("captureEvents");
        networkEventsProperty = serializedObject.FindProperty("networkEvents");
        scanEventsProperty = serializedObject.FindProperty("scanEvents");
        sessionEventsProperty = serializedObject.FindProperty("sessionEvents");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Marker Settings", EditorStyles.boldLabel);
        minimumCountProperty.intValue = EditorGUILayout.IntField(new GUIContent("Minimum Count", "Minimum number of marker images needed for setup."), minimumCountProperty.intValue);
        scalingFactorProperty.floatValue = EditorGUILayout.FloatField(new GUIContent("Scaling Factor", "Scaling factor to apply before sending images."), scalingFactorProperty.floatValue);
        scanningTimeProperty.floatValue = EditorGUILayout.FloatField(new GUIContent("Scanning Time", "Time needed to scan a given marker."), scanningTimeProperty.floatValue);
        autocreateSessionProperty.boolValue = EditorGUILayout.Toggle("Autocreate Session", autocreateSessionProperty.boolValue);
        if (autocreateSessionProperty.boolValue) {
            autocreateDelayTimeProperty.floatValue = EditorGUILayout.FloatField(new GUIContent("Delay Time", "Delay before the session is started automatically."), autocreateDelayTimeProperty.floatValue);
        }
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("AR Managers", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(planeManagerProperty);
        EditorGUILayout.PropertyField(raycastManagerProperty);
        EditorGUILayout.PropertyField(trackedImageManagerProperty);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Starting Scene", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(staticScenePrefabProperty);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Gizmos", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(scanningPrefabProperty, new GUIContent("Scanning Prefab", "A tooltip here."));
        EditorGUILayout.PropertyField(markerPendingPrefabProperty, new GUIContent("Marker Scanned Prefab", "A tooltip here."));
        EditorGUILayout.PropertyField(markerDonePrefabProperty, new GUIContent("Marker Finalised Prefab", "A tooltip here."));
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Callback Events", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(captureEventsProperty);
        EditorGUILayout.PropertyField(networkEventsProperty);
        EditorGUILayout.PropertyField(scanEventsProperty);
        EditorGUILayout.PropertyField(sessionEventsProperty);

        serializedObject.ApplyModifiedProperties();
    }
}
