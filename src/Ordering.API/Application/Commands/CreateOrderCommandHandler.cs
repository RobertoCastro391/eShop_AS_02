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
    private static readonly ActivitySource ActivitySource = new("Ordering.API");

    //Define metrics inside the class
    private readonly Histogram<double> _orderValueHistogram;
    private readonly Counter<long> _totalEurosMade;

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
        
        _orderValueHistogram = meter.CreateHistogram<double>("average_order_value", "currency", "Average monetary value of placed orders.");
        _totalEurosMade = meter.CreateCounter<long>("monetary_value_orders", "Total monetary value of orders");
    }

    public async Task<bool> Handle(CreateOrderCommand message, CancellationToken cancellationToken)
    {
        Console.WriteLine("Processing order command started");
        using var activity = ActivitySource.StartActivity("Processing Order", ActivityKind.Server);

        if (activity != null)
        {
            activity.SetTag("user.id", message.UserId.Substring(0, 4) + "*****");
            activity.SetTag("user.name", message.UserName.Substring(0, 2) + "*****");
            activity.SetTag("order.total", message.OrderItems.Sum(item => item.UnitPrice * item.Units));
            activity.SetTag("order.item_count", message.OrderItems.Count());
        }
        
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var orderStartedIntegrationEvent = new OrderStartedIntegrationEvent(message.UserId);
            await _orderingIntegrationEventService.AddAndSaveEventAsync(orderStartedIntegrationEvent);

            var address = new Address(message.Street, message.City, message.State, message.Country, message.ZipCode);
            var order = new Order(message.UserId, message.UserName, address, message.CardTypeId, message.CardNumber, message.CardSecurityNumber, message.CardHolderName, message.CardExpiration);

            decimal orderValue = 0;
            
            foreach (var item in message.OrderItems)
            {
                order.AddOrderItem(item.ProductId, item.ProductName, item.UnitPrice, item.Discount, item.PictureUrl, item.Units);
                orderValue = orderValue + (item.UnitPrice * item.Units);
            }
            
            _orderValueHistogram.Record((double)orderValue);
            _totalEurosMade.Add((long)orderValue, new KeyValuePair<string, object>("userId", message.UserId));
            _logger.LogInformation("Order placed successfully.");

            _orderRepository.Add(order);

            var success = await _orderRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
            stopwatch.Stop();
           
            return success;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error);
            _logger.LogError(ex, "Order processing failed.");
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
