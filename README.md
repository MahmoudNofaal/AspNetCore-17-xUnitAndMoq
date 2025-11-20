
```csharp
// Setup - Define# ASP.NET Core - Advanced Testing Guide

## Table of Contents
1. [Introduction](#introduction)
2. [Testing Fundamentals](#testing-fundamentals)
3. [Fluent Assertions](#fluent-assertions)
4. [Mocking with Moq](#mocking-with-moq)
5. [AutoFixture](#autofixture)
6. [Unit Testing](#unit-testing)
7. [Integration Testing](#integration-testing)
8. [Best Practices](#best-practices)
9. [Interview Preparation](#interview-preparation)

---

## Introduction

### What is Testing?

Testing is the practice of verifying that your code works as expected. In ASP.NET Core, we use automated tests to:

- Catch bugs early in development
- Document expected behavior
- Enable safe refactoring
- Improve code quality
- Build confidence in deployments

### Types of Tests

**Unit Tests**
- Test individual components in isolation
- Fast and focused
- Use mocking for dependencies
- Run frequently during development

**Integration Tests**
- Test how components work together
- Slower but more realistic
- May use real or in-memory databases
- Validate end-to-end scenarios

### Testing Stack

This guide covers four essential tools:

| Tool | Purpose |
|------|---------|
| **xUnit** | Test framework (test runner and assertions) |
| **Fluent Assertions** | Readable, expressive assertions |
| **Moq** | Mocking framework for dependencies |
| **AutoFixture** | Automatic test data generation |

---

## Testing Fundamentals

### The AAA Pattern

Every good test follows the **Arrange-Act-Assert** pattern:

```csharp
[Fact]
public async Task AddPerson_ValidPerson_ReturnsPersonResponse()
{
    // Arrange - Set up test data and dependencies
    var person = new Person { PersonName = "John Doe", Email = "john@example.com" };
    var service = new PersonsService(mockRepository.Object);
    
    // Act
    var result = await _service.GetPersonByID(person.PersonID);

    // Assert
    result.Should().NotBeNull();
    result!.PersonID.Should().Be(person.PersonID);
    result.PersonName.Should().Be(person.PersonName);
    
    _mockRepository.Verify(
        r => r.GetPersonByID(person.PersonID), 
        Times.Once);
}

[Fact]
public async Task GetPersonByID_InvalidID_ReturnsNull()
{
    // Arrange
    var invalidId = Guid.NewGuid();
    
    _mockRepository
        .Setup(r => r.GetPersonByID(invalidId))
        .ReturnsAsync((Person?)null);

    // Act
    var result = await _service.GetPersonByID(invalidId);

    // Assert
    result.Should().BeNull();
    
    _mockRepository.Verify(
        r => r.GetPersonByID(invalidId), 
        Times.Once);
}
```

#### Update Tests

```csharp
[Fact]
public async Task UpdatePerson_ValidPerson_ReturnsUpdatedPerson()
{
    // Arrange
    var existingPerson = _fixture.Create<Person>();
    var updateRequest = new PersonUpdateRequest
    {
        PersonID = existingPerson.PersonID,
        PersonName = "Updated Name",
        Email = existingPerson.Email
    };

    _mockRepository
        .Setup(r => r.GetPersonByID(existingPerson.PersonID))
        .ReturnsAsync(existingPerson);
    
    _mockRepository
        .Setup(r => r.UpdatePerson(It.IsAny<Person>()))
        .ReturnsAsync(existingPerson);

    // Act
    var result = await _service.UpdatePerson(updateRequest);

    // Assert
    result.Should().NotBeNull();
    result.PersonID.Should().Be(existingPerson.PersonID);
    
    _mockRepository.Verify(
        r => r.UpdatePerson(It.IsAny<Person>()), 
        Times.Once);
}

[Fact]
public async Task UpdatePerson_NonExistentID_ThrowsArgumentException()
{
    // Arrange
    var updateRequest = _fixture.Create<PersonUpdateRequest>();
    
    _mockRepository
        .Setup(r => r.GetPersonByID(updateRequest.PersonID))
        .ReturnsAsync((Person?)null);

    // Act
    Func<Task> act = async () => await _service.UpdatePerson(updateRequest);

    // Assert
    await act.Should().ThrowAsync<ArgumentException>()
        .WithMessage("*doesn't exist*");
    
    _mockRepository.Verify(
        r => r.UpdatePerson(It.IsAny<Person>()), 
        Times.Never);
}
```

#### Delete Tests

```csharp
[Fact]
public async Task DeletePerson_ValidID_ReturnsTrue()
{
    // Arrange
    var person = _fixture.Create<Person>();
    
    _mockRepository
        .Setup(r => r.GetPersonByID(person.PersonID))
        .ReturnsAsync(person);
    
    _mockRepository
        .Setup(r => r.DeletePerson(person.PersonID))
        .ReturnsAsync(true);

    // Act
    var result = await _service.DeletePerson(person.PersonID);

    // Assert
    result.Should().BeTrue();
    
    _mockRepository.Verify(
        r => r.DeletePerson(person.PersonID), 
        Times.Once);
}

[Fact]
public async Task DeletePerson_InvalidID_ReturnsFalse()
{
    // Arrange
    var invalidId = Guid.NewGuid();
    
    _mockRepository
        .Setup(r => r.GetPersonByID(invalidId))
        .ReturnsAsync((Person?)null);
    
    _mockRepository
        .Setup(r => r.DeletePerson(invalidId))
        .ReturnsAsync(false);

    // Act
    var result = await _service.DeletePerson(invalidId);

    // Assert
    result.Should().BeFalse();
}
```

### Testing Controller Actions

```csharp
public class PersonsControllerTest
{
    private readonly Mock<IPersonsService> _mockService;
    private readonly Mock<ICountriesService> _mockCountriesService;
    private readonly PersonsController _controller;
    private readonly IFixture _fixture;

    public PersonsControllerTest()
    {
        _fixture = new Fixture();
        _mockService = new Mock<IPersonsService>();
        _mockCountriesService = new Mock<ICountriesService>();
        _controller = new PersonsController(_mockService.Object, _mockCountriesService.Object);
    }

