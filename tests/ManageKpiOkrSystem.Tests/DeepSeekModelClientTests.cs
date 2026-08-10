using System.Text.Json;
using Manage_KPI_or_OKR_System.Models.AI;
using Manage_KPI_or_OKR_System.Services.AI;
using Xunit;

namespace ManageKpiOkrSystem.Tests;

public class DeepSeekModelClientTests
{
    [Fact]
    public void ParseResponse_RejectsInvalidJson()
    {
        Assert.Throws<AIModelResponseValidationException>(() =>
            DeepSeekModelClient.ParseResponse("not-json", Array.Empty<AIModelToolDefinition>()));
    }

    [Fact]
    public void ParseResponse_RejectsUnapprovedTool()
    {
        const string response = """{"choices":[{"message":{"role":"assistant","content":null,"tool_calls":[{"id":"call_1","type":"function","function":{"name":"delete_data","arguments":"{}"}}]}}]}""";

        Assert.Throws<AIModelResponseValidationException>(() =>
            DeepSeekModelClient.ParseResponse(response, Array.Empty<AIModelToolDefinition>()));
    }

    [Fact]
    public void ParseResponse_AcceptsApprovedToolWithObjectArguments()
    {
        const string response = "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":null,\"tool_calls\":[{\"id\":\"call_1\",\"type\":\"function\",\"function\":{\"name\":\"search_evidence\",\"arguments\":\"{\\\"limit\\\":2}\"}}]}}]}";
        var tool = new AIModelToolDefinition("search_evidence", "Search approved evidence.", "{\"type\":\"object\"}");

        var result = DeepSeekModelClient.ParseResponse(response, new[] { tool });

        var call = Assert.Single(result.ToolCalls);
        Assert.Equal("search_evidence", call.Name);
        Assert.Equal(JsonValueKind.Object, call.Arguments.ValueKind);
        Assert.Equal(2, call.Arguments.GetProperty("limit").GetInt32());
    }
}
