using System;
using System.Runtime.InteropServices;

namespace TwitchChatVideo
{
    public class NativeMethods
    {

        /// <summary>
        /// Delete a GDI object
        /// </summary>
        /// <param name="o">The poniter to the GDI object to be deleted</param>
        /// <returns></returns>
        [DllImport("gdi32")]
        public static extern int DeleteObject(IntPtr o);
    }
}
