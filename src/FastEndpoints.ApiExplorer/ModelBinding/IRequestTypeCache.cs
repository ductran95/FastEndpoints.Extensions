namespace FastEndpoints.ApiExplorer.ModelBinding;

public interface IRequestTypeCache
{
    RequestParameter GetRequestParameter<T>() where T : class;
    RequestParameter GetRequestParameter(Type type);
}