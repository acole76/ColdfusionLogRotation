using ArgumentParser;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;

namespace ColdfusionLogRotation
{
	class Program
	{
		public static string baseUrl = "";
		public static WebProxy proxy = new WebProxy();
		public static CookieContainer cookieContainer = new CookieContainer();
		public static BetterWebClient wc = new BetterWebClient(cookieContainer, false);

		static void Main(string[] args)
		{
			ArgParse argparse = new ArgParse
			(
					new ArgItem("base-url", "b", true, "The base url of the coldfusion administrator.", "", ArgParse.ArgParseType.Url),
					new ArgItem("password-hash", "p", true, "The SHA1 hash of the administrator password.", "", ArgParse.ArgParseType.String),
					new ArgItem("web-proxy", "w", false, "The proxy server url. ex: http://127.0.0.1:8080", "", ArgParse.ArgParseType.String)
			);

			argparse.parse(args);

			baseUrl = argparse.Get<string>("base-url");
			string passwordHash = argparse.Get<string>("password-hash");
			string webProxyString = argparse.Get<string>("web-proxy");

			if(webProxyString != null && webProxyString.Length > 0)
			{
				wc.Proxy = proxy;
			}

			Login(passwordHash);
			List<string> urls = GetArchiveUrls();

			foreach (string url in urls)
			{
				ArchiveLog(url);
			}
		}

		public static List<string> GetArchiveUrls()
		{
			List<string> urls = new List<string>();
			string response = wc.DownloadString(string.Format("{0}/CFIDE/administrator/logging/index.cfm", baseUrl));

			string regex = "archiveexecute.cfm\\?([^\"]+)";
			MatchCollection matches = Regex.Matches(response, regex);

			Console.WriteLine("[+] {0} log files found.", matches.Count);

			foreach (Match match in matches)
			{
				urls.Add(match.Groups[0].Value);
			}

			return urls;
		}

		public static void ArchiveLog(string url)
		{
			Console.WriteLine("[+] Archiving: {0}", url);
			wc.DownloadString(string.Format("{0}/CFIDE/administrator/logging/{1}", baseUrl, url));
		}

		public static void Login(string passwordHash)
		{
			try
			{
				wc.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
				wc.AutoRedirect = false;
				wc.UploadString(string.Format("{0}/CFIDE/administrator/enter.cfm", baseUrl), string.Format("cfadminPassword={0}&requestedURL=%2FCFIDE%2Fadministrator%2Findex.cfm&submit=Login", passwordHash));

				if (wc.StatusCode != HttpStatusCode.Found)
				{
					if (wc.StatusCode == HttpStatusCode.OK)
					{
						Console.WriteLine("[!] Login Failed, check your credentials.");
					}

					if (wc.StatusCode == HttpStatusCode.Forbidden)
					{
						Console.WriteLine("[!] CFAdmin can not be reached.  Make sure the colfusion administrator is accessible.");
					}

					Environment.Exit(0);
				}

				Console.WriteLine("[+] Login Successful.");
			}
			catch(Exception e)
			{
				Console.WriteLine("[!] {0}", e.Message);
				Environment.Exit(0);
			}
		}
	}
}