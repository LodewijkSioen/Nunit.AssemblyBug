using Alba;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Wolverine;
using Wolverine.Http;

namespace Nunit.AssemblyBug;

public class Reproduction
{
    private IAlbaHost _host = null!;


    [Test]
    public async Task TestWolverine()
    {
        var response = await _host.Scenario(s =>
        {
            s.Get.Url("/wolverine");
            s.StatusCodeShouldBe(200);
        });
        var result = await response.ReadAsTextAsync();

        using var scope = Assert.EnterMultipleScope();
        Assert.That(result, Is.EqualTo($"\"{Holder.TestValue.ToString()}\""));
    }

    [Test]
    public async Task TestMinimal()
    {
        var response = await _host.Scenario(s =>
        {
            s.Get.Url("/minimal");
            s.StatusCodeShouldBe(200);
        });
        var result = await response.ReadAsTextAsync();

        using var scope = Assert.EnterMultipleScope();
        Assert.That(result, Is.EqualTo($"\"{Holder.TestValue.ToString()}\""));
    }

    [SetUp]
    public async Task Setup()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Host.UseWolverine(opts =>
        {
            opts.Discovery.DisableConventionalDiscovery();
            opts.Discovery.IncludeAssembly(GetType().Assembly);
        });
        builder.Services.AddWolverineHttp();

        _host = await AlbaHost.For(builder, app =>
        {
            app.MapWolverineEndpoints();
            app.MapGet("/minimal", () => Results.Ok(Holder.TestValue));
        });
    }

    [TearDown]
    public void TearDown()
    {
        _host.Dispose();
    }
}

public static class Holder
{
    public static Guid TestValue = Guid.NewGuid();
}

public class ValidationEndpoint
{

    [WolverineGet("/wolverine")]
    public IResult Handle()
    {
        return Results.Ok(Holder.TestValue);
    }
}