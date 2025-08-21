using System;
using System.Net;
using System.Text.Json;
using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using Foody_System.Models;

namespace Foody_System
{
    [TestFixture]
    public class Tests
    {
        private RestClient client;
        private static string lastCreatedFoodId;

        private const string baseUrl = "http://softuni-qa-loadbalancer-2137572849.eu-north-1.elb.amazonaws.com:86/api/";

        private const string staticToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiI2NzRlNjhiYi04MmE2LTQ5YWItOTlhYS1kZGZmYzYzNjJkMzkiLCJpYXQiOiIwOC8xOS8yMDI1IDE1OjUxOjQ5IiwiVXNlcklkIjoiZTc2M2ZhMTUtNGJiZS00ZjY3LTliMmEtMDhkZGQ4ZTVkYWIyIiwiRW1haWwiOiJ6aGVrb25pQHNtaXRoLmNvbSIsIlVzZXJOYW1lIjoiemhla29uaVNtaXRoIiwiZXhwIjoxNzU1NjQwMzA5LCJpc3MiOiJGb29keV9BcHBfU29mdFVuaSIsImF1ZCI6IkZvb2R5X1dlYkFQSV9Tb2Z0VW5pIn0.Rc54DeBn9XeH5U7M_7jyLWavZaEKbNjX8kzxRvY6GGU";

        private const string loginUsername = "zhekoniSmith";
        private const string loginPassword = "zhekoni123";

        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken;

            if (!string.IsNullOrWhiteSpace(staticToken))
            {
                jwtToken = staticToken;
            }
            else
            {
                jwtToken = GetjwtToken(loginUsername, loginPassword);
            }

            var options = new RestClientOptions(baseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };
            this.client = new RestClient(options);
        }

        private string GetjwtToken(string username, string password)
        {
            var tempClient = new RestClient(baseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { username, password });

            var response = tempClient.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);

                var token = content.GetProperty("accessToken").GetString();

                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Failed to retrieve JWT token from the response.");
                }

                return token;
            }

            else
            {
                throw new InvalidOperationException($"Failed to authenticate. Status code:{response.StatusCode}, Content: {response.Content}");
            }
        }

        [Order(1)]
        [Test]
        public void CreateANewFood_ShouldReturnStatusCode201()
        {

            var foodRequest = new FoodDTO
            {
                Name = "Test Food by Zhekoni",
                Description = "This is a test food description",
                Url = ""
            };

            var request = new RestRequest("Food/Create", Method.Post);
            request.AddJsonBody(foodRequest);
            var response = this.client.Execute(request);

            Console.WriteLine($"Status Code: {response.StatusCode}");
            Console.WriteLine($"Response Content: '{response.Content}'");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

            //var createResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            var json = JsonSerializer.Deserialize<JsonElement>( response.Content );
            lastCreatedFoodId = json.GetProperty("foodId").GetString() ?? string.Empty;

           
            Assert.That(lastCreatedFoodId, Is.Not.Null.And.Not.Empty, "Food ID should not be null or empty.");         


        }

        [Order(2)]
        [Test]
        public void EditCreatedFoodTitle_ShouldReturnSuccessfullyEdited()
        {
            var patchFoodRequest = new[]
            {
                new
                    {
                         path = "/name",
                         op = "replace",
                         value = "Edited Food Name"
                    }
            };

            var request = new RestRequest($"Food/Edit/{lastCreatedFoodId}", Method.Patch);
            //request.AddQueryParameter("foodId", lastCreatedFoodId);
            request.AddJsonBody(patchFoodRequest);
            var response = this.client.Execute(request);

            Console.WriteLine($"Status Code: {response.StatusCode}");
            Console.WriteLine($"Response Content: '{response.Content}'");

            var patchResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode,Is.EqualTo(HttpStatusCode.OK));
            Assert.That(patchResponse.Msg,Is.EqualTo("Successfully edited"));
        }

        [Order(3)]
        [Test]
        public void GetAllFoods_ShouldNotReturnEmptyResponse()
        {
            var request = new RestRequest("Food/All", Method.Get);

            var response = this.client.Execute(request);

            var responseItems = JsonSerializer.Deserialize<List<ApiResponseDTO>>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response, Is.Not.Null);
            Assert.That(responseItems, Is.Not.Empty);
        }

        [Order(4)]
        [Test]
        public void DeleteEditedFood_ShouldReturnDelitedSuccessfully()
        {
            var request = new RestRequest($"Food/Delete/{lastCreatedFoodId}", Method.Delete);
            //request.AddQueryParameter("ideaId", lastCreatedIdeaId);
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Does.Contain("Deleted successfully!"));
        }

        [Test, Order(5)]
        public void CreateFood_WithoutRequiredFields()
        {
            var foodRequest = new FoodDTO
            {
                Name = "",
                Description = "",
                Url = ""
            };

            var request = new RestRequest("Food/Create", Method.Post);
            request.AddJsonBody(foodRequest);
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test, Order(6)]
        public void EditFood_WhichIsNotExisting()
        {
            var patchFoodRequest = new[]
            {
                new
                    {
                         path = "/name",
                         op = "replace",
                         value = "New Food Name"
                    }
            };

            var request = new RestRequest("Food/Edit/112223", Method.Patch);
            request.AddJsonBody(patchFoodRequest);
            var response = this.client.Execute(request);

            //var editResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(response.Content, Does.Contain("No food revues..."));

        }

        [Test, Order(7)]
        public void DeleteFood_WhichIsNotExisting()
        {
            string fakeId = "123434";

            var request = new RestRequest($"Food/Delete/{fakeId}", Method.Delete);
            var response = this.client.Execute(request);

            //var deleteResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Content, Does.Contain("Unable to delete this food revue!"));
        }

            [OneTimeTearDown]
        public void TearDown()
        {
            this.client?.Dispose();
        }
    }
}