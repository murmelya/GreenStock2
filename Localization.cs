using System.Collections.Generic;

namespace GreenStock;

public static class Localization
{
    private static string _currentLanguage = "ru"; 

    public static void SetLanguage(string lang)
    {
        _currentLanguage = lang;
    }

    public static string GetString(string ru, string en)
    {
        return _currentLanguage == "ru" ? ru : en;
    }

    public static string Login => GetString("Войти", "Login");
    public static string Register => GetString("Регистрация", "Register");
    public static string Back => GetString("Назад", "Back");
    public static string Save => GetString("Сохранить", "Save");
    public static string Cancel => GetString("Отмена", "Cancel");
    public static string Error => GetString("Ошибка", "Error");
    public static string Warning => GetString("Внимание", "Warning");




    public static string CatalogTitle => GetString("Каталог товаров", "Product Catalog");
    public static string Catalog => GetString("Каталог", "Catalog");
    public static string Categories => GetString("Категории", "Categories");
    public static string Category => GetString("Категория:", "Category:");
    public static string Shipments => GetString("Отгрузки", "Shipments");
    public static string History => GetString("История", "History");
    public static string Settings => GetString("Настройки", "Settings");
    public static string Exit => GetString("Выход", "Exit");
    public static string Currency => GetString("Валюта", "Currency");
    public static string Language => GetString("Язык / Language", "Language / Language");
    public static string Russian => GetString("Русский", "Russian");
    public static string English => GetString("Английский", "English");
    public static string Add => GetString("Добавить", "Add");
    public static string Edit => GetString("Редактировать", "Edit");
    public static string Delete => GetString("Удалить", "Delete");
    public static string Search => GetString("Поиск:", "Search:");



    public static string ContractorCheck => GetString("Проверка контрагента", "Contractor Check");
    public static string ContractorData => GetString("Данные контрагента", "Contractor Data");
    public static string ContractorResult => GetString("Результат проверки", "Check Result");
    public static string ContractorHistory => GetString("История проверок", "Check History");
    public static string CheckByAPI => GetString("Проверить по API", "Check by API");
    public static string Clean => GetString("Чист", "Clean");
    public static string Blacklisted => GetString("В ЧС", "Blacklisted");
    public static string SaveData => GetString("Сохранить", "Save");
    public static string ClearForm => GetString("Очистить", "Clear");




    public static string WeatherTitle => GetString("Прогноз погоды для логистики", "Weather Forecast for Logistics");
    public static string DeliveryRegion => GetString("Регион доставки:", "Delivery Region:");
    public static string DeliveryDate => GetString("Дата доставки:", "Delivery Date:");
    public static string GetForecast => GetString("Получить прогноз", "Get Forecast");
    public static string ThermoRecommendation => GetString("Рекомендуется термоконтейнер", "Thermal container recommended");
    public static string InsuranceRecommendation => GetString("Рекомендуется страховка груза", "Cargo insurance recommended");

   



    public static string HeatmapTitle => GetString("Тепловая карта склада", "Warehouse Heatmap");
}