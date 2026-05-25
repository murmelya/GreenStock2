using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GreenStock.Services;

namespace GreenStock.Forms
{
    public class WarehouseHeatmapForm : Form
    {
        private Panel _heatmapPanel = null!;
        private Panel _legendPanel = null!;
        private Label _lblStats = null!;
        private Button _btnRefresh = null!;
        private RadioButton _rbExpiry = null!;
        private RadioButton _rbMovement = null!;
        private ToolTip _toolTip = new ToolTip();
        private List<HeatmapCell> _cells = null!;
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

            // --- Меню ---
            var menuStrip = new MenuStrip { BackColor = Color.FromArgb(28, 42, 74), ForeColor = Color.White };
            menuStrip.Font = new Font("Segoe UI", 10);
            menuStrip.Items.Add(new ToolStripMenuItem("Файл"));
            menuStrip.Items.Add(new ToolStripMenuItem("Поставки"));
            menuStrip.Items.Add(new ToolStripMenuItem("Отгрузки"));
            menuStrip.Items.Add(new ToolStripMenuItem("Отчёты"));
            var skladMenu = new ToolStripMenuItem("Склад ▼");
            skladMenu.DropDownItems.Add(new ToolStripMenuItem("Тепловая карта"));
            menuStrip.Items.Add(skladMenu);
            menuStrip.Items.Add(new ToolStripMenuItem("Помощь"));

            // --- Заголовок ---
            var titleLabel = new Label
            {
                Text = "Тепловая карта склада",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 200),
                Location = new Point(20, 40),
                AutoSize = true
            };

