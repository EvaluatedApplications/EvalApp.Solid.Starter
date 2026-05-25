using EvalApp.Solid.Starter.Features.OrderSaga.Services;
using EvalApp.Solid.Starter.Features.OrderSaga.Steps;

namespace EvalApp.Solid.Starter.Features.OrderSaga.Pipelines;

/// <summary>
/// OrderSaga Pipeline — Demonstrates SOLID principles via distributed transaction handling.
/// 
/// Saga Topology with Resource Gating:
///   BeginSagaStep
///     → Gate(Network) → ReserveInventoryStep [compensates with: ReleaseReservationStep]
///     → Gate(Network) → ChargePaymentStep [compensates with: RefundPaymentStep]
///     → Gate(Network) → ShipStep [compensates with: CancelShipmentStep]
///     → EndSagaStep
/// 
/// Gates Pattern:
/// - WithResource registers Network resource for adaptive tuning
/// - Gate(ResourceKind.Network) wraps external service calls
/// - Tuning adapts concurrency based on network wait times
/// - Configuration: min 1, max 10, default 5 concurrent calls
/// 
/// Compensation Semantics:
///   - If ReserveInventoryStep fails, nothing to compensate
///   - If ChargePaymentStep fails, ReserveInventoryStep is compensated (release reservation)
///   - If ShipStep fails, ChargePaymentStep and ReserveInventoryStep are compensated in reverse order
///   - If any compensation fails, order is marked as orphaned/manual-review required
/// 
/// SOLID Benefits:
///   - SRP: Each step (forward + compensation) has single responsibility
///   - OCP: New saga steps added without changing pipeline topology
///   - DIP: Depend on interfaces (IInventoryService, IPaymentService, IShipmentService)
///   - LSP: All steps follow same contract (AsyncStep<T>)
/// </summary>
public static class OrderSagaPipeline
{
    /// <summary>
    /// Build saga pipeline with resource gating and adaptive tuning for external service calls.
    /// </summary>
    public static ICompiledPipeline<OrderSagaData> Build(
        IInventoryService inventoryService,
        IPaymentService paymentService,
        IShipmentService shipmentService,
        decimal orderAmount)
    {
        ICompiledPipeline<OrderSagaData> pipeline = null!;

        Eval.App("OrderSaga")
            .WithResource(ResourceKind.Network, new TunableConfig(Min: 1, Max: 10, Default: 5))
            .DefineDomain("Fulfillment")
                .DefineTask<OrderSagaData>("ProcessOrder")
                    .AddStep("BeginSaga", new BeginSagaStep())
                    .Gate(ResourceKind.Network, null, gate => gate
                        .AddStep("ReserveInventory", new ReserveInventoryStep(inventoryService)))
                    .Gate(ResourceKind.Network, null, gate => gate
                        .AddStep("ChargePayment", new ChargePaymentStep(paymentService, orderAmount)))
                    .Gate(ResourceKind.Network, null, gate => gate
                        .AddStep("Ship", new ShipStep(shipmentService)))
                    .AddStep("EndSaga", new EndSagaStep())
                    .Run(out pipeline)
                .Build();

        return pipeline;
    }
}
