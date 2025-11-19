using AutoFixture;
using Entities;
using EntityFrameworkCoreMock;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using RepositoryContracts;
using ServiceContracts;
using ServiceContracts.DTOs;
using ServiceContracts.Enums;
using Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace CURDTests;

public class PersonsServiceTest
{
   //private fields
   private readonly IPersonsService _personService;
   private readonly ICountriesService _countriesService;

   private readonly ITestOutputHelper _testOutputHelper;
   private readonly IFixture _fixture;

   private readonly Mock<IPersonsRepository> _personRepositoryMock;
   private readonly IPersonsRepository _personsRepository;


   //constructor
   // ITestOutputHelper is used to write output to the test results in xUnit
   // It allows capturing and displaying output during test execution,
   // which can be helpful for debugging and logging purposes.
   public PersonsServiceTest(ITestOutputHelper testOutputHelper)
   {
      this._fixture = new Fixture();

      var countriesInitialData = new List<Country>();
      var personsInitialData = new List<Person>();

      _personRepositoryMock = new Mock<IPersonsRepository>();
      _personsRepository = _personRepositoryMock.Object;

      var dbContextMock = new DbContextMock<ApplicationDbContext>(
         new DbContextOptionsBuilder<ApplicationDbContext>().Options
      );

      // now create the ApplicationDbContext object from dbContextMock
      ApplicationDbContext dbContext = dbContextMock.Object;

      // create a mock dbset for countrires with initial data
      // create a mock dbset for persons with initial data
      dbContextMock.CreateDbSetMock(x => x.Countries, countriesInitialData);
      dbContextMock.CreateDbSetMock(x => x.Persons, personsInitialData);

      _countriesService = new CountriesService(null);
      _personService = new PersonsService(_personsRepository);
      this._testOutputHelper = testOutputHelper;
   }

   #region AddPerson Test MEthods

   //When we supply null value as PersonAddRequest, it should throw ArgumentNullException
   [Fact]
   public async Task AddPerson_NullPerson()
   {
      //Arrange
      PersonAddRequest? personAddRequest = null;

      //Act
      //Assert
      //await Assert.ThrowsAsync<ArgumentNullException>(() => {
      //   //Act
      //   return _personService.AddPerson(personAddRequest);
      //});

      Func<Task> act = (async () =>
      {
         await _personService.AddPerson(personAddRequest);
      });

      await act.Should().ThrowAsync<ArgumentNullException>(); 

   }


   //When we supply null value as PersonName, it should throw ArgumentException
   [Fact]
   public async Task AddPerson_PersonNameIsNull()
   {
      //Arrange
      //PersonAddRequest? personAddRequest = new PersonAddRequest() { PersonName = null };
      PersonAddRequest? personAddRequest = _fixture.Build<PersonAddRequest>()
                                                   .With(temp => temp.PersonName, null as string)
                                                   .Create();

      //Act
      Func<Task> act = (async () =>
      {
         await _personService.AddPerson(personAddRequest);
      });

      await act.Should().ThrowAsync<ArgumentException>();
   }

   //When we supply proper person details, it should insert the person into the persons list; and it should return an object of PersonResponse, which includes with the newly generated person id
   [Fact]
   public async Task AddPerson_FullPersonDetails_ToBeSuccessful()
   {
      //Arrange
      PersonAddRequest? personAddRequest = _fixture.Build<PersonAddRequest>()
       .With(temp => temp.Email, "someone@example.com")
       .Create();

      Person person = personAddRequest.ToPerson();
      PersonResponse person_response_expected = person.ToPersonResponse();

      //If we supply any argument value to the AddPerson method, it should return the same return value
      _personRepositoryMock.Setup
       (temp => temp.AddPerson(It.IsAny<Person>()))
       .ReturnsAsync(person);

      //Act
      PersonResponse person_response_from_add = await _personService.AddPerson(personAddRequest);

      person_response_expected.PersonID = person_response_from_add.PersonID;

      //Assert
      person_response_from_add.PersonID.Should().NotBe(Guid.Empty);
      person_response_from_add.Should().Be(person_response_expected);
   }




