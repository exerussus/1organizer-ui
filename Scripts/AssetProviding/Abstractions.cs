using System.Collections.Generic;
using System.Threading.Tasks;
using Exerussus._1Extensions.Abstractions;
using Source.Scripts.Global.Managers.AssetManagement;
using UnityEngine.AddressableAssets;

namespace Exerussus._1OrganizerUI.Scripts.AssetProviding
{
    public interface IAssetProvider : IInitializable
    {
        public Task InitializeAsync();
        public List<IAssetReferencePack> GetPacksByType(string type);
        public IAssetReferencePack GetPack(string id);
        public bool TryGetPack(string id, out IAssetReferencePack assetReferencePack);
        public Task<T> LoadAssetPackAsync<T>(string packId) where T : UnityEngine.Object;
        public Task<(bool result, VfxPack vfxPack)> TryLoadVfxPackAsync(string packId);
        public Task<(bool result, T asset)> TryLoadAssetPackAsync<T>(string packId) where T : UnityEngine.Object;
        public void UnloadAssetPack(string packId);        
        public void OnBeforeInitialize() {}
        public void OnInitialize() {}
        public bool IsLoaded { get; }
    }
    
    public interface IAssetReferencePack
    {
        public string Id { get; set; }
        public string AssetType { get; set; }
        public AssetReference Reference { get; set; }
        public List<string> Tags { get; set; }
        string[] TypeDropdown();
        string[] IdDropdown();
        string[] TagDropdown();
    }

    public interface IGroupReferencePack
    {
        public List<AssetReferencePack> AssetPacks { get; set; }
    }
}