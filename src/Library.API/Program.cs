namespace Library.API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateSlimBuilder(args);

        builder.Services.AddOpenApi();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.Run();
    }
}
