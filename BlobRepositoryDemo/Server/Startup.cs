using BlobRepositoryDemo.Server.Data;
using BlobRepositoryDemo.Shared.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Linq;

namespace BlobRepositoryDemo.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<MemoryDataManager<Customer>>(x =>
                new MemoryDataManager<Customer>("Id"));
            services.AddSingleton<IRepository<Customer>>(x => 
                new BlobDataManager<Customer>(
                    Configuration["AzureBlobConnectionString"],
                    Configuration["AzureParentContainerUrl"],
                    "customers", // Name of blob storage container
                    "Id", // Name of primary key property
                    5));
            services.AddSingleton<IRepository<CustomerWithGuidId>>(x => 
                new BlobDataManager<CustomerWithGuidId>(
                    Configuration["AzureBlobConnectionString"],
                    Configuration["AzureParentContainerUrl"],
                    "customers", // Name of blob storage container
                    "Id", // Name of primary key property
                    5));
            services.AddControllersWithViews();
            services.AddRazorPages();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseWebAssemblyDebugging();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseBlazorFrameworkFiles();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
                endpoints.MapFallbackToFile("index.html");
            });
        }
    }
}
