#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Exerussus._1OrganizerUI.Scripts.AssetProviding
{
    public static class AddressableEditorExtensions
    {
        public static void SyncReferenceToPackGroup(Object groupPackAsset, AssetReference reference)
        {
            if (groupPackAsset == null || reference == null || string.IsNullOrEmpty(reference.AssetGUID))
            {
                Debug.LogWarning("Invalid input: groupPackAsset or reference is null, or reference has no GUID.");
                return;
            }

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("Addressable settings not found.");
                return;
            }

            string packGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(groupPackAsset));
            
            if (string.IsNullOrEmpty(packGuid))
            {
                Debug.LogError("Failed to get GUID of the group pack asset.", groupPackAsset);
                return;
            }

            var packEntry = settings.FindAssetEntry(packGuid);
            var referenceEntry = settings.FindAssetEntry(reference.AssetGUID);

            if (packEntry == null || referenceEntry == null)
            {
                Debug.LogWarning("Could not find addressable entries for groupPack or reference.", groupPackAsset);
                return;
            }
            
            var groupOfPack = settings.groups.FirstOrDefault(g => g.entries.Contains(packEntry));
            var groupOfReference = settings.groups.FirstOrDefault(g => g.entries.Contains(referenceEntry));

            if (groupOfPack == null)
            {
                Debug.LogError("Group of the pack asset not found.", groupPackAsset);
                return;
            }

            if (groupOfReference != groupOfPack)
            {
                settings.MoveEntry(referenceEntry, groupOfPack);
                Debug.Log($"Moved '{reference.AssetGUID}' to group '{groupOfPack.Name}'.", groupPackAsset);
            }

            AssetDatabase.SaveAssets();
        }
        
        public static void EnsureAssetsAreAddressable(AssetReference assetReference)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("AddressableAssetSettings not found. Please make sure Addressables are properly set up.");
                return;
            }
            
            var guid = assetReference.AssetGUID;
            
            if (string.IsNullOrEmpty(guid))
            {
                var asset = assetReference.editorAsset;
                if (asset == null)
                {
                    Debug.LogWarning("AssetReference has no editor asset assigned.");
                    return;
                }

                var path = AssetDatabase.GetAssetPath(asset);
                guid = AssetDatabase.AssetPathToGUID(path);
            }

            var entry = settings.FindAssetEntry(guid);
            if (entry == null)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var group = settings.DefaultGroup;

                entry = settings.CreateOrMoveEntry(guid, group);
                entry.address = path; // можно заменить на более удобный адрес, если нужно
                Debug.Log($"Added asset to Addressables: {path}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
#endif