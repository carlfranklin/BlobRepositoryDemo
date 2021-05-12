using BlobRepositoryDemo.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace BlobRepositoryDemo.Client.Services
{
    public class CustomerWithGuidIdManager : APIRepository<CustomerWithGuidId>
    {
        HttpClient http;

        public CustomerWithGuidIdManager(HttpClient _http)
            : base(_http, "customerwithguidids", "Id")
        {
            http = _http;
        }
    }
}
