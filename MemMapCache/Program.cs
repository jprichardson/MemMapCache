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
using System.Timers;


namespace MemMapCache
{
	class Program
	{
		private static string DELIM = "[!@#]";

		private static Dictionary<string, KeyValuePair<MemoryMappedFile, DateTime>> _files = new Dictionary<string, KeyValuePair<MemoryMappedFile, DateTime>>();
		private static SortedList<DateTime, Dictionary<string,string>> _expirationKeys = new SortedList<DateTime, Dictionary<string, string>>();

		private static object _fileLock = new object();
		private static object _keyListLock = new object();

		public static void Main(string[] args) {
			Console.WriteLine("Starting MemMapCache...");
			var server = new TcpListener(IPAddress.Parse("127.0.0.1"), 57742);

			var mainTask = Task.Factory.StartNew(() => {
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
										try {
											dt = Convert.ToDateTime(data[1].Trim());
										}
										catch (Exception ex) {
											Console.WriteLine(ex.Message);
											Console.WriteLine(data[1].Trim());
										}
										AddKey(key, mmf, dt.Value);
									}
									else
										AddKey(key, mmf);

									Console.Write("Key: " + key);
									if (dt != null)
										Console.WriteLine(" expires at: " + dt.Value.ToString("s"));
									else
										Console.WriteLine("");
									
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

			var timer = new Timer();
			timer.Elapsed += new ElapsedEventHandler(OnCleanCacheEvent);

			timer.Interval = 60000;
			timer.Start();

			Task.WaitAll(mainTask);
		}

		private static void AddKey(string key, MemoryMappedFile mmf) {
			AddKey(key, mmf, DateTime.MaxValue);
		}

		private static void AddKey(string key, MemoryMappedFile mmf, DateTime dt) {
			lock (_fileLock){
				if (!_files.ContainsKey(key)) {
					var kvp = new KeyValuePair<MemoryMappedFile, DateTime>(mmf, dt);
					_files.Add(key, kvp);
				} else {
					_files[key] = new KeyValuePair<MemoryMappedFile, DateTime>(mmf, dt);
				}
			}

			lock (_keyListLock) {
				if (!_expirationKeys.ContainsKey(dt)) {
					var d = new Dictionary<string, string>();
					_expirationKeys.Add(dt, d);
					d.Add(key, key);
				}
				else
					if (!_expirationKeys[dt].ContainsKey(key))
						_expirationKeys[dt].Add(key, key);
			}
		}

		private static void ClearBuffer(byte[] buf) {
			for (int x = 0; x < buf.Length; ++x)
				buf[x] = 0;
		}

		private static void OnCleanCacheEvent(object source, ElapsedEventArgs e) {
			Console.WriteLine("Cleaning Cache...");
			if (_expirationKeys.Count > 0) {
				var dt = _expirationKeys.Keys.First();
				if (DateTime.UtcNow > dt) {
					foreach (var key in _expirationKeys[dt].Keys) {
						lock (_fileLock) {
							var mmf = _files[key].Key;
							mmf.Dispose(); //kill it;
							_files.Remove(key);
							Console.WriteLine("Removed: " + key + " at " + dt.ToString("s"));
						}
					}

					lock (_keyListLock) {
						_expirationKeys.Remove(dt);
					}
				}
			}
			Console.WriteLine("Cache clean done.");
		}
	}
}
