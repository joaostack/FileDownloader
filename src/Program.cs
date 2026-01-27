/*                                                                                          
███████╗██╗██╗     ███████╗██████╗  ██████╗ ██╗    ██╗███╗   ██╗██╗      ██████╗  █████╗ ██████╗ ███████╗██████╗  ██╗
██╔════╝██║██║     ██╔════╝██╔══██╗██╔═══██╗██║    ██║████╗  ██║██║     ██╔═══██╗██╔══██╗██╔══██╗██╔════╝██╔══██╗███║
█████╗  ██║██║     █████╗  ██║  ██║██║   ██║██║ █╗ ██║██╔██╗ ██║██║     ██║   ██║███████║██║  ██║█████╗  ██████╔╝╚██║
██╔══╝  ██║██║     ██╔══╝  ██║  ██║██║   ██║██║███╗██║██║╚██╗██║██║     ██║   ██║██╔══██║██║  ██║██╔══╝  ██╔══██╗ ██║
██║     ██║███████╗███████╗██████╔╝╚██████╔╝╚███╔███╔╝██║ ╚████║███████╗╚██████╔╝██║  ██║██████╔╝███████╗██║  ██║ ██║
╚═╝     ╚═╝╚══════╝╚══════╝╚═════╝  ╚═════╝  ╚══╝╚══╝ ╚═╝  ╚═══╝╚══════╝ ╚═════╝ ╚═╝  ╚═╝╚═════╝ ╚══════╝╚═╝  ╚═╝ ╚═╝
https://github.com/joaostack
this is a experimental project for educational purposes
*/

using System.Collections.Frozen;
using System.ComponentModel;
using System.Net;
using Spectre.Console;
using Spectre.Console.Cli;

namespace FileDownloader1;

class Program
{
    static async Task Main(string[] args)
    {
        AnsiConsole.Write(new FigletText("FileDownloader").Color(Color.Purple));
        var app = new CommandApp();
        app.Configure(config => { config.AddCommand<DownloaderCommand>("download"); });
        await app.RunAsync(args);
    }
}

/// <summary>
/// The DownloaderCommand class is a downloader command base 
/// </summary>
internal class DownloaderCommand : AsyncCommand<DownloaderCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandOption("--output|-o")]
        [Description("The output of downloaded file")]
        public string Output { get; set; }

        [CommandOption("--url|-u")]
        [Description("The URL of the file to download")]
        public required string Url { get; set; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        await AnsiConsole.Progress()
            .AutoClear(false)
            .Columns(new ProgressColumn[]
            {
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn()
            })
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask($"[blue][[*]][/] [yellow]Downloading...[/]");

                if (string.IsNullOrEmpty(settings.Output))
                {
                    var fileName = settings.Url.Split('/').Last();
                    settings.Output = fileName;
                    await Downloader.DownloadAsync(fileName, settings.Url, task);
                }
                else
                {
                    await Downloader.DownloadAsync(settings.Output, settings.Url, task);
                }
            });

        AnsiConsole.MarkupLine($"[green][[OK]][/] [white]Download completed, saved has '{settings.Output}'.[/]");
        
        return 0;
    }
}

/// <summary>
/// Downloader class implements basic methods for file downloading
/// </summary>
public class Downloader
{
    private static readonly HttpClient _httpClient = new HttpClient();
    
    public static async Task DownloadAsync(string output, string url, ProgressTask task)
    {
        try
        {
            var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            task.MaxValue = response.Content.Headers.ContentLength ?? 0;
            await using var fs = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite);
            await using  var stream = await response.Content.ReadAsStreamAsync();
            var buffer = new byte[128000]; // 128kb
            int readBytes = 0;
            while ((readBytes = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fs.WriteAsync(buffer, 0, readBytes);
                task.Increment(readBytes);
                task.Description = $"[blue][[*]][/] [yellow]{fs.Length} bytes downloaded - Remaining Time {task.RemainingTime.Value.Seconds} seconds...[/]";
            }
        }
        catch (Exception e)
        {
            AnsiConsole.WriteException(e);
        }
    }
}