using Bam.DependencyInjection;
using Bam.Test;

namespace Bam.Svc.Tests.Unit;

[UnitTestMenu("BamsvcConfiguration Should", Selector = "bcs")]
public class BamsvcConfigurationShould : UnitTestMenuContainer
{
    public BamsvcConfigurationShould(ServiceRegistry serviceRegistry) : base(serviceRegistry)
    {
    }

    [UnitTest]
    public void ParseServerNameFromPositionalArg()
    {
        When.A<BamsvcConfiguration>(
            "parses server name from positional arg",
            () => BamsvcConfiguration.FromArgs(new[] { "my-svc" }),
            (config) => config.ServerName)
        .TheTest
        .ShouldPass(because =>
        {
            BamsvcConfiguration config = BamsvcConfiguration.FromArgs(new[] { "my-svc" });
            because.ItsTrue("server name is 'my-svc'", config.ServerName == "my-svc");
        })
        .SoBeHappy()
        .UnlessItFailed();
    }

    [UnitTest]
    public void DefaultServerNameToBamsvc()
    {
        When.A<BamsvcConfiguration>(
            "defaults server name when no positional arg",
            () => BamsvcConfiguration.FromArgs(Array.Empty<string>()),
            (config) => config.ServerName)
        .TheTest
        .ShouldPass(because =>
        {
            BamsvcConfiguration config = BamsvcConfiguration.FromArgs(Array.Empty<string>());
            because.ItsTrue("server name defaults to 'bamsvc'", config.ServerName == "bamsvc");
        })
        .SoBeHappy()
        .UnlessItFailed();
    }

    [UnitTest]
    public void ParseHttpPort()
    {
        When.A<BamsvcConfiguration>(
            "parses --port= argument",
            () => BamsvcConfiguration.FromArgs(new[] { "--port=9090" }),
            (config) => config.HttpPort)
        .TheTest
        .ShouldPass(because =>
        {
            BamsvcConfiguration config = BamsvcConfiguration.FromArgs(new[] { "--port=9090" });
            because.ItsTrue("HTTP port is 9090", config.HttpPort == 9090);
        })
        .SoBeHappy()
        .UnlessItFailed();
    }

    [UnitTest]
    public void DefaultHttpPort()
    {
        When.A<BamsvcConfiguration>(
            "defaults HTTP port to 8080",
            () => BamsvcConfiguration.FromArgs(Array.Empty<string>()),
            (config) => config.HttpPort)
        .TheTest
        .ShouldPass(because =>
        {
            BamsvcConfiguration config = BamsvcConfiguration.FromArgs(Array.Empty<string>());
            because.ItsTrue("HTTP port defaults to 8080", config.HttpPort == 8080);
        })
        .SoBeHappy()
        .UnlessItFailed();
    }

    [UnitTest]
    public void EnableHttpsByFlag()
    {
        When.A<BamsvcConfiguration>(
            "enables HTTPS when --https is present",
            () => BamsvcConfiguration.FromArgs(new[] { "--https" }),
            (config) => config.EnableHttps)
        .TheTest
        .ShouldPass(because =>
        {
            BamsvcConfiguration config = BamsvcConfiguration.FromArgs(new[] { "--https" });
            because.ItsTrue("HTTPS is enabled", config.EnableHttps);
        })
        .SoBeHappy()
        .UnlessItFailed();
    }

    [UnitTest]
    public void ParseHttpsPort()
    {
        When.A<BamsvcConfiguration>(
            "parses --https-port= argument",
            () => BamsvcConfiguration.FromArgs(new[] { "--https", "--https-port=8444" }),
            (config) => config.HttpsPort)
        .TheTest
        .ShouldPass(because =>
        {
            BamsvcConfiguration config = BamsvcConfiguration.FromArgs(new[] { "--https", "--https-port=8444" });
            because.ItsTrue("HTTPS port is 8444", config.HttpsPort == 8444);
        })
        .SoBeHappy()
        .UnlessItFailed();
    }

    [UnitTest]
    public void EnableTcpAndUdpWithCombinedFlag()
    {
        When.A<BamsvcConfiguration>(
            "enables TCP and UDP with --tcp-udp flag",
            () => BamsvcConfiguration.FromArgs(new[] { "--tcp-udp" }),
            (config) => new ProtocolFlagsOutcome(config.EnableTcp, config.EnableUdp))
        .TheTest
        .ShouldPass(because =>
        {
            BamsvcConfiguration config = BamsvcConfiguration.FromArgs(new[] { "--tcp-udp" });
            because.ItsTrue("TCP is enabled", config.EnableTcp);
            because.ItsTrue("UDP is enabled", config.EnableUdp);
        })
        .SoBeHappy()
        .UnlessItFailed();
    }

    [UnitTest]
    public void EnableTcpIndependently()
    {
        When.A<BamsvcConfiguration>(
            "enables TCP independently with --tcp flag",
            () => BamsvcConfiguration.FromArgs(new[] { "--tcp" }),
            (config) => new ProtocolFlagsOutcome(config.EnableTcp, config.EnableUdp))
        .TheTest
        .ShouldPass(because =>
        {
            BamsvcConfiguration config = BamsvcConfiguration.FromArgs(new[] { "--tcp" });
            because.ItsTrue("TCP is enabled", config.EnableTcp);
            because.ItsTrue("UDP is not enabled", !config.EnableUdp);
        })
        .SoBeHappy()
        .UnlessItFailed();
    }

