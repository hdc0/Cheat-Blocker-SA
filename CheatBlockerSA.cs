using System;
using System.Collections.Generic;

namespace cheat_blocker_gtasa
{
	static class CheatBlockerSA
	{
		private class Addresses
		{
			/// <summary>
			/// Address of block in CPad::AddToCheatString that updates _cheatString.
			/// </summary>
			public int UpdateCheatString { get; private set; }

			/// <summary>
			/// Address of block in CPad::AddToCheatString that gets
			/// executed when _cheatString's hash matches a cheat.
			/// </summary>
			public int CheatActivated { get; private set; }

			/// <summary>
			/// Address of epilogue block in CPad::AddToCheatString.
			/// </summary>
			public int Epilogue { get; private set; }

			/// <summary>
			/// Address of _cheatString.
			/// </summary>
			public int CheatString { get; private set; }

			/// <summary>
			/// Address of _currKeyState.standardKeys.
			/// </summary>
			public int StandardKeys { get; private set; }

			public Addresses(int updateCheatString, int cheatActivated,
				int epilogue, int cheatString, int standardKeys)
			{
				UpdateCheatString = updateCheatString;
				CheatActivated = cheatActivated;
				Epilogue = epilogue;
				CheatString = cheatString;
				StandardKeys = standardKeys;
			}
		}

		/// <summary>
		/// Enables cheat blocker for the specified process.
		/// </summary>
		/// <param name="process">The target process.</param>
		public static void Enable(LocalProcess process)
		{
			var addrs = GetAddresses(process);

			// Replaces _cheatString with backup and marks the
			// key passed to AddToCheatString as "not pressed".
			var blockCheat = new byte[]
			{
				0xBF, 0xCC, 0xCC, 0xCC, 0xCC,                               // mov edi, 0XXXXXXXXh                // memcpy destination
				0xBE, 0xCC, 0xCC, 0xCC, 0xCC,                               // mov esi, 0XXXXXXXXh                // memcpy source
				0xB9, 0x1E, 0x00, 0x00, 0x00,                               // mov ecx, 30                        // memcpy size
				0xF3, 0xA4,                                                 // rep movsb                          // memcpy
				0x0F, 0xB6, 0x7C, 0x24, 0x30,                               // movzx edi, byte ptr [esp+30h]      // edi = "character" parameter
				0x66, 0xC7, 0x04, 0x7D, 0xCC, 0xCC, 0xCC, 0xCC, 0x00, 0x00, // mov word ptr [edi*2+0XXXXXXXXh], 0 // _currKeyState.standardKeys[character] = 0
				0xEB, 0xCC                                                  // jmp rel8 0XXh                      // Jump to epilogue
			};

			// Creates backup of _cheatString.
			var backupCheatString = new byte[]
			{
				0x57,                         // push edi            // Save edi
				0x56,                         // push esi            // Save esi
				0xBF, 0xCC, 0xCC, 0xCC, 0xCC, // mov edi, 0XXXXXXXXh // memcpy destination
				0xBE, 0xCC, 0xCC, 0xCC, 0xCC, // mov esi, 0XXXXXXXXh // memcpy source
				0xB9, 0x1E, 0x00, 0x00, 0x00, // mov ecx, 30         // memcpy size
				0xF3, 0xA4,                   // rep movsb           // memcpy
				0x5E,                         // pop esi             // Restore esi
				0x5F,                         // pop edi             // Restore edi
				0xB8, 0x1C, 0x00, 0x00, 0x00, // mov eax, 1Ch        // Restore overwritten instruction
				0xC3                          // ret
			};

			// Allocate memory for backup of _cheatString
			int cheatStringBackupAddr = process.AllocateMemory(30).ToInt32();

			// Configure blockCheat code

			// Set memcpy dest to _cheatString and src to backup
			BitConverter.GetBytes(addrs.CheatString).CopyTo(blockCheat, 1);
			BitConverter.GetBytes(cheatStringBackupAddr).CopyTo(blockCheat, 6);
			// Set address of _currKeyState.standardKeys
			BitConverter.GetBytes(addrs.StandardKeys).CopyTo(blockCheat, 26);
			// Set jump target to function epilogue
			blockCheat[blockCheat.Length - 1] =
				(byte)(addrs.Epilogue - (addrs.CheatActivated + blockCheat.Length));
			// Copy code to process
			process.WriteMemory(new IntPtr(addrs.CheatActivated), blockCheat);

			// Configure backupCheatString code

			// Set memcpy dest to backup and src to _cheatString
			int backupCheatStringAddr = addrs.CheatActivated + blockCheat.Length;
			BitConverter.GetBytes(cheatStringBackupAddr).CopyTo(backupCheatString, 3);
			BitConverter.GetBytes(addrs.CheatString).CopyTo(backupCheatString, 8);
			// Copy code to process
			process.WriteMemory(new IntPtr(backupCheatStringAddr), backupCheatString);

			// Call code that backs up _cheatString when it is about to be modified
			WriteCallRel32(process, addrs.UpdateCheatString,
				backupCheatStringAddr - addrs.UpdateCheatString);
		}

		// Tries to identify the GTA version of the process and returns the corresponding addresses.
		// Throws an exception if the version is unknown.
		private static Addresses GetAddresses(LocalProcess process)
		{
			// Create address dictionary
			var versions = new Dictionary<GtaVersion, Addresses>
			{
				{ GtaVersion.SA_1_0,     new Addresses(0x438493, 0x438530, 0x4385A3, 0x969110, 0xb731A8) },
				{ GtaVersion.SA_1_01,    new Addresses(0x438513, 0x4385B0, 0x438623, 0x96B790, 0xB755B8) },
				{ GtaVersion.SA_1_01_PL, new Addresses(0x43B672, 0x43B724, 0x43B798, 0x9DEA38, 0xBE8300) }
			};

			// Try to find the addresses corresponding to the process' GTA version
			if (!versions.TryGetValue(GtaVersions.Detect(process), out Addresses addrs))
			{
				throw new Exception("Unsupported GTA version");
			}
			return addrs;
		}

		private static void WriteCallRel32(LocalProcess process, int addr, int relativeTargetAddr)
		{
			var bytes = new byte[5];
			bytes[0] = 0xE8; // call rel32
			BitConverter.GetBytes(relativeTargetAddr - bytes.Length).CopyTo(bytes, 1);
			process.WriteMemory(new IntPtr(addr), bytes);
		}
	}
}
