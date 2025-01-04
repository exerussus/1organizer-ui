using System;
using System.Collections.Generic;
using Exerussus._1OrganizerUI.Scripts.AssetProviding;
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
    }
}