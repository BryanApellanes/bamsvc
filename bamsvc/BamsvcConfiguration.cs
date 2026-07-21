namespace Bam.Svc;

/// <summary>
/// Configuration model for bamsvc multi-protocol startup.
/// Populated from CLI arguments or configuration files.
/// </summary>
public class BamsvcConfiguration
{
    public string ServerName { get; set; } = "bamsvc";
    public int HttpPort { get; set; } = 8080;
    public int HttpsPort { get; set; } = 8443;
    public int TcpPort { get; set; } = 8413;
    public int UdpPort { get; set; } = 8414;
    public bool EnableHttp { get; set; } = true;
    public bool EnableHttps { get; set; }
    public bool EnableTcp { get; set; }
    public bool EnableUdp { get; set; }
    public string? CertificatePath { get; set; }

    /// <summary>
    /// bamid's TCP endpoint port. bamid derives its port deterministically from its server name
    /// ("bamid") via UseNameBasedPort — 24515 as long as bamid's server name stays "bamid".
    /// Override with --bamid-tcp-port= when bamid is deployed under a different name/port.
    /// </summary>
    public int BamidTcpPort { get; set; } = 24515;

    public static BamsvcConfiguration FromArgs(string[] args)
    {
        var config = new BamsvcConfiguration();

        var nameArg = args.FirstOrDefault(a => !a.StartsWith("--"));
        if (nameArg != null)
        {
            config.ServerName = nameArg;
        }

        config.EnableHttps = args.Contains("--https");
        config.EnableTcp = args.Contains("--tcp") || args.Contains("--tcp-udp");
        config.EnableUdp = args.Contains("--udp") || args.Contains("--tcp-udp");

        ParseIntArg(args, "--port=", v => config.HttpPort = v);
        ParseIntArg(args, "--https-port=", v => config.HttpsPort = v);
        ParseIntArg(args, "--tcp-port=", v => config.TcpPort = v);
        ParseIntArg(args, "--udp-port=", v => config.UdpPort = v);
        ParseIntArg(args, "--bamid-tcp-port=", v => config.BamidTcpPort = v);

        var certArg = args.FirstOrDefault(a => a.StartsWith("--cert="));
        if (certArg != null)
        {
            config.CertificatePath = certArg["--cert=".Length..];
        }

        return config;
    }

    private static void ParseIntArg(string[] args, string prefix, Action<int> setter)
    {
        var arg = args.FirstOrDefault(a => a.StartsWith(prefix));
        if (arg != null && int.TryParse(arg[prefix.Length..], out var value))
        {
            setter(value);
        }
    }
}
