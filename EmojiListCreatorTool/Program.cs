using Newtonsoft.Json;

namespace EmojiListCreatorTool {
    class Program {
        static void Main(string[] args) {
            Console.WriteLine("Emoji list creation tool by ELOR");
            Run().Wait();
        }

        private static async Task Run() {
            string lang = null;
            string cmdLine = Environment.CommandLine;
            if (!String.IsNullOrEmpty(cmdLine)) {
                string[] cmds = cmdLine.Split(' ');
                if (cmds.Length >= 2) {
                    lang = cmds[1];
                }
            }

            if (String.IsNullOrEmpty(lang)) {
                lang = "en";
                Console.WriteLine("You can run this program with language command-line parameter to get localized keywords and group names.");
                Console.WriteLine($"Example: \"{AppDomain.CurrentDomain.FriendlyName} ru\". Default is \"{lang}\"\n");
            } else {
                Console.WriteLine($"Language: \"{lang}\"\n");
            }

            Console.WriteLine("Getting emoji list...");
            HttpClient hc = new HttpClient();
            var resp1 = await hc.GetAsync(new Uri("https://raw.githubusercontent.com/iamcal/emoji-data/master/emoji.json"));
            string emojiJson = await resp1.Content.ReadAsStringAsync();

            Console.WriteLine("Parsing emoji list...");
            List<Emoji> emojiList = JsonConvert.DeserializeObject<List<Emoji>>(emojiJson);
            emojiList = emojiList.Where(e => e.HasImage).OrderBy(e => e.SortOrder).ToList();
            Console.WriteLine($"Total emoji: {emojiList.Count}");

            Console.WriteLine("Getting localized emoji names...");
            var resp2 = await hc.GetAsync(new Uri($"https://cdn.jsdelivr.net/npm/emojibase-data/{lang}/compact.json"));
            string localizedEmojiJson = await resp2.Content.ReadAsStringAsync();

            Console.WriteLine("Parsing localized emoji names...");
            List<LocalizedEmoji> localizedEmojiList = JsonConvert.DeserializeObject<List<LocalizedEmoji>>(localizedEmojiJson);
            Console.WriteLine($"Total localized emoji names: {localizedEmojiList.Count}");

            Console.WriteLine("Getting localized emoji group names...");
            var resp3 = await hc.GetAsync(new Uri($"https://cdn.jsdelivr.net/npm/emojibase-data/{lang}/messages.json"));
            string localizedGroupsJson = await resp3.Content.ReadAsStringAsync();

            Console.WriteLine("Parsing localized emoji group names...");
            EmojiBaseMessageResponse ebmr = JsonConvert.DeserializeObject<EmojiBaseMessageResponse>(localizedGroupsJson);
            Console.WriteLine($"Groups: {ebmr.Groups.Count}, skin tones: {ebmr.SkinTones.Count}");

            Console.WriteLine("Setting localized names and fixing categories...");
            foreach (Emoji emoji in emojiList) {
                emoji.Category = emoji.Category.ToLower().Replace(" & ", "-");

                LocalizedEmoji localized = localizedEmojiList.Where(le => le.HexCode == emoji.Unified).FirstOrDefault();
                if (localized == null) localized = localizedEmojiList.Where(le => le.HexCode == emoji.NonQualified).FirstOrDefault();
                if (localized != null) {
                    emoji.Name = localized.Label;
                    emoji.ShortNames = localized.Tags;
                }
            }

            Console.WriteLine("Saving...");
            EmojiExport result = new EmojiExport {
                Emoji = emojiList,
                Groups = ebmr.Groups,
                SkinTones = ebmr.SkinTones
            };
            emojiJson = JsonConvert.SerializeObject(result).Replace("\"has_img_apple\":true,", "");

            string outputFileName = $"emoji_{lang}.json";
            string outputFilePath = Path.Join(Environment.CurrentDirectory, outputFileName);
            File.WriteAllText(outputFilePath, emojiJson);

            Console.WriteLine($"Done! Check {outputFilePath} file.");
        }
    }

    class Emoji {
        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("unified")]
        public string Unified { get; private set; }

        [JsonProperty("non_qualified")]
        public string NonQualified { get; private set; }

        [JsonProperty("sheet_x")]
        public int SheetX { get; private set; }

        [JsonProperty("sheet_y")]
        public int SheetY { get; private set; }

        [JsonProperty("short_names")]
        public List<string> ShortNames { get; set; }

        [JsonProperty("texts")]
        public List<string> Texts { get; private set; }

        [JsonProperty("sort_order")]
        public int SortOrder { get; private set; }

        [JsonProperty("has_img_apple")]
        public bool HasImage { get; private set; }

        [JsonProperty("skin_variations")]
        public Dictionary<string, Emoji> SkinVariations { get; private set; }
    }

    class LocalizedEmoji {
        [JsonProperty("label")]
        public string Label { get; private set; }

        [JsonProperty("hexcode")]
        public string HexCode { get; private set; }

        [JsonProperty("tags")]
        public List<string> Tags { get; private set; }
    }

    class EmojiBaseMessage {
        [JsonProperty("key")]
        public string Key { get; private set; }

        [JsonProperty("message")]
        public string Message { get; private set; }
    }

    class EmojiBaseMessageResponse {
        [JsonProperty("groups")]
        public List<EmojiBaseMessage> Groups { get;  set; }

        [JsonProperty("skinTones")]
        public List<EmojiBaseMessage> SkinTones { get; set; }
    }

    class EmojiExport : EmojiBaseMessageResponse {
        [JsonProperty("emoji")]
        public List<Emoji> Emoji { get; set; }
    }
}