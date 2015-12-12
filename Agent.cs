using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Pipes;
using System.Text;

namespace AirfoilMetadataAgent
{
	/// <summary>
	/// Class that creates the interface for Airfoil to talk to, and handles messaging between Airfoil and the application.
	/// </summary>
	public class Agent : IpcCallback
	{
		private AirfoilMessageBuffer messageBuffer;
		private IpcServer ipcServer;
		private AirfoilAgentListener agentListener;

		#region Airfoil Constants
		// Remote commands
		const string kPlayPause = "remotePlayPause";
		const string kTrackNext = "remoteTrackNext";
		const string kTrackPrevious = "remoteTrackPrevious";

		// Remote command succeded response. 
		// Any other response means is interpreted as the remote command failed.
		const string kRemoteCommandOK = "OK";

		// Capabilities queries.
		const string kSupportsRemoteControl = "supportsRemoteControl";
		const string kProvidesTrackData = "providesTrackData";

		// Possible responses for capability queries
		const string kTrue = "true";
		const string kFalse = "false";

		// Metadata request types. All responses should be strings. The album art
		// response should be the base64 representation of the album art in PNG format.
		const string kRequestTrackTitle = "requestTrackTitle";
		const string kRequestTrackArtist = "requestTrackArtist";
		const string kRequestTrackAlbum = "requestTrackAlbum";
		const string kRequestAlbumArt = "requestAlbumArt";
		#endregion

		/// <summary>
		/// Creates a new instance of the agent, which will immediately start listening for Airfoil messages
		/// and passing them along to the provided listener.
		/// </summary>
		/// <param name="listener">An AirfoilAgentListener that will receive notifications of incoming messages.</param>
		public Agent(AirfoilAgentListener listener)
		{
			agentListener = listener;
			messageBuffer = new AirfoilMessageBuffer();
			Start();
		}

		/// <summary>
		/// Starts listening for Airfoil messages. 
		/// The Agent starts automatically when created, so it is generally not necessary to call this.
		/// </summary>
		public void Start()
		{
			if (ipcServer == null)
			{
				ipcServer = new IpcServer($"{Process.GetCurrentProcess().Id}_airfoil_metadata", this, 1);
			}
		}

		/// <summary>
		/// Shuts down the server and stops listening for messages.
		/// </summary>
		public void Stop()
		{
			if (ipcServer != null)
			{
				ipcServer.IpcServerStop();
				ipcServer = null;
				messageBuffer.Reset();
			}
		}

		protected String HandleAirfoilMessage(String message)
		{
			switch (message)
			{
				// Handle remote commands, then return kRemoteCommandOK to indicate success, 
				// or any other string to indicate failure.
				case kSupportsRemoteControl:
					return agentListener.SupportsRemoteControl ? kTrue : kFalse;
				case kPlayPause:
					return HandleRemoteControl(RemoteControlType.PlayPause);
				case kTrackNext:
					return HandleRemoteControl(RemoteControlType.NextTrack);
				case kTrackPrevious:
					return HandleRemoteControl(RemoteControlType.PreviousTrack);
				// Handle metadata commands.
				case kProvidesTrackData:
					return agentListener.SupportsMetadata ? kTrue : kFalse;
				case kRequestTrackTitle:
					return HandleMetadata(MetadataType.TrackTitle);
				case kRequestTrackArtist:
					return HandleMetadata(MetadataType.TrackArtist);
				case kRequestTrackAlbum:
					return HandleMetadata(MetadataType.TrackAlbum);
				case kRequestAlbumArt:
					String artwork = HandleMetadata(MetadataType.AlbumArt);
					return !String.IsNullOrEmpty(artwork)
						? ImageToPng(artwork)
						: "";
			}
			return "";
		}

		protected String HandleRemoteControl(RemoteControlType rcType)
		{
			return agentListener.SupportsRemoteControl && agentListener.HandleRemoteControl(rcType)
				? kRemoteCommandOK
				: "";
		}

		protected String HandleMetadata(MetadataType mdType)
		{
			return agentListener.SupportsMetadata
				? agentListener.HandleMetadata(mdType)
				: "";
		}

		public void OnAsyncConnect(PipeStream pipe, out object state)
		{
			state = new object();
		}

		public void OnAsyncDisconnect(PipeStream pipe, object state)
		{
		}

		public void OnAsyncMessage(PipeStream pipe, byte[] data, int bytes, object state)
		{
			// Grab the text message, which will include a length followed by a body.
			String message = Encoding.UTF8.GetString(data, 0, bytes);
			foreach (var c in message)
			{
				// Push it character-by-character into the buffer.
				String m = messageBuffer.Accept(c);
				// If at any point the buffer says we have a message, then do something with it and respond to Airfoil.
				if (m != null)
				{
					SendString(pipe, HandleAirfoilMessage(m));
				}
			}
		}

		private void OnAsyncWriteComplete(IAsyncResult result)
		{
			PipeStream pipe = (PipeStream)result.AsyncState;
			pipe.EndWrite(result);
		}

		protected void SendString(PipeStream pipe, String text)
		{
			// Although the protocol says empty strings are supported and should be sent to indicate when some kind of data is unavailable,
			// sending an empty string (0;) appears to crash Airfoil; a single space (1; ) seems to have the desired effect instead.
			var finalText = String.IsNullOrEmpty(text) ? " " : text;
			// Format the message according to the protocol (http://weblog.rogueamoeba.com/2014/05/16/developer-note-integrating-with-airfoil-for-windows/).
			var message = $"{Encoding.UTF8.GetByteCount(finalText)};{finalText}";
			// Send the bytes.
			var mBytes = Encoding.UTF8.GetBytes(message);
			pipe.BeginWrite(mBytes, 0, mBytes.Length, OnAsyncWriteComplete, pipe);
		}

		protected String ImageToPng(String image64)
		{
			var result = "";

			// If anything at all goes wrong in here, we don't want things getting all crashy,
			// so just ignore the results instead.
			try
			{
				// Put the base64 source into a byte array.
				var imageBytes = Convert.FromBase64String(image64);

				using (var inStream = new MemoryStream(imageBytes))
				{
					// Read it into an image and check the format.
					var inImage = Image.FromStream(inStream);
					// If it's already a PNG, then great, nothing more to do here; just return the original base64.
					if (inImage.RawFormat.Equals(ImageFormat.Png))
					{
						result = image64;
					}
					// Otherwise we'll have to do a conversion.
					else
					{
						using (var outStream = new MemoryStream())
						{
							inImage.Save(outStream, ImageFormat.Png);
							result = Convert.ToBase64String(outStream.ToArray());
						}
					}
				}
			}
			catch { }

			return result;
		}
	}
}