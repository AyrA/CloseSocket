namespace CloseSocketLib
{
    internal static class Tools
    {
        internal static ushort Swap(ushort data)
        {
            return (ushort)((data >> 8) | ((data & 0xFF) << 8));
        }
    }
}
