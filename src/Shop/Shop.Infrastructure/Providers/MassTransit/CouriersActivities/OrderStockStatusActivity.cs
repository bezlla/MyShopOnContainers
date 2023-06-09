using Interfaces.Stock.Interfaces;
using Interfaces.Stock.Requests;
using MassTransit;
using MediatR;
using Shop.MediatR.Contracts.Requests;

namespace Shop.Infrastructure.Providers.MassTransit.CouriersActivities;

public class OrderStockStatusActivity
: IExecuteActivity<OrderStockStatusActivityArguments>

{
    private readonly IStockProvider _stockProvider;
    private readonly IMediator _mediator;

    public OrderStockStatusActivity(IStockProvider stockProvider,
        IMediator mediator)
    {
        _stockProvider = stockProvider;
        _mediator = mediator;
    }

    public async Task<ExecutionResult> Execute(ExecuteContext<OrderStockStatusActivityArguments> context)
    {
        var mediatrRequest = new GetOrdersRequest(orderIds: new[] { context.Arguments.OrderId });
        var mediatrResponse = await _mediator.Send(mediatrRequest);

        var orderInfo = mediatrResponse.Orders.First();

        var getStockProductInfoRequest = new StockGetProductListRequest(new[] { orderInfo.ProductId });
        var getStockProductInfoResponse = await _stockProvider.GetProductList(getStockProductInfoRequest);

        var stockProductInfo = getStockProductInfoResponse.Products.FirstOrDefault();
        if (stockProductInfo == null)
        {
            throw new ArgumentException($"Not found product {orderInfo.ProductId}");
        }

        if (stockProductInfo.Free < orderInfo.Quantity)
        {
            throw new ArgumentException("Can't reserve products");
        }
        
        return context.Completed();
    }
}

public class OrderStockStatusActivityDefinition :
    ExecuteActivityDefinition<OrderStockStatusActivity, OrderStockStatusActivityArguments>
{
    public OrderStockStatusActivityDefinition()
    {
        ConcurrentMessageLimit = 20;
    }
}