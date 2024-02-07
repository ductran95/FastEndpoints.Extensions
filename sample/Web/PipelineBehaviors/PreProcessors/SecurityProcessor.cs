namespace Web.PipelineBehaviors.PreProcessors;

public class SecurityProcessor<TRequest> : IPreProcessor<TRequest>
{
    public Task PreProcessAsync(IPreProcessorContext<TRequest> context, CancellationToken ct)
    {
        var tenantID = context.HttpContext.Request.Headers["tenant-id"].FirstOrDefault();

        if (tenantID == null)
        {
            context.ValidationFailures.Add(new("MissingHeaders", "The [tenant-id] header needs to be set!"));
            return context.HttpContext.Response.SendErrorsAsync(context.ValidationFailures);
        }

        return tenantID != "qwerty" ? context.HttpContext.Response.SendForbiddenAsync() : Task.CompletedTask;
    }
}