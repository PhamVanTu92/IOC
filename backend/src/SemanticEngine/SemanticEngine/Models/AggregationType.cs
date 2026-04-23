namespace SemanticEngine.Models;

public enum AggregationType
{
    Sum,
    Average,
    Count,
    CountDistinct,
    Min,
    Max,
    // Window functions
    RunningTotal,
    PercentOfTotal
}
