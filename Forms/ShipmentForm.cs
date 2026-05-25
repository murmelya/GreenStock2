using GreenStock;
using GreenStock.Data;
using GreenStock.Infrastructure;
using GreenStock.Interfaces;
using GreenStock.Logging;
using GreenStock.Models;
using GreenStock.Services;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace GreenStock.Forms;

public class ShipmentForm : Form
{
    private static readonly ILogger _log = AppLogger.For<ShipmentForm>();

    private readonly User _currentUser;
    private readonly IRepository _repository;
    private readonly ICurrencyService _currencyService;
    private readonly IWeatherLogisticsService _weatherService;

    private class ShipmentRow
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Total => Quantity * Price;
    }

    private readonly List<ShipmentRow> _rows = new();
    private List<Product> _products = new();

    private Label _lblTitle = null!;
    private Panel _separator1 = null!;
    private Panel _separator2 = null!;

    private GroupBox _grpShipmentData = null!;
    private Label _lblCustomer = null!;
    private ComboBox _cmbCustomer = null!;
    private Label _lblShipmentDate = null!;
    private DateTimePicker _dtpShipmentDate = null!;
    private Label _lblDeliveryRegion = null!;
    private ComboBox _cmbDeliveryRegion = null!;
    private Label _lblComment = null!;
    private TextBox _txtComment = null!;
    private DataGridView _dgvItems = null!;

    private GroupBox _grpWeather = null!;
    private Label _lblWeatherRegionValue = null!;
    private Label _lblWeatherDateValue = null!;
    private Label _lblTempMinValue = null!;
    private Label _lblTempMaxValue = null!;
    private Label _lblWeatherIcon = null!;
    private Label _lblWeatherCondition = null!;
    private Panel _weatherWarningPanel = null!;
    private Label _lblWeatherWarning = null!;
    private Label _lblRecommendationTitle = null!;
    private Label _lblRecommendation = null!;
    private Button _btnGetWeather = null!;

    private Label _lblTotalTitle = null!;
    private Label _lblTotalAmount = null!;

    private Button _btnAddProduct = null!;
    private Button _btnDeleteItem = null!;
    private Button _btnConfirm = null!;
    private Button _btnCancel = null!;

    private ComboBox? _cmbProduct;
    private NumericUpDown? _nudQuantity;

    public ShipmentForm(
        User currentUser,
        IRepository repository,
        ICurrencyService currencyService,
        IWeatherLogisticsService weatherService)
    {
        _currentUser = currentUser;
        _repository = repository;
        _currencyService = currencyService;
        _weatherService = weatherService;

        InitializeComponent();
        LoadProductsAsync();
        LoadCustomers();
        _ = UpdateWeatherAsync();
    }

    public ShipmentForm(User currentUser)
        : this(currentUser,
               ServiceLocator.GetService<IRepository>(),
               ServiceLocator.GetService<ICurrencyService>(),
               ServiceLocator.GetService<IWeatherLogisticsService>())
    {
    }

    private void InitializeComponent()
    {
        this.Text = "Новая отгрузка";
        this.Size = new Size(1100, 720);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.BackColor = Color.White;

        _lblTitle = new Label
        {
            Text = "Новая отгрузка",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 120, 200),
            Location = new Point(20, 15),
            AutoSize = true
        };

        _separator1 = new Panel
        {
            Location = new Point(20, 50),
            Size = new Size(this.ClientSize.Width - 40, 2),
            BackColor = Color.LightGray,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        _grpShipmentData = new GroupBox
        {
            Text = "Данные отгрузки",
            Location = new Point(20, 70),
            Size = new Size(520, 450),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.FromArgb(28, 42, 74),
            BackColor = Color.White
        };

        int leftY = 35;
        int rowHeight = 35;
        int fieldWidth = 350;
        int fieldX = 140;

        _lblCustomer = new Label
        {
            Text = "Покупатель:",
            Location = new Point(15, leftY),
            Size = new Size(100, 25),
            Font = new Font("Segoe UI", 10, FontStyle.Regular),
            TextAlign = ContentAlignment.MiddleLeft
        };
        _cmbCustomer = new ComboBox
        {
            Location = new Point(fieldX, leftY),
            Size = new Size(fieldWidth, 27),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 10, FontStyle.Regular)
        };

        _lblShipmentDate = new Label
        {
            Text = "Дата отгрузки:",
            Location = new Point(15, leftY + rowHeight),
            Size = new Size(100, 25),
            Font = new Font("Segoe UI", 10, FontStyle.Regular),
            TextAlign = ContentAlignment.MiddleLeft
        };
        _dtpShipmentDate = new DateTimePicker
        {
            Location = new Point(fieldX, leftY + rowHeight),
            Size = new Size(fieldWidth, 27),
            Font = new Font("Segoe UI", 10, FontStyle.Regular),
            Value = DateTime.Today.AddDays(2),
            Format = DateTimePickerFormat.Short
        };
        _dtpShipmentDate.ValueChanged += async (s, e) => await UpdateWeatherAsync();

        _lblDeliveryRegion = new Label
        {
            Text = "Регион доставки:",
            Location = new Point(15, leftY + rowHeight * 2),
            Size = new Size(120, 25),
            Font = new Font("Segoe UI", 10, FontStyle.Regular),
            TextAlign = ContentAlignment.MiddleLeft
        };
        _cmbDeliveryRegion = new ComboBox
        {
            Location = new Point(fieldX, leftY + rowHeight * 2),
            Size = new Size(fieldWidth, 27),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 10, FontStyle.Regular)
        };
        _cmbDeliveryRegion.Items.AddRange(_weatherService.GetSupportedRegions().ToArray());
        if (_cmbDeliveryRegion.Items.Count > 0) _cmbDeliveryRegion.SelectedIndex = 0;
        _cmbDeliveryRegion.SelectedIndexChanged += async (s, e) => await UpdateWeatherAsync();

        _lblComment = new Label
        {
            Text = "Комментарий:",
            Location = new Point(15, leftY + rowHeight * 3),
            Size = new Size(100, 25),
            Font = new Font("Segoe UI", 10, FontStyle.Regular),
            TextAlign = ContentAlignment.MiddleLeft
        };
        _txtComment = new TextBox
        {
            Location = new Point(fieldX, leftY + rowHeight * 3),
            Size = new Size(fieldWidth, 27),
            Font = new Font("Segoe UI", 10, FontStyle.Regular),
            Multiline = true,
            Height = 60
        };

        _dgvItems = new DataGridView
        {
            Location = new Point(15, leftY + rowHeight * 5 + 20),
            Size = new Size(490, 180),
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.Fixed3D,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            RowHeadersVisible = false
        };
        _dgvItems.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(200, 200, 200);
        _dgvItems.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;
        _dgvItems.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        _dgvItems.EnableHeadersVisualStyles = false;
        _dgvItems.RowsDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Regular);

        _grpShipmentData.Controls.AddRange(new Control[]
        {
            _lblCustomer, _cmbCustomer,
            _lblShipmentDate, _dtpShipmentDate,
            _lblDeliveryRegion, _cmbDeliveryRegion,
            _lblComment, _txtComment,
            _dgvItems
        });

        // ===== ПОГОДА В РЕГИОНЕ =====
        _grpWeather = new GroupBox
        {
            Text = "Погода в регионе",
            Location = new Point(560, 70),
            Size = new Size(500, 450),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.FromArgb(28, 42, 74),
            BackColor = Color.White
        };

        int weatherY = 30;
        int weatherX = 15;

        var lblRegionBold = new Label
        {
            Text = "Регион:",
            Location = new Point(weatherX, weatherY),
            Size = new Size(70, 25),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.Black
        };
        _lblWeatherRegionValue = new Label
        {
            Text = "—",
            Location = new Point(weatherX + 70, weatherY),
            Size = new Size(200, 25),
            Font = new Font("Segoe UI", 10, FontStyle.Regular),
            ForeColor = Color.Black
        };

        var lblDateLight = new Label
        {
            Text = "Дата прогноза:",
            Location = new Point(weatherX, weatherY + 30),
            Size = new Size(95, 20),
            Font = new Font("Segoe UI", 9, FontStyle.Regular),
            ForeColor = Color.Gray
        };
        _lblWeatherDateValue = new Label
        {
            Text = "—",
            Location = new Point(weatherX + 105, weatherY + 30),
            Size = new Size(150, 20),
            Font = new Font("Segoe UI", 9, FontStyle.Regular),
            ForeColor = Color.Gray
        };

        _btnGetWeather = new Button
        {
            Text = "Получить прогноз",
            Location = new Point(weatherX + 280, weatherY - 5),
            Size = new Size(150, 35),
            BackColor = Color.FromArgb(40, 120, 200),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };
        _btnGetWeather.Click += async (s, e) => await UpdateWeatherAsync();

        var weatherSeparator1 = new Panel
        {
            Location = new Point(weatherX, weatherY + 60),
            Size = new Size(470, 1),
            BackColor = Color.LightGray
        };

        // Мин. температура (голубая)
        var lblTempMinTitle = new Label
        {
            Text = "Мин. температура:",
            Location = new Point(weatherX, weatherY + 75),
            Size = new Size(140, 25),
            Font = new Font("Segoe UI", 10, FontStyle.Regular),
            ForeColor = Color.Black
        };
        _lblTempMinValue = new Label
        {
            Text = "—",
            Location = new Point(weatherX + 140, weatherY + 75),
            Size = new Size(100, 25),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 100, 200)
        };

        // Макс. температура (голубая)
        var lblTempMaxTitle = new Label
        {
            Text = "Макс. температура:",
            Location = new Point(weatherX, weatherY + 105),
            Size = new Size(140, 25),
            Font = new Font("Segoe UI", 10, FontStyle.Regular),
            ForeColor = Color.Black
        };
        _lblTempMaxValue = new Label
        {
            Text = "—",
            Location = new Point(weatherX + 140, weatherY + 105),
            Size = new Size(100, 25),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 100, 200)
        };

        _lblWeatherIcon = new Label
        {
            Text = "🌤️",
            Location = new Point(weatherX, weatherY + 140),
            Size = new Size(40, 40),
            Font = new Font("Segoe UI", 28, FontStyle.Regular),
            TextAlign = ContentAlignment.MiddleCenter
        };

        var lblWeatherText = new Label
        {
            Text = "Погода",
            Location = new Point(weatherX + 50, weatherY + 150),
            Size = new Size(65, 25),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.Black
        };

        _lblWeatherCondition = new Label
        {
            Text = "—",
            Location = new Point(weatherX + 115, weatherY + 150),
            Size = new Size(150, 25),
            Font = new Font("Segoe UI", 10, FontStyle.Regular),
            ForeColor = Color.Gray
        };

        var weatherSeparator2 = new Panel
        {
            Location = new Point(weatherX, weatherY + 190),
            Size = new Size(470, 1),
            BackColor = Color.LightGray
        };

        _weatherWarningPanel = new Panel
        {
            Location = new Point(weatherX, weatherY + 205),
            Size = new Size(470, 45),
            BackColor = Color.FromArgb(240, 240, 240),
            BorderStyle = BorderStyle.FixedSingle
        };

        _lblWeatherWarning = new Label
        {
            Text = "",
            Location = new Point(10, 12),
            Size = new Size(450, 25),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.Black,
            TextAlign = ContentAlignment.MiddleCenter
        };
        _weatherWarningPanel.Controls.Add(_lblWeatherWarning);

        _lblRecommendationTitle = new Label
        {
            Text = "Рекомендация:",
            Location = new Point(weatherX, weatherY + 265),
            Size = new Size(120, 25),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.Black
        };

        _lblRecommendation = new Label
        {
            Text = "",
            Location = new Point(weatherX + 115, weatherY + 265),
            Size = new Size(340, 25),
            Font = new Font("Segoe UI", 10, FontStyle.Regular),
            ForeColor = Color.Gray
        };

        _grpWeather.Controls.AddRange(new Control[]
        {
            lblRegionBold, _lblWeatherRegionValue,
            lblDateLight, _lblWeatherDateValue,
            _btnGetWeather,
            weatherSeparator1,
            lblTempMinTitle, _lblTempMinValue,
            lblTempMaxTitle, _lblTempMaxValue,
            _lblWeatherIcon, lblWeatherText, _lblWeatherCondition,
            weatherSeparator2,
            _weatherWarningPanel,
            _lblRecommendationTitle, _lblRecommendation
        });

        _separator2 = new Panel
        {
            Location = new Point(20, 540),
            Size = new Size(this.ClientSize.Width - 40, 2),
            BackColor = Color.LightGray,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        _lblTotalTitle = new Label
        {
            Text = "Итого по отгрузке:",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            ForeColor = Color.FromArgb(28, 42, 74),
            Location = new Point(20, 560),
            AutoSize = true
        };
        _lblTotalAmount = new Label
        {
            Text = "0 ₽",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 150, 0),
            Location = new Point(200, 558),
            AutoSize = true
        };

        // ===== КНОПКИ =====
        // Добавить (серая)
        _btnAddProduct = new Button
        {
            Text = "Добавить",
            Location = new Point(20, 600),
            Size = new Size(100, 38),
            BackColor = Color.FromArgb(180, 180, 180),
            ForeColor = Color.Black,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };
        _btnAddProduct.Click += BtnAddProduct_Click;

        // Удалить (серая)
        _btnDeleteItem = new Button
        {
            Text = "Удалить",
            Location = new Point(130, 600),
            Size = new Size(100, 38),
            BackColor = Color.FromArgb(180, 180, 180),
            ForeColor = Color.Black,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };
        _btnDeleteItem.Click += BtnDeleteItem_Click;

        // Подтвердить (голубая)
        _btnConfirm = new Button
        {
            Text = "Подтвердить",
            Location = new Point(this.ClientSize.Width - 280, 600),
            Size = new Size(130, 38),
            BackColor = Color.FromArgb(40, 120, 200),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right
        };
        _btnConfirm.Click += BtnConfirm_Click;

        // Отмена (серая)
        _btnCancel = new Button
        {
            Text = "Отмена",
            Location = new Point(this.ClientSize.Width - 140, 600),
            Size = new Size(110, 38),
            BackColor = Color.FromArgb(180, 180, 180),
            ForeColor = Color.Black,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 10, FontStyle.Regular),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right
        };
        _btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        this.Controls.Add(_lblTitle);
        this.Controls.Add(_separator1);
        this.Controls.Add(_grpShipmentData);
        this.Controls.Add(_grpWeather);
        this.Controls.Add(_separator2);
        this.Controls.Add(_lblTotalTitle);
        this.Controls.Add(_lblTotalAmount);
        this.Controls.Add(_btnAddProduct);
        this.Controls.Add(_btnDeleteItem);
        this.Controls.Add(_btnConfirm);
        this.Controls.Add(_btnCancel);

        this.Resize += (s, e) =>
        {
            int width = this.ClientSize.Width - 40;
            _separator1.Width = width;
            _separator2.Width = width;
            _grpWeather.Width = width - 540;
            _btnConfirm.Location = new Point(this.ClientSize.Width - 280, 600);
            _btnCancel.Location = new Point(this.ClientSize.Width - 140, 600);
        };
    }

    private async Task UpdateWeatherAsync()
    {
        var region = _cmbDeliveryRegion.SelectedItem?.ToString();
        if (string.IsNullOrEmpty(region)) return;

        var deliveryDate = _dtpShipmentDate.Value;
        _btnGetWeather.Enabled = false;

        try
        {
            var forecast = await _weatherService.GetForecastAsync(region, deliveryDate);

            if (forecast == null)
            {
                _lblWeatherWarning.Text = "Данные о погоде недоступны";
                _weatherWarningPanel.BackColor = Color.FromArgb(255, 220, 220);
                _lblRecommendation.Text = "";
                return;
            }

            _lblWeatherRegionValue.Text = region;
            _lblWeatherDateValue.Text = deliveryDate.ToString("dd.MM.yyyy");
            _lblTempMinValue.Text = $"{forecast.TemperatureC - 5}°C";
            _lblTempMaxValue.Text = $"{forecast.TemperatureC + 5}°C";
            _lblWeatherCondition.Text = forecast.Condition ?? "—";

            if (forecast.TemperatureC < 0)
                _lblWeatherIcon.Text = "❄️";
            else if (forecast.TemperatureC > 25)
                _lblWeatherIcon.Text = "☀️";
            else
                _lblWeatherIcon.Text = "🌤️";

            if (forecast.TemperatureC <= -15)
            {
                _lblWeatherWarning.Text = "⚠ Требуется термоконтейнер!";
                _weatherWarningPanel.BackColor = Color.FromArgb(255, 240, 150);
                _lblRecommendation.Text = "оформить страховку груза";
                _lblRecommendation.ForeColor = Color.FromArgb(200, 80, 0);
            }
            else if (forecast.TemperatureC >= 35)
            {
                _lblWeatherWarning.Text = "⚠ Рекомендуется страховка груза!";
                _weatherWarningPanel.BackColor = Color.FromArgb(255, 200, 100);
                _lblRecommendation.Text = "оформить страховку груза";
                _lblRecommendation.ForeColor = Color.FromArgb(200, 80, 0);
            }
            else
            {
                _lblWeatherWarning.Text = "";
                _weatherWarningPanel.BackColor = Color.FromArgb(240, 240, 240);
                _lblRecommendation.Text = "";
            }
        }
        catch (Exception)
        {
            _lblWeatherWarning.Text = "Данные о погоде недоступны";
            _weatherWarningPanel.BackColor = Color.FromArgb(255, 220, 220);
            _lblRecommendation.Text = "";
        }
        finally
        {
            _btnGetWeather.Enabled = true;
        }
    }

    private async void LoadProductsAsync()
    {
        var products = await _repository.GetProductsAsync();
        var today = DateOnly.FromDateTime(DateTime.Today);
        _products = products
            .Where(p => p.Stock > 0 && (p.ExpiryDate == null || p.ExpiryDate >= today))
            .OrderBy(p => p.Name)
            .ToList();
    }

    private void LoadCustomers()
    {
        _cmbCustomer.Items.Clear();
        _cmbCustomer.Items.Add("ООО Ромашка");
        _cmbCustomer.Items.Add("ИП Иванов");
        _cmbCustomer.Items.Add("ООО Лютик");
        if (_cmbCustomer.Items.Count > 0) _cmbCustomer.SelectedIndex = 0;
    }

    private Product? SelectedProduct()
    {
        if (_cmbProduct == null || _cmbProduct.SelectedIndex < 0) return null;
        return _products[_cmbProduct.SelectedIndex];
    }

    private void BtnAddProduct_Click(object? sender, EventArgs e)
    {
        using var dialog = new Form();
        dialog.Text = "Выбор товара";
        dialog.Size = new Size(400, 300);
        dialog.StartPosition = FormStartPosition.CenterParent;

        var cmb = new ComboBox { Location = new Point(10, 10), Size = new Size(350, 27), DropDownStyle = ComboBoxStyle.DropDownList };
        var nud = new NumericUpDown { Location = new Point(10, 50), Size = new Size(100, 27), Minimum = 1, Maximum = 99999, Value = 1 };
        var btn = new Button { Text = "Добавить", Location = new Point(120, 48), Size = new Size(100, 30), DialogResult = DialogResult.OK };

        foreach (var p in _products)
            cmb.Items.Add($"{p.Article} — {p.Name}");
        if (cmb.Items.Count > 0) cmb.SelectedIndex = 0;

        dialog.Controls.Add(cmb);
        dialog.Controls.Add(nud);
        dialog.Controls.Add(btn);

        if (dialog.ShowDialog() == DialogResult.OK && cmb.SelectedIndex >= 0)
        {
            var product = _products[cmb.SelectedIndex];
            var qty = (int)nud.Value;

            if (qty > product.Stock)
            {
                MessageBox.Show($"Недостаточно товара: {product.Name}. Доступно: {product.Stock}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal price = product.SellingPrice > 0 ? product.SellingPrice : product.PurchasePrice;

            var existing = _rows.FirstOrDefault(r => r.ProductId == product.Id);
            if (existing != null)
                existing.Quantity += qty;
            else
            {
                _rows.Add(new ShipmentRow
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Unit = product.Unit,
                    Quantity = qty,
                    Price = price
                });
            }

            RefreshGrid();
            UpdateTotal();
        }
    }

    private void BtnDeleteItem_Click(object? sender, EventArgs e)
    {
        if (_dgvItems.CurrentRow == null) return;
        var index = _dgvItems.CurrentRow.Index;
        if (index >= 0 && index < _rows.Count)
        {
            _rows.RemoveAt(index);
            RefreshGrid();
            UpdateTotal();
        }
    }

    private void RefreshGrid()
    {
        _dgvItems.DataSource = null;
        _dgvItems.DataSource = _rows.Select(r => new
        {
            Товар = r.ProductName,
            Кол_во = r.Quantity,
            Ед = r.Unit,
            Цена = $"{r.Price:N2} ₽",
            Сумма = $"{r.Total:N2} ₽"
        }).ToList();
        if (_dgvItems.Columns.Contains("Товар"))
            _dgvItems.Columns["Товар"]!.HeaderText = "Товар";
        if (_dgvItems.Columns.Contains("Кол_во"))
            _dgvItems.Columns["Кол_во"]!.HeaderText = "Кол-во";
        if (_dgvItems.Columns.Contains("Ед"))
            _dgvItems.Columns["Ед"]!.HeaderText = "Ед. изм.";
        if (_dgvItems.Columns.Contains("Цена"))
            _dgvItems.Columns["Цена"]!.HeaderText = "Цена";
        if (_dgvItems.Columns.Contains("Сумма"))
            _dgvItems.Columns["Сумма"]!.HeaderText = "Сумма";

        
    }

    private void UpdateTotal()
    {
        var total = _rows.Sum(r => r.Total);
        _lblTotalAmount.Text = $"{total:N2} ₽";
    }

    private void UpdateConfirmButton()
    {
        bool hasItems = _rows.Count > 0;
        bool hasRecipient = !string.IsNullOrWhiteSpace(_cmbCustomer.Text);
        _btnConfirm.Enabled = hasItems && hasRecipient;
        _btnConfirm.BackColor = _btnConfirm.Enabled ? Color.FromArgb(40, 120, 200) : Color.FromArgb(180, 180, 180);
    }

    private async void BtnConfirm_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_cmbCustomer.Text))
        {
            MessageBox.Show("Выберите покупателя", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_rows.Count == 0)
        {
            MessageBox.Show("Добавьте хотя бы одну позицию", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            foreach (var row in _rows)
            {
                var product = await _repository.GetProductByIdAsync(row.ProductId);
                if (product == null || row.Quantity > product.Stock)
                {
                    MessageBox.Show($"Недостаточно товара: {row.ProductName}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            var shipment = new Shipment
            {
                Id = Guid.NewGuid(),
                CreatedBy = _currentUser.Id,
                CreatedAt = DateTime.UtcNow,
                Recipient = _cmbCustomer.Text,
                Items = new List<ShipmentItem>()
            };

            foreach (var row in _rows)
            {
                var product = await _repository.GetProductByIdAsync(row.ProductId);
                decimal salePrice = product!.SellingPrice > 0 ? product.SellingPrice : product.PurchasePrice;

                shipment.Items.Add(new ShipmentItem
                {
                    Id = Guid.NewGuid(),
                    ShipmentId = shipment.Id,
                    ProductId = row.ProductId,
                    Quantity = row.Quantity,
                    Price = salePrice
                });
            }

            await _repository.AddShipmentAsync(shipment);

            MessageBox.Show("Отгрузка успешно оформлена!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Ошибка при оформлении отгрузки");
            MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}