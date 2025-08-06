using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Exerussus._1Extensions.SmallFeatures;
using UnityEngine;
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

        public AssetProvider AssetProvider => _assetProvider;

        #region Universal Loading

        public async Task<T> LoadAsset<T>(long id) where T : Object
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
                assetLoaders.Add(pack);
            }
            
            return pack.loadedObject as T;
        }

        #endregion

        #region Sprite Loading

        public async Task<Sprite> LoadSprite(long id)
        {
            if (!_spritesDict.TryGetValue(id, out var pack))
            {
                var assetPack = await _assetProvider.LoadAssetPackAsync<Sprite>(id);
                
                pack = new SpriteLoader
                {
                    id = id,
                    loadedSprite = assetPack
#if UNITY_EDITOR
                    , name = id.ToStringFromStableId()
#endif
                };
                
                _spritesDict[id] = pack;
                spriteLoaders.Add(pack);
            }
            
            return pack.loadedSprite;
        }

        #endregion

        #region GameObject Loading

        public async Task<GameObject> LoadGameObject(long id)
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
                gameObjectLoaders.Add(pack);
            }
            
            return pack.loadedObject;
        }

        #endregion

        #region Unloading

        public void Unload()
        {
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