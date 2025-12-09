namespace mark.davison.common.tests.CQRS;

public sealed class ResponseTests
{
    [Test]
    public async Task BaseResponse_WhereWarnings_IsSuccess()
    {
        var response = new Response
        {
            Warnings = ["Warn"]
        };

        await Assert.That(response.Success).IsTrue();
    }

    [Test]
    public async Task BaseResponse_WhereErrors_IsNotSuccess()
    {
        var response = new Response
        {
            Errors = ["Error"]
        };

        await Assert.That(response.Success).IsFalse();
    }

    [Test]
    public async Task Response_WhereWarningsAndValue_IsSuccess()
    {
        var response = new Response<object>
        {
            Value = new(),
            Warnings = ["Warn"]
        };

        await Assert.That(response.Success).IsTrue();
        await Assert.That(response.SuccessWithValue).IsTrue();
    }

    [Test]
    public async Task Response_WhereWarningsAndNoValue_IsNotSuccess()
    {
        var response = new Response<object>
        {
            Value = null,
            Warnings = ["Warn"]
        };

        await Assert.That(response.Success).IsTrue();
        await Assert.That(response.SuccessWithValue).IsFalse();
    }

    [Test]
    public async Task Response_WhereErrorsAndValue_IsNotSuccess()
    {
        var response = new Response<object>
        {
            Value = new(),
            Errors = ["Error"]
        };

        await Assert.That(response.Success).IsFalse();
        await Assert.That(response.SuccessWithValue).IsFalse();
    }

    [Test]
    public async Task Response_WhereErrorsAndNoValue_IsNotSuccess()
    {
        var response = new Response<object>
        {
            Value = null,
            Errors = ["Error"]
        };

        await Assert.That(response.Success).IsFalse();
        await Assert.That(response.SuccessWithValue).IsFalse();
    }
}