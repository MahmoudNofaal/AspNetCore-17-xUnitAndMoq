using Entities;
using EntityFrameworkCoreMock;
using Microsoft.EntityFrameworkCore;
using ServiceContracts;
using ServiceContracts.DTOs;
using Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CURDTests;

public class CountriesServiceTest
{
   private readonly ICountriesService _countriesService;

   public CountriesServiceTest() 
	{
      var countriesInitialData = new List<Country>();

      var dbContextMock = new DbContextMock<ApplicationDbContext>(
         new DbContextOptionsBuilder<ApplicationDbContext>().Options
      );

      ApplicationDbContext dbContext = dbContextMock.Object;

      // create a mock dbset for countrires with initial data
      dbContextMock.CreateDbSetMock(x => x.Countries, countriesInitialData);

      this._countriesService = new CountriesService(dbContext);
   }

   #region AddCountry Test Methods
   //When CountryAddRequest is null, it should throw ArgumentNullException
   [Fact]
   public async Task AddCountry_NullCountry()
   {
      //Arrange
      CountryAddRequest? request = null;

      //Assert
      await Assert.ThrowsAsync<ArgumentNullException>(async () =>
      {
         //Act
         await _countriesService.AddCountry(request);
      });
   }

   //When the CountryName is null, it should throw ArgumentException
   [Fact]
   public async Task AddCountry_CountryNameIsNull()
   {
      //Arrange
      CountryAddRequest? request = new CountryAddRequest() { CountryName = null };

      //Assert
      await Assert.ThrowsAsync<ArgumentException>(async () =>
      {
         //Act
         await _countriesService.AddCountry(request);
      });
   }

   //When the CountryName is duplicate, it should throw ArgumentException
   [Fact]
   public async Task AddCountry_DuplicateCountryName()
   {
      //Arrange
      CountryAddRequest? request1 = new CountryAddRequest() { CountryName = "USA" };
      CountryAddRequest? request2 = new CountryAddRequest() { CountryName = "USA" };

      //Assert
      await Assert.ThrowsAsync<ArgumentException>(async () =>
      {
         //Act
         await _countriesService.AddCountry(request1);
         await _countriesService.AddCountry(request2);
      });
   }

   //When you supply proper country name, it should insert (add) the country to the existing list of countries
   [Fact]
   public async Task AddCountry_ProperCountryDetails()
   {
      //Arrange
      CountryAddRequest? request = new CountryAddRequest() { CountryName = "Japan" };

      //Act
      CountryResponse response = await _countriesService.AddCountry(request);
      List<CountryResponse> countries_from_GetAllCountries = await _countriesService.GetAllCountries();

      //Assert
      Assert.True(response.CountryID != Guid.Empty);

      // Verify that the response object is present in the list of countries
      Assert.Contains(response, countries_from_GetAllCountries);
   }
   #endregion


   #region GetAllCountries Test MEthods

   //The list of countries should be empty by default (before adding any countries)
   [Fact]
   public async Task GetAllCountries_EmptyListAsync()
   {
      //Act
      List<CountryResponse> actual_country_response_list = await _countriesService.GetAllCountries();

      //Assert
      Assert.Empty(actual_country_response_list);
   }

   [Fact]
   public async Task GetAllCountries_AddFewCountriesAsync()
   {
      //Arrange
      var country_request_list = new List<CountryAddRequest>()
      {
         new CountryAddRequest { CountryName = "USA" },
         new CountryAddRequest { CountryName = "UK" }
      };

      //Act
      var countries_list_from_add_country = new List<CountryResponse>();

      foreach (CountryAddRequest country_request in country_request_list)
      {
         countries_list_from_add_country.Add(await _countriesService.AddCountry(country_request));
      }

      List<CountryResponse> actualCountryResponseList = await _countriesService.GetAllCountries();

      //read each element from countries_list_from_add_country
      foreach (CountryResponse expected_country in countries_list_from_add_country)
      {
         Assert.Contains(expected_country, actualCountryResponseList);
      }
   }

   #endregion

   #region GetCountryByCountryID

   //If we supply null as CountryID, it should return null as CountryResponse
   [Fact]
   public async Task GetCountryByCountryID_NullCountryIDAsync()
   {
      //Arrange
      Guid? countrID = null;

      //Act
      CountryResponse? country_response_from_get_method = await _countriesService.GetCountryByCountryID(countrID);


      //Assert
      Assert.Null(country_response_from_get_method);
   }

   [Fact]
   //If we supply a valid country id, it should return the matching country details as CountryResponse object
   public async Task GetCountryByCountryID_ValidCountryIDAsync()
   {
      //Arrange
      CountryAddRequest? country_add_request = new CountryAddRequest()
      {
         CountryName = "China"
      };

      CountryResponse country_response_from_add = await _countriesService.AddCountry(country_add_request);

      //Act
      CountryResponse? country_response_from_get = await _countriesService.GetCountryByCountryID(country_response_from_add.CountryID);

      //Assert
      Assert.Equal(country_response_from_add, country_response_from_get);
   }

   #endregion

}

