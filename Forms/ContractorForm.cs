using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GreenStock.Data;
using GreenStock.Models;
using GreenStock.Services;

namespace GreenStock.Forms;

public partial class ContractorForm : Form
{
    private Contractor? _currentContractor;

    // Контролы формы
    private Label _lblTitle = null!;
    private Label _lblInn = null!;
    private Label _lblName = null!;
    private Label _lblKpp = null!;
    private Label _lblPhone = null!;
    private Label _lblAddress = null!;
    private TextBox _txtInn = null!;
    private TextBox _txtName = null!;
    private TextBox _txtKpp = null!;
    private TextBox _txtPhone = null!;
    private TextBox _txtAddress = null!;
    private Label _lblStatus = null!;
    private Label _lblCheckDate = null!;
    private Label _lblReason = null!;
    private Label _lblResultMessage = null!;
    private Button _btnCheck = null!;
    private Button _btnSave = null!;
    private Button _btnClear = null!;
    private Button _btnClose = null!;
    private DataGridView _dgvHistory = null!;
    private GroupBox _grpData = null!;
    private GroupBox _grpResult = null!;
    private GroupBox _grpHistory = null!;
    private Panel _separator = null!;

    public event EventHandler<bool>? BlockShipment;

    public ContractorForm()
    {
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        // Настройки формы
        this.Text = "Контрагенты";
        this.Size = new Size(1100, 700);
        this.StartPosition = FormStartPosition.CenterParent;
        this.BackColor = Color.White;
        this.MinimumSize = new Size(900, 600);
        this.Font = new Font("Segoe UI", 10, FontStyle.Regular);

        // ===== МЕНЮ =====
        var menuStrip = new MenuStrip { BackColor = Color.FromArgb(28, 42, 74), ForeColor = Color.White };
        menuStrip.Font = new Font("Segoe UI", 10, FontStyle.Regular);
        menuStrip.Items.Add(new ToolStripMenuItem("Файл") { ForeColor = Color.White });
        menuStrip.Items.Add(new ToolStripMenuItem("Контрагенты") { ForeColor = Color.White });
        menuStrip.Items.Add(new ToolStripMenuItem("Отгрузки") { ForeColor = Color.White });
        menuStrip.Items.Add(new ToolStripMenuItem("Отчеты") { ForeColor = Color.White });
        menuStrip.Items.Add(new ToolStripMenuItem("Помощь") { ForeColor = Color.White });

        // ===== ЗАГОЛОВОК (голубой, жирный) =====
        _lblTitle = new Label
        {
            Text = "Проверка контрагента",
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 120, 200),
            Location = new Point(20, 30),
            AutoSize = true
        };

        // ===== РАЗДЕЛИТЕЛЬ =====
        _separator = new Panel
        {
            Location = new Point(20, 65),
            Size = new Size(this.ClientSize.Width - 40, 2),
            BackColor = Color.FromArgb(180, 180, 180)
        };

        // ===== ГРУППА "Данные контрагента" (жирный заголовок) =====
        _grpData = new GroupBox
        {
            Text = "Данные контрагента",
            Location = new Point(20, 85),
            Size = new Size(500, 220),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.Black
        };

        // ===== ГРУППА "Результат проверки" (жирный заголовок) =====
        _grpResult = new GroupBox
        {
            Text = "Результат проверки",
            Location = new Point(540, 85),
            Size = new Size(520, 220),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.Black
        };

