namespace dp;

using CommandLine;
using Rssdp;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

class Program
{
    public static System.Net.IPAddress? GetIPAddress(string? iface)
    {
        return NetworkInterface
            .GetAllNetworkInterfaces()
            .Where(i => i.OperationalStatus == OperationalStatus.Up)
            .Where(i => i.Name == iface)
            .Select(i => i.GetIPProperties().UnicastAddresses)
            .SelectMany(u => u)
            .Where(u => u.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            .Select(i => i.Address)
            .FirstOrDefault();
    }

    public class Options
    {
        [Option('i', "iface", Required = true, HelpText = "Network interface to bind to")]
        public string? IFace { get; set; }

        [Option('n', "name", Required = false, HelpText = "Device name")]
        public string? Name { get; set; }

        [Option('l', "location", Required = false, HelpText = "Device location")]
        public string? Location { get; set; }

        [Option('u', "uuid", Required = false, HelpText = "Device UUID")]
        public string? Uuid { get; set; }

        [Option('d', "description", Required = false, HelpText = "Device description")]
        public string? Description { get; set; }

        [Option('m', "manufacturer", Required = false, HelpText = "Device manufacturer")]
        public string? Manufacturer { get; set; }

        [Option('o', "model", Required = false, HelpText = "Device model")]
        public string? Model { get; set; }

        [Option('t', "type", Required = false, HelpText = "Device type")]
        public string? Type { get; set; }

        [Option('p', "port", Required = false, Default = 0, HelpText = "Device port")]
        public int Port { get; set; }

        [Option('c', "cache-lifetime", Required = false, Default = 1, HelpText = "Device cache lifetime in minutes")]
        public int CacheLifetime { get; set; }
    }

    static string GetLocationURL(IPAddress? ip, int port = 0, string location = "") => $"http://{ip}{(port > 0 ? $":{port}" : "")}{location}";

    static int FreeTcpPort()
    {
        TcpListener l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        int port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return port;
    }

    static void Main(string[] args)
    {
        int httpPort = FreeTcpPort();
        IPAddress? ipAddress = null;
        var _Publisher = new SsdpDevicePublisher();

        _ = Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(o =>
        {
            try
            {
                var interfaces = NetworkInterface.GetAllNetworkInterfaces().Where(i => i.OperationalStatus == OperationalStatus.Up);
                if (!interfaces.Where(i => i.Name == o.IFace).Any())
                {
                    Console.WriteLine($"Network interface '{o.IFace}' not found or not up. Please check your appsettings.json file.");
                    Console.WriteLine("Available network interfaces:");
                    Console.WriteLine($"  {"Name",30}: Description\n----------------------------------------------");
                    foreach (var i in interfaces)
                        Console.WriteLine($"- {i.Name,30}: {i.Description}");

                    const int ERROR_BAD_ARGUMENTS = 0xA0;
                    Environment.Exit(ERROR_BAD_ARGUMENTS);
                }

                ipAddress = GetIPAddress(o.IFace);
                var location = GetLocationURL(ipAddress, httpPort, "/");
                var presentationUrl = GetLocationURL(ipAddress, httpPort, o.Location ?? "");
                var deviceDefinition = new SsdpRootDevice
                {
                    CacheLifetime = TimeSpan.FromMinutes(o.CacheLifetime),
                    Location = new Uri(location),
                    PresentationUrl = new Uri(presentationUrl),
                    DeviceType = "upnp:rootdevice",
                    FriendlyName = o.Name ?? "discovery-app",
                    Manufacturer = o.Manufacturer ?? "sparse",
                    ModelName = o.Model ?? "discovery-protocol",
                    Uuid = o.Uuid ?? $"{Guid.NewGuid()}",
                };

                // Create device description document http listener
                new Thread(() =>
                {
                    var listener = new HttpListener();
                    listener.Prefixes.Add(location);
                    listener.Start();
                    Console.WriteLine($"[{DateTimeOffset.Now,30}] Listening on '{location}'");

                    while (true)
                    {
                        try
                        {
                            var context = listener.GetContext();
                            var response = context.Response;
                            var buffer = Encoding.UTF8.GetBytes(deviceDefinition.ToRootDevice().ToDescriptionDocument());
                            response.ContentLength64 = buffer.Length;
                            response.OutputStream.Write(buffer, 0, buffer.Length);
                            response.OutputStream.Close();
                        }
                        catch (Exception e) { Console.WriteLine(e); }
                    }
                }).Start();

                // Create device ip address change listener
                new Thread(() =>
                {
                    // Add device first time
                    _Publisher.AddDevice(deviceDefinition);
                    Console.WriteLine($"[{DateTimeOffset.Now,30}] Publish device: '\n{deviceDefinition.ToRootDevice().ToDescriptionDocument()}\n'");

                    while (true)
                    {
                        try
                        {
                            // Update device location if IP address changes
                            var ip = GetIPAddress(o.IFace);
                            if ($"{ip}" != $"{ipAddress}")
                            {
                                Console.WriteLine($"[{DateTimeOffset.Now,30}] IP address changed from '{ipAddress}' to '{ip}'");
                                ipAddress = ip;
                                _Publisher.RemoveDevice(deviceDefinition);
                                deviceDefinition.Location = new Uri(GetLocationURL(ipAddress, httpPort, "/"));
                                deviceDefinition.PresentationUrl = new Uri(GetLocationURL(ipAddress, o.Port, o.Location ?? ""));

                                // Update device
                                _Publisher.AddDevice(deviceDefinition);
                                Console.WriteLine($"[{DateTimeOffset.Now,30}] Update device: '\n{deviceDefinition.ToRootDevice().ToDescriptionDocument()}\n'");
                            }
                        }
                        catch (Exception e) { Console.WriteLine(e); }
                        Thread.Sleep(10000);
                    }
                }).Start();

                Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        });
    }
}
