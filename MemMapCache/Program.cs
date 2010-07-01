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
		private static string DELIM = "[!@#]";

		private static Dictionary<string, KeyValuePair<MemoryMappedFile, DateTime>> _files = new Dictionary<string, KeyValuePair<MemoryMappedFile, DateTime>>();

		private static object _fileLock = new object();

		public static void Main(string[] args) {
			Console.WriteLine("Starting MemMapCache...");
			var server = new TcpListener(IPAddress.Parse("127.0.0.1"), 57742);

			var task = Task.Factory.StartNew(() => {
				server.Start();

				while (true) {
					Console.WriteLine("Listening...");

					var client = server.AcceptTcpClient();
					Task.Factory.StartNew(() => {
						var buf = new byte[4096];
						var ns = client.GetStream();

						while (client.Connected) {
							try {
								ns.Read(buf, 0, buf.Length);

								string input = ASCIIEncoding.ASCII.GetString(buf);
								input = input.TrimEnd('\0');

								var data = input.Split(new string[] { DELIM }, StringSplitOptions.None);
								var key = data[0].Trim();
								if (key != "") {
									var mmf = MemoryMappedFile.OpenExisting(key);

									DateTime? dt = null;
									if (data.Length > 1) {
										dt = Convert.ToDateTime(data[1].Trim());
										AddKey(key, mmf, dt.Value);
									}
									else
										AddKey(key, mmf);

									Console.WriteLine("Key: " + key);
									ClearBuffer(buf);
								}
								else
									client.Close();
							}
							catch (Exception ex) {
								Console.WriteLine(ex.Message);
							}
						}
						client.Close();
					});
				}

			}, TaskCreationOptions.LongRunning);

			task.Wait();
		}

		private static void AddKey(string key, MemoryMappedFile mmf) {
			AddKey(key, mmf, DateTime.MaxValue);
		}

		private static void AddKey(string key, MemoryMappedFile mmf, DateTime dt) {
			lock (_fileLock){
				if (!_files.ContainsKey(key)) {
					var kvp = new KeyValuePair<MemoryMappedFile, DateTime>(mmf, dt);
					_files.Add(key, kvp);
				}
				else {
					_files[key] = new KeyValuePair<MemoryMappedFile, DateTime>(mmf, dt);
				}
			}
		}

		private static void ClearBuffer(byte[] buf) {
			for (int x = 0; x < buf.Length; ++x)
				buf[x] = 0;
		}
	}
}
