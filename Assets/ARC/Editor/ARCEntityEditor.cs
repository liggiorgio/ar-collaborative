using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ARC.ARCEntity))]
public class ARCEntityEditor : Editor
{
    SerializedProperty updateRateProperty;
    SerializedProperty positionThresholdProperty;
    SerializedProperty positionSnapProperty;
    SerializedProperty positionFactorProperty;
    SerializedProperty rotationThresholdProperty;
    SerializedProperty rotationSnapProperty;
    SerializedProperty rotationFactorProperty;
    SerializedProperty minScaleProperty;
    SerializedProperty maxScaleProperty;
    SerializedProperty scaleFactorProperty;
    SerializedProperty sleepThresholdProperty;
    SerializedProperty isInteractableProperty;
    SerializedProperty exclusiveModeProperty;
    SerializedProperty positionModeProperty;
    SerializedProperty positionOffsetProperty;
    SerializedProperty rotationModeProperty;
    SerializedProperty lookForwardProperty;
    SerializedProperty forkliftFactorProperty;
    SerializedProperty snapToGridProperty;
    SerializedProperty gridSizeProperty;
    SerializedProperty grabEventProperty;
    SerializedProperty releaseEventProperty;

    void OnEnable()
    {
        updateRateProperty = serializedObject.FindProperty("_updateRate");

        positionThresholdProperty = serializedObject.FindProperty("_positionThreshold");
        positionSnapProperty = serializedObject.FindProperty("_positionSnap");
        positionFactorProperty = serializedObject.FindProperty("_positionFactor");

        rotationThresholdProperty = serializedObject.FindProperty("_rotationThreshold");
        rotationSnapProperty = serializedObject.FindProperty("_rotationSnap");
        rotationFactorProperty = serializedObject.FindProperty("_rotationFactor");

        minScaleProperty = serializedObject.FindProperty("_minScale");
        maxScaleProperty = serializedObject.FindProperty("_maxScale");
        scaleFactorProperty = serializedObject.FindProperty("_scaleFactor");

        sleepThresholdProperty = serializedObject.FindProperty("_sleepThreshold");

        isInteractableProperty = serializedObject.FindProperty("_isInteractable");
        exclusiveModeProperty = serializedObject.FindProperty("_exclusiveMode");

        positionModeProperty = serializedObject.FindProperty("_positionMode");
        positionOffsetProperty = serializedObject.FindProperty("_positionOffset");

        rotationModeProperty = serializedObject.FindProperty("_rotationMode");
        lookForwardProperty = serializedObject.FindProperty("_lookForward");
        forkliftFactorProperty = serializedObject.FindProperty("_forkliftFactor");

        snapToGridProperty = serializedObject.FindProperty("_snapToGrid");
        gridSizeProperty = serializedObject.FindProperty("_gridSize");
        
        grabEventProperty = serializedObject.FindProperty("_grabEvent");
        releaseEventProperty = serializedObject.FindProperty("_releaseEvent");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Network", EditorStyles.boldLabel);
        EditorGUILayout.IntSlider(updateRateProperty, 1, 20, "Update Rate");
        EditorGUILayout.LabelField("Position:");
        EditorGUI.indentLevel = 1;
        positionThresholdProperty.floatValue = EditorGUILayout.FloatField(new GUIContent("Movement Threshold", "This is my tooltip"), positionThresholdProperty.floatValue);
        positionSnapProperty.floatValue = EditorGUILayout.FloatField("Snap Threshold", positionSnapProperty.floatValue);
        EditorGUILayout.Slider(positionFactorProperty, 0f, 1f, "Smoothing Factor");
        EditorGUI.indentLevel = 0;
        EditorGUILayout.LabelField("Orientation:");
        EditorGUI.indentLevel = 1;
        rotationThresholdProperty.floatValue = EditorGUILayout.FloatField("Rotation Threshold", rotationThresholdProperty.floatValue);
        rotationSnapProperty.floatValue = EditorGUILayout.FloatField("Snap Threshold", rotationSnapProperty.floatValue);
        EditorGUILayout.Slider(rotationFactorProperty, 0f, 1f, "Smoothing Factor");
        EditorGUI.indentLevel = 0;
        EditorGUILayout.LabelField("Scaling:");
        EditorGUI.indentLevel = 1;
        EditorGUILayout.PropertyField(minScaleProperty);
        EditorGUILayout.PropertyField(maxScaleProperty);
        EditorGUILayout.Slider(scaleFactorProperty, 0f, 1f, "Smoothing Factor");
        EditorGUI.indentLevel = 0;
        EditorGUILayout.PropertyField(sleepThresholdProperty);
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Interaction", EditorStyles.boldLabel);
        isInteractableProperty.boolValue = EditorGUILayout.Toggle("Is Interactable", isInteractableProperty.boolValue);

        if (isInteractableProperty.boolValue) {
            exclusiveModeProperty.boolValue = EditorGUILayout.Toggle("Exclusive Mode", exclusiveModeProperty.boolValue);
            EditorGUILayout.PropertyField(positionModeProperty);
            if (positionModeProperty.enumValueIndex == 0) {
                EditorGUILayout.PropertyField(positionOffsetProperty);
            }
            
            EditorGUILayout.PropertyField(rotationModeProperty);
            if (rotationModeProperty.enumValueIndex == 1) {
                EditorGUILayout.PropertyField(lookForwardProperty);
            } else if (rotationModeProperty.enumValueIndex == 2) {
                EditorGUILayout.PropertyField(forkliftFactorProperty);
            }
        }

        snapToGridProperty.boolValue = EditorGUILayout.Toggle("Snap To Grid", snapToGridProperty.boolValue);
        if (snapToGridProperty.boolValue) {
            gridSizeProperty.floatValue = EditorGUILayout.FloatField("Grid Size", gridSizeProperty.floatValue);
        }

        EditorGUILayout.PropertyField(grabEventProperty);
        EditorGUILayout.PropertyField(releaseEventProperty);

        serializedObject.ApplyModifiedProperties();
    }
}
