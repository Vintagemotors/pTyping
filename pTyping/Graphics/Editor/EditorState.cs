using System.Collections.Generic;
using System.Collections.ObjectModel;
using Furball.Engine.Engine.Graphics.Drawables;
using pTyping.Graphics.Player;
using pTyping.Songs;

namespace pTyping.Graphics.Editor {
    public class EditorState {
        public readonly List<NoteDrawable>    Notes  = new();
        public readonly List<ManagedDrawable> Events = new();

        public readonly ObservableCollection<ManagedDrawable> SelectedObjects = new();

        public double CurrentTime;
        public double MouseTime;

        public Song Song;
    }
}
