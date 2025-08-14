using SentenceTransformers.MiniLM;
using temp.Service;
using temp.Service.Interface;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowGame", policy =>
    {
        policy
            .WithOrigins("*")
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddSingleton<IPromptMakerService, PromptMakerService>();
builder.Services.AddSingleton<IRetrieverService, RetrieverService>();
// Add this to your service configuration
builder.Services.AddSingleton<SentenceEncoder>(_ =>
    new SentenceEncoder());


builder.Services.AddHttpClient("AvalAi", client =>
{
    client.BaseAddress = new Uri("https://api.avalai.ir/v1/");
});

builder.WebHost.UseUrls(builder.Configuration["ASPNETCORE_URLS"] ?? "http://127.0.0.1:5000");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowGame");

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
