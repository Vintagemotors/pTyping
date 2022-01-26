using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Furball.Engine;
using Furball.Engine.Engine.Graphics;
using Furball.Engine.Engine.Graphics.Drawables;
using Furball.Engine.Engine.Graphics.Drawables.Primitives;
using Furball.Engine.Engine.Graphics.Drawables.Tweens;
using Furball.Engine.Engine.Graphics.Drawables.Tweens.TweenTypes;
using Furball.Engine.Engine.Graphics.Drawables.Tweens.TweenTypes.BezierPathTween;
using JetBrains.Annotations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using pTyping.Engine;
using pTyping.Graphics.Player.Mods;
using pTyping.Scores;
using pTyping.Songs;
using pTyping.Songs.Events;
using sowelipisona;
using Path=Furball.Engine.Engine.Graphics.Drawables.Tweens.TweenTypes.BezierPathTween.Path;
// using Furball.Engine.Engine.Audio;

namespace pTyping.Graphics.Player;

public class Player : CompositeDrawable {
    public override Vector2 Size => new(FurballGame.DEFAULT_WINDOW_WIDTH, 100);

    public const int SCORE_EXCELLENT = 1500;
    public const int SCORE_GOOD      = 1000;
    public const int SCORE_FAIR      = 500;
    public const int SCORE_POOR      = 0;

    public const int SCORE_PER_CHARACTER = 500;
    public const int SCORE_COMBO         = 10;
    public const int SCORE_COMBO_MAX     = 1000;

    public float TIMING_EXCELLENT => 20  / (this.Song.Settings.Strictness / 5f);
    public float TIMING_GOOD      => 50  / (this.Song.Settings.Strictness / 5f);
    public float TIMING_FAIR      => 100 / (this.Song.Settings.Strictness / 5f);
    public float TIMING_POOR      => 200 / (this.Song.Settings.Strictness / 5f);

    public static readonly Color COLOR_EXCELLENT = new(255, 255, 0);
    public static readonly Color COLOR_GOOD      = new(0, 255, 0);
    public static readonly Color COLOR_FAIR      = new(0, 128, 255);
    public static readonly Color COLOR_POOR      = new(128, 128, 128);

    public const float NOTE_HEIGHT = 50f;

    public static readonly Vector2 NOTE_START_POS = new(FurballGame.DEFAULT_WINDOW_WIDTH + 200, NOTE_HEIGHT);
    public static readonly Vector2 NOTE_END_POS   = new(-100, NOTE_HEIGHT);

    public double BaseApproachTime = ConVars.BaseApproachTime.Value;
    public double CurrentApproachTime(double time) => this.BaseApproachTime / this.Song.CurrentTimingPoint(time).ApproachMultiplier;

    private readonly TexturedDrawable _recepticle;

    private readonly LinePrimitiveDrawable      _playfieldTopLine;
    private readonly LinePrimitiveDrawable      _playfieldBottomLine;
    private readonly RectanglePrimitiveDrawable _playfieldBackground;

    private readonly TextDrawable[] _typingIndicators = new TextDrawable[8];
    private          int            _currentTypingIndicatorIndex;
    private TextDrawable _currentTypingIndicator {
        get => this._typingIndicators[this._currentTypingIndicatorIndex];
        set => this._typingIndicators[this._currentTypingIndicatorIndex] = value;
    }

    private readonly List<NoteDrawable>                 _notes  = new();
    private readonly List<Tuple<ManagedDrawable, bool>> _events = new();

    public static readonly Vector2 RECEPTICLE_POS = new(FurballGame.DEFAULT_WINDOW_WIDTH * 0.15f, NOTE_HEIGHT);

    private readonly Texture2D _noteTexture;

    public Song Song;

    public PlayerScore Score;

    private int _noteToType;

    public SoundEffectPlayer HitSoundNormal = null;

    public bool RecordReplay = true;

    public bool IsSpectating = false;

    // private          bool              _playingReplay;
    // private readonly PlayerScore       _playingScoreReplay = new();
    public readonly List<ReplayFrame> ReplayFrames = new();

    public event EventHandler<Color> OnComboUpdate;
    public event EventHandler        OnAllNotesComplete;