    [Fact]
    public async Task Index_ReturnsViewWithPersons()
    {
        // Arrange
        var persons = _fixture.CreateMany<PersonResponse>(3).ToList();
        
        _mockService
            .Setup(s => s.GetFilteredPersons(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(persons);
        
        _mockService
            .Setup(s => s.GetSortedPersons(It.IsAny<List<PersonResponse>>(), 
                It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(persons);

        // Act
        var result = await _controller.Index();

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeAssignableTo<List<PersonResponse>>().Subject;
        model.Should().HaveCount(3);
    }

    [Fact]
    public async Task Create_GET_ReturnsView()
    {
        // Arrange
        var countries = _fixture.CreateMany<CountryResponse>(3).ToList();
        
        _mockCountriesService
            .Setup(s => s.GetAllCountries())
            .ReturnsAsync(countries);

        // Act
        var result = await _controller.Create();

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        _controller.ViewBag.Countries.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_POST_ValidModel_RedirectsToIndex()
    {
        // Arrange
        var personRequest = _fixture.Create<PersonAddRequest>();
        var personResponse = _fixture.Create<PersonResponse>();
        
        _mockService
            .Setup(s => s.AddPerson(personRequest))
            .ReturnsAsync(personResponse);

        // Act
        var result = await _controller.Create(personRequest);

        // Assert
        var redirectResult = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirectResult.ActionName.Should().Be("Index");
        
        _mockService.Verify(
            s => s.AddPerson(It.IsAny<PersonAddRequest>()), 
            Times.Once);
    }

    [Fact]
    public async Task Create_POST_InvalidModel_ReturnsViewWithErrors()
    {
        // Arrange
        var personRequest = _fixture.Create<PersonAddRequest>();
        var countries = _fixture.CreateMany<CountryResponse>(3).ToList();
        
        _mockCountriesService
            .Setup(s => s.GetAllCountries())
            .ReturnsAsync(countries);
        
        _controller.ModelState.AddModelError("PersonName", "Name is required");

        // Act
        var result = await _controller.Create(personRequest);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().Be(personRequest);
        
        _mockService.Verify(
            s => s.AddPerson(It.IsAny<PersonAddRequest>()), 
            Times.Never);
    }

    [Fact]
    public async Task Delete_GET_ValidID_ReturnsViewWithPerson()
    {
        // Arrange
        var person = _fixture.Create<PersonResponse>();
        
        _mockService
            .Setup(s => s.GetPersonByID(person.PersonID))
            .ReturnsAsync(person);

        // Act
        var result = await _controller.Delete(person.PersonID);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<PersonResponse>().Subject;
        model.PersonID.Should().Be(person.PersonID);
    }

    [Fact]
    public async Task Delete_POST_ValidID_RedirectsToIndex()
    {
        // Arrange
        var personId = Guid.NewGuid();
        
        _mockService
            .Setup(s => s.DeletePerson(personId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(new PersonUpdateRequest { PersonID = personId });

        // Assert
        var redirectResult = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirectResult.ActionName.Should().Be("Index");
        
        _mockService.Verify(
            s => s.DeletePerson(personId), 
            Times.Once);
    }
}
```

### Testing Search and Filter Logic

```csharp
[Theory]
[InlineData("PersonName", "John")]
[InlineData("Email", "test@")]
[InlineData("Country", "USA")]
public async Task GetFilteredPersons_DifferentSearchFields_ReturnsFilteredResults(
    string searchBy, 
    string searchString)
{
    // Arrange
    var persons = new List<Person>
    {
        new Person { PersonName = "John Doe", Email = "john@example.com", Country = new Country { CountryName = "USA" } },
        new Person { PersonName = "Jane Smith", Email = "jane@test.com", Country = new Country { CountryName = "Canada" } }
    };
    
    _mockRepository
        .Setup(r => r.GetFilteredPersons(searchBy, searchString))
        .ReturnsAsync(persons.Where(p => 
            (searchBy == "PersonName" && p.PersonName.Contains(searchString)) ||
            (searchBy == "Email" && p.Email.Contains(searchString)) ||
            (searchBy == "Country" && p.Country.CountryName.Contains(searchString))
        ).ToList());

    // Act
    var result = await _service.GetFilteredPersons(searchBy, searchString);

    // Assert
    result.Should().NotBeEmpty();
    result.Should().AllSatisfy(p => 
    {
        var matchesSearch = searchBy switch
        {
            "PersonName" => p.PersonName.Should().Contain(searchString),
            "Email" => p.Email.Should().Contain(searchString),
            "Country" => p.Country.Should().Contain(searchString),
            _ => throw new ArgumentException("Invalid search field")
        };
    });
}
```

---

## Integration Testing

### What are Integration Tests?

Integration tests verify that multiple components work together correctly. They test:

- Controller → Service → Repository flow
- Database interactions
- HTTP request/response handling
- View rendering
- End-to-end scenarios

### Key Differences from Unit Tests

| Aspect | Unit Tests | Integration Tests |
|--------|-----------|-------------------|
| **Scope** | Single component | Multiple components |
| **Dependencies** | Mocked | Real or in-memory |
| **Database** | Never used | In-memory or test DB |
| **Speed** | Very fast (ms) | Slower (seconds) |
| **Setup** | Simple | Complex |
| **Purpose** | Logic verification | Component interaction |

### Setting Up Integration Tests

#### Required Packages

```bash
dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add package Microsoft.EntityFrameworkCore.InMemory
dotnet add package HtmlAgilityPack
```

#### Custom Web Application Factory

```csharp
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
 
        // Set test environment
        builder.UseEnvironment("Test");
 
        builder.ConfigureServices(services =>
        {
            // Remove real database context
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
 
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }
            
            // Add in-memory database
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("DatabaseForTesting");
            });
            
            // Build service provider
            var serviceProvider = services.BuildServiceProvider();
            
            // Seed test data
            using (var scope = serviceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                SeedTestData(db);
            }
        });
    }
    
    private void SeedTestData(ApplicationDbContext context)
    {
        // Clear existing data
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        
        // Add test data
        var countries = new List<Country>
        {
            new Country { CountryID = Guid.NewGuid(), CountryName = "USA" },
            new Country { CountryID = Guid.NewGuid(), CountryName = "Canada" }
        };
        
        context.Countries.AddRange(countries);
        context.SaveChanges();
    }
}
```

### Writing Integration Tests

#### Test Class Setup

```csharp
public class PersonsControllerIntegrationTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public PersonsControllerIntegrationTest(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }
}
```

#### Testing GET Requests

```csharp
[Fact]
public async Task Index_ReturnsSuccessAndCorrectContentType()
{
    // Act
    var response = await _client.GetAsync("/Persons/Index");

    // Assert
    response.Should().BeSuccessful();
    response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");
}

[Fact]
public async Task Index_ReturnsViewWithTable()
{
    // Act
    var response = await _client.GetAsync("/Persons/Index");
    string responseBody = await response.Content.ReadAsStringAsync();

    // Parse HTML
    var html = new HtmlDocument();
    html.LoadHtml(responseBody);
    var document = html.DocumentNode;

    // Assert
    response.Should().BeSuccessful();
    document.QuerySelectorAll("table.persons").Should().NotBeEmpty();
    document.QuerySelectorAll("th").Should().NotBeEmpty();
}

[Fact]
public async Task Details_ValidID_ReturnsPersonDetails()
{
    // Arrange
    var personId = await CreateTestPerson();

    // Act
    var response = await _client.GetAsync($"/Persons/Details/{personId}");
    string responseBody = await response.Content.ReadAsStringAsync();

    // Assert
    response.Should().BeSuccessful();
    responseBody.Should().Contain("John Doe");
    responseBody.Should().Contain("john@example.com");
}

[Fact]
public async Task Details_InvalidID_ReturnsNotFound()
{
    // Arrange
    var invalidId = Guid.NewGuid();

    // Act
    var response = await _client.GetAsync($"/Persons/Details/{invalidId}");

    // Assert
    response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
}
```

#### Testing POST Requests

```csharp
[Fact]
public async Task Create_ValidPerson_RedirectsToIndex()
{
    // Arrange
    var countryId = await GetFirstCountryId();
    var formData = new Dictionary<string, string>
    {
        { "PersonName", "John Doe" },
        { "Email", "john@example.com" },
        { "DateOfBirth", "1990-01-01" },
        { "Gender", "Male" },
        { "CountryID", countryId.ToString() },
        { "Address", "123 Main St" },
        { "ReceiveNewsLetters", "true" }
    };
    
    var content = new FormUrlEncodedContent(formData);

    // Act
    var response = await _client.PostAsync("/Persons/Create", content);

    // Assert
    response.StatusCode.Should().Be(System.Net.HttpStatusCode.Redirect);
    response.Headers.Location?.ToString().Should().Contain("/Persons/Index");
    
    // Verify person was created
    var indexResponse = await _client.GetAsync("/Persons/Index");
    var indexBody = await indexResponse.Content.ReadAsStringAsync();
    indexBody.Should().Contain("John Doe");
}

[Fact]
public async Task Create_InvalidData_ReturnsViewWithErrors()
{
    // Arrange - Missing required fields
    var formData = new Dictionary<string, string>
    {
        { "PersonName", "" }, // Empty name
        { "Email", "invalid-email" } // Invalid format
    };
    
    var content = new FormUrlEncodedContent(formData);

    // Act
    var response = await _client.PostAsync("/Persons/Create", content);
    string responseBody = await response.Content.ReadAsStringAsync();

    // Assert
    response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    responseBody.Should().Contain("validation");
}

[Fact]
public async Task Edit_ValidData_UpdatesPersonAndRedirects()
{
    // Arrange
    var personId = await CreateTestPerson();
    var formData = new Dictionary<string, string>
    {
        { "PersonID", personId.ToString() },
        { "PersonName", "Updated Name" },
        { "Email", "updated@example.com" },
        { "DateOfBirth", "1990-01-01" }
    };
    
    var content = new FormUrlEncodedContent(formData);

    // Act
    var response = await _client.PostAsync($"/Persons/Edit/{personId}", content);

    // Assert
    response.StatusCode.Should().Be(System.Net.HttpStatusCode.Redirect);
    
    // Verify update
    var detailsResponse = await _client.GetAsync($"/Persons/Details/{personId}");
    var detailsBody = await detailsResponse.Content.ReadAsStringAsync();
    detailsBody.Should().Contain("Updated Name");
}

[Fact]
public async Task Delete_ValidID_RemovesPersonAndRedirects()
{
    // Arrange
    var personId = await CreateTestPerson();

    // Act
    var response = await _client.PostAsync(
        $"/Persons/Delete/{personId}", 
        new StringContent(""));

    // Assert
    response.StatusCode.Should().Be(System.Net.HttpStatusCode.Redirect);
    
    // Verify deletion
    var detailsResponse = await _client.GetAsync($"/Persons/Details/{personId}");
    detailsResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
}
```

#### Testing Search and Filter

```csharp
[Theory]
[InlineData("PersonName", "John")]
[InlineData("Email", "test")]
public async Task Index_WithSearchParameters_ReturnsFilteredResults(
    string searchBy, 
    string searchString)
{
    // Arrange
    await CreateTestPerson("John Doe", "john@test.com");
    await CreateTestPerson("Jane Smith", "jane@example.com");

    // Act
    var response = await _client.GetAsync(
        $"/Persons/Index?searchBy={searchBy}&searchString={searchString}");
    string responseBody = await response.Content.ReadAsStringAsync();

    // Assert
    response.Should().BeSuccessful();
    
    if (searchString == "John")
        responseBody.Should().Contain("John Doe");
    if (searchString == "test")
        responseBody.Should().Contain("john@test.com");
}

[Fact]
public async Task Index_WithSorting_ReturnsSortedResults()
{
    // Arrange
    await CreateTestPerson("Zoe Wilson", "zoe@example.com");
    await CreateTestPerson("Alice Brown", "alice@example.com");
    await CreateTestPerson("Mark Davis", "mark@example.com");

    // Act
    var response = await _client.GetAsync(
        "/Persons/Index?sortBy=PersonName&sortOrder=ASC");
    string responseBody = await response.Content.ReadAsStringAsync();

    // Assert
    response.Should().BeSuccessful();
    
    // Check order (Alice should appear before Mark and Zoe)
    var aliceIndex = responseBody.IndexOf("Alice Brown");
    var markIndex = responseBody.IndexOf("Mark Davis");
    var zoeIndex = responseBody.IndexOf("Zoe Wilson");
    
    aliceIndex.Should().BeLessThan(markIndex);
    markIndex.Should().BeLessThan(zoeIndex);
}
```

#### Testing API Endpoints

```csharp
[Fact]
public async Task GetPersonsAPI_ReturnsJsonWithPersons()
{
    // Arrange
    await CreateTestPerson();

    // Act
    var response = await _client.GetAsync("/api/Persons");
    
    // Assert
    response.Should().BeSuccessful();
    response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    
    var json = await response.Content.ReadAsStringAsync();
    var persons = JsonSerializer.Deserialize<List<PersonResponse>>(json, 
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    
    persons.Should().NotBeEmpty();
}
```

#### Helper Methods

```csharp
private async Task<Guid> CreateTestPerson(
    string name = "John Doe", 
    string email = "john@example.com")
{
    var countryId = await GetFirstCountryId();
    var formData = new Dictionary<string, string>
    {
        { "PersonName", name },
        { "Email", email },
        { "DateOfBirth", "1990-01-01" },
        { "CountryID", countryId.ToString() }
    };
    
    var content = new FormUrlEncodedContent(formData);
    var response = await _client.PostAsync("/Persons/Create", content);
    
    // Extract ID from redirect location or return value
    // Implementation depends on your redirect pattern
    return Guid.NewGuid(); // Simplified
}

private async Task<Guid> GetFirstCountryId()
{
    using var scope = _factory.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var country = await context.Countries.FirstAsync();
    return country.CountryID;
}
```

### Testing Authentication & Authorization

```csharp
[Fact]
public async Task ProtectedEndpoint_WithoutAuth_ReturnsUnauthorized()
{
    // Act
    var response = await _client.GetAsync("/Persons/Delete/1");

    // Assert
    response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
}

[Fact]
public async Task ProtectedEndpoint_WithValidToken_ReturnsSuccess()
{
    // Arrange
    var token = await GetAuthToken("testuser", "password");
    _client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", token);

    // Act
    var response = await _client.GetAsync("/Persons/Delete/1");

    // Assert
    response.Should().BeSuccessful();
}
```

---

## Best Practices

### General Testing Principles

#### 1. Follow AAA Pattern

```csharp
[Fact]
public async Task TestMethod()
{
    // Arrange - Setup
    var data = CreateTestData();
    
    // Act - Execute
    var result = await _service.ProcessData(data);
    
    // Assert - Verify
    result.Should().NotBeNull();
}
```

#### 2. One Assert Per Test (Ideally)

```csharp
// ✅ Good - Focused test
[Fact]
public async Task AddPerson_ValidPerson_ReturnsCorrectName()
{
    var person = await _service.AddPerson(request);
    person.PersonName.Should().Be("John Doe");
}

// ⚠️ Acceptable - Related assertions
[Fact]
public async Task AddPerson_ValidPerson_ReturnsCompleteResponse()
{
    var person = await _service.AddPerson(request);
    person.PersonID.Should().NotBeEmpty();
    person.PersonName.Should().Be("John Doe");
    person.Email.Should().Be("john@example.com");
}

// ❌ Bad - Testing multiple concerns
[Fact]
public async Task TestEverything()
{
    // Tests add, update, delete all in one
}
```

#### 3. Test Independence

```csharp
// ✅ Good - Each test is independent
[Fact]
public async Task Test1()
{
    var person = CreateTestPerson(); // Own data
    // Test logic
}

[Fact]
public async Task Test2()
{
    var person = CreateTestPerson(); // Own data
    // Test logic
}

// ❌ Bad - Tests depend on each other
private static Person _sharedPerson;

[Fact]
public async Task Test1_CreatesPerson()
{
    _sharedPerson = await _service.AddPerson(request);
}

[Fact]
public async Task Test2_UpdatesPerson() // Depends on Test1
{
    await _service.UpdatePerson(_sharedPerson);
}
```

#### 4. Descriptive Test Names

```csharp
// ✅ Good - Clear what's being tested
[Fact]
public async Task AddPerson_NullRequest_ThrowsArgumentNullException()

[Fact]
public async Task GetAllPersons_EmptyDatabase_ReturnsEmptyList()

[Fact]
public async Task DeletePerson_NonExistentID_ReturnsFalse()

// ❌ Bad - Unclear purpose
[Fact]
public async Task Test1()

[Fact]
public async Task TestAddPerson()

[Fact]
public async Task ShouldWork()
```

### Unit Testing Best Practices

✅ **Do**

- **Mock only external dependencies**
```csharp
// ✅ Mock repository (external)
var mockRepo = new Mock<IPersonsRepository>();

// ❌ Don't mock the class you're testing
var mockService = new Mock<PersonsService>(); // Wrong!
```

- **Verify important interactions**
```csharp
_mockRepository.Verify(
    r => r.AddPerson(It.IsAny<Person>()), 
    Times.Once);
```

- **Test edge cases**
```csharp
[Theory]
[InlineData(null)]
[InlineData("")]
[InlineData("   ")]
public async Task AddPerson_InvalidName_ThrowsException(string name)
```

- **Use realistic test data**
```csharp
// ✅ Good
var person = new Person 
{ 
    PersonName = "John Doe",
    Email = "john@example.com",
    DateOfBirth = new DateTime(1990, 1, 1)
};

// ❌ Bad
var person = new Person 
{ 
    PersonName = "Test",
    Email = "test",
    DateOfBirth = DateTime.MinValue
};
```

❌ **Don't**

- **Over-mock** - Mock only what's necessary
- **Test implementation details** - Focus on behavior
- **Write brittle tests** - Avoid testing exact strings unless necessary
- **Skip negative tests** - Test failure scenarios too
- **Ignore async/await** - Always use async properly

### Integration Testing Best Practices

✅ **Do**

- **Use in-memory database for speed**
```csharp
options.UseInMemoryDatabase("TestDatabase");
```

- **Clean up between tests**
```csharp
public async Task InitializeAsync()
{
    await _context.Database.EnsureDeletedAsync();
    await _context.Database.EnsureCreatedAsync();
}
```

- **Test realistic scenarios**
```csharp
[Fact]
public async Task CompleteUserJourney_CreateEditDelete_WorksCorrectly()
{
    // Create
    var createResponse = await CreatePerson();
    
    // Edit
    var editResponse = await EditPerson(personId);
    
    // Delete
    var deleteResponse = await DeletePerson(personId);
}
```

- **Verify HTTP responses**
```csharp
response.Should().BeSuccessful();
response.StatusCode.Should().Be(HttpStatusCode.OK);
response.Headers.Location.Should().NotBeNull();
```

- **Test actual database operations**
```csharp
// Verify data was actually saved
using var scope = _factory.Services.CreateScope();
var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
var person = await context.Persons.FindAsync(personId);
person.Should().NotBeNull();
```

❌ **Don't**

- **Test everything as integration** - Use unit tests for logic
- **Use production database** - Always use test database
- **Create dependencies between tests** - Keep tests independent
- **Ignore performance** - Integration tests should still be reasonably fast
- **Skip cleanup** - Always clean up test data

### Mocking Best Practices

✅ **Do**

- **Setup only what you need**
```csharp
// ✅ Good - Specific setup
_mockRepository
    .Setup(r => r.GetById(1))
    .Returns(person);

// ❌ Bad - Over-setup
_mockRepository.Setup(r => r.GetById(It.IsAny<int>())).Returns(person);
_mockRepository.Setup(r => r.GetAll()).Returns(list);
_mockRepository.Setup(r => r.Add(It.IsAny<Person>())).Returns(person);
// ... unnecessary setups
```

- **Use appropriate parameter matchers**
```csharp
// Specific value
.Setup(r => r.GetById(1))

// Any value
.Setup(r => r.GetById(It.IsAny<int>()))

// Conditional
.Setup(r => r.GetById(It.Is<int>(id => id > 0)))
```

- **Verify critical operations**
```csharp
_mockRepository.Verify(
    r => r.Save(), 
    Times.Once);
```

❌ **Don't**

- **Mock concrete classes** - Mock interfaces instead
- **Over-verify** - Don't verify every interaction
- **Create complex mock setups** - Keep it simple
- **Reuse mocks across unrelated tests** - Create fresh mocks

### Performance Considerations

**Unit Tests**
- Should run in milliseconds
- No I/O operations
- No database access
- No external dependencies

**Integration Tests**
- Should run in seconds
- Use in-memory database when possible
- Minimize external API calls
- Clean up efficiently

```csharp
// ✅ Fast - In-memory database
options.UseInMemoryDatabase("TestDb");

// ⚠️ Slower - Real database (use sparingly)
options.UseSqlServer(connectionString);
```


### Test Naming Convention

Use descriptive names that explain:
1. What you're testing
2. Under what conditions
3. What the expected result is

**Format**: `MethodName_Scenario_ExpectedBehavior`

```csharp
// ✅ Good - Clear and descriptive
[Fact]
public async Task AddPerson_NullRequest_ThrowsArgumentNullException()

[Fact]
public async Task GetAllPersons_EmptyDatabase_ReturnsEmptyList()

[Fact]
public async Task DeletePerson_ValidId_ReturnsTrue()

// ❌ Bad - Unclear purpose
[Fact]
public async Task Test1()

[Fact]
public async Task TestAddPerson()
```

### xUnit Attributes

**[Fact]**
- Single test with no parameters
- Runs once

```csharp
[Fact]
public void SimpleMathTest()
{
    int result = 2 + 2;
    Assert.Equal(4, result);
}
```

**[Theory]** with **[InlineData]**
- Parameterized test
- Runs multiple times with different inputs

```csharp
[Theory]
[InlineData(2, 2, 4)]
[InlineData(5, 3, 8)]
[InlineData(-1, 1, 0)]
public void Add_DifferentNumbers_ReturnsCorrectSum(int a, int b, int expected)
{
    int result = a + b;
    Assert.Equal(expected, result);
}
```

**[ClassFixture]**
- Shares setup across all tests in a class
- Created once, disposed after all tests complete

```csharp
public class PersonsServiceTest : IClassFixture<TestFixture>
{
    private readonly TestFixture _fixture;
    
    public PersonsServiceTest(TestFixture fixture)
    {
        _fixture = fixture;
    }
}
```

---

## Fluent Assertions

### What is Fluent Assertions?

Fluent Assertions provides a natural, readable syntax for writing test assertions. Instead of cryptic `Assert` methods, you write expectations that read like English.

### Why Use Fluent Assertions?

**Readability**
```csharp
// ❌ xUnit assertions (less readable)
Assert.Equal(5, result);
Assert.True(person.IsActive);
Assert.NotNull(person.Email);

// ✅ Fluent Assertions (more readable)
result.Should().Be(5);
person.IsActive.Should().BeTrue();
person.Email.Should().NotBeNull();
```

**Better Error Messages**
```csharp
// xUnit error: "Assert.Equal() Failure: Expected: 5, Actual: 3"
// Fluent error: "Expected result to be 5, but found 3."
```

**Rich API**
- Comprehensive assertion methods
- Covers collections, strings, exceptions, dates, and more
- Extensible for custom assertions

### Installation

```bash
dotnet add package FluentAssertions
```

### Basic Assertions

```csharp
using FluentAssertions;

// Equality
result.Should().Be(5);                    // Equal to 5
result.Should().NotBe(10);                 // Not equal to 10

// Boolean
isValid.Should().BeTrue();                 // True
isValid.Should().BeFalse();                // False

// Null checks
person.Should().BeNull();                  // Null
person.Should().NotBeNull();               // Not null

// Comparison
number.Should().BeGreaterThan(5);          // > 5
number.Should().BeLessThanOrEqualTo(10);   // <= 10
number.Should().BeInRange(1, 100);         // Between 1 and 100

// Types
obj.Should().BeOfType<Person>();           // Exact type
obj.Should().BeAssignableTo<object>();     // Can be assigned to type
```

### String Assertions

```csharp
string name = "John Doe";

// Content checks
name.Should().StartWith("John");           // Starts with "John"
name.Should().EndWith("Doe");              // Ends with "Doe"
name.Should().Contain("oh");               // Contains "oh"
name.Should().NotContain("Smith");         // Doesn't contain "Smith"

// Length
name.Should().HaveLength(8);               // Exactly 8 characters
name.Should().NotBeEmpty();                // Not empty
name.Should().NotBeNullOrEmpty();          // Not null or empty
name.Should().NotBeNullOrWhiteSpace();     // Not null, empty, or whitespace

// Pattern matching
email.Should().MatchRegex(@"^\w+@\w+\.\w+$"); // Matches email pattern

// Case sensitivity
name.Should().Be("john doe");              // Case-sensitive (fails)
name.Should().BeEquivalentTo("john doe");  // Case-insensitive (passes)
```

### Collection Assertions

```csharp
List<string> fruits = new List<string> { "apple", "banana", "cherry" };

// Count
fruits.Should().HaveCount(3);              // Exactly 3 items
fruits.Should().NotBeEmpty();              // Has items
fruits.Should().BeEmpty();                 // No items

// Content
fruits.Should().Contain("apple");          // Contains item
fruits.Should().NotContain("grape");       // Doesn't contain item
fruits.Should().ContainSingle();           // Exactly one item
fruits.Should().ContainSingle(x => x.StartsWith("b")); // One item matching predicate

// All items
fruits.Should().OnlyContain(x => x.Length > 0); // All items match predicate
fruits.Should().AllSatisfy(x => x.Should().NotBeEmpty()); // All items satisfy assertion

// Order
numbers.Should().BeInAscendingOrder();     // Sorted ascending
numbers.Should().BeInDescendingOrder();    // Sorted descending

// Equivalence (order doesn't matter)
list1.Should().BeEquivalentTo(list2);      // Same items, any order
list1.Should().Equal(list2);               // Same items, same order

// Subsets
list.Should().BeSubsetOf(otherList);       // All items in otherList
list.Should().Contain(x => x.Age > 18);    // At least one match
```

### Numeric Assertions

```csharp
int number = 42;

// Sign
number.Should().BePositive();              // > 0
number.Should().BeNegative();              // < 0

// Comparison
number.Should().BeGreaterThan(40);         // > 40
number.Should().BeGreaterThanOrEqualTo(42); // >= 42
number.Should().BeLessThan(50);            // < 50
number.Should().BeLessThanOrEqualTo(42);   // <= 42

// Range
number.Should().BeInRange(40, 50);         // Between 40 and 50
number.Should().NotBeInRange(0, 10);       // Not between 0 and 10

// Precision (for decimals)
decimal value = 3.1415m;
value.Should().BeApproximately(3.14m, 0.01m); // Within ±0.01
```

### DateTime Assertions

```csharp
DateTime date = DateTime.Now;

// Comparison
date.Should().BeAfter(DateTime.Now.AddDays(-1));   // After yesterday
date.Should().BeBefore(DateTime.Now.AddDays(1));    // Before tomorrow
date.Should().BeOnOrAfter(DateTime.Today);          // Today or later
date.Should().BeOnOrBefore(DateTime.Today);         // Today or earlier

// Precision
date.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1)); // Within 1 second

