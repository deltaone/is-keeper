using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using Core;


namespace ControlsEx
{
    public class RichTextLog : RichTextBox
    {
        private int _LogMaxLines;
        private int _LogKeepLines;
        public int LogMaxLines { get { return (_LogMaxLines); } set { _LogMaxLines = value; _LogKeepLines = (value * 75 / 100); } }

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr window, int message, int wparam, int lparam);

        public RichTextLog()
        {
            LogMaxLines = 150;

            base.ReadOnly = true;
            base.BorderStyle = BorderStyle.None;
            base.TabStop = false;
            base.SetStyle(ControlStyles.Selectable, false);

            base.Font = new Font("Consolas", 9, FontStyle.Regular);
            base.ForeColor = System.Drawing.Color.Blue;
            base.BackColor = SystemColors.ControlLight;

            base.MouseEnter += delegate(object sender, EventArgs e)
            {
                this.Cursor = Cursors.Default;
            };

            ContextMenu contextMenu = new System.Windows.Forms.ContextMenu();

            MenuItem menuItem;
            menuItem = new MenuItem("Copy");
            menuItem.Click += new EventHandler(MenuActionCopy);
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("Copy all");
            menuItem.Click += new EventHandler(MenuActionCopyAll);
            contextMenu.MenuItems.Add(menuItem);

            menuItem = new MenuItem("Clear");
            menuItem.Click += new EventHandler(MenuActionClear);
            contextMenu.MenuItems.Add(menuItem);
            
            base.ContextMenu = contextMenu;
        }

        void MenuActionCopyAll(object sender, EventArgs e)
        {
            Clipboard.Clear();
            if (!((String.IsNullOrEmpty(base.Text) || base.Text.Trim().Length == 0))) Clipboard.SetText(base.Text);
        }

        void MenuActionCopy(object sender, EventArgs e)
        {
            Clipboard.Clear();
            if (!((String.IsNullOrEmpty(SelectedText) || SelectedText.Trim().Length == 0))) Clipboard.SetText(base.SelectedText);
        }

        void MenuActionClear(object sender, EventArgs e)
        {
            base.ResetText();
        }

        public void AppendTextLog(string text)
        {
            Color color = Color.Gray; // Color.Blue; Color.DarkBlue; Color.Red; Color.Maroon;

            if (text[0] == '{')
            {
                Match match = Regex.Match(text, @"\{(\w+)\}(.*)$", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    color = (Color)typeof(Color).GetProperty(match.Groups[1].Value).GetValue(null, null);
                    text = match.Groups[2].Value;
                }
            }
            if (text[0] == '[')
            {
                if (text.StartsWith("[debug]", StringComparison.InvariantCultureIgnoreCase)) color = Color.Maroon;
                else if (text.StartsWith("[error]", StringComparison.InvariantCultureIgnoreCase)) color = Color.Maroon; // Red DarkBlue Maroon
                else if (text.StartsWith("[warning]", StringComparison.InvariantCultureIgnoreCase)) color = Color.Maroon;
            }

            Action code = delegate { this.AppendTextLog(text, color); };
            if (this.InvokeRequired) this.BeginInvoke(code);
            else code.Invoke();
        }

        public void AppendTextLog(string text, Color color)
        {
            Action code = delegate
            {
                if (base.Lines.Length > _LogMaxLines)
                {
                    base.ReadOnly = false;
                    base.Select(0, base.GetFirstCharIndexFromLine(base.Lines.Length - _LogKeepLines));
                    base.SelectedText = "";
                    base.ReadOnly = true;
                }
                if (Lines.Length > 0) base.AppendText("\n");
                base.SelectionStart = base.TextLength;
                base.SelectionLength = 0;
                base.SelectionColor = Color.Gray;
                base.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] ");
                base.SelectionStart = base.TextLength;
                base.SelectionLength = 0;
                base.SelectionColor = color;
                base.AppendText(text);
                base.SelectionColor = base.ForeColor;
                SendMessage(base.Handle, 0x115, 0x7, 0x0); // SBBottom = 0x7; WMVscroll = 0x115;
            };
            if (this.InvokeRequired) this.BeginInvoke(code);
            else code.Invoke();
        }
    }
}