   #endregion

   #region GetPersonByPersonID

   //If we supply null as PersonID, it should return null as PersonResponse
   [Fact]
   public async Task GetPersonByPersonID_NullPersonIDAsync()
   {
      //Arrange
      Guid? personID = null;

      //Act
      PersonResponse? person_response_from_get = await _personService.GetPersonByPersonID(personID);

      //Assert
      //Assert.Null(person_response_from_get);
      person_response_from_get.Should().BeNull();
   }


   //If we supply a valid person id, it should return the valid person details as PersonResponse object
   [Fact]
   public async Task GetPersonByPersonID_WithPersonIDAsync()
   {
      //Arange
      //CountryAddRequest country_request = new CountryAddRequest() { CountryName = "Canada" };
      CountryAddRequest country_request = _fixture.Create<CountryAddRequest>();

      CountryResponse country_response = await _countriesService.AddCountry(country_request);

      //PersonAddRequest person_request = new PersonAddRequest
      //{
      //   PersonName = "person name...",
      //   Email = "email@sample.com",
      //   Address = "address",
      //   CountryID = country_response.CountryID,
      //   DateOfBirth = DateTime.Parse("2000-01-01"),
      //   Gender = GenderOptions.Male,
      //   ReceiveNewsLetters = false
      //};
      PersonAddRequest person_request = _fixture.Build<PersonAddRequest>()
                                                .With(temp => temp.Email, "email@sample.com")
                                                .With(temp => temp.CountryID, country_response.CountryID)
                                                .Create();


      PersonResponse person_response_from_add = await _personService.AddPerson(person_request);

      PersonResponse? person_response_from_get = await _personService.GetPersonByPersonID(person_response_from_add.PersonID);

      //Assert
      //Assert.Equal(person_response_from_add, person_response_from_get);
      person_response_from_add.Should().Be(person_response_from_get);
   }


   #endregion

   #region GetAllPersons


   //The GetAllPersons() should return an empty list by default
   [Fact]
   public async Task GetAllPersons_EmptyListAsync()
   {
      //Act
      List<PersonResponse> persons_from_get = await _personService.GetAllPersons();

      //Assert
      //Assert.Empty(persons_from_get);
      persons_from_get.Should().BeEmpty();
   }


