using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DesignArsenal.DataFD;

namespace DesignArsenal.Apps
{
	static class XD
	{
		public static void CopyLayer(string family, string variant)
		{
			var ffj = Joiner.FFJ_ByNormalName(family);
			var psfamily = FaceVariant.GetPostScriptName(ffj, variant);

			StringBuilder sb = new StringBuilder();

			sb.Append("{");
			sb.Append("   \"version\":\"1.5.0\",");
			sb.Append("   \"children\":[");
			sb.Append("      {");
			sb.Append("         \"type\":\"text\",");
			sb.Append("         \"name\":\"test\",");
			sb.Append("         \"meta\":{");
			sb.Append("            \"ux\":{");
			sb.Append("               \"nameL10N\":\"SHAPE_GROUP\",");
			sb.Append("               \"sourceGuid\":\"517b266e-9a22-4319-a4f6-4193a9c85bd5\",");
			sb.Append("               \"singleTextObject\":true,");
			sb.Append("               \"rangedStyles\":[");
			sb.Append("                  {");
			sb.Append("                     \"length\":0,");
			sb.Append("                     \"linkId\":\"\",");
			sb.Append("                     \"fontFamily\":\"" + family + "\",");
			sb.Append("                     \"fontStyle\":\"" + variant + "\",");
			sb.Append("                     \"postscriptName\":\"" + psfamily + "\",");
			sb.Append("                     \"fontSize\":40,");
			sb.Append("                     \"charSpacing\":0,");
			sb.Append("                     \"underline\":false,");
			sb.Append("                     \"textTransform\":\"none\",");
			sb.Append("                     \"textScript\":\"none\",");
			sb.Append("                     \"fill\":{");
			sb.Append("                        \"value\":4278190080");
			sb.Append("                     },");
			sb.Append("                     \"strikethrough\":false");
			sb.Append("                  }");
			sb.Append("               ]");
			sb.Append("            }");
			sb.Append("         },");
			sb.Append("         \"transform\":{");
			sb.Append("            \"a\":1,");
			sb.Append("            \"b\":0,");
			sb.Append("            \"c\":0,");
			sb.Append("            \"d\":1,");
			sb.Append("            \"tx\":217,");
			sb.Append("            \"ty\":503");
			sb.Append("         },");
			sb.Append("         \"style\":{");
			sb.Append("            \"fill\":{");
			sb.Append("               \"type\":\"solid\",");
			sb.Append("               \"color\":{");
			sb.Append("                  \"mode\":\"RGB\",");
			sb.Append("                  \"value\":{");
			sb.Append("                     \"r\":255,");
			sb.Append("                     \"g\":255,");
			sb.Append("                     \"b\":255");
			sb.Append("                  }");
			sb.Append("               }");
			sb.Append("            },");
			sb.Append("            \"font\":{");
			sb.Append("               \"postscriptName\":\"" + psfamily+ "\",");
			sb.Append("               \"family\":\"" + family + "\",");
			sb.Append("               \"style\":\"" + variant + "\",");
			sb.Append("               \"size\":40");
			sb.Append("            }");
			sb.Append("         },");
			sb.Append("         \"text\":{");
			sb.Append("            \"frame\":{");
			sb.Append("               \"type\":\"positioned\"");
			sb.Append("            },");
			sb.Append("            \"paragraphs\":[");
			sb.Append("               {");
			sb.Append("                  \"lines\":[");
			sb.Append("                     [");
			sb.Append("                        {");
			sb.Append("                           \"y\":0,");
			sb.Append("                           \"from\":0,");
			sb.Append("                           \"to\":4,");
			sb.Append("                           \"x\":0");
			sb.Append("                        }");
			sb.Append("                     ]");
			sb.Append("                  ]");
			sb.Append("               }");
			sb.Append("            ],");
			sb.Append("            \"rawText\":\"" + family + "\"");
			sb.Append("         }");
			sb.Append("      }");
			sb.Append("   ],");
			sb.Append("   \"viewSource\":{");
			sb.Append("      \"x\":0,");
			sb.Append("      \"y\":0,");
			sb.Append("      \"width\":150,");
			sb.Append("      \"height\":150");
			sb.Append("   }");
			sb.Append("}");

#if WINDOWS
            ClipboardHelper.CopyCustomRawData("com.adobe.xd", Encoding.Unicode.GetBytes(sb.ToString()));
#else
            Utils.CopyCustomFormat("com.adobe.xd", sb.ToString());
#endif
		}
	}
}