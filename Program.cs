using MotionDataVisualization.IOFactory;
using MotionDataVisualization.DLModels;
using MotionDataVisualization.Models;
using Text2Motion.TorchTrainer;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("configs/gcn-spatial-temporal.json", optional: false, reloadOnChange: false);

builder.Services.AddSingleton<IIOFactory, NpyIOFactory>();
builder.Services.AddSingleton<ClipModel>();
builder.Services.Configure<Text2MotionSettings>(
    builder.Configuration.GetSection("Text2Motion"));
builder.Services.Configure<GcnSpatialTemporalConfig>(
    builder.Configuration.GetSection("GcnSpatialTemporalConfig"));
builder.Services.Configure<DatasetSettings>(
    builder.Configuration.GetSection("DatasetSettings"));
builder.Services.AddSingleton<Text2MotionModel>();
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

app.MapGet("/generate", () => Results.File(
    Path.Combine(app.Environment.WebRootPath, "generate.html"),
    "text/html"));

app.MapControllers();

app.Run();
