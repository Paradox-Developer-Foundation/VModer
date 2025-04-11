using System.Globalization;
using System.Text;
using AngleSharp;
using AngleSharp.Html.Parser;
using CsvHelper;

// 根据P社的文档来生成 Modifiers.csv 和 DynamicModifiers.csv 文件, 用作修饰符查询器的数据源

var context = BrowsingContext.New(Configuration.Default);
var parser = context.GetService<IHtmlParser>() ?? new HtmlParser();

string fileText = File.ReadAllText(
    @"D:\SteamLibrary\steamapps\common\Hearts of Iron IV\documentation\modifiers_documentation.html"
);

var modifiers = new Dictionary<string, IEnumerable<string>>(1024);
var dynamicModifiers = new Dictionary<string, IEnumerable<string>>(16);

var document = parser.ParseDocument(fileText);
bool isReady = false;
foreach (var element in document.All)
{
    bool addToDynamicModifiers = false;
    if (element.Id == "modifiers-for-scope-aggressive")
    {
        isReady = true;
    }

    if (element.Id == "a-id-technology-_cost_factortechnology_cost_factor")
    {
        break;
    }

    if (isReady && element.TagName.Equals("li", StringComparison.OrdinalIgnoreCase))
    {
        string? query = element.Children[0].GetAttribute("href");
        if (string.IsNullOrWhiteSpace(query))
        {
            continue;
        }

        if (element.TextContent.Contains('<'))
        {
            addToDynamicModifiers = true;
        }

        var modifier = document.QuerySelector(query);
        var ulElement = modifier?.NextElementSibling;
        if (ulElement is null)
        {
            // 如果没有找到下一个元素, 可能是一个动态生成的修饰符, 嵌套在在一个 <h2> 中
            ulElement = modifier?.ParentElement?.NextElementSibling;
            if (ulElement is null)
            {
                continue;
            }
        }

        var categoriesElement = addToDynamicModifiers
            ? ulElement.Children[0].Children.First(item => item.TextContent.Contains("Categories"))
            : ulElement.Children.First(item => item.TextContent.Contains("Categories"));
        string categories = categoriesElement.TextContent.Split(':')[1];
        if (addToDynamicModifiers)
        {
            dynamicModifiers[modifier!.TextContent] = categories.Split(',', StringSplitOptions.TrimEntries);
        }
        else
        {
            modifiers[modifier!.TextContent] = categories.Split(',', StringSplitOptions.TrimEntries);
        }
    }
}

using var modifierCsv = new CsvWriter(
    new StreamWriter(File.Open("Modifiers.csv", FileMode.Create), new UTF8Encoding(false)),
    CultureInfo.InvariantCulture
);
modifierCsv.WriteField("Name");
modifierCsv.WriteField("Categories");
modifierCsv.NextRecord();

foreach (var element in modifiers)
{
    modifierCsv.WriteField(element.Key);
    modifierCsv.WriteField(string.Join(';', element.Value));
    modifierCsv.NextRecord();
}

using var dynamicModifierCsv = new CsvWriter(
    new StreamWriter(File.Open("DynamicModifiers.csv", FileMode.Create), new UTF8Encoding(false)),
    CultureInfo.InvariantCulture
);

dynamicModifierCsv.WriteField("Name");
dynamicModifierCsv.WriteField("Categories");
dynamicModifierCsv.NextRecord();

foreach (var element in dynamicModifiers)
{
    dynamicModifierCsv.WriteField(element.Key);
    dynamicModifierCsv.WriteField(string.Join(';', element.Value));
    dynamicModifierCsv.NextRecord();
}
