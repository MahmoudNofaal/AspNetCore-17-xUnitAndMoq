using Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceContracts.DTOs;

/// <summary>
/// DTO class for adding a new country
/// </summary>
public class CountryAddRequest
{
   public string? CountryName { get; set; }

   public Country ToCountry()
   {
      return new Country() { CountryName = CountryName };
   }
}
