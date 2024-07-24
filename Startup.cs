using System;
using Amazon.DynamoDBv2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Trnkt.Services;
using Trnkt.Configuration;

namespace Trnkt
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            Env = env;
        }

        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Env { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddHttpClient();
            services.AddLogging();

            services.Configure<AppConfig>(Configuration.GetSection("AppConfig"));

            var awsOptions = Configuration.GetAWSOptions();
            services.AddDefaultAWSOptions(awsOptions);
            services.AddAWSService<IAmazonDynamoDB>();
            services.AddSingleton<DynamoDbService>();
            services.AddSingleton<IFavoritesRepository, FavoritesRepository>();

            // var allowedOrigins = new[] 
            // {
            //     "http://localhost:5173",
            //     "https://main.dxq2smeges624.amplifyapp.com",
            //     "https://trnkt.jordansmithdigital.com",
            // };

            // services.AddCors(options =>
            // {
            //     options.AddPolicy("AllowSpecificOrigin",
            //         builder =>
            //         {
            //             builder.WithOrigins(allowedOrigins)
            //                 .AllowAnyHeader()
            //                 .AllowAnyMethod()
            //                 .AllowCredentials();
            //         });
            // });

                services.AddCors(options =>
                {
                    options.AddPolicy("AllowAllOrigins",
                        builder =>
                        {
                            builder.AllowAnyOrigin()
                                .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS")
                                .AllowAnyHeader()
                                .AllowCredentials();
                        });
                });

            var jwtKey = Env.IsProduction()
                ? Environment.GetEnvironmentVariable("JWT_KEY")
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
                    ValidIssuer = Configuration["AppConfig:JwtIssuer"],
                    ValidateAudience = true,
                    ValidAudience = Configuration["AppConfig:JwtAudience"]
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
            // Handle trailing slashes in urls
            app.Use(async (context, next) =>
            {
                if (context.Request.Path.HasValue && context.Request.Path.Value.EndsWith("/"))
                {
                    context.Request.Path = context.Request.Path.Value.TrimEnd('/');
                }

                await next();
            });

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
            app.UseStaticFiles();
            app.UseRouting();

            //app.UseCors("AllowSpecificOrigin");
            app.UseCors("AllowAllOrigins");

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
