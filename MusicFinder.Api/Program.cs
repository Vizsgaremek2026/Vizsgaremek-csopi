using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// MySQL connection (MySqlConnector) – one transient connection per request scope
builder.Services.AddTransient<MySqlConnection>(_ =>
    new MySqlConnection(builder.Configuration.GetConnectionString("MusicFinderDb")));

// CORS – allow the Vite dev server and any localhost frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactDev", p =>
        p.WithOrigins("http://localhost:5173", "http://localhost:3000")
         .AllowAnyHeader()
         .AllowAnyMethod());
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("ReactDev");
app.MapControllers();

app.Run();
