namespace mark.davison.common;

[ExcludeFromCodeCoverage]
public static class SerializationHelpers
{
    public static JsonSerializerOptions CreateStandardSerializationOptions() =>
        new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            Converters =
            {
                new JsonStringEnumConverter()
            }
        };
}
