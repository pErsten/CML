namespace ApiServer.Controllers;

public static class HomeController //: IController
{
    public static IEndpointRouteBuilder UserHomeController(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("Home");

        group.MapGet("/", GetData);

        return builder;
    }

    public async static void GetData()
    {

    }
}