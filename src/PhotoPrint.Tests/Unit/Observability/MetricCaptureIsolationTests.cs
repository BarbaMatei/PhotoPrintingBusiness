using System.Diagnostics;
using FluentAssertions;
using PhotoPrint.API.Observability;
using PhotoPrint.Tests.Helpers;

namespace PhotoPrint.Tests.Unit.Observability;

public class MetricCaptureIsolationTests
{
    private static void EmitOneOrder() =>
        FotoMetrics.OrdersCreated.Add(1, new TagList
        {
            { MetricNames.Labels.Processor, MetricNames.ProcessorValues.Stripe },
            { MetricNames.Labels.Status,    MetricNames.OrderStatusValues.Created },
        });

    [Fact]
    public void A_capture_sees_what_its_own_test_emits()
    {
        using var metrics = new MetricCapture(MetricNames.Instruments.OrdersCreatedTotal);

        EmitOneOrder();

        metrics.For(MetricNames.Instruments.OrdersCreatedTotal).Should().ContainSingle();
    }

    [Fact]
    public async Task A_capture_does_not_see_what_another_test_emits()
    {
        using var metrics = new MetricCapture(MetricNames.Instruments.OrdersCreatedTotal);

        // Suppressing the flow is what a genuinely unrelated test looks like from here: its work
        // runs without this capture's context. The await sits outside the block because
        // AsyncFlowControl has to be undone on the thread that created it.
        Task emitted;
        using (ExecutionContext.SuppressFlow())
        {
            emitted = Task.Run(EmitOneOrder);
        }

        await emitted;

        metrics.For(MetricNames.Instruments.OrdersCreatedTotal).Should().BeEmpty(
            "a listener on a process-wide meter must not attribute another test's measurement");
    }

    [Fact]
    public async Task Work_the_test_awaits_is_still_captured()
    {
        using var metrics = new MetricCapture(MetricNames.Instruments.OrdersCreatedTotal);

        await Task.Run(EmitOneOrder);

        metrics.For(MetricNames.Instruments.OrdersCreatedTotal).Should().ContainSingle(
            "the context flows into work the test starts, so real call sites stay visible");
    }
}
