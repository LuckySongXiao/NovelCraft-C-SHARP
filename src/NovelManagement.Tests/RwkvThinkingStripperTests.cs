using NovelManagement.AI.Services.RWKV;
using Xunit;

namespace NovelManagement.Tests;

/// <summary>
/// RwkvThinkingStripper 单元测试：验证 world 模型思维链前缀剥离（样本取自 RWKV 13B 实测输出）。
/// </summary>
public class RwkvThinkingStripperTests
{
    [Fact]
    public void Strip_ExplicitThinkClose_ReturnsProseAfterLastClose()
    {
        var raw = "We need to comply with user request. Let's aim ~180-200 words.</think>The lighthouse on Blackthorn Point had stood for a hundred years.";
        var result = RwkvThinkingStripper.Strip(raw);
        Assert.Equal("The lighthouse on Blackthorn Point had stood for a hundred years.", result);
    }

    [Fact]
    public void Strip_UnclosedThink_DropsTruncatedThinking()
    {
        var raw = "Perfect prose. <think>the model opened thinking again but got cut";
        var result = RwkvThinkingStripper.Strip(raw);
        Assert.Equal("Perfect prose.", result);
    }

    [Fact]
    public void Strip_LeadingPlanningParagraphs_StripsUntilProse()
    {
        var raw = "We need to write an opening paragraph of an English novel. Let's craft. Need to include setting, character, discovery, mood. Let's write.\n" +
                  "We'll write in third person, past tense, descriptive. Let's write.\n" +
                  "Paragraph:\n" +
                  "\"The lighthouse keeper had watched the same sea for forty years.\"";
        var result = RwkvThinkingStripper.Strip(raw);
        Assert.StartsWith("\"The lighthouse keeper", result);
    }

    [Fact]
    public void Strip_MarkdownPlanningBlock_StripsBulletsUntilProse()
    {
        var raw = "**I need to construct a paragraph that captures the essence of the query:** atmospheric, eerie.\n" +
                  "**Drafting:**\n" +
                  "*   **Opening:** Establish the setting. A coastal lighthouse.\n" +
                  "*   **Sensory Details:** The sound of the waves.\n" +
                  "**Writing:**\n" +
                  "The wind whispered secrets through the creaking timbers of the lighthouse.";
        var result = RwkvThinkingStripper.Strip(raw);
        Assert.StartsWith("The wind whispered secrets", result);
    }

    [Fact]
    public void Strip_CleanProse_ReturnsUnchanged()
    {
        var prose = "The lighthouse keeper had watched the same sea for forty years, and he knew the rhythm of its breath.";
        Assert.Equal(prose, RwkvThinkingStripper.Strip(prose));
    }

    [Fact]
    public void Strip_CleanChineseProse_ReturnsUnchanged()
    {
        var prose = "夜色如墨，林晚推开书店的木门，风铃轻响。";
        Assert.Equal(prose, RwkvThinkingStripper.Strip(prose));
    }

    [Fact]
    public void Strip_NullOrEmpty_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, RwkvThinkingStripper.Strip(null));
        Assert.Equal(string.Empty, RwkvThinkingStripper.Strip(string.Empty));
    }

    [Fact]
    public void Strip_AllPlanningNoProse_ReturnsOriginal()
    {
        var raw = "We need to write something. Let's craft.";
        Assert.Equal(raw, RwkvThinkingStripper.Strip(raw));
    }
}
