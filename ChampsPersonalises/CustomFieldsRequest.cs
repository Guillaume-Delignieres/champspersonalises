using Microsoft.Extensions.Caching.Memory;

namespace ChampsPersonalises;

public class CustomFieldCache
{
    // Set a limit to 1024 for the Cache size, this value would have to be evaluated based on the size of the cached object 
    private const int CacheSize = 1024;
    
    // Set a default cache entry to 1 - So we can store 1024 cache entry for each cache
    private const int CacheEntrySize = 1;
    
    // Set a sliding expiration for the cache - Cache entry will be removed after 1 minute if not accessed since.
    private static readonly TimeSpan CacheSlidingOperation = TimeSpan.FromMinutes(1);

    private static readonly MemoryCacheOptions MemoryCacheOptions = new MemoryCacheOptions
    {
        SizeLimit = CacheSize
    };

    // We are creating a cache of each type of Custom Fields we manage.
    // It is probably OK to do so as their number is limited to 4.
    // This decision have to be reviewed if that number was to grow
    private readonly IMemoryCache _clientCache = new MemoryCache(MemoryCacheOptions);
    
    private readonly IMemoryCache _resourceCache = new MemoryCache(MemoryCacheOptions);
    
    private readonly IMemoryCache _taskCache  = new MemoryCache(MemoryCacheOptions);
    
    private readonly IMemoryCache _projectCache = new MemoryCache(MemoryCacheOptions);
    
    // GetOrCreate - If the data is not cached, MemoryCachew will call the cache entry factory which will create the entry in the cache and return it.
    // Otherwise, just return the cache entry.
    public Task<List<CustomFieldJs>?> GetClientCustomFields(string companyId, CustomFieldTypeUsedFor customFieldTypeUsedFor, Func<Task<List<CustomFieldJs>>> factory)
    {
        async Task<List<CustomFieldJs>> CacheEntryFactory(ICacheEntry entry)
        {
            entry.Size = CacheEntrySize;
            entry.SlidingExpiration = CacheSlidingOperation;
            return await factory.Invoke();
        }

        return customFieldTypeUsedFor switch
        {
            CustomFieldTypeUsedFor.Project => _projectCache.GetOrCreateAsync(companyId, async entry => await CacheEntryFactory(entry)),
            CustomFieldTypeUsedFor.Task => _taskCache.GetOrCreateAsync(companyId, async entry => await CacheEntryFactory(entry)),
            CustomFieldTypeUsedFor.Resource => _resourceCache.GetOrCreateAsync(companyId, async entry => await CacheEntryFactory(entry)),
            CustomFieldTypeUsedFor.Client => _clientCache.GetOrCreateAsync(companyId, async entry => await CacheEntryFactory(entry)),
            _ => throw new ArgumentOutOfRangeException(nameof(customFieldTypeUsedFor), customFieldTypeUsedFor, null)
        };
    }

    public void InvalidateWholeCacheForCompany(string companyId)
    {
        _projectCache.Remove(companyId);
        _taskCache.Remove(companyId);
        _resourceCache.Remove(companyId);
        _clientCache.Remove(companyId);
    }

    // To invalidate, remove the data for the company in the cache
    public void InvalidateCacheForCompany(CustomFieldTypeUsedFor customFieldTypeUsedFor, string companyId)
    {
        switch (customFieldTypeUsedFor)
        {
            case CustomFieldTypeUsedFor.Project:
                _projectCache.Remove(companyId);
                break;
            case CustomFieldTypeUsedFor.Task:
                _taskCache.Remove(companyId);
                break;
            case CustomFieldTypeUsedFor.Resource:
                _resourceCache.Remove(companyId);
                break;
            case CustomFieldTypeUsedFor.Client:
                _clientCache.Remove(companyId);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(customFieldTypeUsedFor), customFieldTypeUsedFor, null);
        }
    }
    
}

public class CustomFieldsRequest
{
    private readonly IUserSettingsService _userSettingsService;
    private readonly ICustomFieldData _customFieldData;
    private readonly CustomFieldCache _cache;

    public CustomFieldsRequest(IUserSettingsService userSettingsService, ICustomFieldData customFieldData, CustomFieldCache cache)
    {
        _userSettingsService = userSettingsService;
        _customFieldData = customFieldData;
        _cache = cache;
    }
    
    // I removed a lot of code in that class and leverage the factory that is used by the cache.
    // I create a fake Data interface which will have the responsibility to fetch the CustomDataField if they are not in the cache
    public Task<List<CustomFieldJs>?> GetCustomFieldsFor(CustomFieldTypeUsedFor cfObject)
    {
        var companyId = _userSettingsService.GetUserDescription().CompanyId;

        Func<Task<List<CustomFieldJs>>> factory = cfObject switch
        {
            CustomFieldTypeUsedFor.Project => () => _customFieldData.GetCfsForProjects(companyId),
            CustomFieldTypeUsedFor.Task => () =>  _customFieldData.GetCfsForTasks(companyId),
            CustomFieldTypeUsedFor.Resource => () =>  _customFieldData.GetCfsForResources(companyId),
            CustomFieldTypeUsedFor.Client => () =>  _customFieldData.GetCfsForClients(companyId),
            _ => throw new ArgumentOutOfRangeException(nameof(cfObject), cfObject, null)
        };
        
        return _cache.GetClientCustomFields(_userSettingsService.GetUserDescription().CompanyId, cfObject, factory);
    }
}

public interface ICustomFieldData
{
    Task<List<CustomFieldJs>> GetCfsForClients(string companyId);
    Task<List<CustomFieldJs>> GetCfsForResources(string companyId);
    Task<List<CustomFieldJs>> GetCfsForTasks(string companyId);
    Task<List<CustomFieldJs>> GetCfsForProjects(string companyId);
}