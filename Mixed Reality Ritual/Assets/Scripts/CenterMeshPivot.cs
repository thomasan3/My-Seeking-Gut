#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class CenterMeshPivot
{
    [MenuItem("Tools/Center Mesh Pivot + Normalize Size")]
    static void Center()
    {
        var go = Selection.activeGameObject;
        if (!go) return;

        var mf = go.GetComponent<MeshFilter>();
        if (!mf || !mf.sharedMesh) return;

        Mesh original = mf.sharedMesh;
        Mesh m = Object.Instantiate(original);
        m.name = go.name;

        // ----- STEP 1: CENTER PIVOT -----
        Vector3 center = m.bounds.center;
        Vector3[] verts = m.vertices;

        for (int i = 0; i < verts.Length; i++)
            verts[i] -= center;

        m.vertices = verts;
        m.RecalculateBounds();

        // ----- STEP 2: NORMALIZE SIZE (longest side = 1 meter) -----
        Vector3 size = m.bounds.size;
        float longestSide = Mathf.Max(size.x, Mathf.Max(size.y, size.z));

        if (longestSide > 0f)
        {
            float scale = 1f / longestSide;

            verts = m.vertices;
            for (int i = 0; i < verts.Length; i++)
                verts[i] *= scale;

            m.vertices = verts;
            m.RecalculateBounds();
        }

        // ----- SAVE NEW ASSET -----
        string path = "Assets/Centered/" + m.name + ".asset";
        path = AssetDatabase.GenerateUniqueAssetPath(path);

        AssetDatabase.CreateAsset(m, path);
        AssetDatabase.SaveAssets();

        // assign new mesh
        mf.sharedMesh = m;

        Debug.Log($"Mesh centered and normalized. Saved to: {path}");
    }
}
#endif