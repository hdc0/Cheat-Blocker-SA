using System;

namespace cheat_blocker_gtasa
{
	enum GtaVersion
	{
		Unknown,
		SA_1_0,
		SA_1_01,
		SA_1_01_PL
	}

	static class GtaVersions
	{
		/// <summary>
		/// Tries to detect the version of GTA Vice City that is running in the specified process.
		/// </summary>
		/// <param name="process">The process to check.</param>
		/// <returns>The detected GTA version or GtaVersion.Unknown.</returns>
		public static GtaVersion Detect(LocalProcess process)
		{
			// Get size of main module
			int mainModuleSize;
			try
			{
				using (var mainModule = process.Process.MainModule)
				{
					mainModuleSize = mainModule.ModuleMemorySize;
				}
			}
			catch (Exception ex)
			{
				throw new Exception($"Cannot obtain main module size: {ex.Message}");
			}

			switch (mainModuleSize)
			{
				case 0x1177000: return GtaVersion.SA_1_0;
				case 0x20E0000: return GtaVersion.SA_1_01;
				case 0x092D000: return GtaVersion.SA_1_01_PL;
				default: return GtaVersion.Unknown;
			}
		}
	}
}
