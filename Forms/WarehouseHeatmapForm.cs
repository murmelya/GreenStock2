using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GreenStock.Services;

namespace GreenStock.Forms;

public class WarehouseHeatmapForm : Form
{
    private Panel _heatmapPanel;
    private Label _titleLabel;
    private Button _btnRefresh;
    private ToolTip _toolTip = new ToolTip();
    private ComboBox _cmbMode;
    private Label _lblStats;
    private GroupBox _legendBox;
    private List<HeatmapCell> _cells;
    private string _currentMode = "expiry";

    public WarehouseHeatmapForm()
    {
        InitializeComponent();
        LoadHeatmap();
    }

    private void InitializeComponent()
    {
        this.Text = "GreenStock — Тепловая карта склада";
        this.Size = new Size(1300, 800);
        this.StartPosition = FormStartPosition.CenterParent;
        this.BackColor = Color.White;
        this.MinimumSize = new Size(1100, 700);

        var menuStrip = new MenuStrip { BackColor = Color.FromArgb(28, 42, 74), ForeColor = Color.White };
        menuStrip.Font = new Font("Segoe UI", 10, FontStyle.Regular);
        menuStrip.Items.Add(new ToolStripMenuItem("Файл") { ForeColor = Color.White });
        menuStrip.Items.Add(new ToolStripMenuItem("Поставки") { ForeColor = Color.White });
        menuStrip.Items.Add(new ToolStripMenuItem("Отгрузки") { ForeColor = Color.White });
        menuStrip.Items.Add(new ToolStripMenuItem("Отчёты") { ForeColor = Color.White });

        var skladMenu = new ToolStripMenuItem("Склад ▼") { ForeColor = Color.White };
        skladMenu.DropDownItems.Add(new ToolStripMenuItem("Тепловая карта") { ForeColor = Color.Black });
        menuStrip.Items.Add(skladMenu);

        menuStrip.Items.Add(new ToolStripMenuItem("Помощь") { ForeColor = Color.White });

        _titleLabel = new Label
        {
            Text = "Тепловая карта склада",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 120, 200),
            Location = new Point(20, 40),
            AutoSize = true
        };

        var separator1 = new Panel
        {
            Location = new Point(20, 75),
            Size = new Size(this.ClientSize.Width - 40, 2),
            BackColor = Color.LightGray,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        var lblMode = new Label
        {
            Text = "Режим отображения",
            Location = new Point(20, 95),
            Size = new Size(150, 25),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.Black
        };

        _cmbMode = new ComboBox
        {
            Location = new Point(180, 92),
            Size = new Size(200, 27),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 10, FontStyle.Regular)
        };
        _cmbMode.Items.AddRange(new object[] { "По сроку годности", "По скорости движения" });
        _cmbMode.SelectedIndex = 0;
        _cmbMode.SelectedIndexChanged += (s, e) =>
        {
            _currentMode = _cmbMode.SelectedIndex == 0 ? "expiry" : "movement";
            LoadHeatmap();
        };

        var separator2 = new Panel
        {
            Location = new Point(20, 130),
            Size = new Size(this.ClientSize.Width - 40, 2),
            BackColor = Color.LightGray,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        _heatmapPanel = new Panel
        {
            Location = new Point(20, 145),
            Size = new Size(820, 500),
            BackColor = Color.LightGray,
            BorderStyle = BorderStyle.FixedSingle
        };

        _legendBox = new GroupBox
        {
            Text = "Легенда (Срок годности)",
            Location = new Point(860, 145),
            Size = new Size(400, 260),
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = Color.FromArgb(28, 42, 74)
        };

        int legendY = 25;
        AddLegendItem(_legendBox, Color.FromArgb(0, 200, 0), "Свежий — более 30 дней", ref legendY);
        AddLegendItem(_legendBox, Color.FromArgb(255, 200, 0), "Внимание — 7–30 дней", ref legendY);
        AddLegendItem(_legendBox, Color.FromArgb(255, 100, 0), "Срочно — менее 7 дней", ref legendY);
        AddLegendItem(_legendBox, Color.FromArgb(255, 0, 0), "Просрочен / Истёк", ref legendY);
        AddLegendItem(_legendBox, Color.LightGray, "Ячейка свободна", ref legendY);
        AddLegendItem(_legendBox, Color.FromArgb(173, 216, 230), "Срок не отслеживается", ref legendY);

        var separator3 = new Panel
        {
            Location = new Point(20, 660),
            Size = new Size(this.ClientSize.Width - 40, 2),
            BackColor = Color.LightGray,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        _lblStats = new Label
        {
            Text = "Занято ячеек: 0 из 48 | Просрочено: 0 | Истекает в течение 7 дней: 0",
            Location = new Point(20, 680),
            Size = new Size(600, 30),
            Font = new Font("Segoe UI", 10, FontStyle.Regular),
            ForeColor = Color.FromArgb(64, 64, 64)
        };

        _btnRefresh = new Button
        {
            Text = "Обновить карту",
            Location = new Point(this.ClientSize.Width - 150, 675),
            Size = new Size(130, 35),
            BackColor = Color.FromArgb(40, 120, 200),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 10, FontStyle.Regular),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right
        };
        _btnRefresh.Click += (s, e) => LoadHeatmap();

        this.Controls.Add(menuStrip);
        this.Controls.Add(_titleLabel);
        this.Controls.Add(separator1);
        this.Controls.Add(lblMode);
        this.Controls.Add(_cmbMode);
        this.Controls.Add(separator2);
        this.Controls.Add(_heatmapPanel);
        this.Controls.Add(_legendBox);
        this.Controls.Add(separator3);
        this.Controls.Add(_lblStats);
        this.Controls.Add(_btnRefresh);

        this.Resize += (s, e) =>
        {
            int width = this.ClientSize.Width - 40;
            separator1.Width = width;
            separator2.Width = width;
            separator3.Width = width;
            _btnRefresh.Location = new Point(this.ClientSize.Width - 150, 675);
        };
    }

