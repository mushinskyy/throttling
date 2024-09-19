using System.Net;

public interface IHttpServer
{
    public void Start(CancellationToken token) {}
    public void Stop() {}
}

public class HttpServer : IHttpServer
{
    private volatile bool isServerRunning = false;
    private CancellationToken token;
    private string prefix;

    private HttpListener? httpListener;

    private ThrottlingMiddleware throttlingMiddleware;

    public HttpServer(string prefix)
    {
        this.prefix = prefix;
    }

    public void Start(CancellationToken token)
    {
        if (isServerRunning) throw new InvalidOperationException("Server is already started");

        this.token = token;

        Task.Run(() => InternalStart());
    }

    private async Task InternalStart()
    {
        try 
        {
            httpListener = new HttpListener();
            throttlingMiddleware = new ThrottlingMiddleware(token, 5000, 5);
            httpListener.Prefixes.Add(prefix);
            httpListener.Start();
            isServerRunning = true;

            Console.WriteLine($"Server started. Listening on {prefix}. \nPress Enter to stop the server. ");

            while (isServerRunning && !token.IsCancellationRequested)
            {
                HttpListenerContext context = await httpListener.GetContextAsync();
                Task handleRequest = Task.Run(() => HandleRequest(context));
            }
        }
        catch (HttpListenerException) when (token.IsCancellationRequested) 
        { 

        }
        catch (HttpListenerException exception)
        {
            Console.WriteLine($"Error: {exception.Message}");
        }
        finally
        {
            Stop();
        }
    }

    private async Task HandleRequest(HttpListenerContext context)
    {
        if (context != null)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            string clientId = "";
            if (context.Request.QueryString != null) 
            {
                clientId = context.Request.QueryString["clientId"];
            }
            else
            {
                RespondWithStatusCode(response, HttpStatusCode.ServiceUnavailable);
            }

            if (!String.IsNullOrEmpty(clientId) && throttlingMiddleware.CheckAndIncrease(clientId))
            {
                RespondWithStatusCode(response, HttpStatusCode.OK);
            }
            else
            {
                RespondWithStatusCode(response, HttpStatusCode.ServiceUnavailable);
            }


            Console.WriteLine($"Responded to {clientId} with {response.StatusCode}. ");
        }
    }

    private static void RespondWithStatusCode(HttpListenerResponse response, HttpStatusCode statusCode)
    {
        response.StatusCode = (int)statusCode;
        response.Close();
    }

    public void Stop()
    {
        httpListener?.Stop();
        isServerRunning = false;
    }
}