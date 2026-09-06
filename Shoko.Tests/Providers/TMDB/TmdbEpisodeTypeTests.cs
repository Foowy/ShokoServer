using Shoko.Abstractions.Metadata;
using Shoko.Abstractions.Metadata.Enums;
using Shoko.Server.Models.TMDB;
using TMDbLib.Objects.Search;
using TMDbLib.Objects.TvShows;
using Xunit;

namespace Shoko.Tests.Providers.TMDB;

/// <summary>
/// User Story 5 / O4: adopt TMDB's own <c>episode_type</c>. TMDbLib master exposes it on
/// <see cref="TvSeasonEpisode.EpisodeType"/> (and <c>TvEpisodeBase</c>); before the upgrade
/// Shoko had no episode-type signal at all and derived <see cref="IEpisode.Type"/> from
/// <c>SeasonNumber == 0</c> alone.
/// </summary>
public class TmdbEpisodeTypeTests
{
    private static readonly TvShow Show = new() { Id = 45782, Name = "SAO" };
    private static readonly TvSeason Season = new() { Id = 61862, SeasonNumber = 1 };

    private static TvSeasonEpisode ReducedEpisode(string? episodeType, int seasonNumber = 1) => new()
    {
        Id = 979329,
        SeasonNumber = seasonNumber,
        EpisodeNumber = 1,
        Name = "Ep",
        Overview = "",
        StillPath = "",
        EpisodeType = episodeType,
    };

    [Theory]
    [InlineData("standard", "standard")]
    [InlineData("finale", "finale")]
    [InlineData("mid_season", "mid_season")]
    [InlineData("special", "special")]
    [InlineData("  finale  ", "finale")]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    public void Populate_StoresTrimmedEpisodeTypeOrNull(string? wire, string? expected)
    {
        var episode = new TMDB_Episode();
        episode.Populate(Show, Season, ReducedEpisode(wire), translations: null);

        Assert.Equal(expected, episode.TmdbEpisodeType);
    }

    [Fact]
    public void Populate_EpisodeTypeChange_IsReportedAsAnUpdate()
    {
        var episode = new TMDB_Episode();
        episode.Populate(Show, Season, ReducedEpisode("standard"), translations: null);

        Assert.True(episode.Populate(Show, Season, ReducedEpisode("finale"), translations: null));
        Assert.False(episode.Populate(Show, Season, ReducedEpisode("finale"), translations: null));
    }

    [Theory]
    [InlineData("special", 1, EpisodeType.Special)]
    [InlineData("special", 0, EpisodeType.Special)]
    [InlineData("standard", 1, EpisodeType.Episode)]
    [InlineData("finale", 5, EpisodeType.Episode)]
    [InlineData("mid_season", 2, EpisodeType.Episode)]
    public void Type_UsesTmdbEpisodeTypeWhenSet(string wire, int seasonNumber, EpisodeType expected)
    {
        var episode = new TMDB_Episode();
        episode.Populate(Show, Season, ReducedEpisode(wire, seasonNumber), translations: null);

        Assert.Equal(expected, ((IEpisode)episode).Type);
    }

    [Theory]
    [InlineData(0, EpisodeType.Special)]
    [InlineData(1, EpisodeType.Episode)]
    public void Type_FallsBackToSeasonNumberDerivationWhenNull(int seasonNumber, EpisodeType expected)
    {
        var episode = new TMDB_Episode { SeasonNumber = seasonNumber };

        Assert.Null(episode.TmdbEpisodeType);
        Assert.Equal(expected, ((IEpisode)episode).Type);
    }
}
