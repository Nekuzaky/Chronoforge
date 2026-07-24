namespace Chronoforge
{
    /// <summary>Native step categories. <see cref="Custom"/> defers naming to the step title.</summary>
    public enum BuildOrderActionType
    {
        Unit,
        Building,
        Upgrade,
        Economy,
        Scout,
        Attack,
        Expand,
        Tech,
        Defense,
        Note,
        Custom
    }

    /// <summary>Planned vs actual authoring lane for a step.</summary>
    public enum BuildOrderStepStatus
    {
        Planned,
        Actual,
        Skipped
    }

    /// <summary>Cached outcome of the last validation pass over a step.</summary>
    public enum BuildOrderValidationState
    {
        Unknown,
        Valid,
        Warning,
        Error
    }

    /// <summary>Designer-facing execution priority, independent of ordering.</summary>
    public enum BuildOrderPriority
    {
        Low,
        Normal,
        High,
        Critical
    }

    /// <summary>Severity ramp for a single validation issue.</summary>
    public enum BuildOrderIssueSeverity
    {
        Info,
        Warning,
        Error
    }

    /// <summary>Stable identifiers for every validation rule Chronoforge can raise.</summary>
    public enum BuildOrderValidationCode
    {
        EmptyStep,
        DuplicateId,
        NegativeTime,
        NonMonotonicTime,
        ImpossibleSupply,
        NegativeCost,
        MissingPrerequisite,
        DuplicatePrerequisite,
        BrokenBranch,
        SuspiciousOrder,
        ResourceShortfall,
        BenchmarkMissed,
        IncompleteData
    }

    /// <summary>What a prerequisite points at. Keeps requirements generic across games.</summary>
    public enum BuildOrderRequirementType
    {
        Step,
        Building,
        Upgrade,
        Tech,
        Resource,
        Supply,
        Custom
    }

    /// <summary>Comparison used when a branch condition is evaluated.</summary>
    public enum BuildOrderConditionOperator
    {
        Equals,
        NotEquals,
        Greater,
        GreaterOrEqual,
        Less,
        LessOrEqual,
        Exists,
        NotExists
    }
}
