using EvalApp.Consumer;
using EvalApp.Solid.Starter.Features.BatchSync;
using EvalApp.Solid.Starter.Features.BatchSync.Pipelines;
using EvalApp.Solid.Starter.Features.Ingestion;
using EvalApp.Solid.Starter.Features.Ingestion.Pipelines;
using EvalApp.Solid.Starter.Features.OrderSaga;
using EvalApp.Solid.Starter.Features.OrderSaga.Pipelines;
using EvalApp.Solid.Starter.Features.OrderSaga.Services;
using EvalApp.Solid.Starter.Features.RulesEngine;
using EvalApp.Solid.Starter.Features.RulesEngine.Pipelines;
using EvalApp.Solid.Starter.Shared;
using System.Collections.Immutable;

Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
Console.WriteLine("║   EvalApp SOLID Starter Tutorial - All 4 Features Demo        ║");
Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝\n");

// Feature 1: RulesEngine
Console.WriteLine("📋 [1/4] RulesEngine: Pure Logic & Business Rules");
Console.WriteLine("─────────────────────────────────────────────────");
try
{
    var rulesResult = await RunRulesEngineDemo();
    Console.WriteLine($"✅ RulesEngine completed. Final price: {rulesResult.FinalPrice:C}\n");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ RulesEngine failed: {ex.Message}\n");
}

// Feature 2: BatchSync
Console.WriteLine("📦 [2/4] BatchSync: Async I/O & Partial Failure Handling");
Console.WriteLine("─────────────────────────────────────────────────");
try
{
    var batchResult = await RunBatchSyncDemo();
    Console.WriteLine($"✅ BatchSync completed. Success: {batchResult.SuccessCount}, Failed: {batchResult.ErrorCount}\n");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ BatchSync failed: {ex.Message}\n");
}

// Feature 3: Ingestion
Console.WriteLine("🔄 [3/4] Ingestion: Stream Processing & Validation");
Console.WriteLine("─────────────────────────────────────────────────");
try
{
    var ingestionResult = await RunIngestionDemo();
    Console.WriteLine($"✅ Ingestion completed. Valid: {ingestionResult.SuccessCount}, Invalid: {ingestionResult.ErrorCount}\n");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Ingestion failed: {ex.Message}\n");
}

// Feature 4: OrderSaga
Console.WriteLine("💳 [4/4] OrderSaga: Distributed Transactions & Compensation");
Console.WriteLine("─────────────────────────────────────────────────");
try
{
    var sagaResult = await RunOrderSagaDemo();
    Console.WriteLine($"✅ OrderSaga completed. Order: {sagaResult.OrderId}, Status: {sagaResult.State}\n");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ OrderSaga failed: {ex.Message}\n");
}

Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
Console.WriteLine("║                   All Features Completed ✨                   ║");
Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝\n");

// Helper methods
static async Task<PricingData> RunRulesEngineDemo()
{
    var items = ImmutableList.Create(
        new Item("TSHIRT-1", "Blue T-Shirt", 45m, ItemCategory.Standard),
        new Item("JEANS-1", "Denim Jeans", 120m, ItemCategory.Standard),
        new Item("HAT-CLEAR", "Summer Hat", 15m, ItemCategory.Clearance));

    var shopper = new ShopperProfile(
        CustomerId: "SHOPPER-42",
        PurchaseHistoryCount: 12,
        TotalSpend: 5000m,
        IsVip: true);

    var order = new OrderContext(
        Shopper: shopper,
        Items: items,
        PromotionCode: "SUMMER20");

    var data = new PricingData(order);
    var pipeline = RulesEnginePipeline.Build();

    var result = await pipeline.RunAsync(data);

    var finalData = result switch
    {
        PipelineResult<PricingData>.Success s => s.Data,
        PipelineResult<PricingData>.Failure f => f.Data,
        _ => throw new InvalidOperationException("Pipeline did not complete")
    };

    Console.WriteLine($"Shopper: {shopper.CustomerId} (VIP: {shopper.IsVip})");
    Console.WriteLine($"  Items: {items.Count}, Net: ${finalData.NetPrice:F2}");
    Console.WriteLine($"  Discount: {finalData.DiscountPercent:P0}, Final: ${finalData.FinalPrice:F2}");
    Console.WriteLine($"  Savings: ${finalData.NetPrice - finalData.FinalPrice:F2}");

    return finalData;
}

static async Task<BatchSyncData> RunBatchSyncDemo()
{
    var pipeline = BatchSyncPipeline.Build(successRate: 0.85);
    var input = new BatchSyncData(new List<int>());
    
    var result = await pipeline.RunAsync(input);
    
    var finalData = result switch
    {
        PipelineResult<BatchSyncData>.Success s => s.Data,
        PipelineResult<BatchSyncData>.Failure f => f.Data,
        _ => throw new InvalidOperationException("Pipeline did not complete")
    };

    Console.WriteLine($"Processed: {finalData.ItemIds.Count} items");
    Console.WriteLine($"  Successes: {finalData.SuccessCount}");
    Console.WriteLine($"  Failures: {finalData.ErrorCount}");
    if (finalData.FailedIds != null && finalData.FailedIds.Count > 0)
    {
        Console.WriteLine($"  Failed IDs: {string.Join(", ", finalData.FailedIds)}");
    }

    return finalData;
}

