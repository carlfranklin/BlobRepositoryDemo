using BlobRepositoryDemo.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace BlobRepositoryDemo.Client.Services
{
    public class CustomerManager : APIRepository<Customer>
    {
        HttpClient http;

        public CustomerManager(HttpClient _http)
            : base(_http, "customers", "Id")
        {
            http = _http;
        }
   }
}
