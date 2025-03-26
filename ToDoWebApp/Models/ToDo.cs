namespace ToDoWebApp.Models
{
	public class ToDo
	{
        public int id { get; set; }
        public string? name { get; set; }
		public List<SubTask>? SubTasks { get; set; } = new List<SubTask>();
    }
}
