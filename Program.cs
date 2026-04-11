using MotionDataVisualization.IOFactory;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IIOFactory, NpyIOFactory>();
builder.Services.AddControllers();

WebApplication app = builder.Build();

app.MapControllers();

app.Run();
