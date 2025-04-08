using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Exerussus._1OrganizerUI.Scripts.AssetProviding
{
    public class AssetHandle<T> : IDisposable where T : UnityEngine.Object
    {
        private T _asset;
        private bool _isLoaded;
        private bool _isLoadingProcessStarted;

        public string Id { get; private set; }
        public bool IsValid { get; private set; }

        public async Task<(bool result, T asset)> Load(string id)
        {
            lock (this)
            {
                if (_isLoadingProcessStarted)
                {
                    Debug.LogError($"AssetHandle | Can't start loading process : {id}. Loading process is already started : {Id}.");
                    return (false, null);
                }

                TryUnload();
                
                Id = id;
                _isLoadingProcessStarted = true;
            }

            (_isLoaded, _asset) = await AssetProvider.Instance.TryLoadAssetPackContentAsync<T>(id);

            if (!IsValid)
            {
                TryUnload();
                lock (this) _isLoadingProcessStarted = false;
                return (false, null);
            }

            lock (this) _isLoadingProcessStarted = false;
            return (_isLoaded, _asset);
        }

        public void TryUnload()
        {
            if (_isLoaded && _asset != null)
            {
                if (_asset is GameObject gameObject) UnityEngine.Object.Destroy(gameObject);
                _asset = null;
                _isLoaded = false;
                AssetProvider.Instance.UnloadAssetPack(Id);
            }
        }
        
        public void Dispose()
        {
            IsValid = false;
            TryUnload();
        }
    }
}