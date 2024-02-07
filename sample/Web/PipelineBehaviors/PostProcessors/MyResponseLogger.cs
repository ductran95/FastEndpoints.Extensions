namespace Web.PipelineBehaviors.PostProcessors;

public class MyResponseLogger<TRequest, TResponse> : IPostProcessor<TRequest, TResponse>
{
    public Task PostProcessAsync(IPostProcessorContext<TRequest, TResponse> context, CancellationToken ct)
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<TResponse>>();

        if (context.Response is Sales.Orders.Create.Response response)
        {
            logger.LogWarning($"sale complete: {response?.OrderID}");
        }

        return Task.CompletedTask;
    }
}