using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;

namespace EasySave.WPF.Localization
{
    public static class LocalizationManager
    {
        private const string DictPrefix = "Resources/Strings.";

        public static void SetCulture(string cultureName)
        {
            if (string.IsNullOrWhiteSpace(cultureName))
            {
                cultureName = "en-US";
            }

            CultureInfo culture = new CultureInfo(cultureName);

            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            Assembly entryAssembly = Assembly.GetEntryAssembly();
            string assemblyName = entryAssembly != null ? entryAssembly.GetName().Name : "EasySave.WPF";

            string uriString = "pack://application:,,,/" + assemblyName + ";component/" + DictPrefix + cultureName + ".xaml";
            Uri dictUri = new Uri(uriString, UriKind.Absolute);

            ResourceDictionary newDict = new ResourceDictionary();
            newDict.Source = dictUri;

            Collection<ResourceDictionary> merged = Application.Current.Resources.MergedDictionaries;

            // Supprime les anciens dictionnaires Strings.*
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
            return value ?? key;
        }
    }
}
