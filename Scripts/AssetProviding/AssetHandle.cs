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

        public long Id { get; private set; }
        public bool IsValid { get; private set; } = true;
        private readonly object _lock = new();
        
        public async Task<(bool result, T asset)> Load(long id)
        {
            if (!IsValid) return (false, null);
            if (id == Id && _asset != null) return (_isLoaded, _asset);
            
            lock (_lock)
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

            try
            {
                if (!IsValid)
                {
                    TryUnload();
                    return (false, null);
                }

                return (_isLoaded, _asset);
            }
            finally
            {
                lock (_lock)
                {
                    if (!_isLoaded) Id = 0;
                    _isLoadingProcessStarted = false;
                }
            }
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