using System;
using System.Collections.Generic;
using Exerussus._1OrganizerUI.Scripts.AssetProviding;
using UnityEngine;

namespace Exerussus._1OrganizerUI.Scripts.ContentHandlerFeature
{
    public static class AssetContentRecognizer
    {
        private static Dictionary<string, Func<IAssetReferencePack, IContentHandler>> _handlers = new ();
        private static AssetProvider _assetProvider;
        
        public static void InitRecognizer(AssetProvider assetProvider)
        {
            _assetProvider = assetProvider;
        }
        
        public static void InitHandler(string assetType, Func<IAssetReferencePack, IContentHandler> creatingMethod)
        {
            if (string.IsNullOrEmpty(assetType))
            {
                Debug.LogError("AssetContentRecognizer.InitHandler | AssetType is null or empty.");
                return;
            }
            if (!_handlers.TryAdd(assetType, creatingMethod))
            {
                Debug.LogError($"AssetContentRecognizer.InitHandler | AssetType {assetType} already exists.");
            }
        }

        public static IContentHandler GetHandle(string assetPackId)
        {
            if (!_assetProvider.TryGetPack(assetPackId, out var referencePack))
            {
                Debug.LogError($"AssetContentRecognizer.GetHandle | AssetPack {assetPackId} not found.");
                return null;
            }
            
            if (!_handlers.ContainsKey(referencePack.AssetType))
            {
                Debug.LogError($"AssetContentRecognizer.GetHandle | AssetType {referencePack.AssetType} not found.");
                return null;
            }
            
            return _handlers[referencePack.AssetType](referencePack);
        }

        public static bool TryGetHandle(string assetPackId, out IContentHandler instance)
        {
            
            if (!_assetProvider.TryGetPack(assetPackId, out var referencePack))
            {
                instance = null;
                return false;
            }
            
            if (!_handlers.ContainsKey(referencePack.AssetType))
            {
                instance = null;
                return false;
            }
            
            instance = _handlers[referencePack.AssetType](referencePack);
            return true;
        }
        
        

#if UNITY_EDITOR

        [UnityEditor.InitializeOnLoad]
        public static class StaticCleaner
        {
            static StaticCleaner()
            {
                UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            }

            private static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange state)
            {
                if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode || state == UnityEditor.PlayModeStateChange.ExitingEditMode)
                {
                    _handlers = new();
                    _assetProvider = null;
                }
            }
        }
#endif
    }
}