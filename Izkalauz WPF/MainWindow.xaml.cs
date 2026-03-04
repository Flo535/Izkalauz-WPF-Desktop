using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Data;
using Newtonsoft.Json;
using Izkalauz_WPF.Models;
using System.IO;
using Microsoft.Win32;
using System.Linq;

namespace Izkalauz_WPF
{
    // --- SZÍNKONVERTÁLÓ OSZTÁLY (A MainWindow osztályon kívül!) ---
    public class CategoryToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string category = value?.ToString() ?? "";
            return category switch
            {
                "Leves" => new SolidColorBrush(Colors.Gold),
                "Főétel" => new SolidColorBrush(Colors.Crimson),
                "Desszert" => new SolidColorBrush(Colors.DodgerBlue),
                "Egytálétel" => new SolidColorBrush(Colors.DarkOrange),
                _ => new SolidColorBrush(Colors.DimGray)
            };
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();
    }

    // --- FŐABLAK LOGIKÁJA ---
    public partial class MainWindow : Window
    {
        private readonly HttpClient _client = new HttpClient();

        // Inicializálás üres listával, hogy ne legyen null hiba
        private List<Recipe> _allRecipes = new List<Recipe>();
        private List<Recipe> _myWeeklyMenu = new List<Recipe>();
        private Random _random = new Random();

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string url = "https://localhost:5150/api/Recipes";
                var response = await _client.GetStringAsync(url);
                var result = JsonConvert.DeserializeObject<RecipeResponse>(response);

                // Csak akkor töltjük be, ha nem null
                if (result?.Items != null)
                {
                    _allRecipes = result.Items;
                    ApplyFilter();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hiba az adatok letöltésekor: " + ex.Message);
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();
        private void CategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilter();

        private void ApplyFilter()
        {
            // Biztonsági ellenőrzés a null elemekre
            if (_allRecipes == null || RecipeList == null || CategoryFilter == null || SearchBox == null) return;

            var selectedItem = CategoryFilter.SelectedItem as ComboBoxItem;
            string selectedCat = selectedItem?.Content?.ToString() ?? "Összes";
            string search = SearchBox.Text?.ToLower() ?? "";

            var filtered = _allRecipes.Where(r =>
                (selectedCat == "Összes" || r.Category == selectedCat) &&
                (string.IsNullOrEmpty(search) || (r.Name != null && r.Name.ToLower().Contains(search)))
            ).ToList();

            RecipeList.ItemsSource = filtered;
        }

        private void BtnRandom_Click(object sender, RoutedEventArgs e)
        {
            if (_allRecipes == null || _allRecipes.Count == 0)
            {
                MessageBox.Show("Előbb töltsd be a recepteket a Frissítés gombbal!");
                return;
            }

            var r = _allRecipes[_random.Next(_allRecipes.Count)];

            if (MessageBox.Show($"Mit szólnál ehhez: {r.Name}?\n\nSzeretnéd megnézni a leírást?", "Szerencsekerék", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                string info = $"{r.Name?.ToUpper()}\n\nKategória: {r.Category}\n\nLeírás: {r.Description}\n\nElkészítés:\n{r.HowTo}";
                MessageBox.Show(info, "Recept részletei");
            }
        }

        private void BtnInfo_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is Recipe r)
            {
                string info = $"{r.Name?.ToUpper()}\n\nKategória: {r.Category}\n\nLeírás: {r.Description}\n\nElkészítés:\n{r.HowTo}";
                MessageBox.Show(info, "Infó");
            }
        }

        private void BtnAddToMenu_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var r = btn?.Tag as Recipe;

            if (btn != null && r != null)
            {
                var panel = VisualTreeHelper.GetParent(btn) as StackPanel;
                var cb = panel?.Children.OfType<ComboBox>().FirstOrDefault();
                string day = (cb?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Hétfő";

                // Duplikáció ellenőrzés
                if (_myWeeklyMenu.Any(x => x.Id == r.Id && x.AssignedDay == day))
                {
                    MessageBox.Show("Ezt már hozzáadtad ehhez a naphoz!");
                    return;
                }

                _myWeeklyMenu.Add(new Recipe
                {
                    Id = r.Id,
                    Name = r.Name ?? "Névtelen",
                    Category = r.Category ?? "Egyéb",
                    AssignedDay = day
                });

                RefreshMenu();
            }
        }

        private void BtnRemoveOne_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is Recipe r)
            {
                _myWeeklyMenu.Remove(r);
                RefreshMenu();
            }
        }

        private void RefreshMenu()
        {
            WeeklyMenuList.ItemsSource = null;
            WeeklyMenuList.ItemsSource = _myWeeklyMenu.OrderBy(x => GetDayOrder(x.AssignedDay)).ToList();
        }

        private int GetDayOrder(string? day)
        {
            if (string.IsNullOrEmpty(day)) return 0;
            string[] days = { "Hétfő", "Kedd", "Szerda", "Csütörtök", "Péntek", "Szombat", "Vasárnap" };
            int index = Array.IndexOf(days, day);
            return index == -1 ? 0 : index;
        }

        private void BtnSaveMenu_Click(object sender, RoutedEventArgs e)
        {
            if (_myWeeklyMenu.Count == 0) return;

            SaveFileDialog sfd = new SaveFileDialog { Filter = "Szöveges fájl|*.txt", FileName = "Heti_Menu_Terv" };
            if (sfd.ShowDialog() == true)
            {
                var content = _myWeeklyMenu.OrderBy(x => GetDayOrder(x.AssignedDay))
                                           .Select(x => $"{x.AssignedDay}: {x.Name}");

                File.WriteAllLines(sfd.FileName, new[] { "HETI ÉTREND", "==========" }.Concat(content));
                MessageBox.Show("Mentés sikeres!");
            }
        }

        private void BtnClearMenu_Click(object sender, RoutedEventArgs e)
        {
            if (_myWeeklyMenu.Count > 0 && MessageBox.Show("Biztosan törlöd a teljes heti tervet?", "Törlés", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                _myWeeklyMenu.Clear();
                RefreshMenu();
            }
        }
    }
}