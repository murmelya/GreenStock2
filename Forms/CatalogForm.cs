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

/// <summary>
/// Главная форма каталога товаров.
/// </summary>
public class CatalogForm : Form
{
    private static readonly ILogger _log = AppLogger.For<CatalogForm>();

    private readonly User _currentUser;
    private readonly ICurrencyService _currencyService;

    private readonly IProductRepository? _productRepository;
    private readonly ICategoryRepository? _categoryRepository;
    private readonly IUnitOfWork? _unitOfWork;

    private MenuStrip _menuStrip = null!;
    private ToolStripMenuItem _menuCatalog = null!;
    private ToolStripMenuItem _menuCategories = null!;
    private ToolStripMenuItem _menuShipments = null!;
    private ToolStripMenuItem _menuHistory = null!;
    private ToolStripMenuItem _menuSettings = null!;
    private ToolStripMenuItem _menuCurrency = null!;
    private ToolStripMenuItem _menuRub = null!;
    private ToolStripMenuItem _menuUsd = null!;
    private ToolStripMenuItem _menuEur = null!;
    private ToolStripMenuItem _menuExit = null!;
    private Label _lblSearch = null!;
    private Label _lblCategory = null!;
    private TextBox _txtSearch = null!;
    private ComboBox _cmbCategory = null!;
    private Button _btnAdd = null!;
    private Button _btnEdit = null!;
    private Button _btnDelete = null!;
    private Label _lblAdminOnly = null!;
    private DataGridView _dgvProducts = null!;
    private Label _lblCount = null!;

    private List<Product> _allProducts = new();

    public CatalogForm( //это основн конструк
        User currentUser,
        ICurrencyService currencyService,
        IProductRepository? productRepository = null,
        ICategoryRepository? categoryRepository = null,
        IUnitOfWork? unitOfWork = null)
    {
        _currentUser = currentUser;
        _currencyService = currencyService;
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;

        InitializeComponent();
        ApplyRolePermissions();
        WriteOffExpiredProducts();
        LoadDataAsync();
    }

    public CatalogForm(User currentUser)
        : this(currentUser,
               ServiceLocator.GetService<ICurrencyService>())
    {
    }

