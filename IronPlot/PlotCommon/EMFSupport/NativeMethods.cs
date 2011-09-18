using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace WpfToWmfClipboard
{
    internal static class NativeMethods
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool OpenClipboard(IntPtr hWndNewOwner);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EmptyClipboard();
        [DllImport("user32.dll")]
        internal static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CloseClipboard();
        [DllImport("gdi32.dll")]
        internal static extern IntPtr CopyEnhMetaFile(IntPtr hemfSrc, IntPtr hNULL);
        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteEnhMetaFile(IntPtr hemf);

        /// <see href="http://msdn2.microsoft.com/en-us/library/ms648063.aspx"/>
        [DllImport("User32.dll", SetLastError = true)]
        internal static extern int DestroyIcon(IntPtr hIcon);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteObject(IntPtr hObject);

    }
}
