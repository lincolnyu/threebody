using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Xml;
using ThreeBody.Properties;
using ThreeBodyEngine;

namespace ThreeBody
{
    public partial class ThreeBodyForm : Form
    {
        #region Enumerations

        public enum States
        {
            Paused,
            Running,
            GoingToTime,
            Loading,
            Closing,
        }

        #endregion

        #region Fields

        #region Product

        private const string AppTitle = "Three Body";

        #endregion

        #region IO

        private const string SettingXmlFile = "system.xml";

        #endregion

        #region Threading

        private ManualResetEvent _resumeEvent;

        #endregion

        #region current operational states

        private States _state;

        #endregion

        #region Display buffers

        private readonly Bitmap[] _buffers = new Bitmap[1];
        private int _currBufferIndex;

        #endregion

        #region Graphics related

        private readonly List<Brush> _brushes = new List<Brush>();
        private readonly List<Pen> _pens = new List<Pen>();

        #endregion

        #region Display transformation related

        private double _screenCoeff = 0.2;
        private double _pc;

        #endregion

        #region Temporal settings for simulation

        /// <summary>
        ///  the time step for the simulation
        /// </summary>
        private double _timeStep = 10;

        /// <summary>
        ///  in seconds, about (11.57 days)
        /// </summary>
        private const double SimTimeRatioFactor = 1000000;

        /// <summary>
        ///  world time vs screen time (if 1 sec play amounts to 'SimTimeRatioFactor' secs, then it's 1; if it does 2000000
        ///  twice 'SimTimeRatioFactor', then it's 2)
        /// </summary>
        private double _simTimeRatio = 1;

        /// <summary>
        ///  Approximate fps (constant just for now)
        /// </summary>
        private const double Fps = 30;

        private double _timeStepsEachFrame;

        private double _frameWorldTime;

        #endregion

        #region display modes

        /// <summary>
        ///  The index of the object being locked to the centre of the screen
        /// </summary>
        private int _lockIndex;

        private DateTime _displayRequestStart;
        private bool _toDisplay;
        private bool _osdOn;
        private bool _showOrbits;

        private const double OsdDuration = 5;

        #endregion

        #region User input

        private int _wheelX, _wheelY;
        private double _wheelRealX, _wheelRealY;

        private bool _dragging;
        private int _mouseDownX, _mouseDownY;
        private double _mouseDownRealX, _mouseDownRealY;

        private bool _controlDown;
        private bool _altDown;

        #endregion

        #region Physical constants

        private const double Au = 1.4959787E11; // in meters

        #endregion

        #region In memory model data

        private PlanetarySystem _sys;

        #endregion

        #region temporal parameters

        private TimeSpan _elapsed;
        private TimeSpan _gotoTime;

        /// <summary>
        ///  invokes UpdateView every such number of timesteps for the going to process
        /// </summary>
        private const int GoToTimeUpdateRate = 10000;

        #endregion

        #region current physical states

        private int _clashCount;

        #endregion

        #endregion

        #region Methods

        public ThreeBodyForm()
        {
            InitializeComponent();

            MouseWheel += SpacePbxOnMouseWheel;
        }

        #endregion

        #region Methods

        #region Event handlers

        private void ThreeBodyFormLoad(object sender, EventArgs e)
        {
            InitScreenSettings();
            InitBuffers();
            LoadFromXml();
            UpdateOsdState();

            SetupAndStartThread();

            SetTitle();
        }

        /// <summary>
        ///  Sets app title as per app name and version
        /// </summary>
        /// <remarks>
        ///  References:
        ///  1. http://stackoverflow.com/questions/22527830/how-to-get-the-publish-version-of-a-wpf-application
        /// </remarks>
        private void SetTitle()
        {
            try
            {
                var ver = System.Deployment.Application.ApplicationDeployment.CurrentDeployment.
                    CurrentVersion;
                Text = string.Format("{0} (Ver {1}.{2})", AppTitle, ver.Major, ver.Minor);
            }
            catch (System.Deployment.Application.InvalidDeploymentException)
            {
                var ver = Assembly.GetExecutingAssembly().GetName().Version;
                Text = string.Format("{0} (Asm Ver {1}.{2})", AppTitle, ver.Major, ver.Minor);
            }
        }

