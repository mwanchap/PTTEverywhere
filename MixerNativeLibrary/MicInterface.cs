using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace MixerNativeLibrary
{
    /// <summary>
    /// Class for interfacing with the Microphone Device on Windows XP and older windows operating systems
    /// Built by Matt Palmerlee November 2010
    /// This class was built from translated c++ code in this post:
    /// http://stackoverflow.com/questions/2078970/how-to-mute-the-microphone-c
    /// and uses the necessary pieces from Gustavo Franco's MixerNative AudioLib source here:
    /// http://www.codeguru.com/csharp/csharp/cs_graphics/sound/article.php/c10931
    /// </summary>
    public static class MicInterface
    {
        public static unsafe void MuteOrUnMuteAllMics(bool mute)
        {
            IntPtr mHMixer = IntPtr.Zero;

            try
            {
                uint dwDestination;
                unchecked
                {
                    dwDestination = (uint)-1;
                }

                int iNumDevs = MixerNative.mixerGetNumDevs();

                if (iNumDevs == 0) throw new ApplicationException("No microphones found!");

                for (int i = 0; i < iNumDevs; i++)
                {
                    MixerNative.mixerOpen(out mHMixer, i, IntPtr.Zero/*mCallbackWindow.Handle*/, IntPtr.Zero, MixerNative.CALLBACK_WINDOW);

                    // Get the line info for the wave in destination line 
                    MIXERLINE mxl = new MIXERLINE();

                    // find the microphone source line connected to this wave in destination 

                    IntPtr pmc = IntPtr.Zero;

                    MMErrors errorCode = 0;

                    mxl.cbStruct = (uint)Marshal.SizeOf(mxl);
                    mxl.dwComponentType = MIXERLINE_COMPONENTTYPE.DST_SPEAKERS;
                    if (MixerNative.mixerGetLineInfo(mHMixer, ref mxl, MIXER_GETLINEINFOF.COMPONENTTYPE) == 0)
                    {
                        dwDestination = mxl.dwDestination;
                        int cConnections = (int)mxl.cConnections;

                        for (uint j = 0; j < cConnections; j++)
                        {
                            mxl.cbStruct = (uint)Marshal.SizeOf(mxl);
                            mxl.dwDestination = dwDestination;
                            mxl.dwSource = (uint)j;
                            mxl.dwComponentType = MIXERLINE_COMPONENTTYPE.SRC_MICROPHONE;
                            if (MixerNative.mixerGetLineInfo(mHMixer, ref mxl, MIXER_GETLINEINFOF.SOURCE) == 0)
                            {
                                if (mxl.dwComponentType == MIXERLINE_COMPONENTTYPE.SRC_MICROPHONE)
                                {
                                    //it is a microphone
                                    pmc = Marshal.AllocHGlobal((int)(Marshal.SizeOf(typeof(MIXERCONTROL)) * mxl.cControls));

                                    MIXERLINECONTROLS mlc = new MIXERLINECONTROLS();
                                    mlc.cbStruct = (uint)sizeof(MIXERLINECONTROLS);
                                    mlc.dwLineID = mxl.dwLineID;
                                    mlc.cControls = mxl.cControls;
                                    mlc.pamxctrl = pmc;
                                    mlc.cbmxctrl = (uint)(Marshal.SizeOf(typeof(MIXERCONTROL)));
                                    mlc.dwControlType = MIXERCONTROL_CONTROLTYPE.MUTE;

                                    errorCode = (MMErrors)MixerNative.mixerGetLineControls(mHMixer, ref mlc, MIXER_GETLINECONTROLSFLAG.ALL);

                                    for (int k = 0; k < mlc.cControls; k++)
                                    {
                                        MIXERCONTROL mc = (MIXERCONTROL)Marshal.PtrToStructure((IntPtr)(((byte*)pmc) + (Marshal.SizeOf(typeof(MIXERCONTROL)) * k)), typeof(MIXERCONTROL));
                                        if (mc.dwControlType == (uint)MIXERCONTROL_CONTROLTYPE.MUTE)
                                        {
                                            MicInterface.muteOrUnMuteMixerControl(mute, mHMixer, mc.dwControlID);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                if (mHMixer != IntPtr.Zero)
                    MixerNative.mixerClose(mHMixer);
            }
        }

        private static unsafe void muteOrUnMuteMixerControl(bool mute, IntPtr mHMixer, uint controlId)
        {
            MMErrors errorCode = 0;
            IntPtr pUnsigned = IntPtr.Zero;

            try
            {

                // find the microphone source line connected to this wave in destination 

                IntPtr pmxcdSelectValue = Marshal.AllocHGlobal((int)(1 * sizeof(MIXERCONTROLDETAILS_BOOLEAN)));

                MIXERCONTROLDETAILS mxcd = new MIXERCONTROLDETAILS();
                mxcd.cbStruct = (uint)sizeof(MIXERCONTROLDETAILS);
                mxcd.dwControlID = controlId;
                mxcd.cChannels = 1;
                mxcd.hwndOwner = IntPtr.Zero;
                mxcd.cbDetails = (uint)sizeof(MIXERCONTROLDETAILS_BOOLEAN);
                mxcd.paDetails = pmxcdSelectValue;

                unchecked
                {
                    errorCode = (MMErrors)MixerNative.mixerGetControlDetails(mHMixer, ref mxcd, (MIXER_GETCONTROLDETAILSFLAG)(int)((uint)MIXER_OBJECTFLAG.HMIXER | (int)MIXER_GETCONTROLDETAILSFLAG.VALUE));
                }
                if (errorCode != MMErrors.MMSYSERR_NOERROR)
                    throw new MixerException(errorCode, MicInterface.GetErrorDescription(FuncName.fnMixerGetControlDetails, errorCode));

                *((uint*)pmxcdSelectValue) = mute ? 1U : 0U;

                errorCode = (MMErrors)MixerNative.mixerSetControlDetails(mHMixer, ref mxcd, MIXER_SETCONTROLDETAILSFLAG.VALUE);
                if (errorCode != MMErrors.MMSYSERR_NOERROR)
                    throw new MixerException(errorCode, MicInterface.GetErrorDescription(FuncName.fnMixerSetControlDetails, errorCode));

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                if (pUnsigned != IntPtr.Zero)
                    Marshal.FreeHGlobal(pUnsigned);
            }
        }

        internal static string GetErrorDescription(FuncName funcName, MMErrors errorCode)
        {
            string errorDesc = "";

            switch (funcName)
            {
                case FuncName.fnWaveOutOpen:
                case FuncName.fnWaveInOpen:
                    switch (errorCode)
                    {
                        case MMErrors.MMSYSERR_ALLOCATED:
                            errorDesc = "Specified resource is already allocated.";
                            break;
                        case MMErrors.MMSYSERR_BADDEVICEID:
                            errorDesc = "Specified device identifier is out of range.";
                            break;
                        case MMErrors.MMSYSERR_NODRIVER:
                            errorDesc = "No device driver is present.";
                            break;
                        case MMErrors.MMSYSERR_NOMEM:
                            errorDesc = "Unable to allocate or lock memory.";
                            break;
                        case MMErrors.WAVERR_BADFORMAT:
                            errorDesc = "Attempted to open with an unsupported waveform-audio format.";
                            break;
                        case MMErrors.WAVERR_SYNC:
                            errorDesc = "The device is synchronous but waveOutOpen was called without using the WAVE_ALLOWSYNC flag.";
                            break;
                    }
                    break;
                case FuncName.fnMixerOpen:
                    switch (errorCode)
                    {
                        case MMErrors.MMSYSERR_ALLOCATED:
                            errorDesc = "The specified resource is already allocated by the maximum number of clients possible.";
                            break;
                        case MMErrors.MMSYSERR_BADDEVICEID:
                            errorDesc = "The uMxId parameter specifies an invalid device identifier.";
                            break;
                        case MMErrors.MMSYSERR_INVALFLAG:
                            errorDesc = "One or more flags are invalid.";
                            break;
                        case MMErrors.MMSYSERR_INVALHANDLE:
                            errorDesc = "The uMxId parameter specifies an invalid handle.";
                            break;
                        case MMErrors.MMSYSERR_INVALPARAM:
                            errorDesc = "One or more parameters are invalid.";
                            break;
                        case MMErrors.MMSYSERR_NODRIVER:
                            errorDesc = "No device driver is present.";
                            break;
                        case MMErrors.MMSYSERR_NOMEM:
                            errorDesc = "Unable to allocate or lock memory.";
                            break;
                    }
                    break;
                case FuncName.fnMixerGetID:
                case FuncName.fnMixerGetLineInfo:
                case FuncName.fnMixerGetLineControls:
                case FuncName.fnMixerGetControlDetails:
                case FuncName.fnMixerSetControlDetails:
                    switch (errorCode)
                    {
                        case MMErrors.MIXERR_INVALCONTROL:
                            errorDesc = "The control reference is invalid.";
                            break;
                        case MMErrors.MIXERR_INVALLINE:
                            errorDesc = "The audio line reference is invalid.";
                            break;
                        case MMErrors.MMSYSERR_BADDEVICEID:
                            errorDesc = "The hmxobj parameter specifies an invalid device identifier.";
                            break;
                        case MMErrors.MMSYSERR_INVALFLAG:
                            errorDesc = "One or more flags are invalid.";
                            break;
                        case MMErrors.MMSYSERR_INVALHANDLE:
                            errorDesc = "The hmxobj parameter specifies an invalid handle.";
                            break;
                        case MMErrors.MMSYSERR_INVALPARAM:
                            errorDesc = "One or more parameters are invalid.";
                            break;
                        case MMErrors.MMSYSERR_NODRIVER:
                            errorDesc = "No device driver is present.";
                            break;
                    }
                    break;
                case FuncName.fnMixerClose:
                    switch (errorCode)
                    {
                        case MMErrors.MMSYSERR_INVALHANDLE:
                            errorDesc = "Specified device handle is invalid.";
                            break;
                    }
                    break;
                case FuncName.fnWaveOutClose:
                case FuncName.fnWaveInClose:
                case FuncName.fnWaveInGetDevCaps:
                case FuncName.fnWaveOutGetDevCaps:
                case FuncName.fnMixerGetDevCaps:
                    switch (errorCode)
                    {
                        case MMErrors.MMSYSERR_BADDEVICEID:
                            errorDesc = "Specified device identifier is out of range.";
                            break;
                        case MMErrors.MMSYSERR_INVALHANDLE:
                            errorDesc = "Specified device handle is invalid.";
                            break;
                        case MMErrors.MMSYSERR_NODRIVER:
                            errorDesc = "No device driver is present.";
                            break;
                        case MMErrors.MMSYSERR_NOMEM:
                            errorDesc = "Unable to allocate or lock memory.";
                            break;
                        case MMErrors.WAVERR_STILLPLAYING:
                            errorDesc = "There are still buffers in the queue.";
                            break;
                    }
                    break;
                case FuncName.fnCustom:
                    switch (errorCode)
                    {
                        case (MMErrors)1000:
                            errorDesc = "Device Not Found.";
                            break;
                    }
                    break;
            }
            return errorDesc;
        }
    }
}
