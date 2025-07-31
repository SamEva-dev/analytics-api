using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.MapGet("/", () => "API Analytics OK");
app.MapGet("/api/visits", () =>
{
    var dir = Path.GetDirectoryName("/app/data/analytics.db");
    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

    using var db = new SqliteConnection("Data Source=/app/data/analytics.db");
    db.Open();
    var cmd = db.CreateCommand();
    cmd.CommandText = "SELECT Id, Ip, UserAgent, Date FROM Visits ORDER BY Date DESC LIMIT 100";
    using var reader = cmd.ExecuteReader();
    var visits = new List<object>();
    while (reader.Read())
    {
        visits.Add(new
        {
            Id = reader.GetInt32(0),
            Ip = reader.GetString(1),
            UserAgent = reader.GetString(2),
            Date = reader.GetString(3)
        });
    }
    return visits;
});

app.MapPost("/api/track", async (HttpContext context) =>
{
    var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    var userAgent = context.Request.Headers["User-Agent"].ToString();
    var dir = Path.GetDirectoryName("/app/data/analytics.db");
    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);


    using var db = new SqliteConnection("Data Source=/app/data/analytics.db");
    db.Open();
    var cmd = db.CreateCommand();
    cmd.CommandText = @"CREATE TABLE IF NOT EXISTS Visits (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        Ip TEXT, UserAgent TEXT, Date TEXT
    )";
    cmd.ExecuteNonQuery();

    var insert = db.CreateCommand();
    insert.CommandText = @"INSERT INTO Visits (Ip, UserAgent, Date)
                           VALUES ($ip, $ua, $date)";
    insert.Parameters.AddWithValue("$ip", ip);
    insert.Parameters.AddWithValue("$ua", userAgent);
    insert.Parameters.AddWithValue("$date", DateTime.UtcNow);
    insert.ExecuteNonQuery();

    return Results.Ok("Visit logged");
});


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
