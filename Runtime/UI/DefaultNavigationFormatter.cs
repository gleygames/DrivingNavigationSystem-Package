using System.Text;
using UnityEngine;

namespace Gley.NavigationSystem
{
    [CreateAssetMenu(menuName = "Gley/Navigation System/Default Formatter")]
    public class DefaultNavigationFormatter : NavigationFormatter
    {
        private const float MetersPerMile = 1609.344f;
        private const float FeetPerMeter = 3.28084f;

        [SerializeField] private NavigationSettings settings;
        [SerializeField] private NavigationUnitSystem unitSystem;

        public override void FormatDistance(float meters, StringBuilder output)
        {
            float clamped = meters;
            if (clamped < 0f)
            {
                clamped = 0f;
            }

            if (ResolveImperial())
            {
                FormatDistanceImperial(clamped, output);
            }
            else
            {
                FormatDistanceMetric(clamped, output);
            }
        }

        public override void FormatDuration(float seconds, StringBuilder output)
        {
            float clamped = seconds;
            if (clamped < 0f)
            {
                clamped = 0f;
            }

            if (clamped < 60f)
            {
                output.Append("< 1 min");
                return;
            }

            int totalMinutes = Mathf.RoundToInt(clamped / 60f);
            if (totalMinutes < 60)
            {
                AppendInteger(totalMinutes, output);
                output.Append(" min");
                return;
            }

            int hours = totalMinutes / 60;
            int minutes = totalMinutes % 60;
            AppendInteger(hours, output);
            output.Append(" h ");
            AppendInteger(minutes, output);
            output.Append(" min");
        }

        internal void SetSettings(NavigationSettings value)
        {
            settings = value;
        }

        internal void SetUnitSystem(NavigationUnitSystem value)
        {
            unitSystem = value;
        }

        private bool ResolveImperial()
        {
            if (unitSystem == NavigationUnitSystem.Metric)
            {
                return false;
            }
            if (unitSystem == NavigationUnitSystem.Imperial)
            {
                return true;
            }
            if (settings != null)
            {
                return settings.ImperialUnits;
            }
            return false;
        }

        private void FormatDistanceMetric(float meters, StringBuilder output)
        {
            if (meters < 1000f)
            {
                int rounded = Mathf.RoundToInt(meters / 10f) * 10;
                AppendInteger(rounded, output);
                output.Append(" m");
                return;
            }

            float km = meters / 1000f;
            if (km < 10f)
            {
                AppendOneDecimal(km, output);
                output.Append(" km");
                return;
            }

            AppendInteger(Mathf.RoundToInt(km), output);
            output.Append(" km");
        }

        private void FormatDistanceImperial(float meters, StringBuilder output)
        {
            float miles = meters / MetersPerMile;
            if (miles < 0.1f)
            {
                float feet = meters * FeetPerMeter;
                int rounded = Mathf.RoundToInt(feet / 50f) * 50;
                AppendInteger(rounded, output);
                output.Append(" ft");
                return;
            }

            if (miles < 10f)
            {
                AppendOneDecimal(miles, output);
                output.Append(" mi");
                return;
            }

            AppendInteger(Mathf.RoundToInt(miles), output);
            output.Append(" mi");
        }

        private void AppendOneDecimal(float value, StringBuilder output)
        {
            float rounded = Mathf.Round(value * 10f) / 10f;
            int wholePart = Mathf.FloorToInt(rounded);
            int decimalDigit = Mathf.RoundToInt((rounded - wholePart) * 10f);
            if (decimalDigit >= 10)
            {
                decimalDigit = 0;
                wholePart++;
            }

            AppendInteger(wholePart, output);
            output.Append('.');
            AppendInteger(decimalDigit, output);
        }

        private void AppendInteger(int value, StringBuilder output)
        {
            int remaining = value;
            if (remaining < 0)
            {
                output.Append('-');
                remaining = -remaining;
            }
            if (remaining == 0)
            {
                output.Append('0');
                return;
            }

            int digitCount = 0;
            int counter = remaining;
            while (counter > 0)
            {
                digitCount++;
                counter = counter / 10;
            }

            int divisor = 1;
            for (int i = 1; i < digitCount; i++)
            {
                divisor = divisor * 10;
            }

            for (int i = 0; i < digitCount; i++)
            {
                int digit = (remaining / divisor) % 10;
                output.Append((char)('0' + digit));
                divisor = divisor / 10;
            }
        }
    }
}
