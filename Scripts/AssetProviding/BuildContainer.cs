using System;
using System.Collections.Generic;
using Exerussus._1Extensions.SmallFeatures;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Exerussus._1OrganizerUI.Scripts.AssetProviding
{
    [CreateAssetMenu(fileName = "BuildContainer", menuName = "Exerussus/AssetProviding/BuildContainer")]
    public class DefaultBuildContainer : BuildContainer
    {
        [SerializeField, FoldoutGroup("PACKS")] private List<Pack> packs = new();

        internal override void AddNew(string id, Object asset)
        {
            packs.Add(new Pack {id = id, asset = asset});
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
        }

        public override (long id, Object asset)[] GetAssets()
        {
            if (packs == null || packs.Count == 0) return null;
            
            var result = new (long id, Object asset)[packs.Count];
            for (var i = 0; i < packs.Count; i++) result[i] = (packs[i].id.GetStableLongId(), packs[i].asset);
            return result;
        }
        
        [Serializable]
        public class Pack
        {
            public string id;
            public Object asset;
        }
    }
}