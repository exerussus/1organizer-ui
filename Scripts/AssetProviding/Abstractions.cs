using System.Collections.Generic;
using System.Threading.Tasks;
using Exerussus._1Extensions.Abstractions;
using Source.Scripts.Global.Managers.AssetManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Exerussus._1OrganizerUI.Scripts.AssetProviding
{
    /// <summary> Главный менеджер ассетов, управляющий загрузкой\выгрузкой через Addressable. </summary>
    public interface IAssetProvider : IInitializable
    {
        public List<IAssetReferencePack> GetPacksByType(string type);
        //public Task<(bool result, GameObject panelUi)> TryLoadUiPanelAsync(string packId);
        //public void UnloadUiPanel(string packId);
        //public List<PanelUiPack> GetAllPanelUiPacks();
        public IAssetReferencePack GetPack(string id);
        public bool TryGetPack(string id, out IAssetReferencePack assetReferencePack);
        public Task<T> LoadAssetPackAsync<T>(string packId) where T : UnityEngine.Object;
        public Task<(bool result, VfxPack vfxPack)> TryLoadVfxPackAsync(string packId);
        public Task<(bool result, T asset)> TryLoadAssetPackContentAsync<T>(string packId) where T : UnityEngine.Object;
        public void UnloadAssetPack(string packId);        
        public void OnBeforeInitialize() {}
        public void OnInitialize() {}
        public bool IsLoaded { get; }
    }
    
    /// <summary> Пакет с мета-информацией об ассете и ссылкой на него. </summary>
    public interface IAssetReferencePack
    {
        /// <summary> Уникальный ID ассета. Может быть отличным от id в Reference. </summary>
        public string Id { get; set; }
        /// <summary> Тип ассета для группирования. </summary>
        public string AssetType { get; set; }
        /// <summary> Ссылка на ассет. </summary>
        public AssetReference Reference { get; set; }
        /// <summary> Тэги для фильтрации. </summary>
        public List<string> Tags { get; set; }

        public bool TryGetMetaInfo<T>(out T info) where T : ScriptableObject;
    }

    /// <summary> Пакет с мета-информацией об ассете и ссылкой на него. </summary>
    public abstract class GroupReferencePack : ScriptableObject
    {
        public abstract void SetAssetReferencePacks(List<AssetReferencePack> references);
    }
}