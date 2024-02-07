namespace Web.PipelineBehaviors.PreProcessors;

public class MyRequestLogger<TRequest> : IPreProcessor<TRequest>
{
    public Task PreProcessAsync(IPreProcessorContext<TRequest> context, CancellationToken ct)
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<TRequest>>();

        logger.LogInformation($"request:{context.Request?.GetType().FullName} path: {context.HttpContext.Request.Path}");

        return Task.CompletedTask;
    }
}
