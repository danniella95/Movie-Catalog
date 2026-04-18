using Movie_Catalog.Models;
using RestSharp;
using RestSharp.Authenticators;
using System.Net;
using System.Text.Json;

namespace Movie_Catalog;

public class Tests
{
    public class MovieTests
    {
        private RestClient client;
        private static string movieId;

        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken = GetJwtToken("dani@d.com", "123456");
            RestClientOptions options = new RestClientOptions("http://144.91.123.158:5000")
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };
            client = new RestClient(options);
        }

        private string GetJwtToken(string email, string password)
        {
            RestClient client = new RestClient("http://144.91.123.158:5000");
            RestRequest request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { email, password });
            RestResponse response = client.Execute(request);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
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

        [Order(1)]
        [Test]
        public void CreateMovie_WithRequiredFields_ShouldSuccess()
        {
            MovieDTO movie = new MovieDTO
            {
                Title = "Titanic",
                Description = "New description"
            };
            RestRequest request = new RestRequest("/api/Movie/Create", Method.Post);
            request.AddJsonBody(movie);
            RestResponse response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            ApiResponseDTO readyResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(readyResponse, Is.Not.Null);
            Assert.That(readyResponse.Movie.Id, Is.Not.Null.And.Not.Empty);
            Assert.That(readyResponse.Msg, Is.EqualTo("Movie created successfully!"));
            movieId = readyResponse.Movie.Id;
        }

        [Order(2)]
        [Test]
        public void EditMovie_ShouldSucceed()
        {
            MovieDTO movie = new MovieDTO
            {
                Title = "Titanic Edited",
                Description = "Edited description"
            };

            RestRequest request = new RestRequest("/api/Movie/Edit", Method.Put);
            request.AddQueryParameter("movieId", movieId);
            request.AddJsonBody(movie);
            RestResponse response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            ApiResponseDTO? readyResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content!);

            Assert.That(readyResponse, Is.Not.Null);
            Assert.That(readyResponse!.Msg, Is.EqualTo("Movie edited successfully!"));
        }

        [Order(3)]
        [Test]
        public void GetAllMovies_ShouldReturnNonEmptyArray()
        {
            RestRequest request = new RestRequest("/api/Catalog/All", Method.Get);

            RestResponse response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            List<MovieDTO>? allMovies = JsonSerializer.Deserialize<List<MovieDTO>>(response.Content!);

            Assert.That(allMovies, Is.Not.Null);
            Assert.That(allMovies!.Count, Is.GreaterThan(0));
        }

        [Order(4)]
        [Test]
        public void DeleteMovie_ShouldSucceed()
        {
            RestRequest request = new RestRequest("/api/Movie/Delete", Method.Delete);
            request.AddQueryParameter("movieId", movieId);

            RestResponse response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            string? responseMessage = response.Content;

            Assert.That(responseMessage, Does.Contain("Movie deleted successfully!"));
        }

        [Order(5)]
        [Test]
        public void CreateMovie_WithoutRequiredFields_ShouldFail()
        {
            MovieDTO movie = new MovieDTO()
            {
                Title = "",
                Description = ""
            };

            RestRequest request = new RestRequest("/api/Movie/Create", Method.Post);
            request.AddJsonBody(movie);

            RestResponse response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Order(6)]
        [Test]
        public void EditNonExistingMovie_ShouldFail()
        {
            string invalidMovieId = "12345678-1234-1234-1234-123456789999";

            MovieDTO movie = new MovieDTO()
            {
                Title = "Edited title",
                Description = "Edited description"
            };

            RestRequest request = new RestRequest("/api/Movie/Edit", Method.Put);
            request.AddQueryParameter("movieId", invalidMovieId);
            request.AddJsonBody(movie);

            RestResponse response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Content, Does.Contain("Unable to edit the movie! Check the movieId parameter or user verification!"));
        }

        [Order(7)]
        [Test]
        public void DeleteNonExistingMovie_ShouldFail()
        {
            string invalidMovieId = "12345678-1234-1234-1234-123456789999";

            RestRequest request = new RestRequest("/api/Movie/Delete", Method.Delete);
            request.AddQueryParameter("movieId", invalidMovieId);

            RestResponse response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Content, Does.Contain("Unable to delete the movie! Check the movieId parameter or user verification!"));
        }


        [OneTimeTearDown]
        public void TearDown()
        {
            this.client?.Dispose();
        }
    }
}