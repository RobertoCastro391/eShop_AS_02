using System.Diagnostics;

namespace eShop.Ordering.Infrastructure.Repositories;

public class OrderRepository
    : IOrderRepository
{
    private readonly OrderingContext _context;
    private static readonly ActivitySource ActivitySource = new("Ordering.API");
    public IUnitOfWork UnitOfWork => _context;

    public OrderRepository(OrderingContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Order Add(Order order)
    {
        using var activity = ActivitySource.StartActivity("Insert Order to DB");

        activity?.SetTag("db.system", "mssql");
        activity?.SetTag("db.operation", "INSERT");
        activity?.SetTag("order.id", order.Id);
        activity?.SetTag("order.user_id", order.BuyerId?.ToString().Substring(0,4) + "****");

        var newOrder = _context.Orders.Add(order).Entity;
        activity?.SetStatus(ActivityStatusCode.Ok);

        return newOrder;

    }

    public async Task<Order> GetAsync(int orderId)
    {

        using var activity = ActivitySource.StartActivity("Retrieve Order from DB");

        activity?.SetTag("db.system", "mssql");
        activity?.SetTag("db.operation", "SELECT");
        activity?.SetTag("order.id", orderId);

        var order = await _context.Orders.FindAsync(orderId);

        if (order != null)
        {
            await _context.Entry(order)
                .Collection(i => i.OrderItems).LoadAsync();
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        else
        {
            activity?.SetStatus(ActivityStatusCode.Error);
        }

        return order;
    }

    public void Update(Order order)
    {
        using var activity = ActivitySource.StartActivity("Update Order in DB");

        activity?.SetTag("db.system", "mssql");
        activity?.SetTag("db.operation", "UPDATE");
        activity?.SetTag("order.id", order.Id);
        activity?.SetTag("order.status", order.OrderStatus.ToString());

        _context.Entry(order).State = EntityState.Modified;
        activity?.SetStatus(ActivityStatusCode.Ok);
    }
}
