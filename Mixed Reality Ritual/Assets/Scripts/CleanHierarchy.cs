#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class HierarchyTools : EditorWindow
{
    [MenuItem("Tools/Hierarchy Utility")]
    public static void ShowWindow() => GetWindow<HierarchyTools>("Hierarchy Utility");

    void OnGUI()
    {
        GUILayout.Label("Direct Hierarchy Operations", EditorStyles.boldLabel);
        
        if (GUILayout.Button("1. Flatten Selection")) Flatten();
        if (GUILayout.Button("2. Cull Objects Without Renderers")) Cull();
    }

    private void Flatten()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null) return;

        // Force Unpack if it's a prefab, otherwise parenting will fail silently
        if (PrefabUtility.IsPartOfAnyPrefab(selected))
        {
            PrefabUtility.UnpackPrefabInstance(selected, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        }

        // Get all children and grandchildren
        Transform[] allDescendants = selected.GetComponentsInChildren<Transform>(true);
        int moveCount = 0;

        // Move them to the selected object
        foreach (Transform t in allDescendants)
        {
            if (t == selected.transform || t.parent == selected.transform) continue;

            t.SetParent(selected.transform, true);
            moveCount++;
        }

        // Force Unity to save and refresh the UI
        EditorUtility.SetDirty(selected);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(selected.scene);
        
        Debug.Log($"Moved {moveCount} objects to {selected.name}.");
    }

    private void Cull()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null) return;

        // Identify objects to kill
        List<GameObject> toDestroy = new List<GameObject>();
        foreach (Transform child in selected.transform)
        {
            if (child.GetComponent<MeshRenderer>() == null)
            {
                toDestroy.Add(child.gameObject);
            }
        }

        int count = toDestroy.Count;
        foreach (GameObject obj in toDestroy)
        {
            // Direct destruction without Undo
            DestroyImmediate(obj);
        }

        Debug.Log($"Culled {count} objects without Renderers.");
    }
}
#endif