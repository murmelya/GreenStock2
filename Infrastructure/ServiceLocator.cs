using GreenStock.Data;
using GreenStock.Interfaces;
using GreenStock.Services;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace GreenStock.Infrastructure;


public static class ServiceLocator
{
    private static IServiceProvider? _serviceProvider;


    public static void Initialize()
    {
        var services = new ServiceCollection();

        services.AddScoped<AppDbContext>();


        services.AddScoped<IRepository, Repository>();

        services.AddSingleton<ICurrencyService, CurrencyService>();
        services.AddSingleton<ICounterpartyService, CounterpartyService>();
        services.AddSingleton<IWeatherLogisticsService, WeatherLogisticsService>();
        services.AddSingleton<IExchangeRateService, ExchangeRateService>();
        services.AddSingleton<IExpiryService, ExpiryService>();
        services.AddSingleton<IHistoryService, HistoryService>();

        services.AddSingleton<HttpClient>();

        _serviceProvider = services.BuildServiceProvider();
    }

    public static T GetService<T>() where T : notnull
    {
        if (_serviceProvider == null)
            throw new InvalidOperationException("ServiceLocator не инициализирован. Вызовите Initialize() в Program.Main");

        return _serviceProvider.GetRequiredService<T>();
    }
}

public class ServiceCollection
{
    private readonly Dictionary<Type, Type> _scopedTypes = new();
    private readonly Dictionary<Type, Type> _singletonTypes = new();
    private readonly Dictionary<Type, object> _singletonInstances = new();


    public void AddScoped<TInterface, TImplementation>() where TImplementation : TInterface
    {
        _scopedTypes[typeof(TInterface)] = typeof(TImplementation);
    }
    

    public void AddScoped<T>() where T : new()
    {
        _scopedTypes[typeof(T)] = typeof(T);
    }

    public void AddSingleton<TInterface, TImplementation>() where TImplementation : TInterface
    {
        _singletonTypes[typeof(TInterface)] = typeof(TImplementation);
    }

    public void AddSingleton<T>(T instance)
    {
        _singletonInstances[typeof(T)] = instance!;
    }

    public void AddSingleton<T>() where T : new()
    {
        _singletonTypes[typeof(T)] = typeof(T);
    }

    public IServiceProvider BuildServiceProvider()
    {
        return new SimpleServiceProvider(_scopedTypes, _singletonTypes, _singletonInstances);
    }
}

public class SimpleServiceProvider : IServiceProvider
{
    private readonly Dictionary<Type, Type> _scopedTypes;
    private readonly Dictionary<Type, Type> _singletonTypes;
    private readonly Dictionary<Type, object> _singletonInstances;

    public SimpleServiceProvider(
        Dictionary<Type, Type> scopedTypes,
        Dictionary<Type, Type> singletonTypes,
        Dictionary<Type, object> singletonInstances)
    {
        _scopedTypes = scopedTypes;
        _singletonTypes = singletonTypes;
        _singletonInstances = singletonInstances;
    }

    public object? GetService(Type serviceType)
    {
        if (_singletonInstances.TryGetValue(serviceType, out var singletonInstance))
            return singletonInstance;
        if (_singletonTypes.TryGetValue(serviceType, out var singletonImpl))
            return Activator.CreateInstance(singletonImpl);

        if (_scopedTypes.TryGetValue(serviceType, out var scopedImpl))
            return Activator.CreateInstance(scopedImpl);

        return null;
    }
}

public interface IServiceProvider
{
    object? GetService(Type serviceType);
}

public static class ServiceProviderExtensions
{
    public static T GetRequiredService<T>(this IServiceProvider provider) where T : notnull
    {
        var service = provider.GetService(typeof(T));
        if (service == null)
            throw new InvalidOperationException($"Сервис {typeof(T)} не зарегистрирован");
        return (T)service;
    }
}