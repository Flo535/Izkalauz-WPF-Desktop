using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Newtonsoft.Json;
using Izkalauz_WPF.Models;

namespace Izkalauz_WPF
{
    public partial class MainWindow : Window
    {
        private readonly HttpClient _httpClient = new HttpClient { BaseAddress = new Uri("https://localhost:5150/") };
        private List<Recipe>? _allRecipes = new List<Recipe>();
        public ObservableCollection<Recipe> WeeklyMenu { get; set; } = new ObservableCollection<Recipe>();

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += (s, e) => {
                WeeklyMenuList.ItemsSource = WeeklyMenu;
                LoadRecipes();
            };
        }

        private async void LoadRecipes()
        {
            try
            {
                string json = await _httpClient.GetStringAsync("api/Recipes");
                var responseData = JsonConvert.DeserializeObject<RecipeResponse>(json);
                if (responseData?.Items != null)
                {
                    _allRecipes = responseData.Items;
                    Dispatcher.Invoke(() => UpdateRecipeList(_allRecipes));
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => MessageBox.Show($"Hiba: Kérlek indítsd el a Backend-et! ({ex.Message})"));
            }
        }

        private void UpdateRecipeList(List<Recipe>? recipes)
        {
            if (RecipeList != null && recipes != null)
            {
                RecipeList.ItemsSource = null;
                RecipeList.ItemsSource = recipes;
            }
        }

        private void AddToWeeklyMenu(Recipe recipe)
        {
            var day = (TargetDaySelector?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Hétfő";

            if (WeeklyMenu.Any(m => m.Name == recipe.Name && m.AssignedDay == day))
            {
                MessageBox.Show($"{recipe.Name} már szerepel a {day}i listádon!", "Duplikáció", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            WeeklyMenu.Add(new Recipe { Name = recipe.Name, AssignedDay = day, Category = recipe.Category });
        }

        private void ShowRecipeDetail(Recipe r)
        {
            DetailName.Text = r.Name;
            DetailDesc.Text = r.Description;
            DetailHowTo.Text = r.HowTo;
            IngredientsList.ItemsSource = r.Ingredients;
            RecipeListPanel.Visibility = Visibility.Collapsed;
            RecipeDetailPanel.Visibility = Visibility.Visible;
        }

        private void FilterRecipes()
        {
            if (_allRecipes == null || RecipeList == null) return;
            var searchText = SearchBox.Text.ToLower();
            var selectedCat = (CategoryFilter.SelectedItem as ComboBoxItem)?.Content.ToString();

            var filtered = _allRecipes.Where(r =>
                (string.IsNullOrEmpty(searchText) || r.Name!.ToLower().Contains(searchText)) &&
                (selectedCat == "Összes" || r.Category == selectedCat)
            ).ToList();
            UpdateRecipeList(filtered);
        }

        // --- GOMB ESEMÉNYEK ---
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => FilterRecipes();
        private void CategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) => FilterRecipes();
        private void BtnLoad_Click(object sender, RoutedEventArgs e) => LoadRecipes();
        private void BtnCloseDetail_Click(object sender, RoutedEventArgs e) { RecipeDetailPanel.Visibility = Visibility.Collapsed; RecipeListPanel.Visibility = Visibility.Visible; }

        // Ez a gomb törli az egész heti listát
        private void BtnClearMenu_Click(object sender, RoutedEventArgs e)
        {
            if (WeeklyMenu.Any())
            {
                var result = MessageBox.Show("Biztosan törölni szeretnéd a teljes heti menüt?", "Törlés megerősítése", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    WeeklyMenu.Clear();
                }
            }
        }

        private void BtnRemoveOne_Click(object sender, RoutedEventArgs e) { if (sender is Button b && b.Tag is Recipe r) WeeklyMenu.Remove(r); }
        private void BtnRandom_Click(object sender, RoutedEventArgs e) { if (_allRecipes != null && _allRecipes.Any()) ShowRecipeDetail(_allRecipes[new Random().Next(_allRecipes.Count)]); }
        private void BtnInfo_Click(object sender, RoutedEventArgs e) { if (sender is Button b && b.Tag is Recipe r) ShowRecipeDetail(r); }
        private void BtnAddToMenu_Click(object sender, RoutedEventArgs e) { if (sender is Button b && b.Tag is Recipe r) AddToWeeklyMenu(r); }
        private void BtnAddFromDetail_Click(object sender, RoutedEventArgs e)
        {
            var recipe = _allRecipes?.FirstOrDefault(r => r.Name == DetailName.Text);
            if (recipe != null) AddToWeeklyMenu(recipe);
        }

        private void BtnSaveMenu_Click(object sender, RoutedEventArgs e)
        {
            if (!WeeklyMenu.Any()) { MessageBox.Show("A menü még üres!"); return; }
            StringBuilder sb = new StringBuilder().AppendLine("HETI MENÜTERV");
            foreach (var item in WeeklyMenu.OrderBy(x => x.AssignedDay)) sb.AppendLine($"{item.AssignedDay}: {item.Name}");
            try
            {
                string path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "heti_menu.txt");
                System.IO.File.WriteAllText(path, sb.ToString());
                MessageBox.Show("Sikeres mentés az asztalra!");
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
    }

    public class CategoryToColorConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object v, Type t, object p, System.Globalization.CultureInfo c)
        {
            return (v as string) switch { "Leves" => Brushes.DodgerBlue, "Főétel" => Brushes.Crimson, "Desszert" => Brushes.SeaGreen, _ => Brushes.Gray };
        }
        public object ConvertBack(object v, Type t, object p, System.Globalization.CultureInfo c) => throw new NotImplementedException();
    }
}