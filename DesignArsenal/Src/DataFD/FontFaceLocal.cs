/*using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using SharpFont;
using FontDrop.Data;

namespace DesignArsenal.DataFD
{
	class FontFaceFamilyLocal
	{
		private static Library _library = new Library();
		private static IDictionary<string, FontFaceFamilyLocal> _instances = new ConcurrentDictionary<string, FontFaceFamilyLocal>();

		public readonly Library _selflibrary;
		public readonly string _family_name;
		public readonly ConcurrentDictionary<string, FaceVariant> _variants = new ConcurrentDictionary<string, FaceVariant>();

		private FontFaceFamilyLocal(Face face)// private constructor
		{
			_family_name = face.FamilyName;
			_instances[_family_name] = this;
			_selflibrary = _library;
		}

		// Factory method: create a FontFaceLocal from local file
		public static FontFaceFamilyLocal CreateByLocalFile(string path)
		{
			Face face;
			try
			{
				var bytes = File.ReadAllBytes(path);
				face = new Face(_library, bytes, 0);
			}
			catch(Exception ex)
			{
				Debug.WriteLine(ex.Message);
				_library = new Library();
				return null;
			}

			if(_instances.ContainsKey(face.FamilyName))
			{
				FontFaceFamilyLocal flcache = _instances[face.FamilyName];
				if(flcache._variants.ContainsKey(face.StyleName))
					return flcache;

				flcache.InternalLoadVariant(face);
				return flcache;
			}

			FontFaceFamilyLocal fl = new FontFaceFamilyLocal(face);
			fl.InternalLoadVariant(face);
			//face.Dispose();
			return fl;
		}

		private void InternalLoadVariant(Face face)
		{
			_variants[face.StyleName] = new FaceVariant(face.FamilyName, face.StyleName)
			{
				_face = face
			};
		}

		public bool IsVariantLoaded(string variant)
		{
			return _variants.ContainsKey(variant) && _variants[variant]._face != null;
		}
	}
}*/