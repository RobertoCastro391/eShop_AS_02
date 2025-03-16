using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net.NetworkInformation;

namespace eShop.Ordering.API.Application.DomainEventHandlers;

public class OrderStatusChangedToPaidDomainEventHandler : INotificationHandler<OrderStatusChangedToPaidDomainEvent>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger _logger;
    private readonly IBuyerRepository _buyerRepository;
    private readonly IOrderingIntegrationEventService _orderingIntegrationEventService;

    private readonly Histogram<double> _orderPaymentProcessingTime;
    private static readonly ActivitySource ActivitySource = new("Ordering.API");

    public OrderStatusChangedToPaidDomainEventHandler(
        IOrderRepository orderRepository,
        ILogger<OrderStatusChangedToPaidDomainEventHandler> logger,
        IBuyerRepository buyerRepository,
        IOrderingIntegrationEventService orderingIntegrationEventService,
        Meter meter)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _buyerRepository = buyerRepository ?? throw new ArgumentNullException(nameof(buyerRepository));
        _orderingIntegrationEventService = orderingIntegrationEventService ?? throw new ArgumentNullException(nameof(orderingIntegrationEventService));
        
        _orderPaymentProcessingTime = meter.CreateHistogram<double>("payment_processing_time_ms", unit: "ms", 
            description: "Time taken to process a payment."
        );
    }

    public async Task Handle(OrderStatusChangedToPaidDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        OrderingApiTrace.LogOrderStatusUpdated(_logger, domainEvent.OrderId, OrderStatus.Paid);

        var order = await _orderRepository.GetAsync(domainEvent.OrderId);
        var buyer = await _buyerRepository.FindByIdAsync(order.BuyerId.Value);

        using var activity = ActivitySource.StartActivity("Processing Payment");

        if (activity != null)
        {
            activity?.SetTag("user.id", buyer.IdentityGuid.Substring(0, 4) + "****");
            activity?.SetTag("payment.id", order.PaymentId);
        }

        var orderStockList = domainEvent.OrderItems
            .Select(orderItem => new OrderStockItem(orderItem.ProductId, orderItem.Units));

        var integrationEvent = new OrderStatusChangedToPaidIntegrationEvent(
            domainEvent.OrderId,
            order.OrderStatus,
            buyer.Name,
            buyer.IdentityGuid,
            orderStockList);

        var stopwatch = Stopwatch.StartNew(); //Start measuring time
        await _orderingIntegrationEventService.AddAndSaveEventAsync(integrationEvent);

        stopwatch.Stop();
        activity?.SetTag("order.payment.processing_time_ms", stopwatch.ElapsedMilliseconds);
        _orderPaymentProcessingTime.Record(stopwatch.ElapsedMilliseconds);
    }
}
