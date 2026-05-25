using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GreenStock.Infrastructure;
using GreenStock.Interfaces;
using GreenStock.Logging;
using NLog;

namespace GreenStock.Forms;

public class HistoryForm : Form
{
    private static readonly ILogger _log = AppLogger.For<HistoryForm>();

    private Label _lblShipments = null!;
    private Label _lblItems = null!;
    private Panel _sepShipments = null!;
    private Panel _sepItems = null!;
    private DataGridView _dgvShipments = null!;
    private DataGridView _dgvItems = null!;

    public HistoryForm()
    {
        InitializeComponent();
        LoadShipments();
    }

    private void InitializeComponent()
    {
        BackColor = Color.FromArgb(240, 240, 245);
        ClientSize = new Size(1100, 700);
        MinimumSize = new Size(900, 600);
        StartPosition = FormStartPosition.CenterParent;
        Text = "История отгрузок";

        _lblShipments = new Label
        {
            Text = "Накладные",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            ForeColor = Color.FromArgb(28, 42, 74),
            Location = new Point(20, 15),
            AutoSize = true
        };

        _sepShipments = new Panel
        {
            Location = new Point(20, 40),
            Size = new Size(1060, 2),
            BackColor = Color.FromArgb(28, 42, 74)
        };

        _dgvShipments = new DataGridView
        {
            Location = new Point(20, 50),
            Size = new Size(1060, 280),
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.Fixed3D,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            Font = new Font("Segoe UI", 10),
            RowHeadersVisible = false
        };
        _dgvShipments.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(28, 42, 74);
        _dgvShipments.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        _dgvShipments.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        _dgvShipments.EnableHeadersVisualStyles = false;
        _dgvShipments.SelectionChanged += DgvShipments_SelectionChanged;

        _lblItems = new Label
        {
            Text = "Состав накладной",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            ForeColor = Color.FromArgb(28, 42, 74),
            Location = new Point(20, 350),
            AutoSize = true
        };

        _sepItems = new Panel
        {
            Location = new Point(20, 375),
            Size = new Size(1060, 2),
            BackColor = Color.FromArgb(28, 42, 74)
        };

        _dgvItems = new DataGridView
        {
            Location = new Point(20, 385),
            Size = new Size(1060, 260),
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.Fixed3D,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            Font = new Font("Segoe UI", 10),
            RowHeadersVisible = false
        };
        _dgvItems.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(28, 42, 74);
        _dgvItems.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        _dgvItems.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        _dgvItems.EnableHeadersVisualStyles = false;

        Controls.Add(_lblShipments);
        Controls.Add(_sepShipments);
        Controls.Add(_dgvShipments);
        Controls.Add(_lblItems);
        Controls.Add(_sepItems);
        Controls.Add(_dgvItems);
    }

    private void LoadShipments()
    {
        try
        {
            var historyService = ServiceLocator.GetService<IHistoryService>();
            var shipments = historyService.GetAllShipments();

            _log.Debug("История: загружено {0} накладных", shipments.Count);

            _dgvShipments.DataSource = shipments.Select((s, idx) => new
            {
                N = idx + 1,
                Дата_Время = s.CreatedAt,
                Кто_оформил = s.CreatedBy,
                Получатель = s.Recipient,
                Позиций = s.ItemCount,
                Сумма = $"{s.TotalAmount:N2} ₽",
                Id = s.Id
            }).ToList();

            if (_dgvShipments.Columns.Contains("Id"))
                _dgvShipments.Columns["Id"]!.Visible = false;
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Ошибка загрузки истории отгрузок");
            MessageBox.Show($"Ошибка загрузки:\n{ex.Message}",
                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void DgvShipments_SelectionChanged(object? sender, EventArgs e)
    {
        if (_dgvShipments.CurrentRow == null) return;

        if (_dgvShipments.CurrentRow.Cells["Id"].Value is not Guid shipmentId) return;

        var shipmentNumber = _dgvShipments.CurrentRow.Index + 1;
        _lblItems.Text = $"Состав накладной №{shipmentNumber}";

        try
        {
            var historyService = ServiceLocator.GetService<IHistoryService>();
            var items = historyService.GetShipmentItems(shipmentId);

            _dgvItems.DataSource = items.Select(i => new
            {
                Артикул = i.Article,
                Товар = i.ProductName,
                Количество = i.Quantity,
                Ед_изм = i.Unit,
                Цена = $"{i.Price:N2} ₽",
                Сумма = $"{i.Total:N2} ₽"
            }).ToList();

            if (_dgvItems.Columns.Contains("Ед_изм"))
                _dgvItems.Columns["Ед_изм"]!.HeaderText = "Ед. изм.";
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Ошибка загрузки позиций отгрузки");
            MessageBox.Show($"Ошибка загрузки позиций:\n{ex.Message}",
                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}