        // ===== ГРУППА "История проверок" (жирный заголовок) =====
        _grpHistory = new GroupBox
        {
            Text = "История проверок",
            Location = new Point(20, 320),
            Size = new Size(this.ClientSize.Width - 40, 260),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.Black,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        // ===== ПОЛЯ ВНУТРИ "Данные контрагента" =====
        int fieldY = 28;
        int rowHeight = 32;
        int labelWidth = 100;
        int fieldWidth = 350;
        int fieldX = 115;

        // ИНН
        _lblInn = new Label
        {
            Text = "ИНН:",
            Location = new Point(15, fieldY),
            Size = new Size(labelWidth, 25),
            Font = new Font("Segoe UI", 10, FontStyle.Regular),
            TextAlign = ContentAlignment.MiddleLeft
        };
        _txtInn = new TextBox
        {
            Location = new Point(fieldX, fieldY),
            Size = new Size(180, 27),
            Font = new Font("Segoe UI", 10, FontStyle.Regular)
        };

        // Кнопка "Проверить по API" (полностью видна)
        _btnCheck = new Button
        {
            Text = "✓ Проверить по API",
            Location = new Point(fieldX + 190, fieldY - 2),
            Size = new Size(155, 30),
            BackColor = Color.FromArgb(40, 120, 200),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };
        _btnCheck.Click += BtnCheck_Click;

        // Наименование
        _lblName = new Label
        {
            Text = "Наименование:",
            Location = new Point(15, fieldY + rowHeight),
            Size = new Size(labelWidth, 25),
            Font = new Font("Segoe UI", 10, FontStyle.Regular),
            TextAlign = ContentAlignment.MiddleLeft
        };
        _txtName = new TextBox
        {
            Location = new Point(fieldX, fieldY + rowHeight),
            Size = new Size(fieldWidth, 27),
            Font = new Font("Segoe UI", 10, FontStyle.Regular)
        };

        // КПП (длиннее, как наименование)
        _lblKpp = new Label
        {
            Text = "КПП:",
            Location = new Point(15, fieldY + rowHeight * 2),
            Size = new Size(labelWidth, 25),
            Font = new Font("Segoe UI", 10, FontStyle.Regular),
            TextAlign = ContentAlignment.MiddleLeft
        };
        _txtKpp = new TextBox
        {
            Location = new Point(fieldX, fieldY + rowHeight * 2),
            Size = new Size(fieldWidth, 27),
            Font = new Font("Segoe UI", 10, FontStyle.Regular)
        };

        // Телефон (длиннее)
        _lblPhone = new Label
        {
            Text = "Телефон:",
            Location = new Point(15, fieldY + rowHeight * 3),
            Size = new Size(labelWidth, 25),
            Font = new Font("Segoe UI", 10, FontStyle.Regular),
            TextAlign = ContentAlignment.MiddleLeft
        };
        _txtPhone = new TextBox
        {
            Location = new Point(fieldX, fieldY + rowHeight * 3),
            Size = new Size(fieldWidth, 27),
            Font = new Font("Segoe UI", 10, FontStyle.Regular)
        };

        // Адрес
        _lblAddress = new Label
        {
            Text = "Адрес:",
            Location = new Point(15, fieldY + rowHeight * 4),
            Size = new Size(labelWidth, 25),
            Font = new Font("Segoe UI", 10, FontStyle.Regular),
            TextAlign = ContentAlignment.MiddleLeft
        };
        _txtAddress = new TextBox
        {
            Location = new Point(fieldX, fieldY + rowHeight * 4),
            Size = new Size(fieldWidth, 27),
            Font = new Font("Segoe UI", 10, FontStyle.Regular)
        };

        _grpData.Controls.AddRange(new Control[]
        {
            _lblInn, _txtInn, _btnCheck,
            _lblName, _txtName,
            _lblKpp, _txtKpp,
            _lblPhone, _txtPhone,
            _lblAddress, _txtAddress
        });

        // ===== ПОЛЯ ВНУТРИ "Результат проверки" =====
        int resultY = 28;

        // Заголовки в результатах (жирные)
        var lblStatusText = new Label
        {
            Text = "Статус проверки:",
            Location = new Point(15, resultY),
            Size = new Size(120, 25),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.Black
        };
        _lblStatus = new Label
        {
            Text = "—",
            Location = new Point(145, resultY),
            Size = new Size(200, 25),
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };

        var lblDateText = new Label
        {
            Text = "Дата проверки:",
            Location = new Point(15, resultY + 40),
            Size = new Size(120, 25),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.Black
        };
        _lblCheckDate = new Label
        {
            Text = "—",
            Location = new Point(145, resultY + 40),
            Size = new Size(200, 25),
            Font = new Font("Segoe UI", 10, FontStyle.Regular)
        };

        var lblReasonText = new Label
        {
            Text = "Причина:",
            Location = new Point(15, resultY + 80),
            Size = new Size(120, 25),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.Black
        };
        _lblReason = new Label
        {
            Text = "—",
            Location = new Point(145, resultY + 80),
            Size = new Size(350, 25),
            Font = new Font("Segoe UI", 10, FontStyle.Regular)
        };

        _lblResultMessage = new Label
        {
            Text = "Контрагент не проверен",
            Location = new Point(15, resultY + 125),
            Size = new Size(480, 30),
            Font = new Font("Segoe UI", 11, FontStyle.Regular),
            ForeColor = Color.Gray
        };

        _grpResult.Controls.AddRange(new Control[]
        {
            lblStatusText, _lblStatus,
            lblDateText, _lblCheckDate,
            lblReasonText, _lblReason,
            _lblResultMessage
        });

        // ===== ТАБЛИЦА ИСТОРИИ (заголовки жирные) =====
        _dgvHistory = new DataGridView
        {
            Location = new Point(10, 28),
            Size = new Size(_grpHistory.Width - 20, _grpHistory.Height - 45),
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            BackgroundColor = Color.White,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            Font = new Font("Segoe UI", 10, FontStyle.Regular),
            RowHeadersVisible = false,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
        };
        _dgvHistory.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(200, 200, 200);
        _dgvHistory.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;
        _dgvHistory.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        _dgvHistory.EnableHeadersVisualStyles = false;

        _grpHistory.Controls.Add(_dgvHistory);

        // ===== КНОПКИ (Сохранить НЕ жирным) =====
        int btnY = this.ClientSize.Height - 65;

        _btnSave = new Button
        {
            Text = "Сохранить",
            Location = new Point(20, btnY),
            Size = new Size(100, 35),
            BackColor = Color.FromArgb(200, 200, 200),
            ForeColor = Color.Black,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 10, FontStyle.Regular),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left
        };
        _btnSave.Click += BtnSave_Click;

