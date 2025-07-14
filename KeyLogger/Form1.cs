
/* Copyright ┬® 2017 Robert Preddy
*
* Licensed under the Apache License, Version 2.0 (the "License"); you may not use this
* file except in compliance with the License. You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software distributed under
* the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
* ANY KIND, either express or implied. See the License for the specific language
* governing permissions and limitations under the License.
*
*/

using BaseUtils.Win32Constants;
using EliteDangerousCore;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Forms;

namespace KeyLogger
{
    public partial class KeyLogger : Form
    {
        Stopwatch ws;
        int curinputlangindex = 0;

        public KeyLogger()
        {
            InitializeComponent();
            actionfilesmessagefilter = new ActionMessageFilter(this);
            Application.AddMessageFilter(actionfilesmessagefilter);
            ws = new Stopwatch();
            ws.Start();
            richTextBox1.Text = Environment.CommandLine + Environment.NewLine;
            richTextBox1.Text += "UI Culture " + CultureInfo.CurrentUICulture.Name + Environment.NewLine;

            var array = InputLanguage.InstalledInputLanguages;

            curinputlangindex = 0;
            foreach (InputLanguage lang in InputLanguage.InstalledInputLanguages)
            {
                if (InputLanguage.CurrentInputLanguage.LayoutName == lang.LayoutName)
                {
                    break;
                }

                curinputlangindex++;
            }

            foreach (InputLanguage lang in InputLanguage.InstalledInputLanguages)
            {
                System.Diagnostics.Debug.Write($"{lang.Culture}");

            }

            labelIL.Text = InputLanguage.CurrentInputLanguage.LayoutName + " " + InputLanguage.CurrentInputLanguage.Culture.Name;

            FrontierKeyConversion.Check();

            //for( int vk = 0; vk < 256; vk++ )
            //{
            //    short v = UnsafeNativeMethods.VkKeyScanEx((char)i, kblayout);
            //    bool shift = (v & 256) != 0;

            //    bool ctrl = (v & 512) != 0;

            //    bool alt = (v & 1024) != 0;

            //    v = (short)(v & 0xff);
            //    short 
            //}




#if false
            // some test code exercising the key win32 funcs

            for (int i = 0; i <= 255; i++)
            {
                Keys ret = (Keys)BaseUtils.Win32.UnsafeNativeMethods.VkKeyScan((char)i);
                System.Diagnostics.Debug.WriteLine(i + " " + (int)ret + " "+ ret.ToString());
            }

            for (int i = 0; i <= 255; i++)
            { 
                uint v = BaseUtils.Win32.UnsafeNativeMethods.MapVirtualKey((uint)i, 2);
                if (v > 0)
                {
                    string vk = (v > 0) ? ((Keys)i).ToString() : "";
                    char c = (v > 0) ? ((char)v) : '?';
                    System.Diagnostics.Debug.WriteLine(i + " " + vk + " = " + v + " = " + c);
                }
            }

            for (int i = 1; i < 0x5f; i++)
            {
                StringBuilder s = new StringBuilder();
                BaseUtils.Win32.UnsafeNativeMethods.GetKeyNameText(i << 16, s, 100);
                StringBuilder es = new StringBuilder();
                BaseUtils.Win32.UnsafeNativeMethods.GetKeyNameText(i << 16 | 1 << 24, es, 100);
                System.Diagnostics.Debug.WriteLine("SC {0:x} {1,20} {2,20}", i, s.ToNullSafeString(), es.ToNullSafeString());
            }

            {
                char c = '²';
                short vk = BaseUtils.Win32.UnsafeNativeMethods.VkKeyScan(c);
                if (vk != -1)
                {
                    System.Diagnostics.Debug.WriteLine("{0} = {1:x} {2}", c, vk, ((Keys)vk).VKeyToString());
                }
            }
#endif

        }

        ActionMessageFilter actionfilesmessagefilter;

