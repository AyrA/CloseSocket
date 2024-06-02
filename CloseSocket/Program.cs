using CloseSocketLib;
using System.Net;

namespace CloseSocket
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args == null || args.Length == 0 || args.Contains("/?"))
            {
                Help();
                return;
            }
            //Variables for command line arguments
            IPAddress? localIp = null, remoteIp = null;
            int? localPort = null, remotePort = null;
            TcpState? state = null;
            int? processId = null;
            bool dryRun = false, verbose = false, hasFilter = false;

            //Process command line arguments
            for (var i = 0; i < args.Length; i++)
            {
                switch (args[i].ToUpperInvariant())
                {
                    case "/D":
                        if (dryRun)
                        {
                            throw new DuplicateArgumentException(args[i]);
                        }
                        dryRun = true;
                        break;
                    case "/V":
                        if (verbose)
                        {
                            throw new DuplicateArgumentException(args[i]);
                        }
                        verbose = true;
                        break;
                    case "/L":
                        if (localIp != null || localPort != null)
                        {
                            throw new DuplicateArgumentException(args[i]);
                        }
                        ParseIpPort(args[++i], out localIp, out localPort);
                        hasFilter = localIp != null || localPort != null;
                        break;
                    case "/R":
                        if (remoteIp != null || remotePort != null)
                        {
                            throw new DuplicateArgumentException(args[i]);
                        }
                        ParseIpPort(args[++i], out remoteIp, out remotePort);
                        hasFilter = remoteIp != null || remotePort != null;
                        break;
                    case "/S":
                        if (state != null)
                        {
                            throw new DuplicateArgumentException(args[i]);
                        }
                        if (int.TryParse(args[i + 1], out var temp))
                        {
                            if (!Enum.IsDefined((TcpState)temp))
                            {
                                throw new ArgumentException($"Unknown state: {temp}");
                            }
                            state = (TcpState)temp;
                        }
                        else
                        {
                            if (!Enum.TryParse(args[i + 1], true, out TcpState parsed))
                            {
                                throw new ArgumentException($"Unknown state: {args[i + 1]}");
                            }
                            else
                            {
                                state = parsed;
                            }
                        }
                        ++i;
                        hasFilter = true;
                        break;
                    case "/P":
                        if (processId != null)
                        {
                            throw new DuplicateArgumentException(args[i]);
                        }
                        if (!int.TryParse(args[i + 1], out var procId))
                        {
                            throw new ArgumentException("Unable to parse process id");
                        }
                        processId = procId;
                        hasFilter = true;
                        break;
                    default:
                        throw new ArgumentException($"Unknown argument: '{args[i]}'. Use /? for help");
                }
            }
            //Abort early if user did not specify a filter
            if (!hasFilter && !dryRun)
            {
                throw new InvalidOperationException("At least one filter must be supplied");
            }

            //Get connections that match filter
            var connections = TcpInfo.GetConnections(state, localIp, localPort, remoteIp, remotePort, processId);
            if (verbose && connections.Length == 0)
            {
                Console.WriteLine("The given filters do not match any connection");
            }

            //Report and/or close all matched connections
            foreach (var c in connections)
            {
                if (dryRun)
                {
                    if (verbose)
                    {
                        Console.WriteLine(c);
                    }
                }
                else
                {
                    if (verbose)
                    {
                        Console.Write("Killing {0}... ", c);
                        Console.WriteLine(c.Kill() == 0 ? "Success" : "Failed");
                    }
                    else
                    {
                        c.Kill();
                    }
                }
            }
        }

        private static void ParseIpPort(string data, out IPAddress? ip, out int? port)
        {
            if (!data.Contains(':'))
            {
                throw new FormatException($"<ip>:<port> combination is in an invalid format: '{data}'");
            }
            var stringIp = data[..data.LastIndexOf(':')].Trim();
            var stringPort = data[(data.LastIndexOf(':') + 1)..].Trim();
            if (string.IsNullOrEmpty(stringIp) || stringIp == "*")
            {
                ip = null;
            }
            else
            {
                try
                {
                    ip = IPAddress.Parse(stringIp);
                }
                catch
                {
                    throw new ArgumentException($"Unable to parse '{stringIp}' into a valid IP address");
                }
            }
            if (string.IsNullOrEmpty(stringPort) || stringPort == "-" || stringPort == "*")
            {
                port = null;
            }
            else
            {
                try
                {
                    port = int.Parse(stringPort);
                    if (port < ushort.MinValue || port > ushort.MaxValue)
                    {
                        port = null;
                        throw new ArgumentException($"{port} is outside of the valid TCP port number range");
                    }
                }
                catch
                {
                    throw new ArgumentException($"Unable to parse '{stringPort}' into a valid TCP port");
                }
            }
        }

        private static void Help()
        {
            Console.WriteLine(@"CloseSocket [/L localIP:localPort] [/R remoteIP:remotePort] [/S state] [/P pid] [/D] [/V]
Closes all matching TCP connections

At least one filter must be specified

/L   Local IP address and port
        The IP and/or port can be given as '*' to indicate a wildcard.
        '*:80' matches all local endpoints with port 80.
        '127.0.0.1:*' matches all local endpoints that use the loopback address.
        Not specifying this argument implies '*:*'.
        IPv6 addresses must be enclosed in square brackets.

/R   Remote IP address and port
        See /L for format information.
        This should not be specified for connections in the 'Listen' state.

/S   Connection state
        Filters connections based on the given state
        A list of states is at the end of this help.

/P   Process id
        Id of the process that owns the connection.

/D   Dry-run
        Filter connections but do not actually close them
        Not useful unless /V is specified.

/V   Verbose output
        Lists all connections that were processed or failed.

Windows will always attempt to close a TCP connection properly.
Because of this, connections closed with this tool may linger around
until the remote end acknowledges the closure, or the connection times out.

You cannot close UDP connections because it's a connectionless protocol.

Possible connection states:");
            foreach (var val in Enum.GetValues<TcpState>().Except([TcpState.Closed, TcpState.DeleteTcb]))
            {
                Console.WriteLine("- {0}", val);
            }
        }
    }
}
