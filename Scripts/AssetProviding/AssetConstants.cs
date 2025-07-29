using Exerussus._1Extensions.SmallFeatures;

namespace Exerussus._1OrganizerUI.Scripts.AssetProviding
{
    public static class AssetConstants
    {
        public const string VfxPack = "vfx_pack";
        public const string Sprite = "sprite";
        public const string UiPanel = "ui_panel_pack";
        
        public static readonly long VfxPackId = VfxPack.GetStableLongId();
        public static readonly long SpriteId = Sprite.GetStableLongId();
        public static readonly long UiPanelId = UiPanel.GetStableLongId();
    }
}