            // --- Разделитель ---
            var separator = new Panel
            {
                Location = new Point(20, 75),
                Size = new Size(this.ClientSize.Width - 40, 2),
                BackColor = Color.LightGray,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            // ===== РЕЖИМ ОТОБРАЖЕНИЯ (Жирный текст + радиокнопки) =====
            var lblModeTitle = new Label
            {
                Text = "Режим отображения",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.Black,
                Location = new Point(20, 85),
                AutoSize = true
            };

            _rbExpiry = new RadioButton
            {
                Text = "По сроку годности",
                Location = new Point(200, 87),  // ← сдвинуто вправо, чтобы было рядом с текстом
                AutoSize = true,
                Checked = true,
                Font = new Font("Segoe UI", 10)
            };
            _rbMovement = new RadioButton
            {
                Text = "По скорости движения",
                Location = new Point(360, 87),  // ← сдвинуто вправо
                AutoSize = true,
                Font = new Font("Segoe UI", 10)
            };
            _rbExpiry.CheckedChanged += (s, e) => { if (_rbExpiry.Checked) { _currentMode = "expiry"; LoadHeatmap(); } };
            _rbMovement.CheckedChanged += (s, e) => { if (_rbMovement.Checked) { _currentMode = "movement"; LoadHeatmap(); } };

            // ===== СЕКЦИЯ A — СТЕЛЛАЖ 1 (сверху над таблицей) =====
            var lblSectionTitle = new Label
            {
                Text = "Секция A — Стеллаж 1",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(28, 42, 74),
                Location = new Point(20, 125),
                AutoSize = true
            };

            // ===== ЗАГОЛОВКИ КОЛОНОК (К.1, К.2, ..., К.10) =====
            int cellWidth = 80;
            int startX = 20;
            int colY = 155;  // ← было 190
            for (int col = 1; col <= 10; col++)
            {
                var lblCol = new Label
                {
                    Text = $"К.{col}",
                    Location = new Point(startX + (col - 1) * cellWidth, colY),
                    Size = new Size(cellWidth - 2, 25),
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Color.Transparent,
                    ForeColor = Color.Black
                };
                this.Controls.Add(lblCol);
            }

            // ===== ПАНЕЛЬ С РЯДАМИ (P.1 - P.8 слева) =====
            int startY = 185;  // ← было 220
            int rowHeight = 62;  // ← чуть меньше (было 65)
            for (int row = 1; row <= 8; row++)
            {
                var lblRow = new Label
                {
                    Text = $"P.{row}",
                    Location = new Point(5, startY + (row - 1) * rowHeight),
                    Size = new Size(40, rowHeight - 2),
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Color.Transparent,
                    ForeColor = Color.Black,
                    BorderStyle = BorderStyle.None
                };
                this.Controls.Add(lblRow);
            }

            // ===== ТЕПЛОВАЯ КАРТА =====
            _heatmapPanel = new Panel
            {
                Location = new Point(50, 185),  // ← было 220
                Size = new Size(800, 495),      // ← высоту чуть уменьшили
                BackColor = Color.LightGray,
                BorderStyle = BorderStyle.FixedSingle
            };

            // ===== ЛЕГЕНДА (справа) =====
            _legendPanel = new Panel
            {
                Location = new Point(880, 185),  // ← было 220, теперь как у карты
                Size = new Size(380, 495),       // ← высота как у карты
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            var legendTitle = new Label
            {
                Text = "Легенда (Срок годности)",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(28, 42, 74),
                Location = new Point(15, 15),
                AutoSize = true
            };
            _legendPanel.Controls.Add(legendTitle);

            int legendY = 50;
            AddLegendItem(Color.Green, "Свежий — более 30 дней", legendY); legendY += 35;
            AddLegendItem(Color.Yellow, "Внимание — 7–30 дней", legendY); legendY += 35;
            AddLegendItem(Color.Orange, "Срочно — менее 7 дней", legendY); legendY += 35;
            AddLegendItem(Color.Red, "Просрочен / Истёк", legendY); legendY += 35;
            AddLegendItem(Color.LightGray, "Ячейка свободна", legendY); legendY += 35;
            AddLegendItem(Color.LightBlue, "Срок не отслеживается", legendY);

            // ===== НИЖНЯЯ ПАНЕЛЬ =====
            var bottomPanel = new Panel
            {
                Location = new Point(20, 700),  // ← было 755
                Size = new Size(this.ClientSize.Width - 40, 35),
                BackColor = Color.WhiteSmoke,
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            _lblStats = new Label
            {
                Text = "Занято ячеек: 0 из 48 | Просрочено: 0 | Истекает в течение 7 дней: 0",
                Location = new Point(10, 8),
                AutoSize = true,
                Font = new Font("Segoe UI", 9)
            };
            _btnRefresh = new Button
            {
                Text = "Обновить карту",
                Location = new Point(bottomPanel.Width - 120, 4),
                Size = new Size(110, 28),
                BackColor = Color.FromArgb(40, 120, 200),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9)
            };
            _btnRefresh.Click += (s, e) => LoadHeatmap();
            bottomPanel.Controls.Add(_lblStats);
            bottomPanel.Controls.Add(_btnRefresh);

            // Добавляем всё на форму
            this.Controls.Add(menuStrip);
            this.Controls.Add(titleLabel);
            this.Controls.Add(separator);
            this.Controls.Add(lblModeTitle);
            this.Controls.Add(_rbExpiry);
            this.Controls.Add(_rbMovement);
            this.Controls.Add(lblSectionTitle);
            this.Controls.Add(_heatmapPanel);
            this.Controls.Add(_legendPanel);
            this.Controls.Add(bottomPanel);

            this.Resize += (s, e) =>
            {
                separator.Width = this.ClientSize.Width - 40;
                bottomPanel.Width = this.ClientSize.Width - 40;
                _btnRefresh.Location = new Point(bottomPanel.Width - 120, 4);
            };
        }

        private void AddLegendItem(Color color, string text, int y)
        {
            var colorBox = new Panel
            {
                Location = new Point(15, y),
                Size = new Size(25, 20),
                BackColor = color,
                BorderStyle = BorderStyle.FixedSingle
            };
            var textLabel = new Label
            {
                Text = text,
                Location = new Point(50, y + 2),
                Size = new Size(300, 20),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.Black
            };
            _legendPanel.Controls.Add(colorBox);
            _legendPanel.Controls.Add(textLabel);
        }

        private void LoadHeatmap()
        {
            _heatmapPanel.Controls.Clear();
            _cells = WarehouseHeatmapService.GenerateHeatmap();
            var (rows, cols) = WarehouseHeatmapService.GetGridSize();

            int cellWidth = _heatmapPanel.Width / cols;
            int cellHeight = _heatmapPanel.Height / rows;

            int occupied = 0, expired = 0, expiringSoon = 0;

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    int index = row * cols + col;
                    var cell = index < _cells.Count ? _cells[index] : null;

                    if (cell == null || cell.ProductName == "Свободно")
                    {
                        var emptyBtn = new Button
                        {
                            Location = new Point(col * cellWidth, row * cellHeight),
                            Size = new Size(cellWidth - 2, cellHeight - 2),
                            BackColor = Color.LightGray,
                            FlatStyle = FlatStyle.Flat,
                            Text = "",
                            Tag = null
                        };
                        emptyBtn.FlatAppearance.BorderColor = Color.Gray;
                        _heatmapPanel.Controls.Add(emptyBtn);
                        continue;
                    }

                    occupied++;
                    if (cell.DaysUntilExpiry <= 0 && cell.DaysUntilExpiry != -1) expired++;
                    else if (cell.DaysUntilExpiry <= 7 && cell.DaysUntilExpiry > 0) expiringSoon++;

                    Color heatColor = _currentMode == "expiry" ? cell.HeatColor : GetMovementColor(cell.MovementVelocity);

                    var button = new Button
                    {
                        Location = new Point(col * cellWidth, row * cellHeight),
                        Size = new Size(cellWidth - 2, cellHeight - 2),
                        BackColor = heatColor,
                        FlatStyle = FlatStyle.Flat,
                        Tag = cell,
                        Text = cell.ProductName.Length > 10 ? cell.ProductName.Substring(0, 8) + ".." : cell.ProductName,
                        Font = new Font("Segoe UI", 7, FontStyle.Bold),
                        TextAlign = ContentAlignment.MiddleCenter
                    };
                    button.FlatAppearance.BorderColor = Color.Gray;
                    button.MouseEnter += (s, e) =>
                    {
                        var hoveredCell = (HeatmapCell)((Button)s).Tag;
                        if (hoveredCell != null)
                        {
                            string placementDate = hoveredCell.DaysUntilExpiry >= 0
                                ? DateTime.Today.AddDays(-hoveredCell.DaysUntilExpiry).ToString("dd.MM.yyyy")
                                : "не указана";
                            string expiryDate = hoveredCell.DaysUntilExpiry >= 0
                                ? DateTime.Today.AddDays(hoveredCell.DaysUntilExpiry).ToString("dd.MM.yyyy")
                                : "Бессрочно";
                            string daysOnStock = hoveredCell.DaysUntilExpiry >= 0 ? hoveredCell.DaysUntilExpiry.ToString() : "—";

                            _toolTip.SetToolTip(button,
                                $"Ячейка {GetRowLetter(row + 1)}-{col + 1}\n" +
                                $"Товар: {hoveredCell.ProductName}\n" +
                                $"Размещён: {placementDate}\n" +
                                $"Срок годности: {expiryDate}\n" +
                                $"На складе: {daysOnStock} дней");
                        }
                    };
                    button.Click += (s, e) =>
                    {
                        var clickedCell = (HeatmapCell)((Button)s).Tag;
                        if (clickedCell != null)
                        {
                            string placementDate = clickedCell.DaysUntilExpiry >= 0
                                ? DateTime.Today.AddDays(-clickedCell.DaysUntilExpiry).ToString("dd.MM.yyyy")
                                : "не указана";
                            string expiryDate = clickedCell.DaysUntilExpiry >= 0
                                ? DateTime.Today.AddDays(clickedCell.DaysUntilExpiry).ToString("dd.MM.yyyy")
                                : "Бессрочно";
                            string daysOnStock = clickedCell.DaysUntilExpiry >= 0 ? clickedCell.DaysUntilExpiry.ToString() : "—";

                            MessageBox.Show(
                                $"Ячейка {GetRowLetter(row + 1)}-{col + 1}\n\n" +
                                $"Товар: {clickedCell.ProductName}\n" +
                                $"Размещён: {placementDate}\n" +
                                $"Срок годности: {expiryDate}\n" +
                                $"На складе: {daysOnStock} дней",
                                "Информация о товаре",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                        }
                    };

                    _heatmapPanel.Controls.Add(button);
                }
            }

            int totalCells = rows * cols;
            _lblStats.Text = $"Занято ячеек: {occupied} из {totalCells} | Просрочено: {expired} | Истекает в течение 7 дней: {expiringSoon}";
        }

        private string GetRowLetter(int row)
        {
            return ((char)('A' + row - 1)).ToString();
        }

        private Color GetMovementColor(double velocity)
        {
            if (velocity > 10) return Color.Green;
            if (velocity > 5) return Color.YellowGreen;
            if (velocity > 2) return Color.Yellow;
            if (velocity > 0.5) return Color.Orange;
            return Color.Red;
        }
    }
}