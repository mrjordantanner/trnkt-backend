using System;
using System.Text;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Trnkt.Configuration;
using Trnkt.Services;

namespace Trnkt
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Env { get; }

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            Env = env;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var allowedOrigins = new[]
            {
                 "http://localhost:5173",
                 "https://trnkt.jordansmithdigital.com",
                 "https://main.dxq2smeges624.amplifyapp.com"
            };

            services.AddCors(options =>
            {
                //options.AddPolicy("AllowAllOrigins",
                options.AddPolicy("AllowSpecificOrigin",
                    builder =>
                    {
                        //builder.AllowAnyOrigin()
                        builder.WithOrigins(allowedOrigins)
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials();
                    });
            });

            services.AddControllers();
            services.AddHttpClient();
            services.AddLogging();

            services.Configure<AppConfig>(Configuration.GetSection("AppConfig"));

            var accessKeyId = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
            var secretAccessKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");
            //var region = Environment.GetEnvironmentVariable("AWS_REGION");

            if (string.IsNullOrEmpty(accessKeyId) || string.IsNullOrEmpty(secretAccessKey))
            {
                throw new ArgumentNullException("AWS Credentials are not configured.");
            }

            var awsOptions = Configuration.GetAWSOptions();
            awsOptions.Credentials = new BasicAWSCredentials(accessKeyId, secretAccessKey);
            //awsOptions.Region = RegionEndpoint.GetBySystemName(region);
            awsOptions.Region = RegionEndpoint.USEast1;

            services.AddAWSService<IAmazonDynamoDB>();
            services.AddSingleton<DynamoDbService>();
            services.AddSingleton<IFavoritesRepository, FavoritesRepository>();

            var jwtKey = Env.IsProduction()
                ? Environment.GetEnvironmentVariable("AppConfig__JwtKey")
                : Configuration["AppConfig:JwtKey"];

            if (string.IsNullOrEmpty(jwtKey))
            {
                throw new ArgumentNullException(nameof(jwtKey), "JWT Key is not configured.");
            }

            var key = Encoding.ASCII.GetBytes(jwtKey);

            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = Env.IsProduction() ? Environment.GetEnvironmentVariable("AppConfig__JwtIssuer") : Configuration["AppConfig:JwtIssuer"],
                    ValidateAudience = true,
                    ValidAudience = Env.IsProduction() ? Environment.GetEnvironmentVariable("AppConfig__JwtAudience") : Configuration["AppConfig:JwtAudience"]
                };
            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Trnkt", Version = "v1" });
                var securityScheme = new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "JWT Authorization header using the Bearer scheme.",
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                };
                c.AddSecurityDefinition("Bearer", securityScheme);
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    { securityScheme, new[] { "Bearer" } }
                });
            });

            services.AddAuthorization();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Trnkt v1"));
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseStaticFiles();
            app.UseCors("AllowSpecificOrigin");
            //app.UseCors("AllowAllOrigins");
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