// Date components
date.Should().HaveYear(2025);              // Year is 2025
date.Should().HaveMonth(11);               // Month is November
date.Should().HaveDay(20);                 // Day is 20th

// Day of week
date.Should().BeOn(DayOfWeek.Wednesday);   // Is Wednesday
```

### Exception Assertions

```csharp
// Method that might throw
Action act = () => service.DeletePerson(null);

// Should throw specific exception
act.Should().Throw<ArgumentNullException>();

// Should throw with specific message
act.Should().Throw<ArgumentException>()
    .WithMessage("Person ID cannot be null");

// Should throw with message containing text
act.Should().Throw<InvalidOperationException>()
    .WithMessage("*not found*"); // Wildcard matching

// Should throw with inner exception
act.Should().Throw<Exception>()
    .WithInnerException<InvalidOperationException>();

// Should not throw
act.Should().NotThrow();

// Async exceptions
Func<Task> act = async () => await service.DeletePersonAsync(null);
await act.Should().ThrowAsync<ArgumentNullException>();
```

### Type Assertions

```csharp
object obj = new Person();

// Exact type
obj.Should().BeOfType<Person>();           // Exactly Person
obj.Should().NotBeOfType<Employee>();      // Not Employee

// Assignable type (inheritance/interface)
obj.Should().BeAssignableTo<object>();     // Can be assigned to object
obj.Should().BeAssignableTo<IPerson>();    // Implements IPerson

