using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Exerussus._1Extensions.SmallFeatures;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Exerussus._1OrganizerUI.Scripts.AssetProviding
{
#if UNITY_EDITOR
    [Serializable]
#endif
    public class AddressableContainer
    {
        public AddressableContainer(AssetProvider assetProvider)
        {
            _assetProvider = assetProvider;
        }

#if UNITY_EDITOR
        [SerializeField, Sirenix.OdinInspector.FoldoutGroup("DEBUG")] private List<AddressableLoader> assetLoaders = new();
        [SerializeField, Sirenix.OdinInspector.FoldoutGroup("DEBUG")] private List<GameObjectLoader> gameObjectLoaders = new();
        [SerializeField, Sirenix.OdinInspector.FoldoutGroup("DEBUG")] private List<SpriteLoader> spriteLoaders = new();
#endif

        private readonly AssetProvider _assetProvider;
        private Dictionary<long, AddressableLoader> _loadersDict = new();
        private Dictionary<long, SpriteLoader> _spritesDict = new();
        private Dictionary<long, GameObjectLoader> _gameObjectsDict = new();
        private HashSet<SpriteRenderer> _spriteRenderers = new();
        private HashSet<Image> _imageRenderers = new();
        private Dictionary<long, Sprite> _defaultSpritesForGroups = new();

        private Sprite _defaultSprite;
        
        public AssetProvider AssetProvider => _assetProvider;

        public void SetDefaultSprite(Sprite sprite)
        {
            _defaultSprite = sprite;
        }
        
        #region Universal Loading

        public async UniTask<T> LoadAsset<T>(long id) where T : Object
        {
            if (!_loadersDict.TryGetValue(id, out var pack))
            {
                var assetPack = await _assetProvider.LoadAssetPackAsync<T>(id);
                
                pack = new AddressableLoader
                {
                    id = id,
                    loadedObject = assetPack
#if UNITY_EDITOR
                    , name = id.ToStringFromStableId()
#endif
                };
                
                _loadersDict[id] = pack;
#if UNITY_EDITOR
                assetLoaders.Add(pack);
#endif
            }
            
            return pack.loadedObject as T;
        }

        #endregion

        #region Sprite Loading

        public async UniTask<Sprite> LoadSprite(long id)
        {
            if (!_spritesDict.TryGetValue(id, out var pack))
            {
                var assetPack = await _assetProvider.LoadAssetPackAsync<Sprite>(id);
                
                pack = new SpriteLoader
                {
                    id = id,
                    loadedSprite = assetPack ?? _defaultSprite
#if UNITY_EDITOR
                    , name = id.ToStringFromStableId()
#endif
                };
                
                _spritesDict[id] = pack;
#if UNITY_EDITOR
                spriteLoaders.Add(pack);
#endif
            }
            
            return pack.loadedSprite;
        }
        
        public async UniTask<(bool isLoaded, Sprite sprite)> TryLoadSprite(long id)
        {
            if (!_spritesDict.TryGetValue(id, out var pack))
            {
                var result = await _assetProvider.TryLoadAssetPackContentAsync<Sprite>(id);
                
                if (!result.isLoaded) return result;
                
                pack = new SpriteLoader
                {
                    id = id,
                    loadedSprite = result.asset ?? _defaultSprite
#if UNITY_EDITOR
                    , name = id.ToStringFromStableId()
#endif
                };
                
                _spritesDict[id] = pack;
#if UNITY_EDITOR
                spriteLoaders.Add(pack);
#endif
            }
            
            return (pack.loadedSprite != _defaultSprite, pack.loadedSprite);
        }

        #endregion

        #region GameObject Loading

        public async UniTask<GameObject> LoadGameObject(long id)
        {
            if (!_gameObjectsDict.TryGetValue(id, out var pack))
            {
                var assetPack = await _assetProvider.LoadAssetPackAsync<GameObject>(id);
                
                pack = new GameObjectLoader
                {
                    id = id,
                    loadedObject = assetPack
#if UNITY_EDITOR
                    , name = id.ToStringFromStableId()
#endif
                };
                
                _gameObjectsDict[id] = pack;
#if UNITY_EDITOR
                gameObjectLoaders.Add(pack);
#endif
            }
            
            return pack.loadedObject;
        }

        #endregion

        #region To Renderer Loading 

        public async UniTask<bool> LoadToImage(long id, Image image)
        {
            if (!_spritesDict.TryGetValue(id, out var pack))
            {
                var result = await _assetProvider.TryLoadAssetPackContentAsync<Sprite>(id);
                
                pack = new SpriteLoader
                {
                    id = id,
                    loadedSprite = result.asset ?? _defaultSprite
#if UNITY_EDITOR
                    , name = id.ToStringFromStableId()
#endif
                };
                
                _spritesDict[id] = pack;
#if UNITY_EDITOR
                spriteLoaders.Add(pack);
#endif
            }
            
            _imageRenderers.Add(image);
            image.sprite = pack.loadedSprite;
            return image.sprite != _defaultSprite;
        }
        
        public async UniTask<bool> LoadToSpriteRenderer(long id, SpriteRenderer spriteRenderer)
        {            
            if (!_spritesDict.TryGetValue(id, out var pack))
            {
                var result = await _assetProvider.TryLoadAssetPackContentAsync<Sprite>(id);
                
                pack = new SpriteLoader
                {
                    id = id,
                    loadedSprite = result.asset ?? _defaultSprite
#if UNITY_EDITOR
                    , name = id.ToStringFromStableId()
#endif
                };
                
                _spritesDict[id] = pack;
#if UNITY_EDITOR
                spriteLoaders.Add(pack);
#endif
            }
            
            _spriteRenderers.Add(spriteRenderer);
            spriteRenderer.sprite = pack.loadedSprite;
            return spriteRenderer.sprite != _defaultSprite;
        }

        #endregion
        
        #region Unloading

        public void Unload()
        {
            foreach (var spriteRenderer in _spriteRenderers) if (spriteRenderer != null) spriteRenderer.sprite = null;
            foreach (var imageRenderer in _imageRenderers) if (imageRenderer != null) imageRenderer.sprite = null;
            
            _spriteRenderers.Clear();
            _imageRenderers.Clear();
            
            foreach (var loader in _loadersDict.Values)
            {
                _assetProvider.UnloadAssetPack(loader.id);
            }
            
            foreach (var loader in _spritesDict.Values)
            {
                _assetProvider.UnloadAssetPack(loader.id);
            }
            
            foreach (var loader in _gameObjectsDict.Values)
            {
                _assetProvider.UnloadAssetPack(loader.id);
            }
            
            _loadersDict.Clear();
            _spritesDict.Clear();
            _gameObjectsDict.Clear();

#if UNITY_EDITOR
            assetLoaders.Clear();
            spriteLoaders.Clear();
            gameObjectLoaders.Clear();
#endif

            OnUnload();
        }
        
        protected virtual void OnUnload() { }

        #endregion

        #region Loaders

#if UNITY_EDITOR
        [Serializable]
#endif
        public class AddressableLoader
        {
#if UNITY_EDITOR
            public string name;
#endif
            public long id;
            public Object loadedObject;
        }
        
#if UNITY_EDITOR
        [Serializable]
#endif
        public class GameObjectLoader
        {
#if UNITY_EDITOR
            public string name;
#endif
            public long id;
            public GameObject loadedObject;
        }

#if UNITY_EDITOR
        [Serializable]
#endif
        public class SpriteLoader
        {
#if UNITY_EDITOR
            public string name;
#endif
            public long id;
            public Sprite loadedSprite;
        }

        #endregion
    }

    public static class AddressableContainerExtensions
    {
        public static AddressableContainer CreateContainer(this AssetProvider assetProvider)
        {
            return new AddressableContainer(assetProvider);
        }
    }
}