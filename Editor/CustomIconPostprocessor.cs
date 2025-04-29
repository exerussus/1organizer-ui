#if UNITY_EDITOR

using System.Collections.Generic;
using Exerussus._1OrganizerUI.Scripts.AssetProviding;
using UnityEditor;
using UnityEngine;

namespace Exerussus._1OrganizerUI.Editor
{
    public class CustomIconPostprocessor : AssetPostprocessor
    {
        private static readonly HashSet<string> _importedAssets = new HashSet<string>();
        
        static void OnPostprocessAllAssets(string[] importedAssetsArray, string[] _, string[] __, string[] ___)
        {
            var settings = AssetProviderSettings.GetInstanceEditor();
            var importedAssets = new HashSet<string>(importedAssetsArray);
            
            foreach (var path in importedAssets)
            {
                if (_importedAssets.Contains(path)) continue;
                
                if (TryGetInstance<AssetProvider>(path, out var assetProvider))
                {
                    var icon = settings.AssetProviderTexture;
                        
                    if (icon != null)
                    {
                        EditorGUIUtility.SetIconForObject(assetProvider, icon);
                        EditorUtility.SetDirty(assetProvider);
                        AssetDatabase.SaveAssets();
                    }

                    _importedAssets.Add(path);
                }
                
                if (TryGetInstance<GroupReferencePack>(path, out var groupReferencePack))
                {
                    var icon = settings.GroupReferenceTexture;
                        
                    if (icon != null)
                    {
                        EditorGUIUtility.SetIconForObject(groupReferencePack, icon);
                        EditorUtility.SetDirty(groupReferencePack);
                        AssetDatabase.SaveAssets();
                    }

                    _importedAssets.Add(path);
                }
                
                if (TryGetInstance<VfxPack>(path, out var vfxPack))
                {
                    var icon = settings.VfxPackTexture;
                        
                    if (icon != null)
                    {
                        EditorGUIUtility.SetIconForObject(vfxPack, icon);
                        EditorUtility.SetDirty(vfxPack);
                        AssetDatabase.SaveAssets();
                    }

                    _importedAssets.Add(path);
                }
            }
        }

        private static bool TryGetInstance<T>(string path, out T assetResult) where T : Object
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (asset != null && asset is T)
            {
                assetResult = AssetDatabase.LoadAssetAtPath<T>(path);
                    
                if (assetResult == null) return false;
                return true;
            }

            assetResult = null;
            return false;
        }
    }
}

#endif