using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    // Configuration - adjust these to match your server
    private const string ServerIp = "127.0.0.1";
    private const int ServerPort = 8080;

    static async Task Main(string[] args)
    {
        Console.WriteLine("Logging Service Test Client");
        Console.WriteLine("Choose mode: (1) Manual Test, (2) Automated Test, (3) Abuse Prevention Test");
        var mode = Console.ReadKey();
        Console.WriteLine();

        switch (mode.KeyChar)
        {
            case '1':
                await ManualTest();
                break;
            case '2':
                await AutomatedTest();
                break;
            case '3':
                AbusePreventionTest();
                break;
            default:
                Console.WriteLine("Invalid option.");
                break;
        }
    }

    private static async Task ManualTest()
    {
        Console.WriteLine("Enter log level (INFO, WARNING, ERROR):");
        string logLevel = Console.ReadLine();

        Console.WriteLine("Enter client ID:");
        string clientId = Console.ReadLine();

        Console.WriteLine("Enter message:");
        string message = Console.ReadLine();

        await SendLogMessage(logLevel, clientId, message);
    }

    private static async Task AutomatedTest()
    {
        string[] logLevels = { "INFO", "WARNING", "ERROR" };
        foreach (var logLevel in logLevels)
        {
            await SendLogMessage(logLevel, "AutomatedTest", $"This is a test message for {logLevel}.");
        }
    }

    private static void AbusePreventionTest()
    {
        // Simulate rapid requests to test rate limiting
        Parallel.For(0, 200, async i =>
        {
            string logLevel = "INFO";
            string clientId = "AbuseTest";
            string message = $"Message {i}";
            await SendLogMessage(logLevel, clientId, message);
        });
    }

    private static async Task SendLogMessage(string logLevel, string clientId, string message)
    {
        try
        {
            using (TcpClient client = new TcpClient(ServerIp, ServerPort))
            using (NetworkStream stream = client.GetStream())
            {
                string formattedMessage = $"{DateTime.UtcNow}|{clientId}|{logLevel}|{message}";
                byte[] data = Encoding.ASCII.GetBytes(formattedMessage);

                await stream.WriteAsync(data, 0, data.Length);
                Console.WriteLine($"Sent: {formattedMessage}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending message: {ex.Message}");
        }
    }
}
