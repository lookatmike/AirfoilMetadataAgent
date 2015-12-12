using System;

namespace AirfoilMetadataAgent
{
	/// <summary>
	/// Defines the incoming message type when processing metadata reqeusts.
	/// </summary>
	public enum MetadataType
	{
		TrackTitle,
		TrackArtist,
		TrackAlbum,
		AlbumArt
	}

	/// <summary>
	/// Defines the incoming message type when processing remote control requests.
	/// </summary>
	public enum RemoteControlType
	{
		PlayPause,
		NextTrack,
		PreviousTrack
	}

	/// <summary>
	/// An interface for handling incoming requests from Airfoil.
	/// </summary>
	public interface AirfoilAgentListener
	{
		/// <summary>
		/// Indicates whether the listener supports remote control or not.
		/// </summary>
		bool SupportsRemoteControl { get; }

		/// <summary>
		/// Handles incoming remote control requests.
		/// </summary>
		/// <param name="messageType">The specific type of remote control operation being requested.</param>
		/// <returns>True if the request was handled and succeeded; false otherwise.</returns>
		bool HandleRemoteControl(RemoteControlType messageType);

		/// <summary>
		/// Indicates whether the listener supports track/album metadata or not.
		/// </summary>
		bool SupportsMetadata { get; }

		/// <summary>
		/// Handles incoming metadata requests.
		/// </summary>
		/// <param name="messageType">The specific type of metadata being requested.</param>
		/// <returns>A string with the requested metadata; null or empty if it is not available.</returns>
		/// <remarks>
		/// Title, Artist, and Album are pretty straightforward. 
		/// AlbumArt should be a base64 string of the image. PNG is preferred if you have a choice, but
		/// whatever you return will be converted if necessary.
		/// </remarks>
		String HandleMetadata(MetadataType messageType);
	}
}
