using mark.davison.common.server.tests.CQRS;
using mark.davison.common.server.Utilities;
using Microsoft.Extensions.Primitives;
using System.Text;
using System.Text.Json;

namespace mark.davison.common.server.tests.Utilities;

public sealed class WebUtilitiesTests
{
    [Test]
    public async Task CreateBearerHeaderValue_AppendsBearer()
    {
        var token = "XYZ";

        await Assert.That(WebUtilities.CreateBearerHeaderValue(token))
            .IsEqualTo($"Bearer {token}");
    }

    [Test]
    public async Task RedirectPreserveMethod_PopulatesResultAsExpected()
    {
        var result = WebUtilities.RedirectPreserveMethod("http://localhost:8080");

        await Assert.That(result.Permanent).IsFalse();
        await Assert.That(result.PreserveMethod).IsTrue();
    }

    [Test]
    public async Task CreateQueryUri_CreatesExpectedResult()
    {
        var baseUri = "http://localhost:8080/entity";
        var queryParams = new Dictionary<string, string> {
            { "a", "b" }
        };

        await Assert.That(WebUtilities.CreateQueryUri(baseUri, queryParams).ToString())
            .IsEqualTo($"{baseUri}?a=b");

        queryParams.Add("c", "d");

        await Assert.That(WebUtilities.CreateQueryUri(baseUri, queryParams).ToString())
            .IsEqualTo($"{baseUri}?a=b&c=d");

        queryParams.Add("e", "f");

        await Assert.That(WebUtilities.CreateQueryUri(baseUri, queryParams).ToString())
            .IsEqualTo($"{baseUri}?a=b&c=d&e=f");
    }

