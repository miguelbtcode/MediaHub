using FluentAssertions;
using MediaHub.Behaviors;
using MediaHub.Core;
using MediaHub.DependencyInjection;
using MediaHub.Validation;
using Microsoft.Extensions.DependencyInjection;

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
                .RegisterServicesFromAssemblies(typeof(ValidationTests).Assembly)
                .AddGlobalPipelineBehavior(typeof(ValidationBehavior<,>)));
            
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
                .RegisterServicesFromAssemblies(typeof(ValidationTests).Assembly)
                .AddGlobalPipelineBehavior(typeof(ValidationBehavior<,>)));
            
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
                
                return Task.FromResult(new ValidationResult(failures));
            }
        }
    }
}