   //First, we will add few persons; and then when we call GetAllPersons(), it should return the same persons that were added
   [Fact]
   public async Task GetAllPersons_AddFewPersonsAsync()
   {
      //Arrange
      //CountryAddRequest country_request_1 = new CountryAddRequest
      //{
      //   CountryName = "USA" 
      //};

      CountryAddRequest country_request_1 = _fixture.Create<CountryAddRequest>();
      CountryResponse country_response_1 = await _countriesService.AddCountry(country_request_1);

      //PersonAddRequest person_request_1 = new PersonAddRequest
      //{ 
      //   PersonName = "Smith",
      //   Email = "smith@example.com",
      //   Gender = GenderOptions.Male,
      //   Address = "address of smith",
      //   CountryID = country_response_1.CountryID,
      //   DateOfBirth = DateTime.Parse("2002-05-06"),
      //   ReceiveNewsLetters = true
      //};

      //PersonAddRequest person_request_2 = new PersonAddRequest
      //{
      //   PersonName = "Mary",
      //   Email = "mary@example.com",
      //   Gender = GenderOptions.Female,
      //   Address = "address of mary",
      //   CountryID = country_response_1.CountryID,
      //   DateOfBirth = DateTime.Parse("2000-02-02"),
      //   ReceiveNewsLetters = false
      //};

      //PersonAddRequest person_request_3 = new PersonAddRequest
      //{
      //   PersonName = "Rahman",
      //   Email = "rahman@example.com",
      //   Gender = GenderOptions.Male,
      //   Address = "address of rahman",
      //   CountryID = country_response_1.CountryID,
      //   DateOfBirth = DateTime.Parse("1999-03-03"),
      //   ReceiveNewsLetters = true
      //};

      PersonAddRequest person_request_1 = _fixture.Build<PersonAddRequest>()
                                                  .With(temp => temp.Email, "someone_1@example.com")
                                                  .With(x => x.CountryID, country_response_1.CountryID)
                                                  .Create();

      PersonAddRequest person_request_2 = _fixture.Build<PersonAddRequest>()
                                                  .With(temp => temp.Email, "someone_2@example.com")
                                                  .With(x => x.CountryID, country_response_1.CountryID)
                                                  .Create();

      PersonAddRequest person_request_3 = _fixture.Build<PersonAddRequest>()
                                                  .With(temp => temp.Email, "someone_3@example.com")
                                                  .With(x => x.CountryID, country_response_1.CountryID)
                                                  .Create();

      List<PersonAddRequest> person_requests = new List<PersonAddRequest>
      {
         person_request_1,
         person_request_2,
         person_request_3
      };

      List<PersonResponse> person_response_list_from_add = new List<PersonResponse>();

      foreach (PersonAddRequest person_request in person_requests)
      {
         PersonResponse person_response = await _personService.AddPerson(person_request);

         person_response_list_from_add.Add(person_response);
      }

      // Write output to test results
      //print person_response_list_from_add
      _testOutputHelper.WriteLine("Expected:");
      foreach (PersonResponse person_response_from_add in person_response_list_from_add)
      {
         _testOutputHelper.WriteLine(person_response_from_add.ToString());
      }

      //Act
      List<PersonResponse> persons_list_from_get = await _personService.GetAllPersons();

      // Write output to test results
      //print persons_list_from_get
      _testOutputHelper.WriteLine("Actual:");
      foreach (PersonResponse person_response_from_get in persons_list_from_get)
      {
         _testOutputHelper.WriteLine(person_response_from_get.ToString());
      }

      //Assert
      //foreach (PersonResponse person_response_from_add in person_response_list_from_add)
      //{
      //   Assert.Contains(person_response_from_add, persons_list_from_get);
      //}

      person_response_list_from_add.Should().BeEquivalentTo(persons_list_from_get);

   }

   #endregion

   #region GetFilteredPersons

   //If the search text is empty and search by is "PersonName", it should return all persons
   [Fact]
   public async Task GetFilteredPersons_EmptySearchTextAsync()
   {
      //Arrange
      //CountryAddRequest country_request_1 = new CountryAddRequest() { CountryName = "USA" };
      CountryAddRequest country_request_1 = _fixture.Create<CountryAddRequest>();

      CountryResponse country_response_1 = await _countriesService.AddCountry(country_request_1);

      //PersonAddRequest person_request_1 = new PersonAddRequest() { PersonName = "Smith", Email = "smith@example.com", Gender = GenderOptions.Male, Address = "address of smith", CountryID = country_response_1.CountryID, DateOfBirth = DateTime.Parse("2002-05-06"), ReceiveNewsLetters = true };

      //PersonAddRequest person_request_2 = new PersonAddRequest() { PersonName = "Mary", Email = "mary@example.com", Gender = GenderOptions.Female, Address = "address of mary", CountryID = country_response_2.CountryID, DateOfBirth = DateTime.Parse("2000-02-02"), ReceiveNewsLetters = false };

      //PersonAddRequest person_request_3 = new PersonAddRequest() { PersonName = "Rahman", Email = "rahman@example.com", Gender = GenderOptions.Male, Address = "address of rahman", CountryID = country_response_2.CountryID, DateOfBirth = DateTime.Parse("1999-03-03"), ReceiveNewsLetters = true };

      PersonAddRequest person_request_1 = _fixture.Build<PersonAddRequest>()
                                                  .With(temp => temp.Email, "someone_1@example.com")
                                                  .With(x => x.CountryID, country_response_1.CountryID)
                                                  .Create();

      PersonAddRequest person_request_2 = _fixture.Build<PersonAddRequest>()
                                                  .With(temp => temp.Email, "someone_2@example.com")
                                                  .With(x => x.CountryID, country_response_1.CountryID)
                                                  .Create();

      PersonAddRequest person_request_3 = _fixture.Build<PersonAddRequest>()
                                                  .With(temp => temp.Email, "someone_3@example.com")
                                                  .With(x => x.CountryID, country_response_1.CountryID)
                                                  .Create();

      List<PersonAddRequest> person_requests = new List<PersonAddRequest>()
      {
         person_request_1,
         person_request_2,
         person_request_3
      };

      List<PersonResponse> person_response_list_from_add = new List<PersonResponse>();

      foreach (PersonAddRequest person_request in person_requests)
      {
         PersonResponse person_response = await _personService.AddPerson(person_request);
         person_response_list_from_add.Add(person_response);
      }

      //print person_response_list_from_add
      _testOutputHelper.WriteLine("Expected:");
      foreach (PersonResponse person_response_from_add in person_response_list_from_add)
      {
         _testOutputHelper.WriteLine(person_response_from_add.ToString());
      }

      //Act
      List<PersonResponse> persons_list_from_search = await _personService.GetFilteredPersons(nameof(Person.PersonName), "");

      //print persons_list_from_get
      _testOutputHelper.WriteLine("Actual:");
      foreach (PersonResponse person_response_from_get in persons_list_from_search)
      {
         _testOutputHelper.WriteLine(person_response_from_get.ToString());
      }

      //Assert
      //foreach (PersonResponse person_response_from_add in person_response_list_from_add)
      //{
      //   Assert.Contains(person_response_from_add, persons_list_from_search);
      //}
      person_response_list_from_add.Should().BeEquivalentTo(persons_list_from_search);

   }


