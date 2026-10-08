using UnityEngine;

namespace BattlestarLabs.TransferTable
{
    [System.Serializable]
    public class TransferTableDefinition
    {
        [Header("Vertical Alignment")]
        [Tooltip("Height of the rail head above the pit origin in meters.")]
        public float railHeightOffset = 0.13f; // Sits exactly flush on the pit rim/bridge deck!
        [Header("Table Scale")]
        [Tooltip("Length of the bridge deck along the rail axis (Z) in meters. 30.48m for 100ft, 60.96m for 200ft.")]
        public float bridgeLength = 30.48f; // Default 100 ft

        [Tooltip("Center-to-center lateral spacing between parallel tracks (X) in meters. 4.572m is 15 ft.")]
        public float trackSpacing = 4.572f; // Default 15 ft

        [Header("Pit Layout")]
        [Tooltip("Total number of parallel stalls/tracks across the pit")]
        [Range(2, 10)]
        public int totalStalls = 5;

        [Tooltip("Default starting stall index (0-based: 0 is the first track)")]
        public int defaultStallIndex = 0;

        [Header("Alignment / Mag-Lock")]
        [Tooltip("Capture window for the magnetic lock in meters. 0.0762m is ~3 inches.")]
        public float lockTolerance = 0.0762f; // ~3 inches

        /// <summary>
        /// Calculates the local X-axis offset in meters for a given stall.
        /// </summary>
        public float GetOffsetForStall(int stallIndex)
        {
            return stallIndex * trackSpacing;
        }

        /// <summary>
        /// Checks if a stall index is within the valid range.
        /// </summary>
        public bool IsValidStall(int stallIndex)
        {
            return stallIndex >= 0 && stallIndex < totalStalls;
        }

        /// <summary>
        /// Finds the closest stall index for a given offset, if within lock tolerance.
        /// </summary>
        public int? GetStallNearOffset(float currentOffset)
        {
            float rawIndex = currentOffset / trackSpacing;
            int nearest = Mathf.RoundToInt(rawIndex);

            if (!IsValidStall(nearest))
                return null;

            float distanceToCenter = Mathf.Abs(currentOffset - GetOffsetForStall(nearest));
            if (distanceToCenter <= lockTolerance)
                return nearest;

            return null;
        }
    }
}