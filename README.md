<p align="center">
  <img src="https://github.com/miguelbtcode/MediaHub/blob/v1.0.6.2/resources/mediahub-logo.png" alt="MediaHub logo" width="120"/>
</p>

# MediaHub

[![NuGet](https://img.shields.io/nuget/v/MediaHub.svg)](https://www.nuget.org/packages/MediaHub/)
[![Build Status](https://github.com/miguelbtcode/MediaHub/workflows/build/badge.svg)](https://github.com/miguelbtcode/MediaHub/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

MediaHub is a lightweight and efficient implementation of the Mediator pattern for .NET applications, designed to facilitate communication between components while maintaining low coupling.

## Features

- Lightweight and fast implementation of the Mediator pattern
- Behavior pipeline for intercepting and modifying requests
- Built-in caching support
- Easy registration with .NET's service container
- Compatible with dependency injection
- Automated validations

## Installation

You can install MediaHub via NuGet:

```
dotnet add package MediaHub
```

Or using the Visual Studio Package Manager:

```
Install-Package MediaHub
```

## Basic Usage

### Configuration

Register MediaHub in your service container:

```csharp
services.AddMediaHub(config => config
    .RegisterServicesFromAssemblies(typeof(Startup).Assembly));
```

### Define a Request and Handler

```csharp
// Define a request
public class GetUserByIdQuery : IRequest<UserDto>
{
    public int UserId { get; set; }
}

// Implement a handler
public class GetUserByIdHandler : IRequestHandler<GetUserByIdQuery, UserDto>
{
    private readonly IUserRepository _userRepository;

    public GetUserByIdHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserDto> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        return new UserDto { Id = user.Id, Name = user.Name, Email = user.Email };
    }
}
```

### Using the Mediator

```csharp
public class UserController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        var query = new GetUserByIdQuery { UserId = id };
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}
```

## Pipeline Behaviors

MediaHub allows you to add pipeline behaviors that execute before and after handlers:

### Validation Behavior

```csharp
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);
            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));
            
            var failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .ToList();

            if (failures.Count != 0)
                throw new ValidationException(failures);
        }

        return await next();
    }
}
```

### Registering Behaviors

```csharp
services.AddMediaHub(config => config
    .RegisterServicesFromAssemblies(typeof(Startup).Assembly)
    .AddGlobalPipelineBehavior<ValidationBehavior<,>>());
```

## Caching Support

MediaHub includes built-in caching support:

```csharp
// Mark requests as cacheable
public class GetCachedDataQuery : IRequest<DataResult>, ICacheable
{
    public int Id { get; set; }
    
    public string CacheKey => $"GetCachedData-{Id}";
}

// Register the caching behavior
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
```

## Examples

For more detailed examples, check the `/samples` folder in the repository.

## Contributing

Contributions are welcome. Please feel free to submit pull requests or open issues if you find any problems or have suggestions.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.