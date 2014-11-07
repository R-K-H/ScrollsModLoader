using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Mono.Cecil;
using Mono.Cecil.Cil;
using LinFu.AOP.Cecil.Extensions;
using LinFu.Reflection.Emit;
using UnityEngine;

namespace ScrollsModLoader
{
	class Patcher
	{

		/*
		 *  Actual patcher class
		 *  Makes use of Mono.Cecil and Linfu
		 * 
		 *  May be called directly via Main for first time patches
		 *  or from the inside of modloader to adjust(re-patch) the (backup)assembly for new game/mod/modloader updates
		 * 
		 */

		public List<String> modPaths = new List<String>();

		public static void Main (string[] args)
		{
			try {
				Patcher.standalonePatch();
			} catch {
				Dialogs.showNotification ("Patching failed", "Scrolls Summoner was unable to prepare your client, make sure Scrolls is not running while installing. More info at scrollsguide.com/summoner");
			}
		}

		public static void standalonePatch () {
			bool writetofile = (Platform.getOS () == Platform.OS.Mac);

			Console.WriteLine ("Preparing...");
			if(writetofile) Platform.ErrorLog ("Preparing...");

			//get Path of Scrolls Data Folder
			String installPath = Platform.getGlobalScrollsInstallPath();
			String modloaderpath = Platform.getModLoaderPath();
			if (installPath == null) return;
			Console.WriteLine ("installpath: " +installPath);
			if(writetofile) Platform.ErrorLog ("installpath: " +installPath);
			Console.WriteLine ("Creating ModLoader folder...");
			if(writetofile) Platform.ErrorLog("Creating ModLoader folder...");
			//create modloader folder
			if (!System.IO.Directory.Exists(modloaderpath)) {
				System.IO.Directory.CreateDirectory(modloaderpath);
			}

			Console.WriteLine ("Backup/Reset assembly...");
			if(writetofile) Platform.ErrorLog("Backup/Reset assembly...");

			//backup original assembly
			if (!System.IO.File.Exists(modloaderpath+ System.IO.Path.DirectorySeparatorChar +"Assembly-CSharp.dll"))
				System.IO.File.Copy (installPath+"Assembly-CSharp.dll", modloaderpath + System.IO.Path.DirectorySeparatorChar +"Assembly-CSharp.dll");
			else {
			//if a backup already exists, it is much more likely that the current assembly is messed up and the user wants to repatch
				System.IO.File.Delete(installPath+"Assembly-CSharp.dll");
				System.IO.File.Copy(modloaderpath + System.IO.Path.DirectorySeparatorChar +"Assembly-CSharp.dll", installPath+"Assembly-CSharp.dll");
			}

			Console.WriteLine ("Copying ModLoader.dll...");
			if(writetofile) Platform.ErrorLog("Copying ModLoader.dll...");
			//copy modloader for patching
			if (System.IO.File.Exists(installPath+"ScrollsModLoader.dll"))
				System.IO.File.Delete(installPath+"ScrollsModLoader.dll");
			System.IO.File.Copy(System.Reflection.Assembly.GetExecutingAssembly().Location, installPath+"ScrollsModLoader.dll");

			//reset ini
			if (System.IO.File.Exists(modloaderpath+ System.IO.Path.DirectorySeparatorChar +"mods.ini"))
				System.IO.File.Delete(modloaderpath+ System.IO.Path.DirectorySeparatorChar +"mods.ini");
				
			Console.WriteLine("Create shortcut...");
			if(writetofile) Platform.ErrorLog("Create shortcut...");
			string ddsc = System.IO.Path.DirectorySeparatorChar+"";

			String apppath = "";
			if(Platform.getOS() == Platform.OS.Win) apppath = installPath.Split (new string[]{ "Scrolls_Data" +  System.IO.Path.DirectorySeparatorChar}, StringSplitOptions.RemoveEmptyEntries) [0];
			if(Platform.getOS() == Platform.OS.Mac) apppath = installPath.Split (new string[]{ "Data" +  System.IO.Path.DirectorySeparatorChar}, StringSplitOptions.RemoveEmptyEntries) [0] + "MacOS" + ddsc;
			String fpath = installPath.Split (new string[]{ "game" +  System.IO.Path.DirectorySeparatorChar + "versions"}, StringSplitOptions.RemoveEmptyEntries) [0];
			if (Platform.getOS () == Platform.OS.Mac) fpath = installPath.Split (new string[]{ "versions" +  System.IO.Path.DirectorySeparatorChar + "version-"}, StringSplitOptions.RemoveEmptyEntries) [0];

			string args = "--assetsDir \""+ fpath +"game"+ddsc+"assets"+ddsc+"objects\" --assetIndex \""+fpath+ "game"+ddsc+"assets"+ddsc+"indexes"+ddsc+"index-133-production-win.json\"";
			if(Platform.getOS() == Platform.OS.Mac) args = "--assetsDir \""+ fpath + "assets"+ddsc+"objects\" --assetIndex \""+fpath+"assets"+ddsc+"indexes"+ddsc+"index-133-production-osx.json\"";

			String filetxt = "";
			String filetxt2 = "";
			switch (Platform.getOS ()) 
			{
			case Platform.OS.Win:
				apppath += "Scrolls.exe";
				fpath += "summoner.bat";
				filetxt = "START \"\" \"" + apppath + "\" " + args;
				break;
			case Platform.OS.Mac:
				apppath +="Scrolls";
				fpath = System.IO.Directory.GetParent (System.Reflection.Assembly.GetExecutingAssembly ().Location).ToString ().Replace( "Summoner.app" + ddsc + "Contents" + ddsc + "MacOS", "") + "summoner.command";
				filetxt ="\"" + apppath + "\" " + args; //+path to scrolls.exe + arguments!
				filetxt2 ="#!/bin/bash\r\n\"" + apppath + "\" " + args; //+path to scrolls.exe + arguments!
				break;
			default:
				break;
			}
			System.IO.File.WriteAllText (fpath, filetxt);

			if (Platform.getOS () == Platform.OS.Mac) //platform specific patch :D (need this to restart scrolls!)
			{
				//make .command executeable
				new Process { StartInfo = { FileName = "chmod", Arguments = "u+x " + "\"" +fpath + "\"", UseShellExecute=true } }.Start ();

				fpath = installPath.Split (new string[]{ "versions" + System.IO.Path.DirectorySeparatorChar + "version-" }, StringSplitOptions.RemoveEmptyEntries) [0] + "summoner.sh";
				System.IO.File.WriteAllText (fpath, filetxt2);
			}




			Console.WriteLine ("Patching...");
			if(writetofile) Platform.ErrorLog("Patching...");
			//patch it
			Patcher patcher = new Patcher();
			if (!patcher.patchAssembly(installPath)) {
				Console.WriteLine("Patching failed");
				if(writetofile) Platform.ErrorLog("Patching failed");
				//don't safe patch at this point. If the "real" patcher fails, we should tell the user instead
				//save-patching is for installs, that get broken by updates, etc, to keep the install until ScrollsModLoader is updated
				Dialogs.showNotification ("Patching failed", "Scrolls Summoner was unable to prepare your client, you are likely using an incompatible version. More at scrollsguide.com");
				return;
			}




			Dialogs.showNotification ("Patching complete", "Summoner successfully patched your Scrolls installation. Visit scrollsguide.com/summoner for more information. You can now start Scrolls and enjoy the benefits of Summoner. Warning: fullscreen users may have to manually restart the game");
			Console.WriteLine ("Done");
			return;
		}


