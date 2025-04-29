
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Exerussus._1OrganizerUI.Editor.QOL
{
    public static class AddressableQoL
    {
        // public static T GetObjectByReference<T>(AssetReference assetReference)
        // {
        //     assetReference.editorAsset
        // }
        
        public static bool IsAssetAddressableByAsset(Object asset)
        {
            string assetPath = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(assetPath)) return false;

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return false;

            AddressableAssetEntry entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(assetPath));
            return entry != null;
        }
        
        public static bool IsAssetAddressableByPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return false;

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return false;

            AddressableAssetEntry entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(assetPath));
            return entry != null;
        }
        
        public static bool IsAssetAddressableByGuid(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return false;

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return false;

            AddressableAssetEntry entry = settings.FindAssetEntry(guid);
            return entry != null;
        }
    }
}
#endif