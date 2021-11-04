using System;
using System.Collections.Generic;
using Furball.Engine.Engine.Audio;
using pTyping.Drawables;
using pTyping.Screens;

namespace pTyping.Player.Mods {
    public class DoubleTimeMod : PlayerMod {
        public override List<Type> IncompatibleMods() => new() {
            typeof(HalfTimeMod)
        };

        public override string Name()            => "Double Time";
        public override string ShorthandName()   => "DT";
        public override double ScoreMultiplier() => 1.05d;

        public override void OnMapStart(AudioStream musicTrack, List<NoteDrawable> notes, PlayerScreen player) {
            musicTrack.AudioRate = 1.5f;

            base.OnMapStart(musicTrack, notes, player);
        }

        public override void OnMapEnd(AudioStream musicTrack, List<NoteDrawable> notes, PlayerScreen player) {
            musicTrack.AudioRate = 1f;

            base.OnMapEnd(musicTrack, notes, player);
        }
    }
}
