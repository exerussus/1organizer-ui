using Exerussus._1OrganizerUI.Scripts.AssetProviding;

namespace Exerussus._1OrganizerUI.Scripts.ContentHandlerFeature
{
    public interface IHandleManager
    {
        public string AssetType { get; }
        public IContentHandle CreateHandle(string assetPackId);
        public bool ContainsAssetPackId(string assetPackId);
    }
}