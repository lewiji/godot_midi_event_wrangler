using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MidiWranglerGodot.src;

namespace MidiWranglerGodot
{
    public class Main : Control
    {
        
        public MidiDeviceStatus MidiDeviceStatus
        {
            get => _deviceStatus;
            set
            {
                _deviceStatus = value;
                _statusText.BbcodeText = _deviceStatus switch
                {
                    MidiDeviceStatus.Disconnected => "DISCONNECTED",
                    MidiDeviceStatus.Connected => "[wave]CONNECTED[/wave]",
                    MidiDeviceStatus.Receiving => "[shake]RECEIVING[/shake]",
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
        }
        MidiDeviceStatus _deviceStatus = MidiDeviceStatus.Disconnected;

        public float CutOffSecs
        {
            get => _cutOffUSec / 1000000f;
            set => _cutOffUSec = (ulong)(value * 1000000);
        }
        ulong _cutOffUSec;
        ulong? _firstEventReceived = null;
        ulong? _lastEventReceived = null;
        
        OptionButton _optionList;
        RichTextLabel _midiLog;
        RichTextLabel _statusText;
        Timer _pollDeviceTimer = new Timer();
        HashSet<string> _currentDevices = new HashSet<string>();

        public Main()
        {
            CutOffSecs = 15;
        }
        
        public override void _Ready()
        {
            _optionList = GetNode<OptionButton>("%DevicesList");
            _midiLog = GetNode<RichTextLabel>("%MidiEventLog");
            _statusText = GetNode<RichTextLabel>("%StatusText");
            AddChild(_pollDeviceTimer);
            
            OS.OpenMidiInputs();
            EnumerateMidiDevices();
            
            _pollDeviceTimer.Connect("timeout", this, nameof(EnumerateMidiDevices));
            _pollDeviceTimer.Start(3f);
        }

        public override void _Input(InputEvent @event)
        {
            if (@event is not InputEventMIDI eventMidi) return;
            if (eventMidi.Message == (int)MidiMessageList.TimingClock) return;
            if (MidiDeviceStatus != MidiDeviceStatus.Receiving)
                MidiDeviceStatus = MidiDeviceStatus.Receiving;
            
            _firstEventReceived ??= OS.GetTicksUsec();
            _lastEventReceived ??= OS.GetTicksUsec();
            
            _midiLog.AppendBbcode($"d{eventMidi.Device} m{eventMidi.Message} n{eventMidi.Pitch} p{eventMidi.Pressure} v{eventMidi.Velocity}\n");
        }

        public override void _Process(float delta)
        {
            if (_lastEventReceived != null && OS.GetTicksUsec() - _lastEventReceived > _cutOffUSec)
            {
                MidiDeviceStatus = MidiDeviceStatus.Connected;
            }
        }

        void EnumerateMidiDevices()
        {
            var devices = OS.GetConnectedMidiInputs();
            
            if (devices.Length > 0 && MidiDeviceStatus != MidiDeviceStatus.Connected)
            {
                MidiDeviceStatus = MidiDeviceStatus.Connected;
            }
            else if (devices.Length == 0)
            {
                if (MidiDeviceStatus != MidiDeviceStatus.Disconnected) MidiDeviceStatus = MidiDeviceStatus.Disconnected;
                if (_optionList.Items.Count > 0) _optionList.Clear();
                _currentDevices.Clear();
                return;
            }

            if (_optionList.Items.Count == 0)
            {
                _currentDevices.Clear();
                _currentDevices.UnionWith(devices);
                foreach (var device in _currentDevices)
                {
                    _optionList.AddItem(device);
                }
            }
            else
            {
                var removedItems = _currentDevices.Except(devices).ToHashSet();
                foreach (var removedItem in removedItems)
                {
                    _optionList.RemoveItem(_optionList.Items.IndexOf(removedItem));
                }
                
                var newItems = devices.Except(_currentDevices).ToHashSet();
                foreach (var newItem in newItems)
                {
                    _optionList.AddItem(newItem);
                }
                
                _currentDevices.ExceptWith(removedItems);
                _currentDevices.UnionWith(newItems);
            }
        }
    }
}
