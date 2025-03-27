
using System.Collections.Generic;
using UnityEngine;

namespace Exerussus._1OrganizerUI.Scripts.ContentHandlerFeature
{
    public static class AssetContentRecognizer
    {
        private static Dictionary<string, IHandleManager> _handlers = new ();
        private static Dictionary<string, IHandleManager> _packIdToHandleManager = new ();
        
        public static void InitHandler(string assetType, IHandleManager handleManager)
        {
            if (string.IsNullOrEmpty(assetType))
            {
                Debug.LogError("AssetContentRecognizer.InitHandler | AssetType is null or empty.");
                return;
            }
            
            if (!_handlers.TryAdd(assetType, handleManager))
            {
                Debug.LogError($"AssetContentRecognizer.InitHandler | AssetType {assetType} already exists.");
            }
        }

        public static IContentHandle GetHandle(string assetPackId)
        {
            if (!_packIdToHandleManager.TryGetValue(assetPackId, out var manager))
            {
                foreach (var (assetType, handleManager) in _handlers)
                {
                    if (!handleManager.ContainsAssetPackId(assetPackId)) continue;
                    _packIdToHandleManager[assetPackId] = handleManager;
                    manager = handleManager;
                    break;
                }

                if (manager == null)
                {
                    Debug.LogError($"AssetContentRecognizer.GetHandle | AssetPack {assetPackId} not found.");
                    return null;
                }
            }
            
            return manager.CreateHandle(assetPackId);
        }

        public static bool TryGetHandle(string assetPackId, out IContentHandle handle)
        {
            handle = GetHandle(assetPackId);
            return handle != null;
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
                    _packIdToHandleManager = new();
                }
            }
        }
#endif
    }
}