    public Player(Song song) {
        this.Song = song;

        this.BaseApproachTime /= song.Settings.GlobalApproachMultiplier;

        this.Score            = new(this.Song.MapHash, ConVars.Username.Value);
        this.Score.Mods       = pTypingGame.SelectedMods;
        this.Score.ModsString = string.Join(',', this.Score.Mods);

        this._playfieldTopLine = new(new Vector2(0, 0), FurballGame.DEFAULT_WINDOW_WIDTH, 0) {
            ColorOverride = Color.Gray
        };
        this._playfieldBottomLine = new(new Vector2(0, 100), FurballGame.DEFAULT_WINDOW_WIDTH, 0) {
            ColorOverride = Color.Gray
        };
        this._drawables.Add(this._playfieldTopLine);
        this._drawables.Add(this._playfieldBottomLine);

        this._playfieldBackground = new(new(0, 0), new(FurballGame.DEFAULT_WINDOW_WIDTH, 100), 0f, true) {
            ColorOverride = new(100, 100, 100, 100),
            Depth         = 0.9f
        };

        this._drawables.Add(this._playfieldBackground);

        FileInfo[] noteFiles = this.Song.FileInfo.Directory?.GetFiles("note.png");

        this._noteTexture = noteFiles == null || noteFiles.Length == 0 ? ContentManager.LoadTextureFromFile("note.png", ContentSource.User)
                                : ContentManager.LoadTextureFromFile(noteFiles[0].FullName,                             ContentSource.External);


        this._recepticle = new TexturedDrawable(this._noteTexture, RECEPTICLE_POS) {
            Scale      = new(0.55f),
            OriginType = OriginType.Center
        };

        this._drawables.Add(this._recepticle);

        //Called before creating the notes
        this.Score.Mods.ForEach(mod => mod.BeforeNoteCreate(this));

        this.CreateNotes();
        this.CreateEvents();

        this.HitSoundNormal = FurballGame.AudioEngine.CreateSoundEffectPlayer(ContentManager.LoadRawAsset("hitsound.wav", ContentSource.User));

        ConVars.Volume.BindableValue.OnChange += this.OnVolumeChange;
        this.HitSoundNormal.Volume            =  ConVars.Volume.Value;

        //This wont be needed soon
        this._drawables = this._drawables.OrderByDescending(o => o.Depth).ToList();

        this.Play();

        foreach (PlayerMod mod in pTypingGame.SelectedMods)
            mod.OnMapStart(pTypingGame.MusicTrack, this._notes, this);
    }

    private void OnVolumeChange(object sender, float f) {
        this.HitSoundNormal.Volume = f;
    }

    private void CreateEvents() {
        for (int i = 0; i < this.Song.Events.Count; i++) {
            Event @event = this.Song.Events[i];

            ManagedDrawable drawable = Event.CreateEventDrawable(@event, this._noteTexture, new(this.CurrentApproachTime(@event.Time)));

            if (drawable != null) {
                drawable.TimeSource = pTypingGame.MusicTrackTimeSource;
                drawable.Depth      = 0.5f;

                this._events.Add(new(drawable, false));
            }
        }
    }

    private void CreateNotes() {
        foreach (Note note in this.Song.Notes) {
            NoteDrawable noteDrawable = this.CreateNote(note);

            this._notes.Add(noteDrawable);
        }
    }

    [Pure]
    private NoteDrawable CreateNote(Note note) {
        NoteDrawable noteDrawable = new(new(NOTE_START_POS.X, NOTE_START_POS.Y + note.YOffset), this._noteTexture, pTypingGame.JapaneseFont, 50) {
            TimeSource = pTypingGame.MusicTrackTimeSource,
            NoteTexture = {
                ColorOverride = note.Color
            },
            RawTextDrawable = {
                Text = $"{note.Text}"
            },
            ToTypeTextDrawable = {
                Text = $"{string.Join("\n", note.TypableRomaji.Romaji)}"
            },
            Scale      = new(0.55f),
            Depth      = 0f,
            OriginType = OriginType.Center,
            Note       = note
        };

        noteDrawable.UpdateTextPositions();

        noteDrawable.CreateTweens(new(this.CurrentApproachTime(note.Time)));

        return noteDrawable;
    }

