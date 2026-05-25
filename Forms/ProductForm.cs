using GreenStock;
using GreenStock.Data;
using GreenStock.Infrastructure;
using GreenStock.Interfaces;
using GreenStock.Logging;
using GreenStock.Models;
using GreenStock.Services;
using NLog;

namespace GreenStock.Forms;

/// <summary>
/// добавление или редактирования товара.
/// </summary>
public class ProductForm : Form
{
    private static readonly ILogger _log = AppLogger.For<ProductForm>();

    private readonly Product? _existing;
    private readonly ICurrencyService _currencyService;

    private Label         _lblArticle       = null!;
    private Label         _lblName          = null!;
    private Label         _lblCategory      = null!;
    private Label         _lblUnit          = null!;
    private Label         _lblBuyPrice      = null!;
    private Label         _lblSellPrice     = null!;
    private Label         _lblStock         = null!;
    private Label         _lblExpiry        = null!;
    private TextBox       _txtArticle       = null!;
    private TextBox       _txtName          = null!;
    private TextBox       _txtStock         = null!;
    private ComboBox      _cmbCategory      = null!;
    private ComboBox      _cmbUnit          = null!;
    private NumericUpDown _nudBuyPrice      = null!;
    private NumericUpDown _nudSellPrice     = null!;
    private DateTimePicker _dtpExpiry       = null!;
    private CheckBox      _chkNoExpiry      = null!;
    private Label         _lblArticleError  = null!;
    private Label         _lblNameError     = null!;
    private Label         _lblSellPriceHint = null!;
    private Button        _btnSave          = null!;
    private Button        _btnCancel        = null!;

    private ComboBox   _cmbPurchaseCurrency = null!;
    private Label      _lblPurchaseRate =     null!;
    

    public ProductForm(ICurrencyService currencyService, Product? existing)
    {
        _currencyService = currencyService;
        _existing = existing;
        InitializeComponent();
        LoadCategories();
        if (_existing != null) FillFields();
    }
    public ProductForm(Product? existing = null) : this(
        ServiceLocator.GetService<ICurrencyService>(),
        existing)
    {
    }

