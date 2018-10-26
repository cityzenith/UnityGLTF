using System;
using System.Collections;
using System.IO;
using GLTF;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Net;
using UnityEngine.Networking;
using System.Threading;

#if WINDOWS_UWP
using System.Threading.Tasks;
#endif

namespace UnityGLTF.Loader
{
	public class WebRequestLoader : ILoader
	{
		public Stream LoadedStream { get; private set; }

		public bool HasSyncLoadMethod { get; private set; }

		/// <summary>
		/// To saved downloads to local storage
		/// </summary>
		public string LocalStoragePath { get; set; }

		private string _rootURI;
		private string _URIQuery;

		public WebRequestLoader(string rootURI, string URIQuery = "")
		{
			_rootURI = rootURI;
			_URIQuery = URIQuery;
			HasSyncLoadMethod = false;
		}

		public IEnumerator LoadStream(string gltfFilePath)
		{
			if (gltfFilePath == null)
			{
				throw new ArgumentNullException("gltfFilePath");
			}

			yield return CreateHTTPRequest(_rootURI, gltfFilePath);
		}

		public void LoadStreamSync(string jsonFilePath)
		{
			throw new NotImplementedException();
		}

		private IEnumerator CreateHTTPRequest(string rootUri, string httpRequestPath)
		{
			UnityWebRequest www = new UnityWebRequest(Path.Combine(rootUri, httpRequestPath) + _URIQuery, "GET", new DownloadHandlerBuffer(), null);
			www.timeout = 5000;
			yield return www.SendWebRequest();

			if ((int)www.responseCode >= 400)
			{
				Debug.LogErrorFormat("{0} - {1}", www.responseCode, www.url);
				throw new Exception("Response code invalid");
			}

			if (www.downloadedBytes > int.MaxValue)
			{
				throw new Exception("Stream is larger than can be copied into byte array");
			}

			byte[] data = www.downloadHandler.data;

			LoadedStream = new MemoryStream(data, 0, www.downloadHandler.data.Length, true, true);

			if (!string.IsNullOrEmpty(LocalStoragePath))
			{
				ThreadPool.QueueUserWorkItem((o) => File.WriteAllBytes(Path.Combine(LocalStoragePath, httpRequestPath), data));
			}
		}
	}
}
