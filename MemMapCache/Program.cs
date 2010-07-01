using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net;
using System.Net.Sockets;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;

namespace MemMapCache
{
	class Program
	{
		private static Dictionary<string, MemoryMappedFile> _files = new Dictionary<string, MemoryMappedFile>();

		public static void Main(string[] args) {
			Console.WriteLine("Starting MemMapCache...");
			var server = new TcpListener(IPAddress.Parse("127.0.0.1"), 57742);

			var task = Task.Factory.StartNew(() =>
			{
				server.Start();

				while (true) {
					Console.WriteLine("Listening...");

					var client = server.AcceptTcpClient();
					Task.Factory.StartNew(() =>
					{
						var buf = new byte[4096];
						var ns = client.GetStream();

						try {
							ns.Read(buf, 0, buf.Length);

							string key = ASCIIEncoding.ASCII.GetString(buf);
							key = key.Trim();

							var mmf = MemoryMappedFile.OpenExisting(key);
							if (_files.ContainsKey(key))
								_files.Add(key, mmf);
							else 
								_files[key] = mmf;

							Console.WriteLine("Key: " + key);
						}
						catch (Exception ex) {
							Console.WriteLine(ex.Message);
						}
					});
				}

			}, TaskCreationOptions.LongRunning);

			Task.WaitAny(task);
		}
	}
}
