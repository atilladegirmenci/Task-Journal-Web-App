using Microsoft.AspNetCore.Mvc;
using System.Data.SQLite;
using System.Diagnostics;
using ToDoWebApp.Models;
using ToDoWebApp.Models.ViewModels;

namespace ToDoWebApp.Controllers
{
	public class HomeController : Controller
	{
		private readonly ILogger<HomeController> _logger;

		public HomeController(ILogger<HomeController> logger)
		{
			_logger = logger;
		}

		public IActionResult Index()
		{
			var todoListViewModel = GetAllToDos();
            var noteListViewModel = GetAllNotes();

            var model = new HomeViewModel
            {
                ToDo = todoListViewModel,
                Note = noteListViewModel,
            };

            return View(model);

        }

		public IActionResult InsertToDo(ToDoViewModel todo)
		{
			using (SQLiteConnection con = new SQLiteConnection("Data Source=db.sqlite"))
			{
				using (var tableCmd = con.CreateCommand())
				{
					con.Open();
					tableCmd.CommandText = $"INSERT INTO todo (name) VALUES ('{todo.ToDoItem.name}')";
					try
					{
						tableCmd.ExecuteNonQuery();
					}
					catch (Exception ex)
					{
						Console.WriteLine(ex.Message);
					}
				}

				return Redirect("Index");

			}
			
		}
		[HttpGet]
		public JsonResult PopulateForm(int id)
		{
			var todo = GetById(id);
			if (todo == null) return Json(new { error = "Todo not found" });
			return Json(todo);

		}

		internal ToDo GetById(int id)
		{
			using (SQLiteConnection con = new SQLiteConnection("Data Source=db.sqlite"))
			{
				con.Open();
				using (var tableCmd = con.CreateCommand())
				{
					tableCmd.CommandText = "SELECT * FROM todo WHERE Id = @id";
					tableCmd.Parameters.AddWithValue("@id", id);
					using (var reader = tableCmd.ExecuteReader())
					{
						if (reader.HasRows)
						{
							reader.Read();
							return new ToDo
							{
								id = reader.GetInt32(0),
								name = reader.GetString(1)
							};
						}
					}
				}
			}
			return new ToDo();
			
		}

		[HttpPost]
		public IActionResult UpdateToDo(ToDoViewModel model)
		{
			if (model.ToDoItem == null)
			{
				Console.WriteLine("Model binding failed! No ToDoItem found.");
				return RedirectToAction("Index");
			}

			Console.WriteLine($"Update method was called! ID: {model.ToDoItem.id}, Name: {model.ToDoItem.name}");

			using (SQLiteConnection con = new SQLiteConnection("Data Source=db.sqlite"))
			{
				con.Open();
				using (var tableCmd = con.CreateCommand())
				{
					tableCmd.CommandText = "UPDATE todo SET name = @name WHERE Id = @id";
					tableCmd.Parameters.AddWithValue("@name", model.ToDoItem.name);
					tableCmd.Parameters.AddWithValue("@id", model.ToDoItem.id);

					try
					{
						int rowsAffected = tableCmd.ExecuteNonQuery();
						Console.WriteLine($"Rows updated: {rowsAffected}");
					}
					catch (Exception ex)
					{
						Console.WriteLine($"SQL Error: {ex.Message}");
					}
				}
			}

			return RedirectToAction("Index");
		}


		public JsonResult Delete(int id)
		{
			using (SQLiteConnection con = new SQLiteConnection("Data Source=db.sqlite"))
			{
				using (var tableCmd = con.CreateCommand())
				{
					con.Open();
					tableCmd.CommandText = $"DELETE from todo WHERE Id= '{id}'";
					try
					{
						tableCmd.ExecuteNonQuery();
					}
					catch(Exception ex)
					{
						Console.WriteLine(ex.Message);
					}
					
				}
			}
			
			return Json(new { });

		}

		internal ToDoViewModel GetAllToDos()
		{
			List<ToDo> todolist = new List<ToDo>();

			using (SQLiteConnection con = new SQLiteConnection("Data Source=db.sqlite"))
			{
				using (var tableCmd = con.CreateCommand())
				{
					con.Open();
					tableCmd.CommandText = "select * from todo";

					using (var reader = tableCmd.ExecuteReader())
					{
						if (reader.HasRows)
						{
							while (reader.Read())
							{
								todolist.Add(
									new ToDo
									{
										id = reader.GetInt32(0),
										name = reader.GetString(1)
									});
							}

							return new ToDoViewModel
							{
								ToDoList = todolist
							};
						}
						else
						{
							return new ToDoViewModel
							{
								ToDoList = todolist
							};
						}
					}
				}

				
			}
		}
		
