# ASP.NET Core - Advanced Testing

## Fluent Assertions

Fluent Assertions is a .NET library that supercharges your unit tests by providing a more fluent, natural language syntax for assertions. Instead of using traditional, sometimes cryptic assertions like `Assert.Equal` or `Assert.True`, you write assertions that closely resemble how you would express expectations in plain English.

---

## Benefits of Fluent Assertions

- **Readability**: Tests become more self-explanatory and easier to understand, even for developers who are not familiar with the codebase
- **Maintainability**: Changes to the underlying code often result in more understandable test failures due to the descriptive nature of the assertions
- **Rich API**: Offers a vast collection of assertion methods covering various scenarios (collections, strings, exceptions, and more)
- **Extensibility**: Allows you to create custom assertions for specific needs

---

## Important Fluent Assertions Methods with Examples

### Basic Assertions

```csharp
result.Should().Be(5);                 // result should be equal to 5
result.Should().NotBe(10);              // result should not be equal to 10
result.Should().BeTrue();                // result should be true
result.Should().BeFalse();               // result should be false
result.Should().BeNull();               // result should be null
result.Should().NotBeNull();             // result should not be null
```

### Collection Assertions

```csharp
list.Should().HaveCount(3);            // list should have 3 elements
list.Should().Contain("apple");        // list should contain "apple"
list.Should().OnlyContain(x => x > 0);  // all elements in list should be greater than 0
list.Should().BeEquivalentTo(new[] { 1, 2, 3 }); // lists should contain the same elements (order doesn't matter)
list.Should().NotContain("banana");    // list should not contain "banana"
list.Should().BeEmpty();               // list should be empty
list.Should().NotBeEmpty();            // list should not be empty
```

### String Assertions

```csharp
name.Should().StartWith("John");       // name should start with "John"
name.Should().EndWith("Doe");         // name should end with "Doe"
name.Should().Contain("Middle");       // name should contain "Middle"
name.Should().MatchRegex(@"\d{3}-\d{3}-\d{4}"); // name should match a phone number pattern
name.Should().BeEmpty();              // name should be empty
name.Should().NotBeNullOrEmpty();     // name should not be null or empty
name.Should().HaveLength(10);         // name should have length 10
```

### Exception Assertions

```csharp
Action act = () => someMethod(); 
act.Should().Throw<ArgumentException>(); // should throw ArgumentException
act.Should().Throw<Exception>()
    .WithMessage("Invalid operation");  // should throw an exception with a specific message
act.Should().NotThrow();               // should not throw any exception
```

### Type Assertions

```csharp
object obj = new Person();
obj.Should().BeOfType<Person>();       // obj should be of type Person
obj.Should().BeAssignableTo<object>();   // obj should be assignable to object
obj.Should().NotBeOfType<Employee>();  // obj should not be of type Employee
```

### Numeric Assertions

```csharp
number.Should().BePositive();          // number should be positive
number.Should().BeNegative();          // number should be negative
number.Should().BeGreaterThan(5);      // number should be greater than 5
number.Should().BeLessThanOrEqualTo(10); // number should be less than or equal to 10
number.Should().BeInRange(1, 100);     // number should be between 1 and 100
```

### DateTime Assertions

```csharp
date.Should().BeAfter(DateTime.Now);   // date should be after now
date.Should().BeBefore(DateTime.Now);  // date should be before now
date.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1)); // date should be close to now
```

---

## AutoFixture

AutoFixture is another powerful library that helps with unit testing by automatically generating test data for your classes. It saves you time and effort in creating complex objects for your tests, especially when dealing with objects that have many properties or nested objects.

### Benefits of AutoFixture

- **Test Data Generation**: Easily create instances of your classes with sensible default values for properties
- **Customization**: You can customize the generated data to fit specific test scenarios
- **Reduced Boilerplate**: Eliminates the need to manually create test data for each test

---

## Integrating AutoFixture with xUnit in ASP.NET Core

### Installation

Install Package: Add the `AutoFixture.Xunit2` NuGet package to your test project.

```bash
dotnet add package AutoFixture.Xunit2
```

### Use the [AutoData] Attribute

Decorate your test methods with `[AutoData]`. AutoFixture will automatically generate instances of the required types and pass them as arguments to your test methods.

### Example with AutoFixture

