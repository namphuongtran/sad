using Quiz.Api.Configurations;
using Quiz.Api.Repositories;
using Quiz.Api.Services;
using Quiz.Api.Workers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<KafkaConfiguration>(builder.Configuration.GetSection(nameof(KafkaConfiguration)));
builder.Services.Configure<RedisConfiguration>(builder.Configuration.GetSection(nameof(RedisConfiguration)));
builder.Services.AddSingleton<GameScoreService>();
builder.Services.AddTransient<IGameScoreRepository,GameScoreRepository>();
builder.Services.AddSingleton<WebSocketServer>();
builder.Services.AddSingleton<LeaderBoardService>();
builder.Services.AddHostedService<GameScorePgConsumer>();
builder.Services.AddHostedService<GameScoreRedisConsumer>();
builder.Services.AddHostedService<LeaderBoardConsumer>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("_myAllowSpecificOrigins",
                      policy =>
                      {
                          policy.WithOrigins("http://localhost:3000");
                      });
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();
var webSocketOptions = new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromMinutes(2)
};
app.UseWebSockets(webSocketOptions);
app.UseCors("_myAllowSpecificOrigins");


app.Use(async (context, next) =>
{
    if (context.Request.Path == "/online-game")
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            var webSocketServer = context.RequestServices.GetRequiredService<WebSocketServer>();
            await webSocketServer.HandleWebSocketAsync(webSocket);
        }
        else
        {
            context.Response.StatusCode = 400;
        }
    }
    else
    {
        await next();
    }
});
app.Run();
