using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Exerussus._1OrganizerUI.Scripts.Pooling
{
    public class AssetPool<T> where T : MonoBehaviour, ILoadAsset
    {
        private readonly Dictionary<string, AddressablePoolObject<T>> _assetPool = new();
        
        public async void Initialize(string name, string path)
        {
            var newPool = new AddressablePoolObject<T>();
            await newPool.InitPrefab(name, path, false, 5);
            _assetPool[name] = newPool;
        }

        public void GetAndExecute(string name, string path, Action<T> action) => GetAndExecuteAsync(name, path, action);
        public void GetAndExecute(string name, string path, Vector3 position, Action<T> action) => GetAndExecuteAsync(name, path, position, action);
        public void GetAndExecute(string name, string path, Vector3 position, Quaternion quaternion, Action<T> action) => GetAndExecuteAsync(name, path, position, quaternion, action);
        
        private async void GetAndExecuteAsync(string name, string path, Action<T> action)
        {
            var pool = await TryInitializePool(name, path);
            action(pool.GetObject(Vector3.zero, Quaternion.identity));
        }
        
        private async void GetAndExecuteAsync(string name, string path, Vector3 position, Action<T> action)
        {
            var pool = await TryInitializePool(name, path);
            action(pool.GetObject(position, Quaternion.identity));
        }
        
        private async void GetAndExecuteAsync(string name, string path, Vector3 position, Quaternion quaternion, Action<T> action)
        {
            var pool = await TryInitializePool(name, path);
            action(pool.GetObject(position, quaternion));
        }

        private async Task<AddressablePoolObject<T>> TryInitializePool(string name, string path)
        {
            if (!_assetPool.TryGetValue(name, out var pool))
            {
                pool = new AddressablePoolObject<T>();
                
                await pool.InitPrefab(name, path, false, 5);
                _assetPool[name] = pool;
            }

            return pool;
        }

        public T Get(string name)
        {
            if (!_assetPool.TryGetValue(name, out var pool)) Debug.LogError("Ассет не проинициализирован.");
            return pool.GetObject(Vector3.zero, Quaternion.identity);
        }

        public void Release(T asset)
        {
            if (!_assetPool.TryGetValue(asset.AssetName, out var pool)) Debug.LogError("Ассет не проинициализирован.");
            pool.ReleaseObject(asset);
        }
    }

    public interface ILoadAsset
    {
        public string AssetName { get; }
    }
}