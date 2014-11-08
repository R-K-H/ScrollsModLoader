using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ScrollsModLoader
{

	/*
	 * OS Specific Code
	 */

	public class Platform
	{
		public enum OS {
			Win,
			Mac,
			Unix,
			Unknown
		}

		public static OS getOS() {
			OperatingSystem os = Environment.OSVersion;
			PlatformID     pid = os.Platform;
			switch (pid) 
    		{
    			case PlatformID.Win32NT:
    			case PlatformID.Win32S:
    			case PlatformID.Win32Windows:
    			case PlatformID.WinCE:
					return OS.Win;
				case PlatformID.MacOSX:
				case PlatformID.Unix:
					try	{
						byte[] monoMacLoad = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("ScrollsModLoader.NativeAPIs.MonoMac.dll").ReadToEnd();
						System.Reflection.Assembly.Load(monoMacLoad);
					} catch (System.IO.FileLoadException) {
						return OS.Unix;
					}
					return OS.Mac;
				default:
					return OS.Unknown;
		
			}
		}

		public static bool written = false;

		public static void ErrorLog(string s)
		{
			string dsc = System.IO.Path.DirectorySeparatorChar+"";
			string path = System.IO.Directory.GetParent (System.Reflection.Assembly.GetExecutingAssembly ().Location).ToString ().Replace( "Summoner.app" + dsc + "Contents" + dsc + "MacOS", "") + "summonerlog1.txt";
			if (!System.IO.File.Exists (path)) 
			{
				System.IO.File.WriteAllText (path, s+"\r\n");
			} else 
			{
				if (Platform.written) {
					System.IO.File.AppendAllText (path, s + "\r\n");
				} 
				else 
				{
					//System.IO.File.AppendAllText (path, s + "\r\n");
					System.IO.File.WriteAllText (path, s+"\r\n");
					Platform.written = true;
				}
			}
		}

		public static String ModsPath = null;
		public static String ModLoaderPath = null;
		public static String VersionNumber = null;
		public static String GlobalScrollsInstallPath = null;

		public static String getModsPath()
		{
			if (Platform.ModsPath != null)
				return Platform.ModsPath;

			String path = "";
			String up = ".." + Path.DirectorySeparatorChar + Path.DirectorySeparatorChar;
			path= Platform.getGlobalScrollsInstallPath();
			if (Platform.getOS () == Platform.OS.Win) path += up + up + up + up + up + "Mods";
			if (Platform.getOS () == Platform.OS.Mac) path += up + up + up + up + up + up + up + "Mods";
			Console.WriteLine ("mods: " + path);
			Platform.ErrorLog("mods: " + path);
			Platform.ModsPath = path;
			return path;
		}



		public static String getModLoaderPath()
		{
			if (Platform.ModLoaderPath != null)
				return Platform.ModLoaderPath;

			String path = "";
			path= Platform.getGlobalScrollsInstallPath()+ "ModLoader";
			Console.WriteLine ("modloader: " + path);
			Platform.ErrorLog("modloader: " + path);
			Platform.ModLoaderPath = path;
			return path;
		}


		public static String getVersionNumber()
		{
			if (Platform.VersionNumber != null)
				return Platform.VersionNumber;

			String globalScrollsInstallPath = Platform.getGlobalScrollsInstallPath ();
			String version = "0";
			if (globalScrollsInstallPath.Contains ("version-")) 
			{
				version = globalScrollsInstallPath.Split (new string[]{ "version-" }, StringSplitOptions.RemoveEmptyEntries) [1].Split ('-') [0];
			}

			Platform.VersionNumber = version;
			return version;
		}

		public static String getGlobalScrollsInstallPath() 
		{

			if (Platform.GlobalScrollsInstallPath != null)
				return Platform.GlobalScrollsInstallPath;

			String path = null;
			//if we are already loaded from the game folder, get that instead
			if ((from file in Directory.GetParent (System.Reflection.Assembly.GetExecutingAssembly ().Location).GetFiles ()
			     where file.Name.Contains ("Assembly-CSharp.dll")
			     select file).Count () > 0) 
			{
				Platform.GlobalScrollsInstallPath = Directory.GetParent (System.Reflection.Assembly.GetExecutingAssembly ().Location).ToString () + Path.DirectorySeparatorChar;
				Console.WriteLine ("GlobalScrollsInstallPath is : " + Platform.GlobalScrollsInstallPath);
				Platform.ErrorLog("GlobalScrollsInstallPath is : " + Platform.GlobalScrollsInstallPath);
				return Platform.GlobalScrollsInstallPath;
			}

			// we are located in ScrollsLauncher folder
			string searchpath = Directory.GetParent (System.Reflection.Assembly.GetExecutingAssembly ().Location).ToString () + Path.DirectorySeparatorChar + "game"+ Path.DirectorySeparatorChar;
			string dsc = Path.DirectorySeparatorChar +"";
			if (Platform.getOS () == Platform.OS.Mac) 
			{
				searchpath = Directory.GetParent (System.Reflection.Assembly.GetExecutingAssembly ().Location).ToString ().Replace ("Desktop" + dsc + "Summoner.app" + dsc + "Contents" + dsc + "MacOS", "");
				searchpath += "Library" + dsc + "Application Support" + dsc + "Scrolls" + dsc;
			}

			/*Platform.ErrorLog("path zero step : " + Directory.GetParent (searchpath));
			Platform.ErrorLog (searchpath + " contains this files:");
			foreach (FileInfo file in Directory.GetParent (searchpath).GetFiles ()) 
			{
				Platform.ErrorLog("found file: " + file.FullName);
			}*/

			Platform.ErrorLog("path zero step : " + Directory.GetParent (searchpath));

			if (Platform.getOS () == Platform.OS.Mac) {

				if ((from file in Directory.GetParent (searchpath).GetFiles ()
				     where file.Name.Contains ("launcher")
					select file).Count () == 0) 
				{
					//maybe we are in Scrolls folder?
					searchpath = Directory.GetParent (System.Reflection.Assembly.GetExecutingAssembly ().Location).ToString ();
				}
			}



			if ((from file in Directory.GetParent (searchpath).GetFiles ()
				where file.Name.Contains ("launcher")
				select file).Count () > 0) 
			{

				String folderpath = "";

				folderpath = Directory.GetParent (searchpath).ToString() +  Path.DirectorySeparatorChar + "versions" ;

				Console.WriteLine ("path first step : " + folderpath);
				Platform.ErrorLog("path first step : " + folderpath);

				if (Directory.Exists (folderpath)) 
				{
					//Console.WriteLine ("path exists");
					//search newest version
					String newestVersion = "0";
					foreach (String pathh in System.IO.Directory.GetDirectories(folderpath)) 
					{
						if (pathh.EndsWith ("-production"))  // change this for test builds!!!
						{
							String v = pathh.Split (Path.DirectorySeparatorChar) [pathh.Split (Path.DirectorySeparatorChar).Length - 1];
							v = v.Replace ("version-", "");
							v = v.Replace ("-production", ""); // change this for test builds!!!

							if (Convert.ToInt32 (v) > Convert.ToInt32 (newestVersion)) 
							{
								newestVersion = v;
							}
						}
					}
					folderpath +=  Path.DirectorySeparatorChar + "version-" + newestVersion + "-production";

					//search newest file!
					Console.WriteLine ("path second step : " + folderpath);
					Platform.ErrorLog("path second step : " + folderpath);

					String fullnewest = "version-" + newestVersion + "-production-natives-";

					String timestamp = "0";
					foreach (String pathh in System.IO.Directory.GetDirectories(folderpath)) 
					{
						String p = pathh.Split (Path.DirectorySeparatorChar) [pathh.Split (Path.DirectorySeparatorChar).Length - 1];
						if (p.StartsWith (fullnewest))  // change this for test builds!!!
						{
							String v = p.Replace (fullnewest, "");
							if (Convert.ToInt64 (v) > Convert.ToInt64 (timestamp)) 
							{
								timestamp = v;
							}
						}
					}

					if (Platform.getOS () == Platform.OS.Win) 
					{
						folderpath += Path.DirectorySeparatorChar + fullnewest + timestamp + Path.DirectorySeparatorChar + "Scrolls_Data" + Path.DirectorySeparatorChar + "Managed" + Path.DirectorySeparatorChar;
					}

					if (Platform.getOS () == Platform.OS.Mac) 
					{
						folderpath += Path.DirectorySeparatorChar + fullnewest + timestamp + Path.DirectorySeparatorChar + "MacScrolls.app" + Path.DirectorySeparatorChar + "Contents" + Path.DirectorySeparatorChar + "Data" + Path.DirectorySeparatorChar + "Managed" + Path.DirectorySeparatorChar;
					}
						

					Platform.GlobalScrollsInstallPath = folderpath;
					Console.WriteLine ("GlobalScrollsInstallPath is : " + Platform.GlobalScrollsInstallPath);
					Platform.ErrorLog("GlobalScrollsInstallPath is : " + Platform.GlobalScrollsInstallPath);
					return Platform.GlobalScrollsInstallPath;
				}



			}


			switch (Platform.getOS()) 
    		{
    			case Platform.OS.Win:
        			path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Mojang\\Scrolls\\game\\Scrolls_Data\\Managed\\";
					if (!System.IO.File.Exists(path+"Assembly-CSharp.dll")) {
						Console.WriteLine ("Expected Path: " + path);
						Dialogs.showNotification("Scrolls was not found", "Please select your local install of Scrolls");

						path = Dialogs.fileOpenDialog();
						if (path == null) {
							Dialogs.showNotification("No Selection was made", "Scrolls Summoner was not able to find your local install of Scrolls. Scrolls Summoner will close now");
							return null;
						}
						if (!System.IO.File.Exists(path+"\\Assembly-CSharp.dll")) {
							Dialogs.showNotification("Wrong Selection", "The selected file is not a valid Scrolls game folder. Scrolls Summoner will close now");
							return null;
						}
					}

					Platform.GlobalScrollsInstallPath = path;
					Console.WriteLine ("GlobalScrollsInstallPath is : " + Platform.GlobalScrollsInstallPath);
					Platform.ErrorLog("GlobalScrollsInstallPath is : " + Platform.GlobalScrollsInstallPath);
					return Platform.GlobalScrollsInstallPath;
				case Platform.OS.Mac:

					//Apps are bundles (== folders) on MacOS
					if (System.IO.Directory.Exists("/Applications/Scrolls.app")) {
						path = "/Applications/Scrolls.app/Contents/MacOS/game/MacScrolls.app/Contents/Data/Managed/";
						break;
					}
					
					// MacOS User needs to tell us the path of their Scrolls.app
					Dialogs.showNotification("Scrolls was not found", "Please select your local install of Scrolls");
					
					path = Dialogs.fileOpenDialog();
					if (path == null) {
						Dialogs.showNotification("No Selection was made", "Scrolls Summoner was not able to find your local install of Scrolls. Scrolls Summoner will close now");
						return null;
					}
					path += "/Contents/MacOS/game/MacScrolls.app/Contents/Data/Managed/";
					
					if (!System.IO.File.Exists(path+"Assembly-CSharp.dll")) {
						Dialogs.showNotification("Wrong Selection", "The selected file is not a valid Scrolls.app. Scrolls Summoner will close now");
						return null;
					}

				break;
    			default:
        			Console.WriteLine("Unsupported Platform detected");
					return null;
			}

			Platform.GlobalScrollsInstallPath = path;
			Console.WriteLine ("GlobalScrollsInstallPath is : " + Platform.GlobalScrollsInstallPath);
			Platform.ErrorLog("GlobalScrollsInstallPath is : " + Platform.GlobalScrollsInstallPath);
			return Platform.GlobalScrollsInstallPath;
		}

		public static void RestartGame() {
			//restart the game
			String installPath = Platform.getGlobalScrollsInstallPath();
			String fpath = "";
			if (Platform.getOS () == Platform.OS.Win) fpath = installPath.Split (new string[]{ "game" +  System.IO.Path.DirectorySeparatorChar + "versions"}, StringSplitOptions.RemoveEmptyEntries) [0];
			if (Platform.getOS () == Platform.OS.Mac) fpath = installPath.Split (new string[]{ "versions" +  System.IO.Path.DirectorySeparatorChar + "version-"}, StringSplitOptions.RemoveEmptyEntries) [0];

			string ddsc = System.IO.Path.DirectorySeparatorChar+"";
			string args = "--assetsDir \""+ fpath +"game"+ddsc+"assets"+ddsc+"objects\" --assetIndex \""+fpath+ "game"+ddsc+"assets"+ddsc+"indexes"+ddsc+"index-"+Platform.getVersionNumber()+"-production-win.json\"";
			if(Platform.getOS() == Platform.OS.Mac) args = "--assetsDir \""+ fpath + "assets"+ddsc+"objects\" --assetIndex \""+fpath+"assets"+ddsc+"indexes"+ddsc+"index-"+Platform.getVersionNumber()+"-production-osx.json\"";

			if (getOS () == OS.Win) {
				string filename = getGlobalScrollsInstallPath () + "..\\..\\Scrolls.exe";
				if (!System.IO.File.Exists (filename)) {
					//If the Scrolls.exe does not exist we search for a .exe, that has the same name as the _Data folder (Scrolls.exe -> Scrolls_Data, ScrollsTest.exe -> ScrollsTest_Data)
					DirectoryInfo dir = new DirectoryInfo (getGlobalScrollsInstallPath ());
					dir = dir.Parent.Parent; //Go from Scrolls/game/Scrolls_data/Managed to Scrolls/game
					DirectoryInfo[] dirs = dir.GetDirectories ("*_Data");
					if (dirs.Length == 1) {
						string name = dirs [0].Name;
						name = name.Replace ("_Data", ".exe");
						FileInfo[] files = dir.GetFiles(name);
						if (files.Length == 1) {
							filename = files [0].FullName;
						} else {
							//... We just accept, that the Process will fail...
						}
					} 
				}

				new Process { StartInfo = { FileName = filename, Arguments = args } }.Start ();
				Application.Quit ();
			} else if (getOS () == OS.Mac) {

				//new Process { StartInfo = { FileName = "bash", Arguments = getGlobalScrollsInstallPath() + "/../../../../../run_sleep.sh" + " " +args, UseShellExecute=true } }.Start ();
				Platform.ErrorLog ("##do: bash"  + " \"" +fpath + "summoner.sh\"" + " " +args);
				new Process { StartInfo = { FileName = "bash", Arguments = "\"" +fpath + "summoner.sh\"" + " " +args, UseShellExecute=true } }.Start ();

				Application.Quit ();
			} else {
				Application.Quit ();
			}
		}


		public static void PlatformPatches(String path) {
			if (getOS() == OS.Mac) {
				String runsh = path + "/../../../../../run.sh";
				String runshsleep = path + "/../../../../../run_sleep.sh";

				//new launcher doesnt has a run.sh
				/*if (!File.Exists (runshsleep)) {
					String[] lines = File.ReadAllLines (runsh);
					StreamWriter writer = File.CreateText (runshsleep);
					for (int i = 0; i < lines.Count(); i++) {
						String line = lines [i];
						if (line.Contains ("/installer") && !lines [i - 1].Contains ("sleep")) {
							writer.WriteLine ("sleep 2");
						}
						writer.WriteLine (line);
					}
					writer.Close ();
				}*/
				Platform.ErrorLog ("checking: " + path + "System.Drawing.dll");
				if (!File.Exists (path + System.IO.Path.DirectorySeparatorChar + "System.Drawing.dll")) {
					byte[] sysdrawing = System.Reflection.Assembly.GetExecutingAssembly ().GetManifestResourceStream ("ScrollsModLoader.Resources.System.Drawing.dll").ReadToEnd ();
					System.IO.File.Create (path +  "System.Drawing.dll").Write (sysdrawing, 0, sysdrawing.Length);
				}
			}
		}
	}
}