		[HttpPost]
		public IActionResult InsertNote(NoteViewModel model)
		{
			using (SQLiteConnection con = new SQLiteConnection("Data Source=db.sqlite"))
			{
				con.Open();
				using (var tableCmd = con.CreateCommand())
				{
					tableCmd.CommandText = "INSERT INTO note (title, content) VALUES (@title, @content)";
					tableCmd.Parameters.AddWithValue("@title", model.NoteItem.Title);
					tableCmd.Parameters.AddWithValue("@content", model.NoteItem.Content);

					try
					{
						tableCmd.ExecuteNonQuery();
					}
					catch (Exception ex)
					{
						Console.WriteLine($"SQL Error: {ex.Message}");
					}
				}
			}

			return RedirectToAction("Index");
		}
		[HttpGet]
		public JsonResult GetNoteById(int id)
		{
			var note = GetNote(id);
			if (note == null) return Json(new { error = "Note not found" });
			return Json(note);
		}

		[HttpPost]
		public IActionResult UpdateNote(NoteViewModel model)
		{
			if (model.NoteItem == null)
			{
				Console.WriteLine("Model binding failed! No NoteItem found.");
				return RedirectToAction("Notes");
			}

			using (SQLiteConnection con = new SQLiteConnection("Data Source=db.sqlite"))
			{
				con.Open();
				using (var tableCmd = con.CreateCommand())
				{
					tableCmd.CommandText = "UPDATE note SET title = @title, content = @content WHERE Id = @id";
					tableCmd.Parameters.AddWithValue("@title", model.NoteItem.Title);
					tableCmd.Parameters.AddWithValue("@content", model.NoteItem.Content);
					tableCmd.Parameters.AddWithValue("@id", model.NoteItem.Id);

					try
					{
						tableCmd.ExecuteNonQuery();
					}
					catch (Exception ex)
					{
						Console.WriteLine($"SQL Error: {ex.Message}");
					}
				}
			}

			return RedirectToAction("Index");
		}

		public JsonResult DeleteNote(int id)
		{
			using (SQLiteConnection con = new SQLiteConnection("Data Source=db.sqlite"))
			{
				con.Open();
				using (var tableCmd = con.CreateCommand())
				{
					tableCmd.CommandText = "DELETE from note WHERE Id= @id";
					tableCmd.Parameters.AddWithValue("@id", id);

					try
					{
						tableCmd.ExecuteNonQuery();
					}
					catch (Exception ex)
					{
						Console.WriteLine($"SQL Error: {ex.Message}");
					}
				}
			}

			return Json(new { message = "Note deleted successfully!" });
		}

		internal Note GetNote(int id)
		{
			using (SQLiteConnection con = new SQLiteConnection("Data Source=db.sqlite"))
			{
				con.Open();
				using (var tableCmd = con.CreateCommand())
				{
					tableCmd.CommandText = "SELECT * FROM note WHERE Id = @id";
					tableCmd.Parameters.AddWithValue("@id", id);

					using (var reader = tableCmd.ExecuteReader())
					{
						if (reader.HasRows)
						{
							reader.Read();
							return new Note
							{
								Id = reader.GetInt32(0),
								Title = reader.GetString(1),
								Content = reader.GetString(2)
							};
						}
					}
				}
			}
			return null;
		}

		internal NoteViewModel GetAllNotes()
		{
			List<Note> noteList = new List<Note>();

			using (SQLiteConnection con = new SQLiteConnection("Data Source=db.sqlite"))
			{
				con.Open();
				using (var tableCmd = con.CreateCommand())
				{
					tableCmd.CommandText = "SELECT * FROM note";

					using (var reader = tableCmd.ExecuteReader())
					{
						while (reader.Read())
						{
							noteList.Add(new Note
							{
								Id = reader.GetInt32(0),
								Title = reader.GetString(1),
								Content = reader.GetString(2)
							});
						}
					}
				}
			}

			return new NoteViewModel { NoteList = noteList };
		}
	}

}
