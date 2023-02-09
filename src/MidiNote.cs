using Godot;

namespace MidiWranglerGodot;

public struct MidiNote
{
    public ulong Timestamp;
    public int Message;
    public int Note;
    public int Velocity;
}