        public void PressedKey(string t, Keys k, int extsc, Keys modifiers)
        {
            bool extendedkey = (extsc & (1 << 24)) != 0;
            bool alt = (extsc & (1 << 29)) != 0;
            bool downalready = (extsc & (1 << 30)) != 0;
            bool up = (extsc & (1 << 31)) != 0;
            int sc = (int)((extsc >> 16) & 0xff);
            int extbits = (int)((extsc >> 24) & 0xff);

            string name = k.ToString();

            Keys kadj = KeyObjectExtensions.VKeyAdjust(k, extendedkey, sc);
            string vkeyname = kadj.VKeyToString();

            string frontier = EliteDangerousCore.FrontierKeyConversion.FrontierToKeys("Key_" + vkeyname);

            string res = (ws.ElapsedMilliseconds%10000).ToString("00000") + " " + t + " " + name.PadRight(15) + " VKeyName: " + vkeyname.PadRight(15) + " Keycode: " + ((uint)k).ToString() + 
                                " sc:" + sc.ToString("X2") + " t:" + extbits.ToString("X2") + " " +
                                (up ? "U" : "-") +
                                (downalready ? "D" : "-") +
                                (alt ? "A" : "-") +
                                (extendedkey ? "E" : "-") +
                                " " + modifiers.ToString() + 
                                " Frontier: " + frontier;
            richTextBox1.Text += res + Environment.NewLine;
            richTextBox1.Select(richTextBox1.Text.Length, richTextBox1.Text.Length);
            richTextBox1.ScrollToCaret();
        }

        private void richTextBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            string res = EliteDangerousCore.FrontierKeyConversion.FrontierToKeys("Key_" + e.KeyChar);
            richTextBox1.Text += $"Key press {e.KeyChar} = Frontier: {res}" + Environment.NewLine;
            richTextBox1.Select(richTextBox1.Text.Length, richTextBox1.Text.Length);
            richTextBox1.ScrollToCaret();
            e.Handled = true;
        }

        // maybe a full hook? https://github.com/shanselman/babysmash/blob/master/App.xaml.cs

        protected class ActionMessageFilter : IMessageFilter
        {
            KeyLogger keyform;
            public ActionMessageFilter(KeyLogger k)
            {
                keyform = k;
            }

            public bool PreFilterMessage(ref Message m)
            {
                if (m.Msg == WM.KEYDOWN || m.Msg == WM.SYSKEYDOWN)
                {
                    Keys k = (Keys)m.WParam;
                    int sc = (int)m.LParam;
                    keyform.PressedKey((m.Msg == WM.SYSKEYDOWN) ? "SD" : "KD", k, sc, Control.ModifierKeys);
                   // return true;
                }

                if (m.Msg == WM.KEYUP || m.Msg == WM.SYSKEYUP)
                {
                    Keys k = (Keys)m.WParam;
                    int sc = (int)m.LParam;
                    keyform.PressedKey((m.Msg == WM.SYSKEYDOWN) ? "SU" : "KU", k, sc, Control.ModifierKeys);
                   // return true;
                }

                return false;
            }
        }

        
        
        private void richTextBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            richTextBox1.Text = "";
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string text = richTextBox1.SelectedText;
            if (text.Length == 0)
                text = richTextBox1.Text;
            Clipboard.SetText(text);
        }
        
        private void button1_Click(object sender, EventArgs e)
        {
            curinputlangindex--;
            if (curinputlangindex < 0)
                curinputlangindex = InputLanguage.InstalledInputLanguages.Count - 1;
            InputLanguage.CurrentInputLanguage = InputLanguage.InstalledInputLanguages[curinputlangindex];
            labelIL.Text = InputLanguage.CurrentInputLanguage.LayoutName + " " + InputLanguage.CurrentInputLanguage.Culture.Name;
            richTextBox1.Focus();
        }
        
        private void button2_Click(object sender, EventArgs e)
        {
            curinputlangindex++;
            if (curinputlangindex >= InputLanguage.InstalledInputLanguages.Count)
                curinputlangindex = 0;

            InputLanguage.CurrentInputLanguage = InputLanguage.InstalledInputLanguages[curinputlangindex];
            labelIL.Text = InputLanguage.CurrentInputLanguage.LayoutName + " " + InputLanguage.CurrentInputLanguage.Culture.Name;
            richTextBox1.Focus();
        }
 
    }
}

