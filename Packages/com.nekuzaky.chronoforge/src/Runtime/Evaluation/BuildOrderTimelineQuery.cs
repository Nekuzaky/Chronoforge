namespace Chronoforge
{
    /// <summary>
    /// Read-only queries over an evaluated timeline. Pure functions, extracted so the editor
    /// panels, the overlay and the demo player all answer "where are we?" the same way — and so
    /// the answer is unit-testable.
    /// </summary>
    public static class BuildOrderTimelineQuery
    {
        /// <summary>
        /// Index of the last timeline entry that has started by <paramref name="timeSeconds"/>,
        /// or -1 before the first step. Entries are assumed to be in start order, as produced by
        /// the simulator.
        /// </summary>
        public static int IndexAt(BuildOrderEvaluationResult result, float timeSeconds)
        {
            if (result == null)
                return -1;

            int index = -1;
            for (int i = 0; i < result.m_Timeline.Count; i++)
            {
                if (result.m_Timeline[i].m_StartSeconds > timeSeconds)
                    break;
                index = i;
            }
            return index;
        }

        /// <summary>Projected supply at <paramref name="timeSeconds"/>, or 0 before the first step.</summary>
        public static int SupplyAt(BuildOrderEvaluationResult result, float timeSeconds)
        {
            int index = IndexAt(result, timeSeconds);
            return index >= 0 ? result.m_Timeline[index].m_ProjectedSupply : 0;
        }
    }
}