```csharp
using AutoFixture.Xunit2;
using Xunit;

public class PersonControllerTests
{
    [Theory, AutoData] // AutoFixture will create a Person instance for the test
    public void CreatePerson_ValidPerson_ReturnsOk(
        Person person, 
        Mock<IPersonsService> mockPersonsService)
    {
        // Arrange
        mockPersonsService
            .Setup(s => s.AddPerson(It.IsAny<PersonAddRequest>()))
            .Returns(person.ToPersonResponse());
        
        var controller = new PersonsController(mockPersonsService.Object);
        
        // Act
        var result = controller.Create(person.ToPersonAddRequest());
        
        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
    }
}
```

### Customizing AutoFixture

```csharp
[Theory, AutoData]
public void Test_WithCustomization(IFixture fixture)
{
    // Customize the generation
    fixture.Customize<Person>(c => c
        .With(p => p.Age, 25)
        .Without(p => p.Email));
    
    var person = fixture.Create<Person>();
    
    // person.Age will be 25
    // person.Email will be null
}
```

---

## Mocking

In unit testing, the goal is to test a specific unit of code (like a service class) in isolation from its dependencies. This helps you focus on the logic of the unit you're testing without worrying about external factors like database interactions or network calls. **Mocking** is a technique that enables this isolation.

Mocking involves creating substitute objects (mocks) that simulate the behavior of real dependencies. These mocks can be programmed to return specific data, throw exceptions, or track how they are used.

---

## Moq

Moq is a popular and intuitive mocking framework for .NET. It provides a fluent API to create mock objects easily.

### How Mocking Works Internally (with Moq)

#### 1. Create a Mock

You start by creating a mock object for the interface of the dependency you want to replace.

```csharp
var mockPersonRepository = new Mock<IPersonsRepository>();
```

#### 2. Set Up Behavior

You configure how the mock should behave when its methods are called.

```csharp
mockPersonRepository
    .Setup(repo => repo.GetAllPersons())
    .ReturnsAsync(new List<Person> { /* your test data */ });
```

#### 3. Inject the Mock

You inject the mock object into the class you're testing.

```csharp
var service = new PersonsService(mockPersonRepository.Object);
```

#### 4. Exercise Your Code

You call the methods of the class under test, which will interact with the mock object.

```csharp
var result = await service.GetAllPersons();
```

#### 5. Verify Interactions

Use Moq's verification features to check if the mock's methods were called as expected.

```csharp
mockPersonRepository.Verify(
    repo => repo.GetAllPersons(), 
    Times.Once);
```

---

## Code Example - Complete Test Setup

```csharp
// PersonsServiceTest.cs (Constructor)
public class PersonsServiceTest
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IFixture _fixture;
    private readonly Mock<IPersonsRepository> _personRepositoryMock;
    private readonly IPersonsRepository _personsRepository;
    private readonly IPersonsService _personService;
    private readonly ICountriesService _countriesService;

    public PersonsServiceTest(ITestOutputHelper testOutputHelper)
    {
        _fixture = new Fixture(); // AutoFixture for test data generation
 
        _personRepositoryMock = new Mock<IPersonsRepository>();
        _personsRepository = _personRepositoryMock.Object; // Get the mock object
 
        // Create a mock DbContext using EntityFrameworkCoreMock
        var dbContextMock = new DbContextMock<ApplicationDbContext>(
            new DbContextOptionsBuilder<ApplicationDbContext>().Options
        );
 
        // Mock the Countries DbSet with initial data
        dbContextMock.CreateDbSetMock(temp => temp.Countries, new List<Country> { });
 
        // Mock the Persons DbSet with initial data
        dbContextMock.CreateDbSetMock(temp => temp.Persons, new List<Person> { });
 
        // Create services based on mocked DbContext object
        _countriesService = new CountriesService(dbContextMock.Object);
        _personService = new PersonsService(_personsRepository);
        
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task AddPerson_ValidPerson_ReturnsPersonResponse()
    {
        // Arrange
        Person person = _fixture.Create<Person>();
        PersonAddRequest personAddRequest = person.ToPersonAddRequest();

        _personRepositoryMock
            .Setup(temp => temp.AddPerson(It.IsAny<Person>()))
            .ReturnsAsync(person);

        // Act
        PersonResponse personResponse = await _personService.AddPerson(personAddRequest);

        // Assert
        personResponse.PersonID.Should().Be(person.PersonID);
        personResponse.PersonName.Should().Be(person.PersonName);
        
        _personRepositoryMock.Verify(
            temp => temp.AddPerson(It.IsAny<Person>()), 
            Times.Once);
    }
}
```

**Explanation**:
- Creates mock objects for the `IPersonsRepository` interface and the `ApplicationDbContext` class
- Uses `EntityFrameworkCoreMock` to configure mock `DbSet` objects
- Initializes services, passing the mock repository to `PersonsService`
- Configures the mock repository to return specific data
- Verifies that the mock was called correctly

