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
        [SerializeField, Sirenix.OdinInspector.FoldoutGroup("DEBUG")] private List<VfxPackLoader> vfxPackLoaders = new();
#endif

        private readonly AssetProvider _assetProvider;
        private HashSet<SpriteRenderer> _spriteRenderers = new();
        private HashSet<Image> _imageRenderers = new();
        private Dictionary<long, AddressableLoader> _loadersDict = new();
        private Dictionary<long, SpriteLoader> _spritesDict = new();
        private Dictionary<long, GameObjectLoader> _gameObjectsDict = new();
        private Dictionary<long, VfxPackLoader> _vfxPacksDict = new();

        private Sprite _defaultSprite;
        private VfxPack _defaultVfxPack;
        
        public AssetProvider AssetProvider => _assetProvider;

        public void SetDefaultSprite(Sprite sprite)
        {
            _defaultSprite = sprite;
        }
        
        public virtual void Fill(IBuildContainer container)
        {
            foreach (var (id, asset) in container.GetAssets())
            {
                if (asset is Sprite sprite && !_spritesDict.ContainsKey(id)) CreateSpriteLoader(id, sprite);
                else if (asset is GameObject gameObject && !_gameObjectsDict.ContainsKey(id)) CreateGameObject(id, gameObject);
                else if (asset is VfxPack vfxPack && !_vfxPacksDict.ContainsKey(id)) CreateVfxPackLoader(id, vfxPack);
            }
        }

        #region Creating Loaders

        protected SpriteLoader CreateSpriteLoader(long id, Sprite sprite, bool isAsyncLoaded = false)
        {
            var spriteLoader = new SpriteLoader
            {
                id = id,
                loadedSprite = sprite,
                isAsyncLoaded = isAsyncLoaded
#if UNITY_EDITOR
                , name = id.ToStringFromStableId()
#endif
            };
                    
            _spritesDict[id] = spriteLoader;
#if UNITY_EDITOR
            spriteLoaders.Add(spriteLoader);
#endif
            return spriteLoader;
        }

        protected GameObjectLoader CreateGameObject(long id, GameObject gameObject, bool isAsyncLoaded = false)
        {
            var loader = new GameObjectLoader
            {
                id = id,
                loadedObject = gameObject,
                isAsyncLoaded = isAsyncLoaded
#if UNITY_EDITOR
                , name = id.ToStringFromStableId()
#endif
            };
                
            _gameObjectsDict[id] = loader;
#if UNITY_EDITOR
            gameObjectLoaders.Add(loader);
#endif
            return loader;
        }

        protected VfxPackLoader CreateVfxPackLoader(long id, VfxPack vfxPack, bool isAsyncLoaded = false)
        {
            var loader = new VfxPackLoader
            {
                id = id,
                loadedVfxPack = vfxPack,
                isAsyncLoaded = isAsyncLoaded
#if UNITY_EDITOR
                , name = id.ToStringFromStableId()
#endif
            };
                
            _vfxPacksDict[id] = loader;
#if UNITY_EDITOR
            vfxPackLoaders.Add(loader);
#endif
            return loader;
        }

        #endregion
        
        #region Universal Loading

        public async UniTask<T> LoadAssetAsync<T>(long id) where T : Object
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

        public async UniTask<Sprite> LoadSpriteAsync(long id)
        {
            if (!_spritesDict.TryGetValue(id, out var pack))
            {
                var assetPack = await _assetProvider.LoadAssetPackAsync<Sprite>(id);
                
                pack = CreateSpriteLoader(id, assetPack ?? _defaultSprite);
            }
            
            return pack.loadedSprite;
        }
        
        public async UniTask<(bool isLoaded, Sprite sprite)> TryLoadSpriteAsync(long id)
        {
            if (!_spritesDict.TryGetValue(id, out var pack))
            {
                var result = await _assetProvider.TryLoadAssetPackContentAsync<Sprite>(id);
                
                if (!result.isLoaded) return result;
                
                pack = CreateSpriteLoader(id, result.asset ?? _defaultSprite);
            }
            
            return (pack.loadedSprite != _defaultSprite, pack.loadedSprite);
        }

        #endregion

        #region GameObject Loading

        public async UniTask<GameObject> LoadGameObjectAsync(long id)
        {
            if (!_gameObjectsDict.TryGetValue(id, out var pack))
            {
                var assetPack = await _assetProvider.LoadAssetPackAsync<GameObject>(id);
                pack = CreateGameObject(id, assetPack);
            }
            
            return pack.loadedObject;
        }

        #endregion

        #region To Renderer Loading

        public async UniTask<bool> LoadToImageAsync(long id, Image image)
        {
            if (!_spritesDict.TryGetValue(id, out var pack))
            {
                var result = await _assetProvider.TryLoadAssetPackContentAsync<Sprite>(id);
                
                pack = CreateSpriteLoader(id, result.asset ?? _defaultSprite);
            }
            
            _imageRenderers.Add(image);
            image.sprite = pack.loadedSprite;
            return image.sprite != _defaultSprite;
        }
        
        public void LoadToImage(long id, Image image)
        {
            if (!_spritesDict.TryGetValue(id, out var pack))
            {
                LoadToImageAsync(id, image).Forget();
                return;
            }
            
            _imageRenderers.Add(image);
            image.sprite = pack.loadedSprite;
        }
        
        public async UniTask<bool> LoadToSpriteRendererAsync(long id, SpriteRenderer spriteRenderer)
        {            
            if (!_spritesDict.TryGetValue(id, out var pack))
            {
                var result = await _assetProvider.TryLoadAssetPackContentAsync<Sprite>(id);
                pack = CreateSpriteLoader(id, result.asset ?? _defaultSprite);
            }
            
            _spriteRenderers.Add(spriteRenderer);
            spriteRenderer.sprite = pack.loadedSprite;
            return spriteRenderer.sprite != _defaultSprite;
        }
        
        public void LoadToSpriteRenderer(long id, SpriteRenderer spriteRenderer)
        {            
            if (!_spritesDict.TryGetValue(id, out var pack))
            {
                LoadToSpriteRendererAsync(id, spriteRenderer).Forget();
                return;
            }
            
            _spriteRenderers.Add(spriteRenderer);
            spriteRenderer.sprite = pack.loadedSprite;
        }

        #endregion
        
        #region VFX Loading

        public async UniTask PreLoadVfxPackAsync(long id, long defaultId)
        {
            if (id == 0) return;
            
            if (TryGetVfxPack(id, out _)) return;
            var (result, pack) = await _assetProvider.TryLoadVfxPackAsync(id);

            if (result && pack.Process is not { Length: > 0 })
            {
                Debug.LogError($"Vfx pack with id : {id.ToStringFromStableId()} ({id}) cannot be loaded, process is empty!");
                result = false;
            }
            
            if (result)
            {
                CreateVfxPackLoader(id, pack, true);
            }
            else
            {
                if (!_vfxPacksDict.ContainsKey(defaultId))
                {
                    Debug.LogError($"Vfx pack with id : {id.ToStringFromStableId()} ({id}) cannot be loaded, default vfx pack with id : {defaultId.ToStringFromStableId()} ({defaultId}) cannot be found");
                    return;
                }
                _vfxPacksDict[id] = _vfxPacksDict[defaultId];
            }
        }
        
        public bool TryGetVfxPack(long id, out VfxPack vfxPack)
        {
            if (_vfxPacksDict.TryGetValue(id, out var pack))
            {
                vfxPack = pack.loadedVfxPack;
                return true;
            }
            
            vfxPack = null;
            return false;
        }

        public async UniTask<VfxPack> LoadVfxPackAsync(long id)
        {       
            if (!_vfxPacksDict.TryGetValue(id, out var pack))
            {
                var (result, vfxPack) = await _assetProvider.TryLoadVfxPackAsync(id);

                pack = CreateVfxPackLoader(id, vfxPack ?? _defaultVfxPack);
            }
            
            return pack.loadedVfxPack;
        }

        public async UniTask<(bool, VfxPack)> TryLoadVfxPackAsync(long id)
        {       
            if (!_vfxPacksDict.TryGetValue(id, out var pack))
            {
                var (result, vfxPack) = await _assetProvider.TryLoadVfxPackAsync(id);
                
                if (!result) return (false, null);
                
                pack = CreateVfxPackLoader(id, vfxPack ?? _defaultVfxPack);
            }
            
            return (true, pack.loadedVfxPack);
        }

        #endregion
        
        #region Unloading

        public void Unload()
        {
            foreach (var spriteRenderer in _spriteRenderers) if (spriteRenderer != null) spriteRenderer.sprite = null;
            foreach (var imageRenderer in _imageRenderers) if (imageRenderer != null) imageRenderer.sprite = null;
            
            _spriteRenderers.Clear();
            _imageRenderers.Clear();
            
            foreach (var loader in _loadersDict.Values) _assetProvider.UnloadAssetPack(loader.id);
            foreach (var loader in _spritesDict.Values) if (loader.isAsyncLoaded) _assetProvider.UnloadAssetPack(loader.id);
            foreach (var loader in _gameObjectsDict.Values) if (loader.isAsyncLoaded) _assetProvider.UnloadAssetPack(loader.id);
            foreach (var loader in _vfxPacksDict.Values) if (loader.isAsyncLoaded) _assetProvider.UnloadAssetPack(loader.id);
            
            _loadersDict.Clear();
            _spritesDict.Clear();
            _gameObjectsDict.Clear();
            _vfxPacksDict.Clear();

#if UNITY_EDITOR
            assetLoaders.Clear();
            spriteLoaders.Clear();
            gameObjectLoaders.Clear();
            vfxPackLoaders.Clear();
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
            public bool isAsyncLoaded;
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
            public bool isAsyncLoaded;
        }

#if UNITY_EDITOR
        [Serializable]
#endif
        public class VfxPackLoader
        {
#if UNITY_EDITOR
            public string name;
#endif
            public long id;
            public VfxPack loadedVfxPack;
            public bool isAsyncLoaded;
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