    [Test]
    public async Task GetRequestBody_WhereRequestContentLengthIsZero_ReturnsEmptyString()
    {
        Mock<HttpRequest> request = new();
        request.Setup(_ => _.ContentLength).Returns(0);

        var body = await WebUtilities.GetRequestBody(request.Object);

        await Assert.That(body).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task GetRequestBody_WhereRequestContentLengthThrows_ReturnsEmptyString()
    {
        Mock<HttpRequest> request = new();
        request.Setup(_ => _.ContentLength).Throws<InvalidOperationException>();

        var body = await WebUtilities.GetRequestBody(request.Object);

        await Assert.That(body).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task GetRequestBody_ReadsFromBodyStream()
    {
        string bodyContent = "0123456789";
        Mock<HttpRequest> request = new();

        request.Setup(_ => _.ContentLength).Returns(bodyContent.Length);
        request.Setup(_ => _.Body).Returns(new MemoryStream(Encoding.UTF8.GetBytes(bodyContent)));

        var body = await WebUtilities.GetRequestBody(request.Object);

        await Assert.That(body).IsEqualTo(bodyContent);
    }

    [Test]
    public async Task GetRequestFromBody_WhereBodyIsEmpty_ReturnsNewRequest()
    {
        Mock<HttpRequest> request = new();
        request.Setup(_ => _.ContentLength).Returns(0);

        var value = await WebUtilities.GetRequestFromBody<ExampleCommandRequest, ExampleCommandResponse>(request.Object);

        await Assert.That(value.Name).IsEqualTo(string.Empty);
        await Assert.That(value.Value).IsEqualTo(0);
    }

    [Test]
    public async Task GetRequestFromBody_WhereDeserializeReturnsNull_ReturnsNewRequest()
    {
        Mock<HttpRequest> request = new();
        request.Setup(_ => _.ContentLength).Returns(4);
        request.Setup(_ => _.Body).Returns(new MemoryStream(Encoding.UTF8.GetBytes("null")));

        var value = await WebUtilities.GetRequestFromBody<ExampleCommandRequest, ExampleCommandResponse>(request.Object);

        await Assert.That(value.Name).IsEqualTo(string.Empty);
        await Assert.That(value.Value).IsEqualTo(0);
    }

    [Test]
    public async Task GetRequestFromBody_WhereDeserializeReturnsValid_ReturnsDeserializedRequest()
    {
        Mock<HttpRequest> request = new();
        ExampleCommandRequest command = new()
        {
            Name = "the name",
            Value = 123
        };

        string body = JsonSerializer.Serialize(command, SerializationHelpers.CreateStandardSerializationOptions());

        request.Setup(_ => _.ContentLength).Returns(body.Length);
        request.Setup(_ => _.Body).Returns(new MemoryStream(Encoding.UTF8.GetBytes(body)));

        var value = await WebUtilities.GetRequestFromBody<ExampleCommandRequest, ExampleCommandResponse>(request.Object);

        await Assert.That(value.Name).IsEqualTo(command.Name);
        await Assert.That(value.Value).IsEqualTo(command.Value);
    }

    [Test]
    public async Task GetRequestFromQuery_ForGuidProperty_WhereValid_Works()
    {
        Guid property = Guid.NewGuid();
        Mock<HttpRequest> request = new();
        request.Setup(_ => _.Query).Returns(new QueryCollection(new Dictionary<string, StringValues>()
        {
            { nameof(ExampleQueryRequest.Guid), property.ToString() }
        }));

        var value = WebUtilities.GetRequestFromQuery<ExampleQueryRequest, ExampleQueryResponse>(request.Object);

        await Assert.That(value.Guid).IsEqualTo(property);
    }

    [Test]
    public async Task GetRequestFromQuery_ForGuidProperty_WhereInvalid_Throws()
    {
        Mock<HttpRequest> request = new();
        request.Setup(_ => _.Query).Returns(new QueryCollection(new Dictionary<string, StringValues>()
        {
            { nameof(ExampleQueryRequest.Guid), "INVALID_GUID_FORMAT" }
        }));

        await Assert.That(() =>
            WebUtilities.GetRequestFromQuery<ExampleQueryRequest, ExampleQueryResponse>(request.Object)
        ).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task GetRequestFromQuery_ForStringProperty_WhereValid_Works()
    {
        string property = "some-string-value";
        Mock<HttpRequest> request = new();
        request.Setup(_ => _.Query).Returns(new QueryCollection(new Dictionary<string, StringValues>()
        {
            { nameof(ExampleQueryRequest.String), property }
        }));

        var value = WebUtilities.GetRequestFromQuery<ExampleQueryRequest, ExampleQueryResponse>(request.Object);

        await Assert.That(value.String).IsEqualTo(property);
    }

    [Test]
    public async Task GetRequestFromQuery_ForLongProperty_WhereValid_Works()
    {
        long property = 12465;
        Mock<HttpRequest> request = new();
        request.Setup(_ => _.Query).Returns(new QueryCollection(new Dictionary<string, StringValues>()
        {
            { nameof(ExampleQueryRequest.Long), property.ToString() }
        }));

        var value = WebUtilities.GetRequestFromQuery<ExampleQueryRequest, ExampleQueryResponse>(request.Object);

        await Assert.That(value.Long).IsEqualTo(property);
    }

    [Test]
    public async Task GetRequestFromQuery_ForLongProperty_WhereInvalid_Throws()
    {
        Mock<HttpRequest> request = new();
        request.Setup(_ => _.Query).Returns(new QueryCollection(new Dictionary<string, StringValues>()
        {
            { nameof(ExampleQueryRequest.Long), "INVALID_LONG_FORMAT" }
        }));

        await Assert.That(() =>
            WebUtilities.GetRequestFromQuery<ExampleQueryRequest, ExampleQueryResponse>(request.Object)
        ).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task GetRequestFromQuery_ForIntProperty_WhereValid_Works()
    {
        int property = 12465;
        Mock<HttpRequest> request = new();
        request.Setup(_ => _.Query).Returns(new QueryCollection(new Dictionary<string, StringValues>()
        {
            { nameof(ExampleQueryRequest.Int), property.ToString() }
        }));

        var value = WebUtilities.GetRequestFromQuery<ExampleQueryRequest, ExampleQueryResponse>(request.Object);

        await Assert.That(value.Int).IsEqualTo(property);
    }

    [Test]
    public async Task GetRequestFromQuery_ForIntProperty_WhereInvalid_Throws()
    {
        Mock<HttpRequest> request = new();
        request.Setup(_ => _.Query).Returns(new QueryCollection(new Dictionary<string, StringValues>()
        {
            { nameof(ExampleQueryRequest.Int), "INVALID_INT_FORMAT" }
        }));

        await Assert.That(() =>
            WebUtilities.GetRequestFromQuery<ExampleQueryRequest, ExampleQueryResponse>(request.Object)
        ).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task GetRequestFromQuery_ForBoolProperty_WhereValid_Works()
    {
        bool property = true;
        Mock<HttpRequest> request = new();
        request.Setup(_ => _.Query).Returns(new QueryCollection(new Dictionary<string, StringValues>()
        {
            { nameof(ExampleQueryRequest.Bool), property.ToString() }
        }));

        var value = WebUtilities.GetRequestFromQuery<ExampleQueryRequest, ExampleQueryResponse>(request.Object);

        await Assert.That(value.Bool).IsEqualTo(property);
    }

    [Test]
    public async Task GetRequestFromQuery_ForBoolProperty_WhereInvalid_Throws()
    {
        Mock<HttpRequest> request = new();
        request.Setup(_ => _.Query).Returns(new QueryCollection(new Dictionary<string, StringValues>()
        {
            { nameof(ExampleQueryRequest.Bool), "INVALID_BOOL_FORMAT" }
        }));

        await Assert.That(() =>
            WebUtilities.GetRequestFromQuery<ExampleQueryRequest, ExampleQueryResponse>(request.Object)
        ).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task GetRequestFromQuery_ForDateOnlyProperty_WhereValid_Works()
    {
        DateOnly property = DateOnly.FromDateTime(DateTime.Today);
        Mock<HttpRequest> request = new();
        request.Setup(_ => _.Query).Returns(new QueryCollection(new Dictionary<string, StringValues>()
        {
            { nameof(ExampleQueryRequest.DateOnly), property.ToString() }
        }));

        var value = WebUtilities.GetRequestFromQuery<ExampleQueryRequest, ExampleQueryResponse>(request.Object);

        await Assert.That(value.DateOnly).IsEqualTo(property);
    }

    [Test]
    public async Task GetRequestFromQuery_ForDateOnlyProperty_WhereInvalid_Throws()
    {
        Mock<HttpRequest> request = new();
        request.Setup(_ => _.Query).Returns(new QueryCollection(new Dictionary<string, StringValues>()
        {
            { nameof(ExampleQueryRequest.DateOnly), "INVALID_DATE_ONLY_FORMAT" }
        }));

        await Assert.That(() =>
            WebUtilities.GetRequestFromQuery<ExampleQueryRequest, ExampleQueryResponse>(request.Object)
        ).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task GetRequestFromQuery_ForUnhandledProperty_Throws()
    {
        Mock<HttpRequest> request = new();
        request.Setup(_ => _.Query).Returns(new QueryCollection(new Dictionary<string, StringValues>()
        {
            { nameof(ExampleQueryRequest.Invalid), "INVALID_PROPERTY_TYPE" }
        }));

        await Assert.That(() =>
            WebUtilities.GetRequestFromQuery<ExampleQueryRequest, ExampleQueryResponse>(request.Object)
        ).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task GetRequestFromQuery_ForValidQuery_ReturnsValidRequest()
    {
        var queryRequest = new ExampleQueryRequest
        {
            String = "some-string-value",
            Guid = Guid.NewGuid(),
            Long = 123,
            Int = 512,
            DateOnly = DateOnly.FromDateTime(DateTime.Today),
        };

        Mock<HttpRequest> request = new();
        request.Setup(_ => _.Query).Returns(new QueryCollection(new Dictionary<string, StringValues>()
        {
            { nameof(ExampleQueryRequest.String), queryRequest.String },
            { nameof(ExampleQueryRequest.Guid), queryRequest.Guid.ToString() },
            { nameof(ExampleQueryRequest.Long), queryRequest.Long.ToString() },
            { nameof(ExampleQueryRequest.Int), queryRequest.Int.ToString() },
            { nameof(ExampleQueryRequest.DateOnly), queryRequest.DateOnly.ToString() }
        }));

        var value = WebUtilities.GetRequestFromQuery<ExampleQueryRequest, ExampleQueryResponse>(request.Object);

        await Assert.That(value.String).IsEqualTo(queryRequest.String);
        await Assert.That(value.Guid).IsEqualTo(queryRequest.Guid);
        await Assert.That(value.Long).IsEqualTo(queryRequest.Long);
        await Assert.That(value.Int).IsEqualTo(queryRequest.Int);
        await Assert.That(value.DateOnly).IsEqualTo(queryRequest.DateOnly);
    }
}
