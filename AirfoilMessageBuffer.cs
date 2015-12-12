using System;
using System.Text;

namespace AirfoilMetadataAgent
{
	/// <summary>
	/// Buffers up incoming characters from Airfoil and spits out messages as they are completed.
	/// See the protocol here: http://weblog.rogueamoeba.com/2014/05/16/developer-note-integrating-with-airfoil-for-windows/
	/// </summary>
	internal class AirfoilMessageBuffer
	{
		private int Length { get; set; } = -1;

		private StringBuilder Buffer = new StringBuilder();

		/// <summary>
		/// Adds a character to the buffer.
		/// </summary>
		/// <param name="c">The character to add to the buffer.</param>
		/// <returns>
		/// If the addition of the character resulted in a completed message, the text body of that message is returned.
		/// Otherwise, null.
		/// </returns>
		public String Accept(char c)
		{
			String result = null;
			// While we don't have a length established, read until the semicolon delimiter.
			// Once that's hit, parse the expected length and reset the buffer in preparation for the message body.
			if (Length == -1)
			{
				if (c == ';')
				{
					Length = Int32.Parse(Buffer.ToString());
					Buffer.Clear();
				}
				else
				{
					Buffer.Append(c);
				}
			}
			// If we have a length, add characters to the buffer until it matches. 
			// If it does, then return the buffer (which represents the body of the message)
			// and then reset it in preparation for the next message WHICH COULD BE COMING RIGHT NOW.
			else
			{
				Buffer.Append(c);
				if (Buffer.Length == Length)
				{
					result = Buffer.ToString();
					Reset();
				}
			}
			return result;
		}

		/// <summary>
		/// Resets the buffer.
		/// If you do this in the middle of a message, you're almost certainly screwed.
		/// Generally you'd only ever want to call this after stopping the server.
		/// </summary>
		public void Reset()
		{
			Buffer.Clear();
			Length = -1;
		}
	}
}
