using PdfiumViewer;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace PdfReader
{
    public class MainForm : Form
    {
        private SplitContainer _split;
        private TreeView _outlineTree;
        private PdfScrollPanel _pdfPanel;
        private PdfDocument _doc;
        private ToolStrip _toolbar;
        private ToolStripButton _btnOpen, _btnOutline;
        private ToolStripSeparator _sep1;
        private ToolStripLabel _lblPage;
        private ToolStripTextBox _txtPage;
        private ToolStripLabel _lblTotal;

        public MainForm(string filePath)
        {
            Text = "PDF Reader";
            Size = new Size(1280, 800);
            BackColor = Color.FromArgb(245, 245, 245);
            KeyPreview = true;
            SuspendLayout();
            BuildToolbar();
            BuildSplit();
            ResumeLayout();
            _split.SplitterDistance = Width / 5;
            KeyDown += OnKeyDown;
            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                OpenPdf(filePath);
        }

        private void BuildToolbar()
        {
            _toolbar = new ToolStrip { Dock = DockStyle.Top, BackColor = Color.FromArgb(240, 240, 240), ForeColor = Color.FromArgb(60, 60, 60), GripStyle = ToolStripGripStyle.Hidden, Renderer = new DarkRenderer() };
            _btnOpen = new ToolStripButton("打开") { ForeColor = Color.FromArgb(60, 60, 60) };
            _btnOpen.Click += (s, e) => { using (var d = new OpenFileDialog { Filter = "PDF|*.pdf" }) { if (d.ShowDialog() == DialogResult.OK) OpenPdf(d.FileName); } };
            _btnOutline = new ToolStripButton("目录") { ForeColor = Color.FromArgb(60, 60, 60), CheckOnClick = true, Checked = true };
            _btnOutline.Click += (s, e) => _split.Panel1Collapsed = !_btnOutline.Checked;
            _sep1 = new ToolStripSeparator();
            var btnPrint = new ToolStripButton("打印") { ForeColor = Color.FromArgb(60, 60, 60) };
            btnPrint.Click += (s, e) => OnPrint();
            var sep1b = new ToolStripSeparator();
            var btnZoomOut = new ToolStripButton("−") { ForeColor = Color.FromArgb(60, 60, 60) };
            btnZoomOut.Click += (s, e) => _pdfPanel.SetZoom(Math.Max(0.1f, _pdfPanel.Zoom - 0.1f));
            var lblZoom = new ToolStripLabel("100%") { ForeColor = Color.FromArgb(60, 60, 60) };
            var btnZoomIn = new ToolStripButton("+") { ForeColor = Color.FromArgb(60, 60, 60) };
            btnZoomIn.Click += (s, e) => _pdfPanel.SetZoom(Math.Min(3f, _pdfPanel.Zoom + 0.1f));
            var sep2 = new ToolStripSeparator();
            _lblPage = new ToolStripLabel("跳转:") { ForeColor = Color.FromArgb(60, 60, 60) };
            _txtPage = new ToolStripTextBox { ForeColor = Color.FromArgb(60, 60, 60), BackColor = Color.FromArgb(255, 255, 255), Size = new Size(45, 22) };
            _txtPage.TextBox.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) JumpToPage(); };
            _lblTotal = new ToolStripLabel("/ 0") { ForeColor = Color.FromArgb(60, 60, 60) };
            _toolbar.Items.AddRange(new ToolStripItem[] { _btnOpen, _btnOutline, _sep1, btnPrint, sep1b, btnZoomOut, lblZoom, btnZoomIn, sep2, _lblPage, _txtPage, _lblTotal });
        }

        private void BuildSplit()
        {
            _split = new SplitContainer { Dock = DockStyle.Fill, SplitterWidth = 1, Panel1MinSize = 120, BackColor = Color.FromArgb(235, 235, 235) };
            _outlineTree = new TreeView { Dock = DockStyle.Fill, BackColor = Color.FromArgb(248, 248, 248), ForeColor = Color.FromArgb(60, 60, 60), BorderStyle = BorderStyle.None, Font = new Font("Microsoft YaHei", 9) };
            _outlineTree.NodeMouseClick += (s, e) => { if (e.Node.Tag is int p) GotoPage(p); };
            _split.Panel1.Controls.Add(_outlineTree);
            _pdfPanel = new PdfScrollPanel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(200, 200, 200) };
            _pdfPanel.PageChanged += (p) => { foreach (ToolStripItem item in _toolbar.Items) if (item is ToolStripLabel l && l.Text.EndsWith("%")) l.Text = ((int)(_pdfPanel.Zoom * 100)) + "%"; };
            _split.Panel2.Controls.Add(_pdfPanel);
            Controls.Add(_split);
            Controls.Add(_toolbar); // Top 必须最后加，否则挡住 Fill
        }

        private void OpenPdf(string path)
        {
            try
            {
                _doc?.Dispose();
                _doc = PdfDocument.Load(path);
                Text = "PDF Reader - " + Path.GetFileName(path);
                _pdfPanel.LoadDocument(_doc);
                LoadOutline();
                _lblTotal.Text = "/ " + _doc.PageCount;
            }
            catch (Exception ex) { MessageBox.Show("无法打开：" + ex.Message); }
        }

        private void LoadOutline()
        {
            _outlineTree.Nodes.Clear();
            if (_doc?.Bookmarks == null) return;
            foreach (var bm in _doc.Bookmarks) { var n = BuildNode(bm); if (n != null) _outlineTree.Nodes.Add(n); }
            if (_outlineTree.Nodes.Count > 0) _outlineTree.Nodes[0].EnsureVisible();
        }

        private TreeNode BuildNode(PdfBookmark bm)
        {
            var n = new TreeNode(bm.Title) { Tag = bm.PageIndex };
            foreach (var c in bm.Children) { var cn = BuildNode(c); if (cn != null) n.Nodes.Add(cn); }
            return n;
        }

        private void GotoPage(int p) { _pdfPanel.GotoPage(p); }

        private void OnPrint()
        {
            if (_doc == null) return;
            try
            {
                using (var dlg = new PrintDialog())
                {
                    dlg.AllowSomePages = true;
                    dlg.AllowSelection = false;
                    dlg.AllowCurrentPage = true;
                    using (var pd = new System.Drawing.Printing.PrintDocument())
                    {
                        pd.DocumentName = Text;
                        dlg.Document = pd;
                        if (dlg.ShowDialog() == DialogResult.OK)
                        {
                            using (var printDoc = _doc.CreatePrintDocument(PdfPrintMode.ShrinkToMargin))
                            {
                                printDoc.PrinterSettings = pd.PrinterSettings;
                                printDoc.Print();
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("打印失败：" + ex.Message); }
        }
        private void JumpToPage() { if (_doc == null || !int.TryParse(_txtPage.Text, out int p)) return; GotoPage(Math.Max(1, Math.Min(p, _doc.PageCount)) - 1); _txtPage.Text = ""; }

        private void OnKeyDown(object s, KeyEventArgs e)
        {
            if (_doc == null) return;
            if (e.KeyCode == Keys.PageDown || e.KeyCode == Keys.Down) { GotoPage(_pdfPanel.CurrentPage + 1); e.Handled = true; }
            else if (e.KeyCode == Keys.PageUp || e.KeyCode == Keys.Up) { GotoPage(_pdfPanel.CurrentPage - 1); e.Handled = true; }
            else if (e.KeyCode == Keys.Home) { GotoPage(0); e.Handled = true; }
            else if (e.KeyCode == Keys.End) { GotoPage(_doc.PageCount - 1); e.Handled = true; }
        }

        protected override void Dispose(bool d) { if (d) _doc?.Dispose(); base.Dispose(d); }
    }

    /// <summary>
    /// 自定义PDF渲染面板：标准Panel + OnPaint渲染可见页 + AutoScroll跳转
    /// </summary>
    public class PdfScrollPanel : Panel
    {
        private PdfDocument _doc;
        private float _zoom = 1.0f;
        private List<int> _pageY = new List<int>();
        private int _totalHeight;
        private int _maxWidth;

        public int CurrentPage { get; private set; }
        public float Zoom => _zoom;
        public event Action<int> PageChanged;

        public PdfScrollPanel()
        {
            AutoScroll = true;
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            MouseWheel += (s, e) =>
            {
                if ((ModifierKeys & Keys.Control) != 0)
                {
                    ((HandledMouseEventArgs)e).Handled = true;
                    SetZoom(_zoom + Math.Sign(e.Delta) * 0.1f);
                }
            };
        }

        public void LoadDocument(PdfDocument doc)
        {
            _doc = doc;
            CurrentPage = 0;
            CalcLayout();
            Invalidate();
        }

        private void CalcLayout()
        {
            _pageY.Clear();
            _totalHeight = 0;
            _maxWidth = 0;
            if (_doc == null) return;
            for (int i = 0; i < _doc.PageCount; i++)
            {
                _pageY.Add(_totalHeight);
                _totalHeight += Math.Max(1, (int)(_doc.PageSizes[i].Height * _zoom));
                int w = Math.Max(1, (int)(_doc.PageSizes[i].Width * _zoom));
                if (w > _maxWidth) _maxWidth = w;
            }
            AutoScrollMinSize = new Size(Math.Max(_maxWidth, ClientSize.Width), _totalHeight);
        }

        public void SetZoom(float z)
        {
            _zoom = z;
            CalcLayout();
            GotoPage(CurrentPage);
        }

        public void GotoPage(int page)
        {
            if (_doc == null || _pageY.Count == 0) return;
            page = Math.Max(0, Math.Min(page, _doc.PageCount - 1));
            CurrentPage = page;
            AutoScrollPosition = new Point(0, _pageY[page]);
            PageChanged?.Invoke(page);
        }

        protected override void OnScroll(ScrollEventArgs se)
        {
            base.OnScroll(se);
            UpdateCurrentPage();
        }

        private void UpdateCurrentPage()
        {
            if (_doc == null || _pageY.Count == 0) return;
            int y = -AutoScrollPosition.Y;
            int page = 0;
            for (int i = _pageY.Count - 1; i >= 0; i--)
            {
                if (y >= _pageY[i]) { page = i; break; }
            }
            if (page != CurrentPage)
            {
                CurrentPage = page;
                PageChanged?.Invoke(page);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (_doc == null) { base.OnPaint(e); return; }

            var g = e.Graphics;
            g.Clear(BackColor);

            var scroll = AutoScrollPosition;
            int viewTop = -scroll.Y;
            int viewH = ClientSize.Height;
            int viewBottom = viewTop + viewH;

            int cw = ClientSize.Width;

            for (int i = 0; i < _doc.PageCount; i++)
            {
                int py = _pageY[i];
                int ph = Math.Max(1, (int)(_doc.PageSizes[i].Height * _zoom));
                if (py + ph < viewTop - 50 || py > viewBottom + 50) continue;

                int pw = Math.Max(1, (int)(_doc.PageSizes[i].Width * _zoom));
                int x = (cw - pw) / 2;
                if (x < 0) x = 0;

                try
                {
                    using (var img = _doc.Render(i, pw, ph, 96, 96, false))
                        g.DrawImage(img, x + scroll.X, py + scroll.Y, pw, ph);
                }
                catch { }
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_doc != null) { CalcLayout(); Invalidate(); }
        }
    }

    internal class DarkRenderer : ToolStripProfessionalRenderer { public DarkRenderer() : base(new DC()) { } }
    internal class DC : ProfessionalColorTable
    {
        public override Color ToolStripGradientBegin => Color.FromArgb(45, 45, 48);
        public override Color ToolStripGradientMiddle => Color.FromArgb(45, 45, 48);
        public override Color ToolStripGradientEnd => Color.FromArgb(45, 45, 48);
        public override Color MenuBorder => Color.FromArgb(60, 60, 60);
        public override Color MenuItemBorder => Color.FromArgb(60, 60, 60);
        public override Color MenuItemSelected => Color.FromArgb(0x00, 0x7A, 0xCC);
    }
}