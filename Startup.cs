using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LicenseServer.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using NLog.Extensions.Logging;
using NLog;

namespace LicenseServer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            LogManager.Configuration = new NLogLoggingConfiguration(configuration.GetSection("NLog"));
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy(name: "PolicyCore",
                builder =>
                {
                    builder.WithOrigins(Configuration["WithOrigins"]);
                    builder.WithMethods(Configuration["WithMethods"]);
                    builder.WithHeaders(Configuration["WithHeaders"]);
                });
            });
            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.ClearProviders();
                loggingBuilder.AddNLog();
                loggingBuilder.AddConsole();
            });

            services.AddControllers();
            services.AddTokenAuthentication(Configuration);
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "License Microservice",
                    Version = "v1",
                    Description = "This License Microservice generate Tab product licenses",
                    //TermsOfService = new Uri("https://rashik.com.np"),
                    //Contact = new OpenApiContact
                    //{
                    //    Name = "TAB FUSION",
                    //    Email = "info@tab.com",
                    //    Url = new Uri("https://tab.com"),
                    //},
                    //License = new OpenApiLicense
                    //{
                    //    Name = "Swagger Implementation License",
                    //    Url = new Uri("https://tab.com"),
                    //}

                });
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    //BearerFormat = "JWT",
                    In = ParameterLocation.Header,

                    Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 1safsfsdfdfd\"",
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement {
                            {
                            new OpenApiSecurityScheme {
                            Reference = new OpenApiReference {
                                Type = ReferenceType.SecurityScheme,
                                    Id = "Bearer"
                            }
                        },
                        new string[] {}
                    }
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                //c.RoutePrefix = string.Empty;
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "License Microservice v1");
                c.InjectStylesheet("/swagger-custom/swagger-custom-style.css");
                c.InjectJavascript("/swagger-custom/swagger-custom-script.js", "text/javascript");
            });
            
            app.UseStaticFiles();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
     
            app.UseHttpsRedirection();
            app.UseCors("PolicyCore");
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}


