using System;
using System.Collections.Generic;
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
    public const string automaticTestFilePath = @"..\..\..\TestFiles\automaticTextFile.txt";
    public const int maxLogLevel = 7;
    public static bool sendMisformattedMessage = false;

    static async Task Main(string[] args)
    {
        
        Console.WriteLine("Logging Service Test Client");
        Console.WriteLine("Choose mode: (1)Manual Test, (2) Automated Test, (3) Abuse Prevention Test (4) Help (5) Exit");
        var mode = Console.ReadKey();
        Console.WriteLine(); 

        switch(mode.KeyChar)
        {
            case '1':
                await ManualTest(); 
                break;

            case '2':
                await AutomatedTest();
                break;

        }

    }

    public static string GenerateUniqueID()
    {
        // Use a shorter date-time format
        string timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        // Get the first 8 characters of a GUID
        string shortGuid = Guid.NewGuid().ToString().Substring(0, 8);
        return $"{timestamp}-{shortGuid}";
    }

    private static async Task ManualTest()
    {
        Console.WriteLine("Enter log levels: ALL, INFO, DEBUG, WARNING, ERROR, FATAL, TRACE:");
        string logLevel = Console.ReadLine();

        Console.WriteLine("Enter the client ID: ");
        string clientID = Console.ReadLine();

        Console.WriteLine("Enter Message: ");
        string message = Console.ReadLine();

        await SendLogMessage(logLevel, clientID, message);  
    }

    private static async Task SendLogMessage(string loglevel, string clientID, string message)
    {
        try
        {
            using(TcpClient client = new TcpClient(ServerIp, ServerPort))
            using(NetworkStream stream = client.GetStream())
            {
                if(sendMisformattedMessage == true)
                {
                    string misformattedMessage = message; 
                    byte[] misformattedData = Encoding.ASCII.GetBytes(misformattedMessage);
                    await stream.WriteAsync(misformattedData, 0, misformattedData.Length);

                }
                string timestamp = DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm:ss");
                string formattedMessage = $"{timestamp} | {clientID} | {loglevel} | {message}";
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

    private static async Task SendMisformattedMessage(string message)
    {
        try
        {
            using (TcpClient client = new TcpClient(ServerIp, ServerPort))
            using (NetworkStream stream = client.GetStream())
            {      
                    string misformattedMessage = message;
                    byte[] misformattedData = Encoding.ASCII.GetBytes(misformattedMessage);
                    await stream.WriteAsync(misformattedData, 0, misformattedData.Length);
                    Console.WriteLine($"Sent: {misformattedMessage}");
            }
        }


        catch (Exception ex)
        {
            Console.WriteLine($"Error sending message: {ex.Message}");
        }
    }

    private static async Task AutomatedTest()
    {
        //In automated test we gonna send the test of all the logLevels through a file reading it line by line and sending it 
        //At first read the file from the directory
        string clientID = GenerateUniqueID();
        List<string> logLevels = new List<string> { "ALL", "DEBUG", "INFO", "WARNING", "ERROR", "FATAL", "TRACE" };

        if (File.Exists(automaticTestFilePath))
        {
            using (StreamReader reader = File.OpenText(automaticTestFilePath))
            {
                string line;
                Console.WriteLine("Sending a log message with different level of logs:\n");
                for (int i = 0; i < maxLogLevel; i++)
                {
                    line = reader.ReadLine();
                    Console.WriteLine("Sending a valid log with the level: " + i.ToString());
                    Console.WriteLine("Log LEVEL: " + logLevels[i].ToString());
                    await SendLogMessage(logLevels[i], clientID, line);
                    
                }

                Thread.Sleep(5000);
                //Now lets try with the individual ones
                //Lets send the invalid LOG LEVELS at first
                string invalidLogLevel = "MAIN";
                await SendLogMessage(invalidLogLevel, clientID, "This is invalid check of the LOG_LEVEL");
                Thread.Sleep(5000);

                Console.WriteLine("Now checking with the invalid client ID: ");
                string invalidClientID = ""; 
                await SendLogMessage("WARNING", invalidClientID, "This is a test for invalid client ID Passed!!");
                Thread.Sleep(5000);

                //Now checking with the misformaatted message
                await SendMisformattedMessage("THis is a misformmmated message");

            }
        }

        else
        {
            Console.WriteLine("Automatic Test File is not found!!!");
        }
    }
}
