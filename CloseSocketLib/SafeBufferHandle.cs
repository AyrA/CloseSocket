using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace CloseSocketLib
{
    internal class SafeBufferHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public int Size { get; }

        public byte[] Buffer
        {
            get
            {
                byte[] ret = new byte[Size];
                Marshal.Copy(handle, ret, 0, ret.Length);
                return ret;
            }
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                if (value.Length > Size)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), $"Buffer size cannot exceed {Size}");
                }
                if (value.Length == 0)
                {
                    return;
                }

                Marshal.Copy(value, 0, handle, value.Length);
            }
        }

        public SafeBufferHandle(int size) : base(true)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);
            SetHandle(Marshal.AllocHGlobal(size));
            Size = size;
        }

        public void Clear()
        {
            Buffer = new byte[Size];
        }

        protected override bool ReleaseHandle()
        {
            Marshal.FreeHGlobal(handle);
            SetHandleAsInvalid();
            return true;
        }
    }
}
