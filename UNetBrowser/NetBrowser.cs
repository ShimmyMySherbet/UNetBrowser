using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace UNetBrowser
{
    public partial class NetBrowser : Form
    {
        private Button m_Submit;
        private IReadOnlyCollection<NetMethod> m_Methods;

        public NetBrowser()
        {
            InitializeComponent();
        }

        public NetBrowser(List<NetMethod> methods)
        {
            InitializeComponent();
            m_Methods = methods;
            Init();
        }

        private void Init()
        {
            m_Submit = new Button();
            m_Submit.Click += OnSubmit;
            AcceptButton = m_Submit;

            flowItems.SizeChanged += OnWindowSizeChanged;
            cbClient.CheckedChanged += OnSenderFilterChanged;
            cbServer.CheckedChanged += OnSenderFilterChanged;
            cbCaseSensitive.CheckedChanged += OnSenderFilterChanged;
            cbSearchParam.CheckedChanged += OnSenderFilterChanged;
        }

        private void OnSenderFilterChanged(object sender, EventArgs e)
        {
            OnSubmit(sender, e);
        }

        private void OnWindowSizeChanged(object sender, EventArgs e)
        {
            foreach (var control in flowItems.Controls.OfType<NetMethodControl>())
            {
                AdjustItem(control);
            }
        }

        private void OnSubmit(object sender, EventArgs e)
        {
            var matchingEntires = SelectMethods().Take(50);

            Debug.WriteLine($"Found {matchingEntires}/{m_Methods.Count} matching methods");

            var sw = new Stopwatch();
            sw.Start();

            flowItems.SuspendLayout();
            try
            {
                if (flowItems.Controls.Count > 0)
                {
                    foreach (Control control in flowItems.Controls)
                    {
                        control.Dispose();
                    }
                    flowItems.Controls.Clear();
                }

                foreach (var method in matchingEntires)
                {
                    var control = new NetMethodControl(method);
                    AdjustItem(control);
                    flowItems.Controls.Add(control);
                }
            }
            finally
            {
                flowItems.ResumeLayout();
            }

            sw.Stop();
            Debug.WriteLine($"Updated listing in {sw.ElapsedMilliseconds}ms ({sw.ElapsedTicks} ticks)");
        }

        private List<NetMethod> SelectMethods()
        {
            if (m_Methods == null)
                throw new InvalidOperationException("No Loaded net methods");

            var matchingEntires = m_Methods.Where(MethodDiscriminator);

            return matchingEntires.ToList();
        }

        private void AdjustItem(NetMethodControl methodControl)
        {
            methodControl.Width = flowItems.Width - 25;
        }

        private bool MethodDiscriminator(NetMethod netMethod)
        {
            var comparason = cbCaseSensitive.Checked ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase;

            if (netMethod.SentBy.Equals("Server", StringComparison.InvariantCultureIgnoreCase) && !cbServer.Checked)
            {
                return false;
            }

            if (netMethod.SentBy.Equals("Client", StringComparison.InvariantCultureIgnoreCase) && !cbClient.Checked)
            {
                return false;
            }

            if (cbSearchParam.Checked && !string.IsNullOrEmpty(netMethod.Parameters))
            {
                if (netMethod.Parameters.Contains(txtSearch.Text, comparason))
                {
                    return true;
                }
            }


            if (netMethod.DeclaringType.Contains(txtSearch.Text, comparason))
            {
                return true;
            }

            if (netMethod.Handler.Contains(txtSearch.Text, comparason))
            {
                return true;
            }

            if (netMethod.SendHandle.Contains(txtSearch.Text, comparason))
            {
                return true;
            }

            return false;
        }

        private void OnWindowLoad(object sender, EventArgs e)
        {
            OnSubmit(sender, e);
        }

        private void OnExportRequested(object sender, EventArgs e)
        {
            using (var sfd = new SaveFileDialog() { Filter = "Json Files|*.json", Title = "Export Net Methods" })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    var methods = SelectMethods();
                    File.WriteAllText(sfd.FileName, JsonConvert.SerializeObject(methods, Formatting.Indented));
                    MessageBox.Show($"Exported {methods.Count} methods");
                }
            }
        }
    }
}