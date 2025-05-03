using FluentAssertions;
using MediaHub.Core;
using MediaHub.DependencyInjection;
using MediaHub.Validation;
using MediaHub.Contracts.Pipelines;
using Microsoft.Extensions.DependencyInjection;
using MediaHub.Contracts;

namespace MediaHub.Tests
{
    public class ValidationTests
    {
        [Fact]
        public async Task Send_WithValidRequest_ShouldSucceed()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddMediaHub(config => config
                .RegisterServicesFromAssemblies(typeof(ValidationTests).Assembly));
            
            // Registrar la implementación concreta para la interfaz
            services.AddTransient(typeof(IValidationPipelineBehavior<,>), typeof(ValidationPipelineBehaviorImpl<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationPipelineBehaviorImpl<,>));
            
            services.AddTransient<IRequestHandler<TestValidatableRequest, string>, TestValidatableRequestHandler>();
            services.AddTransient<IValidator<TestValidatableRequest>, TestValidator>();
            
            var provider = services.BuildServiceProvider();
            var mediator = provider.GetRequiredService<IMediator>();
            
            var request = new TestValidatableRequest { Name = "Valid Name", Email = "valid@example.com" };
            
            // Act
            var response = await mediator.Send(request);
            
            // Assert
            response.Should().Be("Request Handled");
        }
        
        [Fact]
        public async Task Send_WithInvalidRequest_ShouldThrowValidationException()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddMediaHub(config => config
                .RegisterServicesFromAssemblies(typeof(ValidationTests).Assembly));
            
            // Registrar la implementación concreta para la interfaz
            services.AddTransient(typeof(IValidationPipelineBehavior<,>), typeof(ValidationPipelineBehaviorImpl<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationPipelineBehaviorImpl<,>));
            
            services.AddTransient<IRequestHandler<TestValidatableRequest, string>, TestValidatableRequestHandler>();
            services.AddTransient<IValidator<TestValidatableRequest>, TestValidator>();
            
            var provider = services.BuildServiceProvider();
            var mediator = provider.GetRequiredService<IMediator>();
            
            var request = new TestValidatableRequest { Name = "", Email = "notavalidemail" };
            
            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => mediator.Send(request));
            
            // Additional assertions
            exception.Errors.Should().HaveCount(2);
            
            var errors = new List<ValidationFailure>(exception.Errors);
            errors[0].PropertyName.Should().Be("Name");
            errors[0].ErrorMessage.Should().Be("Name is required");
            
            errors[1].PropertyName.Should().Be("Email");
            errors[1].ErrorMessage.Should().Be("Email is invalid");
        }

        [Fact]
        public async Task Send_WithCustomValidationPipelineBehavior_ShouldValidate()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddMediaHub(config => config
                .RegisterServicesFromAssemblies(typeof(ValidationTests).Assembly));
            
            services.AddTransient<IRequestHandler<TestValidatableRequest, string>, TestValidatableRequestHandler>();
            services.AddTransient<IValidator<TestValidatableRequest>, TestValidator>();
            services.AddTransient<IValidationPipelineBehavior<TestValidatableRequest, string>, TestValidationPipelineBehavior>();
            services.AddTransient<IPipelineBehavior<TestValidatableRequest, string>>(sp => 
                sp.GetRequiredService<IValidationPipelineBehavior<TestValidatableRequest, string>>());
            
            var provider = services.BuildServiceProvider();
            var mediator = provider.GetRequiredService<IMediator>();
            
            var request = new TestValidatableRequest { Name = "", Email = "notavalidemail" };
            
            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => mediator.Send(request));
            
            exception.Errors.Should().HaveCount(2);
        }
        
        // Test request & handler classes
        public class TestValidatableRequest : IRequest<string>
        {
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
        }
        
        public class TestValidatableRequestHandler : IRequestHandler<TestValidatableRequest, string>
        {
            public Task<string> Handle(TestValidatableRequest request, CancellationToken cancellationToken)
            {
                return Task.FromResult("Request Handled");
            }
        }
        
        public class TestValidator : IValidator<TestValidatableRequest>
        {
            public Task<ValidationResult> ValidateAsync(ValidationContext<TestValidatableRequest> context, CancellationToken cancellationToken)
            {
                var request = context.Instance;
                var failures = new List<ValidationFailure>();
                
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    failures.Add(new ValidationFailure("Name", "Name is required"));
                }
                
                if (string.IsNullOrWhiteSpace(request.Email))
                {
                    failures.Add(new ValidationFailure("Email", "Email is required"));
                }
                else if (!request.Email.Contains("@"))
                {
                    failures.Add(new ValidationFailure("Email", "Email is invalid"));
                }
                
                var result = new ValidationResult(failures);
                return Task.FromResult(result);
            }
        }

        public class ValidationPipelineBehaviorImpl<TRequest, TResponse> : IValidationPipelineBehavior<TRequest, TResponse>
            where TRequest : IRequest<TResponse>
        {
            private readonly IEnumerable<IValidator<TRequest>> _validators;

            public ValidationPipelineBehaviorImpl(IEnumerable<IValidator<TRequest>> validators)
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

        public class TestValidationPipelineBehavior : IValidationPipelineBehavior<TestValidatableRequest, string>
        {
            private readonly IEnumerable<IValidator<TestValidatableRequest>> _validators;

            public TestValidationPipelineBehavior(IEnumerable<IValidator<TestValidatableRequest>> validators)
            {
                _validators = validators;
            }

            public async Task<string> Handle(TestValidatableRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
            {
                if (_validators.Any())
                {
                    var context = new ValidationContext<TestValidatableRequest>(request);
                    
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
    }
}