namespace RegRuDns;

internal sealed class SoaDialog : Form
{
    private readonly TextBox _ttl = new();
    private readonly TextBox _minimum = new();

    public string Ttl => _ttl.Text.Trim();
    public string MinimumTtl => _minimum.Text.Trim();

    public SoaDialog(string ttl, string minimumTtl)
    {
        Text = Lang.SoaTitle;
        Ui.ApplyIcon(this);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        Font = new Font("Segoe UI", 9f);
        ClientSize = new Size(420, 250);
        BackColor = Color.FromArgb(240, 240, 240);
        Padding = new Padding(16);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, Ui.LabelHeight));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, Ui.FieldRow));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, Ui.LabelHeight));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, Ui.FieldRow));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, Ui.ButtonRow));
        Controls.Add(root);

        root.Controls.Add(Ui.FieldLabel(Lang.Ttl), 0, 0);
        Ui.StyleInput(_ttl);
        _ttl.Text = ttl;
        root.Controls.Add(_ttl, 0, 1);
        root.Controls.Add(Ui.FieldLabel(Lang.MinimumTtl), 0, 2);
        Ui.StyleInput(_minimum);
        _minimum.Text = minimumTtl;
        root.Controls.Add(_minimum, 0, 3);

        var hint = new Label
        {
            Text = Lang.SoaHint,
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(70, 70, 70),
            UseCompatibleTextRendering = true,
        };
        root.Controls.Add(hint, 0, 4);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
        };
        var cancel = Ui.TextButton(Lang.Cancel);
        cancel.DialogResult = DialogResult.Cancel;
        var ok = Ui.TextButton(Lang.Ok);
        ok.Click += (_, _) => AcceptIfValid();
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(ok);
        root.Controls.Add(buttons, 0, 5);
        AcceptButton = ok;
        CancelButton = cancel;
    }

    private void AcceptIfValid()
    {
        if (Ttl.Length == 0 || MinimumTtl.Length == 0)
        {
            MessageBox.Show(this, Lang.NeedTtl, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }
}
