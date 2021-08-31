using System;
using System.Collections.Generic;
using System.Text;

namespace CTA.FeatureDetection.Common.Extensions
{
    public static class DictionaryExtensions
    {
        public static void AddKeyValue(this Dictionary<string, List<string>> dictionary, string key, string value)
        {
            if (!dictionary.ContainsKey(key))
            {
                dictionary.Add(key, new List<string>());
            }
            dictionary[key].Add(value);
        }
    }
}
