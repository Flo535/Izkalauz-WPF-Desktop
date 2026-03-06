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
    // Kategória színkódoló konverter
    public class CategoryToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            string category = value?.ToString() ?? "";
            return category switch
            {
                "Leves" => Brushes.Gold,
                "Főétel" => Brushes.Crimson,
                "Desszert" => Brushes.DeepSkyBlue,
                "Egyéb" => Brushes.DarkGray, // Dani kérése alapján az "Egyéb" is kapott színt
                _ => Brushes.Orange
            };
        }
        public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) => Binding.DoNothing;
    }

    // A heti menü bejegyzéseihez használt osztály
    public class MenuEntry : Recipe { }

    public partial class MainWindow : Window
    {
        private readonly HttpClient _client = new HttpClient();
        private List<Recipe> _allRecipes = new List<Recipe>();
        private ObservableCollection<MenuEntry> _weeklyMenu = new ObservableCollection<MenuEntry>();
        private Recipe? _currentlyViewedRecipe;

        private const string ApiUrl = "https://localhost:5150/api/Recipes";
        private const string CachePath = "recipes_cache.json"; // Az offline fájl neve

        public MainWindow()
        {
            // SSL tanúsítvány hiba elkerülése localhost esetén
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

            InitializeComponent();

            WeeklyMenuList.ItemsSource = _weeklyMenu;

            // Betöltés az ablak megjelenésekor
            this.Loaded += (s, e) => LoadData();
        }

        private async void LoadData()
        {
            try
            {
                // 1. Próbálkozás: Letöltés az API-ról
                var json = await _client.GetStringAsync(ApiUrl);

                // Ha sikerült, elmentjük offline használatra (cache)
                File.WriteAllText(CachePath, json);

                ProcessRawJson(json);
            }
            catch (Exception)
            {
                // 2. Próbálkozás: Ha nincs net, megnézzük a helyi fájlt
                if (File.Exists(CachePath))
                {
                    var cachedJson = File.ReadAllText(CachePath);
                    ProcessRawJson(cachedJson);
                    MessageBox.Show("Offline mód: Nincs internetkapcsolat, a legutóbb mentett adatok töltődtek be.", "Információ", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Hiba: Nem sikerült elérni a szervert, és nincs elmentett offline adat sem.", "Hálózati hiba", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ProcessRawJson(string json)
        {
            var response = JsonConvert.DeserializeObject<RecipeResponse>(json);
            if (response?.Items != null)
            {
                _allRecipes = response.Items;
                ApplyFilters();
            }
        }

        private void ApplyFilters()
        {
            if (RecipeList == null || CategoryFilter == null || _allRecipes == null) return;

            string searchText = SearchBox.Text.ToLower();
            string? selectedCategory = (CategoryFilter.SelectedItem as ComboBoxItem)?.Content.ToString();

            var filtered = _allRecipes.Where(r =>
                (string.IsNullOrEmpty(searchText) || r.Name.ToLower().Contains(searchText)) &&
                (CategoryFilter.SelectedIndex <= 0 || r.Category == selectedCategory)
            );

            RecipeList.ItemsSource = filtered.ToList();
        }

        // Eseménykezelők
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilters();
        private void CategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilters();
        private void BtnLoad_Click(object sender, RoutedEventArgs e) => LoadData();

        private void AddToMenu(Recipe r)
        {
            // A napválasztó most már a jobb oldalon van (Dani kérése)
            string day = (TargetDaySelector.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Hétfő";

            if (_weeklyMenu.Any(m => m.Name == r.Name && m.AssignedDay == day))
            {
                MessageBox.Show($"{day} naphoz már hozzáadtad ezt az ételt: {r.Name}");
                return;
            }

            _weeklyMenu.Add(new MenuEntry { Name = r.Name, AssignedDay = day });
        }

        private void BtnAddToMenu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Recipe recipe)
            {
                AddToMenu(recipe);
            }
        }

        private void BtnAddFromDetail_Click(object sender, RoutedEventArgs e)
        {
            if (_currentlyViewedRecipe != null)
            {
                AddToMenu(_currentlyViewedRecipe);
            }
        }

        private void ShowInfo(Recipe r)
        {
            _currentlyViewedRecipe = r;
            DetailName.Text = r.Name;
            DetailDesc.Text = string.IsNullOrEmpty(r.Description) ? "Nincs leírás." : r.Description;
            DetailHowTo.Text = string.IsNullOrEmpty(r.HowTo) ? "Nincs elkészítési útmutató." : r.HowTo;

            RecipeListPanel.Visibility = Visibility.Collapsed;
            RecipeDetailPanel.Visibility = Visibility.Visible;
        }

        private void BtnInfo_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Recipe recipe)
            {
                ShowInfo(recipe);
            }
        }

        private void BtnCloseDetail_Click(object sender, RoutedEventArgs e)
        {
            RecipeDetailPanel.Visibility = Visibility.Collapsed;
            RecipeListPanel.Visibility = Visibility.Visible;
        }

        private void BtnRandom_Click(object sender, RoutedEventArgs e)
        {
            if (_allRecipes.Count > 0)
            {
                var random = new Random();
                var recipe = _allRecipes[random.Next(_allRecipes.Count)];
                ShowInfo(recipe);
            }
        }

        private void BtnRemoveOne_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is MenuEntry entry)
            {
                _weeklyMenu.Remove(entry);
            }
        }

        private void BtnClearMenu_Click(object sender, RoutedEventArgs e) => _weeklyMenu.Clear();

        private void BtnSaveMenu_Click(object sender, RoutedEventArgs e)
        {
            if (_weeklyMenu.Count == 0) return;

            SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = "Szöveges fájl (*.txt)|*.txt",
                FileName = "Heti_Etrend.txt"
            };

            if (sfd.ShowDialog() == true)
            {
                var lines = _weeklyMenu
                    .OrderBy(m => GetDayOrder(m.AssignedDay))
                    .Select(m => $"{m.AssignedDay}: {m.Name}");

                File.WriteAllLines(sfd.FileName, lines);
                MessageBox.Show("Menü elmentve!");
            }
        }

        // Segédfüggvény a napok sorbarendezéséhez
        private int GetDayOrder(string day)
        {
            return day switch
            {
                "Hétfő" => 1,
                "Kedd" => 2,
                "Szerda" => 3,
                "Csütörtök" => 4,
                "Péntek" => 5,
                "Szombat" => 6,
                "Vasárnap" => 7,
                _ => 8
            };
        }
    }
}