namespace Plugins.Exerussus._1OrganizerUI.Scripts.Ui
{
    public interface IObjectUI
    {
        public void Initialize(ShareData shareData);
        public void Activate();
        public void Deactivate();
        public void UpdateObject();
    }
}