using System.Net;

namespace CloseSocketLib
{
    /// <summary>
    /// Holds the result of a call to
    /// <see cref="TcpInfo.Kill(TcpState?, IPAddress?, int?, IPAddress?, int?, int?)"/>
    /// </summary>
    /// <param name="Success">TCP connections sucessfully closed</param>
    /// <param name="Failure">TCP connections that failed to close</param>
    public record TcpKillResult(TcpInfoEntry[] Success, TcpInfoEntry[] Failure);

    /// <summary>
    /// Provides TCP connection information
    /// </summary>
    public static class TcpInfo
    {
        /// <summary>
        /// Gets a list of connections matching the filter
        /// </summary>
        /// <param name="state">TCP state filter</param>
        /// <param name="localAddress">Local endpoint address</param>
        /// <param name="localPort">Local port</param>
        /// <param name="remoteAddress">Remote address</param>
        /// <param name="remotePort">Remote port</param>
        /// <param name="processId">Owner process id</param>
        /// <returns>List of connections</returns>
        /// <remarks>
        /// If all filters are unset, all connections from the TCP table are returned.
        /// Not all filter combinations provide usable results.<br />
        /// Setting <paramref name="state"/> to <see cref="TcpState.Listen"/>
        /// is mutually exclusive with specifying a remote ip and port,
        /// unless the IP is v4 ANY or v6 ANY, and the port is zero.<br />
        /// Process id zero is for connections that are no longer associated with a process.
        /// These are almost always already in a closing state.
        /// </remarks>
        public static TcpInfoEntry[] GetConnections(TcpState? state = null, IPAddress? localAddress = null, int? localPort = null, IPAddress? remoteAddress = null, int? remotePort = null, int? processId = null)
        {
            IEnumerable<TcpInfoEntry> all = NativeMethods.GetConnections();
            if (state != null)
            {
                all = all.Where(m => m.State == state.Value);
            }
            if (localAddress != null)
            {
                all = all.Where(m => m.Local.Address.Equals(localAddress));
            }
            if (localPort != null)
            {
                all = all.Where(m => m.Local.Port == localPort.Value);
            }
            if (remoteAddress != null)
            {
                all = all.Where(m => m.Remote.Address.Equals(remoteAddress));
            }
            if (remotePort != null)
            {
                all = all.Where(m => m.Remote.Port == remotePort.Value);
            }
            if (processId != null)
            {
                all = all.Where(m => m.ProcessId == processId.Value);
            }
            return [.. all];
        }

        /// <summary>
        /// Kills the given connections.
        /// This internally uses <see cref="GetConnections(TcpState?, IPAddress?, int?, IPAddress?, int?, int?)"/>,
        /// but makes sure at least one filter was set.
        /// </summary>
        /// <param name="state">TCP state filter</param>
        /// <param name="localAddress">Local endpoint address</param>
        /// <param name="localPort">Local port</param>
        /// <param name="remoteAddress">Remote address</param>
        /// <param name="remotePort">Remote port</param>
        /// <param name="processId">Owner process id</param>
        /// <returns>Object with processed and failed connections</returns>
        public static TcpKillResult Kill(TcpState? state = null, IPAddress? localAddress = null, int? localPort = null, IPAddress? remoteAddress = null, int? remotePort = null, int? processId = null)
        {
            if (state == null && localAddress == null && localPort == null && remoteAddress == null && remotePort == null && processId == null)
            {
                throw new ArgumentException("At least one filter must be set");
            }

            IEnumerable<TcpInfoEntry> all = GetConnections(state, localAddress, localPort, remoteAddress, remotePort, processId);
            var matches = all.ToArray();
            var success = new List<TcpInfoEntry>();
            var error = new List<TcpInfoEntry>();
            foreach (var match in matches)
            {
                if (match.Kill() == 0)
                {
                    success.Add(match);
                }
                else
                {
                    error.Add(match);
                }
            }
            return new TcpKillResult([.. success], [.. error]);
        }
    }
}