    [UnitTest]
    public void EnableUdpIndependently()
    {
        When.A<BamsvcConfiguration>(
            "enables UDP independently with --udp flag",
            () => BamsvcConfiguration.FromArgs(new[] { "--udp" }),
            (config) => new ProtocolFlagsOutcome(config.EnableTcp, config.EnableUdp))
        .TheTest
        .ShouldPass(because =>
        {
            BamsvcConfiguration config = BamsvcConfiguration.FromArgs(new[] { "--udp" });
            because.ItsTrue("TCP is not enabled", !config.EnableTcp);
            because.ItsTrue("UDP is enabled", config.EnableUdp);
        })
        .SoBeHappy()
        .UnlessItFailed();
    }

    [UnitTest]
    public void ParseTcpAndUdpPorts()
    {
        When.A<BamsvcConfiguration>(
            "parses --tcp-port= and --udp-port= arguments",
            () => BamsvcConfiguration.FromArgs(new[] { "--tcp-port=9413", "--udp-port=9414" }),
            (config) => new PortsOutcome(config.TcpPort, config.UdpPort))
        .TheTest
        .ShouldPass(because =>
        {
            BamsvcConfiguration config = BamsvcConfiguration.FromArgs(new[] { "--tcp-port=9413", "--udp-port=9414" });
            because.ItsTrue("TCP port is 9413", config.TcpPort == 9413);
            because.ItsTrue("UDP port is 9414", config.UdpPort == 9414);
        })
        .SoBeHappy()
        .UnlessItFailed();
    }

    [UnitTest]
    public void ParseCertificatePath()
    {
        When.A<BamsvcConfiguration>(
            "parses --cert= argument",
            () => BamsvcConfiguration.FromArgs(new[] { "--cert=/path/to/cert.pfx" }),
            (config) => config.CertificatePath!)
        .TheTest
        .ShouldPass(because =>
        {
            BamsvcConfiguration config = BamsvcConfiguration.FromArgs(new[] { "--cert=/path/to/cert.pfx" });
            because.ItsTrue("certificate path is set", config.CertificatePath == "/path/to/cert.pfx");
        })
        .SoBeHappy()
        .UnlessItFailed();
    }

    [UnitTest]
    public void ParseFullConfiguration()
    {
        string[] fullArgs = new[]
        {
            "gateway",
            "--port=9000",
            "--https",
            "--https-port=9443",
            "--tcp",
            "--udp",
            "--tcp-port=9413",
            "--udp-port=9414",
            "--cert=/certs/gateway.pfx",
        };

        When.A<BamsvcConfiguration>(
            "parses all arguments together",
            () => BamsvcConfiguration.FromArgs(fullArgs),
            (config) => config)
        .TheTest
        .ShouldPass(because =>
        {
            BamsvcConfiguration config = BamsvcConfiguration.FromArgs(fullArgs);
            because.ItsTrue("server name is 'gateway'", config.ServerName == "gateway");
            because.ItsTrue("HTTP port is 9000", config.HttpPort == 9000);
            because.ItsTrue("HTTPS is enabled", config.EnableHttps);
            because.ItsTrue("HTTPS port is 9443", config.HttpsPort == 9443);
            because.ItsTrue("TCP is enabled", config.EnableTcp);
            because.ItsTrue("UDP is enabled", config.EnableUdp);
            because.ItsTrue("TCP port is 9413", config.TcpPort == 9413);
            because.ItsTrue("UDP port is 9414", config.UdpPort == 9414);
            because.ItsTrue("certificate path is set", config.CertificatePath == "/certs/gateway.pfx");
        })
        .SoBeHappy()
        .UnlessItFailed();
    }

    [UnitTest]
    public void DefaultBamidTcpPort()
    {
        When.A<BamsvcConfiguration>(
            "defaults bamid TCP port to 24515",
            () => BamsvcConfiguration.FromArgs(Array.Empty<string>()),
            (config) => config.BamidTcpPort)
        .TheTest
        .ShouldPass(because =>
        {
            BamsvcConfiguration config = BamsvcConfiguration.FromArgs(Array.Empty<string>());
            because.ItsTrue("bamid TCP port defaults to 24515", config.BamidTcpPort == 24515);
        })
        .SoBeHappy()
        .UnlessItFailed();
    }

    [UnitTest]
    public void ParseBamidTcpPort()
    {
        When.A<BamsvcConfiguration>(
            "parses --bamid-tcp-port= argument",
            () => BamsvcConfiguration.FromArgs(new[] { "--bamid-tcp-port=24000" }),
            (config) => config.BamidTcpPort)
        .TheTest
        .ShouldPass(because =>
        {
            BamsvcConfiguration config = BamsvcConfiguration.FromArgs(new[] { "--bamid-tcp-port=24000" });
            because.ItsTrue("bamid TCP port is 24000", config.BamidTcpPort == 24000);
        })
        .SoBeHappy()
        .UnlessItFailed();
    }

    private sealed record ProtocolFlagsOutcome(bool EnableTcp, bool EnableUdp);

    private sealed record PortsOutcome(int TcpPort, int UdpPort);
}
