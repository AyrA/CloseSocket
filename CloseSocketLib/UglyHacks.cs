using System.Runtime.InteropServices;

namespace CloseSocketLib
{
    /// <summary>
    /// Contains undocumented ungodly abominations
    /// </summary>
    /// <remarks>
    /// See <a href="https://www.x86matthew.com/view_post?id=settcpentry6">x86matthew.com/view_post?id=settcpentry6</a>
    /// </remarks>
    internal static partial class UglyHacks
    {
        private static readonly byte[] NpiMsTcpModuleID = [
            0x18, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
            0x03, 0x4A, 0x00, 0xEB, 0x1A, 0x9B, 0xD4, 0x11,
            0x91, 0x23, 0x00, 0x50, 0x04, 0x77, 0x59, 0xBC
        ];

        [StructLayout(LayoutKind.Sequential)]
        private struct KillTcpSocketDataV6
        {
            public ushort wLocalAddressFamily;
            public ushort wLocalPort;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] bReserved1;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] bLocalAddr;
            public uint dwLocalScopeID;

            public ushort wRemoteAddressFamily;
            public ushort wRemotePort;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] bReserved2;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] bRemoteAddr;
            public uint dwRemoteScopeID;
        };

        private enum NsiStore
        {
            NsiPersistent = 0,
            NsiActive = 1,
            NsiBoth = 2,
            NsiCurrent = 3,
            NsiBootFirmwareTable = 4
        }

        private enum NsiSetAction
        {
            NsiSetDefault = 0,
            NsiSetCreateOnly = 1,
            NsiSetCreateOrSet = 2,
            NsiSetDelete = 3,
            NsiSetReset = 4,
            NsiSetClear = 5,
            NsiSetCreateOrSetWithReference = 6,
            NsiSetDeleteWithReference = 7,
        }

        [DllImport("nsi.dll")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "SYSLIB1054", Justification = "Doesn't fucking work for this one")]
        private static extern int NsiSetAllParameters(NsiStore store, NsiSetAction action,
            [In] byte[] moduleId, int objectIndex, KillTcpSocketDataV6 keyStruct,
            int keyStructLength, nint rwParamStruct, int rwParamStructLength);

        internal static int KillV6(TcpInfoEntry entry)
        {
            var killData = new KillTcpSocketDataV6()
            {
                //Local part
                wLocalAddressFamily = (ushort)entry.Local.AddressFamily,
                wLocalPort = Tools.Swap((ushort)entry.Local.Port),
                bReserved1 = new byte[4],
                bLocalAddr = entry.Local.Address.GetAddressBytes(),
                dwLocalScopeID = (uint)entry.Local.Address.ScopeId,

                //Remote part
                wRemoteAddressFamily = (ushort)entry.Remote.AddressFamily,
                wRemotePort = Tools.Swap((ushort)entry.Remote.Port),
                bReserved2 = new byte[4],
                bRemoteAddr = entry.Remote.Address.GetAddressBytes(),
                dwRemoteScopeID = (uint)entry.Remote.Address.ScopeId
            };

            return NsiSetAllParameters(/*1*/ NsiStore.NsiActive, /*2*/ NsiSetAction.NsiSetCreateOrSet,
                NpiMsTcpModuleID, 16, killData, Marshal.SizeOf(killData),
                /*No extra params*/ 0, 0);
        }
    }
}
