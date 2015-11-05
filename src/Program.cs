namespace EFI.FieryEventsSample
{
    using Newtonsoft.Json;
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
        /// The mutex to signal websocket open, close or error events
        /// </summary>
        private static AutoResetEvent mutex = new AutoResetEvent(false);

        /// <summary>
        /// The websocket object
        /// </summary>
        private static WebSocket ws;

        /// <summary>
        /// Event listener to be called when the wbsocket connection is closed
        /// </summary>
        /// <param name="sender">The wbsocket object.</param>
        /// <param name="eventArgs">The arguments of the close event.</param>
        private static void OnClose(object sender, EventArgs eventArgs)
        {
            mutex.Set();

            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine();
            Console.WriteLine("websocket connection closed");
            Console.ResetColor();
        }

        /// <summary>
        /// Event listener to be called when a wbsocket error occurs
        /// </summary>
        /// <param name="sender">The wbsocket object.</param>
        /// <param name="eventArgs">The arguments of the error event.</param>
        private static void OnError(object sender, SuperSocket.ClientEngine.ErrorEventArgs eventArgs)
        {
            mutex.Set();

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine();
            Console.WriteLine(eventArgs.Exception.GetType() + ": " + eventArgs.Exception.Message);
            Console.ResetColor();
        }

        /// <summary>
        /// Event listener to be called when a message received from the server
        /// </summary>
        /// <param name="sender">The wbsocket object.</param>
        /// <param name="eventArgs">The arguments of the message received event.</param>
        private static void OnMessage(object sender, MessageReceivedEventArgs eventArgs)
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine();
            Console.WriteLine(eventArgs.Message);
            Console.ResetColor();
        }

        /// <summary>
        /// Event listener to be called when a new wbsocket connection is opened
        /// </summary>
        /// <param name="sender">The wbsocket object.</param>
        /// <param name="eventArgs">The arguments of the open event.</param>
        private static void OnOpen(object sender, EventArgs eventArgs)
        {
            mutex.Set();

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
            var ignoreAllEventsExceptDevice = new
            {
                jsonrpc = "2.0",
                method = "ignore",
                @params = new[] { "accounting", "job", "jobprogress", "preset", "property", "queue" },
                id = 1
            };
            ws.Send(JsonConvert.SerializeObject(ignoreAllEventsExceptDevice));

            // Receive device events
            var receiveDeviceEvents = new
            {
                jsonrpc = "2.0",
                method = "receive",
                @params = new[] { "device" },
                id = 2
            };
            ws.Send(JsonConvert.SerializeObject(receiveDeviceEvents));

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
            var ignoreAllEventsExceptJob = new
            {
                jsonrpc = "2.0",
                method = "ignore",
                @params = new[] { "accounting", "device", "jobprogress", "preset", "property", "queue" },
                id = 1
            };
            ws.Send(JsonConvert.SerializeObject(ignoreAllEventsExceptJob));

            // Receive job events
            var receiveJobEvents = new
            {
                jsonrpc = "2.0",
                method = "receive",
                @params = new[] { "job" },
                id = 2
            };
            ws.Send(JsonConvert.SerializeObject(receiveJobEvents));

            // Receive job events only if they contain <is printing?> key in the <attributes> key
            var receiveIsPrintingEvents = new
            {
                jsonrpc = "2.0",
                method = "filter",
                @params = new
                {
                    eventKind = "job",
                    mode = "add",
                    attr = new
                    {
                        attributes = new[] {"is printing?"}
                    }
                },
                id = 3
            };
            ws.Send(JsonConvert.SerializeObject(receiveIsPrintingEvents));

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

            var commandsInBatchMode = new object[] {
                // Ignore all events except job events
                new {
                    jsonrpc = "2.0",
                    method = "ignore",
                    @params = new[] { "accounting", "device", "jobprogress", "preset", "property", "queue" },
                    id = 1
                },

                // Receive job events
                new {
                    jsonrpc = "2.0",
                    method = "receive",
                    @params = new[] { "job" },
                    id = 2
                },

                // Receive job events only if they contain <is printing?> key in the <attributes> key
                new {
                    jsonrpc = "2.0",
                    method = "filter",
                    @params = new {
                        eventKind = "job",
                        mode = "add",
                        attr = new {
                            attributes = new[] {"is printing?"}
                        }
                    },
                    id = 3
                }
            };

            ws.Send(JsonConvert.SerializeObject(commandsInBatchMode));

            Console.ReadLine();
        }

        /// <summary>
        /// Open websocket connection, set event listeners and receive fiery events
        /// </summary>
        /// <param name="serverAddress">The address of the wbsocket server.</param>
        /// <param name="customHeaders">The custom headers to be passed to the websocket server.</param>
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
            if (!mutex.WaitOne(TimeSpan.FromSeconds(15)))
            {
                return;
            }

            if (ws.State == WebSocketState.Open)
            {
                ReceiveFieryStatusChangeEvents();
                ReceiveJobIsPrintingEvents();
                ReceiveJobIsPrintingEventsBatchMode();
            }
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
            var serverAddress = string.Format(System.Globalization.CultureInfo.InvariantCulture, "https://{0}/live/api/v2/", hostname);
            var client = new HttpClient(handler);
            client.BaseAddress = new Uri(serverAddress);

            var loginJson = new
            {
                username = username,
                password = password,
                accessrights = apiKey
            };
            var request = new StringContent(JsonConvert.SerializeObject(loginJson), Encoding.UTF8, "application/json");
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
