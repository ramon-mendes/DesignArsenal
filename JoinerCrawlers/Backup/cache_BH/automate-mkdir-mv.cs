using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

class Script
{

	[STAThread]
	static public void Main(string[] args)
	{
#if !DEBUG
		Console.WriteLine("Compile in DEBUG!");
		Console.ReadLine();
		return;
#endif

		if(args.Length==0)
			return;

		string file = args[0];
		string ext = Path.GetExtension(file);
		Debug.Assert(ext == ".ttf" || ext == ".otf", "Wrong file extension:" + ext);


		// ### Create folder - query user
		Console.WriteLine("### Create folder");
		Console.WriteLine("Font name/dir?");
		string name = Path.GetFileNameWithoutExtension(file);
		SendKeys.SendWait(name);
		name = Console.ReadLine();
		
		// Create folder and move file
		string dir = Environment.CurrentDirectory + '\\' + name + '\\';
		Directory.CreateDirectory(name);

		string newfile = dir + Path.GetFileName(file);
		File.Move(file, newfile);
		Console.WriteLine("Moved to: " + newfile);


		// ### Manifest - query user
		Console.WriteLine();
		Console.WriteLine("### Manifest");

		Console.WriteLine("Font-face internal name?");
		SendKeys.SendWait(name);
		string facename = Console.ReadLine();

		Console.WriteLine("URL?");
		string url = Console.ReadLine();

		Console.WriteLine("Style?");
		string[] categorys = new string[] { "serif", "sans-serif", "display", "handwriting", "monospace", "brush" };// as in D:\MI_MVC\FCCache\BHAPI.cs
		for(int i = 0; i < categorys.Length; i++)
			Console.WriteLine(i + " - " + categorys[i]);

		string line = Console.ReadLine().TrimEnd('\n', '\r');
		int icat = int.Parse(line);
		string cat = categorys[icat];

		// Create manifest.json
		const string MANIFEST = @"{{
	name: ""{0}"",
	category: ""{1}"",
	url_gallery: ""{2}"",
	styles: [
		[""Regular"", ""{3}"", ""{4}""]
	]
}}";
		string txtmanifest = string.Format(MANIFEST, name, cat, url, Path.GetFileName(file), facename);
		File.WriteAllText(dir + "manifest.json", txtmanifest);

		Process.Start("explorer", dir);
	}
}