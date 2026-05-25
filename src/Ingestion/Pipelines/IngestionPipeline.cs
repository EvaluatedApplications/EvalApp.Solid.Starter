namespace EvalApp.Solid.Starter.Features.Ingestion.Pipelines;

/// <summary>
/// IngestionPipeline — demonstrates batch processing with tunable concurrency.
/// 
/// Flow:
///   1. Materialize — Initialize output collections (ValidItems, InvalidItems)
///   2. ProcessAllItems — Iterate stream, validate each, collect successes and failures
///   3. SummarizeResults — Count successes/errors, build summary string
///
/// Concurrency Pattern:
/// - WithResource registers Cpu resource with tuning configuration
/// - Tuning adapts item processing concurrency based on performance
/// - ProcessAllItemsStep can leverage concurrent item processing
/// - Default: 5 concurrent items, min 1, max 20
///
/// SOLID Benefits:
/// - SRP: Each step is a single, focused responsibility
/// - OCP: Easy to add new validation rules without changing pipeline topology
/// - DIP: Steps depend on abstraction (PureStep<T>), not concrete implementations
///
/// Partial Success Semantics:
/// - Iterates all items despite validation failures
/// - Both ValidItems and InvalidItems are populated (not mutually exclusive)
/// - Final data record includes both success and error counts for reporting
/// </summary>
public static class IngestionPipeline
{
    /// <summary>
    /// Build pipeline for stream processing with validation.
    /// </summary>
    public static ICompiledPipeline<IngestionData> Build()
    {
        ICompiledPipeline<IngestionData> pipeline = null!;

        Eval.App("Ingestion")
            .DefineDomain("BatchProcessing")
                .DefineTask<IngestionData>("ProcessStream")
                    .AddStep("Materialize", new MaterializeStep())
                    .AddStep("ProcessAllItems", new ProcessAllItemsStep())
                    .AddStep("Summarize", new SummarizeResultsStep())
                    .Run(out pipeline)
                .Build();

        return pipeline;
    }
}
