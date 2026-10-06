using System.Net;
using System.Text.RegularExpressions;

namespace BlazorApp.RMTWebsite.Tests.Integration;

/// <summary>
/// Checks the FAQ page (plan task 2.7) in the real rendered HTML. The roadmap's "Done when":
/// questions are collapsible and keyboard-accessible (native &lt;details&gt;/&lt;summary&gt;), and at
/// least eight common questions are answered. The new answers are claims to visitors (billing,
/// payment, cancellations), so they're pinned word for word: changing one has to be deliberate.
/// </summary>
public class FaqPageTests(SiteFactory factory) : IClassFixture<SiteFactory>
{
    /// <summary>One collapsible question as rendered: its &lt;details&gt; attributes, question, and answer markup.</summary>
    private sealed record Question(string Attributes, string Text, string AnswerMarkup);

    /// <summary>Fetches the FAQ page HTML through the in-memory site.</summary>
    private async Task<string> GetFaqAsync() =>
        await factory.CreateClient().GetStringAsync("/faq", TestContext.Current.CancellationToken);

    /// <summary>Strips tags, decodes entities, and collapses whitespace, e.g. "doctor&amp;#x27;s" becomes "doctor's".</summary>
    private static string Text(string markup) =>
        Regex.Replace(WebUtility.HtmlDecode(Regex.Replace(markup, "<[^>]+>", "")), @"\s+", " ").Trim();

    /// <summary>Every &lt;details&gt; on the page, in order, split into its summary and the answer after it.</summary>
    private static List<Question> GetQuestions(string html) =>
        Regex.Matches(html, @"<details\b([^>]*)>\s*<summary\b[^>]*>(.*?)</summary>(.*?)</details>", RegexOptions.Singleline)
            .Select(m => new Question(m.Groups[1].Value, Text(m.Groups[2].Value), m.Groups[3].Value))
            .ToList();

    /// <summary>The text of each &lt;p&gt; in an answer, in order. Blazor renders consecutive paragraphs with no
    /// whitespace between them, so stripping tags from the whole answer would glue sentences together.</summary>
    private static List<string> GetParagraphs(string answerMarkup) =>
        Regex.Matches(answerMarkup, @"<p\b[^>]*>(.*?)</p>", RegexOptions.Singleline)
            .Select(m => Text(m.Groups[1].Value))
            .ToList();

    /// <summary>Opening tag of the CallToAction section. "cta" must be a whole class name, so "cta-band" doesn't match.</summary>
    private const string CtaSectionTag = @"<section\b[^>]*\bclass=""(?:[^""]*\s)?cta(?:\s[^""]*)?""[^>]*>";

    /// <summary>The page's content between the PageHeader and the closing call to action.</summary>
    private static string GetFaqBody(string html)
    {
        var start = html.IndexOf("</header>", html.IndexOf("page-header", StringComparison.Ordinal), StringComparison.Ordinal);
        var end = Regex.Match(html, CtaSectionTag).Index;
        return start >= 0 && end > start ? html[start..end] : "";
    }

    // The roadmap asks for at least eight; each one has a question and at least one answer paragraph,
    // and every paragraph has text in it (an empty <p></p> must fail)
    [Fact]
    public async Task Get_Faq_HasAtLeastEightQuestions()
    {
        // Arrange & Act
        var questions = GetQuestions(await GetFaqAsync());

        // Assert
        Assert.True(questions.Count >= 8, $"Expected at least 8 questions, found {questions.Count}.");
        Assert.All(questions, question =>
        {
            Assert.NotEmpty(question.Text);
            var paragraphs = GetParagraphs(question.AnswerMarkup);
            Assert.NotEmpty(paragraphs);
            Assert.All(paragraphs, paragraph => Assert.NotEmpty(paragraph));
        });
    }

    // Closed by default, so the page reads as a scannable list of questions
    [Fact]
    public async Task Get_Faq_QuestionsAreCollapsedByDefault()
    {
        // Arrange & Act
        var html = await GetFaqAsync();
        var questions = GetQuestions(html);

        // Assert (NotEmpty first: with no questions, "none are open" would pass without checking anything;
        // the count check makes sure no <details> was skipped by the parser and left unchecked)
        Assert.NotEmpty(questions);
        Assert.Equal(Regex.Matches(html, @"<details\b").Count, questions.Count);
        Assert.All(questions, question => Assert.DoesNotMatch(@"(?<![\w-])open(?![\w-])", question.Attributes));
    }

    // The agreed questions, grouped and in order (plan 2.7, Decisions). Parking waits for 3.2
    [Fact]
    public async Task Get_Faq_ShowsAgreedQuestionsInOrder()
    {
        // Arrange & Act
        var questions = GetQuestions(await GetFaqAsync()).Select(question => question.Text);

        // Assert
        Assert.Equal(
            [
                "What is massage therapy?",
                "How will massage therapy benefit me?",
                "How often should I book?",
                "This is my first massage. What should I expect?",
                "What should I wear?",
                "Do I need a doctor's referral?",
                "Do you bill my insurance directly?",
                "What payment methods do you accept?",
                "What is your cancellation policy?",
            ],
            questions);
    }

