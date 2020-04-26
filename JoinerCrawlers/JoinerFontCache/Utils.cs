using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JoinerCache
{
	static class Utils
	{
		public static T DeserializeBSON<T>(this byte[] dataBSON)
		{
			using(MemoryStream ms = new MemoryStream(dataBSON))
			{
				using(BsonReader reader = new BsonReader(ms))
				{
					return new JsonSerializer().Deserialize<T>(reader);
				}
			}
		}

		public static byte[] SerializeBSON(this object data)
		{
			using(MemoryStream ms = new MemoryStream())
			{
				using(BsonWriter writer = new BsonWriter(ms))
				{
					new JsonSerializer().Serialize(writer, data);
					return ms.ToArray();
				}
			}
		}


		private static Random rng = new Random();

		public static IList<T> Shuffle<T>(this IList<T> list)
		{
			int n = list.Count;
			while(n > 1)
			{
				n--;
				int k = rng.Next(n + 1);
				T value = list[k];
				list[k] = list[n];
				list[n] = value;
			}
			return list;
		}

		public static T RetryPattern<T>(Func<T> f, string error_msg)// throws after 10 attemps fails
		{
			Exception last_ex = null;
			for(int i = 0; i < 10; i++)
			{
				try
				{
					return f();
				}
				catch(Exception ex)
				{
					last_ex = ex;
					Thread.Sleep(TimeSpan.FromSeconds(2));
				}
			}
			throw new Exception(error_msg, last_ex);
		}

		public static Task<T> RetryPatternAsync<T>(Func<T> f, string error_msg)// throws after 10 attemps fails
		{
			return Task.Run(() =>
			{
				Exception last_ex = null;
				for(int i = 0; i < 10; i++)
				{
					try
					{
						return f();
					}
					catch(Exception ex)
					{
						last_ex = ex;
						Thread.Sleep(TimeSpan.FromSeconds(2));
					}
				}
				throw new Exception(error_msg, last_ex);
			});
		}

		public static void SendMailLogException(Exception ex)
		{
			Debug.Assert(false);
		}
	}

	public class StopwatchAuto : Stopwatch
	{
		public StopwatchAuto()
		{
			Start();
		}

		public string StopAndLog(string what = null)
		{
			Stop();

			if(what == null)
			{
				StackTrace stackTrace = new StackTrace();
				what = stackTrace.GetFrame(1).GetMethod().Name + "()";
			}
			//Utils.DebugOutputString(what + " took " + ElapsedMilliseconds + "ms");
			return what + " took " + ElapsedMilliseconds + "ms";
		}

		/*public string StopAndLogRelease(string what = null)
		{
			Stop();

			if(what == null)
			{
				StackTrace stackTrace = new StackTrace();
				what = stackTrace.GetFrame(1).GetMethod().Name + "()";
			}
			Utils.ReleaseOutputString(what + " took " + ElapsedMilliseconds + "ms");
		}*/
	}
}
