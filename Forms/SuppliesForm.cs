using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using GreenStock.Data;
using GreenStock.Services;
using Npgsql;

namespace GreenStock;

public class SuppliesForm : Form
{
    private Label _lblProduct = null!;
    private ComboBox _cmbProduct = null!;
    private Label _lblQty = null!;
    private NumericUpDown _nudQty = null!;
    private Label _lblPrice = null!;
    private NumericUpDown _nudPrice = null!;
    private Label _lblExpiry = null!;
    private DateTimePicker _dtpExpiry = null!;
    private Button _btnAddManual = null!;
    private Button _btnClear = null!;
    private Button _btnImport = null!;
    private Button _btnSave = null!;
    private Button _btnCancel = null!;
    private DataGridView _dgvSupplies = null!;
    private Label _lblTotal = null!;
    private Label _lblTotalQty = null!;
    private Label _lblTitle = null!;
    private Panel _separator = null!;

    private readonly string _connStr;
    private readonly Guid _userId;
    private List<(Guid ProductId, string ProductName, int Qty, decimal Price, DateTime Expiry)> _supplies = new();

    public SuppliesForm(string connStr, Guid userId)
    {
        _connStr = connStr;
        _userId = userId;
        InitializeComponent();
        LoadProducts();
    }

    private void InitializeComponent()
    {
        Text = "Поставка";
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(950, 580);
        BackColor = Color.White;
        Font = new Font("Segoe UI", 10);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = false;

        const int leftX = 20;
        const int rightX = 350;
        const int fieldWidth = 280;
        const int rowHeight = 35;
        int y = 20;

        // ===== ЛЕВАЯ ЧАСТЬ =====
        // Товар

        _lblProduct = new Label
        {
            Text = "Товар:",
            Location = new Point(leftX, y),
            Size = new Size(100, 25),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 10)
        };
        _cmbProduct = new ComboBox
        {
            Location = new Point(leftX, y + 25),
            Size = new Size(fieldWidth, 27),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 10)
        };

        y += 70;

        // Количество
        _lblQty = new Label
        {
            Text = "Количество:",
            Location = new Point(leftX, y),
            Size = new Size(100, 25),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 10)
        };
        _nudQty = new NumericUpDown
        {
            Location = new Point(leftX, y + 25),
            Size = new Size(120, 27),
            Minimum = 1,
            Maximum = 99999,
            Value = 1,
            Font = new Font("Segoe UI", 10)
        };

        y += 60;

        // Цена закупки
        _lblPrice = new Label
        {
            Text = "Цена закупки:",
            Location = new Point(leftX, y),
            Size = new Size(100, 25),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 10)
        };
        _nudPrice = new NumericUpDown
        {
            Location = new Point(leftX, y + 25),
            Size = new Size(120, 27),
            Minimum = 0,
            Maximum = 999999,
            DecimalPlaces = 2,
            Value = 0,
            Font = new Font("Segoe UI", 10)
        };

        y += 60;

        // Срок годности
        _lblExpiry = new Label
        {
            Text = "Срок годности:",
            Location = new Point(leftX, y),
            Size = new Size(110, 25),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 10)
        };
        _dtpExpiry = new DateTimePicker
        {
            Location = new Point(leftX, y + 25),
            Size = new Size(150, 27),
            Format = DateTimePickerFormat.Short,
            Value = DateTime.Now.AddMonths(12),
            Font = new Font("Segoe UI", 10)
        };

        y += 70;

        // Кнопка "Добавить в поставку" (голубая)
        _btnAddManual = new Button
        {
            Text = "Добавить в поставку",
            Location = new Point(leftX, y),
            Size = new Size(180, 35),
            BackColor = Color.FromArgb(40, 120, 200),
            ForeColor = Color.Black,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };
        _btnAddManual.FlatAppearance.BorderSize = 0;
        _btnAddManual.Click += BtnAddManual_Click;

        // Кнопка "Очистить" (серая)
        _btnClear = new Button
        {
            Text = "Очистить",
            Location = new Point(leftX + 190, y),
            Size = new Size(100, 35),
            BackColor = Color.FromArgb(128, 128, 128),
            ForeColor = Color.Black,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 10)
        };
        _btnClear.FlatAppearance.BorderSize = 0;
        _btnClear.Click += (s, e) => { _supplies.Clear(); RefreshGrid(); };

        // ===== ПРАВАЯ ЧАСТЬ =====
        _lblTitle = new Label
        {
            Text = "Товары в поставке:",
            Location = new Point(rightX, 20),
            AutoSize = true,
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            ForeColor = Color.FromArgb(28, 42, 74)
        };

        // Таблица с кнопкой удаления
        _dgvSupplies = new DataGridView
        {
            Location = new Point(rightX, 50),
            Size = new Size(560, 280),
            ReadOnly = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            Font = new Font("Segoe UI", 10),
            RowHeadersVisible = false,
            AllowUserToResizeRows = false
        };
        _dgvSupplies.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(28, 42, 74);
        _dgvSupplies.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        _dgvSupplies.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        _dgvSupplies.EnableHeadersVisualStyles = false;

        // Колонки
        _dgvSupplies.Columns.Add("ProductId", "ID");
        _dgvSupplies.Columns.Add("Товар", "Товар");
        _dgvSupplies.Columns.Add("Количество", "Количество");
        _dgvSupplies.Columns.Add("Цена", "Цена");
        _dgvSupplies.Columns.Add("Сумма", "Сумма");
        _dgvSupplies.Columns.Add("Удалить", "Удалить");

        _dgvSupplies.Columns["ProductId"]!.Visible = false;
        _dgvSupplies.Columns["Товар"]!.Width = 170;
        _dgvSupplies.Columns["Количество"]!.Width = 100;
        _dgvSupplies.Columns["Цена"]!.Width = 100;
        _dgvSupplies.Columns["Сумма"]!.Width = 90;
        _dgvSupplies.Columns["Удалить"]!.Width = 75;

        // Кнопка удаления в каждой строке
        _dgvSupplies.CellContentClick += DgvSupplies_CellContentClick;

        // Итого позиций
        _lblTotal = new Label
        {
            Text = "Итого позиций: 0",
            Location = new Point(rightX, 345),
            Size = new Size(200, 25),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.FromArgb(28, 42, 74)
        };

        // Общее количество
        _lblTotalQty = new Label
        {
            Text = "Общее количество: 0",
            Location = new Point(rightX, 370),
            Size = new Size(200, 25),
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.FromArgb(64, 64, 64)
        };

        // Разделитель
        _separator = new Panel
        {
            Location = new Point(12, 420),
            Size = new Size(this.ClientSize.Width - 24, 2),
            BackColor = Color.LightGray,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        // Кнопка "Импортировать из файла" (слева, серая)
        _btnImport = new Button
        {
            Text = "Импортировать из файла",
            Location = new Point(12, 440),
            Size = new Size(180, 35),
            BackColor = Color.FromArgb(128, 128, 128),
            ForeColor = Color.Black,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 10),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left
        };
        _btnImport.FlatAppearance.BorderSize = 0;
        _btnImport.Click += BtnImport_Click;

        // Кнопка "Сохранить поставку" (голубая)
        _btnSave = new Button
        {
            Text = "Сохранить поставку",
            Location = new Point(this.ClientSize.Width - 320, 440),
            Size = new Size(160, 35),
            BackColor = Color.FromArgb(40, 120, 200),
            ForeColor = Color.Black,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right
        };
        _btnSave.FlatAppearance.BorderSize = 0;
        _btnSave.Click += BtnSave_Click;

        // Кнопка "Отменить" (серая)
        _btnCancel = new Button
        {
            Text = "Отменить",
            Location = new Point(this.ClientSize.Width - 140, 440),
            Size = new Size(120, 35),
            BackColor = Color.FromArgb(128, 128, 128),
            ForeColor = Color.Black,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 10),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right
        };
        _btnCancel.FlatAppearance.BorderSize = 0;
        _btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
        // Добавляем всё на форму
        Controls.AddRange(new Control[]
        {
            _lblProduct, _cmbProduct,
            _lblQty, _nudQty,
            _lblPrice, _nudPrice,
            _lblExpiry, _dtpExpiry,
            _btnAddManual, _btnClear,
            _lblTitle, _dgvSupplies,
            _lblTotal, _lblTotalQty, _separator,
            _btnImport, _btnSave, _btnCancel
        });
    }

    private void LoadProducts()
    {
        using var conn = new NpgsqlConnection(_connStr);
        conn.Open();
        var da = new NpgsqlDataAdapter("SELECT id, name FROM products ORDER BY name", conn);
        var dt = new DataTable();
        da.Fill(dt);
        _cmbProduct.DisplayMember = "name";
        _cmbProduct.ValueMember = "id";
        _cmbProduct.DataSource = dt;
    }

    private void BtnAddManual_Click(object? sender, EventArgs e)
    {
        object? raw = _cmbProduct.SelectedValue;
        Guid productId;
        if (raw is Guid g) productId = g;
        else if (raw is string s && Guid.TryParse(s, out g)) productId = g;
        else return;
        var productName = _cmbProduct.Text;
        int qty = (int)_nudQty.Value;
        decimal price = _nudPrice.Value;
        DateTime expiry = _dtpExpiry.Value.Date;

        if (qty <= 0 || price <= 0)
        {
            MessageBox.Show("Проверьте количество и цену.", Strings.Warning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _supplies.Add((productId, productName, qty, price, expiry));
        RefreshGrid();

        _nudQty.Value = 1;
        _nudPrice.Value = 0;
        _dtpExpiry.Value = DateTime.Now.AddMonths(12);
    }

    private void DgvSupplies_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;
        if (_dgvSupplies.Columns[e.ColumnIndex].Name == "Удалить")
        {
            if (e.RowIndex < _supplies.Count)
            {
                _supplies.RemoveAt(e.RowIndex);
                RefreshGrid();
            }
        }
    }

    private void RefreshGrid()
    {
        _dgvSupplies.Rows.Clear();

        int totalPositions = _supplies.Count;
        int totalQuantity = 0;
        decimal totalSum = 0;

        foreach (var (_, name, qty, price, _) in _supplies)
        {
            decimal itemSum = qty * price;
            totalQuantity += qty;
            totalSum += itemSum;

            _dgvSupplies.Rows.Add("", name, qty, $"{price:N2} ₽", $"{itemSum:N2} ₽", "❌");
        }

        _lblTotal.Text = $"Итого позиций: {totalPositions}";
        _lblTotalQty.Text = $"Общее количество: {totalQuantity}";
    }

    private void BtnImport_Click(object? sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog { Filter = "CSV (*.csv)|*.csv|All files (*.*)|*.*" };
        if (ofd.ShowDialog() != DialogResult.OK) return;

        try
        {
            var lines = File.ReadAllLines(ofd.FileName);
            int imported = 0;

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line.StartsWith("productId") || line.StartsWith("article")) continue;

                var parts = line.Split(';');
                if (parts.Length < 4) continue;

                string productIdOrArticle = parts[0].Trim();
                Guid productId = Guid.Empty;

                if (Guid.TryParse(productIdOrArticle, out Guid parsedGuid))
                {
                    productId = parsedGuid;
                }
                else
                {
                    using var conn2 = new NpgsqlConnection(_connStr);
                    conn2.Open();
                    using var cmd = new NpgsqlCommand("SELECT id FROM products WHERE article = @article LIMIT 1", conn2);
                    cmd.Parameters.AddWithValue("@article", productIdOrArticle);
                    var o = cmd.ExecuteScalar();
                    if (o != null && Guid.TryParse(o.ToString()!, out Guid foundId))
                        productId = foundId;
                }

                if (productId == Guid.Empty) continue;

                if (!int.TryParse(parts[1].Trim(), out int qty)) continue;
                if (!decimal.TryParse(parts[2].Trim(), System.Globalization.CultureInfo.InvariantCulture, out decimal price)) continue;
                if (!DateTime.TryParse(parts[3].Trim(), System.Globalization.CultureInfo.InvariantCulture, out DateTime expiry)) continue;

                string productName = $"Product {productId}";
                using (var conn2 = new NpgsqlConnection(_connStr))
                {
                    conn2.Open();
                    using var cmd = new NpgsqlCommand("SELECT name FROM products WHERE id = @id", conn2);
                    cmd.Parameters.AddWithValue("@id", productId);
                    var o = cmd.ExecuteScalar();
                    if (o != null) productName = o.ToString()!;
                }

                _supplies.Add((productId, productName, qty, price, expiry.Date));
                imported++;
            }

            RefreshGrid();
            MessageBox.Show($"Импортировано позиций: {imported}", Strings.Done, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка импорта: {ex.Message}", Strings.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        if (_supplies.Count == 0)
        {
            MessageBox.Show("Добавьте хотя бы одну позицию.", Strings.Warning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var conn = new NpgsqlConnection(_connStr);
        conn.Open();
        using var tx = conn.BeginTransaction();

        try
        {
            foreach (var (productId, _, qty, price, expiry) in _supplies)
            {
                var cmdUpdate = new NpgsqlCommand(
                    "UPDATE products SET stock = stock + @qty, purchase_price = @price, expiry_date = @exp WHERE id = @pid", conn, tx);
                cmdUpdate.Parameters.AddWithValue("@qty", qty);
                cmdUpdate.Parameters.AddWithValue("@price", price);
                cmdUpdate.Parameters.AddWithValue("@exp", expiry);
                cmdUpdate.Parameters.AddWithValue("@pid", productId);
                cmdUpdate.ExecuteNonQuery();
            }

            tx.Commit();
            MessageBox.Show("Поставка успешно сохранена!", Strings.Done, MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            tx.Rollback();
            MessageBox.Show($"Ошибка: {ex.Message}", Strings.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}