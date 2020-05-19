using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Web;

namespace JoinerCache
{
	public static class App
	{
		public static readonly string FontsDir = Path.GetFullPath(Environment.CurrentDirectory + @"\..\..\..\NoGIT\FontCache\");
		
		public static void NotifyInternetFault()
		{
#if !DEBUG
			MailMessage message = new MailMessage();
			message.To.Add("ramon@misoftware.rs");
			message.Subject = "MI Software - NotifyInternetFault";
			message.From = new MailAddress("no-reply@misoftware.com.br");
			message.Body = Environment.StackTrace;

			Utils.SendMail(message);
#endif
		}
	}
}