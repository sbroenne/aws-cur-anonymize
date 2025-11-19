using AwsCurAnonymize.Cli;
using Spectre.Console;
using Spectre.Console.Cli;

var app = new CommandApp<RunCommand>();

app.Configure(config =>
{
    config.SetApplicationName("aws-cur-anonymize");
    config.SetApplicationVersion("1.0.0");

    config.ValidateExamples();
});

try
{
    return await app.RunAsync(args);
}
catch (Exception ex)
{
    AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
    if (ex.InnerException != null)
    {
        AnsiConsole.MarkupLine($"[dim]{ex.InnerException.Message}[/]");
    }
    return 1;
}
