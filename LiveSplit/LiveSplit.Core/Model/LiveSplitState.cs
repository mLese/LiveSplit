﻿using LiveSplit.Model.Input;
using LiveSplit.Options;
using LiveSplit.UI;
using LiveSplit.Web.Share;
using System;
using System.Linq;
using System.Threading.Tasks;
using Forms = System.Windows.Forms;

namespace LiveSplit.Model
{
    public class LiveSplitState : ICloneable
    {
        public IRun Run { get; set; }
        public ILayout Layout { get; set; }
        public LayoutSettings LayoutSettings { get; set; }
        public ISettings Settings { get; set; }
        public Forms.Form Form { get; set; }

        public AtomicDateTime AttemptStarted { get; set; }
        public AtomicDateTime AttemptEnded { get; set; }

        public TimeStamp StartTime { get; set; }
        public TimeSpan PauseTime { get; set; }
        public TimeSpan? GameTimePauseTime { get; set; }
        public TimerPhase CurrentPhase { get; set; }
        public string CurrentComparison { get; set; }
        public TimingMethod CurrentTimingMethod { get; set; }

        internal TimeSpan? loadingTimes;
        public TimeSpan LoadingTimes { get { return loadingTimes ?? TimeSpan.Zero; } set { loadingTimes = value; } }
        public bool IsGameTimeInitialized
        {
            get
            {
                return loadingTimes.HasValue;
            }
            set
            {
                if (value)
                {
                    if (loadingTimes == null)
                        loadingTimes = TimeSpan.Zero;
                }
                else
                    loadingTimes = null;
            }
        }
        private bool isGameTimePaused;
        public bool IsGameTimePaused
        {
            get { return isGameTimePaused; }
            set
            {
                if (!value && isGameTimePaused)
                    LoadingTimes = CurrentTime.RealTime.Value - (CurrentTime.GameTime ?? CurrentTime.RealTime.Value);
                else if (value && !isGameTimePaused)
                    GameTimePauseTime = (CurrentTime.GameTime ?? CurrentTime.RealTime);

                isGameTimePaused = value;
            }
        }

        public event EventHandler OnSplit;
        public event EventHandler OnUndoSplit;
        public event EventHandler OnSkipSplit;
        public event EventHandler OnStart;
        public event EventHandlerT<TimerPhase> OnReset;
        public event EventHandler OnPause;
        public event EventHandler OnResume;
        public event EventHandler OnScrollUp;
        public event EventHandler OnScrollDown;
        public event EventHandler OnSwitchComparisonPrevious;
        public event EventHandler OnSwitchComparisonNext;

        public event EventHandler RunManuallyModified;
        public event EventHandler ComparisonRenamed;

        public Time CurrentTime
        { 
            get 
            {
                var curTime = new Time();

                if (CurrentPhase == TimerPhase.NotRunning)
                    curTime.RealTime = TimeSpan.Zero;
                else if (CurrentPhase == TimerPhase.Running)
                    curTime.RealTime = TimeStamp.Now - StartTime;
                else if (CurrentPhase == TimerPhase.Paused)
                    curTime.RealTime = PauseTime;
                else
                    curTime.RealTime = Run.Last().SplitTime.RealTime;

                if (CurrentPhase == TimerPhase.Ended)
                    curTime.GameTime = Run.Last().SplitTime.GameTime;
                else
                    curTime.GameTime = IsGameTimePaused 
                        ? GameTimePauseTime 
                        : curTime.RealTime - (IsGameTimeInitialized ? (TimeSpan?)LoadingTimes : null);

                return curTime;
            }
        }

        public int CurrentSplitIndex { get; set; }
        public ISegment CurrentSplit { get { return (CurrentSplitIndex >= 0 && CurrentSplitIndex < Run.Count) ? Run[CurrentSplitIndex] : null; } }

        private LiveSplitState() { }

        public LiveSplitState(IRun run, Forms.Form form, ILayout layout, LayoutSettings layoutSettings, ISettings settings)
        {
            Run = run;
            Form = form;
            Layout = layout;
            Settings = settings;
            LayoutSettings = layoutSettings;
            StartTime = TimeStamp.Now;
            CurrentPhase = TimerPhase.NotRunning;
            CurrentSplitIndex = -1;
            //DrawLock = new ReaderWriterLockSlim();
        }

        public object Clone()
        {
            return new LiveSplitState()
            {
                Run = Run.Clone() as IRun,
                Form = Form,
                Layout = Layout.Clone() as ILayout,
                Settings = Settings.Clone() as ISettings,
                LayoutSettings = LayoutSettings.Clone() as LayoutSettings,
                StartTime = StartTime,
                PauseTime = PauseTime,
                GameTimePauseTime = GameTimePauseTime,
                isGameTimePaused = isGameTimePaused,
                LoadingTimes = LoadingTimes,
                CurrentPhase = CurrentPhase,
                CurrentSplitIndex = CurrentSplitIndex,
                CurrentComparison = CurrentComparison,
                CurrentTimingMethod = CurrentTimingMethod,
                AttemptStarted = AttemptStarted,
                AttemptEnded = AttemptEnded
            };
        }

        public void RegisterTimerModel(ITimerModel model)
        {
            model.OnSplit += (s, e) =>
            {
                if (OnSplit != null)
                    OnSplit(this, e);
            };
            model.OnSkipSplit += (s, e) =>
            {
                if (OnSkipSplit != null)
                    OnSkipSplit(this, e);
            };
            model.OnUndoSplit += (s, e) =>
            {
                if (OnUndoSplit != null)
                    OnUndoSplit(this, e);
            };
            model.OnStart += (s, e) =>
            {
                if (OnStart != null)
                    OnStart(this, e);
            };
            model.OnReset += (s, e) =>
            {
                if (OnReset != null)
                    OnReset(this, e);
            };
            model.OnPause += (s, e) =>
            {
                if (OnPause != null)
                    OnPause(this, e);
            };
            model.OnResume += (s, e) =>
            {
                if (OnResume != null)
                    OnResume(this, e);
            };
            model.OnScrollUp += (s, e) =>
            {
                if (OnScrollUp != null)
                    OnScrollUp(this, e);
            };
            model.OnScrollDown += (s, e) =>
            {
                if (OnScrollDown != null)
                    OnScrollDown(this, e);
            };
            model.OnSwitchComparisonPrevious += (s, e) =>
            {
                if (OnSwitchComparisonPrevious != null)
                    OnSwitchComparisonPrevious(this, e);
            };
            model.OnSwitchComparisonNext += (s, e) =>
            {
                if (OnSwitchComparisonNext != null)
                    OnSwitchComparisonNext(this, e);
            };
        }

        public void SetGameTime(TimeSpan? gameTime)
        {
            if (CurrentTime.RealTime.HasValue && gameTime.HasValue)
            {
                LoadingTimes = CurrentTime.RealTime.Value - gameTime.Value;
                if (IsGameTimePaused)
                    GameTimePauseTime = gameTime.Value;
            }
        }

        public void FixTimingMethodFromRuleset()
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    if (Run.Metadata.Game != null)
                    {
                        if (Run.Metadata.Game.Ruleset.DefaultTimingMethod == SpeedrunComSharp.TimingMethod.RealTime)
                            CurrentTimingMethod = TimingMethod.RealTime;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
            });
        }

        public void CallRunManuallyModified()
        {
            if (RunManuallyModified != null)
                RunManuallyModified(this, null);
        }

        public void CallComparisonRenamed(EventArgs e)
        {
            if (ComparisonRenamed != null)
                ComparisonRenamed(this, e);
        }
    }
}
