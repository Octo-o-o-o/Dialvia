using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Interop.Plantronics;

namespace Dialvia {
    public enum DeviceButton { ToggleVoice, StartVoice, EndVoice }
    public interface IDeviceAdapter : IDisposable {
        event Action<string> DialRequested;
        event Action<DeviceButton> ButtonPressed;
        event Action<string> StatusChanged;
        bool Connected { get; }
        void Connect();
        void SetVoiceState(bool active);
        void ClearDialDisplay();
        void Ring(bool active);
    }
    // The public adapter boundary is device-neutral. This is the only implemented profile.
    public sealed class PlantronicsP240Adapter : IDeviceAdapter {
        const string SessionName = "Dialvia";
        readonly Control dispatcher;
        readonly bool takeOwnership;
        COMSessionManager manager; COMSession session; COMDevice device; COMCall call;
        ICOMCallEvents_Event callEvents; ICOMDeviceListenerEvents_Event listenerEvents;
        ICOMSessionManagerEvents_Event managerEvents;
        string previousSoftphone; bool changingCall, disposed;
        public event Action<string> DialRequested;
        public event Action<DeviceButton> ButtonPressed;
        public event Action<string> StatusChanged;
        public bool Connected { get { return session != null && device != null; } }
        public string ProductName { get { return device == null ? null : device.ProductName; } }
        public PlantronicsP240Adapter(Control owner, bool ownDevice) { dispatcher = owner; takeOwnership = ownDevice; }
        void Ui(Action action) {
            if (disposed || dispatcher.IsDisposed) return;
            if (dispatcher.InvokeRequired) { try { dispatcher.BeginInvoke(new Action(delegate { Ui(action); })); } catch (InvalidOperationException) { } return; }
            try { action(); } catch (Exception ex) { AppPaths.Log("DEVICE_EVENT " + ex.Message); }
        }
        void Status(string value) { AppPaths.Log("DEVICE " + value); if (StatusChanged != null) StatusChanged(value); }
        public void Connect() {
            if (disposed || Connected) return;
            try {
                if (takeOwnership && !Process.GetProcessesByName("PLTHub").Any()) {
                    string hub = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Plantronics\Spokes3G\PLTHub.exe");
                    if (File.Exists(hub)) Process.Start(new ProcessStartInfo(hub) { WindowStyle = ProcessWindowStyle.Hidden });
                }
                manager = new COMSessionManager(); manager.Register(SessionName, out session);
                device = session.GetActiveDevice();
                if (device == null || device.VendorId != 0x047f || device.ProductId != 0xae04)
                    throw new InvalidOperationException("等待 Plantronics Calisto P240（047F:AE04）。");
                if (takeOwnership) {
                    previousSoftphone = manager.UserPreference.DefaultSoftphone;
                    string restore = Path.Combine(AppPaths.Data, "previous-softphone.txt");
                    if (previousSoftphone != SessionName) AtomicFile.Write(restore, previousSoftphone ?? "");
                    else previousSoftphone = File.Exists(restore) ? File.ReadAllText(restore) : "";
                    callEvents = (ICOMCallEvents_Event)session;
                    callEvents.onCallRequested += OnCallRequested; callEvents.onCallStateChanged += OnCallState;
                    listenerEvents = (ICOMDeviceListenerEvents_Event)device.DeviceListener;
                    listenerEvents.onHeadsetButtonPressed += OnHeadset;
                    managerEvents = (ICOMSessionManagerEvents_Event)manager; managerEvents.onDeviceStateChanged += OnDevice;
                    manager.UserPreference.DefaultSoftphone = SessionName;
                }
                Status("P240 已连接");
            } catch (Exception ex) { Disconnect(true); Status(ex.Message); }
        }
        void OnCallRequested(COMCallRequestEventArgs value) {
            string code = value.contact == null ? "" : value.contact.Phone ?? "";
            Ui(delegate { if (DialRequested != null) DialRequested(code); });
        }
        void OnHeadset(COMDeviceListenerEventArgs value) {
            DeviceHeadsetButton button = value.HeadsetButton;
            Ui(delegate {
                AppPaths.Log("BUTTON " + button);
                if (ButtonPressed == null) return;
                if (button == DeviceHeadsetButton.HeadsetButton_Talk) ButtonPressed(DeviceButton.ToggleVoice);
                else if (button == DeviceHeadsetButton.HeadsetButton_OffHook) ButtonPressed(DeviceButton.StartVoice);
                else if (button == DeviceHeadsetButton.HeadsetButton_OnHook || button == DeviceHeadsetButton.HeadsetButton_Reject) ButtonPressed(DeviceButton.EndVoice);
            });
        }
        void OnCallState(COMCallEventArgs value) {
            CallState state = value.CallState;
            Ui(delegate {
                if (call != null && !changingCall && (state == CallState.CallState_TerminateCall || state == CallState.CallState_RejectCall) && ButtonPressed != null)
                    ButtonPressed(DeviceButton.EndVoice);
            });
        }
        void OnDevice(COMStateDeviceEventArgs value) { Ui(delegate { Disconnect(true); Status("设备变化，正在重新连接…"); }); }
        COMCall NewCall(string label) {
            COMCall value = new LocalCall(240); COMContact contact = new LocalContact(label);
            session.GetCallCommand().OutgoingCall(value, contact, CallAudioRoute.AudioRoute_ToHeadset);
            session.GetCallCommand().AnsweredCall(value); return value;
        }
        public void SetVoiceState(bool active) {
            if (!Connected || !takeOwnership) return;
            try {
                changingCall = true;
                if (active && call == null) call = NewCall("Dialvia voice");
                if (!active && call != null) { session.GetCallCommand().TerminateCall(call); call = null; }
            } finally { changingCall = false; }
        }
        public void ClearDialDisplay() {
            if (!Connected || call != null || !takeOwnership) return;
            try { changingCall = true; var value = NewCall("Dialvia navigation"); session.GetCallCommand().TerminateCall(value); }
            finally { changingCall = false; }
        }
        public void Ring(bool active) { if (Connected && takeOwnership) device.DeviceListener.Ring(active); }
        void Disconnect(bool restore) {
            try {
                if (callEvents != null) { callEvents.onCallRequested -= OnCallRequested; callEvents.onCallStateChanged -= OnCallState; }
                if (listenerEvents != null) listenerEvents.onHeadsetButtonPressed -= OnHeadset;
                if (managerEvents != null) managerEvents.onDeviceStateChanged -= OnDevice;
                if (call != null && session != null) session.GetCallCommand().TerminateCall(call);
                if (restore && takeOwnership && manager != null && manager.UserPreference.DefaultSoftphone == SessionName)
                    manager.UserPreference.DefaultSoftphone = previousSoftphone ?? "";
                if (session != null) session.Unregister();
            } catch (Exception ex) { AppPaths.Log("DISCONNECT " + ex.Message); }
            finally { call = null; callEvents = null; listenerEvents = null; managerEvents = null; device = null; session = null; manager = null; }
        }
        public void Dispose() { if (disposed) return; try { Ring(false); } catch { } Disconnect(true); disposed = true; }
    }
    // Supplying managed COM interface objects avoids E_FAIL from COMCall coclass setters on P240.
    public sealed class LocalCall : COMCall {
        int id; public LocalCall(int value) { id = value; }
        int ICOMCall.Id { get { return id; } set { id = value; } }
        int ICOMCall.ConferenceId { get { return 0; } set { } }
        bool ICOMCall.InConference { get { return false; } set { } }
    }
    public sealed class LocalContact : COMContact {
        string ICOMContact.Name { get; set; } string ICOMContact.FriendlyName { get; set; }
        string ICOMContact.Phone { get; set; } string ICOMContact.Email { get; set; }
        string ICOMContact.HomePhone { get; set; } string ICOMContact.MobilePhone { get; set; }
        string ICOMContact.WorkPhone { get; set; } string ICOMContact.SipUri { get; set; }
        int ICOMContact.Id { get; set; }
        public LocalContact(string label) { ((ICOMContact)this).Name = label; ((ICOMContact)this).FriendlyName = label; }
    }
}