    private void InitializeComponent()
    {
        var isEdit = _existing != null;

        Text            = isEdit ? Strings.Product_TitleEdit : Strings.Product_TitleAdd;
       
        Size            = new Size(420, 570);
        StartPosition   = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        MinimizeBox     = false;
        BackColor       = Color.FromArgb(240, 240, 245);

        const int labelX = 30;
        const int inputX = 165;
        const int inputW = 185;
        const int rowH   = 48;
        const int startY = 18;

        Label MakeLabel(string text, int row) => new Label
        {
            Text     = text,
            Font     = new Font("Segoe UI", 10),
            Location = new Point(labelX, startY + row * rowH + 4),
            AutoSize = true
        };

        _lblArticle = MakeLabel(Strings.Product_LabelArticle, 0);
        _txtArticle = new TextBox
        {
            Font      = new Font("Segoe UI", 10),
            Location  = new Point(inputX, startY + 0 * rowH),
            Size      = new Size(inputW, 24),
            ReadOnly  = isEdit,
            BackColor = isEdit ? Color.FromArgb(220, 220, 220) : Color.White
        };
        _lblArticleError = new Label
        {
            Text      = Strings.Product_ErrArticleExists,
            Font      = new Font("Segoe UI", 8),
            ForeColor = Color.Red,
            Location  = new Point(inputX, startY + 0 * rowH + 26),
            AutoSize  = true,
            Visible   = false
        };

        _lblName = MakeLabel(Strings.Product_LabelName, 1);
        _txtName = new TextBox
            { Font = new Font("Segoe UI", 10), Location = new Point(inputX, startY + 1 * rowH), Size = new Size(inputW, 24) };
        _lblNameError = new Label
        {
            Text      = Strings.RequiredField,
            Font      = new Font("Segoe UI", 8),
            ForeColor = Color.Red,
            Location  = new Point(inputX, startY + 1 * rowH + 26),
            AutoSize  = true,
            Visible   = false
        };

        _lblCategory = MakeLabel(Strings.Product_LabelCategory, 2);
        _cmbCategory = new ComboBox
            { Font = new Font("Segoe UI", 10), Location = new Point(inputX, startY + 2 * rowH), Size = new Size(inputW, 24), DropDownStyle = ComboBoxStyle.DropDownList };

        _lblUnit = MakeLabel(Strings.Product_LabelUnit, 3);
        _cmbUnit = new ComboBox
            { Font = new Font("Segoe UI", 10), Location = new Point(inputX, startY + 3 * rowH), Size = new Size(100, 24), DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbUnit.Items.AddRange(new[] { "шт", "пак", "кг", "л", "г" });
        _cmbUnit.SelectedIndex = 0;

        _lblBuyPrice = MakeLabel(Strings.Product_LabelPrice, 4);
        _nudBuyPrice = new NumericUpDown
            { Font = new Font("Segoe UI", 10), Location = new Point(inputX, startY + 4 * rowH), Size = new Size(90, 24), Minimum = 0, Maximum = 9999999, DecimalPlaces = 2 };
        var lblRub1 = new Label
            { Text = "₽", Font = new Font("Segoe UI", 10), Location = new Point(inputX + 96, startY + 4 * rowH + 4), AutoSize = true };

        _lblSellPrice = MakeLabel("Цена продажи*:", 5);
        _nudSellPrice = new NumericUpDown
            { Font = new Font("Segoe UI", 10), Location = new Point(inputX, startY + 5 * rowH), Size = new Size(90, 24), Minimum = 0, Maximum = 9999999, DecimalPlaces = 2 };
        var lblRub2 = new Label
            { Text = "₽", Font = new Font("Segoe UI", 10), Location = new Point(inputX + 96, startY + 5 * rowH + 4), AutoSize = true };
        _lblSellPriceHint = new Label
        {
            Text      = "используется для расчёта прибыли",
            Font      = new Font("Segoe UI", 7),
            ForeColor = Color.Gray,
            Location  = new Point(inputX, startY + 5 * rowH + 27),
            AutoSize  = true
        };

        var lblCurrency = new Label
        {
            Text = "Валюта закупки:",
            Font = new Font("Segoe UI", 10),
            Location = new Point(labelX, startY + 8 * rowH + 4),
            AutoSize = true
        };

        _cmbPurchaseCurrency = new ComboBox
        {
            Font = new Font("Segoe UI", 10),
            Location = new Point(inputX, startY + 8 * rowH),
            Size = new Size(80, 24),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cmbPurchaseCurrency.Items.AddRange(new object[] { "RUB", "USD", "EUR" });
        _cmbPurchaseCurrency.SelectedIndex = 0;
        _cmbPurchaseCurrency.SelectedIndexChanged += (s, e) => UpdatePurchaseRateDisplay();

        _lblPurchaseRate = new Label
        {
            Text = "Курс: 1.0000",
            Font = new Font("Segoe UI", 8),
            ForeColor = Color.Gray,
            Location = new Point(inputX + 90, startY + 8 * rowH + 4),
            AutoSize = true
        };
        _lblStock = MakeLabel(Strings.Product_LabelStock, 6);
        _txtStock = new TextBox
        {
            Font      = new Font("Segoe UI", 10),
            Location  = new Point(inputX, startY + 6 * rowH),
            Size      = new Size(60, 24),
            Text      = isEdit ? _existing!.Stock.ToString() : "0",
            ReadOnly  = isEdit,
            BackColor = isEdit ? Color.FromArgb(220, 220, 220) : Color.White
        };
        var lblPcs = new Label
            { Text = "шт.", Font = new Font("Segoe UI", 10), Location = new Point(inputX + 66, startY + 6 * rowH + 4), AutoSize = true };

        _lblExpiry = MakeLabel(Strings.Product_LabelExpiry, 7);
        _dtpExpiry = new DateTimePicker
        {
            Font     = new Font("Segoe UI", 10),
            Location = new Point(inputX, startY + 7 * rowH),
            Size     = new Size(130, 24),
            Format   = DateTimePickerFormat.Short
        };
        _chkNoExpiry = new CheckBox
        {
            Text     = Strings.Product_ChkNoExpiry,
            Font     = new Font("Segoe UI", 10),
            Location = new Point(inputX + 140, startY + 7 * rowH + 3),
            AutoSize = true
        };
        _chkNoExpiry.CheckedChanged += (s, e) => _dtpExpiry.Enabled = !_chkNoExpiry.Checked;

        var lblRequired = new Label
        {
            Text      = Strings.Product_RequiredHint,
            Font      = new Font("Segoe UI", 8),
            ForeColor = Color.Gray,
            Location  = new Point(labelX, startY + 9 * rowH + 30),
            AutoSize  = true
        };

        var btnY = startY + 9 * rowH + 26;
        _btnSave = new Button
        {
            Text      = Strings.Save,
            Font      = new Font("Segoe UI", 10, FontStyle.Bold),
            Location  = new Point(155, btnY),
            Size      = new Size(110, 32),
            BackColor = Color.FromArgb(28, 42, 74),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor    = Cursors.Hand
        };
        _btnSave.FlatAppearance.BorderSize = 0;
        _btnSave.Click += BtnSave_Click;
        AcceptButton    = _btnSave;

        _btnCancel = new Button
        {
            Text      = Strings.Cancel,
            Font      = new Font("Segoe UI", 10),
            Location  = new Point(275, btnY),
            Size      = new Size(100, 32),
            FlatStyle = FlatStyle.Flat,
            Cursor    = Cursors.Hand
        };
        _btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
        CancelButton      = _btnCancel;

        Controls.AddRange(new Control[]
        {
            _lblArticle,   _txtArticle,   _lblArticleError,
            _lblName,      _txtName,      _lblNameError,
            _lblCategory,  _cmbCategory,
            _lblUnit,      _cmbUnit,
            _lblBuyPrice,  _nudBuyPrice,  lblRub1,
            _lblSellPrice, _nudSellPrice, lblRub2, _lblSellPriceHint,
            lblCurrency,   _cmbPurchaseCurrency,   _lblPurchaseRate,
            _lblStock,     _txtStock,     lblPcs,
            _lblExpiry,    _dtpExpiry,    _chkNoExpiry,
            lblRequired,   _btnSave,      _btnCancel
        });
    }

    private void LoadCategories()
    {
        using var db = new AppDbContext();
        var cats = db.Categories.OrderBy(c => c.Name).ToList();
        _cmbCategory.Items.Clear();
        foreach (var c in cats) _cmbCategory.Items.Add(c.Name);
        if (_cmbCategory.Items.Count > 0) _cmbCategory.SelectedIndex = 0;
    }

    private void FillFields()
    {
        _txtArticle.Text   = _existing!.Article;
        _txtName.Text      = _existing.Name;
        _cmbCategory.Text  = _existing.Category?.Name ?? string.Empty;
        _cmbUnit.Text      = _existing.Unit;
        _nudBuyPrice.Value = _existing.PurchasePrice;
        _nudSellPrice.Value = _existing.SellingPrice > 0
            ? _existing.SellingPrice
            : Math.Round(_existing.PurchasePrice * 1.3m, 2); 
        _txtStock.Text     = _existing.Stock.ToString();

        if (_existing.ExpiryDate.HasValue)
            _dtpExpiry.Value = _existing.ExpiryDate.Value.ToDateTime(TimeOnly.MinValue);
        else
            _chkNoExpiry.Checked = true;
        _cmbPurchaseCurrency.SelectedItem = _existing.PurchaseCurrency;

        string currency = _existing.PurchaseCurrency;
        if (currency == "RUB")
        {
            _lblPurchaseRate.Text = "Курс: 1.0000 (базовая валюта)";
        }
        else
        {
            _lblPurchaseRate.Text = $"Курс на момент закупки: 1 {currency} = {_existing.PurchaseRate:F4} RUB";
        }
    }

    private void SetFieldError(TextBox txt, bool hasError)
    {
        txt.BackColor = hasError
            ? Color.FromArgb(255, 220, 220)
            : (txt.ReadOnly ? Color.FromArgb(220, 220, 220) : Color.White);
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        _lblArticleError.Visible = false;
        _lblNameError.Visible    = false;
        SetFieldError(_txtArticle, false);
        SetFieldError(_txtName, false);

        var valid = true;
        if (string.IsNullOrWhiteSpace(_txtArticle.Text))
        {
            SetFieldError(_txtArticle, true);
            _lblArticleError.Text    = Strings.RequiredField;
            _lblArticleError.Visible = true;
            valid = false;
        }
        if (string.IsNullOrWhiteSpace(_txtName.Text))
        {
            SetFieldError(_txtName, true);
            _lblNameError.Visible = true;
            valid = false;
        }
        if (_cmbCategory.SelectedIndex < 0) valid = false;
        if (!valid) return;

        try
        {
            using var db = new AppDbContext();
            var category = db.Categories.FirstOrDefault(c => c.Name == _cmbCategory.SelectedItem!.ToString());
            if (category == null) return;

            var expiry = _chkNoExpiry.Checked
                ? (DateOnly?)null
                : DateOnly.FromDateTime(_dtpExpiry.Value);

            string purchaseCurrency = _cmbPurchaseCurrency.SelectedItem?.ToString() ?? "RUB";
            decimal purchaseRate = 1.0m;

            if (purchaseCurrency == "USD")
                purchaseRate = _currencyService.GetRate(Currency.USD);
            else if (purchaseCurrency == "EUR")
                purchaseRate = _currencyService.GetRate(Currency.EUR);


            if (_existing == null)
            {
                var article = _txtArticle.Text.Trim();
                if (db.Products.Any(p => p.Article == article))
                {
                    _lblArticleError.Text    = Strings.Product_ErrArticleExists;
                    _lblArticleError.Visible = true;
                    SetFieldError(_txtArticle, true);
                    return;
                }

                db.Products.Add(new Product
                {
                    Article       = article,
                    Name          = _txtName.Text.Trim(),
                    CategoryId    = category.Id,
                    Unit          = _cmbUnit.SelectedItem!.ToString()!,
                    PurchasePrice = _nudBuyPrice.Value,
                    SellingPrice  = _nudSellPrice.Value,
                    Stock         = int.TryParse(_txtStock.Text, out var stock) ? stock : 0,
                    ExpiryDate    = expiry,
                    PurchaseCurrency = purchaseCurrency,
                    PurchaseRate = purchaseRate
                });

                

                _log.Info("Добавлен товар: {0}", article);
            }
            else
            {
                var p = db.Products.Find(_existing.Id);
                if (p == null) return;
                p.Name          = _txtName.Text.Trim();
                p.CategoryId    = category.Id;
                p.Unit          = _cmbUnit.SelectedItem!.ToString()!;
                p.PurchasePrice = _nudBuyPrice.Value;
                p.SellingPrice  = _nudSellPrice.Value;
                p.ExpiryDate    = expiry;

                p.PurchaseCurrency = purchaseCurrency;
                p.PurchaseRate = purchaseRate;

                _log.Info("Обновлён товар: {0}", p.Article);
            }

            db.SaveChanges();
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Ошибка сохранения товара");
            MessageBox.Show($"{Strings.Error}: {ex.Message}",
                Strings.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
    private void UpdatePurchaseRateDisplay()
    {
        string currency = _cmbPurchaseCurrency.SelectedItem?.ToString() ?? "RUB";
        if (currency == "RUB")
        {
            _lblPurchaseRate.Text = "Курс: 1.0000 (базовая валюта)";
        }
        else
        {
            //реальный курс
            decimal rate = currency == "USD"
                ? _currencyService.GetRate(Currency.USD)
                : _currencyService.GetRate(Currency.EUR);
            _lblPurchaseRate.Text = $"Курс на сейчас: 1 {currency} = {rate:F4} RUB";
        }
    }
}