static async Task<IngestionData> RunIngestionDemo()
{
    var rawItems = new List<RawRecord>
    {
        new RawRecord(1, "Item-A", 100m),
        new RawRecord(2, "Item-B", 250m),
        new RawRecord(3, "", 150m),  // Invalid: empty name
        new RawRecord(4, "Item-D", -50m),  // Invalid: negative amount
        new RawRecord(5, "Item-E", 75m),
        new RawRecord(6, "Item-F", 200m)
    };

    var data = new IngestionData(rawItems);
    var pipeline = IngestionPipeline.Build();

    var result = await pipeline.RunAsync(data);

    var finalData = result switch
    {
        PipelineResult<IngestionData>.Success s => s.Data,
        PipelineResult<IngestionData>.Failure f => f.Data,
        _ => throw new InvalidOperationException("Pipeline did not complete")
    };

    Console.WriteLine($"Total Input: {finalData.TotalProcessed}");
    Console.WriteLine($"  Valid: {finalData.SuccessCount}");
    Console.WriteLine($"  Invalid: {finalData.ErrorCount}");
    Console.WriteLine($"  Summary: {finalData.Summary}");

    return finalData;
}

static async Task<OrderSagaData> RunOrderSagaDemo()
{
    // Create mock services for demo
    IInventoryService inventoryService = new MockInventoryService();
    IPaymentService paymentService = new MockPaymentService(chargeAmount: 250m);
    IShipmentService shipmentService = new MockShipmentService();

    var pipeline = OrderSagaPipeline.Build(
        inventoryService,
        paymentService,
        shipmentService,
        orderAmount: 250m);

    var order = new OrderSagaData(
        OrderId: "ORD-12345",
        Items: new List<LineItem>
        {
            new LineItem("SKU-LAPTOP", 1),
            new LineItem("SKU-MOUSE", 2)
        },
        CustomerId: "CUST-ABC");

    var result = await pipeline.RunAsync(order);

    var finalData = result switch
    {
        PipelineResult<OrderSagaData>.Success s => s.Data,
        PipelineResult<OrderSagaData>.Failure f => f.Data,
        _ => throw new InvalidOperationException("Pipeline did not complete")
    };

    Console.WriteLine($"Order: {finalData.OrderId}");
    Console.WriteLine($"  Customer: {finalData.CustomerId}");
    Console.WriteLine($"  Items: {finalData.Items.Count}");
    Console.WriteLine($"  State: {finalData.State}");
    if (!string.IsNullOrEmpty(finalData.ReservationId))
        Console.WriteLine($"  Reservation: {finalData.ReservationId}");
    if (finalData.ChargeAmount.HasValue)
        Console.WriteLine($"  Charge: ${finalData.ChargeAmount:F2}");
    if (!string.IsNullOrEmpty(finalData.ShipmentId))
        Console.WriteLine($"  Shipment: {finalData.ShipmentId}");

    return finalData;
}

// Mock services for demo (moved from test project into program for immediate use)
public class MockInventoryService : IInventoryService
{
    private readonly bool _shouldFail;
    private readonly Dictionary<string, List<LineItem>> _reservations = new();

    public MockInventoryService(bool shouldFail = false)
    {
        _shouldFail = shouldFail;
    }

    public Task<string?> ReserveAsync(List<LineItem> items, CancellationToken ct)
    {
        if (_shouldFail)
            return Task.FromResult<string?>(null);

        var reservationId = $"RES-{Guid.NewGuid():N}";
        _reservations[reservationId] = items;
        return Task.FromResult<string?>(reservationId);
    }

    public Task<bool> ReleaseAsync(string reservationId, CancellationToken ct)
    {
        if (!_reservations.ContainsKey(reservationId))
            return Task.FromResult(false);

        _reservations.Remove(reservationId);
        return Task.FromResult(true);
    }

    public Dictionary<string, List<LineItem>> GetActiveReservations() => _reservations;
}

public class MockPaymentService : IPaymentService
{
    private readonly bool _shouldFail;
    private readonly decimal _chargeAmount;
    private readonly Dictionary<string, decimal> _charges = new();

    public MockPaymentService(decimal chargeAmount = 100m, bool shouldFail = false)
    {
        _chargeAmount = chargeAmount;
        _shouldFail = shouldFail;
    }

    public Task<decimal?> ChargeAsync(string customerId, decimal amount, CancellationToken ct)
    {
        if (_shouldFail)
            return Task.FromResult<decimal?>(null);

        var chargeId = $"CHG-{Guid.NewGuid():N}";
        _charges[chargeId] = amount;
        return Task.FromResult<decimal?>(amount);
    }

    public Task<bool> RefundAsync(decimal chargeAmount, CancellationToken ct)
    {
        var chargeKey = _charges.FirstOrDefault(x => x.Value == chargeAmount).Key;
        if (chargeKey == null)
            return Task.FromResult(false);

        _charges.Remove(chargeKey);
        return Task.FromResult(true);
    }

    public Dictionary<string, decimal> GetActiveCharges() => _charges;
}

public class MockShipmentService : IShipmentService
{
    private readonly bool _shouldFail;
    private readonly Dictionary<string, string> _shipments = new();

    public MockShipmentService(bool shouldFail = false)
    {
        _shouldFail = shouldFail;
    }

    public Task<string?> CreateShipmentAsync(string orderId, List<LineItem> items, CancellationToken ct)
    {
        if (_shouldFail)
            return Task.FromResult<string?>(null);

        var shipmentId = $"SHIP-{Guid.NewGuid():N}";
        _shipments[shipmentId] = orderId;
        return Task.FromResult<string?>(shipmentId);
    }

    public Task<bool> CancelShipmentAsync(string shipmentId, CancellationToken ct)
    {
        if (!_shipments.ContainsKey(shipmentId))
            return Task.FromResult(false);

        _shipments.Remove(shipmentId);
        return Task.FromResult(true);
    }

    public Dictionary<string, string> GetActiveShipments() => _shipments;
}
