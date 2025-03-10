using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace eShop.Ordering.API.Application.Commands;

using System.Diagnostics;
using eShop.Ordering.Domain.AggregatesModel.OrderAggregate;
using MediatR;

// Regular CommandHandler
public class CreateOrderCommandHandler
    : IRequestHandler<CreateOrderCommand, bool>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IIdentityService _identityService;
    private readonly IMediator _mediator;
    private readonly IOrderingIntegrationEventService _orderingIntegrationEventService;
    private readonly ILogger<CreateOrderCommandHandler> _logger;

    // ✅ Define metrics inside the class
    private readonly Counter<long> _orderPlacedCounter;
    private readonly Counter<long> _orderFailedCounter;
    private readonly Histogram<double> _orderProcessingTimeHistogram;
    private readonly Histogram<double> _orderErrorRateHistogram;

    // Using DI to inject infrastructure persistence Repositories
    public CreateOrderCommandHandler(IMediator mediator,
        IOrderingIntegrationEventService orderingIntegrationEventService,
        IOrderRepository orderRepository,
        IIdentityService identityService,
        ILogger<CreateOrderCommandHandler> logger, 
        Meter meter)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _identityService = identityService ?? throw new ArgumentNullException(nameof(identityService));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _orderingIntegrationEventService = orderingIntegrationEventService ?? throw new ArgumentNullException(nameof(orderingIntegrationEventService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _orderPlacedCounter = meter.CreateCounter<long>("order_placed_total", description: "Total number of successfully placed orders.");
        _orderFailedCounter = meter.CreateCounter<long>("order_failed_total", description: "Total number of failed order attempts.");
        _orderProcessingTimeHistogram = meter.CreateHistogram<double>("order_processing_duration_ms", "ms", "Time taken to process an order.");
        _orderErrorRateHistogram = meter.CreateHistogram<double>("order_error_rate_percent", "percent", "Percentage of orders that encountered an error.");
    }

    public async Task<bool> Handle(CreateOrderCommand message, CancellationToken cancellationToken)
    {
        //// Add Integration event to clean the basket
        //var orderStartedIntegrationEvent = new OrderStartedIntegrationEvent(message.UserId);
        //await _orderingIntegrationEventService.AddAndSaveEventAsync(orderStartedIntegrationEvent);

        //// Add/Update the Buyer AggregateRoot
        //// DDD patterns comment: Add child entities and value-objects through the Order Aggregate-Root
        //// methods and constructor so validations, invariants and business logic 
        //// make sure that consistency is preserved across the whole aggregate
        //var address = new Address(message.Street, message.City, message.State, message.Country, message.ZipCode);
        //var order = new Order(message.UserId, message.UserName, address, message.CardTypeId, message.CardNumber, message.CardSecurityNumber, message.CardHolderName, message.CardExpiration);

        //foreach (var item in message.OrderItems)
        //{
        //    order.AddOrderItem(item.ProductId, item.ProductName, item.UnitPrice, item.Discount, item.PictureUrl, item.Units);
        //}

        //// ✅ Increment order placed count
        //_logger.LogInformation("Incrementing Order Placed Counter");
        //_orderPlacedCounter.Add(1, new KeyValuePair<string, object>("userId", message.UserId));
        //_logger.LogInformation("Order Placed Counter Incremented");

        //_logger.LogInformation("Creating Order - Order: {@Order}", order);
        //_orderRepository.Add(order);

        //return await _orderRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        var stopwatch = Stopwatch.StartNew();

        Console.WriteLine("Processing order command started");


        try
        {
            var orderStartedIntegrationEvent = new OrderStartedIntegrationEvent(message.UserId);
            await _orderingIntegrationEventService.AddAndSaveEventAsync(orderStartedIntegrationEvent);

            var address = new Address(message.Street, message.City, message.State, message.Country, message.ZipCode);
            var order = new Order(message.UserId, message.UserName, address, message.CardTypeId, message.CardNumber, message.CardSecurityNumber, message.CardHolderName, message.CardExpiration);

            foreach (var item in message.OrderItems)
            {
                order.AddOrderItem(item.ProductId, item.ProductName, item.UnitPrice, item.Discount, item.PictureUrl, item.Units);
            }

            _orderPlacedCounter.Add(1, new KeyValuePair<string, object>("userId", message.UserId));
            _logger.LogInformation("Order placed successfully.");

            _orderRepository.Add(order);

            var success = await _orderRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
            stopwatch.Stop();

            _orderProcessingTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
            Console.WriteLine("Processing order command completed in: " + stopwatch.ElapsedMilliseconds);
           
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Order processing failed.");

            _orderFailedCounter.Add(1);
            _orderErrorRateHistogram.Record(100.0); // Increment error rate to 100% when failure occurs
            return false;
        }
    }
}


// Use for Idempotency in Command process
public class CreateOrderIdentifiedCommandHandler : IdentifiedCommandHandler<CreateOrderCommand, bool>
{
    public CreateOrderIdentifiedCommandHandler(
        IMediator mediator,
        IRequestManager requestManager,
        ILogger<IdentifiedCommandHandler<CreateOrderCommand, bool>> logger)
        : base(mediator, requestManager, logger)
    {
    }

    protected override bool CreateResultForDuplicateRequest()
    {
        return true; // Ignore duplicate requests for creating order.
    }
}
