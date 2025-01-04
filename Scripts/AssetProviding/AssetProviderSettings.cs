using Sirenix.OdinInspector;
using UnityEngine;

namespace Exerussus._1OrganizerUI.Scripts.AssetProviding
{
    public class AssetProviderSettings : SerializedScriptableObject
    {
#if UNITY_EDITOR
        [SerializeField] private Texture2D assetProviderTexture;
        [SerializeField] private Texture2D groupReferenceTexture;
        [SerializeField] private Texture2D vfxPackTexture;
#endif

        public Texture2D AssetProviderTexture
        {
            get
            {
#if UNITY_EDITOR
                return assetProviderTexture;
#else
            return null;
#endif
            }
        }

        public Texture2D GroupReferenceTexture
        {
            get
            {
#if UNITY_EDITOR
                return groupReferenceTexture;
#else
            return null;
#endif
            }
        }
        
        public Texture2D VfxPackTexture
        {
            get
            {
#if UNITY_EDITOR
                return vfxPackTexture;
#else
            return null;
#endif
            }
        }
    }
}