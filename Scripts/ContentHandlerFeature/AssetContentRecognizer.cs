using System;
using System.Collections.Generic;
using Exerussus._1OrganizerUI.Scripts.AssetProviding;
using UnityEngine;

namespace Exerussus._1OrganizerUI.Scripts.ContentHandlerFeature
{
    public static class AssetContentRecognizer
    {
        private static Dictionary<string, Func<IAssetReferencePack, IContentHandler>> _handlers = new ();
        
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

        public static IContentHandler GetInstance(IAssetReferencePack referencePack)
        {
            if (!_handlers.ContainsKey(referencePack.AssetType))
            {
                Debug.LogError($"AssetContentRecognizer.GetInstance | AssetType {referencePack.AssetType} not found.");
                return null;
            }
            
            return _handlers[referencePack.AssetType](referencePack);
        }

        public static bool TryGetInstance(IAssetReferencePack referencePack, out IContentHandler instance)
        {
            if (!_handlers.ContainsKey(referencePack.AssetType))
            {
                instance = null;
                return false;
            }
            
            instance = _handlers[referencePack.AssetType](referencePack);
            return true;
        }
    }
}