using System;
using System.Collections.Generic;
using System.Text;
using CoreAudioApi;

namespace WindowsMicrophoneMuteLibrary
{
    /// <summary>
    /// Interface for Muting the Microphone using Microsoft Windows Vista core audio APIs:
    /// http://msdn.microsoft.com/en-us/library/dd370802%28VS.85%29.aspx
    /// Built by Matt Palmerlee November 2010
    /// Using Ray Molenkamp's C# managed wrapper for accessing the Vista Core Audio API
    /// http://www.codeproject.com/KB/vista/CoreAudio.aspx?msg=2489276
    /// </summary>
    internal class CoreAudioMicMute
    {
        private List<MMDevice> micDevices = new List<MMDevice>();

        public CoreAudioMicMute()
        {
            MMDeviceEnumerator DevEnum = new MMDeviceEnumerator();

            MMDeviceCollection devices = DevEnum.EnumerateAudioEndPoints(EDataFlow.eCapture, EDeviceState.DEVICE_STATE_ACTIVE);

            //tbMaster.Value = (int)(device.AudioEndpointVolume.MasterVolumeLevelScalar * 100);
            //device.AudioEndpointVolume.OnVolumeNotification += new AudioEndpointVolumeNotificationDelegate(AudioEndpointVolume_OnVolumeNotification);
            for (int i = 0; i < devices.Count; i++)
            {
                MMDevice deviceAt = devices[i];
                //deviceAt.State


                if (deviceAt.FriendlyName.ToLower() == "microphone")
                {
                    this.micDevices.Add(deviceAt);
                }
            }

            if (this.micDevices.Count == 0)
                throw new InvalidOperationException("Microphone not found by MicMute Library!");
        }

        public void SetMute(bool mute)
        {
            foreach (MMDevice device in this.micDevices)
            {
                device.AudioEndpointVolume.Mute = mute;
            }
        }

    }
}
