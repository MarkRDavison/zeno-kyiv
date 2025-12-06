using mark.davison.common.authentication.server.Configuration;
using mark.davison.common.generators.Configuration;
using mark.davison.common.server.abstractions.Configuration;
using mark.davison.common.server.Configuration;

namespace mark.davison.common.generators.tests.Configuration;

public sealed class AppSettingsSourceGeneratorTests
{
    [Test]
    public async Task AllChildSettingsAreRegistered()
    {
        var source = @"
using mark.davison.common.server.Configuration;
using mark.davison.common.server.abstractions.Configuration;
using mark.davison.common.authentication.server.Configuration;

namespace mark.davison.common.generators.tests.settings
{
    public class IntermediateSettings : IAppSettings
    {
        public string SECTION => ""INTERMEDIATE"";
        public RedisSettings REDIS { get; set; } = new();
    }

    public class RootAppSettings : IRootAppSettings
    {
        public string SECTION => ""COMMON"";
    
        public bool PRODUCTION_MODE { get; set; }
        public AuthenticationSettings AUTHENTICATION { get; set; } = new();
        public IntermediateSettings INTERMEDIATE { get; set; } = new();
    }
}

";

        var redisSettings = typeof(RedisSettings).Assembly.GetReferencedAssemblies().Select(_ => _.Name).ToList();

        var text = string.Join(Environment.NewLine, redisSettings);

        var result = GeneratorTestHelpers.RunSourceGenerator<AppSettingsSourceGenerator>(
            source,
            [
                typeof(IAppSettings),
                typeof(AuthenticationSettings),
                typeof(RedisSettings)
            ]);

        await Assert.That(result).IsNotNull();


        var expectedGeneratedFileName = "AppSettingsExtensions.g.cs";

        var di = result.Results
            .SelectMany(_ => _.GeneratedSources)
            .First(_ => _.HintName == expectedGeneratedFileName);

        var sourceStringDi = di.SourceText.ToString();

        await Assert.That(sourceStringDi).Contains("namespace mark.davison.common.generators.tests.settings");
        await Assert.That(sourceStringDi).Contains("services.Configure<mark.davison.common.generators.tests.settings.RootAppSettings>(section);");
        await Assert.That(sourceStringDi).Contains("services.Configure<mark.davison.common.authentication.server.Configuration.AuthenticationSettings>(section.GetSection(\"AUTHENTICATION\"));");
        await Assert.That(sourceStringDi).Contains("services.Configure<mark.davison.common.generators.tests.settings.IntermediateSettings>(section.GetSection(\"INTERMEDIATE\"));");
        await Assert.That(sourceStringDi).Contains("services.Configure<mark.davison.common.server.Configuration.RedisSettings>(section.GetSection(\"INTERMEDIATE\").GetSection(\"REDIS\"));");
    }
}
