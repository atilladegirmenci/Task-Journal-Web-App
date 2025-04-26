using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
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
                try
                {
                    con.Open();

                    using (var transaction = con.BeginTransaction())  // Transaction başlatıyoruz
                    {
                        // ToDo ekliyoruz
                        var todoCmd = con.CreateCommand();
                        todoCmd.CommandText = "INSERT INTO todo (name) VALUES (@name);";
                        todoCmd.Parameters.AddWithValue("@name", todo.ToDoItem.name);
                        todoCmd.ExecuteNonQuery();

                        // Son eklenen ToDo'nun ID'sini alıyoruz
                        var todoIdCmd = con.CreateCommand();
                        todoIdCmd.CommandText = "SELECT last_insert_rowid();";
                        var lastInsertedId = todoIdCmd.ExecuteScalar();

                        // Eğer SubTask varsa, her birini ekliyoruz
                        if (todo.ToDoItem.SubTasks != null && todo.ToDoItem.SubTasks.Any())
                        {
                            foreach (var subtask in todo.ToDoItem.SubTasks)
                            {
                                var subtaskCmd = con.CreateCommand();
                                subtaskCmd.CommandText = "INSERT INTO SubTasks (ToDoId, Name) VALUES (@todoId, @name);";
                                subtaskCmd.Parameters.AddWithValue("@todoId", lastInsertedId);  // Son eklenen ToDo ID'si
                                subtaskCmd.Parameters.AddWithValue("@name", subtask.Content);
                                subtaskCmd.ExecuteNonQuery();
                            }
                        }

                        // Transaction'ı commit ediyoruz
                        transaction.Commit();
                    }

                    return RedirectToAction("Index");  // Index sayfasına yönlendiriyoruz
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message); 
                    return View(todo);                  }
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
                    // ToDo'yu almak için sorgu
                    tableCmd.CommandText = "SELECT * FROM todo WHERE Id = @id";
                    tableCmd.Parameters.AddWithValue("@id", id);

                    using (var reader = tableCmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            reader.Read();
                            var todo = new ToDo
                            {
                                id = reader.GetInt32(0),
                                name = reader.GetString(1),
                                // Diğer ToDo özellikleri buraya eklenebilir
                            };

                            // SubTasks'leri almak için sorgu
                            var subtaskCmd = con.CreateCommand();
                            subtaskCmd.CommandText = "SELECT * FROM SubTasks WHERE TodoId = @id";
                            subtaskCmd.Parameters.AddWithValue("@id", id);

                            using (var subtaskReader = subtaskCmd.ExecuteReader())
                            {
                                // SubTask'leri ToDo'ya ekle
                                var subTasks = new List<SubTask>();
                                while (subtaskReader.Read())
                                {
                                    subTasks.Add(new SubTask
                                    {
                                        Id = subtaskReader.GetInt32(0),
										ToDoId = subtaskReader.GetInt32(1),
                                        Content = subtaskReader.GetString(2),
                                       
                                    });
                                }

                                // SubTasks'leri ToDo nesnesine ata
                                todo.SubTasks = subTasks;
                            }

                            return todo;
                        }
                    }
                }
            }

            return null;  

        }

		[HttpPost]
		public async Task<IActionResult> UpdateSubTaskStatus(int id, bool isCompleted)
		{
			using (SQLiteConnection con = new SQLiteConnection("Data Source=db.sqlite"))
			{
			     con.Open();

				// SQL sorgusu: IsCompleted değerini güncelleme
                var command = con.CreateCommand();
                command.CommandText = "UPDATE SubTasks SET IsCompleted = @IsCompleted WHERE Id = @Id";
				command.Parameters.AddWithValue("@IsCompleted", isCompleted);
				command.Parameters.AddWithValue("@Id", id);

				// Sorguyu çalıştırma
				int result = await command.ExecuteNonQueryAsync();

				if (result > 0)
				{
					return Ok();  // Başarılı yanıt
				}
				else
				{
					return BadRequest();  // Hata durumunda
				}
			}

			// Güncelleme işleminden sonra, aynı sayfaya yönlendirebiliriz
			return RedirectToAction("Index");  // Ya da uygun bir sayfaya yönlendirin
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
                using (var transaction = con.BeginTransaction())
                {
                    try
                    {
                        //  Ana görevin adını güncelle
                        using (var tableCmd = con.CreateCommand())
                        {
                            tableCmd.CommandText = "UPDATE todo SET name = @name WHERE Id = @id";
                            tableCmd.Parameters.AddWithValue("@name", model.ToDoItem.name);
                            tableCmd.Parameters.AddWithValue("@id", model.ToDoItem.id);
                            int rowsAffected = tableCmd.ExecuteNonQuery();
                            Console.WriteLine($"Todo updated: {rowsAffected} row(s)");
                        }

                        // delete current subtasks first
                        using (var deleteCmd = con.CreateCommand())
                        {
                            deleteCmd.CommandText = "DELETE FROM SubTasks WHERE ToDoId = @todoId";
                            deleteCmd.Parameters.AddWithValue("@todoId", model.ToDoItem.id);
                            deleteCmd.ExecuteNonQuery();
                            Console.WriteLine("Old subtasks deleted.");
                        }

                        // add new ones after
                        model.ToDoItem.SubTasks ??= new List<SubTask>();

                        Console.WriteLine("🔍 Incoming SubTasks from form:");

                        if (!model.ToDoItem.SubTasks.Any())
                        {
                            Console.WriteLine("⚠️ No subtasks received! SubTasks is empty.");
                        }
                        else
                        {
                            for (int i = 0; i < model.ToDoItem.SubTasks.Count; i++)
                            {
                                var sub = model.ToDoItem.SubTasks[i];
                                Console.WriteLine($"📌 SubTask[{i}]: {sub?.Content}");
                            }
                        }
                        foreach (var subtask in model.ToDoItem.SubTasks)
                            {
                                using (var subtaskCmd = con.CreateCommand())
                                {
                                    subtaskCmd.CommandText = "INSERT INTO SubTasks (Name, ToDoId) VALUES (@content, @todoId)";
                                    subtaskCmd.Parameters.AddWithValue("@content", subtask.Content);
                                    subtaskCmd.Parameters.AddWithValue("@todoId", model.ToDoItem.id);
                                    subtaskCmd.ExecuteNonQuery();
                                }
                                Console.WriteLine($"Inserted subtask: {subtask.Content}");
                            }
                        

                        transaction.Commit();
                        Console.WriteLine("Transaction committed successfully.");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback(); 
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
					//delete subtasks of todo
					con.Open();
                    tableCmd.CommandText = $"DELETE FROM  SubTasks WHERE ToDoId = {id}";
                    try
                    {
                        tableCmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error deleting subtasks: " + ex.Message);
                        return Json(new { success = false, message = "Failed to delete subtasks." });
                    }
					
					//delete the todo
                    tableCmd.CommandText = $"DELETE from todo WHERE Id = {id}";
                    try
                    {
                        tableCmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error deleting todo: " + ex.Message);
                        return Json(new { success = false, message = "Failed to delete to-do." });
                    }

                }
			}
			
			return Json(new { success = true, message = "To-Do deleted successfully." });

		}


		internal ToDoViewModel GetAllToDos()
		{
            List<ToDo> todolist = new List<ToDo>();

            using (SQLiteConnection con = new SQLiteConnection("Data Source=db.sqlite"))
            {
                using (var tableCmd = con.CreateCommand())
                {
                    con.Open();

                    // ToDo ve SubTask tablolarını birleştiren SQL sorgusu
                    tableCmd.CommandText = @"
                SELECT t.Id AS ToDoId, t.Name AS ToDoName, 
                       s.Id AS SubTaskId, s.Name AS SubTaskContent, s.IsCompleted 
                FROM todo t
                LEFT JOIN SubTasks s ON t.id = s.ToDoId
                ORDER BY t.Id, s.Id"; 

                    using (var reader = tableCmd.ExecuteReader())
                    {
                        Dictionary<int, ToDo> todoDict = new Dictionary<int, ToDo>();

                        while (reader.Read())
                        {
                            int todoId = reader.GetInt32(0);
                            string todoName = reader.GetString(1);
                            int? subTaskId = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2);
                            string? subTaskContent = reader.IsDBNull(3) ? null : reader.GetString(3);
							bool? isCompleted = reader.IsDBNull(4) ? (bool?)null : reader.GetBoolean(4); 

							// Eğer görev daha önce eklenmediyse, listeye ekle
							if (!todoDict.ContainsKey(todoId))
                            {
                                todoDict[todoId] = new ToDo
                                {
                                    id = todoId,
                                    name = todoName,
                                    SubTasks = new List<SubTask>()
                                };
                            }

                            // Subtask varsa, ekleyelim
                            if (subTaskId.HasValue)
                            {
                                todoDict[todoId].SubTasks.Add(new SubTask
                                {
                                    Id = subTaskId.Value,
                                    Content = subTaskContent,
                                    ToDoId = todoId,
									IsCompleted = isCompleted ?? false
								});
                            }
                        }

                        todolist = todoDict.Values.ToList();
                    }
                }
            }

            return new ToDoViewModel
            {
                ToDoList = todolist
            };
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
