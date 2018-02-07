﻿#define TEST_OCR

using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Threading;
using System.Windows.Threading;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Media.Imaging;
using System.IO;

namespace Iterator
{
    public class PExHelper
    {
        Window _parent;
        IntPtr _gameMainHwnd = IntPtr.Zero;
        IntPtr _parentHwnd = IntPtr.Zero;
        IntPtr _runButton = IntPtr.Zero;
        IntPtr _restartButtonHwnd = IntPtr.Zero;
        IntPtr _summaryReportHwnd = IntPtr.Zero;
        NumbersOCR _ocr;

        public PExHelper(Window parentWindow)
        {
            _parent = parentWindow;
            _parentHwnd = new WindowInteropHelper(parentWindow).Handle;

            // Create screen OCR
            _ocr = new NumbersOCR();

            // This is how we test some bad recognized digits
#if DEBUG
            var s = _ocr.Recognize(new BitmapImage(new Uri("pack://application:,,,/test38.5.png")));
#endif
        }

        #region Service functions

        private bool FindWindowHandles()
        {
            bool result = false;

            _gameMainHwnd = Win32.FindWindow("ThunderRT6MDIForm", "People Express");

            if (_gameMainHwnd != IntPtr.Zero)
            {
                var childHandles = GetAllChildHandles(_gameMainHwnd);

                var inputWnd = IntPtr.Zero;
                foreach (var hWnd in childHandles)
                {
                    int nRet;
                    StringBuilder ClassName = new StringBuilder(256);
                    nRet = Win32.GetClassName(hWnd, ClassName, ClassName.Capacity);
                    if (nRet != 0 && ClassName.ToString() == "ThunderRT6Frame")
                    {
                        inputWnd = hWnd;
                    }
                    else if (nRet != 0 && ClassName.ToString() == "ThunderRT6CommandButton")
                    {
                        var outText = new StringBuilder(255);
                        Win32.SendMessage(hWnd, Win32.WM_GETTEXT, outText.Capacity, outText);
                        if (outText.ToString().Equals("Restart")) _restartButtonHwnd = hWnd;
                        else if (outText.ToString().Equals("Run")) _runButton = hWnd;
                    }
                    else if (nRet != 0 && ClassName.ToString() == "ThunderRT6FormDC")
                    {
                        var outText = new StringBuilder(255);
                        Win32.SendMessage(hWnd, Win32.WM_GETTEXT, outText.Capacity, outText);
                        if (outText.ToString().StartsWith("Summary")) _summaryReportHwnd = hWnd;
                    }
                }

                if (inputWnd != IntPtr.Zero)
                {
                    var inputBoxes = GetAllChildHandles(inputWnd);
                    if (inputBoxes.Count == 5)
                    {
                        _aircraftPurchasesPerQtrHwnd = inputBoxes[4];
                        _peoplesFareHwnd = inputBoxes[3];
                        _marketingAsFracOfRevenueHwnd = inputBoxes[2];
                        _hiringHwnd = inputBoxes[1];
                        _targetServiceScopeHwnd = inputBoxes[0];

                        result = true;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Returns all child window handles for parent window handle
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        private List<IntPtr> GetAllChildHandles(IntPtr handle)
        {
            List<IntPtr> childHandles = new List<IntPtr>();

            GCHandle gcChildhandlesList = GCHandle.Alloc(childHandles);
            IntPtr pointerChildHandlesList = GCHandle.ToIntPtr(gcChildhandlesList);

            try
            {
                Win32.EnumWindowProc childProc = new Win32.EnumWindowProc(EnumWindow);
                Win32.EnumChildWindows(handle, childProc, pointerChildHandlesList);
            }
            finally
            {
                gcChildhandlesList.Free();
            }

            return childHandles;
        }

        /// <summary>
        /// Enumerates child windows
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        private bool EnumWindow(IntPtr hWnd, IntPtr lParam)
        {
            GCHandle gcChildhandlesList = GCHandle.FromIntPtr(lParam);

            if (gcChildhandlesList == null || gcChildhandlesList.Target == null)
            {
                return false;
            }

            List<IntPtr> childHandles = gcChildhandlesList.Target as List<IntPtr>;
            childHandles.Add(hWnd);

            return true;
        }

        private string GetStringValue(IntPtr hWnd)
        {
            var outText = new StringBuilder(255);
            Win32.SendMessage(hWnd, Win32.WM_GETTEXT, outText.Capacity, outText);
            return outText.ToString();
        }

        private double GetValue(IntPtr hWnd)
        {
            double result = 0;
            var outText = new StringBuilder(255);
            Win32.SendMessage(hWnd, Win32.WM_GETTEXT, outText.Capacity, outText);
            double.TryParse(outText.ToString(), out result);
            return result;
        }

        private void SetValue(IntPtr hWnd, object value, double max = double.MinValue)
        {
            if (value is double) { if (max != double.MinValue && (double)value > max) value = max; }
            else if (max != double.MinValue && (int)value > max) value = max;
            Win32.SendMessage(hWnd, Win32.WM_SETTEXT, IntPtr.Zero, value.ToString());
        }

        private Bitmap GetWindowScreenshot(IntPtr hWnd)
        {
            Bitmap bmp = null;
            Graphics g = null;
            IntPtr hdcBitmap = IntPtr.Zero;
            Win32.RECT rc;
            Win32.GetWindowRect(hWnd, out rc);

            if (rc.Right - rc.Left == 0 || rc.Bottom - rc.Top == 0) return null;
            try
            {
                bmp = new Bitmap(rc.Right - rc.Left, rc.Bottom - rc.Top, PixelFormat.Format32bppArgb);
                g = Graphics.FromImage(bmp);
                hdcBitmap = g.GetHdc();
                Win32.PrintWindow(hWnd, hdcBitmap, 0);
            }
            catch { }
            finally
            {
                if (g != null)
                {
                    if (hdcBitmap != IntPtr.Zero) g.ReleaseHdc(hdcBitmap);
                    g.Dispose();
                }
            }
            return bmp;
        }

        private Bitmap CopyBitmapRect(Bitmap source, RectangleF srcRect)
        {
            Bitmap result = new Bitmap((int)srcRect.Width, (int)srcRect.Height);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.DrawImage(source, new RectangleF(0, 0, result.Width, result.Height), srcRect, GraphicsUnit.Pixel);
            }
            return result;
        }

        private void RedrawWindow(IntPtr hWnd)
        {
            Win32.RedrawWindow(hWnd, IntPtr.Zero, IntPtr.Zero, Win32.RedrawWindowFlags.Frame | Win32.RedrawWindowFlags.UpdateNow | Win32.RedrawWindowFlags.Invalidate);
        }

#if TEST_OCR
        public List<Bitmap> TestBitmaps = new List<Bitmap>(7);
#endif

        #endregion

        #region Public functions

        public void ReadSummary()
        {
            if (_summaryReportHwnd == IntPtr.Zero) FindWindowHandles();

            // Push window to redraw
            RedrawWindow(_summaryReportHwnd);

            // Get "Summary" window screenshot
            var summaryBitmap = GetWindowScreenshot(_summaryReportHwnd);

            // If call is not successful, re-try once (probably "Summary report" window was re-opened)
            if (summaryBitmap == null)
            {
                _summaryReportHwnd = IntPtr.Zero;
                FindWindowHandles();
                if (_summaryReportHwnd != IntPtr.Zero)
                {
                    summaryBitmap = GetWindowScreenshot(_summaryReportHwnd);
                    if (summaryBitmap == null) return;
                }
                else return;
            }

            var data = new List<string>();
            TestBitmaps.Clear();
            // Read first column (7 rows)
            int i = 0;
            float y = 52;
            int delta = 38;
            while (++i < 9)
            {
                TestBitmaps.Add(CopyBitmapRect(summaryBitmap, new RectangleF(150, y, 40, 20)));
                data.Add(_ocr.Recognize(TestBitmaps[i - 1]));
                y += delta;
                if (i == 4) y -= 10;
                if (i == 7) y -= 4;
            }

            // Read second column (7 rows)
            TestBitmaps.Clear();
            i = 0;
            y = 48;
            delta = 24;
            while (++i < 10)
            {
                TestBitmaps.Add(CopyBitmapRect(summaryBitmap, new RectangleF(336, y, 40, 20)));
                data.Add(_ocr.Recognize(TestBitmaps[i - 1]));
                y += delta;
                if (i == 4) { y += 10; delta = 38; }
                if (i == 8) y -= 4;
            }

            _summary = new SummaryReport(data);
        }


        public bool Init()
        {
            return FindWindowHandles();
        }

        /// <summary>
        /// Virtually press "Run" button
        /// </summary>
        public void Run()
        {
            int prevQuarter = Quarter;
            Win32.SendMessage(_runButton, Win32.BM_CLICK, new IntPtr(0), new IntPtr(0));
            RedrawWindow(_summaryReportHwnd);
            // Check end of game conditions
            if (Quarter == 40) OnEndGame?.Invoke(this, new EventArgs());
            else if (Quarter > 0 && prevQuarter == Quarter && (_previousStep != GameStep.Restart || _previousStep != GameStep.Back))
                OnBanckrupcy?.Invoke(this, new EventArgs());
            _previousStep = GameStep.Run;
        }

        public void Back()
        {
            _parent.Topmost = true;
            Win32.SetForegroundWindow(_gameMainHwnd);
            SendKeys.SendWait("^g");
            SendKeys.SendWait("y");
            _parent.Topmost = false;
            _parent.Activate();
            RedrawWindow(_summaryReportHwnd);
            _previousStep = GameStep.Back;
        }

        public void Restart()
        {
            _parent.Topmost = true;
            Win32.SetForegroundWindow(_gameMainHwnd);
            Win32.PostMessage(_restartButtonHwnd, Win32.BM_CLICK, new IntPtr(0), new IntPtr(0));
            _previousStep = GameStep.Restart;
            _parent.Dispatcher.Invoke(() => 
            {
                SendKeys.SendWait("y");
                SendKeys.SendWait("y");
                Win32.SetForegroundWindow(_parentHwnd);
                Thread.Sleep(200);
                _parent.Topmost = false;
                _parent.Activate();
            }, DispatcherPriority.ApplicationIdle);
        }

        #endregion

        #region Public properties

        private IntPtr _aircraftPurchasesPerQtrHwnd;
        public uint AircraftPurchasesPerQtr
        {
            get => (uint) GetValue(_aircraftPurchasesPerQtrHwnd);
            set => SetValue(_aircraftPurchasesPerQtrHwnd, value);
        }

        private IntPtr _peoplesFareHwnd;
        public double PeoplesFare
        {
            get => GetValue(_peoplesFareHwnd);
            set => SetValue(_peoplesFareHwnd, value);
        }

        private IntPtr _marketingAsFracOfRevenueHwnd;
        public double MarketingAsFracOfRevenue
        {
            get => GetValue(_marketingAsFracOfRevenueHwnd);
            set => SetValue(_marketingAsFracOfRevenueHwnd, value, 1);
        }

        private IntPtr _hiringHwnd;
        public uint Hiring
        {
            get => (uint) GetValue(_hiringHwnd);
            set => SetValue(_hiringHwnd, value);
        }

        private IntPtr _targetServiceScopeHwnd;
        public double TargetServiceScope
        {
            get => GetValue(_targetServiceScopeHwnd);
            set => SetValue(_targetServiceScopeHwnd, value, 1);
        }

        SummaryReport _summary = new SummaryReport();
        public SummaryReport Summary => _summary;

        public int Quarter
        {
            get
            {
                if (_summaryReportHwnd == IntPtr.Zero) FindWindowHandles();
                if (_summaryReportHwnd != IntPtr.Zero)
                {
                    string[] caption = GetStringValue(_summaryReportHwnd).Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    return int.Parse(caption[4]) * 4 + int.Parse(caption[6]) - 4;
                }
                return -1;
            }
        }

        public int Iteration => Quarter;

        public enum GameStep
        {
            Run,
            Back,
            Restart
        }

        private GameStep _previousStep = GameStep.Restart;
        public GameStep PreviousStep => _previousStep;

        #endregion

        #region Public events

        public event EventHandler OnBanckrupcy;

        public event EventHandler OnEndGame;

        #endregion
    }
}
