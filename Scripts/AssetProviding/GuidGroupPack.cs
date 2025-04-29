using System;
using System.Collections.Generic;
using Exerussus._1Extensions.Scripts.Extensions;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Exerussus._1OrganizerUI.Scripts.AssetProviding
{
    [Serializable]
    public class ExistingGuidGroupPacks
    {
        [SerializeField] private List<GuidGroupPack> guidGroupPacks = new();
        private Dictionary<GroupReferencePack, GuidGroupPack> _guidGroupPacks = new();

        public void Initialize()
        {
            _guidGroupPacks.Clear();
            for (var index = guidGroupPacks.Count - 1; index >= 0; index--)
            {
                var guidGroupPack = guidGroupPacks[index];
                _guidGroupPacks[guidGroupPack.groupReferencePack] = guidGroupPack;
                if (!IsAssetValid(guidGroupPack)) guidGroupPack.assetReference = new AssetReferenceT<GroupReferencePack>(guidGroupPack.guid);
                if (!IsAssetValid(guidGroupPack))
                {
                    Remove(guidGroupPack.groupReferencePack);
                    Debug.LogError($"Guid {guidGroupPack.guid} is not valid.", guidGroupPack.groupReferencePack);
                }
            }
        }

        private bool IsAssetValid(GuidGroupPack guidGroupPack)
        {
            return guidGroupPack.assetReference.RuntimeKeyIsValid();
        }
        
        public void Add(string guid, string path, GroupReferencePack groupReferencePack)
        {
            _guidGroupPacks[groupReferencePack] = new GuidGroupPack { guid = guid, path = path, groupReferencePack = groupReferencePack, assetReference = new AssetReferenceT<GroupReferencePack>(guid) };
            guidGroupPacks.Add(_guidGroupPacks[groupReferencePack]);
        }
        
        public void Remove(GroupReferencePack groupPack)
        {
            if (!_guidGroupPacks.TryPop(groupPack, out var guidGroupPack)) return;
            guidGroupPacks.Remove(guidGroupPack);
        }

        public bool Contains(GroupReferencePack groupPack)
        {
            return _guidGroupPacks.ContainsKey(groupPack);
        }

        public AssetReferenceT<GroupReferencePack> GetAssetReference(GroupReferencePack groupReferencePack)
        {
            _guidGroupPacks.TryGetValue(groupReferencePack, out var guidGroupPack);
            return guidGroupPack?.assetReference;
        }
    }
    
    [Serializable]
    public class GuidGroupPack
    {
        public string guid;
        public string path;
        public GroupReferencePack groupReferencePack;
        public AssetReferenceT<GroupReferencePack> assetReference;
    }
}