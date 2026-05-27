using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using PhotoPrint.API.Authentication;
using PhotoPrint.API.DTOs.Uploads;

namespace PhotoPrint.Tests.Integration;

public class UploadControllerIntegrationTests : IAsyncLifetime
{
    // Minimal JPEG header — passes MimeValidator magic-byte check
    private static readonly byte[] JpegBytes =
    [
        0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01,
        0x00, 0x01, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0xFF, 0xD9,
    ];

    private static readonly byte[] PdfBytes = [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31];

    private UploadFactory _factory = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new UploadFactory();
        _client  = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    // ── POST /api/uploads — authentication ────────────────────────────────────

    [Fact]
    public async Task Upload_NoAuthHeader_Returns401()
    {
        using var content = BuildMultipartContent(JpegBytes, "photo.jpg");

        var response = await _client.PostAsync("/api/uploads", content);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── POST /api/uploads — happy path ────────────────────────────────────────

    [Fact]
    public async Task Upload_AuthenticatedUser_ValidJpeg_Returns201WithDto()
    {
        var (_, token) = await _factory.SeedUserWithJwtAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var content = BuildMultipartContent(JpegBytes, "my-photo.jpg");

        var response = await _client.PostAsync("/api/uploads", content);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<UploadDto>();
        dto.Should().NotBeNull();
        dto!.Id.Should().NotBe(Guid.Empty);
        dto.ContentType.Should().Be("image/jpeg");
        dto.WidthPx.Should().Be(800);
        dto.HeightPx.Should().Be(600);
        dto.OriginalFileName.Should().Be("my-photo.jpg");
    }

    [Fact]
    public async Task Upload_GuestSession_ValidJpeg_Returns201WithDto()
    {
        var guestToken = await _factory.SeedGuestTokenAsync();
        _client.DefaultRequestHeaders.TryAddWithoutValidation(
            GuestAuthenticationHandler.HeaderName, guestToken.ToString());

        using var content = BuildMultipartContent(JpegBytes, "guest-photo.jpg");

        var response = await _client.PostAsync("/api/uploads", content);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<UploadDto>();
        dto!.ContentType.Should().Be("image/jpeg");
    }

    [Fact]
    public async Task Upload_ValidJpeg_LocationHeaderPointsToPreview()
    {
        var (_, token) = await _factory.SeedUserWithJwtAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var content = BuildMultipartContent(JpegBytes, "photo.jpg");

        var response = await _client.PostAsync("/api/uploads", content);

        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("/api/uploads/");
        response.Headers.Location.ToString().Should().Contain("/preview");
    }

    // ── POST /api/uploads — error cases ──────────────────────────────────────

    [Fact]
    public async Task Upload_UnsupportedMimeType_Returns415()
    {
        var (_, token) = await _factory.SeedUserWithJwtAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var content = BuildMultipartContent(PdfBytes, "document.pdf");

        var response = await _client.PostAsync("/api/uploads", content);

        response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
    }

    // ── GET /api/uploads/{id}/preview — happy path ────────────────────────────

    [Fact]
    public async Task GetPreview_AuthenticatedOwner_Returns200WithJpeg()
    {
        var (userId, token) = await _factory.SeedUserWithJwtAsync();
        var upload = await _factory.SeedUploadAsync(userId: userId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync($"/api/uploads/{upload.Id}/preview");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("image/jpeg");
    }

    [Fact]
    public async Task GetPreview_GuestOwner_Returns200WithJpeg()
    {
        var guestToken = await _factory.SeedGuestTokenAsync();
        var upload = await _factory.SeedUploadAsync(guestSessionId: guestToken);
        _client.DefaultRequestHeaders.TryAddWithoutValidation(
            GuestAuthenticationHandler.HeaderName, guestToken.ToString());

        var response = await _client.GetAsync($"/api/uploads/{upload.Id}/preview");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPreview_ETagPresent_Returns200()
    {
        var (userId, token) = await _factory.SeedUserWithJwtAsync();
        var upload = await _factory.SeedUploadAsync(userId: userId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync($"/api/uploads/{upload.Id}/preview");

        response.Headers.ETag.Should().NotBeNull();
    }

    // ── GET /api/uploads/{id}/preview — error cases ───────────────────────────

    [Fact]
    public async Task GetPreview_NoAuth_Returns401()
    {
        var response = await _client.GetAsync($"/api/uploads/{Guid.NewGuid()}/preview");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPreview_UploadNotFound_Returns404()
    {
        var (_, token) = await _factory.SeedUserWithJwtAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync($"/api/uploads/{Guid.NewGuid()}/preview");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPreview_DifferentUserOwner_Returns403()
    {
        var (ownerUserId, _) = await _factory.SeedUserWithJwtAsync();
        var (_, attackerToken) = await _factory.SeedUserWithJwtAsync();
        var upload = await _factory.SeedUploadAsync(userId: ownerUserId);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", attackerToken);

        var response = await _client.GetAsync($"/api/uploads/{upload.Id}/preview");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static MultipartFormDataContent BuildMultipartContent(byte[] fileBytes, string fileName)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(fileContent, "file", fileName);
        return content;
    }
}
