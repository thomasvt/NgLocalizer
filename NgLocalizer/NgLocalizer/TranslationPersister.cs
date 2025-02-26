using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NgLocalizer
{
    public class TranslationPersister
    {
        private readonly string _i18nFolder;

        public TranslationPersister(string i18nFolder)
        {
            _i18nFolder = i18nFolder;
        }

        public JObject LoadTranslationSet(string language)
        {
            var filename = GetLanguageFilename(language);
            if (!File.Exists(filename))
                File.WriteAllText(filename, "{ }");
            var obj = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(filename));
            return obj;
        }

        private void SaveTranslationSet(string language, JObject obj)
        {
            var filename = GetLanguageFilename(language);
            File.WriteAllText(filename, JsonConvert.SerializeObject(obj, Formatting.Indented));
        }

        public void SaveTranslationText(string language, string key, string textValue)
        {
            var set = LoadTranslationSet(language);
            SetNode(language, key, textValue, set);
            SaveTranslationSet(language, set);
        }

        /// <summary>
        /// creates a node in the set at the given path (=key), also creates missing parentnodes of the path.
        /// </summary>
        private static void SetNode(string language, string key, string textValue, JObject set)
        {
            if (textValue == null)
                return; // don't add null

            var path = key.Split('.');
            var current = (JToken)set;
            var lastPart = path.Last();
            var parentPath = string.Empty;
            foreach (var pathPart in path)
            {
                if (string.IsNullOrWhiteSpace(pathPart))
                    throw new FormatException($"Key \"{key}\" is not a valid translation resource path.");

                if (current is JObject obj)
                {
                    if (obj.ContainsKey(pathPart))
                    {
                        // pathpart exists, move current to it:
                        current = current[pathPart];
                        if (pathPart == path.Last())
                        {
                            if (current is JValue value)
                            {
                                value.Value = textValue;
                            }
                            else
                                throw new Exception($"Path \"{key}\" exists but does not lead to a JValue. A longer Key probably already is used for another TextItem.");
                        }
                    }
                    else
                    {
                        // child part of path doesnt exist yet, create it
                        current = pathPart == lastPart
                            ? JToken.FromObject(textValue)
                            : JObject.FromObject(new object());
                        SortedInsert(obj, pathPart, current);
                    }
                }
                else if (current is JValue)
                {
                    throw new Exception($"{key} is a subpath of {parentPath} which is used as Key of another TextItem. Rename either of both keys. \n\n(The json translation files don't support this: a path cannot lead to both a string value ánd lead further down to more subobject(s).)");
                }
                else
                {
                    throw new Exception($"Cannot build path in {language}-translation file: {pathPart} is not a JObject.");
                }
                parentPath += parentPath == string.Empty ? pathPart : "." + pathPart;
            }
        }

        private static void SortedInsert(JObject parent, string name, JToken value)
        {
            var nameLower = name.ToLower();
            foreach (var child in parent.Children<JProperty>())
            {
                if (child.Name.ToLower().CompareTo(nameLower) == 1)
                {
                    child.AddBeforeSelf(new JProperty(name, value));
                    return;
                }
            }
            parent.Add(new JProperty(name, value));
        }

        private string GetLanguageFilename(string language)
        {
            return Path.Combine(_i18nFolder, language + ".json");
        }

        public void RemoveTranslationText(string language, string treeItemKey)
        {
            var path = treeItemKey.Split('.');
            var set = LoadTranslationSet(language);
            RemoveLeafAndClean(set, path, 0);
            SaveTranslationSet(language, set);
        }

        private void RemoveLeafAndClean(JToken token, string[] path, int index)
        {
            if (!(token is JObject obj))
            {
                throw new Exception($"Expected token at index {index} of path {string.Join(".", path)} to be JObject.");
            }

            if (index < path.Length - 1)
            {
                if (obj.ContainsKey(path[index]))
                {
                    var child = obj[path[index]];
                    var childObject = child as JObject;
                    if (childObject == null)
                        return; // key has no translations yet, or it leads to a JValue
                    RemoveLeafAndClean(childObject, path, index + 1);
                    if (childObject.Count == 0)
                    {
                        obj.Remove(path[index]);
                    }
                }
            }
            else
            {

                obj.Remove(path[index]);
            }
        }
    }
}
