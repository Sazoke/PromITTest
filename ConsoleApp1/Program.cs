using ConsoleApp1;
using ConsoleApp1.Migrations;
using Microsoft.Extensions.Configuration;

var configurationBuilder = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json");
var configuration = configurationBuilder.Build();
var connectionString = configuration.GetConnectionString("Default")!;

var migrator = new Migrator(connectionString);
migrator.MigrateUp();
var dataAccess = new DataAccess(connectionString);

await Parallel.ForEachAsync(args, async (file, ct) =>
{
    if (!File.Exists(file))
        return;
    var words = new Dictionary<string, WordStat>();
    await foreach (var line in File.ReadLinesAsync(file, ct))
        AnalyzeLine(line, words);
    await dataAccess.SaveStatsAsync(words.Values.ToList());

});
return;

void AnalyzeLine(string line, Dictionary<string, WordStat> words)
{
    var isWord = false;
    var wordStartIndex = 0;
    for (var i = 0; i < line.Length; i++)
    {
        if (!char.IsLetterOrDigit(line[i]))
        {
            if (isWord)
                SetWord(line, wordStartIndex, i, words);
            isWord = false;
            continue;
        }

        if (!isWord)
            wordStartIndex = i;
        isWord = true;
    }
    if (isWord)
        SetWord(line, wordStartIndex, line.Length, words);
}

void SetWord(string line, int wordStartIndex, int currentIndex, Dictionary<string, WordStat> words)
{
    var word = line.Substring(wordStartIndex, currentIndex - wordStartIndex).ToLower();
    if (words.TryGetValue(word, out var stat))
        stat.Count++;
    else
        words[word] = new WordStat(word, 1);
}