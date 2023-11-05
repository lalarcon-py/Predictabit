using System;
using System.Text;
using System.Windows;
using Predictabit;

namespace PredictabitUI
{
    /// <summary>
    /// Interaction logic for Home.xaml
    /// </summary>
    public partial class Home
    {
        private DatabaseConnection _dbConnection = new DatabaseConnection();
        private Keylogger _keyLogger;
        public Home()
        {
            InitializeComponent();
            _keyLogger = new Keylogger(_dbConnection);
            _keyLogger.StartKeyLogging();
        }
        
        private void New_Click(object sender, RoutedEventArgs e)
        {
            // Handle "New" action
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            // Handle "Open" action
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            // Handle "Exit" action
            Close();
        }

        private void TabStatistics_Click(object sender, RoutedEventArgs e)
        {
            // Handle "Tab Statistics" action
        }
    }
}