### Cross-Platform .NET Core HTTP Base Flying Proxy

This project is demonstrating manage and consume distributed API - Micro Service 
from different regions (endpoints) with ease. 

Flying Proxy allows the management of scalable applications, trigger many operations at the same time from your clients (SPA Web App or Mobile App) and 
start to consume your new resource (Backend Instance, APIs) that you can simply add.

Flying Proxy aims to:
- Simple scalability
- Effective and type-safe management of distributed architecture
- Better performance
- Maintainability
- Provide updated API or SDK usage

[Latest release on Nuget](https://www.nuget.org/packages/NetCoreStack.Proxy/)

### Usage for Client Side

#### Startup ConfigureServices
```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Add NetCoreProxy Dependencies and Configuration
    services.AddNetCoreProxy(Configuration, options =>
    {
        // Register the API to use as Proxy
        options.Register<IGuidelineApi>();
    });

    // Add framework services.
    services.AddMvc();
}
```

#### Example dependency injection
```csharp
public class TestController : Controller
{
    private readonly IGuidelineApi _api;

    public TestController(IGuidelineApi api)
    {
        _api = api;
    }

    public async Task<IActionResult> GetPostsAsync()
    {
        var items = await _api.GetPostsAsync();
        return Json(items);
    }
}
```

#### Add configuration section to the appsettings.json file (or your configuration file)
##### Example:
```json
"ProxySettings": {
    "RegionKeys": {
      "Main": "http://localhost:5000/,http://localhost:5001/",
      "Authorization": "http://localhost:5002/",
      "Integrations": "http://localhost:5003/"
    }
  }
```

### Usage for Contracts (Common) Side

#### API Contract Definition (Default HttpMethod is HttpGet)
```csharp
// This APIs expose methods from localhost:5000 and localhost:5001 as configured on ProxySettings
[ApiRoute("api/[controller]", regionKey: "Main")]
public interface IGuidelineApi : IApiContract
{
    void VoidOperation();

    int PrimitiveReturn(int i, string s, long l, DateTime dt);

    Task TaskOperation();

    Task<IEnumerable<Post>> GetPostsAsync();

    Task GetWithReferenceType(SimpleModel model);

    [HttpPostMarker]
    Task TaskActionPost(SimpleModel model);
}
```

### Backend - Server Side
#### API Contract Implementation
```csharp
[Route("api/[controller]")]
public class GuidelineController : Controller, IGuidelineApi
{
    private readonly ILoggerFactory _loggerFactory;

    protected ILogger Logger { get; }

    public GuidelineController(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
        Logger = _loggerFactory.CreateLogger<GuidelineController>();
    }

    [HttpGet(nameof(GetPostsAsync))]
    public async Task<IEnumerable<Post>> GetPostsAsync()
    {
        var httpRequest = new HttpRequestMessage(HttpMethod.Get, new Uri("https://jsonplaceholder.typicode.com/posts"));
        var response = await Factory.Client.SendAsync(httpRequest);
        var content = await response.Content.ReadAsStringAsync();
        var items = JsonConvert.DeserializeObject<List<Post>>(content);
        Logger.LogDebug($"{nameof(GetPostsAsync)}, PostsCount:{items.Count}");
        return items;
    }

    [HttpGet(nameof(GetWithReferenceType))]
    public async Task GetWithReferenceType([FromQuery]SimpleModel model)
    {
        var serializedModel = JsonConvert.SerializeObject(model);
        Logger.LogDebug($"{nameof(GetWithReferenceType)}, Model: {serializedModel}");
        await Task.Delay(900);
    }

    [HttpGet(nameof(PrimitiveReturn))]
    public int PrimitiveReturn(int i, string s, long l, DateTime dt)
    {
        Logger.LogDebug($"{nameof(PrimitiveReturn)}, i:{i}, s:{s}, l:{l}, dt:{dt}");
        return i + 10;
    }

    [HttpPost(nameof(TaskActionPost))]
    public async Task TaskActionPost([FromBody]SimpleModel model)
    {
        var serializedModel = JsonConvert.SerializeObject(model);
        Logger.LogDebug($"{nameof(TaskActionPost)}, Model: {serializedModel}");
        await Task.Delay(900);
    }

    [HttpGet(nameof(TaskOperation))]
    public async Task TaskOperation()
    {
        await Task.Delay(2000);
        Logger.LogDebug($"{nameof(TaskOperation)}, long running process completed!");
    }

    [HttpGet(nameof(VoidOperation))]
    public void VoidOperation()
    {
        var str = "Hello World!";
        Logger.LogDebug($"{nameof(VoidOperation)}, {str}");
    }
}
```

#### Multipart form data:
Proxy sends all POST methods as JSON but if the method parameter model contains IFormFile type property it converts the content-type to multipart/form-data. In this case, use any model to POST multipart/form-data to API without [FromBody] attribute on action parameter. For example:

```csharp
// Interface
[HttpPostMarker]
Task<AlbumViewModel> SaveAlbumSubmit(AlbumViewModelSubmit model)    
```

```csharp
// API Controller
[HttpPost(nameof(SaveAlbumSubmit))]
public async Task<AlbumViewModel> SaveAlbumSubmit(AlbumViewModelSubmit model)  
```





### Prerequisites
> [ASP.NET Core](https://github.com/aspnet/Home)