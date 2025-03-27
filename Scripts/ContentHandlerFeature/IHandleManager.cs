using System.Threading.Tasks;

namespace Exerussus._1OrganizerUI.Scripts.ContentHandlerFeature
{
    public interface IHandleManager
    {
        public string AssetType { get; }
        public Task<IContentHandle> CreateHandle(string assetPackId);
        public bool ContainsAssetPackId(string assetPackId);
    }
}