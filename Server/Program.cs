using System.Reflection.Metadata;

class Program
{
    private const string PREFIX = "http://localhost:8080/";
    static async Task Main(string[] args)
    {
        CancellationTokenSource source = new CancellationTokenSource();
        CancellationToken token = source.Token;

        Console.WriteLine("Server starting... ");

        IHttpServer server = new HttpServer(PREFIX);
        server.Start(token);

        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            Console.WriteLine("Sigterm recieved. Program terminated, shutting down...");
            eventArgs.Cancel = true;

            server.Stop();
            source.Cancel();
        };

        while (!token.IsCancellationRequested) 
        {
            if (!Console.IsInputRedirected && Console.KeyAvailable)
            {
                ConsoleKeyInfo readKey = Console.ReadKey();
                if (readKey.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine("Enter key press recieved. Program terminated, shutting down...");
                    source.Cancel();
                    server.Stop();
                }
            }
        }
    }
}