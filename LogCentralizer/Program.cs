using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Optionnel : limiter la taille des requêtes si tu veux
// builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 10 * 1024 * 1024);

var app = builder.Build();

// Dossier interne dans le conteneur
Directory.CreateDirectory("logs");

// Lock pour éviter que 2 requêtes écrivent en même temps dans le fichier
var fileLock = new object();

app.MapGet("/", () => Results.Ok("LogCentralizer running ✅"));

app.MapPost("/logs", async (HttpRequest req) =>
{
    // On lit le body brut (JSON envoyé par EasySave)
    using var reader = new StreamReader(req.Body, Encoding.UTF8);
    var body = await reader.ReadToEndAsync();

    if (string.IsNullOrWhiteSpace(body))
        return Results.BadRequest("Empty body");

    // 1 seul fichier journalier
    var day = DateTime.UtcNow.ToString("yyyy-MM-dd");
    var path = Path.Combine("logs", $"{day}.jsonl"); // JSON Lines : 1 JSON par ligne

    lock (fileLock)
    {
        File.AppendAllText(path, body + Environment.NewLine, Encoding.UTF8);
    }

    return Results.Ok();
});

app.Run("http://0.0.0.0:5080");