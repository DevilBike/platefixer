using PlateFixer.SaveData;

namespace PlateFixer.Editor;

internal sealed class MainForm : Form
{
    private readonly ComboBox folderBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly ComboBox bodyBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 320 };
    private readonly DataGridView grid = new()
    {
        Dock = DockStyle.Fill,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells,
        RowHeadersVisible = false,
    };
    private readonly Label status = new() { Dock = DockStyle.Bottom, Height = 24, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Button saveButton = new() { Text = "Save", AutoSize = true, Enabled = false };

    private CharacterFolder? folder;
    private UiSaveFile? file;
    private BannerSection? banner;
    private bool dirty;

    public MainForm()
    {
        Text = "Plate Fixer portrait editor";
        Width = 1100;
        Height = 700;

        var browse = new Button { Text = "Other folder...", AutoSize = true };
        browse.Click += (_, _) => BrowseForRoot();
        var top = new TableLayoutPanel { Dock = DockStyle.Top, Height = 34, ColumnCount = 3, Padding = new Padding(6) };
        top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        top.Controls.Add(new Label { Text = "Character:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        top.Controls.Add(folderBox, 1, 0);
        top.Controls.Add(browse, 2, 0);

        var copyLook = new Button { Text = "Copy look from first selected to the others", AutoSize = true };
        copyLook.Click += (_, _) => CopyLook();
        saveButton.Click += (_, _) => Save();
        var actions = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 38, Padding = new Padding(6) };
        actions.Controls.Add(new Label { Text = "This character is:", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
        actions.Controls.Add(bodyBox);
        actions.Controls.Add(copyLook);
        actions.Controls.Add(saveButton);

        Controls.Add(grid);
        Controls.Add(actions);
        Controls.Add(top);
        Controls.Add(status);

        BuildColumns();
        folderBox.SelectedIndexChanged += (_, _) => LoadSelectedFolder();
        bodyBox.SelectedIndexChanged += (_, _) => RefreshFlags();
        grid.CellValueChanged += (_, e) => OnCellEdited(e.RowIndex, e.ColumnIndex);
        FormClosing += (_, e) => e.Cancel = dirty && !ConfirmDiscard();

        LoadRoot(CharacterFolders.DefaultRoot);
    }

    private void BuildColumns()
    {
        void Col(string name, string header, bool editable = false) =>
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = name, HeaderText = header, ReadOnly = !editable });

        Col("Id", "#");
        Col("Problem", "Problem");
        Col("Background", "Background", true);
        Col("Frame", "Frame", true);
        Col("Accent", "Accent", true);
        Col("Pose", "Pose", true);
        Col("Expression", "Expression", true);
        Col("Body", "Saved on");
        Col("Updated", "Last updated");
    }

    private void LoadRoot(string root)
    {
        var folders = CharacterFolders.Find(root);
        folderBox.Items.Clear();
        foreach (var f in folders)
            folderBox.Items.Add(f);
        if (folders.Count > 0)
            folderBox.SelectedIndex = 0;
        else
            status.Text = $"No character folders found in {root}. Use \"Other folder...\" to pick it.";
    }

    private void BrowseForRoot()
    {
        using var dialog = new FolderBrowserDialog { Description = "Pick the FINAL FANTASY XIV - A Realm Reborn folder" };
        if (dialog.ShowDialog(this) == DialogResult.OK)
            LoadRoot(dialog.SelectedPath);
    }

    private void LoadSelectedFolder()
    {
        if (folderBox.SelectedItem is not CharacterFolder selected || (dirty && !ConfirmDiscard()))
            return;

        try
        {
            folder = selected;
            file = UiSaveFile.Load(selected.UiSavePath);
            var section = file.FindSection(UiSaveFile.BannerSectionId)
                          ?? throw new InvalidDataException("This file has no portrait section.");
            banner = BannerSection.Parse(section.Data);
            dirty = false;
            saveButton.Enabled = false;
            FillBodyTypes();
            FillGrid();
            status.Text = $"Loaded {banner.Portraits.Count} portraits from {selected.UiSavePath}";
        }
        catch (Exception ex)
        {
            file = null;
            banner = null;
            grid.Rows.Clear();
            status.Text = $"Couldn't read this character's portraits: {ex.Message}";
        }
    }

    /// <summary>Offers each body type found, defaulting to the one on the most recently updated portrait.</summary>
    private void FillBodyTypes()
    {
        bodyBox.Items.Clear();
        if (banner is null)
            return;

        var types = banner.Portraits
            .Where(p => p.Appearance is not null)
            .GroupBy(p => p.Appearance!.Value)
            .OrderByDescending(g => g.Max(p => p.LastUpdated ?? DateTimeOffset.MinValue))
            .Select(g => (object)g.Key)
            .ToArray();
        bodyBox.Items.AddRange(types);
        if (types.Length > 0)
            bodyBox.SelectedIndex = 0;
    }

    private void FillGrid()
    {
        grid.Rows.Clear();
        if (banner is null)
            return;

        foreach (var p in banner.Portraits)
        {
            var row = grid.Rows[grid.Rows.Add()];
            row.Tag = p;
            row.Cells["Id"].Value = p.Id;
            row.Cells["Background"].Value = p.Background;
            row.Cells["Frame"].Value = p.Frame;
            row.Cells["Accent"].Value = p.Accent;
            row.Cells["Pose"].Value = p.Pose;
            row.Cells["Expression"].Value = p.Expression;
            row.Cells["Body"].Value = p.Appearance?.ToString();
            row.Cells["Updated"].Value = p.LastUpdated?.LocalDateTime.ToString("yyyy-MM-dd");
        }

        RefreshFlags();
    }

    private void RefreshFlags()
    {
        var current = bodyBox.SelectedItem as PortraitAppearance?;
        foreach (DataGridViewRow row in grid.Rows)
        {
            var p = (Portrait)row.Tag!;
            var mismatch = current is not null && p.Appearance is { } a && a != current;
            row.Cells["Problem"].Value = mismatch ? "Saved on a different body type (copied from another character?)" : "";
            row.DefaultCellStyle.BackColor = mismatch ? Color.MistyRose : grid.DefaultCellStyle.BackColor;
        }
    }

    private void OnCellEdited(int rowIndex, int columnIndex)
    {
        if (rowIndex < 0 || grid.Rows[rowIndex].Tag is not Portrait p)
            return;

        var column = grid.Columns[columnIndex].Name;
        var cell = grid.Rows[rowIndex].Cells[columnIndex];
        if (!ushort.TryParse(cell.Value?.ToString(), out var value))
        {
            status.Text = "Enter a whole number from 0 to 65535.";
            cell.Value = Read(p, column);
            return;
        }

        switch (column)
        {
            case "Background": p.Background = value; break;
            case "Frame": p.Frame = value; break;
            case "Accent": p.Accent = value; break;
            case "Pose": p.Pose = value; break;
            case "Expression": p.Expression = value; break;
            default: return;
        }

        MarkDirty();
    }

    private static ushort? Read(Portrait p, string column) => column switch
    {
        "Background" => p.Background,
        "Frame" => p.Frame,
        "Accent" => p.Accent,
        "Pose" => p.Pose,
        "Expression" => p.Expression,
        _ => null,
    };

    private void CopyLook()
    {
        var rows = grid.SelectedRows.Cast<DataGridViewRow>().OrderBy(r => r.Index).ToList();
        if (rows.Count < 2)
        {
            status.Text = "Select the portrait to copy from, then Ctrl+click the ones to copy onto.";
            return;
        }

        var source = (Portrait)rows[0].Tag!;
        foreach (var row in rows.Skip(1))
            ((Portrait)row.Tag!).CopyLookFrom(source);

        FillGrid();
        MarkDirty();
        status.Text = $"Copied the pose, background, frame, camera and lighting of #{source.Id} onto {rows.Count - 1} portrait(s). Not saved yet.";
    }

    private void MarkDirty()
    {
        dirty = true;
        saveButton.Enabled = true;
    }

    private void Save()
    {
        if (file is null || banner is null || folder is null)
            return;

        if (CharacterFolders.GameIsRunning())
        {
            MessageBox.Show(this, "Close the game first. It overwrites this file while it's running.", Text,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            file.FindSection(UiSaveFile.BannerSectionId)!.Data = banner.ToBytes();
            var bytes = file.ToBytes();
            UiSaveFile.Parse(bytes); // never write something we can't read back

            var backup = $"{folder.UiSavePath}.{DateTime.Now:yyyyMMdd-HHmmss}.platefixer.bak";
            File.Copy(folder.UiSavePath, backup);
            File.WriteAllBytes(folder.UiSavePath, bytes);

            dirty = false;
            saveButton.Enabled = false;
            status.Text = $"Saved. The old file was backed up to {Path.GetFileName(backup)}.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Couldn't save: {ex.Message}", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private bool ConfirmDiscard() =>
        MessageBox.Show(this, "You have unsaved changes. Throw them away?", Text,
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
}
