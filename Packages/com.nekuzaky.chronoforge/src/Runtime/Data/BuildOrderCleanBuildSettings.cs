using System;

namespace Chronoforge
{
    /// <summary>
    /// Thresholds for the "clean build" analysis — the execution-quality checks competitive play
    /// cares about: never supply-blocked, production never idle, resources never stockpiling,
    /// worker production never interrupted. Off by default because it needs the extra per-step
    /// data (supply provided, facility ids, worker flag) to be meaningful.
    /// </summary>
    [Serializable]
    public sealed class BuildOrderCleanBuildSettings
    {
        public bool m_Enabled;

        /// <summary>Supply cap at the start of the build, before any step adds to it.</summary>
        public int m_StartingSupplyCap = 15;

        public bool m_CheckSupplyBlocks = true;
        public bool m_CheckStockpiling = true;
        public bool m_CheckIdleProduction = true;
        public bool m_CheckWorkerGaps = true;

        /// <summary>Balance above which a modelled resource counts as stockpiling.</summary>
        public float m_StockpileThreshold = 400f;

        /// <summary>Seconds a facility may sit without producing before it is reported.</summary>
        public float m_MaxFacilityIdleSeconds = 25f;

        /// <summary>Seconds allowed between two worker steps before a gap is reported.</summary>
        public float m_MaxWorkerGapSeconds = 20f;
    }
}
