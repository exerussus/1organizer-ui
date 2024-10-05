using System;
using System.Collections.Generic;
using UnityEngine;

namespace Exerussus._1OrganizerUI.Scripts.Pooling
{
    public class AssetPool<T> where T : MonoBehaviour, ILoadAsset
    {
        private readonly Dictionary<string, AddressablePoolObject<T>> _assetPool = new();
        private Dictionary<string, Queue<Job>> _jobs = new();
        
        public async void Initialize(string name, string path)
        {
            var newPool = new AddressablePoolObject<T>();
            await newPool.InitPrefab(name, path, false, 5);
            _assetPool[name] = newPool;
        }

        public void GetAndExecute(string name, string path, Action<T> action)
        {
            if (_jobs.ContainsKey(name))
            {
                _jobs[name].Enqueue(new Job(name, path, Vector3.zero, Quaternion.identity, action));
                return;
            }
            
            if (HasPool(name)) action(_assetPool[name].GetObject(Vector3.zero, Quaternion.identity));
            else
            {
                var job = new Queue<Job>();
                _jobs[name] = job;
                job.Enqueue(new Job(name, path, Vector3.zero, Quaternion.identity, action));
                InitializePool(name, path);
            }
        }
        
        public void GetAndExecute(string name, string path, Vector3 position, Action<T> action)
        {
            if (_jobs.ContainsKey(name))
            {
                _jobs[name].Enqueue(new Job(name, path, position, Quaternion.identity, action));
                return;
            }
            
            if (HasPool(name)) action(_assetPool[name].GetObject(position, Quaternion.identity));
            else
            {
                var job = new Queue<Job>();
                _jobs[name] = job;
                job.Enqueue(new Job(name, path, position, Quaternion.identity, action));
                InitializePool(name, path);
            }
        }

        public void GetAndExecute(string name, string path, Vector3 position, Quaternion quaternion, Action<T> action)
        {
            if (_jobs.ContainsKey(name))
            {
                _jobs[name].Enqueue(new Job(name, path, position, quaternion, action));
                return;
            }
            
            if (HasPool(name)) action(_assetPool[name].GetObject(position, quaternion));
            else
            {
                var job = new Queue<Job>();
                _jobs[name] = job;
                job.Enqueue(new Job(name, path, position, quaternion, action));
                InitializePool(name, path);
            }
        }

        private async void InitializePool(string name, string path)
        {
            var pool = new AddressablePoolObject<T>();
                
            await pool.InitPrefab(name, path, false, 5);
            _assetPool[name] = pool;

            foreach (var job in _jobs[name]) job.Action(_assetPool[name].GetObject(job.Position, job.Quaternion));
            _jobs.Remove(name);
        }
        
        private bool HasPool(string name) => _assetPool.ContainsKey(name);

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

        private struct Job
        {
            public Job(string name, string path, Vector3 position, Quaternion quaternion, Action<T> action)
            {
                Position = position;
                Quaternion = quaternion;
                Action = action;
            }

            public Vector3 Position;
            public Quaternion Quaternion;
            public Action<T> Action;
        }
    }
    
    public interface ILoadAsset
    {
        public string AssetName { get; }
    }
}