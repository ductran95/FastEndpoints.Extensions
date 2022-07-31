using System.Diagnostics;
using System.Text.Json;
using FastEndpoints.DiagnosticSources.Extensions;
using FastEndpoints.Reflection.Extensions;

namespace FastEndpoints.DiagnosticSources.Endpoint;

public abstract class DiagnosticEndpoint<TRequest, TResponse> : Endpoint<TRequest, TResponse
> where TRequest : notnull, new() where TResponse : notnull, new()
{
    public Activity Activity => Activity.Current != null && Activity.Current.OperationName == this.GetActivityName() ? Activity.Current : null;
    
    /// <summary>
    /// Add ValidationFailed event to Activity if any ValidationFailures
    /// </summary>
    /// <param name="req"></param>
    public override void OnAfterValidate(TRequest req)
    {
        if (ValidationFailures.Any())
        {
            AddValidationFailedEvent();
        }
    }

    /// <summary>
    /// In case ValidationFailureException, OnBeforeValidate may not be run, Start Activity and Add ValidationFailed event
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    public override Task OnValidationFailedAsync(CancellationToken ct = new CancellationToken())
    {
        AddValidationFailedEvent();
        return Task.CompletedTask;
    }

    protected virtual void AddValidationFailedEvent()
    {
        if (Activity != null)
        {
            var tags = new ActivityTagsCollection();
            tags.Add("ValidationFailures", JsonSerializer.Serialize(ValidationFailures));
            var validatedFailedEvent = new ActivityEvent("ValidationFailed", tags: tags);
            Activity.AddEvent(validatedFailedEvent);
            Activity.SetStatus(ActivityStatusCode.Error, nameof(ValidationFailureException));
        }
    }
}

public abstract class DiagnosticEndpointWithoutRequest : EndpointWithoutRequest
{
    public Activity Activity => Activity.Current != null && Activity.Current.OperationName == this.GetActivityName() ? Activity.Current : null;
}

public abstract class DiagnosticEndpointWithoutRequest<TResponse> : EndpointWithoutRequest<TResponse>
    where TResponse : notnull, new()
{
    public Activity Activity => Activity.Current != null && Activity.Current.OperationName == this.GetActivityName() ? Activity.Current : null;
}

public abstract class DiagnosticEndpoint<TRequest> : Endpoint<TRequest> where TRequest : notnull, new()
{
    public Activity Activity => Activity.Current != null && Activity.Current.OperationName == this.GetActivityName() ? Activity.Current : null;

    /// <summary>
    /// Add ValidationFailed event to Activity if any ValidationFailures
    /// </summary>
    /// <param name="req"></param>
    public override void OnAfterValidate(TRequest req)
    {
        if (ValidationFailures.Any())
        {
            AddValidationFailedEvent();
        }
    }

    /// <summary>
    /// In case ValidationFailureException, OnBeforeValidate may not be run, Start Activity and Add ValidationFailed event
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    public override Task OnValidationFailedAsync(CancellationToken ct = new CancellationToken())
    {
        AddValidationFailedEvent();
        return Task.CompletedTask;
    }

    protected virtual void AddValidationFailedEvent()
    {
        if (Activity != null)
        {
            var tags = new ActivityTagsCollection();
            tags.Add("ValidationFailures", JsonSerializer.Serialize(ValidationFailures));
            var validatedFailedEvent = new ActivityEvent("ValidationFailed", tags: tags);
            Activity.AddEvent(validatedFailedEvent);
            Activity.SetStatus(ActivityStatusCode.Error, nameof(ValidationFailureException));
        }
    }
}

public abstract class DiagnosticEndpoint<TRequest, TResponse, TMapper> : Endpoint<TRequest, TResponse, TMapper>
    where TRequest : notnull, new() where TResponse : notnull, new() where TMapper : notnull, IEntityMapper, new()
{
    public Activity Activity => Activity.Current != null && Activity.Current.OperationName == this.GetActivityName() ? Activity.Current : null;

    /// <summary>
    /// Add ValidationFailed event to Activity if any ValidationFailures
    /// </summary>
    /// <param name="req"></param>
    public override void OnAfterValidate(TRequest req)
    {
        if (ValidationFailures.Any())
        {
            AddValidationFailedEvent();
        }
    }

    /// <summary>
    /// In case ValidationFailureException, OnBeforeValidate may not be run, Start Activity and Add ValidationFailed event
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    public override Task OnValidationFailedAsync(CancellationToken ct = new CancellationToken())
    {
        AddValidationFailedEvent();
        return Task.CompletedTask;
    }

    protected virtual void AddValidationFailedEvent()
    {
        if (Activity != null)
        {
            var tags = new ActivityTagsCollection();
            tags.Add("ValidationFailures", JsonSerializer.Serialize(ValidationFailures));
            var validatedFailedEvent = new ActivityEvent("ValidationFailed", tags: tags);
            Activity.AddEvent(validatedFailedEvent);
            Activity.SetStatus(ActivityStatusCode.Error, nameof(ValidationFailureException));
        }
    }
}

public abstract class DiagnosticEndpointWithMapping<TRequest, TResponse, TEntity> : EndpointWithMapping<TRequest, TResponse, TEntity>
    where TRequest : notnull, new() where TResponse : notnull, new()
{
    public Activity Activity => Activity.Current != null && Activity.Current.OperationName == this.GetActivityName() ? Activity.Current : null;

    /// <summary>
    /// Add ValidationFailed event to Activity if any ValidationFailures
    /// </summary>
    /// <param name="req"></param>
    public override void OnAfterValidate(TRequest req)
    {
        if (ValidationFailures.Any())
        {
            AddValidationFailedEvent();
        }
    }

    /// <summary>
    /// In case ValidationFailureException, OnBeforeValidate may not be run, Start Activity and Add ValidationFailed event
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    public override Task OnValidationFailedAsync(CancellationToken ct = new CancellationToken())
    {
        AddValidationFailedEvent();
        return Task.CompletedTask;
    }

    protected virtual void AddValidationFailedEvent()
    {
        if (Activity != null)
        {
            var tags = new ActivityTagsCollection();
            tags.Add("ValidationFailures", JsonSerializer.Serialize(ValidationFailures));
            var validatedFailedEvent = new ActivityEvent("ValidationFailed", tags: tags);
            Activity.AddEvent(validatedFailedEvent);
            Activity.SetStatus(ActivityStatusCode.Error, nameof(ValidationFailureException));
        }
    }
}