---

## Moq Setup Methods

### Basic Setup

```csharp
// Return a value
mock.Setup(x => x.GetById(1)).Returns(person);

// Return async
mock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(person);

// Return different values based on input
mock.Setup(x => x.GetById(It.IsAny<int>())).Returns<int>(id => new Person { Id = id });
```

### Setup with Conditions

```csharp
// Setup for specific parameter
mock.Setup(x => x.GetById(1)).Returns(person1);
mock.Setup(x => x.GetById(2)).Returns(person2);

// Setup with predicate
mock.Setup(x => x.GetById(It.Is<int>(id => id > 0))).Returns(person);

// Setup with range
mock.Setup(x => x.GetById(It.IsInRange(1, 100, Range.Inclusive))).Returns(person);
```

### Setup Exceptions

```csharp
// Throw exception
mock.Setup(x => x.GetById(-1)).Throws<ArgumentException>();

// Throw exception with message
mock.Setup(x => x.GetById(-1)).Throws(new ArgumentException("Invalid ID"));

// Throw async
mock.Setup(x => x.GetByIdAsync(-1)).ThrowsAsync<ArgumentException>();
```

### Setup Callbacks

```csharp
// Execute code when method is called
mock.Setup(x => x.AddPerson(It.IsAny<Person>()))
    .Callback<Person>(p => Console.WriteLine($"Adding {p.Name}"))
    .Returns(person);
```

---

## Moq Verify Methods

```csharp
// Verify method was called once
mock.Verify(x => x.GetById(1), Times.Once);

// Verify method was never called
mock.Verify(x => x.DeleteById(1), Times.Never);

// Verify method was called at least once
mock.Verify(x => x.GetAll(), Times.AtLeastOnce);

// Verify method was called exactly N times
mock.Verify(x => x.Save(), Times.Exactly(3));

// Verify all setups were invoked
mock.VerifyAll();

// Verify no other methods were called
mock.VerifyNoOtherCalls();
```

---

## Best Practices for Mocking

### Focus on Behavior

Mock only the interactions you need to control in your test. Avoid over-mocking.

### Loose Coupling

Design your classes with dependency injection in mind, making it easy to swap out real dependencies for mocks.

### Verification (Optional)

Use `Verify` to ensure that your code interacts with the mock as expected.

### Readability

Strive for clear and expressive setup and verification code.

---

## Things to Avoid

- **Mocking Everything**: Don't mock classes that you are testing directly
- **Excessive Setup**: Avoid overly complex setups that obscure the intent of your tests
- **Verifying Implementation Details**: Focus on verifying behavior, not specific implementation details
- **Mocking Concrete Classes**: Prefer mocking interfaces over concrete classes

---

## Integration Tests

While unit tests focus on individual units in isolation, integration tests examine how different parts of your application work together. In ASP.NET Core MVC, this typically involves testing the interaction between controllers, views, services, and sometimes even external dependencies like databases or APIs.

### Why Integration Tests Matter

- **Real-World Scenarios**: Integration tests simulate real user interactions, revealing potential issues that might not be caught by unit tests
- **End-to-End Testing**: They help you verify that the entire flow of a request works correctly
- **Database Interaction Testing**: Integration tests can test how your application interacts with a real (or in-memory) database
- **Confidence in Deployment**: A strong suite of integration tests boosts your confidence when deploying your application

---

## Key Elements of Integration Tests with xUnit

### Test Server

You create a test server instance using a custom `WebApplicationFactory`, which allows you to simulate your application's behavior in a test environment.

### HTTP Client

You use an `HttpClient` to send HTTP requests to the test server, mimicking how a real client would interact with your application.

### Assertions

