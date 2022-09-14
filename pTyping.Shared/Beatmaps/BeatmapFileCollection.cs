using System.ComponentModel;
using Realms;

namespace pTyping.Shared.Beatmaps;

#nullable enable

public class BeatmapFileCollection : RealmObject {
    [Description("The hash and file path for the audio.")]
    public PathHashTuple? Audio { get; set; }
    [Description("The hash and file path for the background image.")]
    public PathHashTuple? Background { get; set; }
    [Description("The hash and file path for the background video.")]
    public PathHashTuple? BackgroundVideo { get; set; }
}
