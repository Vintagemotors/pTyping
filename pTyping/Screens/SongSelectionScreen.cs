using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using ManagedBass;
using pTyping.Songs;
using pTyping.Player;
using Furball.Engine;
using Furball.Engine.Engine;
using Furball.Engine.Engine.Graphics;
using Furball.Engine.Engine.Graphics.Drawables;
using Furball.Engine.Engine.Graphics.Drawables.Tweens;
using Furball.Engine.Engine.Graphics.Drawables.UiElements;
using Furball.Engine.Engine.Graphics.Drawables.Tweens.TweenTypes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace pTyping.Screens {
    public class SongSelectionScreen : Screen {
        private bool _editor;

        private TextDrawable _songInfo;

        private List<BaseDrawable> _songButtonList    = new();
        private List<BaseDrawable> _scoreDrawableList = new();

        private float _movingDirection;

        public SongSelectionScreen(bool editor) => this._editor = editor;

        public bool GetScoresFromOnline = true;

        public override void Initialize() {
            base.Initialize();

            if (!this._editor && SongManager.Songs.Count == 0) {
                ScreenManager.ChangeScreen(new MenuScreen());
                return;
            }

            #region Back button

            pTypingGame.LoadBackButtonTexture();

            TexturedDrawable backButton = new(pTypingGame.BackButtonTexture, new Vector2(0, FurballGame.DEFAULT_WINDOW_HEIGHT)) {
                OriginType = OriginType.BottomLeft,
                Scale      = new(0.4f, 0.4f)
            };

            backButton.OnClick += delegate {
                pTypingGame.MenuClickSound.Play();
                ScreenManager.ChangeScreen(new MenuScreen());
            };

            this.Manager.Add(backButton);

            #endregion

            #region Song speed buttons

            if (!this._editor) {
                UiButtonDrawable audioSpeed1 = new(
                new(backButton.Size.X + 10, FurballGame.DEFAULT_WINDOW_HEIGHT),
                "0.75x speed",
                FurballGame.DEFAULT_FONT,
                30,
                Color.Red,
                Color.White,
                Color.White,
                new(0),
                this.AudioSpeed1OnClick
                ) {
                    OriginType = OriginType.BottomLeft
                };

                this.Manager.Add(audioSpeed1);

                UiButtonDrawable audioSpeed2 = new(
                new(backButton.Size.X + audioSpeed1.Size.X + 20, FurballGame.DEFAULT_WINDOW_HEIGHT),
                "1.5x speed",
                FurballGame.DEFAULT_FONT,
                30,
                Color.Red,
                Color.White,
                Color.White,
                new(0),
                this.AudioSpeed2OnClick
                ) {
                    OriginType = OriginType.BottomLeft
                };

                this.Manager.Add(audioSpeed2);
            }

            #endregion

            #region Create new song button

            if (this._editor) {
                EventHandler<Point> newSongOnClick = delegate {
                    pTypingGame.MenuClickSound.Play();
                    ScreenManager.ChangeScreen(new NewSongScreen());
                };

                UiButtonDrawable createNewSongButton = new(
                new Vector2(backButton.Size.X + 10f, FurballGame.DEFAULT_WINDOW_HEIGHT),
                "Create Song",
                FurballGame.DEFAULT_FONT,
                30,
                Color.Blue,
                Color.White,
                Color.White,
                Vector2.Zero,
                newSongOnClick
                ) {
                    OriginType = OriginType.BottomLeft,
                    Depth      = 0.5f
                };

                this.Manager.Add(createNewSongButton);
            }

            #endregion

            #region Create new buttons for each song

            float tempY = 50;

            IEnumerable<Song> songList = this._editor ? SongManager.Songs.Where(x => x.Type == SongType.pTyping) : SongManager.Songs;

            foreach (Song song in songList) {
                UiButtonDrawable songButton = new(
                new Vector2(FurballGame.DEFAULT_WINDOW_WIDTH - 50, tempY),
                $"{song.Artist} - {song.Name} [{song.Difficulty}]",
                pTypingGame.JapaneseFont,
                35,
                song.Type == SongType.pTyping ? Color.Blue : Color.Green,
                Color.Black,
                Color.Black,
                new Vector2(650, 50)
                ) {
                    OriginType = OriginType.TopRight,
                    TextDrawable = {
                        OriginType = OriginType.RightCenter
                    },
                    Depth = 0.9f
                };

                songButton.OnClick += delegate {
                    pTypingGame.CurrentSong.Value = song;
                };

                this._songButtonList.Add(songButton);

                this.Manager.Add(songButton);

                tempY += 60;
            }

            #endregion

            #region Start button

            UiButtonDrawable startButton = new(
            new Vector2(FurballGame.DEFAULT_WINDOW_WIDTH, FurballGame.DEFAULT_WINDOW_HEIGHT),
            "Start!",
            FurballGame.DEFAULT_FONT,
            60,
            Color.Red,
            Color.White,
            Color.White,
            new(0)
            ) {
                OriginType = OriginType.BottomRight
            };

            startButton.OnClick += delegate {
                this.PlaySelectedMap();
            };

            this.Manager.Add(startButton);

            #endregion

            #region Song info

            this._songInfo = new TextDrawable(new Vector2(10, 10), pTypingGame.JapaneseFont, "", 35);

            this.Manager.Add(this._songInfo);

            #endregion

            #region background image

            this.Manager.Add(pTypingGame.CurrentSongBackground);
            pTypingGame.CurrentSongBackground.Tweens.Add(
            new ColorTween(
            TweenType.Color,
            pTypingGame.CurrentSongBackground.ColorOverride,
            new Color(175, 175, 175),
            pTypingGame.CurrentSongBackground.TimeSource.GetCurrentTime(),
            pTypingGame.CurrentSongBackground.TimeSource.GetCurrentTime() + 100
            )
            );

            #endregion

            if (pTypingGame.CurrentSong.Value == null && SongManager.Songs.Count > 0)
                pTypingGame.CurrentSong.Value = SongManager.Songs[0];
            else if (pTypingGame.CurrentSong?.Value != null)
                this.UpdateSelectedSong(true);

            pTypingGame.CurrentSong.OnChange += this.OnSongChange;

            FurballGame.InputManager.OnKeyDown += this.OnKeyDown;
            FurballGame.InputManager.OnKeyUp   += this.OnKeyUp;

            FurballGame.InputManager.OnMouseScroll += this.OnMouseScroll;
            
            pTypingGame.UserStatusPickingSong();
        }

        private void AudioSpeed2OnClick(object sender, Point e) {
            pTypingGame.MusicTrack.Frequency *= 1.5f;
        }

        private void AudioSpeed1OnClick(object sender, Point e) {
            pTypingGame.MusicTrack.Frequency *= 0.75f;
        }

        private void OnMouseScroll(object sender, (int scrollAmount, string cursorName) e) {
            foreach (BaseDrawable songButton in this._songButtonList)
                songButton.Position += new Vector2(0f, e.scrollAmount);
        }

        public override void Update(GameTime gameTime) {
            if (this._movingDirection != 0f)
                foreach (BaseDrawable songButton in this._songButtonList)
                    songButton.Position += new Vector2(0f, this._movingDirection * (gameTime.ElapsedGameTime.Ticks / 10000f));

            base.Update(gameTime);
        }

        private void OnKeyDown(object sender, Keys e) {
            this._movingDirection = e switch {
                Keys.Up   => 1f,
                Keys.Down => -1f,
                _         => this._movingDirection
            };

            if (e == Keys.F5) {
                SongManager.UpdateSongs();
                ScreenManager.ChangeScreen(new SongSelectionScreen(this._editor));
            }
        }

        private void OnKeyUp(object sender, Keys e) {
            this._movingDirection = e switch {
                Keys.Up or Keys.Down => 0f,
                _                    => this._movingDirection
            };
        }

        private void OnSongChange(object sender, Song e) {
            this.UpdateSelectedSong();
        }

        public void PlaySelectedMap() {
            pTypingGame.MenuClickSound.Play();
            ScreenManager.ChangeScreen(this._editor ? new EditorScreen() : new PlayerScreen());
        }

        public void UpdateSelectedSong(bool fromPrevScreen = false) {
            this._songInfo.Text =
                $"{pTypingGame.CurrentSong.Value.Artist} - {pTypingGame.CurrentSong.Value.Name} [{pTypingGame.CurrentSong.Value.Difficulty}]\nCreated by {pTypingGame.CurrentSong.Value.Creator}";

            string qualifiedAudioPath = Path.Combine(pTypingGame.CurrentSong.Value.FileInfo.DirectoryName ?? string.Empty, pTypingGame.CurrentSong.Value.AudioPath);

            if (!fromPrevScreen) {
                pTypingGame.LoadMusic(ContentManager.LoadRawAsset(qualifiedAudioPath, ContentSource.External));
                pTypingGame.PlayMusic();
            } else if (pTypingGame.MusicTrack.PlaybackState is PlaybackState.Paused or PlaybackState.Stopped) {
                pTypingGame.PlayMusic();
            }

            pTypingGame.LoadBackgroundFromSong(pTypingGame.CurrentSong.Value);

            #region Scores
            this._scoreDrawableList.ForEach(x => this.Manager.Remove(x));

            List<PlayerScore> origScores;

            if (this.GetScoresFromOnline) {
                Task<List<PlayerScore>> task = pTypingGame.OnlineManager.GetMapScores(pTypingGame.CurrentSong.Value.MapHash);
                
                task.Wait();
                
                origScores = task.Result;
            }
            else
                origScores = pTypingGame.ScoreManager.GetScores(pTypingGame.CurrentSong.Value.MapHash);

            List<PlayerScore> scores = origScores.OrderByDescending(x => x.Score).ToList();
            
            float y = this._songInfo.Size.Y + 25;
            for (int i = 0; i < scores.Count; i++) {
                PlayerScore score = scores[i];
                
                TextDrawable scoreInfo = new(
                new(15f, y),
                pTypingGame.JapaneseFont,
                $"{score.Username}\nScore: {score.Score}, {score.Accuracy * 100:00.##}%, {score.Combo}x",
                25
                );

                y += scoreInfo.Size.Y + 10;

                this._scoreDrawableList.Add(scoreInfo);
                this.Manager.Add(scoreInfo);
            }

            #endregion
        }

        protected override void Dispose(bool disposing) {
            FurballGame.InputManager.OnKeyDown     -= this.OnKeyDown;
            FurballGame.InputManager.OnKeyUp       -= this.OnKeyUp;
            FurballGame.InputManager.OnMouseScroll -= this.OnMouseScroll;

            base.Dispose(disposing);
        }
    }
}