// Null type
Person? nullPerson = null;
nullPerson.Should().BeNull();
nullPerson.Should().NotBeNull();
```

### Custom Assertions

Extend Fluent Assertions for domain-specific validations:

```csharp
public static class PersonAssertions
{
    public static void ShouldBeValidPerson(this PersonResponse person)
    {
        person.Should().NotBeNull();
        person.PersonID.Should().NotBeEmpty();
        person.PersonName.Should().NotBeNullOrWhiteSpace();
        person.Email.Should().MatchRegex(@"^\w+@\w+\.\w+$");
    }
}

// Usage
personResponse.ShouldBeValidPerson();
```

---

## Mocking with Moq

### What is Mocking?

**Mocking** creates substitute objects that simulate the behavior of real dependencies. This allows you to:

- Test code in isolation
- Control dependency behavior
- Verify interactions
- Avoid external systems (databases, APIs)

### Why Mock?

**Focus on Unit Under Test**
```
Without Mocking:
Controller → Service → Repository → Database
(Testing everything, not isolated)

With Mocking:
Controller → Service → Mock Repository
(Testing only Controller and Service logic)
```

**Control Test Conditions**
- Return specific data
- Simulate errors
- Test edge cases
- Make tests deterministic

**Speed**
- No database calls
- No network requests
- Tests run in milliseconds

### Installing Moq

```bash
dotnet add package Moq
```

### Basic Mocking Workflow

**1. Create Mock**

```csharp
var mockRepository = new Mock<IPersonsRepository>();
```

**2. Setup Behavior**

```csharp
mockRepository
    .Setup(repo => repo.GetAllPersons())
    .ReturnsAsync(new List<Person> { /* test data */ });
