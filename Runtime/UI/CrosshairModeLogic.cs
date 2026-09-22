namespace Gley.NavigationSystem
{
    internal class CrosshairModeLogic
    {
        private bool activeInAuto;

        public CrosshairMode Mode { get; set; }

        public bool IsCrosshairActive
        {
            get
            {
                if (Mode == CrosshairMode.Always)
                {
                    return true;
                }
                if (Mode == CrosshairMode.Never)
                {
                    return false;
                }
                return activeInAuto;
            }
        }

        public void NotifyPointerInput()
        {
            activeInAuto = false;
        }

        public void NotifyCrosshairInput()
        {
            activeInAuto = true;
        }

        public void ForceActive(bool active)
        {
            activeInAuto = active;
        }
    }
}