    private void InitializeComponent()
    {
        var roleDisplay = _currentUser.Role == UserRole.Admin
            ? Strings.Role_Admin
            : Strings.Role_Kladovshik;

        Text = Strings.Catalog_Title(_currentUser.Login, roleDisplay);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.White;
        WindowState = FormWindowState.Maximized;
        Size = new Size(1200, 700);

        _menuStrip = new MenuStrip { BackColor = Color.FromArgb(28, 42, 74), ForeColor = Color.White, Font = new Font("Segoe UI", 11) };
        _menuCatalog = new ToolStripMenuItem(Strings.Catalog_MenuCatalog) { ForeColor = Color.White };
        _menuCategories = new ToolStripMenuItem(Strings.Catalog_MenuCategories) { ForeColor = Color.White };
        _menuShipments = new ToolStripMenuItem(Strings.Catalog_MenuShipments) { ForeColor = Color.White };
        _menuHistory = new ToolStripMenuItem(Strings.Catalog_MenuHistory) { ForeColor = Color.White };
        _menuSettings = new ToolStripMenuItem("Настройки") { ForeColor = Color.White, BackColor = Color.FromArgb(28, 42, 74) };
        _menuCurrency = new ToolStripMenuItem("Валюта") { ForeColor = Color.White, BackColor = Color.FromArgb(28, 42, 74) };

        _menuRub = new ToolStripMenuItem("₽ RUB (Российский рубль)")
        {
            ForeColor = Color.White,
            BackColor = Color.FromArgb(28, 42, 74)
        };
        _menuUsd = new ToolStripMenuItem("$ USD (Американский доллар)")
        {
            ForeColor = Color.White,
            BackColor = Color.FromArgb(28, 42, 74)
        };
        _menuEur = new ToolStripMenuItem("€ EUR (Евро)")
        {
            ForeColor = Color.White,
            BackColor = Color.FromArgb(28, 42, 74)
        };

        _menuRub.Click += (s, e) => { _currencyService.SetCurrency(Currency.RUB); LoadDataAsync(); };
        _menuUsd.Click += (s, e) => { _currencyService.SetCurrency(Currency.USD); LoadDataAsync(); };
        _menuEur.Click += (s, e) => { _currencyService.SetCurrency(Currency.EUR); LoadDataAsync(); };

        _menuCurrency.DropDownItems.AddRange(new ToolStripItem[] { _menuRub, _menuUsd, _menuEur });
        _menuSettings.DropDownItems.Add(_menuCurrency);

        //язык локализация???
        var menuLanguage = new ToolStripMenuItem("Язык (Language)")
        {
            ForeColor = Color.White,
            BackColor = Color.FromArgb(28, 42, 74)
        };

        var menuRussian = new ToolStripMenuItem("Русский")
        {
            ForeColor = Color.White,
            BackColor = Color.FromArgb(28, 42, 74)
        };

        var menuEnglish = new ToolStripMenuItem("English")
        {
            ForeColor = Color.White,
            BackColor = Color.FromArgb(28, 42, 74)
        };

        menuRussian.Click += (s, e) =>
        {
            Localization.SetLanguage("ru");
            UpdateUILanguage();
        };

        menuEnglish.Click += (s, e) =>
        {
            Localization.SetLanguage("en");
            UpdateUILanguage();
        };

        menuLanguage.DropDownItems.AddRange(new ToolStripItem[] { menuRussian, menuEnglish });
        _menuSettings.DropDownItems.Add(menuLanguage);

        _menuCategories.Click += (s, e) => OpenCategoryForm();
        _menuShipments.Click += (s, e) => OpenShipmentForm();
        _menuHistory.Click += (s, e) => OpenHistoryForm();

        var menuSupplies = new ToolStripMenuItem("Поставки") { ForeColor = Color.White };
        menuSupplies.Click += (s, e) => OpenSuppliesForm();

        var menuReports = new ToolStripMenuItem("Отчеты") { ForeColor = Color.White };
        menuReports.Click += (s, e) => OpenReportsForm();

        var menuHeatmap = new ToolStripMenuItem("Тепловая карта склада") { ForeColor = Color.White };
        menuHeatmap.Click += (s, e) =>
        {
            var heatmapForm = new WarehouseHeatmapForm();
            heatmapForm.ShowDialog();
        };


        _menuExit = new ToolStripMenuItem(Strings.Catalog_MenuExit) { ForeColor = Color.White };
        _menuExit.Click += (s, e) => Close();

        var menuContractors = new ToolStripMenuItem("Контрагенты") { ForeColor = Color.White };
        menuContractors.Click += (s, e) =>
        {
            var contractorForm = new ContractorForm();
            contractorForm.ShowDialog();
        };
        _menuStrip.Items.AddRange(new ToolStripItem[]
        {
            _menuCatalog,
            _menuCategories,
            menuContractors,
            _menuShipments,
            _menuHistory,
            menuSupplies,
            menuReports,
            _menuSettings,
            menuHeatmap,
            _menuExit
        });
        MainMenuStrip = _menuStrip;

        _lblSearch = new Label { Text = Strings.Catalog_LabelSearch, Font = new Font("Segoe UI", 10), Location = new Point(12, 36), AutoSize = true };
        _txtSearch = new TextBox { Font = new Font("Segoe UI", 10), Location = new Point(70, 33), Size = new Size(180, 26), BorderStyle = BorderStyle.FixedSingle };
        _txtSearch.TextChanged += (s, e) => FilterGrid();

        _lblCategory = new Label { Text = Strings.Catalog_LabelCategory, Font = new Font("Segoe UI", 10), Location = new Point(270, 36), AutoSize = true };
        _cmbCategory = new ComboBox { Font = new Font("Segoe UI", 10), Location = new Point(355, 33), Size = new Size(160, 26), DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbCategory.SelectedIndexChanged += (s, e) => FilterGrid();

        _btnAdd = new Button
        {
            Text = Strings.Catalog_BtnAdd,
            Font = new Font("Segoe UI", 10),
            Location = new Point(12, 68),
            Size = new Size(150, 30),
            BackColor = Color.FromArgb(40, 120, 200),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        _btnAdd.FlatAppearance.BorderSize = 0;
        _btnAdd.Click += BtnAdd_Click;

        _btnEdit = new Button
        {
            Text = Strings.Catalog_BtnEdit,
            Font = new Font("Segoe UI", 10),
            Location = new Point(172, 68),
            Size = new Size(140, 30),
            BackColor = Color.FromArgb(28, 42, 74),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        _btnEdit.FlatAppearance.BorderSize = 0;
        _btnEdit.Click += BtnEdit_Click;

        _btnDelete = new Button
        {
            Text = Strings.Catalog_BtnDelete,
            Font = new Font("Segoe UI", 10),
            Location = new Point(322, 68),
            Size = new Size(110, 30),
            BackColor = Color.FromArgb(200, 50, 50),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        _btnDelete.FlatAppearance.BorderSize = 0;
        _btnDelete.Click += BtnDelete_Click;

        _lblAdminOnly = new Label
        {
            Text = Strings.Catalog_AdminOnly,
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.Gray,
            AutoSize = true,
            Location = new Point(442, 75),
            Visible = false
        };

        _dgvProducts = new DataGridView
        {
            Location = new Point(12, 108),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            Size = new Size(1160, 520),
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.Fixed3D,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            Font = new Font("Segoe UI", 10)
        };
        _dgvProducts.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(28, 42, 74);
        _dgvProducts.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        _dgvProducts.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        _dgvProducts.EnableHeadersVisualStyles = false;
        _dgvProducts.CellFormatting += DgvProducts_CellFormatting;

        _lblCount = new Label
        {
            Text = Strings.Catalog_CountLabel(0),
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.Gray,
            AutoSize = true,
            Location = new Point(14, 635),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left
        };

        Controls.AddRange(new Control[]
        {
            _menuStrip, _lblSearch, _txtSearch, _lblCategory, _cmbCategory,
            _btnAdd, _btnEdit, _btnDelete, _lblAdminOnly, _dgvProducts, _lblCount
        });
    }

    private void ApplyRolePermissions()
    {
        var isAdmin = _currentUser.Role == UserRole.Admin;
        var isKlad = _currentUser.Role == UserRole.Kladovshik;

        _menuCategories.Visible = isAdmin;
        _menuHistory.Visible = isAdmin;
        _menuShipments.Visible = isKlad;

        _btnAdd.Enabled = isAdmin;
        _btnEdit.Enabled = isAdmin;
        _btnDelete.Enabled = isAdmin;
        _lblAdminOnly.Visible = isKlad;
    }

    public async Task LoadDataAsync()
    {
        try
        {
            //ассинхронизация вот тута
            var categories = await Task.Run(() =>
            {
                using var db = new AppDbContext();
                return db.Categories.OrderBy(c => c.Name).ToList();
            });

            var products = await Task.Run(() =>
            {
                using var db = new AppDbContext();
                return db.Products.Include(p => p.Category).OrderBy(p => p.Article).ToList();
            });
            //

            this.Invoke((MethodInvoker)delegate
            {
                _cmbCategory.Items.Clear();
                _cmbCategory.Items.Add("Все");
                foreach (var cat in categories) _cmbCategory.Items.Add(cat.Name);
                if (_cmbCategory.SelectedIndex < 0) _cmbCategory.SelectedIndex = 0;

                _allProducts = products;
                _log.Debug("Загружено {0} товаров", _allProducts.Count);
                FilterGrid();
            });
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Ошибка загрузки каталога");
            MessageBox.Show($"Ошибка загрузки:\n{ex.Message}",
                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void WriteOffExpiredProducts()
    {
        try
        {
            using var db = new AppDbContext();
            var today = DateOnly.FromDateTime(DateTime.Today);
            var expired = db.Products
                .Where(p => p.ExpiryDate.HasValue && p.ExpiryDate.Value < today && p.Stock > 0)
                .ToList();

            if (expired.Count == 0) return;

            var lines = new System.Text.StringBuilder();
            foreach (var p in expired)
            {
                _log.Info("Автосписание: {0} ({1}), кол-во {2}, срок истёк {3}",
                    p.Article, p.Name, p.Stock, p.ExpiryDate);
                lines.AppendLine($"• {p.Article} — {p.Name}: {p.Stock} {p.Unit}  (срок: {p.ExpiryDate:dd.MM.yyyy})");
                p.Stock = 0;
            }
            db.SaveChanges();

            MessageBox.Show(
                $"⚠ Автоматически списаны просроченные товары ({expired.Count} позиций):\n\n{lines}",
                "Списание просрочки", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Ошибка автосписания просроченных товаров");
        }
    }

    private void DgvProducts_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
    {
        if (_dgvProducts.DataSource == null || e.RowIndex < 0) return;
        var rowData = (dynamic?)_dgvProducts.Rows[e.RowIndex].DataBoundItem;
        if (rowData != null && rowData._NearExpiry)
        {
            e.CellStyle.BackColor = Color.FromArgb(255, 240, 160);
            e.CellStyle.ForeColor = Color.FromArgb(100, 60, 0);
        }
    }

    private void FilterGrid()
    {
        var search = _txtSearch.Text.Trim().ToLower();
        var category = _cmbCategory.SelectedItem?.ToString() ?? "Все";
        var today = DateOnly.FromDateTime(DateTime.Today);
        var warnDate = today.AddDays(30);

        var filtered = _allProducts.AsEnumerable();
        filtered = filtered.Where(p => p.ExpiryDate == null || p.ExpiryDate >= today);

        if (!string.IsNullOrEmpty(search))
            filtered = filtered.Where(p =>
                p.Article.ToLower().Contains(search) ||
                p.Name.ToLower().Contains(search));
        if (category != "Все")
            filtered = filtered.Where(p => p.Category.Name == category);

        var list = filtered.ToList();
        var currencySymbol = _currencyService.GetSymbol(_currencyService.CurrentCurrency);

        _dgvProducts.DataSource = list.Select(p =>
        {
            bool nearExpiry = p.ExpiryDate.HasValue && p.ExpiryDate.Value <= warnDate;
            string expiryText = p.ExpiryDate.HasValue
                ? (nearExpiry
                    ? $"⚠ {p.ExpiryDate.Value:dd.MM.yyyy} (скоро истечёт!)"
                    : p.ExpiryDate.Value.ToString("dd.MM.yyyy"))
                : "Бессрочно";

            return new
            {
                Артикул = p.Article,
                Название = p.Name,
                Категория = p.Category.Name,
                Ед_изм = p.Unit,
                Цена = _currencyService.Format(_currencyService.ConvertFromPurchaseCurrency(
                    p.PurchasePrice,
                    p.PurchaseCurrency,
                    p.PurchaseRate,
                    _currencyService.CurrentCurrency)),
                Остаток = p.Stock,
                Срок_годности = expiryText,
                _Id = p.Id,
                _NearExpiry = nearExpiry
            };
        }).ToList();

        if (_dgvProducts.Columns.Contains("_Id")) _dgvProducts.Columns["_Id"]!.Visible = false;
        if (_dgvProducts.Columns.Contains("_NearExpiry")) _dgvProducts.Columns["_NearExpiry"]!.Visible = false;
        if (_dgvProducts.Columns.Contains("Ед_изм")) _dgvProducts.Columns["Ед_изм"]!.HeaderText = "Ед. изм.";
        if (_dgvProducts.Columns.Contains("Цена")) _dgvProducts.Columns["Цена"]!.HeaderText = $"Цена ({currencySymbol})";
        if (_dgvProducts.Columns.Contains("Срок_годности")) _dgvProducts.Columns["Срок_годности"]!.HeaderText = "Срок годности";

        _lblCount.Text = $"всего позиций: {list.Count}";
    }

    private Guid? GetSelectedId()
    {
        if (_dgvProducts.CurrentRow == null) return null;
        var val = _dgvProducts.CurrentRow.Cells["_Id"].Value;
        return val is Guid id ? id : null;
    }

    private void BtnAdd_Click(object? sender, EventArgs e)
    {
        var form = new ProductForm(null);
        if (form.ShowDialog() == DialogResult.OK) LoadDataAsync();
    }

    private void BtnEdit_Click(object? sender, EventArgs e)
    {
        var id = GetSelectedId();
        if (id == null)
        {
            MessageBox.Show("Выберите товар.", "Внимание",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var db = new AppDbContext();
        var product = db.Products.Include(p => p.Category).FirstOrDefault(p => p.Id == id);
        if (product == null) return;

        var form = new ProductForm(product);
        if (form.ShowDialog() == DialogResult.OK) LoadDataAsync();
    }

    private void BtnDelete_Click(object? sender, EventArgs e)
    {
        var id = GetSelectedId();
        if (id == null)
        {
            MessageBox.Show(Strings.Catalog_SelectProduct, Strings.Warning,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var db = new AppDbContext();
        var product = db.Products.FirstOrDefault(p => p.Id == id);
        if (product == null) return;

        var dlg = new DeleteConfirmForm($"{product.Article} — {product.Name}");
        if (dlg.ShowDialog() == DialogResult.Yes)
        {
            _log.Info("Удаление товара: {0} ({1})", product.Article, product.Name);
            db.Products.Remove(product);
            db.SaveChanges();
            LoadDataAsync();
        }
    }

    private void OpenCategoryForm()
    {
        new CategoryForm().ShowDialog();
        LoadDataAsync();
    }

    private void OpenShipmentForm() { new ShipmentForm(_currentUser).ShowDialog(); LoadDataAsync(); }
    private void OpenHistoryForm() { new HistoryForm().ShowDialog(); }
    private void OpenSuppliesForm() { new SuppliesForm(DbConfig.ConnectionString, _currentUser.Id).ShowDialog(); LoadDataAsync(); }
    private void OpenReportsForm() { new ReportsForm(DbConfig.ConnectionString).ShowDialog(); }


    private void UpdateUILanguage()
    {
        Text = Localization.CatalogTitle;
        _menuCatalog.Text = Localization.Catalog;
        _menuCategories.Text = Localization.Categories;
        _menuShipments.Text = Localization.Shipments;
        _menuHistory.Text = Localization.History;
        _menuSettings.Text = Localization.Settings;
        _menuCurrency.Text = Localization.Currency;
        _menuExit.Text = Localization.Exit;
        _btnAdd.Text = Localization.Add;
        _btnEdit.Text = Localization.Edit;
        _btnDelete.Text = Localization.Delete;
        _lblSearch.Text = Localization.Search;
        _lblCategory.Text = Localization.Category;

        FilterGrid();
    }

}