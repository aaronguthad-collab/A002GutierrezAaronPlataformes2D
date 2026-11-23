using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class AssetRenamer
{
    private static readonly string[] ExcludedRoots =
    {
        "Assets/TextMesh Pro",
    };

    [MenuItem("Ginebro/1. Rename Assets To PascalCase")]
    public static void RenameAll()
    {
        int renamed = 0;
        var paths = AssetDatabase.GetAllAssetPaths()
            .Where(p => p.StartsWith("Assets/") && !ExcludedRoots.Any(r => p.StartsWith(r)))
            .Where(p => !p.EndsWith(".cs") && !p.EndsWith(".asmdef"))
            .ToList();

        var files = paths.Where(p => !AssetDatabase.IsValidFolder(p)).ToList();
        var folders = paths.Where(AssetDatabase.IsValidFolder).OrderByDescending(p => p.Count(c => c == '/')).ToList();

        var caseOnly = new List<string>();
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (string path in files.Concat(folders))
            {
                if (TryRename(path, caseOnly)) renamed++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        foreach (string path in caseOnly)
        {
            string name = NameOf(path);
            string pascal = ToPascalCase(name);
            string dir = Path.GetDirectoryName(path).Replace('\\', '/');
            string ext = AssetDatabase.IsValidFolder(path) ? "" : Path.GetExtension(path);
            if (string.IsNullOrEmpty(AssetDatabase.RenameAsset(path, pascal + "Tmp")) &&
                string.IsNullOrEmpty(AssetDatabase.RenameAsset(dir + "/" + pascal + "Tmp" + ext, pascal)))
                renamed++;
        }

        foreach (var pair in ExplicitRenames)
        {
            if (AssetDatabase.IsValidFolder(pair.Key) || File.Exists(pair.Key))
                if (string.IsNullOrEmpty(AssetDatabase.RenameAsset(pair.Key, pair.Value))) renamed++;
        }

        foreach (string path in AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/") && NameOf(p).EndsWith("Tmp")).ToList())
        {
            string final = NameOf(path).Substring(0, NameOf(path).Length - 3);
            if (string.IsNullOrEmpty(AssetDatabase.RenameAsset(path, final))) renamed++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[AssetRenamer] {renamed} assets renamed to PascalCase.");
    }

    private static readonly Dictionary<string, string> ExplicitRenames = new()
    {
        { "Assets/Sprites/LegacyFantasyHighForest2", "LegacyFantasyHighForest" },
    };

    private static string NameOf(string path)
    {
        return AssetDatabase.IsValidFolder(path) ? Path.GetFileName(path) : Path.GetFileNameWithoutExtension(path);
    }

    private static bool TryRename(string path, List<string> caseOnly)
    {
        string name = NameOf(path);
        string pascal = ToPascalCase(name);
        if (pascal == name || string.IsNullOrEmpty(pascal)) return false;

        string ext = AssetDatabase.IsValidFolder(path) ? "" : Path.GetExtension(path);
        string dir = Path.GetDirectoryName(path).Replace('\\', '/');
        string target = dir + "/" + pascal + ext;

        if (string.Equals(path, target, System.StringComparison.OrdinalIgnoreCase))
        {
            caseOnly.Add(path);
            return false;
        }

        if (File.Exists(target) || Directory.Exists(target))
        {
            Debug.LogWarning($"[AssetRenamer] Skipped {path}: {target} already exists.");
            return false;
        }

        string error = AssetDatabase.RenameAsset(path, pascal);
        if (!string.IsNullOrEmpty(error))
        {
            Debug.LogWarning($"[AssetRenamer] {path}: {error}");
            return false;
        }
        return true;
    }

    public static string ToPascalCase(string name)
    {
        var sb = new StringBuilder();
        bool upperNext = true;
        foreach (char c in name)
        {
            if (char.IsLetterOrDigit(c))
            {
                sb.Append(upperNext ? char.ToUpperInvariant(c) : c);
                upperNext = false;
            }
            else
            {
                upperNext = true;
            }
        }
        return sb.ToString();
    }

    public static void RenameHierarchy(IEnumerable<GameObject> roots)
    {
        foreach (var root in roots)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                string pascal = ToPascalCase(t.name);
                if (!string.IsNullOrEmpty(pascal)) t.name = pascal;
            }
        }
    }
}
