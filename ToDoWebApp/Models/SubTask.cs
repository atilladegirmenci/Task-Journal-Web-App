namespace ToDoWebApp.Models
{
	public class SubTask
	{
        public string Content { get; set; }

		public bool IsCompleted { get; set; }
		public int Id { get; set; }
        public int ToDoId { get; set; }


    }
}
