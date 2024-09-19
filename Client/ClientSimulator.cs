using System.Net.Mail;
using System.Reflection.Metadata.Ecma335;

public class ClientSimulator
{
    static readonly HttpClient client = new HttpClient();
    private volatile bool isClientSimulatorRunning = false;

    private string baseUrl;
    Queue<string> clientIdsQueue;

    public ClientSimulator(string baseUrl)
    {
        this.baseUrl = baseUrl;
        clientIdsQueue = new Queue<string>();
    }

    public async void Start(CancellationToken token, int numberOfClients, int minWaitTime, int maxWaitTime)
    {
        if (numberOfClients <= 0) throw new ArgumentException("Number of clients must be greater than 0. ");
        if (Math.Min(minWaitTime, maxWaitTime) < 0) throw new ArgumentException("Minimum and maximum wait times must be greater or equal to 0. ");
        if (isClientSimulatorRunning) throw new InvalidOperationException("Client simulator is already started");

        isClientSimulatorRunning = true;

        Console.WriteLine($"Simulating {numberOfClients} clients. ");
        var clientIds = Enumerable.Range(0, numberOfClients).Select(clientId => $"ClientId{clientId}").ToList();
        clientIdsQueue = new Queue<string>(clientIds);
        var tasks = Enumerable.Range(0, numberOfClients).
            Select(clientNumber => Task.Run(() => SimulateClient(token, clientNumber, minWaitTime, maxWaitTime))).ToList();

        await Task.WhenAll(tasks);
    }

    private async void SimulateClient(CancellationToken token, int clientNumber, int minWaitTimeMS, int maxWaitTimeMS)
    {
        var clientId = GetNextClientId();
        
        Console.WriteLine($"Task {clientNumber} simulating {clientId}. ");

        while (isClientSimulatorRunning && !token.IsCancellationRequested)
        {
            var targetUrl = $"{baseUrl}?clientId={clientId}";

            try 
            {
                var response = await client.GetAsync(targetUrl);
                Console.WriteLine($"Task {clientNumber} for target URL {targetUrl} received response with status code {response.StatusCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Task {clientNumber} for target URL {targetUrl} received an error while trying to query {targetUrl}: {ex.Message}");
            }

            int waitTimeMS = GetRandom(minWaitTimeMS, maxWaitTimeMS);
            token.WaitHandle.WaitOne(waitTimeMS);
        }
    }

    private object GetNextClientId()
    {
        var nextClientId = clientIdsQueue.Dequeue();
        clientIdsQueue.Enqueue(nextClientId);
        return nextClientId;
    }

    public void Stop()
    {
        isClientSimulatorRunning = false;
    }

    private int GetRandom(int minValue, int maxValue) => (int)(new Random().NextDouble() * (maxValue - minValue)) + minValue;
}