        _btnClear = new Button
        {
            Text = "Очистить",
            Location = new Point(130, btnY),
            Size = new Size(100, 35),
            BackColor = Color.FromArgb(200, 200, 200),
            ForeColor = Color.Black,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 10, FontStyle.Regular),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left
        };
        _btnClear.Click += (s, e) => ClearForm();

        _btnClose = new Button
        {
            Text = "Закрыть",
            Location = new Point(this.ClientSize.Width - 120, btnY),
            Size = new Size(100, 35),
            BackColor = Color.FromArgb(200, 200, 200),
            ForeColor = Color.Black,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 10, FontStyle.Regular),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right
        };
        _btnClose.Click += (s, e) => this.Close();

        // Добавляем всё на форму
        this.Controls.Add(menuStrip);
        this.Controls.Add(_lblTitle);
        this.Controls.Add(_separator);
        this.Controls.Add(_grpData);
        this.Controls.Add(_grpResult);
        this.Controls.Add(_grpHistory);
        this.Controls.Add(_btnSave);
        this.Controls.Add(_btnClear);
        this.Controls.Add(_btnClose);

        // Обновляем размеры при изменении окна
        this.Resize += (s, e) =>
        {
            _separator.Width = this.ClientSize.Width - 40;
            _grpHistory.Width = this.ClientSize.Width - 40;
            _dgvHistory.Width = _grpHistory.Width - 20;
            _dgvHistory.Height = _grpHistory.Height - 45;
            _btnClose.Location = new Point(this.ClientSize.Width - 120, this.ClientSize.Height - 65);
        };
    }

    private async void BtnCheck_Click(object? sender, EventArgs e)
    {
        var inn = _txtInn.Text.Trim();

        // Валидация ИНН (10 или 12 цифр)
        if (string.IsNullOrWhiteSpace(inn))
        {
            MessageBox.Show("Введите ИНН", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var cleanInn = new string(inn.Where(char.IsDigit).ToArray());
        if (cleanInn.Length != 10 && cleanInn.Length != 12)
        {
            MessageBox.Show("ИНН должен содержать 10 или 12 цифр", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _btnCheck.Enabled = false;
        _lblResultMessage.Text = "Проверка...";
        _lblResultMessage.ForeColor = Color.Gray;

        try
        {
            var result = await ContractorService.CheckContractorAsync(cleanInn, "admin");

            _txtName.Text = result.Name;
            _txtKpp.Text = result.Kpp;
            _txtAddress.Text = result.Address;
            _lblStatus.Text = result.Status;
            _lblCheckDate.Text = result.CheckDate.ToString("dd.MM.yyyy HH:mm");
            _lblReason.Text = result.Reason ?? "—";

            if (result.Status == "Чист")
            {
                _lblResultMessage.Text = "✓ Контрагент прошёл проверку";
                _lblResultMessage.ForeColor = Color.Green;
                _lblStatus.ForeColor = Color.Green;
                BlockShipment?.Invoke(this, false);
            }
            else if (result.Status == "В ЧС")
            {
                _lblResultMessage.Text = "⚠ Контрагент в чёрном списке! Отгрузка не рекомендуется!";
                _lblResultMessage.ForeColor = Color.Red;
                _lblStatus.ForeColor = Color.Red;
                BlockShipment?.Invoke(this, true);
            }
            else
            {
                _lblResultMessage.Text = "✗ Ошибка проверки";
                _lblResultMessage.ForeColor = Color.Orange;
                _lblStatus.ForeColor = Color.Orange;
            }

            LoadHistory(cleanInn);
        }
        catch (Exception ex)
        {
            _lblResultMessage.Text = $"Сервис проверки недоступен. Попробуйте позже.";
            _lblResultMessage.ForeColor = Color.Red;
        }
        finally
        {
            _btnCheck.Enabled = true;
        }
    }

    private void LoadHistory(string inn)
    {
        using var db = new AppDbContext();
        var contractor = db.Contractors.FirstOrDefault(c => c.Inn == inn);
        if (contractor != null)
        {
            var history = db.ContractorCheckHistories
                .Where(h => h.ContractorId == contractor.Id)
                .OrderByDescending(h => h.CheckDate)
                .Select(h => new
                {
                    Дата = h.CheckDate.ToString("dd.MM.yyyy HH:mm"),
                    ИНН = h.Inn,
                    Статус = h.Status,
                    Причина = h.Reason,
                    Ктопроверил = h.CheckedBy
                }).ToList();

            _dgvHistory.DataSource = history;

            if (_dgvHistory.Columns.Contains("Дата"))
                _dgvHistory.Columns["Дата"]!.HeaderText = "Дата проверки";
            if (_dgvHistory.Columns.Contains("ИНН"))
                _dgvHistory.Columns["ИНН"]!.HeaderText = "ИНН";
            if (_dgvHistory.Columns.Contains("Статус"))
                _dgvHistory.Columns["Статус"]!.HeaderText = "Статус";
            if (_dgvHistory.Columns.Contains("Причина"))
                _dgvHistory.Columns["Причина"]!.HeaderText = "Причина";
            if (_dgvHistory.Columns.Contains("Ктопроверил"))
                _dgvHistory.Columns["Ктопроверил"]!.HeaderText = "Кто проверял";
        }
        else
        {
            _dgvHistory.DataSource = null;
        }
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        using var db = new AppDbContext();
        var inn = _txtInn.Text.Trim();
        if (string.IsNullOrWhiteSpace(inn))
        {
            MessageBox.Show("Введите ИНН", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var cleanInn = new string(inn.Where(char.IsDigit).ToArray());
        var contractor = db.Contractors.FirstOrDefault(c => c.Inn == cleanInn);
        if (contractor != null)
        {
            contractor.Name = _txtName.Text;
            contractor.Kpp = _txtKpp.Text;
            contractor.Phone = _txtPhone.Text;
            contractor.Address = _txtAddress.Text;
            db.SaveChanges();
            MessageBox.Show("Данные сохранены", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
            MessageBox.Show("Сначала выполните проверку контрагента", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ClearForm()
    {
        _txtInn.Clear();
        _txtName.Clear();
        _txtKpp.Clear();
        _txtPhone.Clear();
        _txtAddress.Clear();
        _lblStatus.Text = "—";
        _lblCheckDate.Text = "—";
        _lblReason.Text = "—";
        _lblResultMessage.Text = "Контрагент не проверен";
        _lblResultMessage.ForeColor = Color.Gray;
        _lblStatus.ForeColor = Color.Black;
        _dgvHistory.DataSource = null;
    }
}