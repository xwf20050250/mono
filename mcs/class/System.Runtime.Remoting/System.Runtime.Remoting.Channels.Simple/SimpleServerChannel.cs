//
// System.Runtime.Remoting.Channels.Simple.SimpleServerChannel.cs
//
// Author: Dietmar Maurer (dietmar@ximian.com)
//
// 2002 (C) Copyright, Ximian, Inc.
//

using System.Collections;
using System.Runtime.Remoting.Messaging;
using System.Text.RegularExpressions;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.IO;

namespace System.Runtime.Remoting.Channels.Simple
{
	public class SimpleServerChannel : IChannelReceiver, IChannel
	{
		int port = 0;
		string name = "simple";
		string host;
		int priority = 1;
		Thread server_thread = null;
		TcpListener listener;
		SimpleServerTransportSink sink;
		ChannelDataStore channel_data;
		
		void Init (IServerChannelSinkProvider provider) {
			if (provider == null) {
				provider = new SimpleServerFormatterSinkProvider ();
			}
			
			IServerChannelSink next_sink = ChannelServices.CreateServerChannelSinkChain (provider, this);

			host = Dns.GetHostByName(Dns.GetHostName()).HostName;
			
			string [] uris = null;
			
			if (port != 0) {
				uris = new String [1];
				uris [0] = GetChannelUri ();
			}
			
			channel_data = new ChannelDataStore (uris);;

			sink = new SimpleServerTransportSink (next_sink);
			
			listener = new TcpListener (port);
			StartListening (null);
		}
		
		public SimpleServerChannel (int port)
		{
			this.port = port;
			Init (null);
		}

		public SimpleServerChannel (IDictionary properties,
					    IServerChannelSinkProvider serverSinkProvider)
		{
			port = (int)properties ["port"];
			Init (serverSinkProvider);
		}

		public SimpleServerChannel (string name, int port,
					    IServerChannelSinkProvider serverSinkProvider)
		{
			name = name;
			this.port = port;
			Init (serverSinkProvider);
		}
		
		public SimpleServerChannel (string name, int port)
		{
			name = name;
			this.port = port;
			Init (null);
		}
		
		public object ChannelData
		{
			get {
				return channel_data;
			}
		}

		public string ChannelName
		{
			get {
				return name;
			}
		}

		public int ChannelPriority
		{
			get {
				return priority;
			}
		}

		string GetChannelUri ()
		{
			return "simple://" + host + ":" + port;
		}
		
		public string[] GetUrlsForUri (string uri)
		{
			string [] chnl_uris = channel_data.ChannelUris;
			
			if (uri.IndexOf ('/') != 0)
				uri = "/" + uri;

			string [] result = new String [chnl_uris.Length];

			for (int i = 0; i < chnl_uris.Length; i++) {
				result [i] = chnl_uris [i] + uri;
			}
			
			return result;
		}

		public string Parse (string url, out string objectURI)
		{
			int port;
			
			string host = SimpleChannel.ParseSimpleURL (url, out objectURI, out port);

			return GetChannelUri ();
		}

		void WaitForConnections ()
		{
			TcpClient client = listener.AcceptTcpClient ();
			Stream network_stream = client.GetStream ();

			while (true) {

				sink.InternalProcessMessage (network_stream);

			}
		}
		
		public void StartListening (object data)
		{
			if (server_thread == null) {
				listener.Start ();
				if (port == 0) {
					port = ((IPEndPoint)listener.LocalEndpoint).Port;
					channel_data.ChannelUris = new String [1];
					channel_data.ChannelUris [0] = GetChannelUri ();
				}

				server_thread = new Thread (new ThreadStart (WaitForConnections));
				server_thread.Start ();
			}
		}

		public void StopListening (object data)
		{
			if (server_thread != null) {
				server_thread.Abort ();
				server_thread = null;
				listener.Stop ();
			}
		}
	}
}
