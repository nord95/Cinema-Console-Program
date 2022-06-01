using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Assignment1
{
    public class Movie
    {
        public int ID { get; set; }
        [MaxLength(255)]
        [Required]
        public string Title { get; set; }
        [Column(TypeName = "Date")]
        public DateTime ReleaseDate { get; set; }
        public List<Screening> Screening { get; set; }
    }

    public class Screening
    {
        public int ID { get; set; }
        public DateTime DateTime { get; set; }
        [Required]
        public Movie Movie { get; set; }
        public Int16 Seats { get; set; }
    }

    public class AppDbContext : DbContext
    {
        public DbSet<Movie> Movie { get; set; }
        public DbSet<Screening> Screening { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlServer(@"Data Source=(local)\SQLEXPRESS;Initial Catalog=DataAccessConsoleAssignment;Integrated Security=True");
        }
    }
    public class Program
    {
        private static AppDbContext database;
        public static void Main()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            using (database = new AppDbContext()) 
            { 
                bool running = true;
                while (running)
                {
                    int selected = Utils.ShowMenu("What do you want to do?", new[] {
                        "List Movie",
                        "Add Movie",
                        "Delete Movie",
                        "Load Movie from CSV File",
                        "List Screening",
                        "Add Screening",
                        "Delete Screening",
                        "Exit"
                    });
                    Console.Clear();

                    if (selected == 0) ListMovie();
                    else if (selected == 1) AddMovie();
                    else if (selected == 2) DeleteMovie();
                    else if (selected == 3) LoadMovieFromCSVFile();
                    else if (selected == 4) ListScreening();
                    else if (selected == 5) AddScreening();
                    else if (selected == 6) DeleteScreening();
                    else running = false;

                    Console.WriteLine();
                }
            }
        }
        

        public static void ListMovie()
        {
            
            if (database.Movie.Count() == 0)
            {
                Console.WriteLine("There are no Movie in the database.");
            }
            else
            {
                Console.WriteLine("Movie in database:");
                foreach (var movie in database.Movie)
                {
                    Console.WriteLine("- " + movie.Title + " (" + movie.ReleaseDate.Year + ")");
                }
            }
            
        }

        public static void AddMovie()
        {
            Utils.WriteHeading("Add New Movie");

            Movie movie = new Movie();
            movie.Title = Utils.ReadString("Title:");
            movie.ReleaseDate = Utils.ReadDate("Release date:");

            database.Add(movie);
            database.SaveChanges();
        }

        public static void DeleteMovie()
        {

            if (database.Movie.Count() == 0)
            {
                Console.WriteLine("There are no Movie in the database.");
            }
            else
            {
                List<string> listString = new List<string>();
                List<Movie> listObject = new List<Movie>();

                var Movie = database.Movie;
                foreach (var s in Movie)
                {
                    string movieString = (s.Title + " (" + s.ReleaseDate.Year + ")");
                    listString.Add(movieString);
                    listObject.Add(s);
                }
                string[] movieTitles = listString.ToArray();

                int movieIndex = Utils.ShowMenu("Delete Screening", movieTitles);
                Movie deleteThis = listObject[movieIndex];
                database.Remove(deleteThis);

                database.SaveChanges();

                Console.WriteLine(listString[movieIndex] + " removed.");
            }
        }
        
        public static void LoadMovieFromCSVFile()
        {
            string[] answers = new string[] { "Yes", "no" };

            int answerIndex = Utils.ShowMenu("All existing movies will be deleted, are you sure?", answers);
            if (answerIndex == 0)
            {
                database.Screening.RemoveRange(database.Screening);
                database.Movie.RemoveRange(database.Movie);

                var Movie = ReadMovie();
                foreach (var movie in Movie.Values)
                {
                    database.Add(movie);
                }
                database.SaveChanges();
            }
            else
            {
                Console.WriteLine("No files were deleted");
            }               
        }
        public static Dictionary<int, Movie> ReadMovie()
        {
            string userCSVfile = Utils.ReadString("Please enter name for CSV-file");
            var Movie = new Dictionary<int, Movie>();

            string[] lines = File.ReadAllLines(userCSVfile).Skip(1).ToArray();
            foreach (string line in lines)
            {
                try
                {
                    string[] values = line.Split(',').Select(v => v.Trim()).ToArray();

                    int id = int.Parse(values[0]);
                    string title = values[1];
                    DateTime releaseDate = Convert.ToDateTime(values[2]);

                    Movie[id] = new Movie
                    {
                        Title = title,
                        ReleaseDate = releaseDate
                    };
                }
                catch
                {
                    Console.WriteLine("Could not read movie: " + line);
                }
            }

            return Movie;
        }

        public static void ListScreening()
        {
            if (database.Screening.Count() == 0)
            {
                Console.WriteLine("There are no Screening in the database.");
            }
            else
            {
                Utils.WriteHeading("Screening");
                foreach (var screening in database.Screening.Include(a => a.Movie))
                {
                    string movieTitle = screening.Movie.Title;
                    Console.WriteLine("- " + screening.DateTime + ": " + movieTitle + " (" + screening.Seats + " seats)");
                }
            }
        }

        public static void AddScreening()
        {
            if (database.Movie.Count() == 0)
            {
                Console.WriteLine("You must add a movie before you can add screening.");
                return;
            }

            Utils.WriteHeading("Add New Screening");

            var screening = new Screening();
            string[] movieNames = database.Movie.Select(a => a.Title).ToArray();
            int movieIndex = Utils.ShowMenu("Movie", movieNames);
            string movieName = movieNames[movieIndex];
            screening.Movie = database.Movie.First(a => a.Title == movieName);

            DateTime date = Utils.ReadFutureDate("Day");
            string stringTimeHHMM = Utils.ReadString("Enter time (HH:MM)");
            int timeHH = Convert.ToInt32(stringTimeHHMM.Substring(0,2));
            int timeMM = Convert.ToInt32(stringTimeHHMM.Substring(3, 2));
            TimeSpan ts = new TimeSpan(timeHH, timeMM, 0);
            date = date.Date + ts;  
            screening.DateTime = date; 

            screening.Seats = (short)Utils.ReadInt("Seats");
            
            database.Add(screening);
            database.SaveChanges();
        }

        public static void DeleteScreening()
        {
            if (database.Screening.Count() == 0)
            {
                Console.WriteLine("There are no Screening in the database.");
            }
            else
            {
                Utils.WriteHeading("Delete Screening");
                List<string> listString = new List<string>();
                List<Screening> listObject = new List<Screening>();

                var Screening = database.Screening.Include(s => s.Movie);
                foreach (var s in Screening)
                {
                    string Screeningtring = (s.DateTime + ": " + s.Movie.Title + "(" + s.Seats + ")" );
                    listString.Add(Screeningtring);
                    listObject.Add(s);
                }
                string[] screeningTitles = listString.ToArray();

                int screeningIndex = Utils.ShowMenu("Delete Screening", screeningTitles);
                Screening deleteThis = listObject[screeningIndex];
                database.Remove(deleteThis);

                database.SaveChanges();

                Console.WriteLine(listString[screeningIndex] + " removed.");
            }

        }
    }

    public static class Utils
    {
        public static string ReadString(string prompt)
        {
            Console.Write(prompt + " ");
            string input = Console.ReadLine();
            return input;
        }

        public static int ReadInt(string prompt)
        {
            Console.Write(prompt + " ");
            int input = int.Parse(Console.ReadLine());
            return input;
        }

        public static DateTime ReadDate(string prompt)
        {
            Console.WriteLine(prompt);
            int year = ReadInt("Year:");
            int month = ReadInt("Month:");
            int day = ReadInt("Day:");
            var date = new DateTime(year, month, day);
            return date;
        }

        public static DateTime ReadFutureDate(string prompt)
        {
            var dates = new[]
            {
                DateTime.Now.Date,
                DateTime.Now.AddDays(1).Date,
                DateTime.Now.AddDays(2).Date,
                DateTime.Now.AddDays(3).Date,
                DateTime.Now.AddDays(4).Date,
                DateTime.Now.AddDays(5).Date,
                DateTime.Now.AddDays(6).Date,
                DateTime.Now.AddDays(7).Date
            };
            var wordOptions = new[] { "Today", "Tomorrow" };
            var nameOptions = dates.Skip(2).Select(d => d.DayOfWeek.ToString());
            var options = wordOptions.Concat(nameOptions);
            int daysAhead = ShowMenu(prompt, options.ToArray());
            var selectedDate = dates[daysAhead];
            return selectedDate;
        }

        public static void WriteHeading(string text)
        {
            Console.WriteLine(text);
            string underline = new string('-', text.Length);
            Console.WriteLine(underline);
        }

        public static int ShowMenu(string prompt, string[] options)
        {
            if (options == null || options.Length == 0)
            {
                throw new ArgumentException("Cannot show a menu for an empty array of options.");
            }

            Console.WriteLine(prompt);

            int selected = 0;

            // Hide the cursor that will blink after calling ReadKey.
            Console.CursorVisible = false;

            ConsoleKey? key = null;
            while (key != ConsoleKey.Enter)
            {
                // If this is not the first iteration, move the cursor to the first line of the menu.
                if (key != null)
                {
                    Console.CursorLeft = 0;
                    Console.CursorTop = Console.CursorTop - options.Length;
                }

                // Print all the options, highlighting the selected one.
                for (int i = 0; i < options.Length; i++)
                {
                    var option = options[i];
                    if (i == selected)
                    {
                        Console.BackgroundColor = ConsoleColor.Blue;
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    Console.WriteLine("- " + option);
                    Console.ResetColor();
                }

                // Read another key and adjust the selected value before looping to repeat all of this.
                key = Console.ReadKey().Key;
                if (key == ConsoleKey.DownArrow)
                {
                    selected = Math.Min(selected + 1, options.Length - 1);
                }
                else if (key == ConsoleKey.UpArrow)
                {
                    selected = Math.Max(selected - 1, 0);
                }
            }

            // Reset the cursor and return the selected option.
            Console.CursorVisible = true;
            return selected;
        }
    }
}
