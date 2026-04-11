using motion_data_visualization.ModelLoader;
using MotionDataVisualization.IOFactory;
using MotionDataVisualization.DLModels;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IIOFactory, NpyIOFactory>();
builder.Services.AddSingleton<IModelLoader, ModelLoader>();
builder.Services.AddSingleton<CLIPModel>();
builder.Services.AddControllers();

WebApplication app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/", () => Results.Redirect("/homepage"));
app.MapGet("/homepage", () => Results.File(
    Path.Combine(app.Environment.WebRootPath, "index.html"),
    "text/html"));

app.MapGet("/embeddings", () => Results.File(
    Path.Combine(app.Environment.WebRootPath, "embeddings.html"),
    "text/html"));

app.MapControllers();

app.Run();
