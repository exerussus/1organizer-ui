using System;
using System.Collections.Generic;
using Exerussus._1OrganizerUI.Scripts.AssetProviding;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Source.Scripts.Global.Managers.AssetManagement
{
    [Serializable]
    public class AssetReferencePack : IAssetReferencePack
    {
        public string id;
#if UNITY_EDITOR
        [ValueDropdown("AssetTypeValues")] 
#endif
        public string assetType;
#if UNITY_EDITOR
        [ValueDropdown("TagValues")] 
#endif
        public List<string> tags = new();
        public AssetReference reference;
        public List<ScriptableObject> metaInfo;
        
        private Dictionary<Type, ScriptableObject> _metaInfoDict = new();

#if UNITY_EDITOR
        private static List<string> AssetTypeValues => AssetProviderSettings.GetInstanceEditor().AssetTypes;
        private static List<string> TagValues => AssetProviderSettings.GetInstanceEditor().Tags; 
#endif
        
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
    }
}