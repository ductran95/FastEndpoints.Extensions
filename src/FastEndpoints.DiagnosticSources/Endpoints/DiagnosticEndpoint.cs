using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using FastEndpoints.DiagnosticSources.Attributes;
using FastEndpoints.Reflection.Extensions;

namespace FastEndpoints.DiagnosticSources.Endpoints;

public abstract class DiagnosticEndpoint<TRequest, TResponse> : Endpoint<TRequest, TResponse
> where TRequest : notnull, new() where TResponse : notnull, new()
{
    public Activity Activity { get; private set; }
    
    public ExceptionDispatchInfo ExceptionDispatchInfo { get; private set; }

    public string ActivityName =>
        $"{GetType().Name}.{(Definition.GetExecuteAsyncImplemented() ? nameof(ExecuteAsync) : nameof(HandleAsync))}";

    /// <summary>
    /// Wrap HandleAsync in try-catch block
    /// </summary>
    /// <param name="req"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public sealed override Task HandleAsync(TRequest req, CancellationToken ct)
    {
        try
        {
            return HandleAsync(req, Activity, ct);
        }
        catch (Exception ex)
        {
            ExceptionDispatchInfo = ExceptionDispatchInfo.Capture(ex);
            return Task.CompletedTask;
        }
    }
    
    /// <summary>
    /// Wrap ExecuteAsync in try-catch block
    /// </summary>
    /// <param name="req"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public sealed override Task<TResponse> ExecuteAsync(TRequest req,  CancellationToken ct)
    {
        try
        {
            return ExecuteAsync(req, Activity, ct);
        }
        catch (Exception ex)
        {
            ExceptionDispatchInfo = ExceptionDispatchInfo.Capture(ex);
            return Task.FromResult(default(TResponse));
        }
    }

    /// <summary>
    /// the handler method for the endpoint. this method is called for each request received.
    /// </summary>
    /// <param name="req">the request dto</param>
    /// <param name="activity"></param>
    /// <param name="ct">a cancellation token</param>
    [NotImplemented]
    public virtual Task HandleAsync(TRequest req, Activity activity, CancellationToken ct) => throw new NotImplementedException();

    /// <summary>
    /// the handler method for the endpoint that returns the response dto. this method is called for each request received.
    /// </summary>
    /// <param name="req">the request dto</param>
    /// <param name="activity"></param>
    /// <param name="ct">a cancellation token</param>
    [NotImplemented]
    public virtual Task<TResponse> ExecuteAsync(TRequest req, Activity activity, CancellationToken ct) => throw new NotImplementedException();

    /// <summary>
    /// Override ExecuteAsyncImplemented to use Internal method before executing
    /// </summary>
    /// <param name="req"></param>
    public override void OnBeforeHandle(TRequest req)
    {
        var executeAsyncMethods = this.GetType().GetMethods().Where(x=>x.Name == "ExecuteAsync").ToArray();
        var implementsExecuteAsync = executeAsyncMethods.Any(x => !x.IsDefined(Types.NotImplementedAttribute, false));
        Definition.SetExecuteAsyncImplemented(implementsExecuteAsync);
    }

    /// <summary>
    /// Start Activity OnBeforeValidate
    /// </summary>
    /// <param name="req"></param>
    public override void OnBeforeValidate(TRequest req)
    {
        StartActivity();
    }

    /// <summary>
    /// Add ValidationFailed event to Activity if any ValidationFailures
    /// </summary>
    /// <param name="req"></param>
    public override void OnAfterValidate(TRequest req)
    {
        if (Activity != null && ValidationFailures.Any())
        {
            AddValidationFailedEvent();
        }
    }

    /// <summary>
    /// Stop Activity and rethrow Exception from Internal Executed method
    /// If Internal Executed method throw ValidationFailureException, do nothing, OnValidationFailedAsync will handle
    /// </summary>
    /// <param name="req"></param>
    /// <param name="res"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public override Task OnAfterHandleAsync(TRequest req, TResponse res, CancellationToken ct = new CancellationToken())
    {
        if (ExceptionDispatchInfo.SourceException is ValidationFailureException)
        {
            return Task.CompletedTask;
        }
        
        StopActivity();

        if (ExceptionDispatchInfo != null)
        {
            Activity.SetStatus(ActivityStatusCode.Error, ExceptionDispatchInfo.SourceException.GetType().Name);
            ExceptionDispatchInfo.Throw();
        }
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// In case ValidationFailureException, OnBeforeValidate may not be run, Start Activity and Add ValidationFailed event
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    public override Task OnValidationFailedAsync(CancellationToken ct = new CancellationToken())
    {
        StartActivity();

        AddValidationFailedEvent();

        StopActivity();
        return Task.CompletedTask;
    }

    protected virtual void StartActivity()
    {
        if (Activity == null)
        {
            Activity = Tracing.ActivitySource.StartActivity(ActivityName);
        }
        else
        {
            Activity.Current = Activity;
        }
    }

    protected virtual void StopActivity()
    {
        if (Activity != null)
        {
            Activity.Dispose();
            Activity = null;
        }
    }

    protected virtual void AddValidationFailedEvent()
    {
        var tags = new ActivityTagsCollection();
        tags.Add("ValidationFailures", JsonSerializer.Serialize(ValidationFailures));
        var validatedFailedEvent = new ActivityEvent("ValidationFailed", tags: tags);
        Activity.AddEvent(validatedFailedEvent);
        Activity.SetStatus(ActivityStatusCode.Error, nameof(ValidationFailureException));
    }
}

public abstract class DiagnosticEndpointWithoutRequest : DiagnosticEndpoint<EmptyRequest>
{
    /// <summary>
    /// the handler method for the endpoint. this method is called for each request received.
    /// </summary>
    /// <param name="activity"></param>
    /// <param name="ct">a cancellation token</param>
    [NotImplemented]
    public virtual Task HandleAsync(Activity activity, CancellationToken ct) => throw new NotImplementedException();

    /// <summary>
    /// override the HandleAsync(CancellationToken ct) method instead of using this method!
    /// </summary>
    [NotImplemented]
    public sealed override Task HandleAsync(EmptyRequest _, Activity activity, CancellationToken ct) => HandleAsync(activity, ct);

    /// <summary>
    /// the handler method for the endpoint. this method is called for each request received.
    /// </summary>
    /// <param name="activity"></param>
    /// <param name="ct">a cancellation token</param>
    [NotImplemented]
    public virtual Task<object> ExecuteAsync(Activity activity,CancellationToken ct) => throw new NotImplementedException();

    /// <summary>
    /// override the ExecuteAsync(CancellationToken ct) method instead of using this method!
    /// </summary>
    [NotImplemented]
    public sealed override Task<object> ExecuteAsync(EmptyRequest _, Activity activity, CancellationToken ct) => ExecuteAsync(activity, ct);
}

/// <summary>
/// use this base class for defining endpoints that doesn't need a request dto but return a response dto.
/// </summary>
/// <typeparam name="TResponse">the type of the response dto</typeparam>
public abstract class DiagnosticEndpointWithoutRequest<TResponse> : DiagnosticEndpoint<EmptyRequest, TResponse> where TResponse : notnull, new()
{
    /// <summary>
    /// the handler method for the endpoint. this method is called for each request received.
    /// </summary>
    /// <param name="activity"></param>
    /// <param name="ct">a cancellation token</param>
    [NotImplemented]
    public virtual Task HandleAsync(Activity activity, CancellationToken ct) => throw new NotImplementedException();

    /// <summary>
    /// override the HandleAsync(CancellationToken ct) method instead of using this method!
    /// </summary>
    [NotImplemented]
    public sealed override Task HandleAsync(EmptyRequest _, Activity activity, CancellationToken ct) => HandleAsync(activity, ct);

    /// <summary>
    /// the handler method for the endpoint that returns the response dto. this method is called for each request received.
    /// </summary>
    /// <param name="activity"></param>
    /// <param name="ct">a cancellation token</param>
    [NotImplemented]
    public virtual Task<TResponse> ExecuteAsync(Activity activity, CancellationToken ct) => throw new NotImplementedException();

    /// <summary>
    /// override the ExecuteAsync(CancellationToken ct) method instead of using this method!
    /// </summary>
    [NotImplemented]
    public sealed override Task<TResponse> ExecuteAsync(EmptyRequest _, Activity activity, CancellationToken ct) => ExecuteAsync(activity, ct);
}

/// <summary>
/// use this base class for defining endpoints that only use a request dto and don't use a response dto.
/// </summary>
/// <typeparam name="TRequest">the type of the request dto</typeparam>
public abstract class DiagnosticEndpoint<TRequest> : DiagnosticEndpoint<TRequest, object> where TRequest : notnull, new() { };

/// <summary>
/// use this base class for defining endpoints that use both request and response dtos as well as require mapping to and from a domain entity using a seperate entity mapper.
/// </summary>
/// <typeparam name="TRequest">the type of the request dto</typeparam>
/// <typeparam name="TResponse">the type of the response dto</typeparam>
/// <typeparam name="TMapper">the type of the entity mapper</typeparam>
public abstract class DiagnosticEndpoint<TRequest, TResponse, TMapper> : DiagnosticEndpoint<TRequest, TResponse> where TRequest : notnull, new() where TResponse : notnull, new() where TMapper : notnull, IEntityMapper, new()
{
    /// <summary>
    /// the entity mapper for the endpoint
    /// <para>HINT: entity mappers are singletons for performance reasons. do not maintain state in the mappers.</para>
    /// </summary>
    public static TMapper Map { get; } = new();
}

/// <summary>
/// use this base class for defining endpoints that use both request and response dtos as well as require mapping to and from a domain entity.
/// </summary>
/// <typeparam name="TRequest">the type of the request dto</typeparam>
/// <typeparam name="TResponse">the type of the response dto</typeparam>
/// <typeparam name="TEntity">the type of domain entity that will be mapped to/from</typeparam>
public abstract class DiagnosticEndpointWithMapping<TRequest, TResponse, TEntity> : DiagnosticEndpoint<TRequest, TResponse> where TRequest : notnull, new() where TResponse : notnull, new()
{
    /// <summary>
    /// override this method and place the logic for mapping the request dto to the desired domain entity
    /// </summary>
    /// <param name="r">the request dto</param>
    public virtual TEntity MapToEntity(TRequest r) => throw new NotImplementedException($"Please override the {nameof(MapToEntity)} method!");
    /// <summary>
    /// override this method and place the logic for mapping the request dto to the desired domain entity
    /// </summary>
    /// <param name="r">the request dto to map from</param>
    public virtual Task<TEntity> MapToEntityAsync(TRequest r) => throw new NotImplementedException($"Please override the {nameof(MapToEntityAsync)} method!");

    /// <summary>
    /// override this method and place the logic for mapping a domain entity to a response dto
    /// </summary>
    /// <param name="e">the domain entity to map from</param>
    public virtual TResponse MapFromEntity(TEntity e) => throw new NotImplementedException($"Please override the {nameof(MapFromEntity)} method!");
    /// <summary>
    /// override this method and place the logic for mapping a domain entity to a response dto
    /// </summary>
    /// <param name="e">the domain entity to map from</param>
    public virtual Task<TResponse> MapFromEntityAsync(TEntity e) => throw new NotImplementedException($"Please override the {nameof(MapFromEntityAsync)} method!");
}