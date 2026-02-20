using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows;

namespace EasySave.WPF.Localization
{
    public static class LocalizationManager
    {
        private const string DictPrefix = "Resources/Strings.";

        public static void SetCulture(string cultureName)
        {
            CultureInfo culture = new CultureInfo(cultureName);

            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            Uri dictUri = new Uri(
                "pack://application:,,,/EasySaveWPF;component/" + DictPrefix + cultureName + ".xaml",
                UriKind.Absolute);

            ResourceDictionary newDict = new ResourceDictionary();
            newDict.Source = dictUri;

            Collection<ResourceDictionary> merged = Application.Current.Resources.MergedDictionaries;

            List<ResourceDictionary> oldDicts = merged
                .Where(d => d.Source != null &&
                            d.Source.OriginalString.IndexOf(DictPrefix, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            foreach (ResourceDictionary oldDict in oldDicts)
            {
                merged.Remove(oldDict);
            }

            merged.Add(newDict);
        }

        public static string T(string key)
        {
            object resource = Application.Current.TryFindResource(key);
            string value = resource as string;

            if (value == null)
            {
                return key;
            }

            return value;
        }

        public static string T(string key, params object[] args)
        {
            string format = T(key);
            return string.Format(CultureInfo.CurrentUICulture, format, args);
        }
    }
}