    public void TypeCharacter(object sender, TextInputEventArgs e) => this.TypeCharacter(e);
    public void TypeCharacter(TextInputEventArgs args, bool checkingNext = false) {
        if (char.IsControl(args.Character))
            return;

        if (this.RecordReplay || this.IsSpectating) {
            ReplayFrame f = new() {
                Character = args.Character,
                Time      = pTypingGame.MusicTrackTimeSource.GetCurrentTime()
            };
            this.ReplayFrames.Add(f);
        }

        if (this.Song.AllNotesHit()) return;

        NoteDrawable noteDrawable = this._notes[checkingNext ? this._noteToType + 1 : this._noteToType];

        Note note = noteDrawable.Note;

        // Makes sure we dont hit an already hit note, which would cause a crash currently
        // this case *shouldnt* happen but it could so its good to check anyway
        if (note.IsHit)
            return;

        int currentTime = pTypingGame.MusicTrackTimeSource.GetCurrentTime();

        if (currentTime > note.Time - this.TIMING_POOR) {
            (string hiragana, List<string> romajiToType) = note.TypableRomaji;

            List<string> filteredRomaji = romajiToType.Where(romaji => romaji.StartsWith(note.TypedRomaji)).ToList();

            foreach (string romaji in filteredRomaji) {
                double timeDifference = Math.Abs(currentTime - note.Time);
                if (romaji[note.TypedRomaji.Length] == args.Character) {
                    if (checkingNext && !this._notes[this._noteToType].Note.IsHit) {
                        this._notes[this._noteToType].Miss();
                        this.NoteUpdate(false, this._notes[this._noteToType].Note);

                        this._noteToType++;
                        checkingNext = false;
                    }
                    
                    //If true, then we finished the note, if false, then we continue
                    if (noteDrawable.TypeCharacter(hiragana, romaji, timeDifference, this)) {
                        this.HitSoundNormal.PlayNew();
                        this.NoteUpdate(true, note);

                        this._noteToType += checkingNext ? 2 : 1;
                    }
                    this.ShowTypingIndicator(args.Character);

                    foreach (PlayerMod mod in pTypingGame.SelectedMods)
                        mod.OnCharacterTyped(note, args.Character.ToString(), true);

                    break;
                }

                //We do this so you can type the next note even if you fucked up the last one, which makes gameplay a lot easier
                if (this._noteToType != this.Song.Notes.Count - 1 && !checkingNext && currentTime > note.Time) {
                    this.TypeCharacter(args, true);
                    return;
                }
                
                this.ShowTypingIndicator(args.Character, true);

                foreach (PlayerMod mod in pTypingGame.SelectedMods)
                    mod.OnCharacterTyped(note, args.Character.ToString(), false);
            }
        }

        //Update the text on all notes to show the new Romaji paths
        this.UpdateNoteText();
    }

    private void ShowTypingIndicator(char character, bool miss = false) {
        if (this._currentTypingIndicator != null)
            this._drawables.Remove(this._currentTypingIndicator);

        if (this._currentTypingIndicator == null) {
            this._currentTypingIndicator = new(RECEPTICLE_POS, pTypingGame.JapaneseFont, character.ToString(), 60) {
                OriginType = OriginType.Center
            };
        } else {
            this._currentTypingIndicator.Tweens.Clear();
            this._currentTypingIndicator.Position = RECEPTICLE_POS;
            this._currentTypingIndicator.Text     = character.ToString();
        }

        this._drawables.Add(this._currentTypingIndicator);

        if (miss) {
            //random bool
            bool right = FurballGame.Random.Next(-1, 2) == 1;

            this._currentTypingIndicator.Tweens.Add(new ColorTween(TweenType.Color, new(200, 0, 0, 255), new(200, 0, 0, 0), FurballGame.Time, FurballGame.Time + 400));
            this._currentTypingIndicator.Tweens.Add(
            new PathTween(
            new Path(
            new PathSegment(
            this._currentTypingIndicator.Position,
            this._currentTypingIndicator.Position + new Vector2(FurballGame.Random.Next(9,  26) * (right ? 1 : -1), -FurballGame.Random.Next(9, 31)),
            this._currentTypingIndicator.Position + new Vector2(FurballGame.Random.Next(24, 46) * (right ? 1 : -1), FurballGame.Random.Next(29, 51))
            )
            ),
            FurballGame.Time,
            FurballGame.Time + 400
            )
            );
        } else {
            this._currentTypingIndicator.Tweens.Add(new ColorTween(TweenType.Color, Color.White, new(255, 255, 255, 0), FurballGame.Time, FurballGame.Time + 400));
            this._currentTypingIndicator.Tweens.Add(new VectorTween(TweenType.Scale, new(1f), new(1.5f), FurballGame.Time, FurballGame.Time                + 400));
        }

        this._currentTypingIndicatorIndex++;
        this._currentTypingIndicatorIndex %= this._typingIndicators.Length;
    }

    private void UpdateNoteText() {
        foreach (NoteDrawable noteDrawable in this._notes) {
            noteDrawable.RawTextDrawable.Text    = $"{noteDrawable.Note.Text}";
            noteDrawable.ToTypeTextDrawable.Text = $"{string.Join("\n", noteDrawable.Note.TypableRomaji.Romaji)}";

            noteDrawable.UpdateTextPositions();
        }
    }

