namespace Gley.NavigationSystem
{
    public class SpeedUnits
    {
        private const float KmhPerMetersPerSecond = 3.6f;
        private const float MphPerMetersPerSecond = 2.2369362920544f;

        public float KmhToMetersPerSecond(float kmh)
        {
            return kmh / KmhPerMetersPerSecond;
        }

        public float MphToMetersPerSecond(float mph)
        {
            return mph / MphPerMetersPerSecond;
        }

        public float MetersPerSecondToKmh(float metersPerSecond)
        {
            return metersPerSecond * KmhPerMetersPerSecond;
        }

        public float MetersPerSecondToMph(float metersPerSecond)
        {
            return metersPerSecond * MphPerMetersPerSecond;
        }
    }
}
