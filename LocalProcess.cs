using static cheat_blocker_gtasa.WinApi;
using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace cheat_blocker_gtasa
{
	class LocalProcess : IDisposable
	{
		public Process Process { get; private set; }
		private bool ownsProcess;

		private SafeProcessHandle handle;

		/// <summary>
		/// Creates a LocalProcess object for the specified process.
		/// </summary>
		/// <param name="process">The process object.</param>
		public LocalProcess(Process process, bool ownsProcess)
		{
			Process = process;
			this.ownsProcess = ownsProcess;

			handle = OpenProcess(PROCESS_VM_OPERATION | PROCESS_VM_READ | PROCESS_VM_WRITE,
				false, process.Id);
			if (handle.IsInvalid) throw new Win32Exception("OpenProcess failed");
		}

		public void Dispose()
		{
			handle.Dispose();
			if (ownsProcess) Process.Dispose();
		}

		/// <summary>
		/// Allocates the specified amount of bytes in the process
		/// and makes them readable, writable and executable.
		/// </summary>
		/// <param name="count">The amount of bytes to allocate.</param>
		/// <returns></returns>
		public IntPtr AllocateMemory(int count)
		{
			var ptr = VirtualAllocEx(handle, IntPtr.Zero, new IntPtr(count),
				MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
			if (ptr == IntPtr.Zero) throw new Win32Exception("VirtualAllocEx failed");
			return ptr;
		}

		/// <summary>
		/// Frees the memory that was previously allocated with AllocateMemory.
		/// </summary>
		/// <param name="addr"></param>
		public void FreeMemory(IntPtr addr)
		{
			if (!VirtualFreeEx(handle, addr, IntPtr.Zero, 0))
			{
				throw new Win32Exception("VirtualFreeEx failed");
			}
		}

		/// <summary>
		/// Calls ReadProcessMemory or WriteProcessMemory.
		/// </summary>
		/// <param name="addr">The address in the process to read from/write to.</param>
		/// <param name="buf">The buffer that will receive the bytes read from
		/// the process/that contains the bytes to write to the process.</param>
		/// <param name="offset">The start offset in buf.</param>
		/// <param name="count">The number of bytes to read from/write to the process</param>
		/// <param name="winApiFunc">Pointer to the ReadProcessMemory/WriteProcessMemory function.</param>
		/// <param name="errMsg">The message of the Win32Exception that is
		/// thrown when the ReadProcessMemory/WriteProcessMemory fails.</param>
		private void ReadOrWriteMemory(IntPtr addr, byte[] buf, int offset, int count,
			Func<SafeProcessHandle, IntPtr, IntPtr, IntPtr, IntPtr, bool> winApiFunc, string errMsg)
		{
			var bufGCHandle = GCHandle.Alloc(buf, GCHandleType.Pinned);
			try
			{
				// Get address of the element in buf that is specified by offset
				var lpBuffer = Marshal.UnsafeAddrOfPinnedArrayElement(buf, offset);

				// Call ReadProcessMemory/WriteProcessMemory
				if (!winApiFunc(handle, addr, lpBuffer, new IntPtr(count), IntPtr.Zero))
				{
					throw new Win32Exception(errMsg);
				}
			}
			finally
			{
				bufGCHandle.Free();
			}
		}

		/// <summary>
		/// Reads the specified amount of bytes from the process.
		/// </summary>
		/// <param name="addr">The address to read from.</param>
		/// <param name="buf">The buffer that will receive the bytes read from the process.</param>
		/// <param name="offset">The start offset in buf.</param>
		/// <param name="count">The number of bytes to read.</param>
		public void ReadMemory(IntPtr addr, byte[] buf, int offset, int count)
		{
			ReadOrWriteMemory(addr, buf, offset, count, ReadProcessMemory, "ReadProcessMemory failed");
		}

		/// <summary>
		/// Reads the specified amount of bytes from the process.
		/// </summary>
		/// <param name="addr">The address to read from.</param>
		/// <param name="count">The number of bytes to read.</param>
		/// <returns>An array containing the bytes read from the process.</returns>
		public byte[] ReadMemory(IntPtr addr, int count)
		{
			var buf = new byte[count];
			ReadMemory(addr, buf, 0, count);
			return buf;
		}

		/// <summary>
		/// Reads an integer from the process.
		/// </summary>
		/// <param name="addr">The address to read from.</param>
		/// <returns>The integer read from the process.</returns>
		public int ReadMemoryInt(IntPtr addr)
		{
			return BitConverter.ToInt32(ReadMemory(addr, 4), 0);
		}

		/// <summary>
		/// Writes the specified array of bytes to the process.
		/// </summary>
		/// <param name="addr">The address to write to.</param>
		/// <param name="buf">The array containing the bytes to write to the process.</param>
		/// <param name="offset">The start offset in buf.</param>
		/// <param name="count">The number of bytes to write.</param>
		public void WriteMemory(IntPtr addr, byte[] buf, int offset, int count)
		{
			ReadOrWriteMemory(addr, buf, offset, count, WriteProcessMemory, "WriteProcessMemory failed");
		}

		/// <summary>
		/// Writes the specified array of bytes to the process.
		/// </summary>
		/// <param name="addr">The address to write to.</param>
		/// <param name="buf">The array containing the bytes to write to the process.</param>
		public void WriteMemory(IntPtr addr, byte[] buf)
		{
			WriteMemory(addr, buf, 0, buf.Length);
		}

		public void FlushInstructionCache()
		{
			if (!WinApi.FlushInstructionCache(handle, IntPtr.Zero, IntPtr.Zero))
			{
				throw new Win32Exception("FlushInstructionCache failed");
			}
		}
	}
}
