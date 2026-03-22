using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace RpgApi.IntegrationTests;

public class V1ApiTests : IClassFixture<RpgTestFactory>
{
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public V1ApiTests(RpgTestFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ------------------------------------------------------------------ helpers

    private async Task<JsonNode?> ParseEnvelope(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonNode.Parse(json);
    }

    // ------------------------------------------------------------------ envelope shape tests

    [Fact]
    public async Task GetPlayers_V1_ReturnsEnvelopeShape()
    {
        var response = await _client.GetAsync("/api/v1/players");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await ParseEnvelope(response);
        Assert.NotNull(body);
        Assert.NotNull(body!["meta"]);
        Assert.NotNull(body["meta"]!["serverTime"]);
        Assert.NotNull(body["meta"]!["traceId"]);
        Assert.NotNull(body["data"]);
    }

    [Fact]
    public async Task GetEnemies_V1_ReturnsEnvelopeShape()
    {
        var response = await _client.GetAsync("/api/v1/enemies");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await ParseEnvelope(response);
        Assert.NotNull(body);
        Assert.NotNull(body!["meta"]);
        Assert.NotNull(body["data"]);
    }

    [Fact]
    public async Task GetClasses_V1_ReturnsEnvelopeShape()
    {
        var response = await _client.GetAsync("/api/v1/classes");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await ParseEnvelope(response);
        Assert.NotNull(body);
        Assert.NotNull(body!["meta"]);
        Assert.NotNull(body["data"]);
    }

    [Fact]
    public async Task GetPlayer_V1_NotFound_ReturnsEnvelopeError()
    {
        var response = await _client.GetAsync("/api/v1/players/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await ParseEnvelope(response);
        Assert.NotNull(body);
        Assert.NotNull(body!["error"]);
        Assert.NotNull(body["error"]!["code"]);
        Assert.NotNull(body["error"]!["message"]);
    }

    // ------------------------------------------------------------------ fight preview tests

    [Fact]
    public async Task PreviewAction_NonExistentFight_ReturnsNotFound()
    {
        var nonExistentId = Guid.NewGuid();
        var payload = new { playerId = 1, skillId = 1 };
        var response = await _client.PostAsJsonAsync($"/api/v1/fights/{nonExistentId}/actions/preview", payload);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await ParseEnvelope(response);
        Assert.NotNull(body!["error"]);
        Assert.Equal("fight_not_found", body!["error"]!["code"]!.GetValue<string>());
    }

    // ------------------------------------------------------------------ loadout CRUD tests

    [Fact]
    public async Task GetLoadouts_NonExistentCharacter_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/v1/players/99999/characters/99999/loadouts");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await ParseEnvelope(response);
        Assert.NotNull(body!["error"]);
    }

    // ------------------------------------------------------------------ challenges / ladder tests

    [Fact]
    public async Task GetChallenges_ReturnsEnvelopeWithDailyAndWeekly()
    {
        var response = await _client.GetAsync("/api/v1/progression/challenges");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await ParseEnvelope(response);
        Assert.NotNull(body);
        Assert.NotNull(body!["data"]);

        var data = body["data"]!;
        Assert.NotNull(data["daily"]);
        Assert.NotNull(data["weekly"]);
        Assert.True(data["daily"]!.AsArray().Count > 0, "Expected at least one daily challenge");
        Assert.True(data["weekly"]!.AsArray().Count > 0, "Expected at least one weekly challenge");
    }

    [Fact]
    public async Task GetSeasonalLadder_ReturnsEnvelopeWithLeaderboard()
    {
        var response = await _client.GetAsync("/api/v1/progression/ladders/seasonal");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await ParseEnvelope(response);
        Assert.NotNull(body);
        Assert.NotNull(body!["data"]);

        var data = body["data"]!;
        Assert.NotNull(data["season"]);
        Assert.NotNull(data["leaderboard"]);
        Assert.True(data["leaderboard"]!.AsArray().Count > 0, "Expected leaderboard entries");
    }

    // ------------------------------------------------------------------ events endpoint test

    [Fact]
    public async Task GetEvents_ReturnsEnvelopeShape()
    {
        var response = await _client.GetAsync("/api/v1/events");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await ParseEnvelope(response);
        Assert.NotNull(body);
        Assert.NotNull(body!["meta"]);
        Assert.NotNull(body["data"]);
    }

    // ------------------------------------------------------------------ talent tree test

    [Fact]
    public async Task GetTalentTree_UnknownClass_CreateDefaultNodes()
    {
        var response = await _client.GetAsync("/api/v1/progression/talents/TestClass");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await ParseEnvelope(response);
        Assert.NotNull(body);
        var data = body!["data"]!.AsArray();
        Assert.True(data.Count >= 3, "Expected at least 3 seeded talent nodes for a new class");
    }

    // ------------------------------------------------------------------ fight start + turn (full flow)

    [Fact]
    public async Task StartFight_WithInvalidPlayer_ReturnsError()
    {
        var payload = new { playerId = 99999, characterId = 1, enemyId = 1 };
        var response = await _client.PostAsJsonAsync("/api/v1/fights/start", payload);

        // Should be 4xx (not found or bad request), never 5xx
        Assert.True(
            (int)response.StatusCode >= 400 && (int)response.StatusCode < 500,
            $"Expected 4xx but got {(int)response.StatusCode}"
        );

        var body = await ParseEnvelope(response);
        Assert.NotNull(body!["error"]);
    }
}