    // Each group is a <section> named by its h2, with its three questions inside it. The outline goes
    // h1 → h2 with no skipped levels (the old page jumped from h1 to h5)
    [Fact]
    public async Task Get_Faq_GroupsAreSectionsWithH2Headings()
    {
        // Arrange
        var body = GetFaqBody(await GetFaqAsync());

        // Act
        var groups = Regex.Matches(body, @"<section\b[^>]*\baria-labelledby=""([^""]+)""[^>]*>(.*?)</section>", RegexOptions.Singleline)
            .Select(m =>
            {
                var h2 = Regex.Match(m.Groups[2].Value, $@"<h2\b[^>]*\bid=""{Regex.Escape(m.Groups[1].Value)}""[^>]*>(.*?)</h2>", RegexOptions.Singleline);
                return (Heading: h2.Success ? Text(h2.Groups[1].Value) : "(no h2 matching aria-labelledby)",
                        Questions: GetQuestions(m.Groups[2].Value).Count);
            })
            .ToList();

        // Assert
        Assert.Equal(
            [("About massage therapy", 3), ("Your first visit", 3), ("Billing and policies", 3)],
            groups);
        Assert.DoesNotMatch(@"<h[3-6]\b", body);
    }

    // Every answer, word for word: approved copy or built only from confirmed facts (plan 2.7, Decisions,
    // 2026-10-06). Multi-paragraph answers are compared with their paragraphs joined by a space
    [Theory]
    [InlineData("What is massage therapy?",
        "Massage therapy is a clinically oriented healthcare option that involves the hands-on manipulation of the " +
        "body's soft tissues, including muscles, connective tissue, tendons, ligaments, and joints. It's an " +
        "evidence-based practice used to develop, maintain, or rehabilitate physical function, and to relieve pain " +
        "and manage stress.")]
    [InlineData("How will massage therapy benefit me?",
        "Registered massage therapy can relieve muscle tension and pain, increase your range of motion, and lower " +
        "your stress levels. Treatments are tailored to your needs. Your RMT will read over your health history and " +
        "discuss your concerns before your treatment begins.")]
    [InlineData("This is my first massage. What should I expect?",
        "Your RMT will start with a brief conversation about your health and wellness, and may do a few short " +
        "assessments of the area of concern to plan your treatment. During your treatment, the areas you've asked " +
        "about are worked on, and your RMT will check throughout that you're comfortable and that the pressure suits " +
        "you. Afterwards, your RMT will usually discuss their findings and suggest stretches or exercises that may help.")]
    [InlineData("What should I wear?",
        "Wear whatever is comfortable. For most treatments you'll undress to your comfort level and be covered by a sheet " +
        "the whole time, with only the area being treated uncovered. You can also stay fully or partly clothed, and Nick " +
        "can adapt the treatment. Your comfort and consent come first, and you can ask to stop or change anything at any time.")]
    [InlineData("Do I need a doctor's referral?",
        "No. You can book directly with a registered massage therapist in Ontario. Some insurance plans require a referral " +
        "before they'll reimburse you, so check your plan's details.")]
    [InlineData("How often should I book?",
        "It depends on your goals. For a specific injury or ongoing pain, sessions are often closer together at first, then " +
        "spaced out as you improve. For general maintenance and stress relief, many people come every 3–6 weeks. Nick will " +
        "suggest a plan after your first visit.")]
    [InlineData("Do you bill my insurance directly?",
        "Not at this time. You pay at your appointment and receive an official receipt to submit to your extended health " +
        "insurance for reimbursement.")]
    [InlineData("What payment methods do you accept?",
        "Debit, credit card, and Interac e-Transfer. Prices are plus HST.")]
    [InlineData("What is your cancellation policy?",
        "Please give at least 24 hours' notice if you need to cancel or reschedule. Late cancellations and missed " +
        "appointments may be charged the full appointment fee.")]
    public async Task Get_Faq_AnswerMatchesApprovedCopy(string question, string expectedAnswer)
    {
        // Arrange & Act
        var match = GetQuestions(await GetFaqAsync()).SingleOrDefault(q => q.Text == question);

        // Assert
        Assert.NotNull(match);
        Assert.Equal(expectedAnswer, string.Join(" ", GetParagraphs(match.AnswerMarkup)));
    }

    // Answers are paragraphs that reflow at any width. A <br> chops sentences mid-line on phones
    [Fact]
    public async Task Get_Faq_AnswersHaveNoLineBreaks()
    {
        // Arrange & Act
        var questions = GetQuestions(await GetFaqAsync());

        // Assert
        Assert.NotEmpty(questions);
        Assert.All(questions, question => Assert.DoesNotMatch(@"<br\b", question.AnswerMarkup));
    }

    // The roadmap's last step: "Still have questions? Contact us", after the last question
    [Fact]
    public async Task Get_Faq_EndsWithContactCallToAction()
    {
        // Arrange & Act
        var html = await GetFaqAsync();
        var lastQuestionEnd = html.LastIndexOf("</details>", StringComparison.Ordinal);
        var cta = Regex.Match(html, CtaSectionTag + ".*?</section>", RegexOptions.Singleline);

        // Assert
        Assert.True(lastQuestionEnd >= 0, "No questions found.");
        Assert.True(cta.Success, "No CallToAction section found.");
        Assert.True(cta.Index > lastQuestionEnd, "CallToAction should come after the questions.");
        Assert.Matches(@"<h2\b[^>]*>\s*Still have questions\?\s*</h2>", cta.Value);
        Assert.Matches(@"<a\b[^>]*\bhref=""contact""[^>]*>\s*Contact Nick\s*</a>", cta.Value);
    }
}
