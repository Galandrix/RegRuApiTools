namespace RegRuApiTools;

internal sealed class SettingsDialog : Form
{
    private readonly ComboBox _language = new();
    private readonly TextBox _username = new();
    private readonly TextBox _password = new();
    private readonly Button _eye = Ui.FieldIconButton();
    private bool _passwordVisible;

    public SettingsDialog()
    {
        Text = Lang.Settings;
        Ui.ApplyIcon(this);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        Font = new Font("Segoe UI", 9f);
        ClientSize = new Size(480, 390);
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

        var fields = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 8,
        };
        fields.RowStyles.Add(new RowStyle(SizeType.Absolute, Ui.LabelHeight));
        fields.RowStyles.Add(new RowStyle(SizeType.Absolute, Ui.FieldRow));
        fields.RowStyles.Add(new RowStyle(SizeType.Absolute, Ui.LabelHeight));
        fields.RowStyles.Add(new RowStyle(SizeType.Absolute, Ui.FieldRow));
        fields.RowStyles.Add(new RowStyle(SizeType.Absolute, Ui.LabelHeight));
        fields.RowStyles.Add(new RowStyle(SizeType.Absolute, Ui.FieldRow));
        fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 8));
        fields.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        fields.Controls.Add(Ui.FieldLabel(Lang.Language), 0, 0);
        _language.DropDownStyle = ComboBoxStyle.DropDownList;
        Ui.StyleInput(_language);
        _language.Items.Add(Lang.LanguageEnglish);
        _language.Items.Add(Lang.LanguageRussian);
        _language.SelectedIndex = AppSettings.Language == AppLanguage.Ru ? 1 : 0;
        fields.Controls.Add(_language, 0, 1);

        fields.Controls.Add(Ui.FieldLabel(Lang.Username), 0, 2);
        Ui.StyleInput(_username);
        _username.Text = AppSettings.Username;
        fields.Controls.Add(_username, 0, 3);

        fields.Controls.Add(Ui.FieldLabel(Lang.ApiPassword), 0, 4);
        var passwordRow = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            Height = Ui.FieldHeight,
            Margin = Ui.FieldMargin,
        };
        passwordRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        passwordRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Ui.FieldIconSize + 4));
        _password.Dock = DockStyle.Fill;
        _password.AutoSize = false;
        _password.UseSystemPasswordChar = true;
        _password.Text = AppSettings.Password;
        _eye.Image = Ui.EyeIcon(false);
        _eye.Click += (_, _) => TogglePassword();
        passwordRow.Controls.Add(_password, 0, 0);
        passwordRow.Controls.Add(_eye, 1, 0);
        fields.Controls.Add(passwordRow, 0, 5);

        var hint = new Label
        {
            Text = Lang.SettingsHint,
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(70, 70, 70),
            UseCompatibleTextRendering = true,
        };
        fields.Controls.Add(hint, 0, 7);
        root.Controls.Add(fields, 0, 0);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
        };
        var cancel = Ui.TextButton(Lang.Cancel);
        cancel.DialogResult = DialogResult.Cancel;
        var ok = Ui.TextButton(Lang.Ok);
        ok.Click += (_, _) => SaveAndClose();
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(ok);
        root.Controls.Add(buttons, 0, 1);

        AcceptButton = ok;
        CancelButton = cancel;
    }

    private void TogglePassword()
    {
        _passwordVisible = !_passwordVisible;
        _password.UseSystemPasswordChar = !_passwordVisible;
        _eye.Image = Ui.EyeIcon(_passwordVisible);
    }

    private void SaveAndClose()
    {
        AppSettings.Language = _language.SelectedIndex == 1 ? AppLanguage.Ru : AppLanguage.En;
        AppSettings.Username = _username.Text.Trim();
        AppSettings.Password = _password.Text;
        try
        {
            AppSettings.Save();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, Lang.Settings, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }
}
