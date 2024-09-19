class Program
{
    private static string baseUrl = "http://localhost:8080/";
    private static int minWaitTimeMS = 0;
    private static int maxWaitTimeMS = 2000;

    static async Task Main(string[] args)
    {
        ClientSimulator simulator = new ClientSimulator(baseUrl);

        string? userInput = null;
        int numberOfClients = 0;

        while (userInput == null || !Int32.TryParse(userInput, out numberOfClients) || numberOfClients <= 0)
        {
            Console.WriteLine("Enter a positive number of clients to simulate");
            userInput = Console.ReadLine();
        }

        CancellationTokenSource source = new CancellationTokenSource();
        CancellationToken token = source.Token;

        simulator.Start(token, numberOfClients, minWaitTimeMS, maxWaitTimeMS);

        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            Console.WriteLine("Sigterm recieved. Program terminated, shutting down...");
            eventArgs.Cancel = true;

            simulator.Stop();
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
                    simulator.Stop();
                }
            }
        }
    }
}



