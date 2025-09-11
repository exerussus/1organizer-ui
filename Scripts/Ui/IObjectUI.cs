using Exerussus.GameSharing.Runtime;

namespace Exerussus._1OrganizerUI.Scripts.Ui
{
    public interface IObjectUI
    {
        public void Initialize(GameShare shareData);
        public void Activate();
        public void Deactivate();
        public void UpdateObject();
    }
}