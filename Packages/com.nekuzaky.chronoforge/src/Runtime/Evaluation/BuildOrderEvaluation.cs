namespace Chronoforge
{
    /// <summary>
    /// Single entry point that runs the full analysis of a build order — simulate, validate,
    /// and check benchmarks — and returns one combined result. Keeps callers (editor, overlay,
    /// tests) from having to know the individual passes or their order.
    /// </summary>
    public static class BuildOrderEvaluation
    {
        public static BuildOrderEvaluationResult Run(BuildOrderAsset asset)
        {
            BuildOrderEvaluationResult result = BuildOrderSimulator.Evaluate(asset);
            result.m_Issues.AddRange(BuildOrderValidator.Validate(asset));
            result.m_Issues.AddRange(BuildOrderBenchmarkEvaluator.Evaluate(asset, result));
            return result;
        }
    }
}
