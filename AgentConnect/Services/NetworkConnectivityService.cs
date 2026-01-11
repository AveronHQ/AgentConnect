using System;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace AgentConnect.Services
{
    public class NetworkConnectivityResult
    {
        public bool HasInternet { get; set; }
        public bool HasIntranet { get; set; }
    }

    public class NetworkConnectivityService
    {
        public async Task<NetworkConnectivityResult> CheckConnectivityAsync()
        {
            var result = new NetworkConnectivityResult();

            // Quick check: is any network available?
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                return result;
            }

            // Run both checks in parallel with short timeouts
            var internetTask = CheckInternetAsync();
            var intranetTask = CheckIntranetAsync();

            await Task.WhenAll(internetTask, intranetTask);

            result.HasInternet = await internetTask;
            result.HasIntranet = await intranetTask;

            return result;
        }

        private async Task<bool> CheckInternetAsync()
        {
            // Try multiple hosts in parallel, return true if any succeed
            var tasks = new[]
            {
                TryConnectAsync("8.8.8.8", 53, 1000),        // Google DNS
                TryConnectAsync("1.1.1.1", 53, 1000),        // Cloudflare DNS
                TryConnectAsync("208.67.222.222", 53, 1000)  // OpenDNS
            };

            var results = await Task.WhenAll(tasks);
            return results[0] || results[1] || results[2];
        }

        private async Task<bool> CheckIntranetAsync()
        {
            // Check if we have a local gateway (indicates local network)
            try
            {
                var interfaces = NetworkInterface.GetAllNetworkInterfaces();
                foreach (var ni in interfaces)
                {
                    if (ni.OperationalStatus != OperationalStatus.Up)
                        continue;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                        continue;

                    var props = ni.GetIPProperties();

                    // Has a gateway = connected to local network
                    if (props.GatewayAddresses.Count > 0)
                    {
                        // Try to connect to gateway
                        foreach (var gw in props.GatewayAddresses)
                        {
                            if (gw.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                // Quick TCP check to common ports on gateway
                                if (await TryConnectAsync(gw.Address.ToString(), 80, 500) ||
                                    await TryConnectAsync(gw.Address.ToString(), 443, 500))
                                {
                                    return true;
                                }

                                // Gateway exists even if we can't connect to it
                                return true;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Ignore errors
            }

            return false;
        }

        private async Task<bool> TryConnectAsync(string host, int port, int timeoutMs)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    var connectTask = client.ConnectAsync(host, port);
                    var timeoutTask = Task.Delay(timeoutMs);

                    var completed = await Task.WhenAny(connectTask, timeoutTask);

                    if (completed == connectTask && client.Connected)
                    {
                        return true;
                    }
                }
            }
            catch
            {
                // Connection failed
            }

            return false;
        }
    }
}
