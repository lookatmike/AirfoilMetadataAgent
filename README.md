# AirfoilMetadataAgent

A C# / .NET 4.0 library for communicating with Rogue Amoeba's Airfoil for Windows over named pipes.

The primary use for this would be for integrating with Airfoil's remote control and metadata capabilities into an audio application of some sort. Remote speakers and clients with metadata and/or remote control capabilities can then make use of those features when Airfoil is intercepting that application's audio. More details on that <a href="http://rogueamoeba.com/support/knowledgebase/?showArticle=AirfoilRemoteControl-Windows">here</a>.

## Usage

Nuget: https://www.nuget.org/packages/AirfoilMetadataAgent/

Start by implementing the `AirfoilAgentListener` interface on a class, then create an `Agent` with that listener and you're off to the races.

Don't forget to `Stop()` the agent after you're done with it.

## Sample

```C#
class AirfoilSupport : AirfoilAgentListener
{
	private Agent airfoilAgent = null;

	public void Start()
	{
		airfoilAgent = new Agent(this);
	}

	public bool SupportsRemoteControl { get { return true; } }
	public bool HandleRemoteControl(RemoteControlType messageType)
	{
		bool handled = false;
		// ...
		return handled;
	}

	public bool SupportsMetadata { get { return true; } }
	public string HandleMetadata(MetadataType messageType)
	{
		string result = null;
		// ...
		return result;
	}

	public void Stop()
	{
		airfoilAgent.Stop();
	}
}
```

See also <a href="https://github.com/lookatmike/MusicBee-AirfoilIntegrationPlugin">MusicBee-AirfoilIntegrationPlugin</a> for a fully working implementation of this library.

## Credits

The named pipes library is courtesy of <a href="https://github.com/webcoyote/CSNamedPipes">webcoyote</a>.

See <a href="http://weblog.rogueamoeba.com/2014/05/16/developer-note-integrating-with-airfoil-for-windows/">this page</a> for a description of the Airfoil for Windows Metadata Protocol.

## License

MIT license generally means you can do anything you like with this code.