   //First we will add few persons; and then we will search based on person name with some search string. It should return the matching persons
   [Fact]
   public async Task GetFilteredPersons_SearchByPersonNameAsync()
   {
      //Arrange
      //CountryAddRequest country_request_1 = new CountryAddRequest() { CountryName = "USA" };
      CountryAddRequest country_request_1 = _fixture.Create<CountryAddRequest>();

      CountryResponse country_response_1 = await _countriesService.AddCountry(country_request_1);

      //PersonAddRequest person_request_1 = new PersonAddRequest() { PersonName = "Smith", Email = "smith@example.com", Gender = GenderOptions.Male, Address = "address of smith", CountryID = country_response_1.CountryID, DateOfBirth = DateTime.Parse("2002-05-06"), ReceiveNewsLetters = true };

      //PersonAddRequest person_request_2 = new PersonAddRequest() { PersonName = "Mary", Email = "mary@example.com", Gender = GenderOptions.Female, Address = "address of mary", CountryID = country_response_2.CountryID, DateOfBirth = DateTime.Parse("2000-02-02"), ReceiveNewsLetters = false };

      //PersonAddRequest person_request_3 = new PersonAddRequest() { PersonName = "Rahman", Email = "rahman@example.com", Gender = GenderOptions.Male, Address = "address of rahman", CountryID = country_response_2.CountryID, DateOfBirth = DateTime.Parse("1999-03-03"), ReceiveNewsLetters = true };

      PersonAddRequest person_request_1 = _fixture.Build<PersonAddRequest>()
                                                  .With(temp => temp.Email, "someone_1@example.com")
                                                  .With(x => x.CountryID, country_response_1.CountryID)
                                                  .Create();

      PersonAddRequest person_request_2 = _fixture.Build<PersonAddRequest>()
                                                  .With(temp => temp.Email, "someone_2@example.com")
                                                  .With(x => x.CountryID, country_response_1.CountryID)
                                                  .Create();

      PersonAddRequest person_request_3 = _fixture.Build<PersonAddRequest>()
                                                  .With(temp => temp.Email, "someone_3@example.com")
                                                  .With(x => x.CountryID, country_response_1.CountryID)
                                                  .Create();

      List<PersonAddRequest> person_requests = new List<PersonAddRequest>() { person_request_1, person_request_2, person_request_3 };

      List<PersonResponse> person_response_list_from_add = new List<PersonResponse>();

      foreach (PersonAddRequest person_request in person_requests)
      {
         PersonResponse person_response = await _personService.AddPerson(person_request);
         person_response_list_from_add.Add(person_response);
      }

      //print person_response_list_from_add
      _testOutputHelper.WriteLine("Expected:");
      foreach (PersonResponse person_response_from_add in person_response_list_from_add)
      {
         _testOutputHelper.WriteLine(person_response_from_add.ToString());
      }

      //Act
      List<PersonResponse> persons_list_from_search = await _personService.GetFilteredPersons(nameof(Person.PersonName), "ma");

      //print persons_list_from_get
      _testOutputHelper.WriteLine("Actual:");
      foreach (PersonResponse person_response_from_get in persons_list_from_search)
      {
         _testOutputHelper.WriteLine(person_response_from_get.ToString());
      }

      //Assert
      //foreach (PersonResponse person_response_from_add in person_response_list_from_add)
      //{
      //   if (person_response_from_add.PersonName != null)
      //   {
      //      if (person_response_from_add.PersonName.Contains("ma", StringComparison.OrdinalIgnoreCase))
      //      {
      //         Assert.Contains(person_response_from_add, persons_list_from_search);
      //      }
      //   }
      //}

      persons_list_from_search.Should().OnlyContain(person_response =>
         person_response.PersonName != null &&
         person_response.PersonName.Contains("ma", StringComparison.OrdinalIgnoreCase)
      );
   }