		public bool patchAssembly(String installPath) {
			if (installPath == null) return false;
			bool writetofile = (Platform.getOS () == Platform.OS.Mac);
			if(writetofile) Platform.ErrorLog ("ModLoader Hooks:");
			//"weave" the assembly
			Console.WriteLine ("------------------------------");
			Console.WriteLine ("ModLoader Hooks:");
			ScrollsFilter.Log ();
			Console.WriteLine ("------------------------------");

			if (!weaveAssembly (installPath+"Assembly-CSharp.dll"))
				return false;
			Console.WriteLine ("Weaved Assembly");
			if(writetofile) Platform.ErrorLog ("Weaved Assembly");

			/*
			 * add init hack
			 */

			try {
				//load assembly
				Hooks.loadBaseAssembly(installPath+"Assembly-CSharp.dll");
				//load self
				Hooks.loadInjectAssembly(installPath+"ScrollsModLoader.dll");
			} catch (Exception exp) {
				//something must be gone horribly wrong if it crashes here
				Console.WriteLine (exp);
				return false;
			}
			if(writetofile) Platform.ErrorLog ("loaded assembly + self");

			//add hooks
			if (!Hooks.hookStaticVoidMethodAtEnd ("App.Awake", "ModLoader.Init"))
				return false;

			if(writetofile) Platform.ErrorLog ("added hocks");

			try {

				//save assembly
				Console.WriteLine ("Write back patched bytecode...");
				if(writetofile) Platform.ErrorLog ("Write back patched bytecode...");
				Hooks.savePatchedAssembly();

				Console.WriteLine ("Platform specific patches...");
				if(writetofile) Platform.ErrorLog ("Platform specific patches...");
				Platform.PlatformPatches(installPath);
				
			} catch (Exception exp) {

				//also very unlikely, but for safety
				Console.WriteLine (exp);
				if(writetofile) Platform.ErrorLog (exp.ToString());
				return false;

			}

			return true;
		}

