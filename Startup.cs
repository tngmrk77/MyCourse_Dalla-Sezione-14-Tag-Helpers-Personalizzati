using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyCourse.Models.Options;
using MyCourse.Models.Services.Application;
using MyCourse.Models.Services.Infrastructure;

namespace MyCourse
{
    public class Startup
    {
        /* Per eliminare i warning per la connsessione al database abbiamo aggiunto nel file appsettings.json
        una connectionString*/
        public IConfiguration Configuration {get;}
        public Startup(IConfiguration configuration)
        {
         Configuration = configuration;   
        }
        /*****************************************************************/


        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddResponseCaching();
            services
            .AddControllersWithViews() //Oppure AddRazorPages o AddMvc
            #if DEBUG
            .AddRazorRuntimeCompilation();
            #endif
  

            services.AddMvc( options => {
                var homeProfile = new CacheProfile();

               // homeProfile.Duration = Configuration.GetValue<int>("ResponseCache:Home:Duration");
               //homeProfile.Location = Configuration.GetValue<ResponseCacheLocation>("ResponseCache:Home:Location");
               // Le due righe sopra riportate possono essere scritte anche nel seguente modo:

               //homeProfile.VaryByQueryKeys = new string[] {"page"};
               Configuration.Bind("ResponseCache:Home", homeProfile);
                options.CacheProfiles.Add("Home", homeProfile);
            });            
            services.AddMvc(options => options.EnableEndpointRouting = false);
            
            //Il codice sotto riportato serve per far funzionare il costruttore presente nella classe CoursesController presente nella dir Controllers
            services.AddTransient<ICourseService, AdoNetCourseService>();
            //services.AddTransient<ICourseService, EfCoreCourseService>();
            services.AddTransient<IDatabaseAccessor, SqliteDatabaseAccessor>();
            services.AddTransient<ICachedCourseService, MemoryCacheCourseService>();

            // services.AddScoped<MyCourseDbContext>();
            //services.AddDbContext<MyCourseDbContext>();
            services.AddDbContextPool<MyCourseDbContext>(optionsBuilder => {
                /* in questo modo richiamiamo la connection string inserita nel file appsettings.json */
                string connectionString = Configuration.GetSection("ConnectionStrings").GetValue<string>("Default");
                optionsBuilder.UseSqlite(connectionString);
                /*****************************************************************************************/

            });

            #region Configurazione del servizio di cache distribuita
            //Se vogliamo usare Redis, bisogna installare il pacchetto NuGet: Microsoft.Extensions.Caching.StackExchangeRedis
            services.AddStackExchangeRedisCache( options => {

                Configuration.Bind("DistributedCache:Redis", options);
            });

            //Se vogliamo usare SqlServer, bisogna installare il pacchetto NuGet per SqlServer
            
            /* services.AddDistributedSqlServerCache( options => {

                Configuration.Bind("DistributedCache:SqlServer", options);
            }); */

            //Se vogliamo usare la memoria mentre siamo in sviuluppo
            // services.AddDistributeMemoryCache();

            #endregion
            // Options
            services.Configure<ConnectionStringsOptions>(Configuration.GetSection("ConnectionStrings"));
            services.Configure<CoursesOptions>(Configuration.GetSection("Courses"));
            services.Configure<MemoryCacheOptions>(Configuration.GetSection(key: "MemoryCache"));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime lifetime)
        {
            
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();

                lifetime.ApplicationStarted.Register(() => {

                       string filePath = Path.Combine(env.ContentRootPath, "bin/reload.txt");
                       File.WriteAllText(filePath, DateTime.Now.ToString());
                });
            }
            else 
            {
                 app.UseExceptionHandler("/Error");
            }   

            app.UseStaticFiles();

            app.UseRouting();
            // Caching
            app.UseResponseCaching();

            //app.UseDeveloperExceptionPage();

            app.UseMvc(routeBuilder => 
            {
                /* id? sta ad indicare che è il parametro è opzionale */
                routeBuilder.MapRoute("default", "{controller=Home}/{action=Index}/{id?}");
            });

         
        }
    }
}
