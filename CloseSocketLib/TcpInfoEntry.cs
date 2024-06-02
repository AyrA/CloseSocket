using System.Net;
using static CloseSocketLib.NativeMethods;

namespace CloseSocketLib
{
    /// <summary>
    /// Contains information about a TCP connection
    /// </summary>
    public class TcpInfoEntry
    {
        /// <summary>
        /// State of the connection
        /// </summary>
        public TcpState State { get; private set; }
        /// <summary>
        /// Local endpoint
        /// </summary>
        public IPEndPoint Local { get; }
        /// <summary>
        /// Remote endpoint
        /// </summary>
        /// <remarks>This will be unspecified for connections in the listen state</remarks>
        public IPEndPoint Remote { get; }
        /// <summary>
        /// Process id that owns the connection
        /// </summary>
        /// <remarks>
        /// This will be zero if the process no longer exists.
        /// The connection will almost always be in a closing state at that point.
        /// </remarks>
        public uint ProcessId { get; }

        private TcpInfoEntry(TcpState state, IPAddress localAddr, uint localPort, IPAddress remoteAddr, uint remotePort, uint processId)
        {
            State = state;

            //Port needs a swap for some reason (IP address doesn't)
            var lPort = (ushort)IPAddress.NetworkToHostOrder((short)localPort);
            var rPort = (ushort)IPAddress.NetworkToHostOrder((short)remotePort);


            Local = new IPEndPoint(localAddr, lPort);
            Remote = new IPEndPoint(remoteAddr, rPort);
            ProcessId = processId;
        }

        internal TcpInfoEntry(TcpV4TableEntry entry) : this(
            entry.State,
            new IPAddress(entry.LocalAddress),
            entry.LocalPort,
            new IPAddress(entry.RemoteAddress),
            entry.RemotePort,
            entry.OwnerPid
            )
        {
            //NOOP
        }

        internal TcpInfoEntry(TcpV6TableEntry entry) : this(
            entry.State,
            new IPAddress(entry.LocalAddress, entry.LocalScopeId),
            entry.LocalPort,
            new IPAddress(entry.RemoteAddress, entry.RemoteScopeId),
            entry.RemotePort,
            entry.OwnerPid
            )
        {
            //NOOP
        }

        /// <summary>
        /// Kills the current connection
        /// </summary>
        /// <returns>Status code of the kill command. Zero indicates success</returns>
        public int Kill()
        {
            var ret = NativeMethods.Kill(this);
            if (ret == 0)
            {
                State = TcpState.DeleteTcb;
            }
            return ret;
        }

        /// <summary>
        /// Gets a "Client --> Server" style representation
        /// </summary>
        /// <returns>String representation of this instance</returns>
        public override string ToString()
        {
            return $"{Local} --> {Remote} ({State}, PID: {ProcessId})";
        }
    }
}
