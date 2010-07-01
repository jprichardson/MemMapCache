using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO.MemoryMappedFiles;
using System.Runtime.Serialization.Formatters.Binary;

namespace MemMapCacheLib
{
	public class MemMapCache
	{
		private static string DELIM = "[!@#]";

		private TcpClient _tcpClient;
		private NetworkStream _ns;
		private BinaryFormatter _bf;

		private Dictionary<string, DateTime> _keyExperations; //this is necessary because the lib still will hold refs to expired MMFs

		public MemMapCache() {
			this.Encoding = ASCIIEncoding.ASCII;
			this.ChunkSize = 1024 * 1024 * 10; //10MB

			this.Server = "127.0.0.1"; //limited to local
			this.Port = 57742;

			_keyExperations = new Dictionary<string, DateTime>();
		}

		public long ChunkSize { get; set; } 

		public Encoding Encoding { get; set; }

		public int MaxKeyLength { get { return 4096 - 32; } } //32 bytes for datetime string... it's an overkill i know

		public int Port { get; set; }
		
		public string Server { get; protected set; }

		public void Connect() {
			_tcpClient = new TcpClient();
			_tcpClient.Connect(this.Server, this.Port);
			_ns = _tcpClient.GetStream();
			_bf = new BinaryFormatter();
		}

		public T Get<T>(string key) {
			try {
				var mmf = MemoryMappedFile.OpenExisting(key);
				if (_keyExperations.ContainsKey(key)) {
					if (DateTime.UtcNow >= _keyExperations[key]) {
						mmf.Dispose();
						_keyExperations.Remove(key);
						return default(T);
					}
				}

				var vs = mmf.CreateViewStream();
				var o = _bf.Deserialize(vs);
				return (T)o;
			}
			catch (Exception ex) {
				if (_keyExperations.ContainsKey(key))
					_keyExperations.Remove(key);

				return default(T);
			}
		}

		public void Set<T>(string key, T obj) {
			this.Set<T>(key, obj, this.ChunkSize, DateTime.MaxValue);
		}

		public void Set<T>(string key, T obj, DateTime expire) {
			this.Set<T>(key, obj, this.ChunkSize, expire);
		}

		public void Set<T>(string key, T obj, long size) {
			this.Set<T>(key, obj, size, DateTime.MaxValue);
		}

		public void Set<T>(string key, T obj, long size, DateTime expire) {
			if (String.IsNullOrEmpty(key))
				throw new Exception("The key can't be null or empty.");

			if (key.Length >= this.MaxKeyLength)
				throw new Exception("The key has exceeded the maximum length.");

			expire = expire.ToUniversalTime();

			if (!_keyExperations.ContainsKey(key))
				_keyExperations.Add(key, expire);
			else
				_keyExperations[key] = expire;

			var mmf = MemoryMappedFile.CreateOrOpen(key, size);
			var vs = mmf.CreateViewStream();
			_bf.Serialize(vs, obj);

			var cmd = "{0}{1}{2}";
			cmd = string.Format(cmd, key, DELIM, expire.ToString("s"));

			var buf = this.Encoding.GetBytes(cmd);
			_ns.Write(buf, 0, buf.Length);
			_ns.Flush();
		}
	}
}
