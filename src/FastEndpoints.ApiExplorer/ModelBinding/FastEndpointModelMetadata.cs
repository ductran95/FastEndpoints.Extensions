using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace FastEndpoints.ApiExplorer.ModelBinding;

public class FastEndpointModelMetadata : ModelMetadata
{
    public FastEndpointModelMetadata(ModelMetadataIdentity identity) : base(identity)
    {
        IsBindingAllowed = true;
    }

    public FastEndpointModelMetadata(ModelMetadataIdentity identity, IEnumerable<ModelMetadata> properties) :
        this(identity)
    {
        Properties = new ModelPropertyCollection(properties);
    }

    public override IReadOnlyDictionary<object, object> AdditionalValues => _additionalValues;

    public override string BinderModelName { get; }
    public override Type BinderType { get; }
    public override Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource BindingSource { get; }
    public override bool ConvertEmptyStringToNull { get; }
    public override string DataTypeName { get; }
    public override string Description { get; }
    public override string DisplayFormatString { get; }
    public override string DisplayName { get; }
    public override string EditFormatString { get; }
    public override ModelMetadata ElementMetadata { get; }
    public override IEnumerable<KeyValuePair<EnumGroupAndName, string>> EnumGroupedDisplayNamesAndValues { get; }
    public override IReadOnlyDictionary<string, string> EnumNamesAndValues { get; }
    public override bool HasNonDefaultEditFormat { get; }
    public override bool HideSurroundingHtml { get; }
    public override bool HtmlEncode { get; }
    public override bool IsBindingAllowed { get; }
    public override bool IsBindingRequired { get; }
    public override bool IsEnum { get; }
    public override bool IsFlagsEnum { get; }
    public override bool IsReadOnly { get; }
    public override bool IsRequired { get; }

    public override ModelBindingMessageProvider ModelBindingMessageProvider { get; } =
        new DefaultModelBindingMessageProvider();

    public override string NullDisplayText { get; }
    public override int Order { get; }
    public override string Placeholder { get; }
    public override ModelPropertyCollection Properties { get; } = new(Enumerable.Empty<ModelMetadata>());
    public override IPropertyFilterProvider PropertyFilterProvider { get; }
    public override Func<object, object> PropertyGetter { get; }
    public override Action<object, object> PropertySetter { get; }
    public override bool ShowForDisplay { get; }
    public override bool ShowForEdit { get; }
    public override string SimpleDisplayProperty { get; }
    public override string TemplateHint { get; }
    public override bool ValidateChildren { get; }
    public override IReadOnlyList<object> ValidatorMetadata { get; } = Array.Empty<object>();

    private Dictionary<object, object> _additionalValues = new();

    public void AddAdditionalValue(object key, object value)
    {
        _additionalValues.TryAdd(key, value);
    }
}