        private void SetStateTo(States state)
        {
            var oldState = _state;
            _state = state;
            if (_state != States.Paused && oldState == States.Paused)
            {
                SetResumeEvent();
            }
        }

        private void SetupAndStartThread()
        {
            _resumeEvent = new ManualResetEvent(false);

            SetStateTo(States.Paused);
            var t = new Thread(EngineThread);
            t.Start();
        }

        private void ThreeBodyForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            SetStateTo(States.Closing);
        }

        private void DemoSolarToolStripMenuItemClick(object sender, EventArgs e)
        {
            SaveSolarToXml();
        }

        private void demoThreeBodyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveThreeBodyToXml();
        }

        private void showTrajetoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _showOrbits = showOrbitsToolStripMenuItem.Checked;
        }

        private void SpacePbxClick(object sender, EventArgs e)
        {
        }

        private void SpacePbxDoubleClick(object sender, EventArgs e)
        {
            // starts simulating
            if (_state == States.Paused)
            {
                SetStateTo(States.Running);
            }
            else if (_state == States.Running)
            {
                SetStateTo(States.Paused);
            }
        }

        private void SpacePbxMouseDown(object sender, MouseEventArgs e)
        {
            if (_lockIndex >= 0)
            {
                return;// TODO or change _lockIndex to -1?
            }
            _mouseDownX = e.X;
            _mouseDownY = e.Y;
            GetPixelRealPos(_mouseDownX, _mouseDownY, out _mouseDownRealX, out _mouseDownRealY);
            _dragging = true;

            DisplayForAWhile();

            InvalidateView();
        }

        private void SpacePbxMouseMove(object sender, MouseEventArgs e)
        {
            if (_dragging)
            {
                _wheelX = e.X;
                _wheelY = e.Y;
                _wheelRealX = _mouseDownRealX;
                _wheelRealY = _mouseDownRealY;

                DisplayForAWhile();

                InvalidateView();
            }
        }

        private void SpacePbxMouseUp(object sender, MouseEventArgs e)
        {
            _dragging = false;
        }

        private void ThreeBodyFormKeyDown(object sender, KeyEventArgs e)
        {
            var val = e.KeyValue;
            if (val == '0')
            {
                SetLockIndex(-1);
                DisplayForAWhile();
            }
            else if (val > '0' && val <= '9')
            {
                var index = val - '0' - 1;
                SetLockIndex(index);
                DisplayForAWhile();
            }
            else if (val >= 'A' && val <= 'F')
            {
                var index = val - 'A' + 9;
                SetLockIndex(index);
                DisplayForAWhile();
            }
            else if (e.Modifiers == Keys.Control)
            {
                _controlDown = true;
            }
            else if (e.Modifiers == Keys.Alt)
            {
                _altDown = true;
            }
            else if (e.KeyCode == Keys.F1)
            {
                ShowHelp();
            }
        }

        private void ThreeBodyFormKeyUp(object sender, KeyEventArgs e)
        {
            if ((e.Modifiers & Keys.Control) == 0)
            {
                _controlDown = false;
            }
            if ((e.Modifiers & Keys.Alt) == 0)
            {
                _altDown = false;
            }
        }
        
        private void EditSystemXmlToolStripMenuItemClick(object sender, EventArgs e)
        {
            var filePath = Path.Combine(Application.UserAppDataPath, SettingXmlFile);
            if (File.Exists(filePath))
            {
                System.Diagnostics.Process.Start(filePath);
            }
        }

        private void OsdToolStripMenuItemClick(object sender, EventArgs e)
        {
            UpdateOsdState();
        }

        private void SpacePbxOnMouseWheel(object sender, MouseEventArgs e)
        {
            if (_controlDown)
            {
                if (e.Delta > 0 && _simTimeRatio < 1000)    // 1 sec
                {
                    _simTimeRatio *= 2;
                    UpdateTemporalParameters(_timeStep);
                }
                else if (e.Delta < 0 && _simTimeRatio > 0.000001) // about 31 years
                {
                    _simTimeRatio /= 2;
                    UpdateTemporalParameters(_timeStep);
                }
                DisplayForAWhile();
                InvalidateView();
            }
            else if (_altDown)
            {
                if (e.Delta > 0 && _timeStep > 0.001)
                {
                    UpdateTemporalParameters(_timeStep * 0.5);
                }
                else if (e.Delta < 0 && _timeStep < 1000)
                {
                    UpdateTemporalParameters(_timeStep * 2);
                }
                DisplayForAWhile();
                InvalidateView();
            }
            else
            {
                GetPixelRealPos(e.X, e.Y, out _wheelRealX, out _wheelRealY);
                _wheelX = e.X;
                _wheelY = e.Y;
                if (e.Delta > 0 && _screenCoeff < 1000000)
                {
                    _screenCoeff *= 2;
                }
                else if (e.Delta < 0 && _screenCoeff > 0.0000001)
                {
                    _screenCoeff *= 0.5;
                }
                DisplayForAWhile();
                UpdatePc();
                InvalidateView();
            }
        }

        private void GotoTimeToolStripMenuItemClick(object sender, EventArgs e)
        {
            var gtf = new GotoTimeForm(_elapsed);
            gtf.ShowDialog();
            GotoTime(gtf.CurrentValue);
        }

        private void reloadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetStateTo(States.Loading);
        }

        #endregion

        private void UpdateTemporalParameters(double timeStep)
        {
            lock (_sys)
            {
                _timeStep = timeStep;
                _timeStepsEachFrame = _simTimeRatio*SimTimeRatioFactor/(_timeStep*Fps);
                _frameWorldTime = _timeStepsEachFrame * _timeStep;

                const double prod = 100000;
                var hi = (int)Math.Round(prod / _timeStep);
                _sys.HistoryInterval = hi > 0 ? hi : 1;
            }
        }

        private void EngineThread()
        {
            var clashMessageShown = false;

            var tsstart = DateTime.Now;

            var outstandingTimeSteps = 0.0;
            var framesDue = 1;
            var framesDone = 0;
            _clashCount = 0;
            //var framesSkipped = 0;

            while (_state != States.Closing)
            {
                switch (_state)
                {
                    case States.Running:
                        lock (_sys)
                        {
                            for (; framesDone < framesDue; framesDone++)
                            {
                                outstandingTimeSteps += _timeStepsEachFrame;
                                var timeStepsTodo = (int) Math.Floor(outstandingTimeSteps);
                                if (timeStepsTodo > 0)
                                {
                                    for (var i = 0; i < timeStepsTodo; i++)
                                    {
                                        if (!_sys.SimulateStep(_timeStep))
                                        {
                                            _clashCount++;
                                            if (!clashMessageShown)
                                            {
                                                //throw new Exception("Bodies have clashed or simulation is unstable");
                                                MessageBox.Show(Resources.BodiesClashed, Resources.AppName);
                                                clashMessageShown = true;
                                            }
                                        }
                                    }
                                    outstandingTimeSteps -= timeStepsTodo;
                                }

                                _elapsed = _elapsed.Add(TimeSpan.FromSeconds(_frameWorldTime));
                                // one frame
                            }
                        }

                        UpdateView();

                        var tsend = DateTime.Now;
                        var timePassed = tsend - tsstart;

                        framesDue = (int)Math.Round(timePassed.TotalSeconds*Fps);

                        if (framesDue > framesDone + 10)
                        {
                            //framesSkipped = framesDue - framesDone - 1;
                            framesDue = framesDone + 1;
                        }
                        else if (framesDue <= framesDone)
                        {
                            var sleep = ((framesDone + 1)/Fps - timePassed.TotalSeconds)*1000 - 10;
                            Thread.Sleep((int)Math.Round(sleep));
                            framesDue = (int)Math.Round(timePassed.TotalSeconds * Fps);
                        }

                        break;
                    case States.Loading:
                        LoadFromXml();
                        clashMessageShown = false;
                        SetStateTo(States.Paused);
                        break;
                    case States.GoingToTime:
                    {
                        if (_elapsed > _gotoTime)
                        {
                            LoadFromXml();
                        }

                        var tsTimeStep = TimeSpan.FromSeconds(_timeStep);
                        var end = _gotoTime - tsTimeStep;
                        var count = 0;
                        var osdWasOn = _osdOn;
                        _osdOn = true;

// ReSharper disable once LoopVariableIsNeverChangedInsideLoop
                        while (_elapsed <= end && _state != States.Closing)
                        {
                            _sys.SimulateStep(_timeStep);
                            _elapsed = _elapsed + tsTimeStep;

                            if (count % GoToTimeUpdateRate == 0 && count != 0)
                            {
                                UpdateView();
                            }
                            count++;
                        }
                        if (_elapsed < end)
                        {
                            var delta = end - _elapsed;
                            _sys.SimulateStep(delta.TotalSeconds);
                            _elapsed = _gotoTime;
                        }

                        _osdOn = osdWasOn;
                        clashMessageShown = false;
                        SetStateTo(States.Paused);
                        break;
                    }
                    case States.Paused:
                        UpdateView();
                        _resumeEvent.WaitOne();
                        _resumeEvent.Reset();

                        tsstart = DateTime.Now;
                        framesDue = 1;
                        framesDone = 0;
                        break;
                }
            }
        }

        private void InvalidateView()
        {
            if (_state == States.Paused)
            {
                SetResumeEvent();
            }
        }

        private void SetResumeEvent()
        {
            if (_resumeEvent != null)
            {
                _resumeEvent.Set();
            }
        }

        private void GotoTime(TimeSpan value)
        {
            _gotoTime = value;
            SetStateTo(States.GoingToTime);
        }

        private void UpdateOsdState()
        {
            _osdOn = oSDToolStripMenuItem.Checked;
            if (!_osdOn)
            {
                _toDisplay = false;
            }
            InvalidateView();
        }

        private void SetLockIndex(int index)
        {
            var same = index == _lockIndex;
            if (!same && index < _sys.Bodies.Count)
            {
                _lockIndex = index;
            }
        }

        private void DisplayForAWhile()
        {
            _displayRequestStart = DateTime.Now;
            _toDisplay = true;
        }

        private void GetCentral(out int centralX, out int centralY, out double centralRealX,
            out double centralRealY)
        {
            if (_lockIndex >= 0)
            {
                centralX = SpacePbx.Width / 2;
                centralY = SpacePbx.Height / 2;
                var centralReal = _sys.Bodies[_lockIndex].Position;
                centralRealX = centralReal.X;
                centralRealY = centralReal.Y;
            }
            else
            {
                centralX = _wheelX;
                centralY = _wheelY;
                centralRealX = _wheelRealX;
                centralRealY = _wheelRealY;
            }
        }

        private void GetPixelRealPos(double px, double py, out double x, out double y)
        {
            int centralX, centralY;
            double centralRealX, centralRealY;

            GetCentral(out centralX, out centralY, out centralRealX, out centralRealY);

            x = (px - centralX) / _pc + centralRealX;
            y = (centralY - py) / _pc + centralRealY;
        }

        private void GetRealPosPixel(double x, double y, out double px, out double py)
        {
            int centralX, centralY;
            double centralRealX, centralRealY;

            GetCentral(out centralX, out centralY, out centralRealX, out centralRealY);

            px = centralX + (x - centralRealX) * _pc;
            py = centralY - (y - centralRealY) * _pc;
        }

        private void UpdatePc()
        {
            var w = SpacePbx.Width;
            var h = SpacePbx.Height;
            var min = Math.Min(w, h);

            // 1.5 * 10^8 km as _screenCoeff * min dimension
            // (0,0,0) in the middle
            _pc = _screenCoeff * min / 1.5E11;
        }

        private void UpdateView()
        {
            _currBufferIndex++;
            if (_currBufferIndex >= _buffers.Length)
            {
                _currBufferIndex = 0;
            }
            DrawBuffer(_currBufferIndex);
            SpacePbx.Image = (Image) _buffers[_currBufferIndex].Clone();
        }

        private void InitScreenSettings()
        {
            _lockIndex = -1;
            _wheelX = SpacePbx.Width/2;
            _wheelY = SpacePbx.Height/2;
            _wheelRealX = _wheelRealY = 0;
            UpdatePc();
        }

        private void InitBuffers()
        {
            for (var i = 0; i < _buffers.Length; i++)
            {
                var buffer = new Bitmap(SpacePbx.Width, SpacePbx.Height);
                _buffers[i] = buffer;
            }
        }

        private double GetRc()
        {
            var w = SpacePbx.Width;
            var h = SpacePbx.Height;
            var min = Math.Min(w, h);
            // 1.5 * 10^8 km as _screenCoeff * min dimension
            // (0,0,0) in the middle
            var pc = _screenCoeff * min / 1.5E11;
            return pc;
        }

        private void DrawBuffer(int index)
        {
            var currBuffer = _buffers[index];

            var rc = GetRc();

            using (var g = Graphics.FromImage(currBuffer))
            {
                g.Clear(Color.Black);
                var i = 0;
                foreach (var b in _sys.Bodies)
                {
                    var pos = b.Position;

                    var x = pos.X;
                    var y = pos.Y;
                    double px, py;
                    GetRealPosPixel(x, y, out px, out py);

                    var r = b.Radius * rc;
                    if (r < 2)
                    {
                        r = 2;
                    }

                    var left = (float)(px - r);
                    var top = (float)(py - r);
                    var width = (float)(r * 2);
                    var height = width;

                    var brush = _brushes[i];
                    g.FillEllipse(brush, left, top, width, height);
                    i++;
                }

                if (_showOrbits)
                {
                    lock (_sys.BodyHistories)
                    {
                        for (i = 0; i < _sys.BodyHistories.Count; i++)
                        {
                            var pen = _pens[i];
                            var lastGot = false;
                            float lastpx = 0, lastpy = 0;
                            foreach (var bh in _sys.BodyHistories[i])
                            {
                                var pos = bh.Position;
                                var x = pos.X;
                                var y = pos.Y;
                                double px, py;
                                GetRealPosPixel(x, y, out px, out py);

                                if (lastGot)
                                {
                                    g.DrawLine(pen, lastpx, lastpy, (float)px, (float)py);
                                }
                                else
                                {
                                    lastGot = true;
                                }
                                lastpx = (float)px;
                                lastpy = (float)py;
                            }
                            if (lastGot)
                            {
                                var b = _sys.Bodies[i];
                                var pos = b.Position;

                                var x = pos.X;
                                var y = pos.Y;
                                double px, py;
                                GetRealPosPixel(x, y, out px, out py);
                                g.DrawLine(pen, lastpx, lastpy, (float)px, (float)py);
                            }
                        }
                    }
                }

                if (_toDisplay || _osdOn)
                {
                    var curr = DateTime.Now;
                    if (_osdOn || (curr - _displayRequestStart).TotalSeconds < OsdDuration)
                    {
                        var timePerSec = _simTimeRatio*SimTimeRatioFactor;
                        var tpsTs = TimeSpan.FromSeconds(timePerSec);
                        var sb = new StringBuilder();

                        if (_state == States.GoingToTime)
                        {
                            sb.AppendFormat("{0} / {1}\n",
                                _elapsed, _gotoTime);
                            _displayRequestStart = curr;
                        }
                        else
                        {
                            sb.AppendFormat("{0} elapsed, targeting {1} s ({2}) per sim sec, time step {3} s\n",
                                _elapsed, timePerSec, tpsTs, _timeStep);
                        }

                        sb.AppendFormat("{0} minor side length roughly equals 1 astronomical unit\n", _screenCoeff);
                        double realX, realY;
                        GetPixelRealPos(SpacePbx.Width / 2.0, SpacePbx.Height / 2.0, out realX, out realY);
                        sb.AppendFormat("Screen center: ({0:0.###E+0}, {1:0.###E+0}) (m)\n", realX, realY);
                        if (_lockIndex >= 0)
                        {
                            sb.AppendFormat("The view is fixed on body {0}", _lockIndex + 1);
                        }
                        
                        g.DrawString(sb.ToString(), new Font(new FontFamily("Arial"), 12), 
                            new SolidBrush(Color.White), 0, 0);

                        if (_clashCount > 0)
                        {
                            var clashMsg = string.Format("Clashed for {0} iterations\n", _clashCount);
                            g.DrawString(clashMsg, new Font(new FontFamily("Arial"), 12),
                                new SolidBrush(Color.Red), 0, SpacePbx.Height-20);
                        }
                    }
                    else
                    {
                        _toDisplay = false;
                    }
                }
            }
        }

        private void ResetParameters()
        {
            _clashCount = 0;
            _elapsed = TimeSpan.Zero;
        }

        private void LoadFromXml()
        {
            var filePath = Path.Combine(Application.UserAppDataPath, SettingXmlFile);
            if (!File.Exists(filePath))
            {
                SaveSolarToXml();
            }

            try
            {
                using (var sr = new StreamReader(filePath))
                {
                    using (var xml = XmlReader.Create(sr))
                    {
                        _sys = new PlanetarySystem();
                        _sys.ReadXml(xml);
                        UpdateTemporalParameters(_sys.RecommendedTimeStep);
                    }
                }
                InitBrushes();
            }
            catch (Exception)
            {
                // ignored
            }

            ResetParameters();
        }

        private void SaveToXml(PlanetarySystem sys)
        {
            var filePath = Path.Combine(Application.UserAppDataPath, SettingXmlFile);
            using (var sw = new StreamWriter(filePath))
            {
                var settings = new XmlWriterSettings { Indent = true };
                using (var xml = XmlWriter.Create(sw, settings))
                {
                    sys.WriteXml(xml);
                }
            }
        }

        private void SaveSolarToXml()
        {
            var sys = InitSolar();
            SaveToXml(sys);
            SetStateTo(States.Loading);
        }

        private void SaveThreeBodyToXml()
        {
            var sys = InitThreeBody();
            SaveToXml(sys);
            SetStateTo(States.Loading);
        }

        private Vector GeneratePositionFromAU(double x, double y, double z)
        {
            return new Vector
            {
                X = x * Au,
                Y = y * Au,
                Z = z * Au
            };
        }

        private Vector GenerateVelocityFromAUPerDay(double x, double y, double z)
        {
            const double day = 3600*24;// s
            const double auPerDay = Au / day; // m/s
            return new Vector
            {
                X = x * auPerDay,
                Y = y * auPerDay,
                Z = z * auPerDay
            };
        }

        /// <summary>
        ///  hardcoded solar system sample
        /// </summary>
        /// <remarks>   
        /// http://ssd.jpl.nasa.gov/horizons.cgi
        /// data: 01/01/2015 00:00
        /// </remarks>
        private PlanetarySystem InitSolar()
        {
            var sun = new SphericalCelestialBody
            {
                Mass = 1.988544E30,
                Radius = 6.96342E8,
                Position = GeneratePositionFromAU(2.841029214124732E-03, -8.551488389783957E-04, -1.372196345812671E-04),
                Velocity = GenerateVelocityFromAUPerDay(3.974748627511212E-06, 5.236981821791105E-06, -9.741909867990459E-08),
                Argb = Color.Orange.ToArgb()
            };

            var mercury = new SphericalCelestialBody
            {
                Mass = 3.302E23,
                Radius = 2.44E6,
                Position = GeneratePositionFromAU(3.401540875319301E-01, -2.044550792740463E-01, -4.772012105321857E-02),
                Velocity = GenerateVelocityFromAUPerDay(9.021681472868817E-03, 2.538170267716257E-02, 1.246034824741292E-03),
                Argb = Color.RosyBrown.ToArgb()
            };

            var venus = new SphericalCelestialBody
            {
                Mass = 4.8685E24,
                Radius = 6.051E6,
                Position = GeneratePositionFromAU(5.524983482189795E-01, -4.769264575796522E-01, -3.838223296316452E-02),
                Velocity =
                    GenerateVelocityFromAUPerDay(1.311677229809423E-02, 1.521762020859779E-02, -5.483300582296667E-04),
                Argb = Color.Gold.ToArgb()
            };

            var earth = new SphericalCelestialBody
            {
                Mass = 5.97219E24,
                Radius = 6.378E6,
                Position = GeneratePositionFromAU(-1.683241372257412E-01, 9.674441923084423E-01, -1.669835242727615E-04),
                //Velocity = GenerateVelocityFromAUPerDay(-1.721229158666165E-02, -3.058878865396910E-03, 5.766702178394309E-07),
                Velocity = GenerateVelocityFromAUPerDay(0,0,0),
                Argb = Color.DodgerBlue.ToArgb()
            };

            var mars = new SphericalCelestialBody
            {
                Mass = 6.4185E23,
                Radius = 3.389E6,
                Position = GeneratePositionFromAU(1.358690797965978E+00, -2.758171771328441E-01, -3.917699408597987E-02),
                Velocity = GenerateVelocityFromAUPerDay(3.322799764204666E-03, 1.491641367405062E-02, 2.308785778904986E-04),
                Argb = Color.SaddleBrown.ToArgb()
            };

            var jupiter = new SphericalCelestialBody
            {
                Mass = 1.89813E27,
                Radius = 7.1492E7, //equatorial
                Position = GeneratePositionFromAU(-3.726726759399367E+00, 3.793259984479633E+00, 6.756037675327993E-02),
                Velocity = GenerateVelocityFromAUPerDay(-5.472628385732627E-03, -4.932423699304493E-03, 1.429822143934273E-04),
                Argb = Color.Salmon.ToArgb()
            };

            var saturn = new SphericalCelestialBody
            {
                Mass = 5.68319E26,
                Radius = 6.0268E7, //equatorial
                Position = GeneratePositionFromAU(-5.405313181366314E+00, -8.350093625720643E+00, 3.603093868713014E-01),
                Velocity = GenerateVelocityFromAUPerDay(4.378243638236281E-03, -3.047908070639465E-03, -1.209045937276834E-04),
                Argb = Color.SandyBrown.ToArgb()
            };

            var uranus = new SphericalCelestialBody
            {
                Mass = 8.6813E23,
                Radius = 2.5559E7, //equatorial
                Position = GeneratePositionFromAU(1.930657051495468E+01, 5.250507992762162E+00, -2.306220390711899E-01),
                Velocity =
                    GenerateVelocityFromAUPerDay(-1.060869194208013E-03, 3.611915584725836E-03, 2.705221245666942E-05),
                Argb = Color.Cyan.ToArgb()
            };

            var neptune = new SphericalCelestialBody
            {
                Mass = 1.02E26,
                Radius = 2.4766E7, //equatorial
                Position = GeneratePositionFromAU(2.752968718187091E+01, -1.184376071522006E+01, -3.905499561180227E-01),
                Velocity =
                    GenerateVelocityFromAUPerDay(1.219336294005641E-03, 2.902323585658736E-03, -8.750297296098705E-05),
                Argb = Color.Navy.ToArgb()
            };

            var moon = new SphericalCelestialBody
            {
                Mass = 7.349E22,
                Radius = 1.73753E6,
                Position = GeneratePositionFromAU(-1.666917632267577E-01, 9.694171153182533E-01, -2.967360187374980E-04),
                Velocity =
                    GenerateVelocityFromAUPerDay(-1.765367898249836E-02, -2.666960413187399E-03, -4.287897546613819E-05),
                Argb = Color.Yellow.ToArgb()
            };

            var halley = new SphericalCelestialBody
            {
                Mass = 2.2E14, // roughly
                Radius = 5E3, //roughly
                Position = GeneratePositionFromAU(-2.046540422163850E+01, 2.522274385658262E+01, -9.786248727284677E+00),
                Velocity = GenerateVelocityFromAUPerDay(-5.245221246226070E-05, 9.234117023245660E-04, -1.674052651033283E-04),
                Argb = Color.NavajoWhite.ToArgb()
            };

            var sys = new PlanetarySystem { RecommendedTimeStep = 10 };
            sys.Bodies.Add(sun);
            sys.Bodies.Add(mercury);
            sys.Bodies.Add(venus);
            sys.Bodies.Add(moon);
            sys.Bodies.Add(earth);
            sys.Bodies.Add(mars);
            sys.Bodies.Add(jupiter);
            sys.Bodies.Add(saturn);
            sys.Bodies.Add(uranus);
            sys.Bodies.Add(neptune);
            sys.Bodies.Add(halley);
            sys.InitializeHistory();

            return sys;
        }

        /// <summary>
        ///  Hardcoded 3body sample
        /// </summary>
        private PlanetarySystem InitThreeBody()
        {
#if false // escaped like a comet
            var sun1 = new SphericalCelestialBody
            {
                Mass = 2.7,
                Radius = 0.8,
                Position = {X = 0.1*Math.Cos(0), Y = 0.1*Math.Sin(0)},
                Velocity = { X = 26.5 * Math.Cos(Math.PI / 2), Y = 28 * Math.Sin(Math.PI / 2) }
            };

            var sun2 = new SphericalCelestialBody
            {
                Mass = 2.5,
                Radius = 0.7,
                Position = { X = 0.1 * Math.Cos(Math.PI * 2 / 3), Y = 0.1 * Math.Sin(Math.PI * 2 / 3) },
                Velocity = { X = 29 * Math.Cos(Math.PI * 7 / 6), Y = 31 * Math.Sin(Math.PI * 7 / 6) }
            };

            var sun3 = new SphericalCelestialBody
            {
                Mass = 3.1,
                Radius = 1,
                Position = { X = 0.1 * Math.Cos(Math.PI * 4 / 3), Y = 0.1 * Math.Sin(Math.PI * 4 / 3) },
                Velocity = { X = 27 * Math.Cos(Math.PI * 11 / 6), Y = 28 * Math.Sin(Math.PI * 11 / 6) }
            };

            var earth = new SphericalCelestialBody
            {
                Mass = 5.97219E-6,
                Radius = 6.378E-3,
                //Position = {X = 0.25},
                Velocity = {Y = 21}
            };
#endif

            var sun1 = new SphericalCelestialBody
            {
                Mass = 2E30,
                Radius = 7E8,
                Position = GeneratePositionFromAU(-0.4, 0, 0),
                Velocity = GenerateVelocityFromAUPerDay(0, -1E-2, 0),
                Argb = Color.OrangeRed.ToArgb()
            };

            var sun2 = new SphericalCelestialBody
            {
                Mass = 2E30,
                Radius = 7E8,
                Position = GeneratePositionFromAU(0, 0.5, 0),
                Velocity = GenerateVelocityFromAUPerDay(-2E-3, 0, 0),
                Argb = Color.OrangeRed.ToArgb()
            };

            var sun3 = new SphericalCelestialBody
            {
                Mass = 2E30,
                Radius = 7E8,
                Position = GeneratePositionFromAU(0.5, -0.5, 0),
                Velocity = GenerateVelocityFromAUPerDay(1E-2, -1E-2, 0),
                Argb = Color.OrangeRed.ToArgb()
            };

            var earth = new SphericalCelestialBody
            {
                Mass = 6E24,
                Radius = 7E6,
                Position = GeneratePositionFromAU(0, 3, 0),
                Velocity = GenerateVelocityFromAUPerDay(-2E-02, -3E-03, 6E-07),
                Argb = Color.Cyan.ToArgb()
            };

            var sys = new PlanetarySystem { RecommendedTimeStep = 10 };
            sys.Bodies.Add(sun1);
            sys.Bodies.Add(sun2);
            sys.Bodies.Add(sun3);
            sys.Bodies.Add(earth);
            sys.InitializeHistory();

            return sys; 
        }

        private void InitBrushes()
        {
            _brushes.Clear();
// ReSharper disable once InconsistentlySynchronizedField
            _pens.Clear();
            foreach (var body in _sys.Bodies)
            {
                var color = Color.FromArgb(body.Argb);
                _brushes.Add(new SolidBrush(color));
// ReSharper disable once InconsistentlySynchronizedField
                _pens.Add(new Pen(color));
            }
        }
        
        private void ShowHelp()
        {
            var helpMessage = new StringBuilder();
            helpMessage.AppendLine("This app is based on a stellar system defining XML file in the app's user directory");
            helpMessage.AppendLine("An exception may occur when the simnulation runs into an unstable/clashing situation");
            helpMessage.AppendLine("Use the following to control the display");
            helpMessage.AppendLine(" - F1 for this help");
            helpMessage.AppendLine(" - Double click the screen to start/pause the simulation");
            helpMessage.AppendLine(" - Number Keys (1 to 9) to centralize corresponding bodies, 0 to disable centralizing");
            helpMessage.AppendLine(" - Scroll mouse wheel to zoom in or out; Ctrl scroll to speed up or slow down");
            helpMessage.AppendLine(" - Drag and drop to pan");
            helpMessage.AppendLine(" - Right click for more options");
            MessageBox.Show(helpMessage.ToString(), Text);
        }

        #endregion
    }
}
