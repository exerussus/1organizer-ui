using System.Collections.Generic;
using Sirenix.OdinInspector;
using Source.Scripts.Global.Managers.AssetManagement;
using UnityEngine;

namespace Exerussus._1OrganizerUI.Scripts.AssetProviding
{
    public class GroupReferencePack<T> : SerializedScriptableObject, IGroupReferencePack<T>
    where T : AssetReferencePack
{
        [SerializeField] private List<T> assetPacks;
        public virtual string IconPath => "";

        public List<T> AssetPacks
        {
            get => assetPacks;
            set => assetPacks = value;
        }
        
        public virtual void TypeValidate() { }

#if UNITY_EDITOR
        public bool IsValidEditor { get; set; } = true;
        private bool _needValidateCyberAssets = false;

        public void InvokeValidateCyberAssets()
        {
            _needValidateCyberAssets = false;
            
            var allInstances = UnityEditor.AssetDatabase.FindAssets("t:CyberAssets");

            foreach (var guid in allInstances)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path))
                {
                    Debug.LogWarning($"Asset with GUID {guid} has an invalid path and will be skipped.");
                    continue;
                }
                var instance = UnityEditor.AssetDatabase.LoadAssetAtPath<AssetProvider<GroupReferencePack<T>, T>>(path);
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
            ChangeIcon();
            if (_needValidateCyberAssets) InvokeValidateCyberAssets();
        }
        
        private void ChangeIcon()
        {
            if (m_hasIcon) return;
            if (string.IsNullOrEmpty(IconPath)) return;
            
            var icon = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
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
                var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<GroupReferencePack<T>>(assetPath);
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