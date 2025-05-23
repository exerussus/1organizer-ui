
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Games.CardEngine.Source.Features.BoardFeature.Scripts.MonoBehaviours
{
    public class AssetProvider : ScriptableObject
    {
        public AssetReferenceT<GroupPack>[] groups;
        public Dictionary<string, AssetReference> references;
    }
    
    public class GroupPack : ScriptableObject
    {
        public ReferencePack[] packs;
    }

    public class ReferencePack
    {
        public string id;
        public AssetReference reference;
    }
}