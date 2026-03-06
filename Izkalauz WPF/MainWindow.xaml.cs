using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Data;
using Newtonsoft.Json;
using Izkalauz_WPF.Models;
using Microsoft.Win32;
using System.IO;

namespace Izkalauz_WPF
{
    public class CategoryToColorConverter : IValueConverter
    {
        public object Convert(object? v, Type t, object? p, System.Globalization.CultureInfo c)
        {
            string cat = v?.ToString() ?? "";
            return cat switch { "Leves" => Brushes.Gold, "Főétel" => Brushes.Crimson, "Desszert" => Brushes.DeepSkyBlue, _ => Brushes.Orange };
        }
        public object ConvertBack(object? v, Type t, object? p, System.Globalization.CultureInfo c) => Binding.DoNothing;
    }

    public class MenuEntry : Recipe { }

    public partial class MainWindow : Window
    {
        private readonly HttpClient _client = new HttpClient();
        private List<Recipe> _allRecipes = new List<Recipe>();
        private ObservableCollection<MenuEntry> _weeklyMenu = new ObservableCollection<MenuEntry>();
        private Recipe? _currentlyViewedRecipe; // A dobókockához kell
        private const string ApiUrl = "https://localhost:5150/api/Recipes";

        public MainWindow()
        {
            ServicePointManager.ServerCertificateValidationCallback += (s, c, ch, er) => true;
            InitializeComponent();
            WeeklyMenuList.ItemsSource = _weeklyMenu;
            this.Loaded += (s, e) => LoadData();
        }

        private async void LoadData()
        {
            try
            {
                var json = await _client.GetStringAsync(ApiUrl);
                var response = JsonConvert.DeserializeObject<RecipeResponse>(json);
                if (response?.Items != null) { _allRecipes = response.Items; ApplyFilters(); }
            }
            catch { }
        }

        private void ApplyFilters()
        {
            if (RecipeList == null || CategoryFilter == null || _allRecipes == null) return;
            var filtered = _allRecipes.Where(r =>
                (string.IsNullOrEmpty(SearchBox.Text) || r.Name.ToLower().Contains(SearchBox.Text.ToLower())) &&
                (CategoryFilter.SelectedIndex <= 0 || r.Category == (CategoryFilter.SelectedItem as ComboBoxItem)?.Content.ToString())
            );
            RecipeList.ItemsSource = filtered.ToList();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilters();
        private void CategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilters();
        private void BtnLoad_Click(object sender, RoutedEventArgs e) => LoadData();

        // KÖZÖS HOZZÁADÁS LOGIKA
        private void AddToMenu(Recipe r)
        {
            string day = (TargetDaySelector.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Hétfő";
            if (_weeklyMenu.Any(m => m.Name == r.Name && m.AssignedDay == day))
            {
                MessageBox.Show($"{day} naphoz már hozzáadtad: {r.Name}");
                return;
            }
            _weeklyMenu.Add(new MenuEntry { Name = r.Name, AssignedDay = day });
        }

        private void BtnAddToMenu_Click(object sender, RoutedEventArgs e) { if (sender is Button b && b.Tag is Recipe r) AddToMenu(r); }

        private void BtnAddFromDetail_Click(object sender, RoutedEventArgs e) { if (_currentlyViewedRecipe != null) AddToMenu(_currentlyViewedRecipe); }

        private void ShowInfo(Recipe r)
        {
            _currentlyViewedRecipe = r;
            DetailName.Text = r.Name; DetailDesc.Text = r.Description; DetailHowTo.Text = r.HowTo;
            RecipeListPanel.Visibility = Visibility.Collapsed; RecipeDetailPanel.Visibility = Visibility.Visible;
        }

        private void BtnInfo_Click(object sender, RoutedEventArgs e) { if (sender is Button b && b.Tag is Recipe r) ShowInfo(r); }
        private void BtnCloseDetail_Click(object sender, RoutedEventArgs e) { RecipeDetailPanel.Visibility = Visibility.Collapsed; RecipeListPanel.Visibility = Visibility.Visible; }
        private void BtnRandom_Click(object sender, RoutedEventArgs e) { if (_allRecipes.Count > 0) ShowInfo(_allRecipes[new Random().Next(_allRecipes.Count)]); }
        private void BtnRemoveOne_Click(object sender, RoutedEventArgs e) { if (sender is Button b && b.Tag is MenuEntry m) _weeklyMenu.Remove(m); }
        private void BtnClearMenu_Click(object sender, RoutedEventArgs e) => _weeklyMenu.Clear();

        private void BtnSaveMenu_Click(object sender, RoutedEventArgs e)
        {
            if (_weeklyMenu.Count == 0) return;
            SaveFileDialog sfd = new SaveFileDialog { Filter = "Text Files (*.txt)|*.txt", FileName = "HetiMenü.txt" };
            if (sfd.ShowDialog() == true) File.WriteAllLines(sfd.FileName, _weeklyMenu.Select(m => $"{m.AssignedDay}: {m.Name}"));
        }
    }
}