    private void NoteUpdate(bool wasHit, Note note) {
        foreach (PlayerMod mod in pTypingGame.SelectedMods)
            mod.OnNoteHit(note);

        double numberHit = 0;
        double total     = 0;
        foreach (NoteDrawable noteDrawable in this._notes) {
            switch (noteDrawable.Note.HitResult) {
                case HitResult.Excellent:
                    numberHit++;
                    break;
                case HitResult.Good:
                    numberHit += (double)SCORE_GOOD / SCORE_EXCELLENT;
                    break;
                case HitResult.Fair:
                    numberHit += (double)SCORE_FAIR / SCORE_EXCELLENT;
                    break;
                case HitResult.Poor:
                    numberHit += (double)SCORE_POOR / SCORE_EXCELLENT;
                    break;
            }

            if (noteDrawable.Note.IsHit)
                total++;
        }

        if (total == 0) this.Score.Accuracy = 1d;
        else
            this.Score.Accuracy = numberHit / total;

        if (wasHit) {
            int scoreToAdd = note.HitResult switch {
                HitResult.Excellent => SCORE_EXCELLENT,
                HitResult.Fair      => SCORE_FAIR,
                HitResult.Good      => SCORE_GOOD,
                HitResult.Poor      => SCORE_POOR,
                _                   => 0
            };

            int scoreCombo = Math.Min(SCORE_COMBO * this.Score.Combo, SCORE_COMBO_MAX);
            this.Score.AddScore(scoreToAdd + scoreCombo);

            if (note.HitResult == HitResult.Poor)
                this.Score.Combo = 0;

            this.Score.Combo++;

            if (this.Score.Combo > this.Score.MaxCombo)
                this.Score.MaxCombo = this.Score.Combo;
        } else {
            if (this.Score.Combo > this.Score.MaxCombo)
                this.Score.MaxCombo = this.Score.Combo;

            this.Score.Combo = 0;
        }

        Color hitColor;
        switch (note.HitResult) {
            case HitResult.Excellent: {
                this.Score.ExcellentHits++;
                hitColor = COLOR_EXCELLENT;
                break;
            }
            case HitResult.Good: {
                this.Score.GoodHits++;
                hitColor = COLOR_GOOD;
                break;
            }
            case HitResult.Fair: {
                this.Score.FairHits++;
                hitColor = COLOR_FAIR;
                break;
            }
            default:
            case HitResult.Poor: {
                this.Score.PoorHits++;
                hitColor = COLOR_POOR;

                this.Score.Combo = 0;
                break;
            }
        }

        this.OnComboUpdate?.Invoke(this, hitColor);
    }

    public override void Update(GameTime time) {
        int currentTime = pTypingGame.MusicTrackTimeSource.GetCurrentTime();

        #region spawn notes and bars as needed

        for (int i = 0; i < this._notes.Count; i++) {
            NoteDrawable note = this._notes[i];

            if (note.Added) continue;

            if (currentTime < note.Note.Time - this.CurrentApproachTime(note.Note.Time)) continue;

            this._drawables.Add(note);
            note.Added = true;
        }

        for (int i = 0; i < this._events.Count; i++) {
            (ManagedDrawable drawable, bool added) = this._events[i];

            if (added) continue;

            if (currentTime < drawable.Tweens[0].StartTime) continue;

            this._drawables.Add(drawable);
            this._events[i] = new(drawable, true);
        }

        #endregion

        bool checkNoteHittability = true;

        if (this._noteToType == this._notes.Count) {
            this.EndScore();
            checkNoteHittability = false;
        }

        if (checkNoteHittability) {
            NoteDrawable noteToType = this._notes[this._noteToType];

            //Checks if the current note is not hit
            if (!noteToType.Note.IsHit && this._noteToType < this._notes.Count - 1) {
                NoteDrawable nextNoteToType = this._notes[this._noteToType + 1];

                //If we are within the next note
                if (currentTime > nextNoteToType.Note.Time) {
                    //Miss the note
                    noteToType.Miss();
                    //Tell the game to update all the info
                    this.NoteUpdate(false, noteToType.Note);
                    //Change us to the next note
                    this._noteToType++;
                }
            }

            foreach (Event cutOffEvent in this.Song.Events) {
                if (cutOffEvent is not TypingCutoffEvent) continue;

                if (currentTime > cutOffEvent.Time && cutOffEvent.Time > noteToType.Note.Time && !noteToType.Note.IsHit) {
                    //Miss the note
                    noteToType.Miss();
                    //Tell the game to update all the info
                    this.NoteUpdate(false, noteToType.Note);
                    //Change us to the next note
                    this._noteToType++;

                    break;
                }
            }
        }

        foreach (PlayerMod mod in this.Score.Mods)
            mod.Update(time);

        base.Update(time);

    }

    public override void Dispose(bool disposing) {
        ConVars.Volume.BindableValue.OnChange -= this.OnVolumeChange;

        base.Dispose(disposing);
    }

    public void CallMapEnd() {
        foreach (PlayerMod mod in pTypingGame.SelectedMods)
            mod.OnMapEnd(pTypingGame.MusicTrack, this._notes, this);
    }

    public void EndScore() {
        this.OnAllNotesComplete?.Invoke(this, EventArgs.Empty);
    }

    public void Play() {
        if (!this.IsSpectating)
            pTypingGame.PlayMusic();
        else
            pTypingGame.MusicTrack.Stop();
    }
}