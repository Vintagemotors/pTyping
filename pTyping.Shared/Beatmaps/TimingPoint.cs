using System.ComponentModel;
using Realms;

namespace pTyping.Shared.Beatmaps;

public class TimingPoint : RealmObject {
    [Description("The exact time where the timing segment starts.")]
    public double Time { get; set; }
    [Description("The time between full beats.")]
    public double Tempo { get; set; }

    [Description("The time division of the song, aka N beat divisions per full beat.")]
    public double TimeSignature { get; set; } = 4;

    public TimingPoint() {}

    public TimingPoint(double time, double tempo) {
        this.Time          = time;
        this.Tempo         = tempo;
        this.TimeSignature = 4;
    }
}
