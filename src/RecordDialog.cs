using System.Net;
using System.Net.Sockets;

namespace RegRuApiTools;

internal sealed class RecordDialog : Form
{
    private static readonly string[] Types = ["A", "AAAA", "CNAME", "MX", "TXT", "NS", "SRV", "SPF"];

    private readonly ComboBox _type = new();
    private readonly Label _nameLabel = new();
    private readonly TextBox _name = new();
    private readonly Label _valueLabel = new();
    private readonly TextBox _value = new();
    private readonly Label _priorityLabel = new();
    private readonly NumericUpDown _priority = new();
    private readonly Label _weightLabel = new();
    private readonly NumericUpDown _weight = new();
    private readonly Label _portLabel = new();
    private readonly NumericUpDown _port = new();
    private readonly Label _hint = new();
    private readonly TableLayoutPanel _fields = new();

    public RecordDraft? Draft { get; private set; }

    public RecordDialog(RecordDraft? existing)
    {
        Text = existing is null ? Lang.RecordTitleNew : Lang.RecordTitleEdit;
        Ui.ApplyIcon(this);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        Font = new Font("Segoe UI", 9f);
        ClientSize = new Size(480, 520);
        BackColor = Color.FromArgb(240, 240, 240);
        Padding = new Padding(16);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, Ui.ButtonRow));
        Controls.Add(root);

        _fields.Dock = DockStyle.Fill;
        _fields.ColumnCount = 1;
        _fields.RowCount = 13;
        for (var i = 0; i < 12; i++)
        {
            _fields.RowStyles.Add(new RowStyle(SizeType.Absolute, i % 2 == 0 ? Ui.LabelHeight : Ui.FieldRow));
        }

        _fields.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _fields.Controls.Add(Ui.FieldLabel(Lang.Type), 0, 0);
        _type.DropDownStyle = ComboBoxStyle.DropDownList;
        Ui.StyleInput(_type);
        _type.Items.AddRange(Types);
        _type.SelectedIndexChanged += (_, _) => ApplyType();
        _fields.Controls.Add(_type, 0, 1);

        StyleCaption(_nameLabel);
        _fields.Controls.Add(_nameLabel, 0, 2);
        Ui.StyleInput(_name);
        _fields.Controls.Add(_name, 0, 3);

        StyleCaption(_valueLabel);
        _fields.Controls.Add(_valueLabel, 0, 4);
        _value.Multiline = true;
        _value.ScrollBars = ScrollBars.Vertical;
        _value.Height = 72;
        _value.Dock = DockStyle.Fill;
        _value.Margin = Ui.FieldMargin;
        _fields.Controls.Add(_value, 0, 5);
        _fields.RowStyles[5] = new RowStyle(SizeType.Absolute, 80);

        StyleNumber(_priorityLabel, _priority, 6);
        StyleNumber(_weightLabel, _weight, 8);
        StyleNumber(_portLabel, _port, 10);
        _port.Minimum = 0;
        _port.Value = 0;

        _hint.Dock = DockStyle.Fill;
        _hint.ForeColor = Color.FromArgb(70, 70, 70);
        _hint.UseCompatibleTextRendering = true;
        _fields.Controls.Add(_hint, 0, 12);
        root.Controls.Add(_fields, 0, 0);

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
        root.Controls.Add(buttons, 0, 1);
        AcceptButton = ok;
        CancelButton = cancel;

        var initial = existing?.Type ?? "A";
        var index = Array.FindIndex(Types, t => t.Equals(initial, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            _name.Text = existing.Name;
            _value.Text = existing.Value;
            _priority.Value = Math.Clamp(existing.Priority, 0, 65535);
            _weight.Value = Math.Clamp(existing.Weight, 0, 65535);
            _port.Value = Math.Clamp(existing.Port, 0, 65535);
        }

        _type.SelectedIndex = index >= 0 ? index : 0;
        ApplyType();
    }

    private static void StyleCaption(Label label)
    {
        label.AutoSize = false;
        label.Dock = DockStyle.Fill;
        label.TextAlign = ContentAlignment.MiddleLeft;
        label.UseCompatibleTextRendering = true;
    }

    private void StyleNumber(Label label, NumericUpDown box, int row)
    {
        label.AutoSize = false;
        label.Dock = DockStyle.Fill;
        label.TextAlign = ContentAlignment.MiddleLeft;
        label.UseCompatibleTextRendering = true;
        box.Maximum = 65535;
        box.Minimum = 0;
        box.Width = 100;
        box.AutoSize = false;
        box.Height = Ui.FieldHeight;
        box.Dock = DockStyle.Left;
        box.Margin = Ui.FieldMargin;
        _fields.Controls.Add(label, 0, row);
        _fields.Controls.Add(box, 0, row + 1);
    }

    private void ApplyType()
    {
        var type = SelectedType;
        var srv = type == "SRV";
        var priority = type is "MX" or "NS" or "SRV";
        var weight = srv;
        SetRow(_priorityLabel, _priority, 6, priority);
        SetRow(_weightLabel, _weight, 8, weight);
        SetRow(_portLabel, _port, 10, srv);

        _nameLabel.Text = srv ? Lang.FieldService : Lang.FieldName;
        _valueLabel.Text = type switch
        {
            "A" => Lang.FieldIp,
            "AAAA" => Lang.FieldIpv6,
            "CNAME" => Lang.FieldCanonical,
            "MX" => Lang.FieldMail,
            "NS" => Lang.FieldNs,
            "SRV" => Lang.FieldTarget,
            _ => Lang.FieldText,
        };
        _priorityLabel.Text = type == "NS" ? Lang.FieldNumber : Lang.FieldPriority;
        _weightLabel.Text = Lang.FieldWeight;
        _portLabel.Text = Lang.FieldPort;
        _hint.Text = type switch
        {
            "CNAME" => Lang.HintCname,
            "MX" => Lang.HintMx,
            "TXT" => Lang.HintTxt,
            "NS" => Lang.HintNs,
            "SRV" => Lang.HintSrv,
            "SPF" => Lang.HintSpf,
            _ => Lang.HintA,
        };
        _priority.Maximum = type == "NS" ? 99 : 65535;
    }

    private void SetRow(Control label, Control box, int row, bool visible)
    {
        label.Visible = visible;
        box.Visible = visible;
        _fields.RowStyles[row].Height = visible ? Ui.LabelHeight : 0;
        _fields.RowStyles[row + 1].Height = visible ? Ui.FieldRow : 0;
    }

    private string SelectedType => _type.SelectedItem as string ?? "A";

    private void AcceptIfValid()
    {
        var type = SelectedType;
        var name = _name.Text.Trim();
        var value = _value.Text.Trim();
        if (name.Length == 0)
        {
            name = type == "SRV" ? "" : "@";
        }

        var error = Validate(type, name, value, (int)_priority.Value, (int)_port.Value);
        if (error is not null)
        {
            MessageBox.Show(this, error, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Draft = new RecordDraft(type, name, value, (int)_priority.Value, (int)_weight.Value, (int)_port.Value);
        DialogResult = DialogResult.OK;
        Close();
    }

    private static string? Validate(string type, string name, string value, int priority, int port)
    {
        if (type == "SRV")
        {
            if (name.Length == 0)
            {
                return Lang.NeedService;
            }
        }
        else if (name.Length == 0)
        {
            return Lang.NeedName;
        }

        if (BadName(name))
        {
            return Lang.BadName;
        }

        if (value.Trim().Length == 0)
        {
            return Lang.NeedValue;
        }

        switch (type)
        {
            case "A" when !IsIpv4(value):
                return Lang.BadIpv4;
            case "AAAA" when !IsIpv6(value):
                return Lang.BadIpv6;
            case "CNAME" or "NS" or "SRV" when !IsHost(value):
                return Lang.BadHost;
            case "MX" when !IsHost(value) && !IPAddress.TryParse(value, out _):
                return Lang.BadHost;
            case "SPF" when !value.StartsWith("v=spf1", StringComparison.OrdinalIgnoreCase):
                return Lang.BadSpf;
            case "NS" when priority is < 0 or > 99:
                return Lang.BadNsNumber;
            case "SRV" when port is < 1 or > 65535:
                return Lang.BadPort;
            default:
                return null;
        }
    }

    private static bool BadName(string name)
    {
        if (name is "@" or "*")
        {
            return false;
        }

        if (name.Contains("..", StringComparison.Ordinal) || name.StartsWith('.') || name.EndsWith('.'))
        {
            return true;
        }

        foreach (var c in name)
        {
            if (char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.' or '*')
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private static bool IsHost(string value)
    {
        if (value.Length is 0 or > 253 || value.Contains(' ') || value.Contains("..", StringComparison.Ordinal))
        {
            return false;
        }

        return value.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '-' or '_');
    }

    private static bool IsIpv4(string value) =>
        IPAddress.TryParse(value, out var address)
        && address.AddressFamily == AddressFamily.InterNetwork
        && value.Contains('.');

    private static bool IsIpv6(string value) =>
        IPAddress.TryParse(value, out var address)
        && address.AddressFamily == AddressFamily.InterNetworkV6;
}