```

**3. Inject Mock**

```csharp
var service = new PersonsService(mockRepository.Object);
```

**4. Execute Code**

```csharp
var result = await service.GetAllPersons();
```

**5. Verify Interactions**

```csharp
mockRepository.Verify(
    repo => repo.GetAllPersons(), 
    Times.Once);
```

### Setup Methods

#### Basic Returns

```csharp
// Return value
mock.Setup(x => x.GetById(1))
    .Returns(person);

// Return async
mock.Setup(x => x.GetByIdAsync(1))
    .ReturnsAsync(person);

// Return null
mock.Setup(x => x.GetById(999))
    .Returns((Person)null);

// Return based on input
mock.Setup(x => x.GetById(It.IsAny<int>()))
    .Returns<int>(id => new Person { PersonID = Guid.NewGuid(), PersonName = $"Person {id}" });
```

#### Parameter Matching

```csharp
// Any value
mock.Setup(x => x.GetById(It.IsAny<int>()))
    .Returns(person);

// Specific value
mock.Setup(x => x.GetById(1))
    .Returns(person1);
mock.Setup(x => x.GetById(2))
    .Returns(person2);

// Conditional matching
mock.Setup(x => x.GetById(It.Is<int>(id => id > 0)))
    .Returns(person);

// Range matching
mock.Setup(x => x.GetById(It.IsInRange(1, 100, Moq.Range.Inclusive)))
    .Returns(person);

