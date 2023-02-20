using System.Collections.Generic;
using System.Linq;
using Godot;
using MidiSharp;
using MidiWranglerGodot.Debug;
using MidiWranglerGodot.Midi;

namespace MidiWranglerGodot
{
    public class Main : Control
    {
        readonly Queue<MidiNote> _midiNotes = new();
        readonly Queue<MidiNote> _notesToLog = new();
        readonly Timer _pollDeviceTimer = new ();
        readonly HashSet<string> _currentDevices = new ();

        StatusBarControl _statusControl = default!;
        OptionButton _optionList;
        RichTextLabel _midiLog;

        ulong? _firstSyncEventReceived;
        ulong? _lastSyncEventReceived;
        ulong _cutOffUSec = 15;
        
        public float CutOffSecs
        {
            get => _cutOffUSec / 1000000f;
            set => _cutOffUSec = (ulong)(value * 1000000);
        }
        
        public override void _Ready()
        {
            _statusControl = GetNode<StatusBarControl>("%StatusBar");
            _optionList = GetNode<OptionButton>("%Toolbar/%DevicesList");
            _midiLog = GetNode<RichTextLabel>("%Monitor/%MidiEventLog");
            AddChild(_pollDeviceTimer);
            
            OS.OpenMidiInputs();
            EnumerateMidiDevices();
            
            _pollDeviceTimer.Connect("timeout", this, nameof(EnumerateMidiDevices));
            _pollDeviceTimer.Start(5f);
        }

        public override void _Input(InputEvent @event)
        {
            if (@event is not InputEventMIDI eventMidi) return;
            
            Logger.Debug($"{eventMidi.Message.ToString()}: {eventMidi.Pitch}@{eventMidi.Velocity}");
            
            if (eventMidi.Message == (int)MidiMessageList.TimingClock)
            {
                _firstSyncEventReceived ??= OS.GetTicksUsec();
                _lastSyncEventReceived = OS.GetTicksUsec();
                return;
            }
            
            _statusControl.MidiDeviceStatus = MidiDeviceStatus.Receiving;
            _lastSyncEventReceived = OS.GetTicksUsec();

            var noteData = new MidiNote
            {
                Message = eventMidi.Message,
                Note = eventMidi.Pitch,
                Velocity = eventMidi.Velocity,
                Timestamp = _lastSyncEventReceived.GetValueOrDefault() - _firstSyncEventReceived.GetValueOrDefault()
            };
            
            _midiNotes.Enqueue(noteData);
            _notesToLog.Enqueue(noteData);
        }

        public override void _Process(float delta)
        {
            if (_lastSyncEventReceived != null && OS.GetTicksUsec() - _lastSyncEventReceived > _cutOffUSec)
            {
                _statusControl.MidiDeviceStatus = MidiDeviceStatus.Connected;
            }

            if (_statusControl.MidiDeviceStatus == MidiDeviceStatus.Connected && _midiNotes.Count > 0)
            {
                var seq = new MidiSequence();
                
                while (_midiNotes.Count > 0)
                {
                }
            }

            if (_notesToLog.Count <= 0) return;
            var noteData = _notesToLog.Dequeue();
            _midiLog.AppendBbcode(
                $"[{noteData.Timestamp / 1000000f}] {(MidiMessageList)noteData.Message}: {noteData.Note} @ {noteData.Velocity}\n");
        }

        void EnumerateMidiDevices()
        {
            var devices = OS.GetConnectedMidiInputs();
            
            switch (devices.Length)
            {
                case > 0 when _statusControl.MidiDeviceStatus != MidiDeviceStatus.Connected:
                    _statusControl.MidiDeviceStatus = MidiDeviceStatus.Connected;
                    break;
                case 0:
                {
                    _statusControl.MidiDeviceStatus = MidiDeviceStatus.Disconnected;
                    if (_optionList.Items.Count > 0) _optionList.Clear();
                    _currentDevices.Clear();
                    return;
                }
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
