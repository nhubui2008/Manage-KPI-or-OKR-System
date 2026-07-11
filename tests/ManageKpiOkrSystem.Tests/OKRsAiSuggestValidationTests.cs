using Manage_KPI_or_OKR_System.Controllers;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public sealed class OKRsAiSuggestValidationTests
{
    [Fact]
    public void StripAiJsonFence_RemovesMarkdownWrappers()
    {
        var raw = "```json\n[{\"KeyResultName\":\"A\",\"TargetValue\":10,\"Unit\":\"%\",\"IsInverse\":false}]\n```";
        var clean = OKRsController.StripAiJsonFence(raw);
        Assert.StartsWith("[", clean);
        Assert.DoesNotContain("```", clean);
    }

    [Fact]
    public void TryParseSuggestedKeyResults_AcceptsValidItems()
    {
        var json = """
            [
              {"KeyResultName":"Ship feature","TargetValue":5,"Unit":"sp","IsInverse":false},
              {"KeyResultName":"Reduce bugs","TargetValue":2,"Unit":"bugs","IsInverse":true}
            ]
            """;

        var result = OKRsController.TryParseSuggestedKeyResults(json);
        Assert.True(result.Success);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("Ship feature", result.Items[0].KeyResultName);
        Assert.True(result.Items[1].IsInverse);
    }

    [Theory]
    [InlineData("not-json")]
    [InlineData("[]")]
    [InlineData("[{\"KeyResultName\":\"\",\"TargetValue\":1,\"Unit\":\"%\"}]")]
    [InlineData("[{\"KeyResultName\":\"A\",\"TargetValue\":0,\"Unit\":\"%\"}]")]
    [InlineData("[{\"KeyResultName\":\"A\",\"TargetValue\":5,\"Unit\":\"\"}]")]
    public void TryParseSuggestedKeyResults_RejectsInvalidPayloads(string json)
    {
        var result = OKRsController.TryParseSuggestedKeyResults(json);
        Assert.False(result.Success);
        Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));
        Assert.Empty(result.Items);
    }

    [Fact]
    public void TryParseSuggestedKeyResults_FiltersInvalidRowsButKeepsValidOnes()
    {
        var json = """
            [
              {"KeyResultName":"Good","TargetValue":10,"Unit":"%","IsInverse":false},
              {"KeyResultName":"","TargetValue":10,"Unit":"%","IsInverse":false},
              {"KeyResultName":"BadTarget","TargetValue":-1,"Unit":"%","IsInverse":false}
            ]
            """;

        var result = OKRsController.TryParseSuggestedKeyResults(json);
        Assert.True(result.Success);
        Assert.Single(result.Items);
        Assert.Equal("Good", result.Items[0].KeyResultName);
    }
}
