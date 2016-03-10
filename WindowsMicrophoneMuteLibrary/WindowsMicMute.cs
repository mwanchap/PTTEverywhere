using System;
using System.Collections.Generic;
using System.Text;

namespace WindowsMicrophoneMuteLibrary
{
    /// <summary>
    /// Built by Matt Palmerlee November 2010 
    /// For muting and unmuting the microphone using C# on Windows XP, Vista, and Windows 7
    /// Uses parts of Gustavo Franco's MixerNative AudioLib source for Windows XP and older from here:
    /// http://www.codeguru.com/csharp/csharp/cs_graphics/sound/article.php/c10931
    /// And uses Ray Molenkamp's C# managed wrapper for accessing the Vista Core Audio API (for Windows Vista and newer)
    /// http://www.codeproject.com/KB/vista/CoreAudio.aspx?msg=2489276
    /// Other references:
    /// http://stackoverflow.com/questions/2078970/how-to-mute-the-microphone-c
    /// http://stackoverflow.com/questions/154089/mute-windows-volume-using-c
    /// http://stackoverflow.com/questions/3046668/how-to-mute-microphone-in-windows-7-with-c-c
    /// </summary>
    public class WindowsMicMute
    {
        private CoreAudioMicMute vistaMicMute = null;

        public WindowsMicMute()
        {
            try
            {
                this.vistaMicMute = new CoreAudioMicMute();
                //for some reason I had to call this to setup the Audio Interfaces on startup instead of later or I get invalid cast com exceptions (I think because it was all initialized from separate threads)
                this.vistaMicMute.SetMute(true);
                this.vistaMicMute.SetMute(false);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
                this.vistaMicMute = null;//we'll try the old (xp) way
            }
        }

        public void MuteMic()
        {
            if (this.vistaMicMute != null)
            {
                this.vistaMicMute.SetMute(true);
            }
            else
                MixerNativeLibrary.MicInterface.MuteOrUnMuteAllMics(true);
        }

        public void UnMuteMic()
        {
            if (this.vistaMicMute != null)
            {
                this.vistaMicMute.SetMute(false);
            }
            else
                MixerNativeLibrary.MicInterface.MuteOrUnMuteAllMics(false);
        }


    }
}
