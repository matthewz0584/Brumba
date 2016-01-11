using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Brumba.Utils
{
    //Place it in the main service start method
    //static DsspServiceExposing()
    //{
    //    System.Diagnostics.Contracts.Contract.ContractFailed += (sender, e) =>
    //    {
    //        try
    //        {
    //            //Throw system exception, manual throw is not good enough for Marshal.GetExceptionPointers()
    //            ((string) null).ToString();
    //        }
    //        catch (Exception)
    //        {
    //            DumpMaker.CreateMiniDump();  
    //        }
    //    };
    //}

    public static class DumpMaker
    {
        private static class MINIDUMP_TYPE
        {
            public const int MiniDumpNormal = 0x00000000;
            public const int MiniDumpWithDataSegs = 0x00000001;
            public const int MiniDumpWithFullMemory = 0x00000002;
            public const int MiniDumpWithHandleData = 0x00000004;
            public const int MiniDumpFilterMemory = 0x00000008;
            public const int MiniDumpScanMemory = 0x00000010;
            public const int MiniDumpWithUnloadedModules = 0x00000020;
            public const int MiniDumpWithIndirectlyReferencedMemory = 0x00000040;
            public const int MiniDumpFilterModulePaths = 0x00000080;
            public const int MiniDumpWithProcessThreadData = 0x00000100;
            public const int MiniDumpWithPrivateReadWriteMemory = 0x00000200;
            public const int MiniDumpWithoutOptionalData = 0x00000400;
            public const int MiniDumpWithFullMemoryInfo = 0x00000800;
            public const int MiniDumpWithThreadInfo = 0x00001000;
            public const int MiniDumpWithCodeSegs = 0x00002000;
            public const int MiniDumpWithoutAuxiliaryState = 0x00004000;
            public const int MiniDumpWithFullAuxiliaryState = 0x00008000;
            public const int MiniDumpWithPrivateWriteCopyMemory = 0x00010000;
            public const int MiniDumpIgnoreInaccessibleMemory = 0x00020000;
            public const int MiniDumpWithTokenInformation = 0x00040000;
            public const int MiniDumpWithModuleHeaders = 0x00080000;
            public const int MiniDumpFilterTriage = 0x00100000;
            public const int MiniDumpValidTypeFlags = 0x001fffff;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct MinidumpExceptionInformation
        {
            public uint ThreadId;
            public IntPtr ExceptionPointers;
            public bool ClientPointers;
        }

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("Dbghelp.dll")]
        private static extern bool MiniDumpWriteDump(IntPtr hProcess, uint ProcessId, IntPtr hFile, int DumpType,
            ref MinidumpExceptionInformation ExceptionParam, IntPtr UserStreamParam, IntPtr CallbackParam);

        public static void CreateMiniDump()
        {
            using (var process = System.Diagnostics.Process.GetCurrentProcess())
            {
                var crushesDir = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "ContractCrushes");
                if (!Directory.Exists(crushesDir))
                    Directory.CreateDirectory(crushesDir);
                var crushFileName = string.Format(@"CRASH_DUMP_{0}_{1}_{2}.dmp", DateTime.Today.ToShortDateString(),
                    DateTime.Now.ToShortTimeString().Replace(":", "."), DateTime.Now.Ticks);
                var crushFilePath = Path.Combine(crushesDir, crushFileName);

                using (var fs = new FileStream(crushFilePath, FileMode.Create))
                {
                    var mdinfo = new MinidumpExceptionInformation
                    {
                        ThreadId = GetCurrentThreadId(),
                        ExceptionPointers = Marshal.GetExceptionPointers(),
                        ClientPointers = true
                    };

                    //Stack and backing store memory written to the minidump file should be filtered to remove all but the pointer values necessary to reconstruct a stack trace.
                    MiniDumpWriteDump(process.Handle, (uint)process.Id, fs.SafeFileHandle.DangerousGetHandle(),
                        MINIDUMP_TYPE.MiniDumpWithFullMemory, ref mdinfo, IntPtr.Zero, IntPtr.Zero);
                }
            }
        }
    }
}
