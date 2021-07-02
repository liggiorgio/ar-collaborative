using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ARC.ARCUser))]
public class ARCUserEditor : Editor
{
    //SerializedProperty usernameProperty;
    SerializedProperty updateRateProperty;
    SerializedProperty positionThresholdProperty;
    SerializedProperty rotationThresholdProperty;
    SerializedProperty clientEnterLobbyProperty;
    SerializedProperty clientExitLobbyProperty;
    SerializedProperty sessionAnchorCreatedProperty;
    SerializedProperty clientReadyProperty;
    SerializedProperty gameStartedProperty;
    SerializedProperty grabEventProperty;
    SerializedProperty releaseEventProperty;

    void OnEnable()
    {
        //usernameProperty = serializedObject.FindProperty("username");
        updateRateProperty = serializedObject.FindProperty("_updateRate");
        positionThresholdProperty = serializedObject.FindProperty("_positionThreshold");
        rotationThresholdProperty = serializedObject.FindProperty("_rotationThreshold");
        clientEnterLobbyProperty = serializedObject.FindProperty("_clientEnterLobby");
        clientExitLobbyProperty = serializedObject.FindProperty("_clientExitLobby");
        sessionAnchorCreatedProperty = serializedObject.FindProperty("_sessionAnchorCreated");
        clientReadyProperty = serializedObject.FindProperty("_clientReady");
        gameStartedProperty = serializedObject.FindProperty("_gameStarted");
        grabEventProperty = serializedObject.FindProperty("_grabEvent");
        releaseEventProperty = serializedObject.FindProperty("_releaseEvent");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        //EditorGUILayout.PropertyField(usernameProperty);
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Network", EditorStyles.boldLabel);
        EditorGUILayout.IntSlider(updateRateProperty, 1, 20, "Update Rate");
        if (updateRateProperty.intValue > 5)
            EditorGUILayout.HelpBox("The send rate is high, an unreliable network channel will be used to update the pose.", MessageType.Info);
        positionThresholdProperty.floatValue = EditorGUILayout.FloatField(new GUIContent("Movement Threshold", "This is my tooltip"), positionThresholdProperty.floatValue);
        rotationThresholdProperty.floatValue = EditorGUILayout.FloatField("Rotation Threshold", rotationThresholdProperty.floatValue);
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Callback events", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(clientEnterLobbyProperty);
        EditorGUILayout.PropertyField(clientExitLobbyProperty);
        EditorGUILayout.PropertyField(clientReadyProperty);
        EditorGUILayout.PropertyField(gameStartedProperty);
        EditorGUILayout.PropertyField(grabEventProperty);
        EditorGUILayout.PropertyField(releaseEventProperty);

        serializedObject.ApplyModifiedProperties();
    }
}
