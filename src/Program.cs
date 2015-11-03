namespace EFI.FieryEventsSample
{
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Net.Security;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using WebSocket4Net;

    internal class Program
    {
        /// <summary>
        /// Set the host name as fiery server name or ip address
        /// </summary>
        private static string hostname = "the_server_name_or_ip_address";

        /// <summary>
        /// Set the key to access Fiery API
        /// </summary>
        private static string apiKey = @"the_api_key";

        /// <summary>
        /// Set the username to login to the fiery server
        /// </summary>
        private static string username = "the_username";

        /// <summary>
        /// Set the password to login to the fiery server
        /// </summary>
        private static string password = "the_password";

        /// <summary>
        /// The websocket object
        /// </summary>
        private static WebSocket ws;

        /// <summary>
        /// Event listener to be called when the wbsocket connection is closed
        /// </summary>
        private static void OnClose(object sender, EventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine();
            Console.WriteLine("websocket connection closed");
            Console.ResetColor();
        }

        /// <summary>
        /// Event listener to be called when a wbsocket error occurs
        /// </summary>
        private static void OnError(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine();
            Console.WriteLine(e.Exception.GetType() + ": " + e.Exception.Message);
            Console.ResetColor();
        }

        /// <summary>
        /// Event listener to be called when a message received from the server
        /// </summary>
        private static void OnMessage(object sender, MessageReceivedEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine();
            Console.WriteLine(e.Message);
            Console.ResetColor();
        }

        /// <summary>
        /// Event listener to be called when a new wbsocket connection is opened
        /// </summary>
        private static void OnOpen(object sender, EventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine();
            Console.WriteLine("new websocket connection is opened");
            Console.ResetColor();
        }

        /// <summary>
        /// Set filters to receive only fiery status change events
        /// </summary>
        private static void ReceiveFieryStatusChangeEvents()
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine();
            Console.WriteLine("Scenario: Receive only Fiery status change events");
            Console.WriteLine("Press <Enter> when you want to run next scenario");
            Console.ResetColor();

            // Ignore all events except device events
            ws.Send("{" +
                      "\"jsonrpc\": \"2.0\", " +
                      "\"method\": \"ignore\", " +
                      "\"params\": [\"accounting\", \"job\", \"jobprogress\", \"preset\", \"property\", \"queue\", \"recpreset\"], " +
                      "\"id\": 1" +
                    "}");
            // Receive device events
            ws.Send("{" +
                      "\"jsonrpc\": \"2.0\", " +
                      "\"method\": \"receive\", " +
                      "\"params\": \"device\", " +
                      "\"id\": 2" +
                    "}");
            Console.ReadLine();
        }

        /// <summary>
        /// Set filters to receive only job is printing? events
        /// </summary>
        private static void ReceiveJobIsPrintingEvents()
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine();
            Console.WriteLine("Scenario: Receive only job is printing? events");
            Console.WriteLine("Press <Enter> when you want to run next scenario");
            Console.ResetColor();

            // Ignore all events except job events
            ws.Send("{" +
                      "\"jsonrpc\": \"2.0\", " +
                      "\"method\": \"ignore\", " +
                      "\"params\": [\"accounting\", \"device\", \"jobprogress\", \"preset\", \"property\", \"queue\", \"recpreset\"], " +
                      "\"id\": 1" +
                    "}");

            // Receive job events
            ws.Send("{" +
                      "\"jsonrpc\": \"2.0\", " +
                      "\"method\": \"receive\", " +
                      "\"params\": \"job\", " +
                      "\"id\": 2" +
                    "}");
            // Receive job events only if they contain <is printing?> key in the <attributes> key
            ws.Send("{" +
                      "\"jsonrpc\": \"2.0\", " +
                      "\"method\": \"filter\", " +
                      "\"params\": {" +
                                    "\"eventKind\": \"job\", " +
                                    "\"mode\": \"add\", " +
                                    "\"attr\": {\"attributes\": \"is printing?\"}" +
                                  "}," +
                      "\"id\": 3" +
                    "}");
            Console.ReadLine();
        }

        /// <summary>
        /// Set filters in batch mode to receive only job is printing? events
        /// </summary>
        private static void ReceiveJobIsPrintingEventsBatchMode()
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine();
            Console.WriteLine("Scenario: Receive only job is is printing? events by sending messages in batch mode");
            Console.WriteLine("Press <Enter> when you want to run next scenario");
            Console.ResetColor();

            ws.Send("[" +
                // Ignore all events except job events
                      "{" +
                        "\"jsonrpc\": \"2.0\", " +
                        "\"method\": \"ignore\", " +
                        "\"params\": [\"accounting\", \"device\", \"jobprogress\", \"preset\", \"property\", \"queue\", \"recpreset\"], " +
                        "\"id\": 1" +
                      "}, " +

                      // Receive job events
                      "{" +
                        "\"jsonrpc\": \"2.0\", " +
                        "\"method\": \"receive\", " +
                        "\"params\": \"job\", " +
                        "\"id\": 2" +
                      "}, " +

                      // Receive job events only if they contain <is printing?> key in the <attributes> key
                      "{" +
                        "\"jsonrpc\": \"2.0\", " +
                        "\"method\": \"filter\", " +
                        "\"params\": {" +
                                      "\"eventKind\": \"job\", " +
                                      "\"mode\": \"add\", " +
                                      "\"attr\": {\"attributes\": \"is printing?\"}" +
                                    "}," +
                        "\"id\": 3" +
                      "}" +
                    "]");

            Console.ReadLine();
        }

        /// <summary>
        /// Open websocket connection, set event listeners and receive fiery events
        /// </summary>
        private static void RunWebsocket(string serverAddress, List<KeyValuePair<string, string>> customHeaders)
        {
            // Create websocket object
            ws = new WebSocket(serverAddress, customHeaderItems: customHeaders);

            // Set event listeners
            ws.Closed += new EventHandler(OnClose);
            ws.Error += new EventHandler<SuperSocket.ClientEngine.ErrorEventArgs>(OnError);
            ws.MessageReceived += new EventHandler<MessageReceivedEventArgs>(OnMessage);
            ws.Opened += new EventHandler(OnOpen);

            // Open websocket connection
            ws.Open();

            // Wait until the websocket connection is opened
            while (ws.State != WebSocketState.Open)
            { }

            ReceiveFieryStatusChangeEvents();
            ReceiveJobIsPrintingEvents();
            ReceiveJobIsPrintingEventsBatchMode();
        }

        /// <summary>
        /// Receive events from fiery server asynchronously
        /// </summary>
        /// <returns>The task object representing the asynchronous operation.</returns>
        private static async Task ReceiveFieryEventsAsync()
        {
            // Login to fiery server
            var handler = new HttpClientHandler();
            handler.CookieContainer = new CookieContainer();
            var client = await LoginAsync(handler);

            // Get the session cookie
            var cookie = handler.CookieContainer.GetCookieHeader(client.BaseAddress);

            // Add cookie to the custom header for websocket client
            var customeHeaders = new List<KeyValuePair<string, string>>();
            customeHeaders.Add(new KeyValuePair<string, string>("Cookie", cookie));

            // Set the websocket server address
            var serverAddress = string.Format(System.Globalization.CultureInfo.InvariantCulture, "wss://{0}/live/api/v2/events", hostname);

            // Establish websocket connection and receive fiery events
            RunWebsocket(serverAddress, customeHeaders);

            // Logout from fiery server and close websocket connection
            await LogoutAsync(client);
        }

        /// <summary>
        /// Login to the fiery server
        /// </summary>
        /// <param name="handler">The HTTP client handler to store session coockies.</param>
        /// <returns>The Task<HttpClient> object with session information.</returns>
        private static async Task<HttpClient> LoginAsync(HttpClientHandler handler)
        {
            var loginJson = new JObject();
            loginJson["username"] = username;
            loginJson["password"] = password;
            loginJson["accessrights"] = apiKey;

            var serverAddress = string.Format(System.Globalization.CultureInfo.InvariantCulture, "https://{0}/live/api/v2/", hostname);
            var client = new HttpClient(handler);
            client.BaseAddress = new Uri(serverAddress);

            var request = new StringContent(loginJson.ToString(), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("login", request);

            Console.WriteLine();
            Console.WriteLine("Login");
            Console.WriteLine(await response.Content.ReadAsStringAsync());
            return client;
        }

        /// <summary>
        /// Logout from the fiery server
        /// </summary>
        /// <param name="client">The HTTP client with valid session.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        private static async Task LogoutAsync(HttpClient client)
        {
            var response = await client.PostAsync("logout", null);

            Console.WriteLine();
            Console.WriteLine("Logout");
            Console.WriteLine(await response.Content.ReadAsStringAsync());
        }

        /// <summary>
        /// Ignore all certificates errors when sending request to the fiery server. Using this method without validation on production environment will increase the risk of MITM attack.
        /// </summary>
        /// <param name="sender">An object that contains state information for this validation.</param>
        /// <param name="certificate">The certificate used to authenticate the remote party.</param>
        /// <param name="chain">The chain of certificate authorities associated with the remote certificate.</param>
        /// <param name="sslPolicyErrors">One or more errors associated with the remote certificate.</param>
        /// <returns>A value that determines whether the specified certificate is accepted for authentication.</returns>
        private static bool ValidateCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        /// <summary>
        /// Entry point of the application
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        static void Main(string[] args)
        {
            System.Net.ServicePointManager.ServerCertificateValidationCallback = ValidateCertificate;
            ReceiveFieryEventsAsync().Wait();
        }
    }
}
