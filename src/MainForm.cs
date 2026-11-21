namespace RegRuDns;

public sealed class MainForm : Form
{
    private readonly ComboBox _domain = new();
    private readonly ListView _records = new();
    private readonly Label _nameValue = new();
    private readonly Label _typeValue = new();
    private readonly Label _valueValue = new();
    private readonly Label _priorityValue = new();
    private readonly Label _stateValue = new();
    private readonly Label _soaValue = new();
    private readonly Label _statusCount = new();
    private readonly Label _statusDomain = new();
    private readonly Label _subtitle = new();
    private readonly Button _addButton = new();
    private readonly Button _refreshButton = new();
    private readonly Button _soaButton = new();
    private readonly Button _settingsButton = new();
    private readonly GroupBox _listBox = new();
    private readonly GroupBox _detailsBox = new();
    private readonly Label[] _detailCaptions = new Label[6];
    private readonly ToolStripMenuItem _copyNameItem = new();
    private readonly ToolStripMenuItem _copyValueItem = new();
    private readonly ToolStripMenuItem _editItem = new();
    private readonly ToolStripMenuItem _deleteItem = new();

    private readonly List<DomainItem> _domains = [];
    private ZoneSnapshot? _zone;
    private bool _filling;
    private bool _busy;
    private bool _started;

