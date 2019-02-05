# M6T.Core.TupleModelBinder
AsNetCore Tuple model binder


# Usage
Modify startup.cs like
```C#
using M6T.Core.TupleModelBinder;
....

public void ConfigureServices(IServiceCollection services)
{
  services.AddMvc(options =>
  {
      options.ModelBinderProviders.Insert(0, new TupleModelBinderProvider());
  });
}
```
