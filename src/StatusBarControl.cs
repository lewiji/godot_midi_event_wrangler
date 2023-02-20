using System;
using Godot;
using MidiWranglerGodot.Midi;

namespace MidiWranglerGodot;

public class StatusBarControl : Control
{
    RichTextLabel? _statusText;
    MidiDeviceStatus _deviceStatus = MidiDeviceStatus.Disconnected;
    
    public MidiDeviceStatus MidiDeviceStatus
    {
        get => _deviceStatus;
        set
        {
            if (_deviceStatus == value) return;
            _deviceStatus = value;
            CallDeferred(nameof(UpdateStatusText));
        }
    }
    
    public override void _Ready()
    {
        _statusText = GetNode<RichTextLabel>("%StatusText");
    }

    void UpdateStatusText()
    {
        if (_statusText == null) return;
        _statusText.BbcodeText = _deviceStatus switch
        {
            MidiDeviceStatus.Disconnected => "DISCONNECTED",
            MidiDeviceStatus.Connected => "CONNECTED",
            MidiDeviceStatus.Receiving => "[wave]RECEIVING[/wave]",
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}