    public MainForm()
    {
        Text = Lang.AppTitle;
        Ui.ApplyIcon(this);
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(940, 680);
        ClientSize = new Size(1020, 760);
        Font = new Font("Segoe UI", 9f);
        BackColor = Color.FromArgb(240, 240, 240);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 230));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        Controls.Add(root);

        root.Controls.Add(BuildHeader(), 0, 0);
        root.Controls.Add(BuildToolbar(), 0, 1);
        root.Controls.Add(BuildList(), 0, 2);
        root.Controls.Add(BuildDetails(), 0, 3);
        root.Controls.Add(BuildStatus(), 0, 4);
        ApplyLanguage();
        Shown += async (_, _) => await StartAsync();
    }

    private Control BuildHeader()
    {
        var header = new Panel { Dock = DockStyle.Fill };
        header.Paint += (_, e) =>
        {
            using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                header.ClientRectangle,
                Color.FromArgb(52, 52, 52),
                Color.FromArgb(24, 24, 24),
                90f);
            e.Graphics.FillRectangle(brush, header.ClientRectangle);
        };

        var title = new Label
        {
            AutoSize = true,
            Text = Lang.AppTitle,
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 16f, FontStyle.Bold),
            Location = new Point(18, 16),
        };
        _subtitle.AutoSize = true;
        _subtitle.ForeColor = Color.Gainsboro;
        _subtitle.BackColor = Color.Transparent;
        _subtitle.Font = new Font("Segoe UI", 9f);
        _subtitle.Location = new Point(20, 56);
        header.Controls.Add(title);
        header.Controls.Add(_subtitle);
        return header;
    }

    private Control BuildToolbar()
    {
        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14, 12, 14, 8),
            WrapContents = false,
        };
        _domain.Width = 280;
        _domain.DropDownStyle = ComboBoxStyle.DropDown;
        _domain.Margin = new Padding(0, 0, 10, 0);
        _domain.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
        _domain.AutoCompleteSource = AutoCompleteSource.ListItems;
        _domain.SelectionChangeCommitted += async (_, _) =>
        {
            if (!_filling)
            {
                await LoadZoneAsync();
            }
        };
        _domain.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                await LoadZoneAsync();
            }
        };

        InitToolbarButton(_addButton, async (_, _) => await AddRecordAsync());
        InitToolbarButton(_refreshButton, async (_, _) => await LoadDomainsAsync(true));
        InitToolbarButton(_soaButton, async (_, _) => await EditSoaAsync());
        InitToolbarButton(_settingsButton, async (_, _) =>
        {
            if (EditSettings() && AppSettings.HasCredentials)
            {
                await LoadDomainsAsync(true);
            }
            else if (!AppSettings.HasCredentials)
            {
                _statusCount.Text = Lang.NeedCredentials;
            }
        });
        bar.Controls.Add(_domain);
        bar.Controls.Add(_addButton);
        bar.Controls.Add(_refreshButton);
        bar.Controls.Add(_soaButton);
        bar.Controls.Add(_settingsButton);
        return bar;
    }

    private Control BuildList()
    {
        _listBox.Dock = DockStyle.Fill;
        _listBox.Margin = new Padding(12, 2, 12, 6);
        _listBox.Padding = new Padding(10, 8, 10, 10);
        _records.Dock = DockStyle.Fill;
        _records.View = View.Details;
        _records.FullRowSelect = true;
        _records.GridLines = true;
        _records.HideSelection = false;
        _records.MultiSelect = false;
        _records.SmallImageList = new ImageList { ImageSize = new Size(1, Ui.ListRowHeight) };
        _records.Columns.Add("", 220);
        _records.Columns.Add("", 80);
        _records.Columns.Add("", 420);
        _records.Columns.Add("", 90);
        _records.Columns.Add("", 110);
        _records.SelectedIndexChanged += (_, _) => ShowSelected();
        _records.MouseDown += RecordsMouseDown;
        _records.DoubleClick += async (_, _) => await EditSelectedAsync();
        _records.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Delete)
            {
                await DeleteSelectedAsync();
            }
        };
        _records.ContextMenuStrip = BuildMenu();
        _listBox.Controls.Add(_records);
        return _listBox;
    }

    private Control BuildDetails()
    {
        _detailsBox.Dock = DockStyle.Fill;
        _detailsBox.Margin = new Padding(12, 4, 12, 8);
        _detailsBox.Padding = new Padding(12, 10, 12, 16);
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 6,
            Padding = new Padding(4),
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        AddDetail(grid, 0, _nameValue);
        AddDetail(grid, 1, _typeValue);
        AddDetail(grid, 2, _valueValue);
        AddDetail(grid, 3, _priorityValue);
        AddDetail(grid, 4, _stateValue);
        AddDetail(grid, 5, _soaValue);
        _detailsBox.Controls.Add(grid);
        return _detailsBox;
    }

    private Control BuildStatus()
    {
        var status = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            BackColor = Color.FromArgb(236, 236, 236),
            Padding = new Padding(12, 4, 12, 4),
        };
        status.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        status.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        _statusCount.AutoSize = true;
        _statusCount.Anchor = AnchorStyles.Left;
        _statusDomain.AutoSize = true;
        _statusDomain.Anchor = AnchorStyles.Right;
        status.Controls.Add(_statusCount, 0, 0);
        status.Controls.Add(_statusDomain, 1, 0);
        return status;
    }

    private void AddDetail(TableLayoutPanel grid, int row, Label value)
    {
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, Ui.DetailRowHeight));
        var label = new Label
        {
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 0, 16, 0),
            Padding = new Padding(0, 2, 0, 0),
            UseCompatibleTextRendering = true,
        };
        _detailCaptions[row] = label;
        value.AutoSize = false;
        value.Dock = DockStyle.Fill;
        value.TextAlign = ContentAlignment.MiddleLeft;
        value.AutoEllipsis = true;
        value.Margin = new Padding(0);
        value.Padding = new Padding(0, 2, 0, 2);
        value.UseCompatibleTextRendering = true;
        grid.Controls.Add(label, 0, row);
        grid.Controls.Add(value, 1, row);
    }

    private static void InitToolbarButton(Button button, EventHandler click)
    {
        Ui.StyleTextButton(button);
        button.Margin = new Padding(0, 0, 10, 0);
        button.Click += click;
    }

    private void ApplyLanguage()
    {
        _subtitle.Text = Lang.Subtitle;
        Ui.StyleTextButton(_addButton, Lang.AddRecord);
        Ui.StyleTextButton(_refreshButton, Lang.Refresh);
        Ui.StyleTextButton(_soaButton, Lang.Soa);
        Ui.StyleTextButton(_settingsButton, Lang.Settings);
        _addButton.Margin = new Padding(0, 0, 10, 0);
        _refreshButton.Margin = new Padding(0, 0, 10, 0);
        _soaButton.Margin = new Padding(0, 0, 10, 0);
        _settingsButton.Margin = new Padding(0, 0, 10, 0);
        _listBox.Text = Lang.Records;
        _detailsBox.Text = Lang.SelectedRecord;
        _records.Columns[0].Text = Lang.ColName;
        _records.Columns[1].Text = Lang.ColType;
        _records.Columns[2].Text = Lang.ColValue;
        _records.Columns[3].Text = Lang.ColPriority;
        _records.Columns[4].Text = Lang.ColState;
        _detailCaptions[0].Text = Lang.Name;
        _detailCaptions[1].Text = Lang.Type;
        _detailCaptions[2].Text = Lang.Value;
        _detailCaptions[3].Text = Lang.Priority;
        _detailCaptions[4].Text = Lang.State;
        _detailCaptions[5].Text = Lang.SoaTtl;
        _copyNameItem.Text = Lang.CopyName;
        _copyValueItem.Text = Lang.CopyValue;
        _editItem.Text = Lang.EditRecord;
        _deleteItem.Text = Lang.DeleteRecord;
        _statusCount.Text = Lang.RecordsFound(_records.Items.Count);
        _statusDomain.Text = CurrentDomain.Length == 0 ? Lang.NoDomain : Lang.DomainStatus(CurrentDomain);
        ShowSelected();
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();
        _copyNameItem.Click += (_, _) => Copy(Selected?.DisplayName(ZoneDomain) ?? "");
        _copyValueItem.Click += (_, _) => Copy(Selected?.Content ?? "");
        _editItem.Click += async (_, _) => await EditSelectedAsync();
        _deleteItem.Click += async (_, _) => await DeleteSelectedAsync();
        menu.Items.AddRange(new ToolStripItem[]
        {
            _copyNameItem,
            _copyValueItem,
            new ToolStripSeparator(),
            _editItem,
            _deleteItem,
        });
        menu.Opening += (_, e) =>
        {
            if (Selected is null)
            {
                e.Cancel = true;
            }
        };
        return menu;
    }

    private void RecordsMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right)
        {
            return;
        }

        var hit = _records.HitTest(e.Location);
        if (hit.Item is not null)
        {
            hit.Item.Selected = true;
        }
    }

    private async Task StartAsync()
    {
        if (_started)
        {
            return;
        }

        _started = true;
        if (!AppSettings.HasCredentials)
        {
            EditSettings();
        }

        if (!AppSettings.HasCredentials)
        {
            _statusCount.Text = Lang.NeedCredentials;
            return;
        }

        await LoadDomainsAsync(true);
    }

    private bool EditSettings()
    {
        using var dialog = new SettingsDialog();
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return false;
        }

        ApplyLanguage();
        return true;
    }

    private async Task LoadDomainsAsync(bool loadZone)
    {
        if (_busy || !AppSettings.HasCredentials)
        {
            if (!AppSettings.HasCredentials)
            {
                _statusCount.Text = Lang.NeedCredentials;
            }

            return;
        }

        _busy = true;
        UseWaitCursor = true;
        try
        {
            var client = Client();
            var domains = await client.GetDomainsAsync();
            _domains.Clear();
            _domains.AddRange(domains);
            var keep = CurrentDomain;
            _filling = true;
            _domain.BeginUpdate();
            _domain.Items.Clear();
            foreach (var domain in _domains)
            {
                _domain.Items.Add(domain);
            }

            var match = _domains.FirstOrDefault(d => d.Name.Equals(keep, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                _domain.SelectedItem = match;
            }
            else if (keep.Length > 0)
            {
                _domain.Text = keep;
            }
            else if (_domain.Items.Count > 0)
            {
                _domain.SelectedIndex = 0;
            }

            _domain.EndUpdate();
            _filling = false;
            if (loadZone && CurrentDomain.Length > 0)
            {
                await FillZoneAsync(client);
            }
            else
            {
                UpdateStatus();
            }
        }
        catch (Exception ex)
        {
            _filling = false;
            ShowError(ex);
        }
        finally
        {
            _busy = false;
            UseWaitCursor = false;
        }
    }

    private async Task LoadZoneAsync()
    {
        if (_busy)
        {
            return;
        }

        if (!AppSettings.HasCredentials)
        {
            _statusCount.Text = Lang.NeedCredentials;
            return;
        }

        if (CurrentDomain.Length == 0)
        {
            MessageBox.Show(this, Lang.SelectDomain, Lang.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _busy = true;
        UseWaitCursor = true;
        try
        {
            await FillZoneAsync(Client());
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
        finally
        {
            _busy = false;
            UseWaitCursor = false;
        }
    }

    private async Task FillZoneAsync(RegRuClient client)
    {
        var selected = Selected;
        var zone = await client.GetZoneAsync(CurrentDomain);
        _zone = zone;
        _records.BeginUpdate();
        _records.Items.Clear();
        foreach (var record in zone.Records)
        {
            var item = new ListViewItem(record.DisplayName(zone.Domain)) { Tag = record };
            item.SubItems.Add(record.Type);
            item.SubItems.Add(record.Content);
            item.SubItems.Add(PriorityText(record));
            item.SubItems.Add(StateText(record.State));
            _records.Items.Add(item);
        }

        _records.EndUpdate();
        UpdateStatus();
        if (_records.Items.Count == 0)
        {
            ShowSelected();
            return;
        }

        var match = selected is null
            ? null
            : _records.Items.Cast<ListViewItem>().FirstOrDefault(item => SameRecord(item.Tag as DnsRecord, selected));
        (match ?? _records.Items[0]).Selected = true;
        match?.EnsureVisible();
    }

    private async Task AddRecordAsync()
    {
        if (!EnsureDomain())
        {
            return;
        }

        using var dialog = new RecordDialog(null);
        if (dialog.ShowDialog(this) != DialogResult.OK || dialog.Draft is null)
        {
            return;
        }

        await Mutate(client => client.AddAsync(CurrentDomain, dialog.Draft));
    }

    private async Task EditSelectedAsync()
    {
        var current = Selected;
        if (current is null || !EnsureDomain())
        {
            return;
        }

        using var dialog = new RecordDialog(ToDraft(current));
        if (dialog.ShowDialog(this) != DialogResult.OK || dialog.Draft is null)
        {
            return;
        }

        var draft = dialog.Draft;
        await Mutate(client => client.ReplaceAsync(CurrentDomain, current, draft));
    }

    private async Task DeleteSelectedAsync()
    {
        var current = Selected;
        if (current is null || !EnsureDomain())
        {
            return;
        }

        var name = current.DisplayName(ZoneDomain);
        var answer = MessageBox.Show(
            this,
            Lang.DeleteConfirm(name, current.Type, current.Content),
            Lang.DeleteRecord,
            MessageBoxButtons.OKCancel,
            MessageBoxIcon.Warning);
        if (answer != DialogResult.OK)
        {
            return;
        }

        await Mutate(client => client.RemoveAsync(CurrentDomain, current));
    }

    private async Task EditSoaAsync()
    {
        if (!EnsureDomain() || _zone is null)
        {
            if (_zone is null && CurrentDomain.Length > 0)
            {
                await LoadZoneAsync();
            }

            if (_zone is null)
            {
                return;
            }
        }

        using var dialog = new SoaDialog(_zone.Ttl, _zone.MinimumTtl);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var ttl = dialog.Ttl;
        var minimum = dialog.MinimumTtl;
        await Mutate(client => client.UpdateSoaAsync(CurrentDomain, ttl, minimum));
    }

    private async Task Mutate(Func<RegRuClient, Task> action)
    {
        if (_busy)
        {
            return;
        }

        _busy = true;
        UseWaitCursor = true;
        var failed = false;
        try
        {
            var client = Client();
            await action(client);
            await FillZoneAsync(client);
        }
        catch (Exception ex)
        {
            failed = true;
            ShowError(ex);
        }
        finally
        {
            _busy = false;
            UseWaitCursor = false;
        }

        if (failed)
        {
            await LoadZoneAsync();
        }
    }

    private bool EnsureDomain()
    {
        if (!AppSettings.HasCredentials)
        {
            MessageBox.Show(this, Lang.NeedCredentials, Lang.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }

        if (CurrentDomain.Length == 0)
        {
            MessageBox.Show(this, Lang.SelectDomain, Lang.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }

        return true;
    }

    private void ShowSelected()
    {
        var record = Selected;
        var domain = ZoneDomain;
        _nameValue.Text = record?.DisplayName(domain) ?? Lang.Dash;
        _typeValue.Text = record?.Type ?? Lang.Dash;
        _valueValue.Text = record is null || record.Content.Length == 0 ? Lang.Dash : record.Content;
        _priorityValue.Text = record is null ? Lang.Dash : PriorityText(record);
        _stateValue.Text = record is null ? Lang.Dash : StateText(record.State);
        _soaValue.Text = _zone is null || (_zone.Ttl.Length == 0 && _zone.MinimumTtl.Length == 0)
            ? Lang.Dash
            : $"{ShowOrDash(_zone.Ttl)} / {ShowOrDash(_zone.MinimumTtl)}";
    }

    private void UpdateStatus()
    {
        _statusCount.Text = Lang.RecordsFound(_records.Items.Count);
        _statusDomain.Text = CurrentDomain.Length == 0 ? Lang.NoDomain : Lang.DomainStatus(CurrentDomain);
    }

    private void ShowError(Exception ex)
    {
        var text = ex is RegRuException ? ex.Message : Lang.NetworkError(ex.Message);
        MessageBox.Show(this, text, Lang.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private void Copy(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text == Lang.Dash)
        {
            MessageBox.Show(this, Lang.NothingToCopy, Lang.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Clipboard.SetText(text);
    }

    private DnsRecord? Selected =>
        _records.SelectedItems.Count > 0 ? _records.SelectedItems[0].Tag as DnsRecord : null;

    private string CurrentDomain => _domain.Text.Trim().TrimEnd('.');

    private string ZoneDomain => _zone?.Domain.Length > 0 ? _zone.Domain : CurrentDomain;

    private static RegRuClient Client() => new(AppSettings.Username, AppSettings.Password);

    private static string StateText(string state) =>
        state.Equals("A", StringComparison.OrdinalIgnoreCase) ? Lang.Active
        : state.Length == 0 ? Lang.Dash : state;

    private static string PriorityText(DnsRecord record) =>
        record.Type is "A" or "AAAA" or "CNAME" or "TXT" or "SPF" && record.Priority == 0
            ? Lang.Dash
            : record.Priority.ToString();

    private static string ShowOrDash(string value) => value.Length == 0 ? Lang.Dash : value;

    private static bool SameRecord(DnsRecord? left, DnsRecord right) =>
        left is not null
        && left.Type.Equals(right.Type, StringComparison.OrdinalIgnoreCase)
        && left.Subname.Equals(right.Subname, StringComparison.OrdinalIgnoreCase)
        && left.Content == right.Content
        && left.Priority == right.Priority;

    private static RecordDraft ToDraft(DnsRecord record)
    {
        var weight = 0;
        var port = 0;
        var value = record.Content;
        if (record.Type.Equals("SRV", StringComparison.OrdinalIgnoreCase))
        {
            var parts = record.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 3
                && int.TryParse(parts[0], out weight)
                && int.TryParse(parts[1], out port))
            {
                value = string.Join(' ', parts.Skip(2));
            }
        }

        var name = record.Subname.Length == 0 ? "@" : record.Subname;
        return new RecordDraft(record.Type, name, value, record.Priority, weight, port);
    }
}
