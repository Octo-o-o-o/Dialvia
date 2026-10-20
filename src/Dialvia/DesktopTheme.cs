using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Dialvia {
    public static class DesktopTheme {
        public static Color Paper { get { return SystemInformation.HighContrast ? SystemColors.Window : ColorTranslator.FromHtml("#F7F5F0"); } }
        public static Color Surface { get { return SystemInformation.HighContrast ? SystemColors.Window : ColorTranslator.FromHtml("#FFFDFA"); } }
        public static Color Ink { get { return SystemInformation.HighContrast ? SystemColors.WindowText : ColorTranslator.FromHtml("#202320"); } }
        public static Color Muted { get { return SystemInformation.HighContrast ? SystemColors.WindowText : ColorTranslator.FromHtml("#62675F"); } }
        public static Color Border { get { return SystemInformation.HighContrast ? SystemColors.WindowText : ColorTranslator.FromHtml("#DFE0D8"); } }
        public static Color Copper { get { return SystemInformation.HighContrast ? SystemColors.Highlight : ColorTranslator.FromHtml("#A15135"); } }
        public static Color Soft { get { return SystemInformation.HighContrast ? SystemColors.Window : ColorTranslator.FromHtml("#EEE9DF"); } }
        public static readonly Font Ui = new Font("Segoe UI", 10);
        public static void Apply(Form form) {
            form.Font = Ui; form.BackColor = Paper; form.ForeColor = Ink; form.Icon = Program.AppIcon;
            form.AutoScaleMode = AutoScaleMode.Dpi; form.AutoScaleDimensions = form.CurrentAutoScaleDimensions; form.StartPosition = FormStartPosition.CenterScreen;
        }
        public static void ScaleWindow(Form form) {
            float scale = Math.Max(form.DeviceDpi, form.CurrentAutoScaleDimensions.Width) / 96f;
            if (Math.Abs(scale - 1f) > 0.01f) form.Scale(new SizeF(scale, scale));
            Rectangle area = Screen.FromControl(form).WorkingArea;
            int width = Math.Max(640, area.Width - 64), height = Math.Max(500, area.Height - 64);
            form.MinimumSize = new Size(Math.Min(form.MinimumSize.Width, width), Math.Min(form.MinimumSize.Height, height));
            form.Size = new Size(Math.Min(form.Width, width), Math.Min(form.Height, height));
        }
        public static Label Label(string text, float size, bool bold, bool muted) {
            return new Label { Text = text, AutoSize = true, ForeColor = muted ? Muted : Ink, Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular), Margin = new Padding(0, 0, 0, 9), UseMnemonic = false };
        }
        public static GraphicsPath Rounded(Rectangle bounds, int radius) {
            var path = new GraphicsPath(); int diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90); path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90); path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90); path.CloseFigure(); return path;
        }
        public static FlowLayoutPanel Actions(params Control[] controls) {
            var row = new FlowLayoutPanel { AutoSize = true, WrapContents = true, Margin = new Padding(0, 5, 0, 3), BackColor = Surface };
            row.Controls.AddRange(controls); return row;
        }
    }
    public sealed class BrandButton : Button {
        readonly bool primary;
        bool hovered;
        public BrandButton(string text, bool accent) {
            Text = text; primary = accent; AutoSize = true; AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(16, 9, 16, 9); MinimumSize = new Size(90, 42); Margin = new Padding(0, 0, 8, 6);
            FlatStyle = FlatStyle.Flat; FlatAppearance.BorderSize = 0; UseVisualStyleBackColor = false;
            Cursor = Cursors.Hand; Font = DesktopTheme.Ui; DoubleBuffered = true; ResizeRedraw = true;
        }
        protected override void OnMouseEnter(EventArgs e) { hovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hovered = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnPaint(PaintEventArgs e) {
            e.Graphics.Clear(Parent == null ? DesktopTheme.Paper : Parent.BackColor); e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            if (Width < 4 || Height < 4) return;
            Color background = !Enabled ? DesktopTheme.Soft : primary ? DesktopTheme.Copper : hovered ? DesktopTheme.Soft : DesktopTheme.Surface;
            Color foreground = primary && Enabled ? Color.White : DesktopTheme.Ink;
            using (var path = DesktopTheme.Rounded(new Rectangle(1, 1, Width - 3, Height - 3), 10))
            using (var fill = new SolidBrush(background)) using (var border = new Pen(primary ? DesktopTheme.Copper : DesktopTheme.Border)) {
                e.Graphics.FillPath(fill, path); e.Graphics.DrawPath(border, path);
            }
            TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, foreground, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
            if (Focused && ShowFocusCues) { Rectangle bounds = ClientRectangle; bounds.Inflate(-6, -6); ControlPaint.DrawFocusRectangle(e.Graphics, bounds); }
        }
    }
    public sealed class BrandComboBox : ComboBox {
        public BrandComboBox() { DrawMode = DrawMode.OwnerDrawFixed; FlatStyle = FlatStyle.Flat; ItemHeight = 26; BackColor = DesktopTheme.Surface; ForeColor = DesktopTheme.Ink; }
        protected override void WndProc(ref Message message) {
            base.WndProc(ref message);
            // Native combo boxes omit their selected item from WM_PRINT on some
            // Framework versions. Keep printing/accessibility previews legible.
            if ((message.Msg == 0x0317 || message.Msg == 0x0318) && message.WParam != IntPtr.Zero && DropDownStyle == ComboBoxStyle.DropDownList) {
                using (Graphics graphics = Graphics.FromHdc(message.WParam)) {
                    var bounds = new Rectangle(2, 2, Math.Max(0, Width - SystemInformation.VerticalScrollBarWidth - 7), Height - 4);
                    using (var brush = new SolidBrush(BackColor)) graphics.FillRectangle(brush, bounds);
                    bounds.X += 7; bounds.Width -= 7;
                    TextRenderer.DrawText(graphics, SelectedItem == null ? Text : GetItemText(SelectedItem), Font, bounds, ForeColor, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
                }
            }
        }
        protected override void OnDrawItem(DrawItemEventArgs e) {
            bool selected = (e.State & DrawItemState.Selected) != 0;
            Color background = selected ? SystemColors.Highlight : DesktopTheme.Surface;
            Color foreground = selected ? SystemColors.HighlightText : DesktopTheme.Ink;
            using (var brush = new SolidBrush(background)) e.Graphics.FillRectangle(brush, e.Bounds);
            string text = e.Index >= 0 && e.Index < Items.Count ? GetItemText(Items[e.Index]) : Text;
            Rectangle bounds = e.Bounds; bounds.X += 7; bounds.Width -= 9;
            TextRenderer.DrawText(e.Graphics, text, Font, bounds, foreground, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
            e.DrawFocusRectangle();
        }
    }
    public sealed class BrandCard : FlowLayoutPanel {
        public BrandCard() {
            DoubleBuffered = true; ResizeRedraw = true; BackColor = DesktopTheme.Surface; Padding = new Padding(20);
            FlowDirection = FlowDirection.TopDown; WrapContents = false; AutoSize = true; AutoSizeMode = AutoSizeMode.GrowAndShrink; Margin = new Padding(0, 0, 0, 16);
        }
        protected override void OnPaint(PaintEventArgs e) {
            e.Graphics.Clear(Parent == null ? DesktopTheme.Paper : Parent.BackColor); e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            if (Width > 3 && Height > 3) using (var path = DesktopTheme.Rounded(new Rectangle(0, 0, Width - 1, Height - 1), 14))
                using (var fill = new SolidBrush(DesktopTheme.Surface)) using (var border = new Pen(DesktopTheme.Border)) { e.Graphics.FillPath(fill, path); e.Graphics.DrawPath(border, path); }
            base.OnPaint(e);
        }
    }
}
