using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace cheat_blocker_gtasa
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			// Show process selection
			Process process;
			using (var form = new ProcessSelectionForm("gta_sa.exe", "gta-sa.exe"))
			{
				Application.Run(form);
				process = form.SelectedProcess;
			}
			if (process == null) return;

			// Create LocalProcess from Process object
			LocalProcess localProcess;
			try
			{
				localProcess = new LocalProcess(process, true);
			}
			catch
			{
				process.Dispose();
				throw;
			}

			// Enable cheat blocker
			using (localProcess)
			{
				try
				{
					CheatBlockerSA.Enable(localProcess);
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}
	}
}
