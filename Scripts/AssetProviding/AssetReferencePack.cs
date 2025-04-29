using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

namespace Exerussus._1OrganizerUI.Scripts.AssetProviding
{
    [Serializable]
    public class AssetReferencePack : IAssetReferencePack
    {
        public string id;
        public string assetType;
        public List<string> tags = new();
#if UNITY_EDITOR
        public Object editorAsset;
        [ReadOnly, FoldoutGroup("DEBUG", order:999)] public GuidReferencePack guidReferencePack = new();
#endif
        public List<ScriptableObject> metaInfo;
        [ReadOnly, FoldoutGroup("DEBUG")] public AssetReference reference;
        
        private Dictionary<Type, ScriptableObject> _metaInfoDict = new();
        
        public string Id
        {
            get => id;
            set => id = value;
        }

        public string AssetType
        {
            get => assetType;
            set => assetType = value;
        }

        public AssetReference Reference
        {
            get => reference;
            set => reference = value;
        }

        public List<string> Tags
        {
            get => tags;
            set => tags = value;
        }

        public bool TryGetMetaInfo<T>(out T info) where T : ScriptableObject
        {
            var type = typeof(T);
            
            if (_metaInfoDict.TryGetValue(type, out var value))
            {
                if (value == null)
                {
                    info = null;
                    return false;
                }
                
                info = (T)value;
                return true;
            }

            foreach (var information in metaInfo)
            {
                if (information is T t)
                {
                    _metaInfoDict[type] = t;
                    
                    info = t;
                    return true;
                }
            }

            _metaInfoDict[type] = null;
            
            info = null;
            return false;
        }

        public void Validate(GroupReferencePack parentGroupReferencePack)
        {
#if UNITY_EDITOR
            var hasErrors = false;
            var troubles = "";

            if (string.IsNullOrEmpty(id))
            {
                troubles += $"Troubles in group reference pack {parentGroupReferencePack.name}:\nAssetReferencePack id отсутствует.\n";
                hasErrors = true;
            }
            else
            {
                troubles += $"Troubles with asset reference pack {id}:\n";
            }
            
            if (editorAsset == null)
            {
                troubles += "AssetReferencePack editorAsset отсутствует.\n";
                hasErrors = true;
            }
            
            if (string.IsNullOrEmpty(assetType))
            {
                troubles += "AssetReferencePack assetType отсутствует.\n";
                hasErrors = true;
            }

            if (hasErrors)
            {
                troubles += $"\n\npath: {AssetDatabase.GetAssetPath(parentGroupReferencePack)}\n";
                Debug.LogError(troubles, parentGroupReferencePack);
                return;
            }
            
            var prevPath = guidReferencePack.path;
            guidReferencePack.path = AssetDatabase.GetAssetPath(editorAsset);

            if (prevPath != guidReferencePack.path)
            {
                guidReferencePack.guid = AssetDatabase.AssetPathToGUID(guidReferencePack.path);
                guidReferencePack.assetReference = new AssetReference(guidReferencePack.guid);
                Reference = guidReferencePack.assetReference;
            }
                    
            if (!Reference.RuntimeKeyIsValid() || !Editor.QOL.AddressableQoL.IsAssetAddressableByAsset(editorAsset))
            {
                guidReferencePack.assetReference = new AssetReference(guidReferencePack.guid);
            }
            
            if (!Reference.RuntimeKeyIsValid())
            {
                troubles += $"assetReference не валиден.\n";
                hasErrors = true;
            }
            if (!Editor.QOL.AddressableQoL.IsAssetAddressableByAsset(editorAsset))
            {
                troubles += $"assetReference не является addressable.\n";
                hasErrors = true;
            }

            if (hasErrors)
            {
                troubles += $"\n\npath: {AssetDatabase.GetAssetPath(parentGroupReferencePack)}\n";
                Debug.LogError(troubles, parentGroupReferencePack);
            }
#endif
        }
    }
}