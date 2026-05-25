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

    private Panel _headerPanel = null!;
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

        var menuStrip = new MenuStrip { BackColor = Color.FromArgb(28, 42, 74), ForeColor = Color.White };
        menuStrip.Font = new Font("Segoe UI", 10, FontStyle.Regular);
        menuStrip.Items.Add(new ToolStripMenuItem("Файл") { ForeColor = Color.White });
        menuStrip.Items.Add(new ToolStripMenuItem("Контрагенты") { ForeColor = Color.White });
        menuStrip.Items.Add(new ToolStripMenuItem("Отгрузки") { ForeColor = Color.White });
        menuStrip.Items.Add(new ToolStripMenuItem("Отчеты") { ForeColor = Color.White });
        menuStrip.Items.Add(new ToolStripMenuItem("Помощь") { ForeColor = Color.White });

        var checkTitleLabel = new Label
        {
            Text = "Проверка контрагента",
            Font = new Font("Segoe UI", 14, FontStyle.Regular),
            ForeColor = Color.FromArgb(0, 120, 200),
            Location = new Point(20, 30),
            AutoSize = true
        };

        var separator = new Panel
        {
            Location = new Point(20, 60),
            Size = new Size(this.ClientSize.Width - 40, 2),
            BackColor = Color.LightGray,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        int groupWidth = 500;
        int groupHeight = 200;
        int leftX = 20;
        int rightX = 560;
        int topY = 80;

        _grpData = new GroupBox
        {
            Text = "Данные контрагента",
            Location = new Point(leftX, topY),
            Size = new Size(groupWidth, groupHeight),
            Font = new Font("Segoe UI", 10, FontStyle.Regular),
            ForeColor = Color.Gray,
            BackColor = Color.White
        };

        _grpResult = new GroupBox
        {
            Text = "Результат проверки",
            Location = new Point(rightX, topY),
            Size = new Size(groupWidth, groupHeight),
            Font = new Font("Segoe UI", 10, FontStyle.Regular),
            ForeColor = Color.Gray,
            BackColor = Color.White
        };

        _grpHistory = new GroupBox
        {
            Text = "История проверок",
            Location = new Point(20, topY + groupHeight + 20),
            Size = new Size(this.ClientSize.Width - 40, 250),
            Font = new Font("Segoe UI", 10, FontStyle.Regular),
            ForeColor = Color.Gray,
            BackColor = Color.White,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
        };

        int fieldY = 35;
        int rowHeight = 32;
        int labelWidth = 90;
        int fieldWidth = 360;
        int fieldX = 110;

        var lblInn = new Label
        {
            Text = "ИНН:",
            Location = new Point(20, fieldY),
            Size = new Size(labelWidth, 25),
            Font = new Font("Segoe UI", 10, FontStyle.Regular),
            TextAlign = ContentAlignment.MiddleRight
        };
        _txtInn = new TextBox
        {
            Location = new Point(fieldX, fieldY),
            Size = new Size(180, 27),
            Font = new Font("Segoe UI", 10, FontStyle.Regular)
        };

        _btnCheck = new Button
        {
            Text = "Проверить по API",
            Location = new Point(fieldX + 190, fieldY - 2),
            Size = new Size(140, 30),
            BackColor = Color.FromArgb(40, 120, 200),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 10, FontStyle.Regular)
        };
        _btnCheck.Click += BtnCheck_Click;

        var lblName = new Label
        {
            Text = "Наименование:",
            Location = new Point(20, fieldY + rowHeight),
            Size = new Size(labelWidth, 25),
            Font = new Font("Segoe UI", 10, FontStyle.Regular),
            TextAlign = ContentAlignment.MiddleRight
        };
        _txtName = new TextBox
        {
            Location = new Point(fieldX, fieldY + rowHeight),
            Size = new Size(fieldWidth, 27),
            Font = new Font("Segoe UI", 10, FontStyle.Regular)
        };

        var lblKpp = new Label
        {
            Text = "КПП:",
            Location = new Point(20, fieldY + rowHeight * 2),
            Size = new Size(labelWidth, 25),
            Font = new Font("Segoe UI", 10, FontStyle.Regular),
            TextAlign = ContentAlignment.MiddleRight
        };
        _txtKpp = new TextBox
        {
            Location = new Point(fieldX, fieldY + rowHeight * 2),
            Size = new Size(180, 27),
            Font = new Font("Segoe UI", 10, FontStyle.Regular)
        };

        var lblPhone = new Label
        {
            Text = "Телефон:",
            Location = new Point(20, fieldY + rowHeight * 3),
            Size = new Size(labelWidth, 25),
            Font = new Font("Segoe UI", 10, FontStyle.Regular),
            TextAlign = ContentAlignment.MiddleRight
        };
        _txtPhone = new TextBox
        {
            Location = new Point(fieldX, fieldY + rowHeight * 3),
            Size = new Size(180, 27),
            Font = new Font("Segoe UI", 10, FontStyle.Regular)
        };

        var lblAddress = new Label
        {
            Text = "Адрес:",
            Location = new Point(20, fieldY + rowHeight * 4),
            Size = new Size(labelWidth, 25),
            Font = new Font("Segoe UI", 10, FontStyle.Regular),
            TextAlign = ContentAlignment.MiddleRight
        };
        _txtAddress = new TextBox
        {
            Location = new Point(fieldX, fieldY + rowHeight * 4),
            Size = new Size(fieldWidth, 27),
            Font = new Font("Segoe UI", 10, FontStyle.Regular)
        };

        _grpData.Controls.AddRange(new Control[]
        {
            lblInn, _txtInn, _btnCheck,
            lblName, _txtName,
            lblKpp, _txtKpp,
            lblPhone, _txtPhone,
            lblAddress, _txtAddress
        });

        int resultY = 35;

        var lblStatusText = new Label
        {
            Text = "Статус проверки:",
            Location = new Point(20, resultY),
            Size = new Size(120, 25),
            Font = new Font("Segoe UI", 10, FontStyle.Regular)
        };
        _lblStatus = new Label
        {
            Text = "—",
            Location = new Point(150, resultY),
            Size = new Size(200, 25),
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };

        var lblDateText = new Label
        {
            Text = "Дата проверки:",
            Location = new Point(20, resultY + 35),
            Size = new Size(120, 25),
            Font = new Font("Segoe UI", 10, FontStyle.Regular)
        };
        _lblCheckDate = new Label
        {
            Text = "—",
            Location = new Point(150, resultY + 35),
            Size = new Size(200, 25),
            Font = new Font("Segoe UI", 10, FontStyle.Regular)
        };

        var lblReasonText = new Label
        {
            Text = "Причина:",
            Location = new Point(20, resultY + 70),
            Size = new Size(120, 25),
            Font = new Font("Segoe UI", 10, FontStyle.Regular)
        };
        _lblReason = new Label
        {
            Text = "—",
            Location = new Point(150, resultY + 70),
            Size = new Size(300, 25),
            Font = new Font("Segoe UI", 10, FontStyle.Regular)
        };

        _lblResultMessage = new Label
        {
            Text = "Контрагент не проверен",
            Location = new Point(20, resultY + 115),
            Size = new Size(450, 30),
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

        _dgvHistory = new DataGridView
        {
            Location = new Point(10, 30),
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
        _dgvHistory.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(28, 42, 74);
        _dgvHistory.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        _dgvHistory.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        _dgvHistory.EnableHeadersVisualStyles = false;

        _grpHistory.Controls.Add(_dgvHistory);

        int btnY = this.ClientSize.Height - 55;
        _btnSave = new Button
        {
            Text = "Сохранить",
            Location = new Point(20, btnY),
            Size = new Size(100, 35),
            BackColor = Color.FromArgb(28, 42, 74),
            ForeColor = Color.White,
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
            BackColor = Color.FromArgb(100, 100, 100),
            ForeColor = Color.White,
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
            BackColor = Color.FromArgb(200, 50, 50),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 10, FontStyle.Regular),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right
        };
        _btnClose.Click += (s, e) => this.Close();

        this.Controls.Add(menuStrip);
        this.Controls.Add(checkTitleLabel);
        this.Controls.Add(separator);
        this.Controls.Add(_grpData);
        this.Controls.Add(_grpResult);
        this.Controls.Add(_grpHistory);
        this.Controls.Add(_btnSave);
        this.Controls.Add(_btnClear);
        this.Controls.Add(_btnClose);

        this.Resize += (s, e) =>
        {
            separator.Width = this.ClientSize.Width - 40;
            _grpHistory.Width = this.ClientSize.Width - 40;
            _dgvHistory.Width = _grpHistory.Width - 20;
            _dgvHistory.Height = _grpHistory.Height - 45;
            _btnClose.Location = new Point(this.ClientSize.Width - 120, this.ClientSize.Height - 55);
        };
    }

    private async void BtnCheck_Click(object? sender, EventArgs e)
    {
        var inn = _txtInn.Text.Trim();
        if (string.IsNullOrWhiteSpace(inn))
        {
            MessageBox.Show("Введите ИНН", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _btnCheck.Enabled = false;
        _lblResultMessage.Text = "Проверка...";
        _lblResultMessage.ForeColor = Color.Gray;

        try
        {
            var result = await ContractorService.CheckContractorAsync(inn, "admin");

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
            }
            else if (result.Status == "В ЧС")
            {
                _lblResultMessage.Text = "⚠ Контрагент в чёрном списке! Отгрузка не рекомендуется!";
                _lblResultMessage.ForeColor = Color.Red;
                _lblStatus.ForeColor = Color.Red;
            }
            else
            {
                _lblResultMessage.Text = "✗ Ошибка проверки";
                _lblResultMessage.ForeColor = Color.Orange;
                _lblStatus.ForeColor = Color.Orange;
            }

            LoadHistory(inn);
        }
        catch (Exception ex)
        {
            _lblResultMessage.Text = $"Ошибка: {ex.Message}";
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
                    Кто_проверил = h.CheckedBy
                }).ToList();

            _dgvHistory.DataSource = history;
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

        var contractor = db.Contractors.FirstOrDefault(c => c.Inn == inn);
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