// Complex predicate
mock.Setup(x => x.AddPerson(It.Is<Person>(p => 
    !string.IsNullOrEmpty(p.PersonName) && 
    p.Email.Contains("@"))))
    .ReturnsAsync(person);
```

#### Exception Handling

```csharp
// Throw exception
mock.Setup(x => x.GetById(-1))
    .Throws<ArgumentException>();

// Throw with message
mock.Setup(x => x.GetById(-1))
    .Throws(new ArgumentException("ID must be positive"));

// Throw async
mock.Setup(x => x.GetByIdAsync(-1))
    .ThrowsAsync<ArgumentException>();

// Conditional exception
mock.Setup(x => x.DeletePerson(It.Is<Guid>(id => id == Guid.Empty)))
    .Throws<InvalidOperationException>();
```

#### Callbacks

```csharp
// Execute code when method is called
var capturedPerson = new Person();
mock.Setup(x => x.AddPerson(It.IsAny<Person>()))
    .Callback<Person>(p => capturedPerson = p)
    .ReturnsAsync(person);

// Multiple callbacks
mock.Setup(x => x.UpdatePerson(It.IsAny<Person>()))
    .Callback<Person>(p => Console.WriteLine($"Updating {p.PersonName}"))
    .Returns(person)
    .Callback(() => Console.WriteLine("Update complete"));
