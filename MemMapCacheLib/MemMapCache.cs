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
		private TcpClient _tcpClient;
		private NetworkStream _ns;
		private BinaryFormatter _bf;

		public MemMapCache() {
			this.Encoding = ASCIIEncoding.ASCII;
			this.ChunkSize = 1024 * 1024 * 10; //10MB

			this.Server = "127.0.0.1"; //limited to local
			this.Port = 57742;
		}

		public long ChunkSize { get; set; } 

		public Encoding Encoding { get; set; }

		public int MaxKeyLength { get { return 4096; } }

		public int Port { get; set; }
		
		public string Server { get; protected set; }

		public void Connect() {
			_tcpClient = new TcpClient();
			_tcpClient.Connect(this.Server, this.Port);
			_ns = _tcpClient.GetStream();
			_bf = new BinaryFormatter();
		}

		public T Get<T>(string key) {
			var mmf = MemoryMappedFile.OpenExisting(key);
			var vs = mmf.CreateViewStream();
			var o = _bf.Deserialize(vs);
			return (T)o;
		}

		public void Set<T>(string key, T obj) {
			this.Set<T>(key, obj, this.ChunkSize);
		}

		public void Set<T>(string key, T obj, long size) {
			if (key.Length >= this.MaxKeyLength)
				throw new Exception("The key has exceeded the maximum length.");

			var mmf = MemoryMappedFile.CreateOrOpen(key, size);
			var vs = mmf.CreateViewStream();
			_bf.Serialize(vs, obj);

			var buf = this.Encoding.GetBytes(key);
			_ns.Write(buf, 0, buf.Length);
			_ns.Flush();
		}
	}
}