   #endregion

   #region GetSortedPersons


   //When we sort based on PersonName in DESC, it should return persons list in descending on PersonName
   [Fact]
   public async Task GetSortedPersonsAsync()
   {
      //Arrange
      //CountryAddRequest country_request_1 = new CountryAddRequest() { CountryName = "USA" };
      CountryAddRequest country_request_1 = _fixture.Create<CountryAddRequest>();


      CountryResponse country_response_1 = await _countriesService.AddCountry(country_request_1);

      //PersonAddRequest person_request_1 = new PersonAddRequest()
      //{
      //   PersonName = "Smith",
      //   Email = "smith@example.com",
      //   Gender = GenderOptions.Male,
      //   Address = "address of smith",
      //   CountryID = country_response_1.CountryID,
      //   DateOfBirth = DateTime.Parse("2002-05-06"),
      //   ReceiveNewsLetters = true
      //};


      //PersonAddRequest person_request_2 = new PersonAddRequest()
      //{
      //   PersonName = "Mary",
      //   Email = "mary@example.com",
      //   Gender = GenderOptions.Female,
      //   Address = "address of mary",
      //   CountryID = country_response_2.CountryID,
      //   DateOfBirth = DateTime.Parse("2000-02-02"),
      //   ReceiveNewsLetters = false
      //};


      //PersonAddRequest person_request_3 = new PersonAddRequest()
      //{
      //   PersonName = "Rahman",
      //   Email = "rahman@example.com",
      //   Gender = GenderOptions.Male,
      //   Address = "address of rahman",
      //   CountryID = country_response_2.CountryID,
      //   DateOfBirth = DateTime.Parse("1999-03-03"),
      //   ReceiveNewsLetters = true
      //};

      PersonAddRequest person_request_1 = _fixture.Build<PersonAddRequest>()
                                                  .With(temp => temp.Email, "someone_1@example.com")
                                                  .With(x => x.CountryID, country_response_1.CountryID)
                                                  .Create();

      PersonAddRequest person_request_2 = _fixture.Build<PersonAddRequest>()
                                                  .With(temp => temp.Email, "someone_2@example.com")
                                                  .With(x => x.CountryID, country_response_1.CountryID)
                                                  .Create();

      PersonAddRequest person_request_3 = _fixture.Build<PersonAddRequest>()
                                                  .With(temp => temp.Email, "someone_3@example.com")
                                                  .With(x => x.CountryID, country_response_1.CountryID)
                                                  .Create();

      List<PersonAddRequest> person_requests = new List<PersonAddRequest>()
      {
         person_request_1,
         person_request_2,
         person_request_3 
      };

      List<PersonResponse> person_response_list_from_add = new List<PersonResponse>();

      foreach (PersonAddRequest person_request in person_requests)
      {
         PersonResponse person_response = await _personService.AddPerson(person_request);
         person_response_list_from_add.Add(person_response);
      }

      //print person_response_list_from_add
      _testOutputHelper.WriteLine("Expected:");
      foreach (PersonResponse person_response_from_add in person_response_list_from_add)
      {
         _testOutputHelper.WriteLine(person_response_from_add.ToString());
      }
      List<PersonResponse> allPersons = await _personService.GetAllPersons();

      //Act
      List<PersonResponse> persons_list_from_sort = await _personService.GetSortedPersons(allPersons, nameof(Person.PersonName), SortOrderOptions.DESC);

      //print persons_list_from_get
      _testOutputHelper.WriteLine("Actual:");
      foreach (PersonResponse person_response_from_get in persons_list_from_sort)
      {
         _testOutputHelper.WriteLine(person_response_from_get.ToString());
      }
      person_response_list_from_add = person_response_list_from_add.OrderByDescending(temp => temp.PersonName).ToList();

      //Assert
      //for (int i = 0; i < person_response_list_from_add.Count; i++)
      //{
      //   Assert.Equal(person_response_list_from_add[i], persons_list_from_sort[i]);
      //}

      persons_list_from_sort.Should().BeInDescendingOrder(temp => temp.PersonName);
   }