```

#### Sequential Returns

```csharp
// Return different values on successive calls
mock.SetupSequence(x => x.GetNextId())
    .Returns(1)
    .Returns(2)
    .Returns(3)
    .Throws<InvalidOperationException>();

// First call returns 1, second returns 2, third returns 3, fourth throws
```

#### Properties

```csharp
// Setup property get
mock.Setup(x => x.ConnectionString)
    .Returns("Server=test");

// Setup property set
mock.SetupSet(x => x.ConnectionString = It.IsAny<string>());

// Track property changes
mock.SetupProperty(x => x.IsConnected, false);
mock.Object.IsConnected = true; // Property now holds value
```

### Verify Methods

```csharp
// Verify exact call count
mock.Verify(x => x.GetById(1), Times.Once);
mock.Verify(x => x.GetById(1), Times.Exactly(2));
mock.Verify(x => x.GetById(1), Times.Never);

// Verify range of calls
mock.Verify(x => x.Save(), Times.AtLeastOnce);
mock.Verify(x => x.Save(), Times.AtMost(3));
mock.Verify(x => x.Save(), Times.Between(1, 5, Moq.Range.Inclusive));

// Verify with parameter matching
mock.Verify(x => x.AddPerson(It.IsAny<Person>()), Times.Once);
mock.Verify(x => x.AddPerson(It.Is<Person>(p => p.PersonName == "John")), Times.Once);

// Verify all setups were invoked
mock.VerifyAll();

// Verify no other methods were called
mock.VerifyNoOtherCalls();

// Verify property access
mock.VerifyGet(x => x.ConnectionString, Times.AtLeastOnce);
mock.VerifySet(x => x.IsConnected = true, Times.Once);
```

### Complete Mocking Example

```csharp
public class PersonsServiceTest
{
    private readonly Mock<IPersonsRepository> _mockRepository;
    private readonly PersonsService _service;

    public PersonsServiceTest()
    {
        _mockRepository = new Mock<IPersonsRepository>();
        _service = new PersonsService(_mockRepository.Object);
    }

    [Fact]
    public async Task AddPerson_ValidPerson_CallsRepositoryOnce()
    {
        // Arrange
        var personRequest = new PersonAddRequest 
        { 
            PersonName = "John Doe",
            Email = "john@example.com"
        };
        
        var expectedPerson = new Person
        {
            PersonID = Guid.NewGuid(),
            PersonName = "John Doe",
            Email = "john@example.com"
        };

        _mockRepository
            .Setup(r => r.AddPerson(It.IsAny<Person>()))
            .ReturnsAsync(expectedPerson);

        // Act
        var result = await _service.AddPerson(personRequest);

        // Assert
        result.Should().NotBeNull();
        result.PersonName.Should().Be("John Doe");
        result.Email.Should().Be("john@example.com");
        
        _mockRepository.Verify(
            r => r.AddPerson(It.Is<Person>(p => 
                p.PersonName == "John Doe" && 
                p.Email == "john@example.com")), 
            Times.Once);
    }

    [Fact]
    public async Task GetPersonById_NonExistentId_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        
        _mockRepository
            .Setup(r => r.GetPersonByID(nonExistentId))
            .ReturnsAsync((Person)null);

        // Act
        var result = await _service.GetPersonByID(nonExistentId);

        // Assert
        result.Should().BeNull();
        
        _mockRepository.Verify(
            r => r.GetPersonByID(nonExistentId), 
            Times.Once);
    }

    [Fact]
    public async Task DeletePerson_NullId_ThrowsArgumentNullException()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.DeletePerson(null))
            .ThrowsAsync(new ArgumentNullException("personID"));

        // Act
        Func<Task> act = async () => await _service.DeletePerson(null);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("personID");
    }
}
```

### Mocking Best Practices

✅ **Do**
- Mock interfaces, not concrete classes
- Use `It.IsAny<T>()` for flexible matching
- Verify important interactions
- Keep setup simple and focused
- Use descriptive variable names for mocks

❌ **Don't**
- Mock everything (only dependencies)
- Over-verify (verify what matters)
- Mock the class you're testing
- Create complex setup chains
- Forget to verify critical interactions

---

## AutoFixture

### What is AutoFixture?

AutoFixture automatically generates test data for your classes, saving you from writing repetitive object initialization code.

### Why Use AutoFixture?

**Before AutoFixture**
```csharp
[Fact]
public void AddPerson_Test()
{
    var person = new Person
    {
        PersonID = Guid.NewGuid(),
        PersonName = "John Doe",
        Email = "john@example.com",
        DateOfBirth = new DateTime(1990, 1, 1),
        Gender = "Male",
        CountryID = Guid.NewGuid(),
        Address = "123 Main St",
        ReceiveNewsLetters = true
    };
    
    // Test logic...
}
```

**With AutoFixture**
```csharp
[Theory, AutoData]
public void AddPerson_Test(Person person)
{
    // person is automatically created with all properties populated
    // Test logic...
}
```

**Benefits**
- Eliminates boilerplate
- Focuses tests on behavior, not data setup
- Encourages testing with varied data
- Reduces maintenance when models change

### Installation

```bash
dotnet add package AutoFixture
dotnet add package AutoFixture.Xunit2
```

### Basic Usage

**Manual Creation**

```csharp
using AutoFixture;

[Fact]
public void Test_WithManualFixture()
{
    // Arrange
    var fixture = new Fixture();
    var person = fixture.Create<Person>();
    
    // person now has all properties populated with random data
    // Act & Assert...
}
```

**With [AutoData]**

```csharp
using AutoFixture.Xunit2;

