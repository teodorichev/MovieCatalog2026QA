using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MovieCatalog.Tests.DTOs;
using RestSharp;
using RestSharp.Authenticators;

namespace MovieCatalog.Tests
{
    [TestFixture]
    public class MovieCatalogTests
    {
        private RestClient client;
        private static string createdMovieId;

        private const string BaseUrl = "http://144.91.123.158:5000";
        private const string LoginEmail = "teodorichev@gmail.com";
        private const string LoginPassword = "123456";

        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken = GetJwtToken(LoginEmail, LoginPassword);

            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };

            client = new RestClient(options);
        }

        private string GetJwtToken(string email, string password)
        {
            var tempClient = new RestClient(BaseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { email, password });

            var response = tempClient.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content!);
                var token = content.GetProperty("accessToken").GetString();

                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Token not found in the response.");
                }

                return token;
            }
            else
            {
                throw new InvalidOperationException($"Failed to authenticate. Status code: {response.StatusCode}, Response: {response.Content}");
            }
        }

        [Test]
        [Order(1)]
        public void CreateMovie_WithRequiredFields_ShouldReturnSuccess()
        {
            var movieData = new
            {
                title = "Test Movie",
                description = "This is a test movie description.",
                posterUrl = "",
                trailerLink = "",
                isWatched = false
            };

            var request = new RestRequest("/api/Movie/Create", Method.Post);
            request.AddJsonBody(movieData);

            var response = client.Execute(request);

            var createResponse = JsonSerializer.Deserialize<ApiResponseDto>(response.Content!);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            Assert.That(createResponse, Is.Not.Null);
            Assert.That(createResponse.Movie, Is.Not.Null);
            Assert.That(createResponse.Movie.Id, Is.Not.Null.And.Not.Empty);
            Assert.That(createResponse.Msg, Is.EqualTo("Movie created successfully!"));

            createdMovieId = createResponse.Movie.Id;
        }

        [Test]
        [Order(2)]
        public void EditMovie_ShouldReturnSuccess()
        {
            var editedMovieData = new
            {
                title = "Edited Movie",
                description = "This is an edited movie description.",
                posterUrl = "",
                trailerLink = "",
                isWatched = true
            };

            var request = new RestRequest("/api/Movie/Edit", Method.Put);
            request.AddQueryParameter("movieId", createdMovieId);
            request.AddJsonBody(editedMovieData);

            var response = client.Execute(request);

            var editResponse = JsonSerializer.Deserialize<ApiResponseDto>(response.Content!);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            Assert.That(editResponse, Is.Not.Null);
            Assert.That(editResponse.Msg, Is.EqualTo("Movie edited successfully!"));
        }

        [Test]
        [Order(3)]
        public void GetAllMovies_ShouldReturnSuccess()
        {
            var request = new RestRequest("/api/Catalog/All", Method.Get);

            var response = client.Execute(request);

            var responseItems = JsonSerializer.Deserialize<List<MovieDto>>(response.Content!);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            Assert.That(responseItems, Is.Not.Null);
            Assert.That(responseItems, Is.Not.Empty);
        }

        [Test]
        [Order(4)]
        public void DeleteMovie_ShouldReturnSuccess()
        {
            var request = new RestRequest("/api/Movie/Delete", Method.Delete);
            request.AddQueryParameter("movieId", createdMovieId);

            var response = client.Execute(request);

            var deleteResponse = JsonSerializer.Deserialize<ApiResponseDto>(response.Content!);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            Assert.That(deleteResponse, Is.Not.Null);
            Assert.That(deleteResponse.Msg, Is.EqualTo("Movie deleted successfully!"));
        }

        [Test]
        [Order(5)]
        public void CreateMovie_WithMissingRequiredFields_ShouldReturnBadRequest()
        {
            var invalidMovieData = new
            {
                title = "",
                description = "",
                posterUrl = "",
                trailerLink = "",
                isWatched = false
            };

            var request = new RestRequest("/api/Movie/Create", Method.Post);
            request.AddJsonBody(invalidMovieData);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Request.");
        }

        [Test]
        [Order(6)]
        public void EditNonExistingMovie_ShouldReturnBadRequest()
        {
            string nonExistingMovieId = "999999";

            var editedMovieData = new
            {
                title = "Edited Invalid Movie",
                description = "This is an edited invalid movie description.",
                posterUrl = "",
                trailerLink = "",
                isWatched = true
            };

            var request = new RestRequest("/api/Movie/Edit", Method.Put);
            request.AddQueryParameter("movieId", nonExistingMovieId);
            request.AddJsonBody(editedMovieData);

            var response = client.Execute(request);

            var errorResponse = JsonSerializer.Deserialize<ApiResponseDto>(response.Content!);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Request.");
            Assert.That(errorResponse, Is.Not.Null);
            Assert.That(errorResponse.Msg, Is.EqualTo("Unable to edit the movie! Check the movieId parameter or user verification!"));
        }

        [Test]
        [Order(7)]
        public void DeleteNonExistingMovie_ShouldReturnBadRequest()
        {
            string nonExistingMovieId = "999999";

            var request = new RestRequest("/api/Movie/Delete", Method.Delete);
            request.AddQueryParameter("movieId", nonExistingMovieId);

            var response = client.Execute(request);

            var errorResponse = JsonSerializer.Deserialize<ApiResponseDto>(response.Content!);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Request.");
            Assert.That(errorResponse, Is.Not.Null);
            Assert.That(errorResponse.Msg, Is.EqualTo("Unable to delete the movie! Check the movieId parameter or user verification!"));
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            client?.Dispose();
        }
    }
}