Use assertions (e.g., from FluentAssertions or xUnit's built-in assertions) to validate the responses received from the server.

---

## Best Practices for Integration Tests

### Focus on Integration

Test the interactions between components, not the isolated behavior of individual units.

### Database

- **In-Memory**: Use an in-memory database (e.g., `UseInMemoryDatabase`) for faster tests and data isolation
- **Real Database (Optional)**: For more realistic testing, use a test database with a separate schema or dataset

### Test Environment

Configure your test server to use a "Test" environment to avoid accidentally affecting your development or production databases.

### Clean Up

If you're using a real database, ensure you clean up the test data after each test or test class to maintain data consistency.

### Avoid External Dependencies

If your application relies on external APIs or services, consider mocking or stubbing them for integration tests.

### Clear Test Names

Use descriptive names that explain the purpose and expected behavior of each test.

---

## Code Example - Integration Tests

### CustomWebApplicationFactory

```csharp
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
 
        builder.UseEnvironment("Test"); // Set the environment to "Test"
 
        builder.ConfigureServices(services => {
            // Replace the default DbContext configuration with an in-memory database
            var descriptor = services.SingleOrDefault(
                temp => temp.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
 
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }
            
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("DatabaseForTesting");
            });
        });
    }
}
```

**Explanation**:
- Inherits from `WebApplicationFactory<Program>`: Provides core functionality for creating a test server
- `ConfigureWebHost Override`: Customizes the configuration of the test server
- `UseEnvironment("Test")`: Sets the ASPNETCORE_ENVIRONMENT variable to "Test"
- `ConfigureServices`: Replaces the default database context with an in-memory database

### PersonsControllerIntegrationTest

```csharp
public class PersonsControllerIntegrationTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
 
    // Constructor injection of the custom factory
    public PersonsControllerIntegrationTest(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }
 
    #region Index
 
    [Fact]
    public async Task Index_ToReturnView()
    {
        // Act: Send a GET request to the "/Persons/Index" endpoint
        HttpResponseMessage response = await _client.GetAsync("/Persons/Index");
 
        // Assert:
        // 1. Check if the response was successful (status code 2xx)
        response.Should().BeSuccessful();
 
        // 2. Read the response content as a string
        string responseBody = await response.Content.ReadAsStringAsync();
 
        // 3. Parse the HTML content using HtmlAgilityPack
        HtmlDocument html = new HtmlDocument();
        html.LoadHtml(responseBody);
        var document = html.DocumentNode;
 
        // 4. Assert that the response contains a table with the class "persons"
        document.QuerySelectorAll("table.persons").Should().NotBeNull();
    }
    
    [Fact]
    public async Task Create_ValidPerson_RedirectsToIndex()
    {
        // Arrange
        var formData = new Dictionary<string, string>
        {
            { "PersonName", "John Doe" },
            { "Email", "john@example.com" },
            { "DateOfBirth", "1990-01-01" }
        };
        
        var content = new FormUrlEncodedContent(formData);
        
        // Act
        HttpResponseMessage response = await _client.PostAsync("/Persons/Create", content);
        
        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Redirect);
        response.Headers.Location.ToString().Should().Contain("/Persons/Index");
    }
 
    #endregion
}
```

**Explanation**:
- `IClassFixture<CustomWebApplicationFactory>`: Tells xUnit to create a single instance of the factory and share it among all tests
- **Constructor Injection**: Receives the factory and creates an `HttpClient`
- **Index_ToReturnView Test**:
  - Sends a GET request to the Persons/Index endpoint
  - Checks if the response is successful
  - Parses the HTML response using HtmlAgilityPack
  - Asserts that the response contains the expected table element
- **Create_ValidPerson_RedirectsToIndex Test**:
  - Creates form data for a new person
  - Sends a POST request
  - Verifies redirect to Index page

---

## Testing Different Scenarios

### Testing JSON Responses

```csharp
[Fact]
public async Task GetPerson_ValidId_ReturnsJson()
{
    // Act
    HttpResponseMessage response = await _client.GetAsync("/api/persons/1");
    
    // Assert
    response.Should().BeSuccessful();
    response.Content.Headers.ContentType.MediaType.Should().Be("application/json");
    
    string json = await response.Content.ReadAsStringAsync();
    var person = JsonSerializer.Deserialize<PersonResponse>(json);
    
    person.Should().NotBeNull();
    person.PersonID.Should().Be(1);
}
```

### Testing Authentication

```csharp
[Fact]
public async Task ProtectedEndpoint_WithoutAuth_ReturnsUnauthorized()
{
    // Act
    HttpResponseMessage response = await _client.GetAsync("/Persons/Delete/1");
    
    // Assert
    response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
}

[Fact]
public async Task ProtectedEndpoint_WithAuth_ReturnsSuccess()
{
    // Arrange
    _client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", "valid-token");
    
    // Act
    HttpResponseMessage response = await _client.GetAsync("/Persons/Delete/1");
    
    // Assert
    response.Should().BeSuccessful();
}
```

### Testing Form Validation

```csharp
[Fact]
public async Task Create_InvalidPerson_ReturnsValidationErrors()
{
    // Arrange
    var formData = new Dictionary<string, string>
    {
        { "PersonName", "" }, // Empty name should fail validation
        { "Email", "invalid-email" } // Invalid email format
    };
    
    var content = new FormUrlEncodedContent(formData);
    
    // Act
    HttpResponseMessage response = await _client.PostAsync("/Persons/Create", content);
    
    // Assert
    string responseBody = await response.Content.ReadAsStringAsync();
    responseBody.Should().Contain("PersonName is required");
    responseBody.Should().Contain("Invalid email format");
}
```

---

## Key Points to Remember

### xUnit Advanced Topics

**[Theory] and [InlineData]**:
- `[Theory]` marks a test method that should be executed with multiple data sets
- `[InlineData(...)]` provides the data sets to use for the test

**[ClassFixture]**:
- Shares a fixture instance across all tests in a class
- Improves performance by avoiding redundant setup/teardown

**Custom Assertions**: Create your own assertions by extending the `Xunit.Assert` class

**Test Collections**: Group related tests using `[Collection]` and `[CollectionDefinition]` attributes

**Parallelization**: xUnit can run tests in parallel to improve execution speed

---

### Mocking (Moq)

**Purpose**: Isolate the unit under test by simulating the behavior of dependencies

**Key Methods**:
- `Setup(expression)`: Configures how a mock method should behave
- `Returns(value)` or `ReturnsAsync(value)`: Specifies the return value
- `Throws(exception)` or `ThrowsAsync(exception)`: Simulates an exception
- `Verify(expression, times)`: Ensures a method was called the expected number of times

**Best Practices**:
- Mock only what's necessary
- Design for dependency injection
- Use clear and expressive setup and verification code

---

### AutoFixture

**Purpose**: Automatically generates test data for your classes

**Key Features**:
- `[AutoData]` attribute: Provides auto-generated instances to test methods
- Customization: Control how data is generated using customizations and builders

**Benefits**:
- Saves time writing test data
- Encourages testing with a variety of inputs

---

### FluentAssertions

**Purpose**: Provides a more fluent and readable syntax for assertions

**Key Features**:
- Method chaining for expressive assertions (e.g., `result.Should().Be(5);`)
- Rich API with assertions for various scenarios (collections, exceptions, strings, etc.)

---

### Repository Implementation & Unit Testing

**Purpose**: Repositories handle data access logic

**Interfaces**: Define interfaces (e.g., `IPersonsRepository`) to abstract data access and facilitate mocking

**Unit Tests**:
- Focus on testing the repository's logic in isolation
- Use mocks for database interactions
- Cover all CRUD operations and edge cases

---

### Controller Unit Testing

**Purpose**: Test controller actions and their interactions with services and models

**Mock Services**: Use mocks to isolate controllers from external dependencies

**Test Scenarios**:
- Verify correct action results are returned
- Check if the controller interacts with services as expected
- Test model validation and error handling

---

### Integration Tests

**Purpose**: Test how multiple components work together

**WebApplicationFactory**: Create a test server instance to simulate real requests

**HttpClient**: Use an HTTP client to send requests to the test server

**In-Memory Database**: Often use an in-memory database for testing

**Test Environment**: Set the ASPNETCORE_ENVIRONMENT to "Test"

---

## Comparison Table: Unit Tests vs Integration Tests

| Aspect | Unit Tests | Integration Tests |
|--------|-----------|-------------------|
| **Scope** | Single unit (method/class) | Multiple components |
| **Dependencies** | Mocked/stubbed | Real or partially real |
| **Speed** | Fast (milliseconds) | Slower (seconds) |
| **Database** | Never touched | In-memory or test DB |
| **Setup Complexity** | Simple | Complex |
| **Purpose** | Test logic in isolation | Test interactions |
| **Frequency** | Run constantly | Run less frequently |
| **Maintenance** | Easier | More complex |

---

## Interview Tips

### Demonstrate Understanding

Explain the purpose and benefits of each tool and technique:
- Why use Fluent Assertions over standard assertions?
- When should you use mocking vs real implementations?
- What's the difference between unit and integration tests?

### Code Examples

Be prepared to write or analyze code snippets showcasing these concepts:
- Write a mock setup for a repository
- Create an integration test for a controller action
- Use Fluent Assertions to verify complex conditions

### Best Practices

Discuss the best practices for each topic:
- AAA pattern (Arrange-Act-Assert)
- Keep tests isolated and independent
- Use descriptive test names
- Don't over-mock
- Clean up test data

### Real-World Scenarios

Connect these concepts to real-world testing challenges:
- How would you test a payment processing system?
- How do you handle external API dependencies in tests?
- What's your strategy for testing database transactions?
- How do you ensure tests run quickly in CI/CD pipelines?

### Common Pitfalls

Be ready to discuss common mistakes:
- Testing implementation details instead of behavior
- Brittle tests that break with minor code changes
- Slow integration tests that block development
- Not cleaning up test data properly
- Over-mocking leading to tests that don't catch real issues