using System;
using System.Collections.Generic;
using Exerussus._1OrganizerUI.Scripts.AssetProviding;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Source.Scripts.Global.Managers.AssetManagement
{
    [Serializable]
    public class AssetReferencePack : IAssetReferencePack
    {
        public string id;
        public string assetType;
        public List<string> tags = new();
        public AssetReference reference;
        public List<ScriptableObject> metaInfo;
        
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
    }
}