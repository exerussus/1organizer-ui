using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Exerussus._1OrganizerUI.Scripts.AssetProviding
{
    [CreateAssetMenu(menuName = "Exerussus/AssetProviding/GroupReferencePack", fileName = "GroupReferencePack")]
    public class DefaultGroupReferencePack : GroupReferencePack
{
        [SerializeField] private List<AssetReferencePack> assetPacks;

        public List<AssetReferencePack> AssetPacks
        {
            get => assetPacks;
            set => assetPacks = value;
        }
        
        public override void SetAssetReferencePacks(List<AssetReferencePack> references)
        {
            foreach (var assetPack in assetPacks) references.Add(assetPack);
        }
        
        public override List<AssetReferencePack> GetAssetReferencePacksEditor()
        {
            return AssetPacks;
        }
        
        public virtual void TypeValidate() { }

#if UNITY_EDITOR

        public bool IsValidEditor { get; set; } = true;
        private bool _needValidateCyberAssets = false;

        public void InvokeValidateCyberAssets()
        {
            _needValidateCyberAssets = false;
            
            var allInstances = UnityEditor.AssetDatabase.FindAssets("t:AssetProvider");

            foreach (var guid in allInstances)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path))
                {
                    Debug.LogWarning($"Asset with GUID {guid} has an invalid path and will be skipped.");
                    continue;
                }
                var instance = UnityEditor.AssetDatabase.LoadAssetAtPath<AssetProvider>(path);
                if (instance == null)
                {
                    Debug.LogWarning($"Asset at path '{path}' could not be loaded and will be skipped.");
                    continue;
                }
                instance.OnValidate();
            }
        }

        private void DuplicationValidate()
        {
            var duplicates = new HashSet<string>();
            var seen = new HashSet<string>();
        
            foreach (var pack in assetPacks) if (!seen.Add(pack.Id)) duplicates.Add(pack.Id);
        
            if (duplicates.Count > 0)
            {
                Debug.LogError($"Duplicate AssetPack IDs detected: {string.Join(", ", duplicates)}");
            }
        }

        [SerializeField, HideInInspector] private bool m_hasIcon;

        private void OnValidate()
        {
            //ChangeIcon();
            
            if (_needValidateCyberAssets) InvokeValidateCyberAssets();
        }
        
        private void ChangeIcon()
        {
            if (m_hasIcon) return;
            
            var resource = AssetDatabase.LoadAssetAtPath<AssetProviderSettings>("Packages/com.exerussus.1organizer-ui/Editor/AssetProviderSettings.asset") ?? 
                           AssetDatabase.LoadAssetAtPath<AssetProviderSettings>("Assets/Plugins/Exerussus/1OrganizerUI/Editor/AssetProviderSettings.asset");
            var icon = resource.GroupReferenceTexture;
            if (icon == null) return;

            var path = UnityEditor.AssetDatabase.GetAssetPath(this);
            var obj = UnityEditor.AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

            if (obj == null) return;
            
            UnityEditor.EditorGUIUtility.SetIconForObject(obj, icon);
            UnityEditor.EditorUtility.SetDirty(obj); 
            m_hasIcon = true;
        }

        [Button]
        public void Validate()
        {
            DuplicationValidate();
            TypeValidate();
        }
#endif

    
#if UNITY_EDITOR
    public class AssetDeletionListener : UnityEditor.AssetModificationProcessor
    {
        public static UnityEditor.AssetDeleteResult OnWillDeleteAsset(string assetPath, UnityEditor.RemoveAssetOptions options)
        {
            if (assetPath.EndsWith(".asset"))
            {
                var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<DefaultGroupReferencePack>(assetPath);
                if (asset != null)
                {
                    asset.IsValidEditor = false;
                    asset.InvokeValidateCyberAssets();
                }
            }

            return UnityEditor.AssetDeleteResult.DidNotDelete;
        }
    }
#endif
}
}