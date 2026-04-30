using System;
using System.Globalization;
using System.Linq;

namespace LmpClient.Extensions
{
    public static class ConfigNodeExtension
    {
        /// <summary>
        /// Checks if the given config node from a protovessel has invalid position or orbit values.
        /// </summary>
        public static bool VesselHasNaNPosition(this ConfigNode vesselNode)
        {
            if (vesselNode == null)
                return true;

            var landed = string.Equals(vesselNode.GetValue("landed"), "True", StringComparison.OrdinalIgnoreCase);
            var splashed = string.Equals(vesselNode.GetValue("splashed"), "True", StringComparison.OrdinalIgnoreCase);

            if (landed || splashed)
            {
                return HasInvalidDouble(vesselNode.values.GetValue("lat"))
                    || HasInvalidDouble(vesselNode.values.GetValue("lon"))
                    || HasInvalidDouble(vesselNode.values.GetValue("alt"));
            }

            var orbitNode = vesselNode.GetNode("ORBIT");
            if (orbitNode != null)
            {
                var orbitValues = orbitNode.values
                    .DistinctNames()
                    .Select(v => orbitNode.GetValue(v))
                    .ToArray();

                var allValuesAre0 = orbitValues
                    .Take(7)
                    .All(v => v == "0");

                return allValuesAre0 || orbitValues.Any(HasInvalidDouble);
            }

            return false;
        }

        private static bool HasInvalidDouble(string value)
        {
            return double.TryParse(
                       value,
                       NumberStyles.Float,
                       CultureInfo.InvariantCulture,
                       out var parsedValue)
                   && (double.IsNaN(parsedValue) || double.IsInfinity(parsedValue));
        }
    }
}