   #endregion

   #region UpdatePerson

   //When we supply null as PersonUpdateRequest, it should throw ArgumentNullException
   [Fact]
   public async Task UpdatePerson_NullPersonAsync()
   {
      //Arrange
      PersonUpdateRequest? person_update_request = null;

      ////Assert
      //await Assert.ThrowsAsync<ArgumentNullException>(() => {
      //   //Act
      //   return _personService.UpdatePerson(person_update_request);
      //});

      //Act
      Func<Task> action = async () =>
      {
         await _personService.UpdatePerson(person_update_request);
      };

      //Assert
      await action.Should().ThrowAsync<ArgumentNullException>();
   }


   //When we supply invalid person id, it should throw ArgumentException
   [Fact]
   public async Task UpdatePerson_InvalidPersonID()
   {
      //Arrange
      //PersonUpdateRequest? person_update_request = new PersonUpdateRequest() { PersonID = Guid.NewGuid() };
      PersonUpdateRequest? person_update_request = _fixture.Build<PersonUpdateRequest>()
                                                           .With(temp => temp.PersonID, Guid.NewGuid())
                                                           .Create();

      ////Assert
      //await Assert.ThrowsAsync<ArgumentException>(async () =>
      //{
      //   //Act
      //   await _personService.UpdatePerson(person_update_request);
      //});

      //Act
      Func<Task> action = async () =>
      {
         await _personService.UpdatePerson(person_update_request);
      };

      //Assert
      await action.Should().ThrowAsync<ArgumentException>();
   }



   //When PersonName is null, it should throw ArgumentException
   [Fact]
   public async Task UpdatePerson_PersonNameIsNull()
   {
      //Arrange
      //CountryAddRequest country_add_request = new CountryAddRequest() { CountryName = "UK" };
      CountryAddRequest country_add_request = _fixture.Build<CountryAddRequest>()
                                                      .Create();


      CountryResponse country_response = await _countriesService.AddCountry(country_add_request);

      //PersonAddRequest person_add_request = new PersonAddRequest()
      //{
      //PersonName = "John",
      //CountryID = country_response_from_add.CountryID,
      //Email = "john@example.com",
      //Address = "address...",
      //Gender = GenderOptions.Male
      //};

      PersonAddRequest person_add_request = _fixture.Build<PersonAddRequest>()
                                                    .With(temp => temp.PersonName, "Rahman")
                                                    .With(temp => temp.Email, "someone@example.com")
                                                    .With(temp => temp.CountryID, country_response.CountryID)
                                                    .Create();

      PersonResponse person_response_from_add = await _personService.AddPerson(person_add_request);

      PersonUpdateRequest person_update_request = person_response_from_add.ToPersonUpdateRequest();
      person_update_request.PersonName = null;


      ////Assert
      //await Assert.ThrowsAsync<ArgumentException>(async () =>
      //{
      //   //Act
      //   await _personService.UpdatePerson(person_update_request);
      //});

      //Act
      var action = async () =>
      {
         await _personService.UpdatePerson(person_update_request);
      };

      //Assert
      await action.Should().ThrowAsync<ArgumentException>();
   }



