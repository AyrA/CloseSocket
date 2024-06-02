using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace CloseSocketLib
{
    internal static partial class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct TcpV4Table
        {
            public uint numberOfEntries;
            public TcpV4TableEntry[] rows;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TcpV6Table
        {
            public uint numberOfEntries;
            public TcpV6TableEntry[] rows;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct TcpV4TableEntry
        {
            public TcpState State;
            public uint LocalAddress;
            public uint LocalPort;
            public uint RemoteAddress;
            public uint RemotePort;
            public uint OwnerPid;
            private readonly OffloadState OffloadState;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct TcpV6TableEntry
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] LocalAddress;
            public uint LocalScopeId;
            public uint LocalPort;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] RemoteAddress;
            public uint RemoteScopeId;
            public uint RemotePort;
            public TcpState State;
            public uint OwnerPid;
            private readonly OffloadState OffloadState;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Tcp4KillEntry
        {
            public TcpState State;
            public uint LocalAddress;
            public uint LocalPort;
            public uint RemoteAddress;
            public uint RemotePort;
            public uint OwnerPid;

            public Tcp4KillEntry(TcpInfoEntry rdpConn) : this()
            {
                State = TcpState.DeleteTcb;
                LocalAddress = BitConverter.ToUInt32(rdpConn.Local.Address.GetAddressBytes());
                LocalPort = (ushort)rdpConn.Local.Port;
                RemoteAddress = BitConverter.ToUInt32(rdpConn.Remote.Address.GetAddressBytes());
                RemotePort = (ushort)rdpConn.Remote.Port;
            }
        }

        private enum OffloadState
        {
            /// <summary>
            /// The TCP connection is currently owned by the network stack on the local computer,
            /// and is not offloaded
            /// </summary>
            TcpConnectionOffloadStateInHost,
            /// <summary>
            /// The TCP connection is in the process of being offloaded,
            /// but the offload has not been completed.
            /// </summary>
            TcpConnectionOffloadStateOffloading,
            /// <summary>
            /// The TCP connection is offloaded to the network interface controller.
            /// </summary>
            TcpConnectionOffloadStateOffloaded,
            /// <summary>
            /// The TCP connection is in the process of being uploaded back to the network stack
            /// on the local computer, but the reinstate-to-host process has not completed.
            /// </summary>
            TcpConnectionOffloadStateUploading,
            /// <summary>
            /// The maximum possible value for the TCP_CONNECTION_OFFLOAD_STATE enumeration type.
            /// This is not a legal value for the possible TCP connection offload state.
            /// </summary>
            TcpConnectionOffloadStateMax
        }

        private static readonly int tableV4RowSize = Marshal.SizeOf<TcpV4TableEntry>();
        private static readonly int tableV6RowSize = Marshal.SizeOf<TcpV6TableEntry>();

        [LibraryImport("Iphlpapi.dll")]
        private static partial int GetTcpTable2(nint buffer, ref int bufferSize, [MarshalAs(UnmanagedType.Bool)] bool order);
        [LibraryImport("Iphlpapi.dll")]
        private static partial int GetTcp6Table2(nint buffer, ref int bufferSize, [MarshalAs(UnmanagedType.Bool)] bool order);
        [LibraryImport("Iphlpapi.dll")]
        private static partial int SetTcpEntry(Tcp4KillEntry buffer);

        internal static int Kill(TcpInfoEntry entry)
        {
            if (entry.Local.AddressFamily == AddressFamily.InterNetworkV6 ||
                entry.Remote.AddressFamily == AddressFamily.InterNetworkV6)
            {
                return UglyHacks.KillV6(entry);
            }
            else
            {
                var kill = new Tcp4KillEntry()
                {
                    LocalAddress = BitConverter.ToUInt32(entry.Local.Address.GetAddressBytes()),
                    LocalPort = Tools.Swap((ushort)entry.Local.Port),
                    RemoteAddress = BitConverter.ToUInt32(entry.Remote.Address.GetAddressBytes()),
                    RemotePort = Tools.Swap((ushort)entry.Remote.Port),
                    OwnerPid = entry.ProcessId,
                    State = TcpState.DeleteTcb
                };

                return SetTcpEntry(kill);
            }
        }

        internal static TcpInfoEntry[] GetConnections()
        {
            using var v4buffer = new SafeBufferHandle(1024 * 1024);
            using var v6buffer = new SafeBufferHandle(1024 * 1024);

            int exactv4Size = v4buffer.Size;
            int exactv6Size = v6buffer.Size;

            int v4returnValue = GetTcpTable2(v4buffer.DangerousGetHandle(), ref exactv4Size, true);
            if (v4returnValue != 0)
            {
                throw new Exception($"V4 Error. Ret not 0: {v4returnValue}");
            }

            int v6returnValue = GetTcp6Table2(v6buffer.DangerousGetHandle(), ref exactv6Size, true);
            if (v6returnValue != 0)
            {
                throw new Exception($"V6 Error. Ret not 0: {v6returnValue}");
            }

            var v4data = v4buffer.Buffer;
            var v6data = v6buffer.Buffer;

            var numberOfv4Entries = BitConverter.ToInt32(v4data, 0);
            var numberOfv6Entries = BitConverter.ToInt32(v6data, 0);

            var entries = new List<TcpInfoEntry>(numberOfv6Entries + numberOfv4Entries);

            for (var i = 0; i < numberOfv4Entries; i++)
            {
                var entry = Marshal.PtrToStructure<TcpV4TableEntry>(v4buffer.DangerousGetHandle() + 4 + (tableV4RowSize * i));
                entries.Add(new TcpInfoEntry(entry));
            }
            for (var i = 0; i < numberOfv6Entries; i++)
            {
                var entry = Marshal.PtrToStructure<TcpV6TableEntry>(v6buffer.DangerousGetHandle() + 4 + (tableV6RowSize * i));
                entries.Add(new TcpInfoEntry(entry));
            }
            return [.. entries];
        }
    }
}
