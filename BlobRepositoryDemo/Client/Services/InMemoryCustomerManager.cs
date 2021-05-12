using BlobRepositoryDemo.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace BlobRepositoryDemo.Client.Services
{
    public class InMemoryCustomerManager : APIRepository<Customer>
    {
        HttpClient http;

        public InMemoryCustomerManager(HttpClient _http)
            : base(_http, "inmemorycustomers", "Id")
        {
            http = _http;
        }
    }
}