[Theory, AutoData]
public void AddPerson_ValidPerson_ReturnsPersonResponse(Person person)
{
    // person is automatically created
    person.PersonName.Should().NotBeNullOrEmpty();
}
```

**With Multiple Parameters**

```csharp
[Theory, AutoData]
public void Test_MultipleParameters(
    Person person,
    string searchTerm,
    int pageNumber,
    List<Country> countries)
{
    // All parameters auto-generated
}
```

### Customization

**Customize Specific Properties**

```csharp
[Fact]
public void Test_WithCustomization()
{
    var fixture = new Fixture();
    
    // Customize Person creation
    fixture.Customize<Person>(c => c
        .With(p => p.Age, 25)                    // Set Age to 25
        .With(p => p.Email, "test@example.com")  // Set specific email
        .Without(p => p.Address));                // Leave Address null
    
    var person = fixture.Create<Person>();
    
    person.Age.Should().Be(25);
    person.Email.Should().Be("test@example.com");
    person.Address.Should().BeNull();
}
```

**Customize Collection Size**

```csharp
[Fact]
public void Test_CustomCollectionSize()
{
    var fixture = new Fixture();
    
    // Create exactly 5 persons
    var persons = fixture.CreateMany<Person>(5).ToList();
    
    persons.Should().HaveCount(5);
}
```

**Custom Specimens**

```csharp
public class EmailAddressSpecimenBuilder : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is Type type && type == typeof(string))
        {
            return $"user{Guid.NewGuid()}@example.com";
        }
        
        return new NoSpecimen();
    }
}

// Usage
var fixture = new Fixture();
fixture.Customizations.Add(new EmailAddressSpecimenBuilder());
```

**Freeze Objects**

```csharp
[Fact]
public void Test_FreezeObject()
{
    var fixture = new Fixture();
    
    // Create and "freeze" a Country - same instance reused
    var country = fixture.Freeze<Country>();
    
    var person1 = fixture.Create<Person>();
    var person2 = fixture.Create<Person>();
    
    // Both persons reference the same country
    person1.CountryID.Should().Be(country.CountryID);
    person2.CountryID.Should().Be(country.CountryID);
}
```

### Advanced AutoFixture

**Combining with Moq**

```csharp
using AutoFixture;
using AutoFixture.AutoMoq;

[Fact]
public void Test_WithAutoMoq()
{
    var fixture = new Fixture().Customize(new AutoMoqCustomization());
    
    // AutoFixture will create mocks for interfaces
    var mockRepository = fixture.Freeze<Mock<IPersonsRepository>>();
    var service = fixture.Create<PersonsService>();
    
    mockRepository.Setup(r => r.GetAllPersons())
        .ReturnsAsync(fixture.CreateMany<Person>().ToList());
}
```

**Custom AutoData Attribute**

```csharp
public class CustomAutoDataAttribute : AutoDataAttribute
{
    public CustomAutoDataAttribute() : base(() =>
    {
        var fixture = new Fixture();
        fixture.Customize<Person>(c => c
            .With(p => p.Age, 18)); // All persons are 18
        return fixture;
    })
    {
    }
}

// Usage
[Theory, CustomAutoData]
public void Test_WithCustomAutoData(Person person)
{
    person.Age.Should().Be(18);
}
```

---

## Unit Testing

### What are Unit Tests?

Unit tests verify individual units of code (methods, classes) in **complete isolation** from dependencies.

### Unit Test Structure

```csharp
public class PersonsServiceTest
{
    private readonly Mock<IPersonsRepository> _mockRepository;
    private readonly Mock<ICountriesRepository> _mockCountriesRepo;
    private readonly PersonsService _service;
    private readonly IFixture _fixture;

    public PersonsServiceTest()
    {
        // Arrange - Setup (runs before each test)
        _fixture = new Fixture();
        _mockRepository = new Mock<IPersonsRepository>();
        _mockCountriesRepo = new Mock<ICountriesRepository>();
        _service = new PersonsService(_mockRepository.Object);
    }

    [Fact]
    public async Task TestMethod()
    {
        // Arrange - Test-specific setup
        // Act - Execute the method
        // Assert - Verify the result
    }
}
```

### Testing CRUD Operations

#### Create (Add) Tests

```csharp
[Fact]
public async Task AddPerson_ValidPerson_ReturnsPersonResponse()
{
    // Arrange
    var person = _fixture.Build<Person>()
        .Without(p => p.Country)
        .Create();
    
    var personRequest = person.ToPersonAddRequest();

    _mockRepository
        .Setup(r => r.AddPerson(It.IsAny<Person>()))
        .ReturnsAsync(person);

    // Act
    var result = await _service.AddPerson(personRequest);

    // Assert
    result.Should().NotBeNull();
    result.PersonID.Should().Be(person.PersonID);
    result.PersonName.Should().Be(person.PersonName);
    result.Email.Should().Be(person.Email);
    
    _mockRepository.Verify(
        r => r.AddPerson(It.IsAny<Person>()), 
        Times.Once);
}

[Fact]
public async Task AddPerson_NullRequest_ThrowsArgumentNullException()
{
    // Arrange
    PersonAddRequest? request = null;

    // Act
    Func<Task> act = async () => await _service.AddPerson(request);

    // Assert
    await act.Should().ThrowAsync<ArgumentNullException>();
    
    _mockRepository.Verify(
        r => r.AddPerson(It.IsAny<Person>()), 
        Times.Never);
}

[Fact]
public async Task AddPerson_DuplicateEmail_ThrowsArgumentException()
{
    // Arrange
    var existingPerson = _fixture.Create<Person>();
    var newPersonRequest = new PersonAddRequest 
    { 
        Email = existingPerson.Email 
    };

    _mockRepository
        .Setup(r => r.GetPersonByEmail(existingPerson.Email))
        .ReturnsAsync(existingPerson);

    // Act
    Func<Task> act = async () => await _service.AddPerson(newPersonRequest);

    // Assert
    await act.Should().ThrowAsync<ArgumentException>()
        .WithMessage("*email*already exists*");
}
```

#### Read (Get) Tests

```csharp
[Fact]
public async Task GetAllPersons_EmptyList_ReturnsEmptyList()
{
    // Arrange
    _mockRepository
        .Setup(r => r.GetAllPersons())
        .ReturnsAsync(new List<Person>());

    // Act
    var result = await _service.GetAllPersons();

    // Assert
    result.Should().BeEmpty();
    
    _mockRepository.Verify(
        r => r.GetAllPersons(), 
        Times.Once);
}

[Fact]
public async Task GetAllPersons_WithPersons_ReturnsAllPersons()
{
    // Arrange
    var persons = _fixture.CreateMany<Person>(3).ToList();
    
    _mockRepository
        .Setup(r => r.GetAllPersons())
        .ReturnsAsync(persons);

    // Act
    var result = await _service.GetAllPersons();

    // Assert
    result.Should().HaveCount(3);
    result.Should().OnlyContain(p => persons.Any(x => x.PersonID == p.PersonID));
    
    _mockRepository.Verify(
        r => r.GetAllPersons(), 
        Times.Once);
}
