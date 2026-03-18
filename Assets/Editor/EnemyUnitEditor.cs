using UnityEditor;

[CustomEditor(typeof(EnemyUnit), true)]
public class EnemyUnitEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "m_Script", "displayName", "baseStats");
        serializedObject.ApplyModifiedProperties();
    }
}
