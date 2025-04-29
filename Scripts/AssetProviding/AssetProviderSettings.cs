
using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Exerussus._1OrganizerUI.Scripts.AssetProviding
{
    public class AssetProviderSettings : SerializedScriptableObject
    {
#if UNITY_EDITOR
        [SerializeField] private Texture2D assetProviderTexture;
        [SerializeField] private Texture2D groupReferenceTexture;
        [SerializeField] private Texture2D organizerIconName;
        [SerializeField] private Texture2D vfxPackTexture;
#endif
        [SerializeField] private List<string> assetTypes = new List<string>()
        {
            "sprite",
            "vfx",
            "vfx_pack",
            "game_object",
            "sound",
            "ui_panel",
            "ui_panel_pack",
            "hero",
            "npc",
            "item",
        };

        public List<string> AssetTypes => assetTypes;

        public const string SettingsFolder = "Assets/Configs/Exerussus/AssetProvider/"; 
        public const string SettingsAssetName = "AssetProviderSettings";
        
        private static readonly string FullPath = $"{SettingsFolder}{SettingsAssetName}.asset";
        
        private const string AssetProviderIconName = "ex_org_cyber_assets";
        private const string GroupRefIconName = "ex_org_cyber_asset_group";
        private const string OrganizerIconName = "ex_org_organizer";
        private const string VfxPackIconName = "ex_org_vfx_pack";
        
#if UNITY_EDITOR
        public static AssetProviderSettings GetInstanceEditor()
        {
            var (isCreated, fullPath) = _1Extensions.QoL.Editor.AssetsCreator.CreateScriptableObject<AssetProviderSettings>(SettingsFolder, SettingsAssetName);
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<AssetProviderSettings>(fullPath);
            
            if (isCreated)
            {
                if (FindIconTexture(AssetProviderIconName, out var assetProviderTexture)) asset.assetProviderTexture = assetProviderTexture;
                if (FindIconTexture(GroupRefIconName, out var groupRefTexture)) asset.groupReferenceTexture = groupRefTexture;
                if (FindIconTexture(OrganizerIconName, out var organizerTexture)) asset.organizerIconName = organizerTexture;
                if (FindIconTexture(VfxPackIconName, out var vfxTexture)) asset.vfxPackTexture = vfxTexture;
                
                UnityEditor.EditorUtility.SetDirty(asset);
                UnityEditor.AssetDatabase.SaveAssets();
            }
             
            return asset;
        }

        private static bool FindIconTexture(string iconName, out Texture2D texture)
        {
            texture = null;

            var guids = UnityEditor.AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(iconName));

            foreach (var guid in guids)
            {
                Debug.Log(guid);
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);

                if (path.Contains(iconName))
                {
                    texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    if (texture != null) return true;
                }
            }

            return false;
        }
#endif

        public Texture2D AssetProviderTexture
        {
            get
            {
#if UNITY_EDITOR
                return assetProviderTexture;
#else
            return null;
#endif
            }
        }

        public Texture2D GroupReferenceTexture
        {
            get
            {
#if UNITY_EDITOR
                return groupReferenceTexture;
#else
            return null;
#endif
            }
        }
        
        public Texture2D VfxPackTexture
        {
            get
            {
#if UNITY_EDITOR
                return vfxPackTexture;
#else
            return null;
#endif
            }
        }
        
        public Texture2D OrganizerTexture
        {
            get
            {
#if UNITY_EDITOR
                return organizerIconName;
#else
            return null;
#endif
            }
        }
    }
}