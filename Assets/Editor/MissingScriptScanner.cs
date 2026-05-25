using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class MissingScriptScanner
{
    private const string SessionKey = "SweetJar.MissingScriptsAutoFixed";

    static MissingScriptScanner()
    {
        EditorApplication.delayCall += AutoFixOncePerSession;
    }

    [MenuItem("Tools/Fix Missing Scripts")]
    public static void FixMissingScripts()
    {
        int removedCount = 0;

        foreach (string scenePath in GetTargetScenePaths())
        {
            removedCount += FixScene(scenePath);
        }

        foreach (string prefabPath in GetTargetPrefabPaths())
        {
            removedCount += FixPrefab(prefabPath);
        }

        if (removedCount > 0)
        {
            AssetDatabase.SaveAssets();
            Debug.Log($"[MissingScriptScanner] Removed {removedCount} missing script component(s).");
        }
        else
        {
            Debug.Log("[MissingScriptScanner] No missing scripts found.");
        }
    }

    private static void AutoFixOncePerSession()
    {
        if (SessionState.GetBool(SessionKey, false))
            return;

        SessionState.SetBool(SessionKey, true);
        FixMissingScripts();
    }

    private static IEnumerable<string> GetTargetScenePaths()
    {
        const string scenesFolder = "Assets/Scenes";
        if (!AssetDatabase.IsValidFolder(scenesFolder))
            yield break;

        foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { scenesFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.IsNullOrEmpty(path))
                yield return path;
        }
    }

    private static IEnumerable<string> GetTargetPrefabPaths()
    {
        const string prefabsFolder = "Assets/Prefabs";
        if (!AssetDatabase.IsValidFolder(prefabsFolder))
            yield break;

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { prefabsFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.IsNullOrEmpty(path))
                yield return path;
        }
    }

    private static int FixScene(string scenePath)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        int removedCount = 0;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            removedCount += RemoveMissingScriptsRecursive(root);
        }

        if (removedCount > 0)
        {
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[MissingScriptScanner] Cleaned scene {scenePath}.");
        }

        return removedCount;
    }

    private static int FixPrefab(string prefabPath)
    {
        string extension = Path.GetExtension(prefabPath);
        if (!string.Equals(extension, ".prefab", System.StringComparison.OrdinalIgnoreCase))
            return 0;

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        int removedCount = RemoveMissingScriptsRecursive(prefabRoot);

        if (removedCount > 0)
        {
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            Debug.Log($"[MissingScriptScanner] Cleaned prefab {prefabPath}.");
        }

        PrefabUtility.UnloadPrefabContents(prefabRoot);
        return removedCount;
    }

    private static int RemoveMissingScriptsRecursive(GameObject gameObject)
    {
        int removedCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject);
        if (removedCount > 0)
        {
            Debug.LogWarning($"[MissingScriptScanner] Removing {removedCount} missing script(s) from {GetHierarchyPath(gameObject)}.");
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(gameObject);
            EditorUtility.SetDirty(gameObject);
        }

        foreach (Transform child in gameObject.transform)
        {
            removedCount += RemoveMissingScriptsRecursive(child.gameObject);
        }

        return removedCount;
    }

    private static string GetHierarchyPath(GameObject gameObject)
    {
        string path = gameObject.name;

        Transform current = gameObject.transform.parent;
        while (current != null)
        {
            path = $"{current.name}/{path}";
            current = current.parent;
        }

        if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            return $"{PrefabStageUtility.GetCurrentPrefabStage().assetPath}::{path}";

        return path;
    }
}
