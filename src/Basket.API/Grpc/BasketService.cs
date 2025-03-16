using System.Diagnostics.CodeAnalysis;
using eShop.Basket.API.Repositories;
using eShop.Basket.API.Extensions;
using eShop.Basket.API.Model;
using System.Diagnostics.Metrics;
using System.Diagnostics;
using Grpc.Core;

namespace eShop.Basket.API.Grpc;

public class BasketService(
    IBasketRepository repository,
    ILogger<BasketService> logger,
    Meter meter) : Basket.BasketBase
{

    private static readonly ActivitySource ActivitySource = new("Basket.API.BasketService");

    // Counter to track the number of baskets created
    private readonly Counter<long> BasketCreatedCounter =
        meter.CreateCounter<long>("basket_created_count", description: "Number of baskets created or updated.");

    [AllowAnonymous]
    public override async Task<CustomerBasketResponse> GetBasket(GetBasketRequest request, ServerCallContext context)
    {
        using var activity = ActivitySource.StartActivity("Get Basket");

        var userId = context.GetUserIdentity();
        if (activity != null && userId != null)
        {
            activity?.SetTag("user.id", userId.Substring(0, 4) + "****");
        }

        if (string.IsNullOrEmpty(userId))
        {
            return new();
        }

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Begin GetBasketById call from method {Method} for basket id {Id}", context.Method, userId.Substring(0,4) + "*****");
        }

        var data = await repository.GetBasketAsync(userId);

        if (data is not null)
        {
            return MapToCustomerBasketResponse(data);
        }

        return new();
    }

    public override async Task<CustomerBasketResponse> UpdateBasket(UpdateBasketRequest request, ServerCallContext context)
    {
        using var activity = ActivitySource.StartActivity("Update Basket");

        var userId = context.GetUserIdentity();

        if (string.IsNullOrEmpty(userId))
        {
            ThrowNotAuthenticated();
        }

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Begin UpdateBasket call from method {Method} for basket id {Id}", context.Method, userId.Substring(0, 4) + "*****");
        }

        // Check if the user already has a basket
        var existingBasket = await repository.GetBasketAsync(userId);

        var customerBasket = MapToCustomerBasket(userId, request);

        // Initialize the activity for saving the basket to the DB
        using (var dbActivity = ActivitySource.StartActivity("Saving Basket to DB"))
        {
            dbActivity?.SetTag("db.system", "redis");
            dbActivity?.SetTag("db.statement", "SET Basket");
        }


        if (activity != null)
        {
            activity?.SetTag("user.id", userId.Substring(0, 4) + "****");
            activity?.SetTag("items", customerBasket.Items);
        }
        
        var response = await repository.UpdateBasketAsync(customerBasket);
        if (response is null)
        {
            ThrowBasketDoesNotExist(userId);
        }

        // If there was no existing basket, this is a new basket → Increment the counter
        if (existingBasket is null)
        {
            BasketCreatedCounter.Add(1, new KeyValuePair<string, object>("userId", userId.Substring(0, 4) + "*****"));
            logger.LogInformation("New basket created for user");
        }

        return MapToCustomerBasketResponse(response);
    }

    public override async Task<DeleteBasketResponse> DeleteBasket(DeleteBasketRequest request, ServerCallContext context)
    {
        using var activity = ActivitySource.StartActivity("Delete Basket");

        var userId = context.GetUserIdentity();

        if (activity != null)
        {
            activity?.SetTag("user.id", userId.Substring(0, 4) + "****");
        }

        if (string.IsNullOrEmpty(userId))
        {
            ThrowNotAuthenticated();
        }

        await repository.DeleteBasketAsync(userId);
        return new();
    }

    [DoesNotReturn]
    private static void ThrowNotAuthenticated() => throw new RpcException(new Status(StatusCode.Unauthenticated, "The caller is not authenticated."));

    [DoesNotReturn]
    private static void ThrowBasketDoesNotExist(string userId) => throw new RpcException(new Status(StatusCode.NotFound, $"Basket with buyer id {userId} does not exist"));

    private static CustomerBasketResponse MapToCustomerBasketResponse(CustomerBasket customerBasket)
    {
        var response = new CustomerBasketResponse();

        foreach (var item in customerBasket.Items)
        {
            response.Items.Add(new BasketItem()
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
            });
        }

        return response;
    }

    private static CustomerBasket MapToCustomerBasket(string userId, UpdateBasketRequest customerBasketRequest)
    {
        var response = new CustomerBasket
        {
            BuyerId = userId
        };

        foreach (var item in customerBasketRequest.Items)
        {
            response.Items.Add(new()
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
            });
        }

        return response;
    }
}
