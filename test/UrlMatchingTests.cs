using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Gainsway.Kiota.Testing.Tests;

/// <summary>
/// Tests that validate the URL matching behavior with exact matching
/// </summary>
[TestFixture]
public class UrlMatchingTests
{
    [Test]
    public void EmptyString_MatchesEverything_ExplainsWhyItWasABadWorkaround()
    {
        // This test explains why empty string "" was a problematic workaround
        // The user was forced to use "" because proper patterns didn't work

        var emptyPattern = "";

        // Any URLs
        var url1 = "{+baseurl}/api/funds/{id}";
        var url2 = "{+baseurl}/api/users/{id}";
        var url3 = "{+baseurl}/api/anything/else";

        // Empty string matches everything
        var matches1 = url1.EndsWith(emptyPattern);
        var matches2 = url2.EndsWith(emptyPattern);
        var matches3 = url3.EndsWith(emptyPattern);

        // THE PROBLEM: Empty string matches EVERYTHING!
        Assert.That(matches1, Is.True, "Empty string matches URL1");
        Assert.That(matches2, Is.True, "Empty string matches URL2");
        Assert.That(matches3, Is.True, "Empty string matches URL3");

        TestContext.Out.WriteLine(
            "This is why user had to use \"\" - it was a workaround for broken URL matching"
        );
        TestContext.Out.WriteLine(
            "With exact matching (current implementation), proper patterns work correctly"
        );
    }

    [Test]
    public void ExactMatch_WithNormalization_DistinguishesEndpoints()
    {
        // Demonstrates how exact matching solves the problem

        var pattern1 = "/api/funds/{id}";
        var pattern2 = "/api/funds/{id}/seeding-metadata";

        var url1 = "{+baseurl}/api/funds/{id}";
        var url2 = "{+baseurl}/api/funds/{id}/seeding-metadata";

        // Normalize: strip {+baseurl} AND query params, ensure leading slash
        string Normalize(string url)
        {
            var cleaned = url.StartsWith("{+baseurl}") ? url.Substring("{+baseurl}".Length) : url;
            cleaned = Regex.Replace(cleaned, @"\{\?.*?\}", string.Empty);
            if (!cleaned.StartsWith("/"))
                cleaned = "/" + cleaned;
            return cleaned;
        }

        var norm1 = Normalize(url1);
        var norm2 = Normalize(url2);
        var normPattern1 = pattern1.StartsWith("/") ? pattern1 : "/" + pattern1;
        var normPattern2 = pattern2.StartsWith("/") ? pattern2 : "/" + pattern2;

        TestContext.WriteLine($"Normalized URL1: '{norm1}'");
        TestContext.WriteLine($"Normalized URL2: '{norm2}'");
        TestContext.WriteLine($"Pattern1: '{normPattern1}'");
        TestContext.WriteLine($"Pattern2: '{normPattern2}'");

        // With exact matching
        var url1_matches_pattern1 = norm1.Equals(normPattern1, StringComparison.OrdinalIgnoreCase);
        var url1_matches_pattern2 = norm1.Equals(normPattern2, StringComparison.OrdinalIgnoreCase);
        var url2_matches_pattern1 = norm2.Equals(normPattern1, StringComparison.OrdinalIgnoreCase);
        var url2_matches_pattern2 = norm2.Equals(normPattern2, StringComparison.OrdinalIgnoreCase);

        // Exact matching gives us precision
        Assert.That(url1_matches_pattern1, Is.True, "URL1 matches pattern1");
        Assert.That(url1_matches_pattern2, Is.False, "URL1 does NOT match pattern2");
        Assert.That(url2_matches_pattern1, Is.False, "URL2 does NOT match pattern1");
        Assert.That(url2_matches_pattern2, Is.True, "URL2 matches pattern2");

        // Each URL matches exactly ONE pattern - no ambiguity!
    }
}
