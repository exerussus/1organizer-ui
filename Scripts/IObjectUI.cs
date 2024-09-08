namespace Exerussus._1OrganizerUI.Scripts
{
    public interface IObjectUI
    {
        public void Initialize(ShareData shareData);
        public void Activate();
        public void Deactivate();
        public void UpdateObject();
    }
}