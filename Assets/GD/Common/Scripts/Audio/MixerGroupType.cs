namespace GD.Audio
{
    /// <summary>
    /// The standard mixer groups a game routes its sounds through. A cue names one of these,
    /// and the AudioManager maps each one to a source prefab, whose Output is the mixer group.
    /// </summary>
    /// <remarks>
    /// Add a value only at the end. Unity saves an enum field as its number, so inserting a value
    /// earlier in the list changes the group of every cue and mapping already saved.
    /// </remarks>
    public enum MixerGroupType
    {
        Master,
        Music,
        SFX,
        Player,
        Environment,
        Ambience,
        UI,
        Gameplay,
        Voice,
        Cinematic
    }
}
