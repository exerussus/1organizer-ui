﻿using Exerussus._1Extensions.SmallFeatures;

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