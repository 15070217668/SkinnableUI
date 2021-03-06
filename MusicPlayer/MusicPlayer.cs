﻿/*
 *  Copyright 2014 Daniele Di Sarli
 *
 *  This file is part of SkinnableUI.
 *
 *  SkinnableUI is free software: you can redistribute it and/or modify
 *  it under the terms of the Lesser GNU General Public License as
 *  published by the Free Software Foundation, either version 3 of the
 *  License, or (at your option) any later version.
 *
 *  SkinnableUI is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  Lesser GNU General Public License for more details.
 *
 *  You should have received a copy of the Lesser GNU General Public License
 *  along with SkinnableUI. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio;
using NAudio.Wave;
using PlayerControls = SkinnableUI.SkinnableControls;

namespace Player
{
    public partial class MusicPlayer : Form
    {
        Dictionary<PlayerControls.SkinnableControl.SemanticType, List<PlayerControls.SkinnableControl>> controls = new Dictionary<PlayerControls.SkinnableControl.SemanticType, List<PlayerControls.SkinnableControl>>();
        List<PlayerControls.Button> play = new List<PlayerControls.Button>();
        List<PlayerControls.Button> pause = new List<PlayerControls.Button>();
        List<PlayerControls.ToggleButton> playPause = new List<PlayerControls.ToggleButton>();
        List<PlayerControls.Button> stop = new List<PlayerControls.Button>();
        List<PlayerControls.TrackBar> songProgress = new List<PlayerControls.TrackBar>();
        List<PlayerControls.Button> back = new List<PlayerControls.Button>();
        List<PlayerControls.Button> forward = new List<PlayerControls.Button>();

        List<PlayerControls.Label> title = new List<PlayerControls.Label>();
        List<PlayerControls.Label> artist = new List<PlayerControls.Label>();
        List<PlayerControls.Label> album = new List<PlayerControls.Label>();
        List<PlayerControls.Label> year = new List<PlayerControls.Label>();

        List<PlayerControls.Label> currentTime = new List<PlayerControls.Label>();
        List<PlayerControls.Label> totalTime = new List<PlayerControls.Label>();
        List<PlayerControls.Label> remainingTime = new List<PlayerControls.Label>();

        List<PlayerControls.ListView> playlist = new List<PlayerControls.ListView>();

        List<PlayerControls.PictureBox> albumArt = new List<PlayerControls.PictureBox>();

        Mp3FileReader mp3Reader;
        WaveOut waveOut;

        Timer tmr = new Timer { Interval = 100 };

        PlayerControls.ListView.ListViewRow currentSong;

        public MusicPlayer()
        {
            InitializeComponent();

            foreach (PlayerControls.SkinnableControl.SemanticType c in Enum.GetValues(typeof(PlayerControls.SkinnableControl.SemanticType)))
                this.controls.Add(c, new List<PlayerControls.SkinnableControl>());
            
            waveOut = new WaveOut();
            waveOut.PlaybackStopped += waveOut_PlaybackStopped;
            
            tmr.Tick += tmr_Tick;
        }

        void waveOut_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            if (mp3Reader != null && mp3Reader.CurrentTime >= mp3Reader.TotalTime.Add(new TimeSpan(0, 0, 0, 0, -100)))
            {
                var pl = playlist.FirstOrDefault();
                if (pl != null)
                {
                    for (int i = 0; i < pl.Items.Count; i++)
                    {
                        if (pl.Items[i] == currentSong)
                        {
                            if (i + 1 < pl.Items.Count)
                            {
                                currentSong = pl.Items[i + 1];
                                LoadSong(currentSong.Values[3].ToString(), currentSong);
                            }
                            else
                            {
                                currentSong = pl.Items[0];
                                LoadSong(currentSong.Values[3].ToString(), currentSong);
                                stopAction();
                            }
                            break;
                        }
                    }
                }
            }
        }

        void tmr_Tick(object sender, EventArgs e)
        {
            if (mp3Reader != null)
            {
                songProgress.ForEach(item => item.Value = (int)mp3Reader.CurrentTime.TotalMilliseconds);
                currentTime.ForEach(item => item.Text = FormatTime(mp3Reader.CurrentTime, mp3Reader.TotalTime));
                remainingTime.ForEach(item => item.Text = FormatTime(mp3Reader.TotalTime - mp3Reader.CurrentTime, mp3Reader.TotalTime));
            }
        }

        string FormatTime(TimeSpan time, TimeSpan totalTime)
        {
            if (totalTime.Hours != 0)
                return time.ToString(@"h\:mm\:ss");
            else
                return time.ToString(@"mm\:ss");
        }

        private void playerView1_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (var file in files.OrderBy(k => k))
                {
                    TagLib.File f = null;
                    try
                    {
                        f = TagLib.File.Create(file);
                    }
                    catch (TagLib.UnsupportedFormatException)
                    {

                    }

                    foreach (var playlist in this.playlist)
                    {
                        var row = new PlayerControls.ListView.ListViewRow();
                        if (f != null)
                        {
                            row.Values.Add(f.Tag.Title.Trim() == "" ? file : f.Tag.Title);
                            row.Values.Add(f.Tag.JoinedPerformers.Trim() != "" ? f.Tag.JoinedPerformers : f.Tag.JoinedAlbumArtists);
                            row.Values.Add(f.Tag.Album);
                        }
                        else
                        {
                            row.Values.Add(file);
                            row.Values.Add("");
                            row.Values.Add("");
                        }
                        row.Values.Add(file);
                        playlist.Items.Add(row);
                    }
                }
            }
        }

        List<T> GetControls<T>(PlayerControls.SkinnableControl.SemanticType type) where T : PlayerControls.SkinnableControl
        {
            var tmp = from ctl in controls[type]
                      where ctl is T
                      select (T)ctl;

            return tmp.ToList();
        }

        /*void ResetUI()
        {
            this.controls.Clear();
            play.Clear();
            songProgress.Clear();

            foreach (PlayerControls.PlayerControl.SemanticType c in Enum.GetValues(typeof(PlayerControls.PlayerControl.SemanticType)))
                this.controls.Add(c, new List<PlayerControls.PlayerControl>());
        }*/

        private void AttachEvents()
        {
            //playerView1.Resize += (sender, e) => this.ClientSize = playerView1.Size;
            this.ClientSize = playerView1.Size;

            var ctrls = this.playerView1.ContainerControl.GetAllChildren();
            foreach (var item in ctrls)
            {
                this.controls[item.Semantic].Add(item);
            }

            play = GetControls<PlayerControls.Button>(PlayerControls.SkinnableControl.SemanticType.Play);
            pause = GetControls<PlayerControls.Button>(PlayerControls.SkinnableControl.SemanticType.Pause);
            playPause = GetControls<PlayerControls.ToggleButton>(PlayerControls.SkinnableControl.SemanticType.PlayPause);
            stop = GetControls<PlayerControls.Button>(PlayerControls.SkinnableControl.SemanticType.Stop);
            back = GetControls<PlayerControls.Button>(PlayerControls.SkinnableControl.SemanticType.Back);
            forward = GetControls<PlayerControls.Button>(PlayerControls.SkinnableControl.SemanticType.Forward);
            songProgress = GetControls<PlayerControls.TrackBar>(PlayerControls.SkinnableControl.SemanticType.SongProgress);
            title = GetControls<PlayerControls.Label>(PlayerControls.SkinnableControl.SemanticType.Title);
            artist = GetControls<PlayerControls.Label>(PlayerControls.SkinnableControl.SemanticType.Artist);
            album = GetControls<PlayerControls.Label>(PlayerControls.SkinnableControl.SemanticType.Album);
            year = GetControls<PlayerControls.Label>(PlayerControls.SkinnableControl.SemanticType.Year);
            currentTime = GetControls<PlayerControls.Label>(PlayerControls.SkinnableControl.SemanticType.CurrentTime);
            totalTime = GetControls<PlayerControls.Label>(PlayerControls.SkinnableControl.SemanticType.TotalTime);
            remainingTime = GetControls<PlayerControls.Label>(PlayerControls.SkinnableControl.SemanticType.RemainingTime);
            playlist = GetControls<PlayerControls.ListView>(PlayerControls.SkinnableControl.SemanticType.Playlist);
            albumArt = GetControls<PlayerControls.PictureBox>(PlayerControls.SkinnableControl.SemanticType.AlbumArt);

            play.ForEach(c => c.Click += play_Click);
            pause.ForEach(c => c.Click += pause_Click);
            playPause.ForEach(c => c.CheckedChanged += playPause_CheckedChanged);
            stop.ForEach(c => c.Click += stop_Click);
            back.ForEach(c => c.Click += back_Click);
            forward.ForEach(c => c.Click += forward_Click);
            songProgress.ForEach(c => c.UserChangedValue += songProgress_UserChangedValue);
            playlist.ForEach(c => c.MouseDoubleClick += playlist_MouseDoubleClick);

        }

        void LoadSong(string path, PlayerControls.ListView.ListViewRow row)
        {
            playlist.ForEach(c => c.ActiveRow = row);
            songProgress.ForEach(item => item.Value = 0);

            if (path == null)
            {
                currentTime.ForEach(item => item.Text = "--:--");
                totalTime.ForEach(item => item.Text = "--:--");
                remainingTime.ForEach(item => item.Text = "--:--");

                title.ForEach(item => item.Text = "");
                artist.ForEach(item => item.Text = "");
                album.ForEach(item => item.Text = "");
                year.ForEach(item => item.Text = "");
                albumArt.ForEach(c => c.Image = null);

                playPause.ForEach(c => c.Checked = false);
            }
            else
            {
                if (mp3Reader != null)
                {
                    mp3Reader.Dispose();
                }

                mp3Reader = new Mp3FileReader(path);
                waveOut.Init(mp3Reader);
                
                songProgress.ForEach(item => item.Maximum = (int)mp3Reader.TotalTime.TotalMilliseconds);
                currentTime.ForEach(item => item.Text = FormatTime(mp3Reader.CurrentTime, mp3Reader.TotalTime));
                totalTime.ForEach(item => item.Text = FormatTime(mp3Reader.TotalTime, mp3Reader.TotalTime));
                remainingTime.ForEach(item => item.Text = FormatTime(mp3Reader.TotalTime - mp3Reader.CurrentTime, mp3Reader.TotalTime));

                TagLib.File f = TagLib.File.Create(path);
                title.ForEach(item => item.Text = f.Tag.Title);
                artist.ForEach(item => item.Text = f.Tag.JoinedPerformers.Trim() != "" ? f.Tag.JoinedPerformers : f.Tag.JoinedAlbumArtists);
                album.ForEach(item => item.Text = f.Tag.Album);
                year.ForEach(item => item.Text = f.Tag.Year.ToString());
                if (f.Tag.Pictures.Length > 0)
                {
                    var bin = (byte[])(f.Tag.Pictures[0].Data.Data);
                    var img = Image.FromStream(new System.IO.MemoryStream(bin)); //.GetThumbnailImage(100, 100, null, IntPtr.Zero);
                    albumArt.ForEach(item => item.Image = img);
                }
                else
                {
                    albumArt.ForEach(item => item.Image = null);
                }

                playPause.ForEach(c => c.Checked = true);
                waveOut.Play();
            }
        }

        void playlist_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            var lstv = (PlayerControls.ListView)sender;
            var row = lstv.RowHitTest(e.X, e.Y);
            if (row != null)
            {
                currentSong = row;
                LoadSong(row.Values[3].ToString(), row);
            }
        }

        void stop_Click(object sender, EventArgs e)
        {
            stopAction();
        }

        void stopAction()
        {
            playPause.ForEach(item => item.Checked = false);
            waveOut.Stop();
            if (mp3Reader != null)
            {
                mp3Reader.Seek(0, System.IO.SeekOrigin.Begin);
            }
        }

        void back_Click(object sender, EventArgs e)
        {
            var pl = playlist.FirstOrDefault();
            if (pl != null)
            {
                for (int i = 0; i < pl.Items.Count; i++)
                {
                    if (pl.Items[i] == currentSong) {
                        currentSong = pl.Items[MathMod(i-1, pl.Items.Count)];
                        LoadSong(currentSong.Values[3].ToString(), currentSong);
                        break;
                    }
                }
            }
        }

        static int MathMod(int a, int b) {
            return (Math.Abs(a * b) + a) % b;
        }

        void forward_Click(object sender, EventArgs e)
        {
            var pl = playlist.FirstOrDefault();
            if (pl != null)
            {
                for (int i = 0; i < pl.Items.Count; i++)
                {
                    if (pl.Items[i] == currentSong)
                    {
                        currentSong = pl.Items[(i + 1) % pl.Items.Count];
                        LoadSong(currentSong.Values[3].ToString(), currentSong);
                        break;
                    }
                }
            }
        }

        void playPause_CheckedChanged(object sender, EventArgs e)
        {
            var ctl = (PlayerControls.ToggleButton)sender;
            if (ctl.Checked)
            {
                if (mp3Reader != null) waveOut.Play();
            }
            else
            {
                waveOut.Pause();
            }
        }

        private void pause_Click(object sender, EventArgs e)
        {
            waveOut.Pause();
        }

        void songProgress_UserChangedValue(object sender, EventArgs e)
        {
            if (mp3Reader != null)
            {
                var tb = (PlayerControls.TrackBar)sender;
                mp3Reader.CurrentTime = new TimeSpan(0, 0, 0, 0, tb.Value);
            }
        }

        void play_Click(object sender, EventArgs e)
        {
            if (mp3Reader != null) waveOut.Play();
        }

        private void playerView1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void playerView1_DragOver(object sender, DragEventArgs e)
        {

        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.waveOut != null) this.waveOut.Dispose();
                if (this.mp3Reader != null) this.mp3Reader.Dispose();

                if(components != null)
                    components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void MusicPlayer_Shown(object sender, EventArgs e)
        {
            OpenFileDialog fd = new OpenFileDialog { Filter = "Skin file|*.skn" };
            if (fd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                this.SuspendLayout();
                playerView1.Visible = false;

                this.Controls.Remove(this.panelLoadSkin);

                playerView1.LoadSkin(fd.FileName);
                this.ClientSize = playerView1.Size;
                playerView1.Width -= 1;
                playerView1.Width += 1;
                playerView1.Dock = DockStyle.Fill;
                AttachEvents();

                playlist.ForEach(item => item.Items.Clear());

                LoadSong(null, null);

                tmr.Start();

                playerView1.Visible = true;
                this.ResumeLayout();
            }
            else
            {
                this.Close();
            }
        }
    }
}
