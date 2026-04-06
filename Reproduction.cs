using System.Reflection;
using System.Runtime.Loader;
using Alba;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Wolverine;
using Wolverine.Http;

namespace Nunit.AssemblyBug;

public class Reproduction
{
    // This is the minimal reproduction of the bug. Fails with TestAdapter v6+ when running in Resharper,
    // but not in `dotnet test`. Runs in both when using TestAdaper v5.2
    [Test]
    public void TestAssemblyLoad()
    {
        var assembly = AssemblyLoadContext.Default.LoadFromAssemblyName(new("Nunit.AssemblyBug"));
        var type = assembly.GetType("Nunit.AssemblyBug.Holder");
        Assert.That(type, Is.EqualTo(typeof(Holder)));
    }




    // the tests below are explortions that brought me to the minimal test


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

    [Test]
    public async Task TestWolverineViaBus()
    {
        var response = await _host.Scenario(s =>
        {
            s.Get.Url("/wolverine-bus");
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
        builder.Host.UseWolverine();
        builder.Services.AddWolverineHttp();

        _host = await AlbaHost.For(builder, app =>
        {
            app.MapWolverineEndpoints();
            app.MapGet("/minimal", () => Results.Ok(Holder.TestValue));
            app.MapGet("wolverine-bus",
                (IMessageBus bus) => bus.InvokeAsync<Guid>(new WolverineClassicHandler.Request()));
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


public class WolverineHttpEndpoint
{
    [WolverineGet("/wolverine")]
    public IResult Handle()
    {
        return Results.Ok(Holder.TestValue);
    }
}

public class WolverineClassicHandler
{
    public record Request;

    public Guid Handle(Request request)
    {
        return Holder.TestValue;
    }
}