# M6T.Core.TupleModelBinder

This package only works with named tuples and JSON post body!!!.
What this package does is simply provide bindings for your controllers that maps incoming json data to tuple object at runtime. so you dont have to create a class for all of your actions in mvc.

If you want to contribute just do it no rules. In fact some other incoming data formats and checking the request content type to bind them is very very wellcome :) 
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
Post request body 
```json
{
  "user" : {
    "Name":"Test",
    "Surname":"Test2",
    "Email":"example@example.com"
  },
  "someData" : "If you like it, you put a data on it"
}
```
And in your controller use it like 
```C#
[HttpPost]
public IActionResult CreateUser((User user, string someData) request)
{
    using (var db = new DBContext())
    {
        var newUser = db.Users.Add(request.user);
        db.SaveChanges();
        return Json(new { userId = request.user.Id, someData = request.someData});
    }
}
```
