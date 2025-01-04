using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Exerussus._1OrganizerUI.Scripts.AssetProviding;
using UnityEngine.AddressableAssets;

namespace Source.Scripts.Global.Managers.AssetManagement
{
    [Serializable]
    public class AssetReferencePack : IAssetReferencePack
    {
        [ValueDropdown("$IdDropdown")] public string id;
        [ValueDropdown("$TypeDropdown")] public string assetType;
        [ValueDropdown("$TagDropdown")] public AssetReference reference;
        public List<string> tags = new();

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

        public virtual string[] TypeDropdown()
        {
            return new []{ 
                AssetConstants.VfxPack, 
                AssetConstants.Sprite 
            };
        }

        public virtual string[] IdDropdown()
        {
            return new []{""};
        }
        
        public virtual string[] TagDropdown()
        {
            return new []{""};
        }
    }
}