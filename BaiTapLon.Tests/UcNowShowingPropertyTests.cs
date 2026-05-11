// Feature: customer-ui-improvements, Property 6 and Property 7
using BaiTapLon.Forms.Customer;
using BaiTapLon.Models;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Xunit;

namespace BaiTapLon.Tests;

/// <summary>
/// Property-based tests for UcNowShowing filter logic.
/// </summary>
public class UcNowShowingPropertyTests
{
    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static Movie MakeMovie(int id, string title, params string[] genres)
    {
        var movie = new Movie
        {
            Id = id,
            Code = $"MV{id:D3}",
            Title = title,
            Duration = 90,
            AgeRating = "P",
            IsActive = true,
            CreatedAt = DateTime.Now,
            MovieGenres = new List<MovieGenre>()
        };

        foreach (var genre in genres)
        {
            movie.MovieGenres.Add(new MovieGenre
            {
                MovieId = id,
                Genre = new Genre { Name = genre }
            });
        }

        return movie;
    }

    // -----------------------------------------------------------------------
    // Property 6: Now Showing title search is a subset filter
    // Validates: Requirements 2.5
    // -----------------------------------------------------------------------

    /// <summary>
    /// Property 6: Now Showing title search is a subset filter.
    /// Validates: Requirements 2.5
    ///
    /// For any non-empty search string q and any list of movies:
    ///   - Result is a subset of the input
    ///   - Every returned movie's title contains q (case-insensitive)
    ///   - Empty query returns all movies unchanged
    ///
    /// Tag: // Feature: customer-ui-improvements, Property 6
    /// </summary>
    [Fact]
    public void FilterByTitle_EmptyQuery_ReturnsAllMovies()
    {
        // Feature: customer-ui-improvements, Property 6
        var movies = new[]
        {
            MakeMovie(1, "Avengers"),
            MakeMovie(2, "Batman"),
            MakeMovie(3, "Captain America")
        };

        var result = UcNowShowing.FilterByTitle(movies, "").ToList();
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void FilterByTitle_WhitespaceQuery_ReturnsAllMovies()
    {
        var movies = new[]
        {
            MakeMovie(1, "Avengers"),
            MakeMovie(2, "Batman")
        };

        var result = UcNowShowing.FilterByTitle(movies, "   ").ToList();
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void FilterByTitle_MatchingQuery_ReturnsSubset()
    {
        var movies = new[]
        {
            MakeMovie(1, "Avengers: Endgame"),
            MakeMovie(2, "Batman Begins"),
            MakeMovie(3, "Avengers: Infinity War")
        };

        var result = UcNowShowing.FilterByTitle(movies, "avengers").ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, m => Assert.Contains("avengers", m.Title, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void FilterByTitle_CaseInsensitive()
    {
        var movies = new[]
        {
            MakeMovie(1, "Spider-Man"),
            MakeMovie(2, "Batman")
        };

        var lower = UcNowShowing.FilterByTitle(movies, "spider-man").ToList();
        var upper = UcNowShowing.FilterByTitle(movies, "SPIDER-MAN").ToList();
        var mixed = UcNowShowing.FilterByTitle(movies, "Spider-Man").ToList();

        Assert.Single(lower);
        Assert.Single(upper);
        Assert.Single(mixed);
    }

    [Fact]
    public void FilterByTitle_ResultIsSubsetOfInput()
    {
        var movies = new[]
        {
            MakeMovie(1, "Avengers"),
            MakeMovie(2, "Batman"),
            MakeMovie(3, "Captain America")
        };

        var result = UcNowShowing.FilterByTitle(movies, "a").ToList();

        // Every result must be in the original list
        Assert.All(result, m => Assert.Contains(m, movies));
    }

    [Property(MaxTest = 100)]
    public Property FilterByTitle_ResultIsAlwaysSubset()
    {
        // Feature: customer-ui-improvements, Property 6
        var movieGen = Gen.Elements(
            MakeMovie(1, "Avengers"),
            MakeMovie(2, "Batman"),
            MakeMovie(3, "Captain America"),
            MakeMovie(4, "Doctor Strange"),
            MakeMovie(5, "Eternals")
        );
        var listGen = Gen.ListOf(movieGen).Select(l => l.DistinctBy(m => m.Id).ToList());
        // Use ArbMap to generate arbitrary strings in FsCheck 3.x
        var queryGen = ArbMap.Default.GeneratorFor<string>().Select(s => s ?? "");

        return Prop.ForAll(
            listGen.ToArbitrary(),
            queryGen.ToArbitrary(),
            (movies, query) =>
            {
                var result = UcNowShowing.FilterByTitle(movies, query).ToList();

                // Result must be a subset
                foreach (var m in result)
                {
                    if (!movies.Any(x => x.Id == m.Id)) return false;
                }

                // If query is non-empty, every result must contain it
                if (!string.IsNullOrWhiteSpace(query))
                {
                    foreach (var m in result)
                    {
                        if (!m.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
                            return false;
                    }
                }
                else
                {
                    // Empty query returns all
                    if (result.Count != movies.Count) return false;
                }

                return true;
            });
    }

    // -----------------------------------------------------------------------
    // Property 7: Now Showing genre filter is a subset filter
    // Validates: Requirements 2.6
    // -----------------------------------------------------------------------

    /// <summary>
    /// Property 7: Now Showing genre filter is a subset filter.
    /// Validates: Requirements 2.6
    ///
    /// For any non-empty set of selected genres G and any list of movies:
    ///   - Result is a subset of the input
    ///   - Every returned movie has at least one genre in G
    ///   - Empty genre set returns all movies unchanged
    ///
    /// Tag: // Feature: customer-ui-improvements, Property 7
    /// </summary>
    [Fact]
    public void FilterByGenres_EmptyGenreSet_ReturnsAllMovies()
    {
        // Feature: customer-ui-improvements, Property 7
        var movies = new[]
        {
            MakeMovie(1, "Avengers", "Action", "Sci-Fi"),
            MakeMovie(2, "Batman", "Action"),
            MakeMovie(3, "Toy Story", "Animation")
        };

        var result = UcNowShowing.FilterByGenres(movies, Array.Empty<string>()).ToList();
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void FilterByGenres_SingleGenre_ReturnsMatchingMovies()
    {
        var movies = new[]
        {
            MakeMovie(1, "Avengers", "Action", "Sci-Fi"),
            MakeMovie(2, "Batman", "Action"),
            MakeMovie(3, "Toy Story", "Animation")
        };

        var result = UcNowShowing.FilterByGenres(movies, new[] { "Action" }).ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, m =>
            Assert.True(m.MovieGenres.Any(mg => mg.Genre.Name == "Action")));
    }

    [Fact]
    public void FilterByGenres_MultipleGenres_ReturnsMoviesMatchingAny()
    {
        var movies = new[]
        {
            MakeMovie(1, "Avengers", "Action", "Sci-Fi"),
            MakeMovie(2, "Batman", "Action"),
            MakeMovie(3, "Toy Story", "Animation"),
            MakeMovie(4, "Interstellar", "Sci-Fi")
        };

        var result = UcNowShowing.FilterByGenres(movies, new[] { "Sci-Fi", "Animation" }).ToList();

        Assert.Equal(3, result.Count); // Avengers (Sci-Fi), Toy Story (Animation), Interstellar (Sci-Fi)
    }

    [Fact]
    public void FilterByGenres_ResultIsSubsetOfInput()
    {
        var movies = new[]
        {
            MakeMovie(1, "Avengers", "Action"),
            MakeMovie(2, "Batman", "Action"),
            MakeMovie(3, "Toy Story", "Animation")
        };

        var result = UcNowShowing.FilterByGenres(movies, new[] { "Action" }).ToList();

        Assert.All(result, m => Assert.Contains(m, movies));
    }

    [Property(MaxTest = 100)]
    public Property FilterByGenres_ResultIsAlwaysSubset()
    {
        // Feature: customer-ui-improvements, Property 7
        var movieGen = Gen.Elements(
            MakeMovie(1, "Avengers", "Action", "Sci-Fi"),
            MakeMovie(2, "Batman", "Action"),
            MakeMovie(3, "Toy Story", "Animation"),
            MakeMovie(4, "Interstellar", "Sci-Fi"),
            MakeMovie(5, "Frozen", "Animation")
        );
        var listGen = Gen.ListOf(movieGen).Select(l => l.DistinctBy(m => m.Id).ToList());
        var genreGen = Gen.Elements("Action", "Sci-Fi", "Animation", "Horror", "Comedy");
        var genreListGen = Gen.ListOf(genreGen).Select(l => l.Distinct().ToList());

        return Prop.ForAll(
            listGen.ToArbitrary(),
            genreListGen.ToArbitrary(),
            (movies, genres) =>
            {
                var result = UcNowShowing.FilterByGenres(movies, genres).ToList();

                // Result must be a subset
                foreach (var m in result)
                {
                    if (!movies.Any(x => x.Id == m.Id)) return false;
                }

                if (genres.Count > 0)
                {
                    // Every result must have at least one matching genre
                    foreach (var m in result)
                    {
                        if (!m.MovieGenres.Any(mg => genres.Contains(mg.Genre.Name)))
                            return false;
                    }
                }
                else
                {
                    // Empty genre set returns all
                    if (result.Count != movies.Count) return false;
                }

                return true;
            });
    }
}