    private void AddLegendItem(GroupBox box, Color color, string text, ref int y)
    {
        var colorBox = new Panel
        {
            Location = new Point(15, y),
            Size = new Size(20, 20),
            BackColor = color,
            BorderStyle = BorderStyle.FixedSingle
        };
        var label = new Label
        {
            Text = text,
            Location = new Point(45, y),
            Size = new Size(340, 20),
            Font = new Font("Segoe UI", 9, FontStyle.Regular),
            TextAlign = ContentAlignment.MiddleLeft
        };
        box.Controls.Add(colorBox);
        box.Controls.Add(label);
        y += 28;
    }

    private void LoadHeatmap()
    {
        _heatmapPanel.Controls.Clear();

        _cells = WarehouseHeatmapService.GenerateHeatmap();
        var (rows, cols) = WarehouseHeatmapService.GetGridSize();

        int cellWidth = _heatmapPanel.Width / cols;
        int cellHeight = _heatmapPanel.Height / rows;

        int occupied = 0;
        int expired = 0;
        int expiringSoon = 0;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                int index = row * cols + col;
                var cell = index < _cells.Count ? _cells[index] : null;

                if (cell == null || cell.ProductName == "Свободно")
                {
                    var emptyButton = new Button
                    {
                        Location = new Point(col * cellWidth, row * cellHeight),
                        Size = new Size(cellWidth - 2, cellHeight - 2),
                        BackColor = Color.LightGray,
                        FlatStyle = FlatStyle.Flat,
                        Tag = null,
                        Text = "",
                        Font = new Font("Segoe UI", 7),
                        TextAlign = ContentAlignment.MiddleCenter
                    };
                    emptyButton.FlatAppearance.BorderColor = Color.Gray;
                    emptyButton.FlatAppearance.BorderSize = 1;
                    _heatmapPanel.Controls.Add(emptyButton);

                    if (cell != null && cell.ProductName == "Свободно")
                        occupied--; 
                    continue;
                }

                occupied++;
                if (cell.DaysUntilExpiry >= 0 && cell.DaysUntilExpiry <= 0)
                    expired++;
                else if (cell.DaysUntilExpiry >= 1 && cell.DaysUntilExpiry <= 7)
                    expiringSoon++;

                Color heatColor;
                if (_currentMode == "expiry")
                    heatColor = cell.HeatColor;
                else
                    heatColor = GetMovementColor(cell.MovementVelocity);

                var button = new Button
                {
                    Location = new Point(col * cellWidth, row * cellHeight),
                    Size = new Size(cellWidth - 2, cellHeight - 2),
                    BackColor = heatColor,
                    FlatStyle = FlatStyle.Flat,
                    Tag = cell,
                    Text = cell.ProductName.Length > 0
                        ? (cell.ProductName.Length > 10 ? cell.ProductName.Substring(0, 8) + ".." : cell.ProductName)
                        : "??",
                    Font = new Font("Segoe UI", 7, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                button.FlatAppearance.BorderColor = Color.Gray;
                button.FlatAppearance.BorderSize = 1;

                button.MouseEnter += (s, e) =>
                {
                    var hoveredCell = (HeatmapCell)((Button)s).Tag;
                    _toolTip.SetToolTip((Button)s, hoveredCell.Tooltip);
                    ((Button)s).BackColor = Color.FromArgb(
                        Math.Min(255, heatColor.R + 30),
                        Math.Min(255, heatColor.G + 30),
                        Math.Min(255, heatColor.B + 30)
                    );
                };

                button.MouseLeave += (s, e) => ((Button)s).BackColor = heatColor;

                button.Click += (s, e) =>
                {
                    var clickedCell = (HeatmapCell)((Button)s).Tag;
                    if (clickedCell != null)
                    {
                        MessageBox.Show(clickedCell.Tooltip, "Информация о товаре",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                };

                _heatmapPanel.Controls.Add(button);
            }
        }

        int totalCells = rows * cols;
        _lblStats.Text = $"Занято ячеек: {occupied} из {totalCells} | Просрочено: {expired} | Истекает в течение 7 дней: {expiringSoon}";
    }

    private Color GetMovementColor(double velocity)
    {
        if (velocity > 10) return Color.FromArgb(0, 200, 0);      
        if (velocity > 5) return Color.FromArgb(150, 200, 0);     
        if (velocity > 2) return Color.FromArgb(255, 200, 0);     
        if (velocity > 0.5) return Color.FromArgb(255, 100, 0);   
        return Color.FromArgb(255, 0, 0);                         
    }
}