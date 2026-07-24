using System;

namespace Chronoforge
{
    /// <summary>
    /// A simulated slot on the timeline for one step: when it starts and ends, the projected
    /// supply after it resolves, and whether the economy could afford it at that moment.
    /// </summary>
    [Serializable]
    public sealed class BuildOrderTimelineEntry
    {
        public string m_StepId = "";
        public int m_StepIndex = -1;
        public float m_StartSeconds;
        public float m_EndSeconds;
        public int m_ProjectedSupply;
        public bool m_HasResourceShortfall;

        public float Duration => m_EndSeconds - m_StartSeconds;
    }
}
