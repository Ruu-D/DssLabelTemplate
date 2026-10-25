using System.Reflection;

namespace DssLabelTemplate.Tests;

public class LocalizationTests
{
    private static readonly AppLanguage[] AllLanguages = { AppLanguage.Dutch, AppLanguage.French, AppLanguage.English };

    [Fact]
    public void Dutch_IsTheDefaultLanguage()
    {
        Assert.Equal(AppLanguage.Dutch, Localizer.Current);
        Assert.Equal("nl", new AppSettings().Language);
    }

    [Theory]
    [InlineData(AppLanguage.Dutch, "Template opslaan")]
    [InlineData(AppLanguage.French, "Enregistrer le modèle")]
    [InlineData(AppLanguage.English, "Save template")]
    public void T_GivesTheTextOfTheSelectedLanguage(AppLanguage language, string expected)
    {
        using var scope = new LanguageScope(language);

        Assert.Equal(expected, Strings.SaveTemplate);
    }

    [Fact]
    public void ToCode_AndFromCode_AreEachOthersInverse()
    {
        foreach (AppLanguage language in AllLanguages)
        {
            Assert.Equal(language, Localizer.FromCode(Localizer.ToCode(language)));
        }

        Assert.Equal(AppLanguage.Dutch, Localizer.FromCode("de")); // unknown: Dutch
    }

    /// <summary>Every user-visible text of Strings (properties, and methods with text arguments).</summary>
    private static List<(string Name, string[] Markers, Func<string> Get)> AllTexts()
    {
        var texts = new List<(string, string[], Func<string>)>();
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;

        foreach (PropertyInfo property in typeof(Strings).GetProperties(flags).Where(p => p.PropertyType == typeof(string)))
        {
            texts.Add((property.Name, Array.Empty<string>(), () => (string)property.GetValue(null)!));
        }

        foreach (MethodInfo method in typeof(Strings).GetMethods(flags).Where(m => m.ReturnType == typeof(string) && !m.IsSpecialName))
        {
            // A text argument gets a marker that must come back in the result; a number argument gets 3.
            ParameterInfo[] parameters = method.GetParameters();
            object[] arguments = parameters.Select(p => p.ParameterType == typeof(int) ? (object)3 : "MARK_" + p.Name).ToArray();
            string[] markers = arguments.OfType<string>().ToArray();
            texts.Add((method.Name, markers, () => (string)method.Invoke(null, arguments)!));
        }

        return texts;
    }

    [Fact]
    public void EveryText_ExistsInAllThreeLanguages_AndIsTranslated()
    {
        var texts = AllTexts();
        Assert.NotEmpty(texts);

        foreach (var (name, markers, get) in texts)
        {
            string dutch;
            using (new LanguageScope(AppLanguage.Dutch))
            {
                dutch = get();
            }

            foreach (AppLanguage language in AllLanguages)
            {
                using var scope = new LanguageScope(language);
                string text = get();

                Assert.False(string.IsNullOrWhiteSpace(text), $"{name} is empty in {language}");
                foreach (string marker in markers)
                {
                    Assert.True(text.Contains(marker), $"{name} in {language} does not show its argument {marker}");
                }

                if (language != AppLanguage.Dutch)
                {
                    Assert.True(text != dutch, $"{name} in {language} is still the Dutch text");
                }
            }
        }
    }
}
