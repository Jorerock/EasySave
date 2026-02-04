using System;
using EasySave.View;
using EasySave.ViewModel;

namespace EasySave
{
    class Program
    {
        static void Main(string[] args)
        {
            // Initialize the localization service
            ILocalizationService localizationService = new LocalizationService();

            // Initialize your ViewModel (you'll need to implement this)
            MainViewModel viewModel = new MainViewModel(localizationService);

            // Create the console view with dependencies
            ConsoleView consoleView = new ConsoleView(viewModel, localizationService);

            // Start the application
            consoleView.Start();

            Console.WriteLine("Application terminated. Press any key to exit...");
            Console.ReadKey();
        }
    }
}
