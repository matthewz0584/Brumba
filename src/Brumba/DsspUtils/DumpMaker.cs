using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;




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

    [DllImport("DbgHelp.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
    private static extern Boolean MiniDumpWriteDump(
        IntPtr hProcess,
        Int32 processId,
        IntPtr fileHandle,
        int dumpType,
        IntPtr excepInfo,
        IntPtr userInfo,
        IntPtr extInfo);

    public static void CreateMiniDump()
    {
        using (System.Diagnostics.Process process = System.Diagnostics.Process.GetCurrentProcess())
        {
            string fileName;
            fileName = string.Format(@"CRASH_DUMP_{0}_{1}_{2}.dmp", DateTime.Today.ToShortDateString(),
                DateTime.Now.ToShortTimeString().Replace(":", "."), DateTime.Now.Ticks);

            var Mdinfo = new MinidumpExceptionInformation
            {
                ThreadId = GetCurrentThreadId(),
                ExceptionPointers = Marshal.GetExceptionPointers(),
                ClientPointers = true
            };

            
            var filepathpart = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            filepathpart = Path.Combine(filepathpart, "ContractCrushes");
            DirectoryInfo drInfo = new DirectoryInfo(filepathpart);
            if(!drInfo.Exists)
                drInfo.Create();
            var filepath = Path.Combine(filepathpart, fileName);

            

            using (var fs = new FileStream(filepath, FileMode.Create))
            {

              {
                    //Stack and backing store memory written to the minidump file should be filtered to remove all but the pointer values necessary to reconstruct a stack trace.
                    MiniDumpWriteDump(
                        process.Handle,
                        (uint) process.Id,
                        fs.SafeFileHandle.DangerousGetHandle(),
                        MINIDUMP_TYPE.MiniDumpWithFullMemory,
                        ref Mdinfo,
                        IntPtr.Zero,
                        IntPtr.Zero);
                }

     
            }
        }


    }

    public static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        CreateMiniDump();
    }
}

