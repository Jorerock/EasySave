using System;
using System.Windows.Input;

namespace EasySave.WPF.ViewModels  // ← Reste dans le namespace WPF
{
    /// <summary>
    /// Implémentation réutilisable de ICommand pour WPF.
    /// Permet de créer des commandes facilement sans créer une classe par commande.
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        /// <summary>
        /// Crée une nouvelle commande
        /// </summary>
        /// <param name="execute">Action à exécuter</param>
        /// <param name="canExecute">Condition pour savoir si la commande peut s'exécuter</param>
        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// Événement déclenché quand l'état de CanExecute change
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// Détermine si la commande peut s'exécuter
        /// </summary>
        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        /// <summary>
        /// Exécute la commande
        /// </summary>
        public void Execute(object parameter)
        {
            _execute(parameter);
        }
    }
}