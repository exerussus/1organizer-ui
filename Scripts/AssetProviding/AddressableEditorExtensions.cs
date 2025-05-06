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
                Debug.LogError("Failed to get GUID of the group pack asset.");
                return;
            }

            var packEntry = settings.FindAssetEntry(packGuid);
            var referenceEntry = settings.FindAssetEntry(reference.AssetGUID);

            if (packEntry == null || referenceEntry == null)
            {
                Debug.LogWarning("Could not find addressable entries for groupPack or reference.");
                return;
            }
            
            var groupOfPack = settings.groups.FirstOrDefault(g => g.entries.Contains(packEntry));
            var groupOfReference = settings.groups.FirstOrDefault(g => g.entries.Contains(referenceEntry));

            if (groupOfPack == null)
            {
                Debug.LogError("Group of the pack asset not found.");
                return;
            }

            if (groupOfReference != groupOfPack)
            {
                settings.MoveEntry(referenceEntry, groupOfPack);
                Debug.Log($"Moved '{reference.AssetGUID}' to group '{groupOfPack.Name}'.");
            }
            else
            {
                Debug.Log("Reference is already in the correct group.");
            }

            AssetDatabase.SaveAssets();
        }
    }
}
#endif