		public bool weaveAssembly(String path) {
			// let LinFu inject some call hooks into all required classes and methods to replace/extend method calls
			try {
				AssemblyDefinition assembly = AssemblyFactory.GetAssembly(path);
				assembly.InterceptMethodBody (new ScrollsFilter(), new ScrollsFilter());
				//assembly.InterceptMethodBody(new ScrollsFilter().ShouldWeave);
				assembly.Save(path);
				return true;
			} catch (Exception exp) {
				Console.WriteLine (exp);
				return false;
			}
		}

		public bool safeModePatchAssembly() {
			String installPath = Platform.getGlobalScrollsInstallPath();
			if (installPath == null) return false;

			try {
				//load assembly
				Hooks.loadBaseAssembly(installPath+"Assembly-CSharp.dll");
				//load self
				Hooks.loadInjectAssembly(installPath+"ScrollsModLoader.dll");
			} catch (Exception exp) {
				//something must be gone horribly wrong if it crashes here
				Console.WriteLine (exp);
				return false;
			}

			if (!Hooks.hookStaticVoidMethodAtEnd ("App.Awake", "Patcher.safeLaunch"))
				return false;

			try {

				//save assembly
				Hooks.savePatchedAssembly();

				Platform.PlatformPatches(installPath);

			} catch (Exception exp) {

				//also very unlikely, but for safety
				Console.WriteLine (exp);
				return false;

			}

			return true;
		}

		public static void safeLaunch() {

			//if we get here, we NEED an update

			String installPath = Platform.getGlobalScrollsInstallPath();
			if (System.IO.File.Exists (installPath + System.IO.Path.DirectorySeparatorChar + "check.txt")) {
				System.IO.File.Delete (installPath + System.IO.Path.DirectorySeparatorChar + "check.txt");
				new Patcher ().patchAssembly (installPath);
			}
			if (Updater.tryUpdate()) { //updater did succeed
				System.IO.File.CreateText (installPath + System.IO.Path.DirectorySeparatorChar + "check.txt");
				Application.Quit ();
			}
		}
	}
}