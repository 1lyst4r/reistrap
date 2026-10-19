using System.Runtime.InteropServices;

namespace Bloxstrap.Integrations
{
    public static class AudioSessions
    {
        public static int SetMuted(int processId, bool muted)
        {
            int changed = 0;
            var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();

            try
            {
                if (enumerator.EnumAudioEndpoints(0, 1, out var devices) != 0)
                    return 0;

                devices.GetCount(out uint deviceCount);

                for (uint i = 0; i < deviceCount; i++)
                {
                    if (devices.Item(i, out var device) != 0)
                        continue;

                    Guid iid = typeof(IAudioSessionManager2).GUID;

                    if (device.Activate(ref iid, 23, IntPtr.Zero, out object managerObject) != 0)
                        continue;

                    var manager = (IAudioSessionManager2)managerObject;

                    if (manager.GetSessionEnumerator(out var sessions) == 0)
                    {
                        sessions.GetCount(out int sessionCount);

                        for (int j = 0; j < sessionCount; j++)
                        {
                            if (sessions.GetSession(j, out var session) != 0)
                                continue;

                            if (session.GetProcessId(out uint pid) == 0 && pid == processId)
                            {
                                Guid context = Guid.Empty;
                                ((ISimpleAudioVolume)session).SetMute(muted, ref context);
                                changed++;
                            }

                            Marshal.ReleaseComObject(session);
                        }

                        Marshal.ReleaseComObject(sessions);
                    }

                    Marshal.ReleaseComObject(manager);
                    Marshal.ReleaseComObject(device);
                }

                Marshal.ReleaseComObject(devices);
            }
            finally
            {
                Marshal.ReleaseComObject(enumerator);
            }

            return changed;
        }

        [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
        private class MMDeviceEnumerator { }

        [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDeviceEnumerator
        {
            [PreserveSig] int EnumAudioEndpoints(int dataFlow, uint stateMask, out IMMDeviceCollection devices);
        }

        [ComImport, Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDeviceCollection
        {
            [PreserveSig] int GetCount(out uint count);
            [PreserveSig] int Item(uint index, out IMMDevice device);
        }

        [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDevice
        {
            [PreserveSig] int Activate(ref Guid iid, int clsCtx, IntPtr activationParams, [MarshalAs(UnmanagedType.IUnknown)] out object instance);
        }

        [ComImport, Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAudioSessionManager2
        {
            [PreserveSig] int GetAudioSessionControl(IntPtr sessionGuid, uint flags, out IntPtr control);
            [PreserveSig] int GetSimpleAudioVolume(IntPtr sessionGuid, uint flags, out IntPtr volume);
            [PreserveSig] int GetSessionEnumerator(out IAudioSessionEnumerator sessions);
        }

        [ComImport, Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAudioSessionEnumerator
        {
            [PreserveSig] int GetCount(out int count);
            [PreserveSig] int GetSession(int index, out IAudioSessionControl2 session);
        }

        [ComImport, Guid("BFB7FF88-7239-4FC9-8FA2-07C950BE9C6D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAudioSessionControl2
        {
            [PreserveSig] int GetState(out int state);
            [PreserveSig] int GetDisplayName(out IntPtr name);
            [PreserveSig] int SetDisplayName(IntPtr name, IntPtr context);
            [PreserveSig] int GetIconPath(out IntPtr path);
            [PreserveSig] int SetIconPath(IntPtr path, IntPtr context);
            [PreserveSig] int GetGroupingParam(out Guid groupingId);
            [PreserveSig] int SetGroupingParam(IntPtr groupingId, IntPtr context);
            [PreserveSig] int RegisterAudioSessionNotification(IntPtr client);
            [PreserveSig] int UnregisterAudioSessionNotification(IntPtr client);
            [PreserveSig] int GetSessionIdentifier(out IntPtr identifier);
            [PreserveSig] int GetSessionInstanceIdentifier(out IntPtr identifier);
            [PreserveSig] int GetProcessId(out uint processId);
        }

        [ComImport, Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface ISimpleAudioVolume
        {
            [PreserveSig] int SetMasterVolume(float level, ref Guid context);
            [PreserveSig] int GetMasterVolume(out float level);
            [PreserveSig] int SetMute([MarshalAs(UnmanagedType.Bool)] bool mute, ref Guid context);
        }
    }
}