   //First, add a new person and try to update the person name and email
   [Fact]
   public async Task UpdatePerson_PersonFullDetailsUpdationAsync()
   {
      //Arrange
      //CountryAddRequest country_add_request = new CountryAddRequest() { CountryName = "UK" };
      CountryAddRequest country_request = _fixture.Create<CountryAddRequest>();

      CountryResponse country_response = await _countriesService.AddCountry(country_request);

      //PersonAddRequest person_add_request = new PersonAddRequest()
      //{
      //PersonName = "John",
      //CountryID = country_response_from_add.CountryID,
      //Address = "Abc road",
      //DateOfBirth = DateTime.Parse("2000-01-01"),
      //Email = "abc@example.com",
      //Gender = GenderOptions.Male,
      //ReceiveNewsLetters = true
      //};

      PersonAddRequest person_add_request = _fixture.Build<PersonAddRequest>()
                                                    .With(temp => temp.PersonName, "Rahman")
                                                    .With(temp => temp.Email, "someone@example.com")
                                                    .With(temp => temp.CountryID, country_response.CountryID)
                                                    .Create();

      PersonResponse person_response_from_add = await _personService.AddPerson(person_add_request);

      PersonUpdateRequest person_update_request = person_response_from_add.ToPersonUpdateRequest();
      person_update_request.PersonName = "William";
      person_update_request.Email = "william@example.com";

      //Act
      PersonResponse person_response_from_update = await _personService.UpdatePerson(person_update_request);

      PersonResponse? person_response_from_get = await _personService.GetPersonByPersonID(person_response_from_update.PersonID);

      ////Assert
      //Assert.Equal(person_response_from_get, person_response_from_update);

      //Assert
      person_response_from_update.Should().Be(person_response_from_get);
   }

   #endregion

   #region DeletePerson

   //If you supply an valid PersonID, it should return true
   [Fact]
   public async Task DeletePerson_ValidPersonIDAsync()
   {
      //Arrange
      //CountryAddRequest country_add_request = new CountryAddRequest()
      //{
      //   CountryName = "USA"
      //};
      CountryAddRequest country_request = _fixture.Create<CountryAddRequest>();

      CountryResponse country_response = await _countriesService.AddCountry(country_request);

      //PersonAddRequest person_add_request = new PersonAddRequest()
      //{
      //   PersonName = "Jones",
      //   Address = "address",
      //   CountryID = country_response_from_add.CountryID,
      //   DateOfBirth = Convert.ToDateTime("2010-01-01"),
      //   Email = "jones@example.com",
      //   Gender = GenderOptions.Male,
      //   ReceiveNewsLetters = true
      //};

      PersonAddRequest person_add_request = _fixture.Build<PersonAddRequest>()
                                                    .With(temp => temp.PersonName, "Rahman")
                                                    .With(temp => temp.Email, "someone@example.com")
                                                    .With(temp => temp.CountryID, country_response.CountryID)
                                                    .Create();

      PersonResponse person_response_from_add = await _personService.AddPerson(person_add_request);


      //Act
      bool isDeleted = await _personService.DeletePerson(person_response_from_add.PersonID);

      //Assert
      //Assert.True(isDeleted);
      isDeleted.Should().BeTrue();
   }


   //If you supply an invalid PersonID, it should return false
   [Fact]
   public async Task DeletePerson_InvalidPersonIDAsync()
   {
      //Act
      bool isDeleted = await _personService.DeletePerson(Guid.NewGuid());

      //Assert
      //Assert.False(isDeleted);
      isDeleted.Should().BeFalse();
   }


   #endregion

}
