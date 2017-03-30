using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

namespace cheat_blocker_gtasa
{
	class WinApi
	{
		public const int MEM_COMMIT = 0x00001000;
		public const int MEM_RESERVE = 0x00002000;

		public const int PAGE_EXECUTE_READWRITE = 0x40;

		public const int PROCESS_VM_OPERATION = 0x0008;
		public const int PROCESS_VM_READ = 0x0010;
		public const int PROCESS_VM_WRITE = 0x0020;

		[DllImport("kernel32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool FlushInstructionCache(
			SafeProcessHandle hProcess,
			IntPtr lpBaseAddress,
			[MarshalAs(UnmanagedType.SysUInt)] IntPtr dwSize);

		[DllImport("kernel32", SetLastError = true)]
		public static extern SafeProcessHandle OpenProcess(
			int dwDesiredAccess,
			[MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
			int dwProcessId);

		[DllImport("kernel32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool ReadProcessMemory(
			SafeProcessHandle hProcess,
			IntPtr lpBaseAddress,
			IntPtr lpBuffer,
			[MarshalAs(UnmanagedType.SysUInt)] IntPtr nSize,
			IntPtr lpNumberOfBytesRead);

		[DllImport("kernel32", SetLastError = true)]
		public static extern IntPtr VirtualAllocEx(
			SafeProcessHandle hProcess,
			IntPtr lpAddress,
			[MarshalAs(UnmanagedType.SysUInt)] IntPtr dwSize,
			int flAllocationType,
			int flProtect);

		[DllImport("kernel32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool VirtualFreeEx(
			SafeProcessHandle hProcess,
			IntPtr lpAddress,
			[MarshalAs(UnmanagedType.SysUInt)] IntPtr dwSize,
			int dwFreeType);

		[DllImport("kernel32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool WriteProcessMemory(
			SafeProcessHandle hProcess,
			IntPtr lpBaseAddress,
			IntPtr lpBuffer,
			[MarshalAs(UnmanagedType.SysUInt)] IntPtr nSize,
			IntPtr lpNumberOfBytesWritten);
	}
}
