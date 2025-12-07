namespace mark.davison.common.generators.CQRS;

[ExcludeFromCodeCoverage]
public static class CQRSSources
{
    public static string UseCQRSServerAttribute(string ns)
    {
        return $@"
using System;

namespace {ns};

//[global::Microsoft.CodeAnalysis.EmbeddedAttribute]
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class UseCQRSServerAttribute : Attribute
{{
    public Type[] Types {{ get; set; }}

    public UseCQRSServerAttribute(params Type[] types)
    {{
        Types = types;
    }}
}}";
    }

    public static string UseCQRSClientAttribute(string ns)
    {
        return $@"
using System;

namespace {ns};

//[global::Microsoft.CodeAnalysis.EmbeddedAttribute]
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class UseCQRSClientAttribute : Attribute
{{
    public Type[] Types {{ get; set; }}

    public UseCQRSClientAttribute(params Type[] types)
    {{
        Types = types;
    }}
}}";
    }
}